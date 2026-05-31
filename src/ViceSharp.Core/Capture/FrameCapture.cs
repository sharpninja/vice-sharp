using ViceSharp.Abstractions;

namespace ViceSharp.Core.Capture;

public static class FrameCapture
{
    /// <summary>
    /// Capture the current video chip frame buffer to a file in BMP format.
    /// </summary>
    public static Task CaptureAsync(
        IVideoChip videoChip,
        string filePath,
        CancellationToken cancellationToken = default)
        => CaptureAsync(videoChip, filePath, "BMP", cancellationToken);

    /// <summary>
    /// Capture the current video chip frame buffer to a file in the specified format.
    /// Supported formats: "BMP" (default), "PNG", "JPEG".
    /// FR-MED-001, RUNTIME-CAPTURE-002, TR-MEDIA-001.
    /// </summary>
    public static Task CaptureAsync(
        IVideoChip videoChip,
        string filePath,
        string format,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(videoChip);
        ArgumentException.ThrowIfNullOrWhiteSpace(format);

        return format.ToUpperInvariant() switch
        {
            "BMP" => BmpFrameArtifactWriter.WriteBgraAsync(
                videoChip.FrameBuffer,
                videoChip.FrameWidth,
                videoChip.FrameHeight,
                filePath,
                cancellationToken),
            "PNG" => PngFrameArtifactWriter.WriteBgraAsync(
                videoChip.FrameBuffer,
                videoChip.FrameWidth,
                videoChip.FrameHeight,
                filePath,
                cancellationToken),
            "JPEG" => JpegFrameArtifactWriter.WriteBgraAsync(
                videoChip.FrameBuffer,
                videoChip.FrameWidth,
                videoChip.FrameHeight,
                filePath,
                cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported image format. Use BMP, PNG, or JPEG.")
        };
    }
}
