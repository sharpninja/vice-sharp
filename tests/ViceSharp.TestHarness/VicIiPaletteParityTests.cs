namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Chips.VicIi;
using Xunit;

/// <summary>
/// PLAN-VICEPARITY-001 remediation Phase 6 (audit
/// docs/audit-vicii-vs-vice-2026-07-06.md M15/L7/L8/L9): the palette must be
/// GENERATED through VICE's color pipeline instead of hardcoded RGB. VICE
/// stores per-model YUV tables (TOBIAS_COLORS, vicii-color.c:363-585; the
/// SEPERATE_ODD_EVEN_COLORS build feeds the EVEN tables to the internal
/// non-CRT palette, video-color.c:758-765) and converts via
/// video_convert_cbm_to_ycbcr + video_calc_palette +
/// video_convert_renderer_to_rgb_gamma with all color resources at their
/// default 1000 (video-resources.c:595-608), which makes gamma/brightness/
/// contrast/saturation/tint neutral.
/// </summary>
public sealed class VicIiPaletteParityTests
{
    /// <summary>
    /// FR: FR-VIC-DRAW-COLOR, TR: TR-VIC-PALETTE-001, TEST: TEST-VIC-PALETTE-01.
    /// Use case: the 6569r5 greys are pure lumas (saturation 0) of
    /// 0.306/0.461/0.639 * 256 (vicii-color.c:452-456,472-476), so the
    /// neutral pipeline renders them at exactly 78/118/164, and Black/White
    /// at 0/255 (audit L9).
    /// Acceptance: the default PAL palette holds (0,0,0), (255,255,255),
    /// (78,78,78), (118,118,118), (164,164,164) for indices 0,1,11,12,15.
    /// </summary>
    [Fact]
    public void DefaultPalette_GreyRamp_Matches_Vice_Lumas()
    {
        Assert.Equal(new VicPalette.Color(0, 0, 0), VicPalette.Colors[0]);
        Assert.Equal(new VicPalette.Color(255, 255, 255), VicPalette.Colors[1]);
        Assert.Equal(new VicPalette.Color(78, 78, 78), VicPalette.Colors[11]);
        Assert.Equal(new VicPalette.Color(118, 118, 118), VicPalette.Colors[12]);
        Assert.Equal(new VicPalette.Color(164, 164, 164), VicPalette.Colors[15]);
    }

    /// <summary>
    /// FR: FR-VIC-DRAW-COLOR, TR: TR-VIC-PALETTE-001, TEST: TEST-VIC-PALETTE-02.
    /// Use case: VICE installs a different palette per chip model
    /// (vicii-color.c:630-648): 6569R1/6567R56A use the 5-luma 6569r1 tables
    /// whose Dark Grey luma is 0.237*256 (:378), rendering at 61 instead of
    /// the 9-luma models' 78; NTSC models convert through the YIQ (Sony)
    /// matrix instead of BT.601 (video-color.c:267-278), so the same 6569r1
    /// table yields different chroma colors on the 6567R56A.
    /// Acceptance: the 6569R1 group's PAL Dark Grey is (61,61,61); the R1
    /// PAL and R1 NTSC Red entries differ.
    /// </summary>
    [Fact]
    public void PerModel_Palettes_Differ_By_Group_And_VideoStandard()
    {
        var r1Pal = VicPalette.ForGroup(VicPalette.Group.Mos6569R1, ntsc: false);
        var r5Pal = VicPalette.ForGroup(VicPalette.Group.Mos6569R5, ntsc: false);
        var r1Ntsc = VicPalette.ForGroup(VicPalette.Group.Mos6569R1, ntsc: true);

        Assert.Equal(new VicPalette.Color(61, 61, 61), r1Pal[11]);
        Assert.Equal(new VicPalette.Color(78, 78, 78), r5Pal[11]);
        Assert.NotEqual(r1Pal[2], r1Ntsc[2]);
        Assert.NotEqual(r5Pal[2], VicPalette.ForGroup(VicPalette.Group.Mos8565R2, ntsc: false)[2]);
    }

    /// <summary>
    /// FR: FR-VIC-DRAW-COLOR, TR: TR-VIC-PALETTE-001, TEST: TEST-VIC-PALETTE-03.
    /// Use case: the boot screen's blue background (index 6) under the
    /// neutral 6569r5 PAL pipeline: even-table Blue is luma 0.237*256 with
    /// angle -12.40-360 and saturation 0.234*256 (vicii-color.c:467),
    /// converting to approximately (49,40,158); the old hardcoded palette
    /// held (27,27,142) and cannot match any VICE model output.
    /// Acceptance: index 6 blue channel exceeds 150 and red differs from
    /// green (the hardcoded 27/27 equality is gone), and index 14 (light
    /// blue) equals the same-chroma higher-luma entry: B - R equals that of
    /// index 6 within 2 (same chroma vector at both lumas).
    /// </summary>
    [Fact]
    public void DefaultPalette_BootBlues_Are_PipelineDerived()
    {
        var blue = VicPalette.Colors[6];
        var lightBlue = VicPalette.Colors[14];

        Assert.True(blue.B > 150, $"blue B={blue.B}");
        Assert.NotEqual(blue.R, blue.G);
        Assert.Equal(blue.B - blue.R, lightBlue.B - lightBlue.R, 2.0);
    }
}
