using ViceSharp.Abstractions;
using ViceSharp.Host.Runtime;
using ViceSharp.Protocol;

namespace ViceSharp.Host.Services;

public interface ILocalVideoFrameSource
{
    ValueTask<GetVideoFrameResponse> GetFrameAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Zero-allocation, lock-free frame read for in-process UI rendering: copies the
    /// emulation thread's latest published frame directly into <paramref name="destination"/>
    /// (e.g. a WriteableBitmap's locked buffer). Returns false when the session is
    /// unknown, has no video chip, has not yet published a frame, or the destination
    /// is too small. The UI never touches the emulation lock via this path, so the
    /// render pull cannot stall the emulation thread (FR-1132, BUG-THROTTLE-001).
    /// </summary>
    bool TryCopyFrameInto(string sessionId, Span<byte> destination, out int width, out int height, out long cycle);
}

/// <summary>
/// Pull-only video frame source: returns the latest committed framebuffer for a
/// session WITHOUT advancing the machine. Per the decoupling design
/// (docs/Decoupling.md), emulation is driven on the host's dedicated worker
/// thread (<see cref="EmulationPumpService"/>), so the UI render loop never
/// drives or blocks emulation.
///
/// The worker advances the CPU clock in sub-frame cycle slices, so to avoid
/// tearing the pull returns the last COMPLETE frame the worker committed at the
/// video chip's FrameCompleted boundary (<see cref="EmulatorRuntimeSession.CommitFrame"/>).
/// Until the first frame completes (a freshly created or stopped session) it falls
/// back to a live snapshot of the chip framebuffer. All copies happen under the
/// session lock.
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

        var videoChip = session.Machine.Devices.GetByRole(DeviceRole.VideoChip) as IVideoChip;
        if (videoChip is null)
            return ValueTask.FromResult(new GetVideoFrameResponse(RpcStatus.Unavailable("The session has no video chip."), null));

        // Allocate the DTO buffer (the gRPC/remote contract returns a byte[]).
        var frame = new byte[videoChip.FrameBuffer.Length];

        // Lock-free: copy the emulation thread's last published frame. Never touches
        // the emulation lock, so this cannot stall the worker.
        if (session.TryCopyLatestFrameInto(frame, out var width, out var height, out var cycle))
        {
            return ValueTask.FromResult(new GetVideoFrameResponse(
                RpcStatus.Ok(),
                new VideoFrameDto(width, height, cycle, frame)));
        }

        // No frame published yet (freshly created / stopped session): snapshot the
        // current live buffer so the caller still gets a valid frame.
        lock (session.SyncRoot)
        {
            videoChip.FrameBuffer.CopyTo(frame, 0);
            return ValueTask.FromResult(new GetVideoFrameResponse(
                RpcStatus.Ok(),
                new VideoFrameDto(videoChip.FrameWidth, videoChip.FrameHeight, session.Machine.GetState().Cycle, frame)));
        }
    }

    public bool TryCopyFrameInto(string sessionId, Span<byte> destination, out int width, out int height, out long cycle)
    {
        width = 0;
        height = 0;
        cycle = 0;

        if (!_registry.TryGet(sessionId, out var session))
            return false;

        // Lock-free read of the emulation thread's published frame straight into the
        // caller's buffer (e.g. the WriteableBitmap). No allocation, no lock.
        return session.TryCopyLatestFrameInto(destination, out width, out height, out cycle);
    }
}
