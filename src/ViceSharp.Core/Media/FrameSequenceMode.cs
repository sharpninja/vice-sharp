namespace ViceSharp.Core.Media;

/// <summary>
/// FR-MED-002: how a <see cref="FrameSequenceCapture"/> decides which submitted
/// frames to write as numbered BMP files.
/// </summary>
public enum FrameSequenceMode
{
    /// <summary>Write every submitted frame (one BMP per emulated frame).</summary>
    AllFrames = 0,

    /// <summary>
    /// Write a frame only when it differs from the previously written one,
    /// skipping consecutive duplicates. Useful for static/largely-still screens
    /// where most emulated frames are identical.
    /// </summary>
    UniqueFrames = 1,
}
