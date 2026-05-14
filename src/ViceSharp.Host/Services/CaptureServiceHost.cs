using ViceSharp.Abstractions;
using ViceSharp.Core.Capture;
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

    public ValueTask<StartCaptureResponse> StartCaptureAsync(
        StartCaptureRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_registry.TryGet(request.SessionId, out var session))
            return ValueTask.FromResult(new StartCaptureResponse(HostProtocolMapper.MissingSessionStatus(request.SessionId), null));

        if (string.IsNullOrWhiteSpace(request.TargetPath))
            return ValueTask.FromResult(new StartCaptureResponse(RpcStatus.InvalidArgument("TargetPath is required."), null));

        lock (session.SyncRoot)
        {
            var capture = new CaptureSessionDto($"capture-{Guid.NewGuid():N}", request.Kind, request.TargetPath, true);
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

        await BmpFrameArtifactWriter
            .WriteBgraAsync(frame, width, height, request.FilePath, cancellationToken)
            .ConfigureAwait(false);

        return new CaptureFrameResponse(
            RpcStatus.Ok(),
            new CaptureArtifactDto(request.FilePath, "bmp", cycle));
    }
}
