using System.IO;
using ViceSharp.Abstractions;
using ViceSharp.Core.Capture;
using ViceSharp.Core.Media;
using ViceSharp.Host.Runtime;
using ViceSharp.Protocol;

namespace ViceSharp.Host.Services;

public sealed class CaptureServiceHost : ICaptureService
{
    private readonly EmulatorRuntimeRegistry _registry;

    public CaptureServiceHost(EmulatorRuntimeRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        _registry = registry;
    }

    // Formats this host can actually encode today. Screenshot (png/bmp) + WAV sound +
    // numbered-BMP video sequence are all wired end-to-end. Container/codec video
    // drivers (ffmpeg/zmbv) advertise additional entries here as they land.
    // The emulated SID drives a single mono channel at the host sample rate; the
    // sound recorders are configured to match it.
    private const int SidSampleRate = 44100;
    private const int SidChannels = 1;

    private static readonly IReadOnlyList<string> SupportedAudioFormats = ["wav"];
    private static readonly IReadOnlyList<CaptureVideoFormatDto> SupportedVideoFormats =
    [
        // A directory of numbered 24-bit BMP frames (frame_NNNNNN.bmp). No external
        // tooling required; the lossless raw frames can be muxed by ffmpeg offline.
        new CaptureVideoFormatDto("bmpseq", "bmp-sequence", ["bmp"], [], RequiresFfmpeg: false),
    ];

    public ValueTask<GetCaptureCapabilitiesResponse> GetCaptureCapabilitiesAsync(
        SessionRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_registry.TryGet(request.SessionId, out _))
            return ValueTask.FromResult(new GetCaptureCapabilitiesResponse(
                HostProtocolMapper.MissingSessionStatus(request.SessionId),
                [], [], []));

        return ValueTask.FromResult(new GetCaptureCapabilitiesResponse(
            RpcStatus.Ok(),
            FrameCapture.ScreenshotFormats,
            SupportedAudioFormats,
            BuildVideoFormats()));
    }

    // The bmpseq sequence is always available; the muxed ffmpeg containers are
    // advertised only when an ffmpeg executable can be located.
    private static IReadOnlyList<CaptureVideoFormatDto> BuildVideoFormats()
    {
        if (!FfmpegLocator.IsAvailable)
            return SupportedVideoFormats;

        var formats = new List<CaptureVideoFormatDto>(SupportedVideoFormats);
        foreach (var f in FfmpegVideoFormats.All)
            formats.Add(new CaptureVideoFormatDto(
                f.Id, f.Container, [f.VideoCodec], [f.AudioCodec],
                RequiresFfmpeg: true,
                SupportsMicrophone: true));
        return formats;
    }

    public ValueTask<ListCapturesResponse> ListCapturesAsync(
        SessionRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_registry.TryGet(request.SessionId, out var session))
            return ValueTask.FromResult(new ListCapturesResponse(
                HostProtocolMapper.MissingSessionStatus(request.SessionId), []));

        lock (session.SyncRoot)
        {
            var captures = new List<CaptureSessionDto>(session.CaptureSessions.Values);
            return ValueTask.FromResult(new ListCapturesResponse(RpcStatus.Ok(), captures));
        }
    }

    public ValueTask<StartCaptureResponse> StartCaptureAsync(
        StartCaptureRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_registry.TryGet(request.SessionId, out var session))
            return ValueTask.FromResult(new StartCaptureResponse(HostProtocolMapper.MissingSessionStatus(request.SessionId), null));

        if (string.IsNullOrWhiteSpace(request.TargetPath))
            return ValueTask.FromResult(new StartCaptureResponse(RpcStatus.InvalidArgument("TargetPath is required."), null));

        var format = (request.Format ?? string.Empty).Trim().ToLowerInvariant();
        var captureId = $"capture-{Guid.NewGuid():N}";

        var response = request.Kind switch
        {
            CaptureKind.Video => StartVideoCapture(session, request, format, captureId),
            CaptureKind.Audio => StartAudioCapture(session, request, format, captureId),
            // Screenshot (and any future one-shot kinds) keep the lightweight
            // metadata handle; the actual encode happens via CaptureFrame.
            _ => StartMetadataCapture(session, request, captureId),
        };
        return ValueTask.FromResult(response);
    }

    private static StartCaptureResponse StartVideoCapture(
        EmulatorRuntimeSession session, StartCaptureRequest request, string format, string captureId)
    {
        // Reject early if a video capture is already active (the atomic backstop for
        // the concurrent-start race is in BeginVideoCapture).
        if (session.IsVideoCaptureActive)
            return new StartCaptureResponse(
                RpcStatus.FailedPrecondition("A video capture is already active for this session."), null);

        // FR-MED-002: numbered-BMP sequence (no external tooling). TargetPath is a directory.
        if (format is "" or "bmp" or "bmpseq")
        {
            if (WantsMicrophone(request))
                return new StartCaptureResponse(
                    RpcStatus.InvalidArgument("Microphone narration is only supported for ffmpeg video formats: mp4, mkv, or avi."),
                    null);

            FrameSequenceCapture sink;
            try
            {
                sink = new FrameSequenceCapture(request.TargetPath, ParseFrameMode(request.Options));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            {
                return new StartCaptureResponse(RpcStatus.InvalidArgument($"Could not start capture: {ex.Message}"), null);
            }

            return RegisterVideo(session, request, captureId, sink, audioTrack: null);
        }

        // FR-MED-004: muxed video+audio via external ffmpeg (mp4/mkv/avi).
        if (FfmpegVideoFormats.TryGet(format, out var videoFormat))
        {
            var ffmpegPath = FfmpegLocator.Locate();
            if (ffmpegPath is null)
                return new StartCaptureResponse(
                    RpcStatus.Unavailable("ffmpeg was not found on PATH (or VICESHARP_FFMPEG). Install ffmpeg to record muxed video."),
                    null);

            int width, height;
            double frameRate;
            bool includeAudio;
            var microphoneInput = ResolveMicrophoneInput(request);
            lock (session.SyncRoot)
            {
                if (session.Machine.Devices.GetByRole(DeviceRole.VideoChip) is not IVideoChip videoChip)
                    return new StartCaptureResponse(RpcStatus.Unavailable("The session has no video chip."), null);
                width = videoChip.FrameWidth;
                height = videoChip.FrameHeight;
                frameRate = (session.Architecture as IProfiledArchitectureDescriptor)?.MachineProfile.RefreshRateHz ?? 50.0;
                includeAudio = session.AudioCaptureTap is not null;
            }

            // Muxed video needs the audio tap; reject if it is already in use (a WAV
            // recording, or another muxed capture) before launching ffmpeg.
            if (includeAudio && session.AudioCaptureTap is { IsRecording: true })
                return new StartCaptureResponse(
                    RpcStatus.FailedPrecondition("Audio is already being recorded for this session."), null);

            FfmpegVideoRecorder recorder;
            try
            {
                // Launch ffmpeg + wait for the socket connect OUTSIDE the session lock
                // so a slow/failed start never stalls other RPCs on the session.
                recorder = new FfmpegVideoRecorder(
                    ffmpegPath, videoFormat, width, height, frameRate, request.TargetPath, includeAudio,
                    SidSampleRate, SidChannels, microphoneInput);
                try
                {
                    recorder.Start();
                }
                catch
                {
                    // Release the ffmpeg process + sockets if Start failed partway
                    // (the caller must not leak a half-started recorder).
                    recorder.Dispose();
                    throw;
                }
            }
            catch (Exception ex) when (ex is InvalidOperationException or IOException or UnauthorizedAccessException or ArgumentException)
            {
                return new StartCaptureResponse(
                    RpcStatus.InvalidArgument($"Could not start ffmpeg video capture: {ex.Message}"), null);
            }

            return RegisterVideo(session, request, captureId, recorder, includeAudio ? recorder : null);
        }

        return new StartCaptureResponse(
            RpcStatus.InvalidArgument($"Unsupported video format '{request.Format}'. Use bmpseq, mp4, mkv or avi."), null);
    }

    // Registers the (already-started) sink as the session's video capture. On a
    // concurrent-start conflict BeginVideoCapture throws; the rejected sink is
    // disposed OFF the session lock and FailedPrecondition is returned.
    private static StartCaptureResponse RegisterVideo(
        EmulatorRuntimeSession session, StartCaptureRequest request, string captureId,
        IVideoCaptureSink sink, IAudioRecorder? audioTrack)
    {
        try
        {
            lock (session.SyncRoot)
            {
                session.BeginVideoCapture(captureId, sink, audioTrack);
                return RegisterCapture(session, request, captureId);
            }
        }
        catch (InvalidOperationException ex)
        {
            sink.Dispose();
            return new StartCaptureResponse(RpcStatus.FailedPrecondition(ex.Message), null);
        }
    }

    private static StartCaptureResponse StartAudioCapture(
        EmulatorRuntimeSession session, StartCaptureRequest request, string format, string captureId)
    {
        // FR-MED-003: WAV sound recording, tapped off the live SID -> output path.
        if (format is not ("" or "wav"))
            return new StartCaptureResponse(
                RpcStatus.InvalidArgument($"Unsupported audio format '{request.Format}'. Use wav."), null);
        if (session.AudioCaptureTap is null)
            return new StartCaptureResponse(
                RpcStatus.Unavailable("This session has no live audio path to record."), null);

        // Reject early if the audio tap is already in use (e.g. a muxed-video
        // recording) before creating the output file.
        if (session.AudioCaptureTap.IsRecording)
            return new StartCaptureResponse(
                RpcStatus.FailedPrecondition("Audio is already being recorded for this session."), null);

        FileStream? stream = null;
        WavAudioRecorder? recorder = null;
        try
        {
            var dir = Path.GetDirectoryName(request.TargetPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            stream = new FileStream(request.TargetPath, FileMode.Create, FileAccess.Write);
            recorder = new WavAudioRecorder(stream, SidSampleRate, SidChannels);
            lock (session.SyncRoot)
            {
                session.BeginAudioCapture(captureId, recorder, stream);
                return RegisterCapture(session, request, captureId);
            }
        }
        catch (InvalidOperationException ex)
        {
            // Lost the concurrent-start race for the tap; release and reject.
            recorder?.Dispose();
            stream?.Dispose();
            return new StartCaptureResponse(RpcStatus.FailedPrecondition(ex.Message), null);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            // Release the file handle + recorder if anything failed after they were
            // allocated (the session only owns them once BeginAudioCapture succeeds).
            recorder?.Dispose();
            stream?.Dispose();
            return new StartCaptureResponse(RpcStatus.InvalidArgument($"Could not start capture: {ex.Message}"), null);
        }
    }

    // BMP-sequence frame selection from the capture options ("frames" = "all" |
    // "unique"). Defaults to writing every frame.
    private static FrameSequenceMode ParseFrameMode(IReadOnlyDictionary<string, string>? options)
    {
        if (options is not null)
        {
            foreach (var kv in options)
            {
                if (kv.Key.Equals("frames", StringComparison.OrdinalIgnoreCase)
                    && kv.Value.Trim().Equals("unique", StringComparison.OrdinalIgnoreCase))
                    return FrameSequenceMode.UniqueFrames;
            }
        }

        return FrameSequenceMode.AllFrames;
    }

    private static bool WantsMicrophone(StartCaptureRequest request)
        => request.CaptureMicrophone
           || !string.IsNullOrWhiteSpace(request.MicrophoneDevice)
           || !string.IsNullOrWhiteSpace(request.MicrophoneInputFormat)
           || TryGetBooleanOption(request.Options, "capture_microphone")
           || TryGetBooleanOption(request.Options, "microphone")
           || TryGetBooleanOption(request.Options, "include_microphone")
           || HasOption(request.Options, "microphone_device")
           || HasOption(request.Options, "microphone_input_format");

    private static FfmpegMicrophoneInput? ResolveMicrophoneInput(StartCaptureRequest request)
    {
        if (!WantsMicrophone(request))
            return null;

        var inputFormat = FirstNonEmpty(
            request.MicrophoneInputFormat,
            GetOption(request.Options, "microphone_input_format"),
            GetOption(request.Options, "microphone_format"));
        var device = FirstNonEmpty(
            request.MicrophoneDevice,
            GetOption(request.Options, "microphone_device"),
            GetOption(request.Options, "microphone"));
        return FfmpegMicrophoneInput.Create(inputFormat, device);
    }

    private static bool TryGetBooleanOption(IReadOnlyDictionary<string, string>? options, string key)
    {
        var value = GetOption(options, key);
        if (value is null)
            return false;

        value = value.Trim();
        return value.Equals("true", StringComparison.OrdinalIgnoreCase)
               || value.Equals("1", StringComparison.OrdinalIgnoreCase)
               || value.Equals("yes", StringComparison.OrdinalIgnoreCase)
               || value.Equals("on", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasOption(IReadOnlyDictionary<string, string>? options, string key)
        => !string.IsNullOrWhiteSpace(GetOption(options, key));

    private static string? GetOption(IReadOnlyDictionary<string, string>? options, string key)
    {
        if (options is null)
            return null;

        foreach (var kv in options)
        {
            if (kv.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
                return kv.Value;
        }

        return null;
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return null;
    }

    private static StartCaptureResponse StartMetadataCapture(
        EmulatorRuntimeSession session, StartCaptureRequest request, string captureId)
    {
        lock (session.SyncRoot)
        {
            return RegisterCapture(session, request, captureId);
        }
    }

    // Records the active capture handle in the session map. Must be called under session.SyncRoot.
    private static StartCaptureResponse RegisterCapture(
        EmulatorRuntimeSession session, StartCaptureRequest request, string captureId)
    {
        var capture = new CaptureSessionDto(captureId, request.Kind, request.TargetPath, true);
        session.CaptureSessions[capture.CaptureId] = capture;
        return new StartCaptureResponse(RpcStatus.Ok(), capture);
    }

    public ValueTask<StopCaptureResponse> StopCaptureAsync(
        StopCaptureRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_registry.TryGet(request.SessionId, out var session))
            return ValueTask.FromResult(new StopCaptureResponse(HostProtocolMapper.MissingSessionStatus(request.SessionId), null));

        CaptureSessionDto stopped;
        bool wasActive;
        lock (session.SyncRoot)
        {
            if (!session.CaptureSessions.TryGetValue(request.CaptureId, out var capture))
                return ValueTask.FromResult(new StopCaptureResponse(RpcStatus.NotFound($"Capture '{request.CaptureId}' was not found."), null));

            // Mark inactive immediately under the lock so the capture map is always
            // consistent even if the off-lock finalisation below throws.
            wasActive = capture.IsActive;
            stopped = capture with { IsActive = false };
            session.CaptureSessions[request.CaptureId] = stopped;
        }

        // Finalise the underlying recorder OUTSIDE session.SyncRoot - the writer
        // flush/join and ffmpeg wait can block for seconds and must not stall other
        // RPCs on the session. Only on the active -> inactive transition (a repeat
        // Stop must not tear down a different live capture).
        if (wasActive)
        {
            switch (stopped.Kind)
            {
                case CaptureKind.Video:
                    session.EndVideoCapture();
                    break;
                case CaptureKind.Audio:
                    session.EndAudioCapture();
                    break;
            }
        }

        return ValueTask.FromResult(new StopCaptureResponse(RpcStatus.Ok(), stopped));
    }

    public async ValueTask<CaptureFrameResponse> CaptureFrameAsync(
        CaptureFrameRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_registry.TryGet(request.SessionId, out var session))
            return new CaptureFrameResponse(HostProtocolMapper.MissingSessionStatus(request.SessionId), null);

        if (string.IsNullOrWhiteSpace(request.FilePath))
            return new CaptureFrameResponse(RpcStatus.InvalidArgument("FilePath is required."), null);

        var format = (request.Format ?? "png").Trim().ToLowerInvariant();
        if (format is not ("png" or "bmp"))
            return new CaptureFrameResponse(
                RpcStatus.InvalidArgument($"Unsupported screenshot format '{request.Format}'. Use png or bmp."),
                null);

        byte[] frame;
        int width;
        int height;
        long cycle;

        lock (session.SyncRoot)
        {
            var videoChip = session.Machine.Devices.GetByRole(DeviceRole.VideoChip) as IVideoChip;
            if (videoChip is null)
                return new CaptureFrameResponse(RpcStatus.Unavailable("The session has no video chip."), null);

            frame = new byte[videoChip.FrameBuffer.Length];
            videoChip.FrameBuffer.CopyTo(frame, 0);
            width = videoChip.FrameWidth;
            height = videoChip.FrameHeight;
            cycle = session.Machine.GetState().Cycle;
        }

        var writtenFormat = await FrameCapture
            .CaptureBgraAsync(frame, width, height, request.FilePath, format, cancellationToken)
            .ConfigureAwait(false);

        return new CaptureFrameResponse(
            RpcStatus.Ok(),
            new CaptureArtifactDto(request.FilePath, writtenFormat, cycle));
    }
}
