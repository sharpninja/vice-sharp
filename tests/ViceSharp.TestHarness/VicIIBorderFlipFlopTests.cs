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
    /// FR: FR-VIC-007, TR: TR-CYCLE-001, TEST: TEST-VIC-001, TODO: BACKFILL-VIDEO-001.
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
}
