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
            SupportedVideoFormats));
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

        lock (session.SyncRoot)
        {
            var captureId = $"capture-{Guid.NewGuid():N}";
            try
            {
                switch (request.Kind)
                {
                    case CaptureKind.Video:
                        // FR-MED-002: continuous numbered-BMP video capture. TargetPath is
                        // the output directory the emulation worker tees frames into.
                        if (format is not ("" or "bmp" or "bmpseq"))
                            return ValueTask.FromResult(new StartCaptureResponse(
                                RpcStatus.InvalidArgument($"Unsupported video format '{request.Format}'. Use bmpseq."), null));
                        session.BeginVideoCapture(captureId, request.TargetPath);
                        break;

                    case CaptureKind.Audio:
                        // FR-MED-003: WAV sound recording, tapped off the live SID -> output path.
                        if (format is not ("" or "wav"))
                            return ValueTask.FromResult(new StartCaptureResponse(
                                RpcStatus.InvalidArgument($"Unsupported audio format '{request.Format}'. Use wav."), null));
                        if (session.AudioCaptureTap is null)
                            return ValueTask.FromResult(new StartCaptureResponse(
                                RpcStatus.Unavailable("This session has no live audio path to record."), null));

                        var dir = Path.GetDirectoryName(request.TargetPath);
                        if (!string.IsNullOrEmpty(dir))
                            Directory.CreateDirectory(dir);
                        var stream = new FileStream(request.TargetPath, FileMode.Create, FileAccess.Write);
                        var recorder = new WavAudioRecorder(stream, 44100, 1);
                        session.BeginAudioCapture(captureId, recorder, stream);
                        break;

                    // Screenshot (and any future one-shot kinds) keep the lightweight
                    // metadata handle; the actual encode happens via CaptureFrame.
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            {
                return ValueTask.FromResult(new StartCaptureResponse(
                    RpcStatus.InvalidArgument($"Could not start capture: {ex.Message}"), null));
            }

            var capture = new CaptureSessionDto(captureId, request.Kind, request.TargetPath, true);
            session.CaptureSessions[capture.CaptureId] = capture;
            return ValueTask.FromResult(new StartCaptureResponse(RpcStatus.Ok(), capture));
        }
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
