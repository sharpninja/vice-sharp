namespace ViceSharp.Core.Media;

using System;

/// <summary>
/// FR-MED-002: a continuous video-capture sink the emulation worker tees each
/// committed BGRA frame into. Both the numbered-BMP sequence
/// (<see cref="FrameSequenceCapture"/>) and the muxed ffmpeg recorder
/// (<see cref="FfmpegVideoRecorder"/>) implement this so the runtime session can
/// drive either through one uniform surface.
/// </summary>
public interface IVideoCaptureSink : IDisposable
{
    /// <summary>Number of frames accepted so far.</summary>
    int FrameCount { get; }

    /// <summary>
    /// Frames dropped under back-pressure (0 for sinks that never drop). A non-zero count means
    /// the recording omits frames, which - since each survivor is tagged at the nominal frame
    /// rate - compresses the clip's timeline and makes it play faster than real time.
    /// </summary>
    long DroppedFrameCount => 0;

    /// <summary>
    /// Persist one BGRA8888 frame (row-major, top-down, length = width*height*4).
    /// Implementations silently ignore frames once disposed so late frames from
    /// the emulation pipeline cannot corrupt a closed capture.
    /// </summary>
    void CaptureFrame(ReadOnlySpan<byte> bgra, int width, int height);
}
