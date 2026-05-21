namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Chips.VicIi;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: FR-VIC-001 / TEST-VIC-001 raster IRQ + raster compare (BACKFILL-VIDEO-001).
/// Use case: $D011 bit 7 plus $D012 form the 9-bit raster-compare register.
/// $D019 latches the per-source IRQ state (bit 0 = raster). $D01A holds the
/// per-source enable mask. IRQ output is asserted iff (latch &amp; enable) != 0
/// for any of the four sources; $D019 is write-1-to-clear.
/// </summary>
public sealed class VicIIRasterIrqTests
{
    private const ushort ScreenControl1 = 0xD011;
    private const ushort RasterCompareLow = 0xD012;
    private const ushort InterruptLatch = 0xD019;
    private const ushort InterruptEnable = 0xD01A;

    private static Mos6569 BuildVic(out IInterruptLine irq)
    {
        irq = new InterruptLine(InterruptType.Irq);
        return new Mos6569(new BasicBus(), irq);
    }

    private static void Advance(Mos6569 vic, int cycles)
    {
        for (var i = 0; i < cycles; i++) vic.Tick();
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
    /// FR/TR: FR-VIC-001 / TEST-VIC-001 raster IRQ + raster compare (BACKFILL-VIDEO-001).
    /// Use case: Writing $D012 sets the low 8 bits of the 9-bit raster-compare
    /// register without altering the high bit. Reading $D012 returns the
    /// current raster line, not the compare value.
    /// Acceptance: After Write $D012 = $42 with no ticks issued, RasterIrqLine
    /// equals $0042 and Read $D012 reflects the current raster line (0).
    /// </summary>
    [Fact]
    public void RasterCompare_WriteD012_SetsLowByteAndPreservesHighBit()
    {
        var vic = BuildVic(out _);

        vic.Write(RasterCompareLow, 0x42);

        Assert.Equal(0x0042, vic.RasterIrqLine);
        Assert.Equal(0x00, vic.Read(RasterCompareLow));
    }

    /// <summary>
    /// FR/TR: FR-VIC-001 / TEST-VIC-001 raster IRQ + raster compare (BACKFILL-VIDEO-001).
    /// Use case: $D011 bit 7 forms the high bit of the 9-bit raster-compare
    /// register on write. Writing $D011 bit 7 set with $D012 = $42 produces
    /// compare line $142; clearing bit 7 produces $042.
    /// Acceptance: Write order $D012 then $D011 produces the expected merged
    /// compare value in both bit-7-set and bit-7-clear configurations.
    /// </summary>
    [Fact]
    public void RasterCompare_D011Bit7_FormsHighBitOfCompare()
    {
        var vic = BuildVic(out _);

        vic.Write(RasterCompareLow, 0x42);
        vic.Write(ScreenControl1, 0x80);
        Assert.Equal(0x0142, vic.RasterIrqLine);

        vic.Write(ScreenControl1, 0x00);
        Assert.Equal(0x0042, vic.RasterIrqLine);
    }

    /// <summary>
    /// FR/TR: FR-VIC-001 / TEST-VIC-001 raster IRQ + raster compare (BACKFILL-VIDEO-001).
    /// Use case: When the live raster line crosses the compare value with
    /// raster IRQ enabled in $D01A bit 0, $D019 bit 0 must latch and the
    /// IRQ line must assert.
    /// Acceptance: Setting compare to line 1 with enable bit 0 and stepping
    /// the VIC past cycle 58 of line 1 produces $D019 bit 0 set and IRQ
    /// asserted on the connected interrupt line.
    /// </summary>
    [Fact]
    public void RasterIrq_LatchesAndAssertsAtCompareMatch_WhenEnabled()
    {
        var vic = BuildVic(out var irq);

        vic.Write(RasterCompareLow, 0x01);
        vic.Write(InterruptEnable, 0x01);

        AdvanceTo(vic, 1, 0);
        Advance(vic, 59);

        Assert.True(irq.IsAsserted);
        Assert.Equal(0x81, vic.Read(InterruptLatch));
    }

    /// <summary>
    /// FR/TR: FR-VIC-001 / TEST-VIC-001 raster IRQ + raster compare (BACKFILL-VIDEO-001).
    /// Use case: $D019 is write-1-to-clear per bit. After a raster IRQ fires,
    /// writing $01 to $D019 must clear bit 0 of the latch and (since no other
    /// latches are set) deassert the IRQ output.
    /// Acceptance: After IRQ has fired and $D019 reads $81, writing $D019 = $01
    /// causes the next $D019 read to return $00 and the IRQ line to release.
    /// </summary>
    [Fact]
    public void RasterIrq_WriteOneToD019_ClearsLatchAndDeassertsIrq()
    {
        var vic = BuildVic(out var irq);

        vic.Write(RasterCompareLow, 0x01);
        vic.Write(InterruptEnable, 0x01);
        AdvanceTo(vic, 1, 0);
        Advance(vic, 59);

        Assert.True(irq.IsAsserted);
        Assert.Equal(0x81, vic.Read(InterruptLatch));

        vic.Write(InterruptLatch, 0x01);

        Assert.False(irq.IsAsserted);
        Assert.Equal(0x00, vic.Read(InterruptLatch));
    }

    /// <summary>
    /// FR/TR: FR-VIC-001 / TEST-VIC-001 raster IRQ + raster compare (BACKFILL-VIDEO-001).
    /// Use case: The $D019 latch tracks the source independently of the
    /// $D01A enable mask. When the raster crosses the compare line with
    /// $D01A bit 0 = 0, $D019 bit 0 must still latch but the IRQ output
    /// must NOT assert.
    /// Acceptance: With enable bit 0 = 0 and a compare-line crossing, the
    /// IRQ line stays released while $D019 bit 0 (without the IR master
    /// bit 7) reads as set.
    /// </summary>
    [Fact]
    public void RasterIrq_LatchesEvenWhenEnableBitIsZero_ButIrqStaysReleased()
    {
        var vic = BuildVic(out var irq);

        vic.Write(RasterCompareLow, 0x01);
        vic.Write(InterruptEnable, 0x00);
        AdvanceTo(vic, 1, 0);
        Advance(vic, 59);

        Assert.False(irq.IsAsserted);
        byte latch = vic.Read(InterruptLatch);
        Assert.Equal(0x01, latch & 0x01);
        Assert.Equal(0x00, latch & 0x80);
    }

    /// <summary>
    /// FR/TR: FR-VIC-001 / TEST-VIC-001 raster IRQ + raster compare (BACKFILL-VIDEO-001).
    /// Use case: Native VICE reset starts on raster line 0 with compare line
    /// 0, but the reset line must not immediately leave a stale raster IRQ
    /// latch after the first scanline.
    /// Acceptance: Advancing from reset through line 0 does not set $D019
    /// bit 0 when software has not programmed a compare event.
    /// </summary>
    [Fact]
    public void RasterIrq_DoesNotLatchDefaultResetLineZeroCompare()
    {
        var vic = BuildVic(out var irq);

        Advance(vic, vic.CyclesPerLine);

        Assert.False(irq.IsAsserted);
        Assert.Equal(0x00, vic.Read(InterruptLatch) & 0x01);
    }
}
