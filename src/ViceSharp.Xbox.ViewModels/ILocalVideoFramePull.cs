namespace ViceSharp.Xbox.ViewModels;

using System;

/// <summary>
/// PLAN-XBOXUWP S21 (IMPL-XBOXUWP-021), area XVIDEO. The pure, read-only video-pull
/// seam OWNED by the 10-foot ViewModels: the head adapts it over the in-process
/// <c>LocalVideoFrameSource.TryCopyFrameInto</c> (lock-free, non-feedback,
/// FR-1132 / BUG-THROTTLE-001), and the off-console tests bind it to a fake.
/// </summary>
/// <remarks>
/// The signature mirrors the in-process <c>LocalVideoFrameSource.TryCopyFrameInto</c>
/// byte-for-byte so the head adapter is a thin pass-through. It is a PURE SINK: it
/// copies the emulation thread's latest committed framebuffer into the caller's
/// buffer without advancing the machine and without touching any core-advancing
/// member, so the render pull can never perturb determinism or stall the worker.
/// </remarks>
public interface ILocalVideoFramePull
{
    /// <summary>
    /// Copies the session's latest committed framebuffer into
    /// <paramref name="destination"/> (e.g. a pinned bitmap buffer).
    /// </summary>
    /// <param name="sessionId">The session whose latest frame is requested.</param>
    /// <param name="destination">
    /// The caller-owned buffer to copy the BGRA8888 frame into. Must be at least the
    /// frame's byte length or the copy is refused.
    /// </param>
    /// <param name="width">The frame width, in pixels, on success; otherwise 0.</param>
    /// <param name="height">The frame height, in pixels, on success; otherwise 0.</param>
    /// <param name="cycle">The emulated cycle stamp of the copied frame on success; otherwise 0.</param>
    /// <returns>
    /// <c>true</c> when a frame was copied; <c>false</c> when the session is unknown,
    /// has no video chip, has not yet published a frame, or the destination is too
    /// small (the caller sizes to the frame length and retries next tick).
    /// </returns>
    bool TryCopyFrameInto(string sessionId, Span<byte> destination, out int width, out int height, out long cycle);
}
