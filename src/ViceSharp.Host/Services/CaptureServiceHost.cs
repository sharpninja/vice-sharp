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
            formats.Add(new CaptureVideoFormatDto(f.Id, f.Container, [f.VideoCodec], [f.AudioCodec], RequiresFfmpeg: true));
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
        // FR-MED-002: numbered-BMP sequence (no external tooling). TargetPath is a directory.
        if (format is "" or "bmp" or "bmpseq")
        {
            try
            {
                var sink = new FrameSequenceCapture(request.TargetPath, ParseFrameMode(request.Options));
                lock (session.SyncRoot)
                {
                    session.BeginVideoCapture(captureId, sink);
                    return RegisterCapture(session, request, captureId);
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            {
                return new StartCaptureResponse(RpcStatus.InvalidArgument($"Could not start capture: {ex.Message}"), null);
            }
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
            lock (session.SyncRoot)
            {
                if (session.Machine.Devices.GetByRole(DeviceRole.VideoChip) is not IVideoChip videoChip)
                    return new StartCaptureResponse(RpcStatus.Unavailable("The session has no video chip."), null);
                width = videoChip.FrameWidth;
                height = videoChip.FrameHeight;
                frameRate = (session.Architecture as IProfiledArchitectureDescriptor)?.MachineProfile.RefreshRateHz ?? 50.0;
                includeAudio = session.AudioCaptureTap is not null;
            }

            FfmpegVideoRecorder recorder;
            try
            {
                // Launch ffmpeg + wait for the socket connect OUTSIDE the session lock
                // so a slow/failed start never stalls other RPCs on the session.
                recorder = new FfmpegVideoRecorder(
                    ffmpegPath, videoFormat, width, height, frameRate, request.TargetPath, includeAudio,
                    SidSampleRate, SidChannels);
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

            lock (session.SyncRoot)
            {
                session.BeginVideoCapture(captureId, recorder, includeAudio ? recorder : null);
                return RegisterCapture(session, request, captureId);
            }
        }

        return new StartCaptureResponse(
            RpcStatus.InvalidArgument($"Unsupported video format '{request.Format}'. Use bmpseq, mp4, mkv or avi."), null);
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

        lock (session.SyncRoot)
        {
            if (!session.CaptureSessions.TryGetValue(request.CaptureId, out var capture))
                return ValueTask.FromResult(new StopCaptureResponse(RpcStatus.NotFound($"Capture '{request.CaptureId}' was not found."), null));

            // Finalise the underlying recorder only on the active -> inactive
            // transition (a repeat Stop must not tear down a different live capture).
            if (capture.IsActive)
            {
                switch (capture.Kind)
                {
                    case CaptureKind.Video:
                        session.EndVideoCapture();
                        break;
                    case CaptureKind.Audio:
                        session.EndAudioCapture();
                        break;
                }
            }

            var stopped = capture with { IsActive = false };
            session.CaptureSessions[request.CaptureId] = stopped;
            return ValueTask.FromResult(new StopCaptureResponse(RpcStatus.Ok(), stopped));
        }
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
