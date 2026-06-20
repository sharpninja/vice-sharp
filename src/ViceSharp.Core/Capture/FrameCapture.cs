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
    /// Screenshot formats offered to the UI / parity with x64sc's selectable image drivers.
    /// JPEG is intentionally excluded (the in-repo JPEG writer is grayscale scaffolding and
    /// x64sc offers no JPEG).
    /// </summary>
    public static IReadOnlyList<string> ScreenshotFormats { get; } = ["png", "bmp"];

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
        CancellationToken cancellationToken = default)
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
            default:
                throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported screenshot format. Use png or bmp.");
        }
    }
}
