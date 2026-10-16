using ViceSharp.Abstractions;
using ViceSharp.Host.Runtime;
using ViceSharp.Protocol;

namespace ViceSharp.Host.Services;

public sealed class VideoServiceHost : IVideoService
{
    private readonly EmulatorRuntimeRegistry _registry;

    public VideoServiceHost(EmulatorRuntimeRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        _registry = registry;
    }

    public ValueTask<GetVideoStatusResponse> GetVideoStatusAsync(
        SessionRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_registry.TryGet(request.SessionId, out var session))
            return ValueTask.FromResult(new GetVideoStatusResponse(HostProtocolMapper.MissingSessionStatus(request.SessionId), null));

        lock (session.SyncRoot)
        {
            var videoChip = session.Machine.Devices.GetByRole(DeviceRole.VideoChip) as IVideoChip;
            if (videoChip is null)
            {
                return ValueTask.FromResult(new GetVideoStatusResponse(
                    RpcStatus.Ok(),
                    new VideoStatusDto(false, 0, 0, session.Machine.GetState().Cycle)));
            }

            return ValueTask.FromResult(new GetVideoStatusResponse(
                RpcStatus.Ok(),
                new VideoStatusDto(true, videoChip.FrameWidth, videoChip.FrameHeight, session.Machine.GetState().Cycle)));
        }
    }

    public ValueTask<GetVideoFrameResponse> GetFrameAsync(
        SessionRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_registry.TryGet(request.SessionId, out var session))
            return ValueTask.FromResult(new GetVideoFrameResponse(HostProtocolMapper.MissingSessionStatus(request.SessionId), null));

        lock (session.SyncRoot)
        {
            var videoChip = session.Machine.Devices.GetByRole(DeviceRole.VideoChip) as IVideoChip;
            if (videoChip is null)
                return ValueTask.FromResult(new GetVideoFrameResponse(RpcStatus.Unavailable("The session has no video chip."), null));

            var frame = new byte[videoChip.FrameBuffer.Length];
            videoChip.FrameBuffer.CopyTo(frame, 0);
            return ValueTask.FromResult(new GetVideoFrameResponse(
                RpcStatus.Ok(),
                new VideoFrameDto(videoChip.FrameWidth, videoChip.FrameHeight, session.Machine.GetState().Cycle, frame)));
        }
    }
}
