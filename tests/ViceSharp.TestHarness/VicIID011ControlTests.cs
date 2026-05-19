namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Chips.VicIi;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: FR-VIC (BACKFILL-VIDEO-001 $D011 control register).
///
/// $D011 (control register 1) packs six independent fields into one byte:
///   bits 2-0: YSCROLL (vertical fine scroll)
///   bit  3  : RSEL (0 = 24 rows / 192 visible lines, 1 = 25 rows / 200)
///   bit  4  : DEN (display enable; gates bad-line + active display)
///   bit  5  : BMM (bitmap mode select)
///   bit  6  : ECM (extended color mode)
///   bit  7  : on write: high bit of raster compare line;
///             on read : bit 8 of the CURRENT raster line (not the
///             value last written).
///
/// YSCROLL, DEN, BMM, ECM, and the write-side of bit 7 are each covered by
/// their own slice tests (bad-line, display-mode, raster-IRQ). This file
/// backfills the remaining gaps: RSEL routing and the bit-7 read-vs-write
/// asymmetry plus full bit-pattern preservation.
/// </summary>
public sealed class VicIID011ControlTests
{
    private const ushort ScreenControl1 = 0xD011;
    private const ushort RasterCompareLow = 0xD012;

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
    /// FR/TR: FR-VIC (BACKFILL-VIDEO-001 $D011 control register).
    /// Use case: RSEL ($D011 bit 3) selects 25-row vs 24-row display. The
    /// bit is purely software-controlled state; a write of bit 3 must be
    /// observable in a subsequent read of the same register on any line
    /// where bit 7 of the raster (current line bit 8) is zero.
    /// Acceptance: Write $D011 = $08 (RSEL=1, all other bits 0) with VIC
    /// at line 0; Read $D011 returns $08 (bit 3 set, bit 7 = current
    /// raster line bit 8 = 0).
    /// </summary>
    [Fact]
    public void D011_Rsel_RoundTripsThroughReadWrite()
    {
        var vic = BuildVic();

        vic.Write(ScreenControl1, 0x08);

        var value = vic.Read(ScreenControl1);
        Assert.Equal(0x08, value & 0x08);
        // Raster line 0 -> bit 7 of read should be 0.
        Assert.Equal(0x00, value & 0x80);
    }

    /// <summary>
    /// FR/TR: FR-VIC (BACKFILL-VIDEO-001 $D011 control register).
    /// Use case: Reset() must restore $D011 to a well-defined power-on
    /// value. Mos6569.Reset() zeroes the entire register backing store,
    /// so RSEL (bit 3), DEN (bit 4), and all other $D011 bits are 0
    /// immediately after reset. This is the same state the real chip
    /// exposes on a cold boot before the C64 KERNAL programs $D011.
    /// Acceptance: After Reset(), Read $D011 returns 0 (all bits clear,
    /// including bit 7 since raster line is at 0).
    /// </summary>
    [Fact]
    public void D011_AfterReset_AllBitsClear()
    {
        var vic = BuildVic();

        vic.Reset();

        Assert.Equal(0x00, vic.Read(ScreenControl1));
    }

    /// <summary>
    /// FR/TR: FR-VIC (BACKFILL-VIDEO-001 $D011 control register).
    /// Use case: The read-side of $D011 bit 7 returns bit 8 of the
    /// CURRENT raster line, not the high bit of the latched raster
    /// compare value. Writing $D011 = $80 latches the raster-compare
    /// high bit but does NOT cause subsequent reads at lines &lt; 256 to
    /// return bit 7 set; conversely, advancing the raster past line
    /// 255 makes bit 7 of the read = 1 even though no $80 write
    /// happened in between.
    /// Acceptance: After Write $D011 = $80 (compare bit 8 = 1) with
    /// VIC at line 0, Read $D011 bit 7 = 0 (current line 0, bit 8 = 0).
    /// After advancing to raster line $100 (bit 8 = 1), Read $D011
    /// bit 7 = 1 regardless of the previously latched compare value.
    /// </summary>
    [Fact]
    public void D011_Bit7Read_ReflectsCurrentRasterLineBit8_NotComparelatch()
    {
        var vic = BuildVic();

        // Latch compare bit 8 = 1 via $D011 bit 7 write.
        vic.Write(ScreenControl1, 0x80);

        // At line 0, current raster line bit 8 is 0 regardless of the
        // write we just performed.
        var atLineZero = vic.Read(ScreenControl1);
        Assert.Equal(0x00, atLineZero & 0x80);

        // Advance to line $100 (raster line 256 -> bit 8 = 1).
        AdvanceTo(vic, 0x100, 0);
        var atLineHigh = vic.Read(ScreenControl1);
        Assert.Equal(0x80, atLineHigh & 0x80);
    }

    /// <summary>
    /// FR/TR: FR-VIC (BACKFILL-VIDEO-001 $D011 control register).
    /// Use case: Bits 6..0 of $D011 (ECM, BMM, DEN, RSEL, YSCROLL2..0)
    /// are pure software-controlled latches. A write of a full bit
    /// pattern across those positions must be exactly recoverable on
    /// read while the raster is on a line whose bit 8 is 0.
    /// Acceptance: Write $D011 = $5F (bits 0,1,2,3,4,6 = ECM+DEN+RSEL+
    /// YSCROLL=7); Read $D011 at line 0 returns $5F unchanged because
    /// bit 7 of the read is sourced from current raster line bit 8 = 0
    /// and ORed in (0 contributes nothing).
    /// </summary>
    [Fact]
    public void D011_FullBitPattern_PreservedAcrossReadWrite()
    {
        var vic = BuildVic();

        vic.Write(ScreenControl1, 0x5F);

        // Raster line 0, bit 8 = 0; read should equal what was written.
        Assert.Equal(0x5F, vic.Read(ScreenControl1));
    }

    /// <summary>
    /// FR/TR: FR-VIC (BACKFILL-VIDEO-001 $D011 control register).
    /// Use case: Reset() must clear YSCROLL ($D011 bits 0..2) so the
    /// bad-line predicate is well-defined immediately after power-on.
    /// The bad-line detection key is (raster &amp; 7) == YSCROLL; a
    /// non-zero YSCROLL post-reset would change which lines qualify
    /// before software has even configured the chip.
    /// Acceptance: After Reset(), the YSCROLL property reports 0 and
    /// the low 3 bits of Read $D011 are 0.
    /// </summary>
    [Fact]
    public void D011_AfterReset_YScrollClear()
    {
        var vic = BuildVic();

        vic.Reset();

        Assert.Equal(0, vic.YScroll);
        Assert.Equal(0x00, vic.Read(ScreenControl1) & 0x07);
    }
}
