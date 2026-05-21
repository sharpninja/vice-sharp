namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Chips.VicIi;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR: FR-VIC-007, TR: TR-CYCLE-001, TEST: TEST-VIC-001, TODO: BACKFILL-VIDEO-001.
/// The VIC-II samples RSEL/CSEL at border check points instead of treating
/// the visible area as a pure function of the latest register values.
/// </summary>
public sealed class VicIIBorderFlipFlopTests
{
    private const ushort ScreenControl1 = 0xD011;
    private const ushort ScreenControl2 = 0xD016;

    private static Mos6569 BuildVic()
    {
        var irq = new InterruptLine(InterruptType.Irq);
        return new Mos6569(new BasicBus(), irq);
    }

    private static Mos6569 BuildVic(string model)
    {
        var irq = new InterruptLine(InterruptType.Irq);
        var bus = new BasicBus();
        return model switch
        {
            "6569" => new Mos6569(bus, irq),
            "8565" => new Mos8565(bus, irq),
            "6567" => new Mos6567(bus, irq),
            "8562" => new Mos8562(bus, irq),
            "6567R56A" => new Mos6567R56A(bus, irq),
            "6572" => new Mos6572(bus, irq),
            _ => throw new ArgumentOutOfRangeException(nameof(model), model, "Unknown VIC-II model."),
        };
    }

    private static void AdvanceTo(Mos6569 vic, ushort rasterLine, byte rasterCycle)
    {
        var maxCycles = vic.TotalLines * vic.CyclesPerLine * 2;
        for (var cycle = 0; cycle < maxCycles; cycle++)
        {
            if (vic.CurrentRasterLine == rasterLine && vic.RasterX == rasterCycle)
                return;

            vic.Tick();
        }

        throw new InvalidOperationException($"VIC did not reach line ${rasterLine:X3}, cycle {rasterCycle}.");
    }

    /// <summary>
    /// FR: FR-VIC-007, TR: TR-CYCLE-001, TEST: TEST-VIC-001, TODO: BACKFILL-VIDEO-001.
    /// Use case: In 25-row mode with DEN set, the upper-border comparison at
    /// raster line 51 clears the vertical border flip-flop, and the left
    /// border check clears the main border for the visible portion of the
    /// line.
    /// Acceptance: Before line 51 the vertical border remains active; after
    /// line 51 reaches the left border check both vertical and main border
    /// flip-flops are inactive.
    /// </summary>
    [Fact]
    public void BorderFlipFlop_ClearsAtUpperBorderCheck_WhenDisplayIsEnabled()
    {
        var vic = BuildVic();
        vic.Write(ScreenControl1, 0x18);

        AdvanceTo(vic, 50, 18);
        Assert.True(vic.IsVerticalBorderActive);

        AdvanceTo(vic, 51, 18);

        Assert.False(vic.IsVerticalBorderActive);
        Assert.False(vic.IsMainBorderActive);
        Assert.False(vic.IsRasterLineVerticalBorderActive(51));
    }

    /// <summary>
    /// FR: FR-VIC-007, TR: TR-CYCLE-001, TEST: TEST-VIC-001, TODO: BACKFILL-VIDEO-001.
    /// Use case: In normal 25-row mode the lower-border comparison at raster
    /// line 251 re-arms the vertical border, and the left border check copies
    /// that pending state into the active vertical border flip-flop.
    /// Acceptance: After raster line 251 reaches the left border check, both
    /// vertical and main border flip-flops are active.
    /// </summary>
    [Fact]
    public void BorderFlipFlop_SetsAtLowerBorderCheck_InNormalTwentyFiveRowMode()
    {
        var vic = BuildVic();
        vic.Write(ScreenControl1, 0x18);

        AdvanceTo(vic, 51, 18);
        Assert.False(vic.IsVerticalBorderActive);

        AdvanceTo(vic, 251, 18);

        Assert.True(vic.IsVerticalBorderActive);
        Assert.True(vic.IsMainBorderActive);
        Assert.True(vic.IsRasterLineVerticalBorderActive(251));
    }

    /// <summary>
    /// FR: FR-VIC-007, TR: TR-CYCLE-001, TEST: TEST-VIC-001, TODO: BACKFILL-VIDEO-001.
    /// Use case: Clearing RSEL before the 25-row lower-border comparison and
    /// restoring it after the comparison opens the lower border because the
    /// vertical border flip-flop is not set on line 251.
    /// Acceptance: After switching RSEL off before line 251 and back on after
    /// the line has passed the left border check, the vertical border remains
    /// inactive and the line snapshot is still visible.
    /// </summary>
    [Fact]
    public void BorderFlipFlop_CanOpenLowerBorder_BySkippingRselComparison()
    {
        var vic = BuildVic();
        vic.Write(ScreenControl1, 0x18);

        AdvanceTo(vic, 51, 18);
        Assert.False(vic.IsVerticalBorderActive);

        AdvanceTo(vic, 250, 0);
        vic.Write(ScreenControl1, 0x10);

        AdvanceTo(vic, 251, 18);
        vic.Write(ScreenControl1, 0x18);

        Assert.False(vic.IsVerticalBorderActive);
        Assert.False(vic.IsRasterLineVerticalBorderActive(251));
    }

    /// <summary>
    /// FR: FR-VIC-007, TR: TR-VIC-EDGE-002, TR: TR-CYCLE-001,
    /// TEST: TEST-VIC-001, TODO: BACKFILL-VIDEO-001.
    /// Use case: VICE x64sc opens the right side border when software
    /// switches CSEL from 40-column to 38-column mode at PAL cycle 56,
    /// after the 38-column right-border set check and before the
    /// 40-column right-border set check.
    /// Acceptance: The line remains horizontally open past the normal
    /// right border, so sprite-visible pixels are not masked at x=340.
    /// </summary>
    [Fact]
    public void BorderFlipFlop_CanOpenRightSideBorder_BySwitchingCselAtCycle56()
    {
        var vic = BuildVic();
        vic.Write(ScreenControl1, 0x18);
        vic.Write(ScreenControl2, 0x08);

        AdvanceTo(vic, 51, 18);
        Assert.False(vic.IsVerticalBorderActive);

        AdvanceTo(vic, 100, 56);
        vic.Write(ScreenControl2, 0x00);

        AdvanceTo(vic, 101, 0);

        Assert.False(vic.IsMainBorderActive);
        Assert.True(vic.IsRasterLineRightBorderOpen(100));
        Assert.True(vic.CanRenderSpritePixelAt(340, 100));
    }

    /// <summary>
    /// FR: FR-VIC-007, TR: TR-VIC-EDGE-002, TR: TR-CYCLE-001,
    /// TEST: TEST-VIC-001, TODO: BACKFILL-VIDEO-001.
    /// Use case: VICE carries an opened right side border into the next
    /// line's left border. This is how continuous side-border effects keep
    /// the side border open across raster lines.
    /// Acceptance: After opening the right side border on line 100, line 101
    /// reports an opened left side border and permits sprite pixels at x=0.
    /// </summary>
    [Fact]
    public void BorderFlipFlop_CarriesOpenedRightBorderIntoNextLeftSideBorder()
    {
        var vic = BuildVic();
        vic.Write(ScreenControl1, 0x18);
        vic.Write(ScreenControl2, 0x08);

        AdvanceTo(vic, 51, 18);
        Assert.False(vic.IsVerticalBorderActive);

        AdvanceTo(vic, 100, 56);
        vic.Write(ScreenControl2, 0x00);

        AdvanceTo(vic, 102, 0);

        Assert.True(vic.IsRasterLineRightBorderOpen(100));
        Assert.True(vic.IsRasterLineLeftBorderOpen(101));
        Assert.True(vic.CanRenderSpritePixelAt(0, 101));
    }

    /// <summary>
    /// FR: FR-VIC-007, TR: TR-VIC-EDGE-002, TR: TR-CYCLE-001,
    /// TEST: TEST-VIC-001, TODO: BACKFILL-VIDEO-001.
    /// Use case: Continuous side-border effects repeat the CSEL timing on
    /// consecutive lines so the right-open state keeps carrying into the
    /// next line's left border.
    /// Acceptance: Opening the right side border on both lines 100 and 101
    /// keeps the left side border open on line 102.
    /// </summary>
    [Fact]
    public void BorderFlipFlop_CarriesContinuousOpenSideBorderAcrossMultipleLines()
    {
        var vic = BuildVic();
        vic.Write(ScreenControl1, 0x18);
        vic.Write(ScreenControl2, 0x08);

        AdvanceTo(vic, 51, 18);
        Assert.False(vic.IsVerticalBorderActive);

        AdvanceTo(vic, 100, 56);
        vic.Write(ScreenControl2, 0x00);

        AdvanceTo(vic, 101, 55);
        vic.Write(ScreenControl2, 0x08);
        AdvanceTo(vic, 101, 56);
        vic.Write(ScreenControl2, 0x00);

        AdvanceTo(vic, 103, 0);

        Assert.True(vic.IsRasterLineRightBorderOpen(100));
        Assert.True(vic.IsRasterLineLeftBorderOpen(101));
        Assert.True(vic.IsRasterLineRightBorderOpen(101));
        Assert.True(vic.IsRasterLineLeftBorderOpen(102));
        Assert.True(vic.CanRenderSpritePixelAt(0, 102));
    }

    /// <summary>
    /// FR: FR-VIC-007, TR: TR-VIC-EDGE-002, TR: TR-CYCLE-001,
    /// TEST: TEST-VIC-001, TODO: BACKFILL-VIDEO-001.
    /// Use case: VICE x64sc blanks a line when CSEL changes from 38-column
    /// to 40-column mode at PAL cycle 17: the 40-column left-border clear
    /// check has already passed, and the 38-column check is skipped by the
    /// new CSEL value.
    /// Acceptance: The line never opens horizontal display, so sprite pixels
    /// remain masked even inside the normal graphics window.
    /// </summary>
    [Fact]
    public void BorderFlipFlop_BlanksLine_WhenCselSwitchesFromThirtyEightToFortyAtCycle17()
    {
        var vic = BuildVic();
        vic.Write(ScreenControl1, 0x18);
        vic.Write(ScreenControl2, 0x00);

        AdvanceTo(vic, 51, 18);
        Assert.False(vic.IsVerticalBorderActive);

        AdvanceTo(vic, 100, 17);
        vic.Write(ScreenControl2, 0x08);

        AdvanceTo(vic, 101, 0);

        Assert.False(vic.IsRasterLineHorizontalDisplayOpen(100));
        Assert.False(vic.CanRenderSpritePixelAt(96, 100));
    }

    /// <summary>
    /// FR: FR-VIC-007, TR: TR-VIC-EDGE-002, TR: TR-CYCLE-001,
    /// TEST: TEST-VIC-001, TODO: BACKFILL-VIDEO-001.
    /// Use case: VICE uses the same CSEL-selective left/right border check
    /// cycles for PAL, NTSC, old NTSC, PAL-N, and HMOS variants even though
    /// the total cycles per line differ.
    /// Acceptance: Each x64sc VIC-II model opens the right side border with
    /// the cycle-56 CSEL switch and carries that opening into the next line.
    /// </summary>
    [Theory]
    [InlineData("6569")]
    [InlineData("8565")]
    [InlineData("6567")]
    [InlineData("8562")]
    [InlineData("6567R56A")]
    [InlineData("6572")]
    public void BorderFlipFlop_RightOpenAndLeftCarry_AreModelInvariant(string model)
    {
        var vic = BuildVic(model);
        vic.Write(ScreenControl1, 0x18);
        vic.Write(ScreenControl2, 0x08);

        AdvanceTo(vic, 51, 18);
        Assert.False(vic.IsVerticalBorderActive);

        AdvanceTo(vic, 100, 56);
        vic.Write(ScreenControl2, 0x00);
        AdvanceTo(vic, 102, 0);

        Assert.True(vic.IsRasterLineRightBorderOpen(100));
        Assert.True(vic.IsRasterLineLeftBorderOpen(101));
        Assert.True(vic.CanRenderSpritePixelAt(0, 101));
    }

    /// <summary>
    /// FR: FR-VIC-007, TR: TR-VIC-EDGE-002, TR: TR-CYCLE-001,
    /// TEST: TEST-VIC-001, TODO: BACKFILL-VIDEO-001.
    /// Use case: The cycle-17 CSEL 0-to-1 blanking edge is not PAL-only; it
    /// comes from the shared CSEL-selective border checks in the VICE x64sc
    /// cycle tables.
    /// Acceptance: Every required x64sc VIC-II model leaves the line's
    /// horizontal display closed after the cycle-17 switch.
    /// </summary>
    [Theory]
    [InlineData("6569")]
    [InlineData("8565")]
    [InlineData("6567")]
    [InlineData("8562")]
    [InlineData("6567R56A")]
    [InlineData("6572")]
    public void BorderFlipFlop_Cycle17BlankLine_IsModelInvariant(string model)
    {
        var vic = BuildVic(model);
        vic.Write(ScreenControl1, 0x18);
        vic.Write(ScreenControl2, 0x00);

        AdvanceTo(vic, 51, 18);
        Assert.False(vic.IsVerticalBorderActive);

        AdvanceTo(vic, 100, 17);
        vic.Write(ScreenControl2, 0x08);
        AdvanceTo(vic, 101, 0);

        Assert.False(vic.IsRasterLineHorizontalDisplayOpen(100));
        Assert.False(vic.CanRenderSpritePixelAt(96, 100));
    }
}
