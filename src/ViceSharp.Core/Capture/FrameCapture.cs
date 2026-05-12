using ViceSharp.Abstractions;

namespace ViceSharp.Core.Capture;

public static class FrameCapture
{
    public static Task CaptureAsync(
        IVideoChip videoChip,
        string filePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(videoChip);

        return BmpFrameArtifactWriter.WriteBgraAsync(
            videoChip.FrameBuffer,
            videoChip.FrameWidth,
            videoChip.FrameHeight,
            filePath,
            cancellationToken);
    }
}
