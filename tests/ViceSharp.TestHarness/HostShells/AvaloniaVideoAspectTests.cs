namespace ViceSharp.TestHarness.HostShells;

using System;
using System.IO;
using ViceSharp.Avalonia;
using Xunit;

/// <summary>
/// FIX-XASPECT-002 (desktop head twin of FIX-XASPECT-001): the Avalonia video surface must
/// honor the display aspect mode and the ACTIVE machine's composite pixel aspect ratio.
///
/// FR: FR-XVIDEO-002 / TR-MVVM-001. Use case: operator report 2026-07-14: "After switching
/// from PAL to NTSC, it does not appear that the NTSC pixel size is being used. Avalonia has
/// same problem." Root cause: VideoSurface.Render computed displayAspect = SourceWidth /
/// SourceHeight (square pixels, PAR never applied), so the "VICE pixel aspect" setting was a
/// no-op and PAL/NTSC rendered identically. VICE models the per-standard pixel aspect in
/// vicii.c vicii_get_pixel_aspect() (PAL 0.93650794, NTSC 0.75), mirrored by the Chips
/// VideoRenderer table.
/// Acceptance:
///   TEST-AVASPECT-001a: ComputeDisplayAspect applies the pixel aspect for the VICE mode
///     (PAL and NTSC differ), returns square pixels for "Square pixels", forces 4:3 for
///     "Force 4:3", and degrades non-positive aspect inputs to square pixels.
///   TEST-AVASPECT-001b: Render consumes the helper (no raw SourceWidth/SourceHeight
///     aspect), and MainWindow re-feeds the surface's mode + pixel aspect from the selected
///     machine profile's standard on settings changes.
/// </summary>
public sealed class AvaloniaVideoAspectTests
{
    [Fact]
    [Trait("Category", "Xbox")]
    public void ComputeDisplayAspect_AppliesModeAndPixelAspect()
    {
        // TEST-AVASPECT-001a. VICE mode: width scales by the PAR, so PAL and NTSC differ.
        var pal = VideoSurface.ComputeDisplayAspect("VICE pixel aspect", 0.93650794);
        var ntsc = VideoSurface.ComputeDisplayAspect("VICE pixel aspect", 0.75);

        Assert.Equal(384.0 * 0.93650794 / 272.0, pal, 6);
        Assert.Equal(384.0 * 0.75 / 272.0, ntsc, 6);
        Assert.True(pal > ntsc);

        // Square pixels: raw frame proportions regardless of the PAR.
        Assert.Equal(384.0 / 272.0, VideoSurface.ComputeDisplayAspect("Square pixels", 0.75), 6);

        // Force 4:3: the classic CRT frame regardless of the PAR.
        Assert.Equal(4.0 / 3.0, VideoSurface.ComputeDisplayAspect("Force 4:3", 0.75), 6);

        // Unknown mode defaults to the VICE pixel aspect; bogus PAR degrades to square.
        Assert.Equal(384.0 * 0.75 / 272.0, VideoSurface.ComputeDisplayAspect(null, 0.75), 6);
        Assert.Equal(384.0 / 272.0, VideoSurface.ComputeDisplayAspect("VICE pixel aspect", 0), 6);
    }

    [Fact]
    [Trait("Category", "Xbox")]
    public void Head_WiresAspect_SurfaceAndMainWindow()
    {
        // TEST-AVASPECT-001b: structural wiring the headless run cannot execute visually.
        var surface = ReadLower("src", "ViceSharp.Avalonia", "VideoSurface.cs");
        Assert.Contains("computedisplayaspect", surface);
        Assert.Contains("pixelaspect", surface);

        var mainWindow = ReadLower("src", "ViceSharp.Avalonia", "MainWindow.axaml.cs");
        Assert.Contains("updatevideoaspect", mainWindow);
        Assert.Contains("getpixelaspectratio", mainWindow);
    }

    private static string ReadLower(params string[] parts)
    {
        var path = Path.Combine(RepoRoot, Path.Combine(parts));
        Assert.True(File.Exists(path), $"Expected source file at '{path}'.");
        return File.ReadAllText(path).ToLowerInvariant();
    }

    private static string RepoRoot
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "ViceSharp.slnx")))
                directory = directory.Parent;

            if (directory is null)
                throw new InvalidOperationException("Could not locate repository root.");

            return directory.FullName;
        }
    }
}
