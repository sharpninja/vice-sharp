namespace ViceSharp.Abstractions;

/// <summary>
/// Common interface for all video display chips.
/// </summary>
public interface IVideoChip : IClockedDevice
{
    /// <summary>Current raster line being rendered</summary>
    ushort CurrentRasterLine { get; }

    /// <summary>Number of cycles per raster line</summary>
    int CyclesPerLine { get; }

    /// <summary>Total visible lines per frame</summary>
    int VisibleLines { get; }

    /// <summary>Total lines per frame including vertical blank</summary>
    int TotalLines { get; }

    /// <summary>True when currently in vertical blanking interval</summary>
    bool IsVBlank { get; }

    /// <summary>
    /// RGBA framebuffer from last completed frame.
    /// Format: 32-bit BGRA (same as Windows/Avalonia PixelFormat.Bgra8888).
    /// </summary>
    byte[] FrameBuffer { get; }

    /// <summary>Framebuffer width in pixels.</summary>
    int FrameWidth { get; }

    /// <summary>Framebuffer height in pixels.</summary>
    int FrameHeight { get; }

    /// <summary>
    /// Raised when a complete frame is ready in FrameBuffer.
    /// </summary>
    event EventHandler? FrameCompleted;
}
