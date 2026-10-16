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

    /// <summary>
    /// Screenshot formats the host can write. PNG and BMP match the common
    /// x64sc drivers. JPEG is FR-MED-005. GIF matches VICE gifdrv.
    /// </summary>
    public static IReadOnlyList<string> ScreenshotFormats { get; } = ["png", "bmp", "jpeg", "gif"];

    /// <summary>
    /// Capture an already-copied BGRA frame to a file in the given format. Used by the host
    /// capture service, which snapshots the framebuffer under its session lock first. Returns
    /// the canonical lower-case format actually written.
    /// </summary>
    public static async Task<string> CaptureBgraAsync(
        byte[] frame,
        int width,
        int height,
        string filePath,
        string format,
        CancellationToken cancellationToken = default,
        int jpegQuality = 50)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ArgumentException.ThrowIfNullOrWhiteSpace(format);

        var normalized = format.Trim().ToLowerInvariant();
        switch (normalized)
        {
            case "bmp":
                await BmpFrameArtifactWriter.WriteBgraAsync(frame, width, height, filePath, cancellationToken).ConfigureAwait(false);
                return "bmp";
            case "png":
                await PngFrameArtifactWriter.WriteBgraAsync(frame, width, height, filePath, cancellationToken).ConfigureAwait(false);
                return "png";
            case "jpeg":
            case "jpg":
                await JpegFrameArtifactWriter.WriteBgraAsync(frame, width, height, filePath, cancellationToken, jpegQuality).ConfigureAwait(false);
                return "jpeg";
            case "gif":
                await GifFrameArtifactWriter.WriteBgraAsync(frame, width, height, filePath, cancellationToken).ConfigureAwait(false);
                return "gif";
            default:
                throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported screenshot format. Use png, bmp, jpeg, or gif.");
        }
    }
}
