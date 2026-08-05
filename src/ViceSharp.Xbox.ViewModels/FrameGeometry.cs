namespace ViceSharp.Xbox.ViewModels;

/// <summary>
/// PLAN-XBOXUWP S23 (IMPL-XBOXUWP-023), area XVIDEO. A ViewModels-owned copy of the
/// video-frame geometry (width, height, and BGRA8888 buffer byte length). The in-process
/// head owns an identical geometry type in the host composition, which the portable
/// 10-foot ViewModels may not reference (TR-MVVM-001), so the pull seam and its adapter
/// use this local copy instead.
/// </summary>
/// <remarks>
/// The pure video-pull adapter (<see cref="VideoFramePullViewModel"/>) reads
/// <see cref="BufferLength"/> to size its reused, pinned pull buffer ONCE - before the
/// first frame is published - so the ~50 Hz render pull never reallocates per tick.
/// </remarks>
/// <param name="Width">Frame width in pixels.</param>
/// <param name="Height">Frame height in pixels.</param>
/// <param name="BufferLength">Length in bytes of the video chip's BGRA8888 frame buffer.</param>
public readonly record struct FrameGeometry(int Width, int Height, int BufferLength);
