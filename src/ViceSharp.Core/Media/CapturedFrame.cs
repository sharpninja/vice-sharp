namespace ViceSharp.Core.Media;

/// <summary>
/// FR-CFG-003: metadata for one captured video frame. The palette id is the
/// color resource that was active when the frame was accepted, so a recording
/// can reproduce the displayed colors.
/// </summary>
public sealed class CapturedFrame
{
    /// <summary>Frame width in pixels.</summary>
    public int Width { get; set; }

    /// <summary>Frame height in pixels.</summary>
    public int Height { get; set; }

    /// <summary>
    /// Palette id used for this frame, for example <c>pepto</c> or the file
    /// name of a discovered <c>.vpl</c> palette.
    /// </summary>
    public string PaletteId { get; set; } = "vice";
}
