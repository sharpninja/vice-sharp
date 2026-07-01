using ViceSharp.Abstractions;
using ViceSharp.Chips.VicIi;
using Xunit;

namespace ViceSharp.TestHarness;

/// <summary>
/// FR/TR/TEST: FR-VIC-004 / TR-VIC-EDGE-003 / TEST-VICRENDER-001 (PLAN-VICRENDER-001).
///
/// Cycle-stable raster bars: the demo rewrites $D020 mid-scanline so a single line shows
/// more than one border colour. The renderer used to sample $D020 once per line (at
/// line-wrap) and fill the whole scanline with that single colour, which collapsed the
/// bars and shifted them by ~1 raster line. This test drives the VIC directly, writes two
/// border colours at two in-line cycles of the same scanline, and asserts the rendered
/// line shows BOTH colours at the expected horizontal spans.
/// </summary>
public sealed class RasterBarRendererTests
{
    private static uint ExpectedBgra(byte colorIndex)
    {
        var c = VicPalette.Colors[colorIndex & 0x0F];
        return 0xFF000000u | (uint)c.B | ((uint)c.G << 8) | ((uint)c.R << 16);
    }

    [Fact]
    public void MidLineBorderColorChange_RendersTwoColourBandsOnOneScanline()
    {
        var machine = MachineTestFactory.CreateC64Machine("c64");
        machine.Reset();
        var vic = (Mos6569)machine.Devices.GetByRole(DeviceRole.VideoChip)!;

        // Line 30 is in the upper border (< UPPER border start 51), so the whole scanline is
        // border - ideal for observing border-colour bands. It is visible (frame y = 15).
        const int targetLine = 30;
        const byte colourA = 2; // red
        const byte colourB = 6; // blue

        // Advance the VIC to the target scanline.
        int guard = 0;
        while (vic.CurrentRasterLine != targetLine && guard++ < 200_000)
            vic.Tick();
        Assert.Equal(targetLine, vic.CurrentRasterLine);

        // Two mid-line $D020 writes at two distinct in-line cycles.
        while (vic.RasterX < 5)
            vic.Tick();
        vic.Write(0xD020, colourA);

        while (vic.RasterX < 40)
            vic.Tick();
        vic.Write(0xD020, colourB);

        // Finish the scanline so it renders (render fires at line-wrap).
        guard = 0;
        while (vic.CurrentRasterLine == targetLine && guard++ < 200_000)
            vic.Tick();

        var fb = vic.FrameBuffer;
        int y = targetLine - VideoRenderer.PalFirstVisibleRasterLine;
        uint PixelAt(int x) => System.BitConverter.ToUInt32(fb, (y * VideoRenderer.ScreenWidth + x) * 4);

        // colourA write at RasterX 5 maps to the left edge (frame pixel 0); colourB write at
        // RasterX 40 maps to ~frame pixel 224. So the left span is colourA and the right span
        // is colourB - two bands on ONE scanline, which the old single-fill renderer could not
        // produce (it would paint the whole line colourB, the last write).
        Assert.Equal(ExpectedBgra(colourA), PixelAt(20));
        Assert.Equal(ExpectedBgra(colourB), PixelAt(360));
        Assert.NotEqual(PixelAt(20), PixelAt(360));
    }

    [Fact]
    public void NoMidLineChange_RendersSolidBorder_FastPathUnaffected()
    {
        var machine = MachineTestFactory.CreateC64Machine("c64");
        machine.Reset();
        var vic = (Mos6569)machine.Devices.GetByRole(DeviceRole.VideoChip)!;

        const int targetLine = 28;
        const byte colour = 5; // green

        int guard = 0;
        while (vic.CurrentRasterLine != targetLine && guard++ < 200_000)
            vic.Tick();

        // One write near the start of the line, then no further change -> whole line one colour.
        while (vic.RasterX < 2)
            vic.Tick();
        vic.Write(0xD020, colour);

        guard = 0;
        while (vic.CurrentRasterLine == targetLine && guard++ < 200_000)
            vic.Tick();

        var fb = vic.FrameBuffer;
        int y = targetLine - VideoRenderer.PalFirstVisibleRasterLine;
        uint PixelAt(int x) => System.BitConverter.ToUInt32(fb, (y * VideoRenderer.ScreenWidth + x) * 4);

        var expected = ExpectedBgra(colour);
        Assert.Equal(expected, PixelAt(10));
        Assert.Equal(expected, PixelAt(200));
        Assert.Equal(expected, PixelAt(370));
    }
}
