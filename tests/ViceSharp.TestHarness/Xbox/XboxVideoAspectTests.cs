namespace ViceSharp.TestHarness.Xbox;

using System;
using System.IO;
using ViceSharp.Chips.VicIi;
using ViceSharp.Xbox.ViewModels;
using Xunit;

/// <summary>
/// FIX-XASPECT-001 (PLAN-XBOXUWP, area XVIDEO): the UWP head's video surface must fill
/// the window (no 90% TV-safe inset) while preserving the TRUE composite pixel aspect
/// ratio of the active machine's video standard.
///
/// FR: FR-XVIDEO-002. Use case: C64 composite pixels are not square; VICE models the
/// display aspect per standard in vicii.c vicii_get_pixel_aspect() (PAL 0.93650794,
/// PAL-N 0.90769231, NTSC 0.75), mirrored by
/// ViceSharp.Chips.VicIi.VideoRenderer.GetPixelAspectRatio. The head letterboxes the
/// frame into the whole panel using that pixel aspect: effective display width =
/// sourceWidth x pixelAspect, uniform scale = min(fit), centered, black bars on the
/// short axis only.
/// Acceptance:
///   TEST-XVIDEO-ASPECT-001a: the Chips pixel-aspect table matches VICE exactly.
///   TEST-XVIDEO-ASPECT-001b: ComputeDrawRect fills the limiting window axis fully
///     (no TV-safe shrink), centers the frame, and scales width by the pixel aspect
///     for PAL and NTSC.
///   TEST-XVIDEO-ASPECT-001c: degenerate inputs (zero sizes, non-positive aspect)
///     never produce a bogus rect.
///   TEST-XVIDEO-ASPECT-001d: the UWP surface consumes the shared geometry helper and
///     a settable pixel aspect, and the 90% TV-safe factor is gone; the head wires the
///     aspect from the live session's video standard at boot and on model change.
/// </summary>
public sealed class XboxVideoAspectTests
{
    [Fact]
    [Trait("Category", "Xbox")]
    public void PixelAspectTable_MatchesVice()
    {
        // TEST-XVIDEO-ASPECT-001a: VICE vicii.c vicii_get_pixel_aspect().
        Assert.Equal(0.93650794f, VideoRenderer.GetPixelAspectRatio(Mos6569.TvSystem.PAL));
        Assert.Equal(0.90769231f, VideoRenderer.GetPixelAspectRatio(Mos6569.TvSystem.PALN));
        Assert.Equal(0.75000000f, VideoRenderer.GetPixelAspectRatio(Mos6569.TvSystem.NTSC));
    }

    [Fact]
    [Trait("Category", "Xbox")]
    public void ComputeDrawRect_Pal_FillsHeight_CentersHorizontally()
    {
        // TEST-XVIDEO-ASPECT-001b: 1999x1032 window, 384x272 PAL frame. Height-limited:
        // the frame fills the FULL window height (1032, not 90% of it), width scales by
        // the PAL pixel aspect (384 x 0.93650794 = 359.62 display units).
        var (x, y, width, height) = VideoDisplayGeometry.ComputeDrawRect(
            1999, 1032, 384, 272, 0.93650794f);

        Assert.Equal(1032, height);
        Assert.Equal(1364, width);
        Assert.Equal(317, x);
        Assert.Equal(0, y);
    }

    [Fact]
    [Trait("Category", "Xbox")]
    public void ComputeDrawRect_Ntsc_IsNarrower_SameHeight()
    {
        // TEST-XVIDEO-ASPECT-001b: NTSC pixels are much taller (0.75): same window, the
        // frame is markedly narrower than PAL at the same full height.
        var (x, y, width, height) = VideoDisplayGeometry.ComputeDrawRect(
            1999, 1032, 384, 272, 0.75f);

        Assert.Equal(1032, height);
        Assert.Equal(1093, width);
        Assert.Equal(453, x);
        Assert.Equal(0, y);
    }

    [Fact]
    [Trait("Category", "Xbox")]
    public void ComputeDrawRect_WidthLimited_FillsWidth_CentersVertically()
    {
        // TEST-XVIDEO-ASPECT-001b: a tall window flips the limiting axis: full width,
        // vertical letterbox bars.
        var (x, y, width, height) = VideoDisplayGeometry.ComputeDrawRect(
            800, 2000, 384, 272, 1.0f);

        Assert.Equal(800, width);
        Assert.Equal(567, height);
        Assert.Equal(0, x);
        Assert.Equal(716, y);
    }

    [Fact]
    [Trait("Category", "Xbox")]
    public void ComputeDrawRect_DegenerateInputs_AreSafe()
    {
        // TEST-XVIDEO-ASPECT-001c.
        Assert.Equal((0, 0, 0, 0), VideoDisplayGeometry.ComputeDrawRect(0, 1080, 384, 272, 1f));
        Assert.Equal((0, 0, 0, 0), VideoDisplayGeometry.ComputeDrawRect(1920, 1080, 0, 272, 1f));

        // Non-positive aspect degrades to square pixels rather than dividing by zero.
        var square = VideoDisplayGeometry.ComputeDrawRect(800, 2000, 384, 272, 0f);
        Assert.Equal((0, 716, 800, 567), square);
    }

    [Fact]
    [Trait("Category", "Xbox")]
    public void VideoSurface_UsesSharedGeometry_AndSessionStandard_NoTvSafeInset()
    {
        // TEST-XVIDEO-ASPECT-001d: structural wiring of the #if HAS_UWP head files the
        // headless fallback cannot execute.
        var surface = ReadLower("src", "ViceSharp.Xbox", "Controls", "VideoSurfaceHost.cs");
        Assert.Contains("videodisplaygeometry.computedrawrect", surface);
        Assert.Contains("setpixelaspect", surface);
        Assert.DoesNotContain("* 0.9", surface);

        var app = ReadLower("src", "ViceSharp.Xbox", "App.xaml.cs");
        Assert.Contains("applyvideoaspectforcurrentsession", app);
        Assert.Contains("getpixelaspectratio", app);

        var settingsPage = ReadLower("src", "ViceSharp.Xbox", "Views", "SettingsPage.xaml.cs");
        Assert.Contains("applyvideoaspectforcurrentsession", settingsPage);
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
