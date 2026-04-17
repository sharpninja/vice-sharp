namespace ViceSharp.Abstractions;

/// <summary>
/// Receives completed video frames from the emulation engine.
/// Frames are delivered as raw pixel data in a platform-neutral format.
/// The sink owns presentation timing (vsync, frame skip).
/// </summary>
public interface IFrameSink
{
    /// <summary>Called when a complete frame is ready for display.</summary>
    void PresentFrame(ReadOnlySpan<byte> pixelData);

    /// <summary>True if the sink is ready to accept a new frame.</summary>
    bool IsReady { get; }

    /// <summary>Total frames presented since last reset.</summary>
    long FrameCount { get; }
}