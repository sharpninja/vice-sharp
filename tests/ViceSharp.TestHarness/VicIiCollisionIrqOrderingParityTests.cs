namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Chips.VicIi;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// PLAN-VICEPARITY-001 remediation Phase 3 (audit
/// docs/audit-vicii-vs-vice-2026-07-06.md M10/M12): collision-clear and
/// IRQ-line ordering inside the VIC cycle. VICE vicii_cycle order
/// (viciisc/vicii-cycle.c:401-433): Phi1 fetch, border check, can_* capture,
/// draw (collision accumulate), DEFERRED $D01E/$D01F clear, collision IRQ
/// check; and every VIC IRQ source reaches the CPU line through the same
/// one-cycle recognition latency (vicii_irq_set_line -> maincpu, interrupt.c)
/// while the $D019 latch and bit 7 are visible in the latch cycle itself.
/// </summary>
public sealed class VicIiCollisionIrqOrderingParityTests
{
    private const ushort SpriteX0Lo   = 0xD000;
    private const ushort SpriteY0     = 0xD001;
    private const ushort SpriteX1Lo   = 0xD002;
    private const ushort SpriteY1     = 0xD003;
    private const ushort SpriteX0Hi   = 0xD010;
    private const ushort SpriteEnable = 0xD015;
    private const ushort InterruptLatch  = 0xD019;
    private const ushort InterruptEnable = 0xD01A;
    private const ushort SpriteSSColl = 0xD01E;

    // Same trigger geometry as VicSpriteRenderDivergentParityTests: sprites at
    // X=20 trigger at RasterX 15 (flags xpos 16..23 covers 20 at pixel 4) on
    // the first render line after DMA start at Y=100.
    private const byte   SpriteTestX  = 20;
    private const byte   SpriteTestY  = 100;
    private const ushort TriggerLine  = 101;
    private const byte   TriggerCycle = 15;

    private static Mos6569 BuildVicWithSprites(out IInterruptLine irq)
    {
        irq = new InterruptLine(InterruptType.Irq);
        var vic = new Mos6569(new BasicBus(), irq);
        vic.Reset();
        var mem = new byte[0x4000];
        System.Array.Fill(mem, (byte)0xFF, 0, 64);   // sprite 0 data: opaque
        System.Array.Fill(mem, (byte)0xFF, 64, 64);  // sprite 1 data: opaque
        mem[0x3F8] = 0; // sprite 0 pointer
        mem[0x3F9] = 1; // sprite 1 pointer
        vic.VideoMemoryReader = addr => addr < mem.Length ? mem[addr] : (byte)0;
        return vic;
    }

    private static void AdvanceTo(Mos6569 vic, ushort rasterLine, byte rasterCycle)
    {
        int maxCycles = vic.TotalLines * vic.CyclesPerLine * 3;
        for (int cycle = 0; cycle < maxCycles; cycle++)
        {
            if (vic.CurrentRasterLine == rasterLine && vic.RasterX == rasterCycle)
                return;
            vic.Tick();
        }

        throw new InvalidOperationException(
            $"VIC did not reach line {rasterLine}, cycle {rasterCycle}.");
    }

    /// <summary>
    /// FR: FR-VIC-SPRITE-COLLISION, TR: TR-VIC-IRQORDER-001, TEST: TEST-VIC-IRQORDER-01.
    /// Use case: VICE applies the deferred $D01E clear AFTER the cycle's
    /// sprite draw has accumulated collisions (vicii_cycle: draw at
    /// vicii-cycle.c:411, clear at :413-425), so a collision landing in the
    /// same cycle as the clear is wiped with the old contents and never
    /// becomes CPU-visible. Clearing before the draw would let the same-cycle
    /// collision survive.
    /// Acceptance: with two overlapping sprites colliding on consecutive
    /// cycles, a $D01E read one cycle before an overlap cycle leaves the
    /// latch exactly 0 after that cycle (fresh collision wiped by the
    /// post-draw clear), and the following cycle re-accumulates a non-zero
    /// latch.
    /// </summary>
    [Fact]
    public void D01eRead_DeferredClear_AppliesAfterTheNextCycleDraw()
    {
        var vic = BuildVicWithSprites(out _);
        vic.Write(SpriteX0Hi, 0);
        vic.Write(SpriteX0Lo, SpriteTestX);
        vic.Write(SpriteY0, SpriteTestY);
        vic.Write(SpriteX1Lo, SpriteTestX);
        vic.Write(SpriteY1, SpriteTestY);
        vic.Write(SpriteEnable, 0x03);

        // First overlap cycle: latch accumulates both sprites.
        AdvanceTo(vic, TriggerLine, TriggerCycle);
        Assert.NotEqual(0, vic.SpriteSpriteCollision);

        // Read $D01E: returns the mask and schedules the deferred clear
        // (VICE vicii-mem.c d01e read; clear_collisions = 0x1e).
        byte mask = vic.Read(SpriteSSColl);
        Assert.NotEqual(0, mask);

        // Next cycle: the sprites are still overlapping, so the draw
        // accumulates a fresh collision; VICE's clear then wipes it in the
        // same cycle (draw first, clear second).
        vic.Tick();
        Assert.Equal(0, vic.SpriteSpriteCollision);

        // One more cycle: no pending clear remains; the still-overlapping
        // sprites re-accumulate a non-zero latch.
        vic.Tick();
        Assert.NotEqual(0, vic.SpriteSpriteCollision);
    }

    /// <summary>
    /// FR: FR-VIC-SPRITE-COLLISION, TR: TR-VIC-IRQORDER-001, TEST: TEST-VIC-IRQORDER-02.
    /// Use case: every VIC IRQ source reaches the CPU with one cycle of
    /// recognition latency (vicii_irq_set_line -> maincpu_set_irq,
    /// interrupt.c), exactly as the managed raster path already models; the
    /// $D019 source bit and bit 7 are visible in the latch cycle itself. The
    /// collision path must not assert the line earlier than the raster path
    /// would.
    /// Acceptance: on the first sprite-sprite collision cycle with $D01A
    /// bit 2 enabled, $D019 shows bits 2 and 7 set but the IRQ line is still
    /// released; the line asserts on the following cycle.
    /// </summary>
    [Fact]
    public void CollisionIrq_LineAsserts_OneCycleAfterLatch_LikeRaster()
    {
        var vic = BuildVicWithSprites(out var irq);
        vic.Write(InterruptEnable, 0x04); // sprite-sprite IRQ enabled
        vic.Write(SpriteX0Hi, 0);
        vic.Write(SpriteX0Lo, SpriteTestX);
        vic.Write(SpriteY0, SpriteTestY);
        vic.Write(SpriteX1Lo, SpriteTestX);
        vic.Write(SpriteY1, SpriteTestY);
        vic.Write(SpriteEnable, 0x03);

        // One cycle before the first overlap: nothing latched, line released.
        AdvanceTo(vic, TriggerLine, (byte)(TriggerCycle - 1));
        Assert.Equal(0, vic.Read(InterruptLatch) & 0x04);
        Assert.False(irq.IsAsserted);

        // Collision cycle: latch bit 2 and IRQ mirror bit 7 appear now, but
        // the line rise is presented one cycle later.
        vic.Tick();
        Assert.Equal(0x04, vic.Read(InterruptLatch) & 0x04);
        Assert.Equal(0x80, vic.Read(InterruptLatch) & 0x80);
        Assert.False(irq.IsAsserted);

        // Recognition cycle: the line is up.
        vic.Tick();
        Assert.True(irq.IsAsserted);
    }

    /// <summary>
    /// FR: FR-VIC-LIGHTPEN, TR: TR-VIC-IRQORDER-001, TEST: TEST-VIC-IRQORDER-03.
    /// Use case: the light-pen IRQ goes through the same vicii_irq_set_line
    /// path as raster and collisions (vicii-irq.c), so its line rise carries
    /// the same one-cycle recognition latency while $D019 bit 3 and bit 7
    /// latch immediately.
    /// Acceptance: with $D01A bit 3 enabled, a pen trigger sets $D019 bits 3
    /// and 7 in the trigger cycle with the line still released; the line
    /// asserts after the next cycle.
    /// </summary>
    [Fact]
    public void LightPenIrq_LineAsserts_OneCycleAfterLatch_LikeRaster()
    {
        var irq = new InterruptLine(InterruptType.Irq);
        var vic = new Mos6569(new BasicBus(), irq);
        vic.Reset();
        vic.Write(InterruptEnable, 0x08); // light-pen IRQ enabled
        Assert.False(irq.IsAsserted);

        vic.TriggerLightPen();
        Assert.Equal(0x08, vic.Read(InterruptLatch) & 0x08);
        Assert.Equal(0x80, vic.Read(InterruptLatch) & 0x80);
        Assert.False(irq.IsAsserted);

        vic.Tick();
        Assert.True(irq.IsAsserted);
    }
}
