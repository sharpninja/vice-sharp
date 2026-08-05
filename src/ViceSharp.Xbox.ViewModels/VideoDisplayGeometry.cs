namespace ViceSharp.Xbox.ViewModels;

using System;

/// <summary>
/// FIX-XASPECT-001 (PLAN-XBOXUWP, area XVIDEO). Pure display-geometry math for the video
/// surface: letterboxes the emulator frame into the FULL target panel (no TV-safe inset)
/// while preserving the TRUE composite display aspect of the active video standard.
/// </summary>
/// <remarks>
/// <para>
/// C64 composite pixels are not square. VICE models the per-standard pixel aspect ratio in
/// vicii.c vicii_get_pixel_aspect() (PAL 0.93650794, PAL-N 0.90769231, NTSC 0.75); the chip
/// layer mirrors that table (VideoRenderer.GetPixelAspectRatio) and the HEAD passes the value
/// in, keeping this project Abstractions-only. The pixel aspect is a horizontal factor:
/// display width = pixel width x aspect, so the effective source width is
/// <c>sourceWidth * pixelAspect</c> and the uniform fit scale is computed against that.
/// </para>
/// <para>
/// Kept in the portable ViewModels project (System only, TR-MVVM-001) so the math is fully
/// unit-testable headless; the #if HAS_UWP surface (VideoSurfaceHost) is a thin consumer.
/// </para>
/// </remarks>
public static class VideoDisplayGeometry
{
    /// <summary>
    /// Computes the centered, aspect-preserving draw rectangle that fills the target panel:
    /// the limiting axis spans the whole target, the other is letterboxed symmetrically.
    /// </summary>
    /// <param name="targetWidth">Target panel width in physical pixels.</param>
    /// <param name="targetHeight">Target panel height in physical pixels.</param>
    /// <param name="sourceWidth">Source frame width in emulator pixels.</param>
    /// <param name="sourceHeight">Source frame height in emulator pixels.</param>
    /// <param name="pixelAspect">
    /// The video standard's pixel aspect ratio (display width per pixel width). Values not
    /// greater than zero degrade to square pixels (1.0) rather than dividing by zero.
    /// </param>
    /// <returns>The draw rectangle, or an all-zero rect for degenerate target/source sizes.</returns>
    public static (int X, int Y, int Width, int Height) ComputeDrawRect(
        int targetWidth, int targetHeight, int sourceWidth, int sourceHeight, float pixelAspect)
    {
        if (targetWidth <= 0 || targetHeight <= 0 || sourceWidth <= 0 || sourceHeight <= 0)
            return (0, 0, 0, 0);

        var aspect = pixelAspect > 0f ? pixelAspect : 1f;

        // Display-space source width: the composite pixel aspect stretches/narrows width only.
        var effectiveWidth = sourceWidth * (double)aspect;

        var scale = Math.Min(targetWidth / effectiveWidth, targetHeight / (double)sourceHeight);
        var width = Math.Max(1, (int)Math.Round(effectiveWidth * scale));
        var height = Math.Max(1, (int)Math.Round(sourceHeight * scale));

        return ((targetWidth - width) / 2, (targetHeight - height) / 2, width, height);
    }
}
