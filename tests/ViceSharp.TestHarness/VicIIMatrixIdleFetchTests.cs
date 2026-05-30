namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Chips.VicIi;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: FR-VIC-001 / FR-VIC-006 / FR-VIC-008 / FR-VIC-009 /
/// TR-VIC-EDGE-005 / TEST-VIC-001 / BACKFILL-VIDEO-001.
/// VICE viciisc matrix and idle fetch behavior: bad-line prefetch slots
/// fill vbuf with $ff and cbuf from the CPU-visible RAM low nibble,
/// matrix slots latch screen bytes plus color RAM low nibbles, and idle
/// graphics fetches read $39ff only when ECM is active.
/// </summary>
public sealed class VicIIMatrixIdleFetchTests
{
    private const ushort ScreenControl1 = 0xD011;
    private const ushort MemoryPointers = 0xD018;

    private static Mos6569 BuildVic()
    {
        var irq = new InterruptLine(InterruptType.Irq);
        return new Mos6569(new BasicBus(), irq);
    }

    /// <summary>
    /// FR-VIC-001 / TR-VIC-EDGE-005.
    /// Use case: VICE bad-line prefetch slots do not fetch screen
    /// matrix bytes; they seed vbuf with $ff and cbuf from the CPU
    /// RAM value's low nibble.
    /// Acceptance: Prefetching with CPU RAM value $ab stores matrix
    /// byte $ff and color nibble $0b in the selected matrix slot.
    /// </summary>
    [Fact]
    public void MatrixPrefetch_LatchesFfAndCpuRamLowNibble()
    {
        var vic = BuildVic();

        vic.LatchVideoMatrixPrefetch(0, 0xAB);

        Assert.Equal(0xFF, vic.PeekVideoMatrixLatch(0));
        Assert.Equal(0x0B, vic.PeekColorMatrixLatch(0));
    }

    /// <summary>
    /// FR-VIC-001 / TR-VIC-EDGE-005.
    /// Use case: VICE matrix fetch slots latch the screen matrix byte
    /// and the low nibble of color RAM for the same VC counter.
    /// Acceptance: Latching matrix byte $42 with color RAM byte $9e
    /// stores matrix $42 and color nibble $0e.
    /// </summary>
    [Fact]
    public void MatrixFetch_LatchesScreenByteAndColorLowNibble()
    {
        var vic = BuildVic();

        vic.LatchVideoMatrixFetch(7, 0x42, 0x9E);

        Assert.Equal(0x42, vic.PeekVideoMatrixLatch(7));
        Assert.Equal(0x0E, vic.PeekColorMatrixLatch(7));
    }

    /// <summary>
    /// FR-VIC-001 / TR-VIC-EDGE-005.
    /// Use case: Standard text graphics fetches consume the latched
    /// vbuf character byte, not the screen matrix address directly.
    /// Acceptance: With character base $0800 and latched character $23,
    /// the next graphics fetch address is $0800 + ($23 * 8).
    /// </summary>
    [Fact]
    public void StandardTextGraphicsFetch_UsesLatchedMatrixByte()
    {
        var vic = BuildVic();

        vic.Write(MemoryPointers, 0x02);
        vic.LatchVideoMatrixFetch(0, 0x23, 0x05);

        Assert.Equal((ushort)0x0918, vic.ConsumeGraphicsFetchAddress());
    }

    /// <summary>
    /// FR-VIC-001 / TR-VIC-EDGE-005.
    /// Use case: VICE idle graphics fetches use a fixed gap address,
    /// but ECM changes that address from $3fff to $39ff.
    /// Acceptance: ECM clear reports $3fff; ECM set reports $39ff;
    /// clearing ECM returns the address to $3fff.
    /// </summary>
    [Fact]
    public void IdleGraphicsFetchAddress_Uses39ffOnlyForEcm()
    {
        var vic = BuildVic();

        Assert.Equal(0x3FFF, vic.IdleGraphicsFetchAddress);

        vic.Write(ScreenControl1, 0x40);
        Assert.Equal(0x39FF, vic.IdleGraphicsFetchAddress);

        vic.Write(ScreenControl1, 0x00);
        Assert.Equal(0x3FFF, vic.IdleGraphicsFetchAddress);
    }

    /// <summary>
    /// FR-VIC-001 / TR-VIC-EDGE-005.
    /// Use case: The VIC-II matrix latch is exactly 40 columns wide.
    /// Acceptance: Slots 0 and 39 are valid; slot 40 is rejected.
    /// </summary>
    [Fact]
    public void MatrixLatch_RejectsSlotsOutsideFortyColumnBuffer()
    {
        var vic = BuildVic();

        vic.LatchVideoMatrixFetch(0, 0x11, 0x01);
        vic.LatchVideoMatrixFetch(39, 0x22, 0x02);

        Assert.Throws<ArgumentOutOfRangeException>(() => vic.LatchVideoMatrixFetch(40, 0x33, 0x03));
    }
}
