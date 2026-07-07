namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Chips.VicIi;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: FR-VIC-006 / TR-CYCLE-001 / TEST-VIC-001 bad-line detection + CPU cycle stealing
/// (BACKFILL-VIDEO-001 slice: VIC-II bad line + CPU cycle stealing).
///
/// A raster line is a "bad line" when:
///   (current_raster &gt;= 0x30) AND (current_raster &lt;= 0xF7)
///   AND ((current_raster &amp; 7) == YSCROLL)
///   AND DEN ($D011 bit 4) is set.
///
/// This suite covers detection, DEN gating, raster-window gating, and the
/// per-frame bad-line counter. CPU stall via BA wiring is exercised in
/// VicIiCoreTimingTests (SystemClock + IsCpuCycleStolen path); this suite
/// focuses on the detection foundation that gates the stall.
/// </summary>
public sealed class VicIIBadLineTests
{
    private const ushort ScreenControl1 = 0xD011;

    private static Mos6569 BuildVic()
    {
        var irq = new InterruptLine(InterruptType.Irq);
        return new Mos6569(new BasicBus(), irq);
    }

    private static void Advance(Mos6569 vic, int cycles)
    {
        for (var cycle = 0; cycle < cycles; cycle++)
            vic.Tick();
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
    /// FR/TR: FR-VIC-006 / TR-CYCLE-001 / TEST-VIC-001
    /// (BACKFILL-VIDEO-001 bad line cycle stealing).
    /// Use case: With $D011 = $1B (DEN=1, YSCROLL=3), bad lines occur on
    /// raster lines whose lower 3 bits equal 3, inside the visible DMA
    /// range [$30, $F7]. The first such line is $33 (0x33 &amp; 7 == 3).
    /// Acceptance: IsBadLine is true on line $33 with DEN set + YSCROLL=3,
    /// and false on a neighbouring line ($34, lower bits 4) with the same
    /// $D011 still latched.
    /// </summary>
    [Fact]
    public void BadLine_DetectsYScrollMatch_WhenDenIsSet()
    {
        var vic = BuildVic();

        vic.Write(ScreenControl1, 0x1B);
        AdvanceTo(vic, 0x33, 0);

        Assert.Equal(0x33, vic.CurrentRasterLine);
        Assert.True(vic.IsBadLine, "Line $33 with DEN=1, YSCROLL=3 should be a bad line.");

        Advance(vic, vic.CyclesPerLine);

        Assert.Equal(0x34, vic.CurrentRasterLine);
        Assert.False(vic.IsBadLine, "Line $34 (lower bits 4) with YSCROLL=3 must not be a bad line.");
    }

    /// <summary>
    /// FR/TR: FR-VIC-006 / TR-CYCLE-001 / TEST-VIC-001
    /// (BACKFILL-VIDEO-001 bad line cycle stealing).
    /// Use case: DEN ($D011 bit 4) gates the entire bad-line latch. With
    /// $D011 = $03 (DEN=0, YSCROLL=3) the YSCROLL match on line $33 must
    /// NOT raise a bad line because display is not enabled.
    /// Acceptance: AdvanceTo line $33 with DEN cleared and YSCROLL=3
    /// produces IsBadLine == false. Advancing one further line where
    /// (line &amp; 7) == 3 (line $3B) still produces IsBadLine == false.
    /// </summary>
    [Fact]
    public void BadLine_DenGatedOff_NoBadLines()
    {
        var vic = BuildVic();

        vic.Write(ScreenControl1, 0x03);
        AdvanceTo(vic, 0x33, 0);

        Assert.Equal(0x33, vic.CurrentRasterLine);
        Assert.False(vic.IsBadLine, "DEN=0 must suppress bad-line detection.");

        AdvanceTo(vic, 0x3B, 0);

        Assert.Equal(0x3B, vic.CurrentRasterLine);
        Assert.False(vic.IsBadLine, "DEN=0 must suppress bad-line detection on the next YSCROLL match too.");
    }

    /// <summary>
    /// FR/TR: FR-VIC-006 / TR-CYCLE-001 / TEST-VIC-001
    /// (BACKFILL-VIDEO-001 bad line cycle stealing).
    /// Use case: Bad lines are only valid in the raster range [$30, $F7].
    /// With YSCROLL=0 + DEN=1, every 8th line outside the visible DMA range
    /// (e.g. lines 0x00, 0x08, ..., 0x28 below the window and 0xF8, 0xFF
    /// above the window) must NOT trigger a bad line.
    /// Acceptance: At line 0x00 (matches YSCROLL=0 in lower bits but below
    /// $30) IsBadLine is false; at line 0xF8 (above $F7) IsBadLine is false;
    /// at line 0x30 inside the window IsBadLine is true.
    /// </summary>
    [Fact]
    public void BadLine_OutsideRasterWindow_NeverBadLine()
    {
        var vic = BuildVic();

        vic.Write(ScreenControl1, 0x10);

        AdvanceTo(vic, 0x08, 0);
        Assert.Equal(0x08, vic.CurrentRasterLine);
        Assert.False(vic.IsBadLine, "Line $08 is below the DMA window even with YSCROLL match.");

        AdvanceTo(vic, 0x28, 0);
        Assert.Equal(0x28, vic.CurrentRasterLine);
        Assert.False(vic.IsBadLine, "Line $28 is below the DMA window even with YSCROLL match.");

        AdvanceTo(vic, 0x30, 0);
        Assert.True(vic.IsBadLine, "Line $30 is the first bad-line in the visible window.");

        AdvanceTo(vic, 0xF8, 0);
        Assert.False(vic.IsBadLine, "Line $F8 is above the DMA window even with YSCROLL match.");
    }

    /// <summary>
    /// FR/TR: FR-VIC-006 / TR-CYCLE-001 / TEST-VIC-001
    /// (BACKFILL-VIDEO-001 bad line cycle stealing).
    /// Use case: With YSCROLL=0 + DEN=1 a PAL frame produces exactly 25
    /// bad lines: lines $30, $38, $40, ..., $F0 (every 8 lines from $30
    /// through $F0). The per-frame counter must reset at the frame
    /// boundary and tick exactly once per new bad line.
    /// Acceptance: After running through one full PAL frame
    /// (PalCyclesPerLine * PalTotalLines), BadLineCountThisFrame == 25
    /// AND, after running into the next frame for a few lines, the
    /// counter resets toward zero (reaches a smaller value, NOT
    /// monotonically increasing across frame boundaries).
    /// </summary>
    [Fact]
    public void BadLine_FrameCounter_25BadLinesInPalFrameWithYScrollZero()
    {
        var vic = BuildVic();

        vic.Write(ScreenControl1, 0x10);

        // Advance through one full PAL frame, starting from raster 0 cycle ResetRasterCycle.
        // We need to land on the last cycle of the frame so the counter reflects
        // the complete sweep including the final $F0 bad line.
        AdvanceTo(vic, 0xF7, (byte)(vic.CyclesPerLine - 1));

        Assert.Equal(25, vic.BadLineCountThisFrame);

        // Cross the frame boundary and verify the counter resets at the frame
        // start (line 0 cycle 1, where VICE applies vicii_cycle_start_of_frame
        // per viciisc/vicii-cycle.c:453-456; line 0 cycle 0 does not exist,
        // PLAN-VICEPARITY-001 slice V2 / TEST-VIC-CYCLE-12).
        AdvanceTo(vic, 0, 1);
        Assert.Equal(0, vic.BadLineCountThisFrame);
    }

    /// <summary>
    /// FR/TR: FR-VIC-006 / TR-CYCLE-001 / TEST-VIC-001
    /// (BACKFILL-VIDEO-001 bad line cycle stealing).
    /// Use case: On a bad line the VIC-II asserts the DMA cycle-steal
    /// signal during the c-access window (RasterX 12..54 inclusive of
    /// 12, exclusive of 55). The CPU is expected to honour this through
    /// ICpuCycleStealer.IsCpuCycleStolen which the SystemClock samples
    /// each tick (see VicIiCoreTimingTests for the end-to-end stall
    /// observation). This test exercises the chip-side observable.
    /// Acceptance: On line $30 with DEN=1+YSCROLL=0, at cycle 11 the
    /// IsCpuCycleStolen flag is false; at cycle 12 it becomes true and
    /// stays true through cycle 54; at cycle 55 it returns false.
    /// </summary>
    [Fact]
    public void BadLine_CycleStealFlag_ActiveDuringCharacterFetchWindow()
    {
        var vic = BuildVic();

        vic.Write(ScreenControl1, 0x10);
        AdvanceTo(vic, 0x30, 11);

        Assert.True(vic.IsBadLine);
        Assert.False(vic.IsCpuCycleStolen, "Cycle 11 of bad-line is before the DMA steal window.");

        vic.Tick();

        Assert.Equal(12, vic.RasterX);
        Assert.True(vic.IsCpuCycleStolen, "Cycle 12 enters the DMA steal window.");

        AdvanceTo(vic, 0x30, 54);
        Assert.True(vic.IsCpuCycleStolen, "Cycle 54 is the final DMA steal cycle of the c-access window.");

        vic.Tick();

        Assert.Equal(55, vic.RasterX);
        Assert.False(vic.IsCpuCycleStolen, "Cycle 55 has released the bus back to the CPU.");
    }
}
