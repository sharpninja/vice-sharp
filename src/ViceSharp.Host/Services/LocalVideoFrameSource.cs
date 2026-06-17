using ViceSharp.Abstractions;
using ViceSharp.Host.Runtime;
using ViceSharp.Protocol;

namespace ViceSharp.Host.Services;

public interface ILocalVideoFrameSource
{
    ValueTask<GetVideoFrameResponse> GetFrameAsync(string sessionId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Pull-only video frame source: returns the latest committed framebuffer for a
/// session WITHOUT advancing the machine. Per the decoupling design
/// (docs/Decoupling.md), emulation is driven on the host's dedicated worker
/// thread (<see cref="EmulationPumpService"/>), so the UI render loop never
/// drives or blocks emulation. The framebuffer is copied under the session lock
/// so it is never read mid-RunFrame (no tearing).
/// </summary>
public sealed class LocalVideoFrameSource : ILocalVideoFrameSource
{
    private readonly EmulatorRuntimeRegistry _registry;

    public LocalVideoFrameSource(EmulatorRuntimeRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        _registry = registry;
    }

    public ValueTask<GetVideoFrameResponse> GetFrameAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_registry.TryGet(sessionId, out var session))
            return ValueTask.FromResult(new GetVideoFrameResponse(HostProtocolMapper.MissingSessionStatus(sessionId), null));

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
