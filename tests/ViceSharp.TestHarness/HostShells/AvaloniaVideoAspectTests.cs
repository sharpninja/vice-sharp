namespace ViceSharp.TestHarness.HostShells;

using System;
using System.IO;
using ViceSharp.Avalonia;
using Xunit;

/// <summary>
/// FIX-XASPECT-002 (desktop head twin of FIX-XASPECT-001) and FIX-XNTSCFILL-001 (desktop twin
/// of Xbox content-row crop): the Avalonia video surface must honor the display aspect mode,
/// the ACTIVE machine's composite pixel aspect, and crop NTSC to written content rows so it
/// fills vertical space instead of letterboxing an in-frame black band.
///
/// FR: FR-XVIDEO-002 / TR-MVVM-001. Use case: operator report 2026-07-14 (PAR ignored) and
/// 2026-08-05 (NTSC does not fill vertically). VICE models PAR in vicii.c
/// vicii_get_pixel_aspect() (PAL 0.93650794, NTSC 0.75); content rows are VisibleLines minus
/// first displayed raster 16 (NTSC 246, PAL 272).
/// Acceptance:
///   TEST-AVASPECT-001a: ComputeDisplayAspect applies PAR, modes, and content height.
///   TEST-AVASPECT-001b: Render/measure wiring uses content height + MeasureOverride fill.
///   TEST-AVASPECT-001c: NTSC content height is shorter than PAL so aspect is taller-filling.
/// </summary>
public sealed class AvaloniaVideoAspectTests
{
    [Fact]
    [Trait("Category", "Xbox")]
    public void ComputeDisplayAspect_AppliesModeAndPixelAspect()
    {
        // TEST-AVASPECT-001a. VICE mode: width scales by the PAR, so PAL and NTSC differ.
        var pal = VideoSurface.ComputeDisplayAspect("VICE pixel aspect", 0.93650794);
        var ntsc = VideoSurface.ComputeDisplayAspect(
            "VICE pixel aspect", 0.75, VideoSurface.NtscContentHeight);

        Assert.Equal(384.0 * 0.93650794 / 272.0, pal, 6);
        Assert.Equal(384.0 * 0.75 / VideoSurface.NtscContentHeight, ntsc, 6);
        // Cropped NTSC is shorter, so W/H is larger than uncropped full-buffer NTSC.
        Assert.True(ntsc > 384.0 * 0.75 / 272.0);
        // Still narrower than PAL VICE aspect on full height.
        Assert.True(pal > ntsc);

        // Square pixels: raw frame proportions regardless of the PAR.
        Assert.Equal(384.0 / 272.0, VideoSurface.ComputeDisplayAspect("Square pixels", 0.75), 6);
        Assert.Equal(
            384.0 / VideoSurface.NtscContentHeight,
            VideoSurface.ComputeDisplayAspect("Square pixels", 0.75, VideoSurface.NtscContentHeight),
            6);

        // Force 4:3: the classic CRT frame regardless of the PAR / content height.
        Assert.Equal(4.0 / 3.0, VideoSurface.ComputeDisplayAspect("Force 4:3", 0.75), 6);
        Assert.Equal(
            4.0 / 3.0,
            VideoSurface.ComputeDisplayAspect("Force 4:3", 0.75, VideoSurface.NtscContentHeight),
            6);

        // Unknown mode defaults to the VICE pixel aspect; bogus PAR degrades to square.
        Assert.Equal(384.0 * 0.75 / 272.0, VideoSurface.ComputeDisplayAspect(null, 0.75), 6);
        Assert.Equal(384.0 / 272.0, VideoSurface.ComputeDisplayAspect("VICE pixel aspect", 0), 6);
    }

    [Fact]
    [Trait("Category", "Xbox")]
    public void ContentHeight_NtscIsShorterThanFullFrame()
    {
        // TEST-AVASPECT-001c: XNTSCFILL crop constant matches VideoRenderer.GetContentLines(262).
        Assert.Equal(246, VideoSurface.NtscContentHeight);
        Assert.True(VideoSurface.NtscContentHeight < VideoSurface.SourceHeight);
    }

    [Fact]
    [Trait("Category", "Xbox")]
    public void Head_WiresAspect_SurfaceAndMainWindow()
    {
        // TEST-AVASPECT-001b: structural wiring the headless run cannot execute visually.
        var surface = ReadLower("src", "ViceSharp.Avalonia", "VideoSurface.cs");
        Assert.Contains("computedisplayaspect", surface);
        Assert.Contains("pixelaspect", surface);
        Assert.Contains("contentheight", surface);
        Assert.Contains("measureoverride", surface);
        Assert.Contains("ntsccontentheight", surface);

        var mainWindow = ReadLower("src", "ViceSharp.Avalonia", "MainWindow.axaml.cs");
        Assert.Contains("updatevideoaspect", mainWindow);
        Assert.Contains("ntsccontentheight", mainWindow);
        Assert.Contains("contentheight", mainWindow);
        // No fixed-height Viewbox path that locked NTSC under black chrome.
        Assert.DoesNotContain("new viewbox", mainWindow);
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
