namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Chips.VicIi;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: FR-VIC-001 / FR-VIC-005 / TR-VIC-EDGE-006 / TEST-VIC-001
/// (BACKFILL-VIDEO-001 VIC-II register readback).
/// VICE viciisc/vicii-mem.c hardcodes observable readback values for
/// selected VIC-II registers: $D019 ORs fixed bits 6-4, $D01A ORs the
/// high nibble, collision registers are read-only latches, and unused
/// $D02F-$D03F registers read as $FF.
/// </summary>
public sealed class VicIIRegisterReadbackTests
{
    private const ushort InterruptLatch = 0xD019;
    private const ushort InterruptEnable = 0xD01A;
    private const ushort SpriteSpriteCollision = 0xD01E;
    private const ushort SpriteBackgroundCollision = 0xD01F;

    private static Mos6569 BuildVic()
    {
        var irq = new InterruptLine(InterruptType.Irq);
        return new Mos6569(new BasicBus(), irq);
    }

    /// <summary>
    /// FR/TR: FR-VIC-001 / TR-VIC-EDGE-006 / TEST-VIC-001.
    /// Use case: x64sc reports $D019 bits 6-4 as fixed high while bits
    /// 3-0 expose the IRQ source latches and bit 7 exposes the IR master
    /// state.
    /// Acceptance: Reset $D019 reads $70; after a raster IRQ source
    /// latches and is enabled, $D019 reads $F1; after write-one-to-clear,
    /// the source and master bits clear but fixed bits remain high.
    /// </summary>
    [Fact]
    public void D019_ReadbackKeepsFixedBitsHigh()
    {
        var vic = BuildVic();

        Assert.Equal(0x70, vic.Read(InterruptLatch));

        vic.Write(0xD012, 0x01);
        vic.Write(InterruptEnable, 0x01);
        AdvanceTo(vic, 1, 59);

        Assert.Equal(0xF1, vic.Read(InterruptLatch));

        vic.Write(InterruptLatch, 0x01);

        Assert.Equal(0x70, vic.Read(InterruptLatch));
    }

    /// <summary>
    /// FR/TR: FR-VIC-001 / TR-VIC-EDGE-006 / TEST-VIC-001.
    /// Use case: x64sc reports $D01A bits 7-4 as fixed high while bits
    /// 3-0 retain the programmable IRQ enable mask.
    /// Acceptance: Reset $D01A reads $F0; writing all bits latches only
    /// the low source-enable nibble and reads back as $FF; writing $05
    /// reads back as $F5.
    /// </summary>
    [Fact]
    public void D01A_ReadbackKeepsHighNibbleHighAndMasksWritesToSources()
    {
        var vic = BuildVic();

        Assert.Equal(0xF0, vic.Read(InterruptEnable));

        vic.Write(InterruptEnable, 0xFF);
        Assert.Equal(0xFF, vic.Read(InterruptEnable));

        vic.Write(InterruptEnable, 0x05);
        Assert.Equal(0xF5, vic.Read(InterruptEnable));
    }

    /// <summary>
    /// FR/TR: FR-VIC-001 / TR-VIC-EDGE-006 / TEST-VIC-001.
    /// Use case: $D02F-$D03F are unused VIC-II register slots. x64sc
    /// ignores writes and reads them back as $FF.
    /// Acceptance: Writing varied values to every unused slot still reads
    /// $FF from each slot.
    /// </summary>
    [Fact]
    public void UnusedRegistersD02FThroughD03F_ReadAsFFAndIgnoreWrites()
    {
        var vic = BuildVic();

        for (ushort address = 0xD02F; address <= 0xD03F; address++)
        {
            vic.Write(address, (byte)address);
            Assert.Equal(0xFF, vic.Read(address));
        }
    }

    /// <summary>
    /// FR/TR: FR-VIC-005 / TR-VIC-EDGE-006 / TEST-VIC-001.
    /// Use case: $D01E and $D01F are read-only collision latches. Writes
    /// must not fabricate collision bits or change the read-clear behavior.
    /// Acceptance: Writes of $FF to both collision registers leave the
    /// latch values at zero, and the read-clear path remains destructive
    /// only for real latched collisions.
    /// </summary>
    [Fact]
    public void CollisionRegistersIgnoreWrites()
    {
        var vic = BuildVic();

        vic.Write(SpriteSpriteCollision, 0xFF);
        vic.Write(SpriteBackgroundCollision, 0xFF);

        Assert.Equal(0x00, vic.Read(SpriteSpriteCollision));
        Assert.Equal(0x00, vic.Read(SpriteBackgroundCollision));
    }

    private static void AdvanceTo(Mos6569 vic, ushort rasterLine, byte rasterCycle)
    {
        var maxCycles = vic.TotalLines * vic.CyclesPerLine * 2;
        for (var cycle = 0; cycle < maxCycles; cycle++)
        {
            if (vic.CurrentRasterLine == rasterLine && vic.RasterX == rasterCycle)
            {
                return;
            }

            vic.Tick();
        }

        throw new InvalidOperationException($"VIC did not reach line ${rasterLine:X3}, cycle {rasterCycle}.");
    }
}
