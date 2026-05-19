namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Chips.VicIi;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: FR-VIC (BACKFILL-VIDEO-001 sprite collision IRQ).
/// Use case: When sprite-sprite or sprite-background collisions are detected
/// inside the VIC-II raster pipeline, $D019 bits 2 and 1 respectively must
/// latch (write-1-to-clear). $D01A bits 2 and 1 form the enable mask for
/// each source. The IRQ output asserts when (latch &amp; enable) != 0 for any
/// of the four sources (bit 0 raster, bit 1 sprite-bg, bit 2 sprite-sprite,
/// bit 3 light pen).
/// </summary>
public sealed class VicIISpriteCollisionIrqTests
{
    private const ushort SpriteX0 = 0xD000;
    private const ushort SpriteY0 = 0xD001;
    private const ushort SpriteX1 = 0xD002;
    private const ushort SpriteY1 = 0xD003;
    private const ushort ScreenControl1 = 0xD011;
    private const ushort SpriteEnable = 0xD015;
    private const ushort ScreenControl2 = 0xD016;
    private const ushort InterruptLatch = 0xD019;
    private const ushort InterruptEnable = 0xD01A;

    private const byte SpriteBgCollisionLatchBit = 0x02;
    private const byte SpriteSpriteCollisionLatchBit = 0x04;

    private const int CyclesPerLine = Mos6569.PalCyclesPerLine;

    /// <summary>
    /// Build a minimal VIC-II configured the same way SpriteCollisionTests
    /// does: fully opaque sprite data block via VideoMemoryReader and a
    /// configurable background foreground pattern for sprite-bg collision.
    /// </summary>
    private static Mos6569 BuildVic(out IInterruptLine irq, byte spriteDataBlock = 0x0D, byte bgPattern = 0x00)
    {
        irq = new InterruptLine(InterruptType.Irq);
        var vic = new Mos6569(new BasicBus(), irq);
        vic.VideoMemoryReader = addr =>
        {
            ushort masked = (ushort)(addr & 0x3FFF);

            if (masked >= 0x03F8 && masked <= 0x03FF)
            {
                return spriteDataBlock;
            }

            ushort spriteBase = (ushort)(spriteDataBlock * 64);
            if (masked >= spriteBase && masked < spriteBase + 64)
            {
                return 0xFF;
            }

            return bgPattern;
        };
        // DEN=1, RSEL=1, YSCROLL=3 + CSEL=1 (40 cols): same as SpriteCollisionTests.
        vic.Write(ScreenControl1, 0x1B);
        vic.Write(ScreenControl2, 0x08);
        return vic;
    }

    private static void AdvanceTo(Mos6569 vic, int line, int extra = CyclesPerLine)
    {
        int budget = vic.TotalLines * CyclesPerLine * 3;
        while (budget-- > 0 && !(vic.CurrentRasterLine == line && vic.RasterX == 0))
        {
            vic.Tick();
        }
        for (int i = 0; i < extra; i++)
        {
            vic.Tick();
        }
    }

    /// <summary>
    /// FR/TR: FR-VIC (BACKFILL-VIDEO-001 sprite collision IRQ).
    /// Use case: When two enabled sprites overlap and the sprite-sprite
    /// collision latch ($D01E) accumulates non-zero bits, $D019 bit 2 must
    /// be set. The latch is independent of the $D01A enable mask.
    /// Acceptance: After driving the raster past two overlapping enabled
    /// sprites, reading $D019 shows bit 2 set.
    /// </summary>
    [Fact]
    public void SpriteSprite_Collision_SetsD019Bit2()
    {
        var vic = BuildVic(out _);
        vic.Write(SpriteX0, 100);
        vic.Write(SpriteY0, 100);
        vic.Write(SpriteX1, 100);
        vic.Write(SpriteY1, 100);
        vic.Write(SpriteEnable, 0x03);

        AdvanceTo(vic, line: 90, extra: 0);
        AdvanceTo(vic, line: 130, extra: 0);

        byte latch = vic.Read(InterruptLatch);
        Assert.Equal(SpriteSpriteCollisionLatchBit, (byte)(latch & SpriteSpriteCollisionLatchBit));
    }

    /// <summary>
    /// FR/TR: FR-VIC (BACKFILL-VIDEO-001 sprite collision IRQ).
    /// Use case: When a sprite overlaps a foreground-bearing background
    /// character pixel and the sprite-background latch ($D01F) accumulates
    /// non-zero bits, $D019 bit 1 must be set. The latch is independent
    /// of the $D01A enable mask.
    /// Acceptance: After driving the raster past an enabled sprite over a
    /// fully-foreground background, reading $D019 shows bit 1 set.
    /// </summary>
    [Fact]
    public void SpriteBackground_Collision_SetsD019Bit1()
    {
        var vic = BuildVic(out _, bgPattern: 0xFF);
        vic.Write(SpriteX0, 100);
        vic.Write(SpriteY0, 100);
        vic.Write(SpriteEnable, 0x01);

        AdvanceTo(vic, line: 90, extra: 0);
        AdvanceTo(vic, line: 130, extra: 0);

        byte latch = vic.Read(InterruptLatch);
        Assert.Equal(SpriteBgCollisionLatchBit, (byte)(latch & SpriteBgCollisionLatchBit));
    }

    /// <summary>
    /// FR/TR: FR-VIC (BACKFILL-VIDEO-001 sprite collision IRQ).
    /// Use case: $D01A bit 2 enables the sprite-sprite IRQ. When enabled
    /// and a sprite-sprite collision occurs, the connected IRQ line must
    /// assert and $D019 bit 7 (IR master) must be set. Writing $04 to
    /// $D019 clears bit 2 and (with no other source latched) deasserts
    /// the IRQ output.
    /// Acceptance: With $D01A = $04, after a sprite-sprite collision the
    /// IRQ is asserted; after writing $D019 = $04 the IRQ deasserts.
    /// </summary>
    [Fact]
    public void SpriteSprite_Collision_AssertsIrq_WhenEnabled_AndClearableViaD019()
    {
        var vic = BuildVic(out var irq);
        vic.Write(InterruptEnable, SpriteSpriteCollisionLatchBit);
        vic.Write(SpriteX0, 100);
        vic.Write(SpriteY0, 100);
        vic.Write(SpriteX1, 100);
        vic.Write(SpriteY1, 100);
        vic.Write(SpriteEnable, 0x03);

        AdvanceTo(vic, line: 90, extra: 0);
        AdvanceTo(vic, line: 130, extra: 0);

        Assert.True(irq.IsAsserted);
        byte latch = vic.Read(InterruptLatch);
        Assert.Equal(SpriteSpriteCollisionLatchBit, (byte)(latch & SpriteSpriteCollisionLatchBit));
        Assert.Equal(0x80, latch & 0x80);

        vic.Write(InterruptLatch, SpriteSpriteCollisionLatchBit);

        Assert.False(irq.IsAsserted);
        Assert.Equal(0x00, vic.Read(InterruptLatch) & SpriteSpriteCollisionLatchBit);
    }

    /// <summary>
    /// FR/TR: FR-VIC (BACKFILL-VIDEO-001 sprite collision IRQ).
    /// Use case: $D01A bit 1 enables the sprite-background IRQ. When
    /// enabled and a sprite-background collision occurs, the IRQ line
    /// must assert and $D019 bit 7 must be set. Writing $02 to $D019
    /// clears bit 1 and deasserts the IRQ (no other source latched).
    /// Acceptance: With $D01A = $02, after a sprite-bg collision the IRQ
    /// is asserted; after writing $D019 = $02 the IRQ deasserts.
    /// </summary>
    [Fact]
    public void SpriteBackground_Collision_AssertsIrq_WhenEnabled_AndClearableViaD019()
    {
        var vic = BuildVic(out var irq, bgPattern: 0xFF);
        vic.Write(InterruptEnable, SpriteBgCollisionLatchBit);
        vic.Write(SpriteX0, 100);
        vic.Write(SpriteY0, 100);
        vic.Write(SpriteEnable, 0x01);

        AdvanceTo(vic, line: 90, extra: 0);
        AdvanceTo(vic, line: 130, extra: 0);

        Assert.True(irq.IsAsserted);
        byte latch = vic.Read(InterruptLatch);
        Assert.Equal(SpriteBgCollisionLatchBit, (byte)(latch & SpriteBgCollisionLatchBit));
        Assert.Equal(0x80, latch & 0x80);

        vic.Write(InterruptLatch, SpriteBgCollisionLatchBit);

        Assert.False(irq.IsAsserted);
        Assert.Equal(0x00, vic.Read(InterruptLatch) & SpriteBgCollisionLatchBit);
    }

    /// <summary>
    /// FR/TR: FR-VIC (BACKFILL-VIDEO-001 sprite collision IRQ).
    /// Use case: With both sprite-sprite AND sprite-background collisions
    /// occurring on the same raster pass, $D019 latches bits 1 and 2
    /// together. Enabling either bit in $D01A drives the IRQ output. The
    /// IRQ stays asserted while at least one (latch &amp; enable) bit is set.
    /// Acceptance: Two overlapping sprites over a foreground background
    /// produce $D019 with bits 1 + 2 set. With $D01A = $02 only, IRQ
    /// asserts; with $D01A = $04 only, IRQ asserts; with $D01A = $00,
    /// IRQ deasserts.
    /// </summary>
    [Fact]
    public void Combined_SpriteSprite_AndSpriteBackground_LatchAndIrqCompose()
    {
        var vic = BuildVic(out var irq, bgPattern: 0xFF);
        vic.Write(SpriteX0, 100);
        vic.Write(SpriteY0, 100);
        vic.Write(SpriteX1, 100);
        vic.Write(SpriteY1, 100);
        vic.Write(SpriteEnable, 0x03);

        AdvanceTo(vic, line: 90, extra: 0);
        AdvanceTo(vic, line: 130, extra: 0);

        byte latch = vic.Read(InterruptLatch);
        byte combined = (byte)(SpriteBgCollisionLatchBit | SpriteSpriteCollisionLatchBit);
        Assert.Equal(combined, (byte)(latch & combined));

        // Enable only sprite-bg: IRQ should assert via bit 1.
        vic.Write(InterruptEnable, SpriteBgCollisionLatchBit);
        Assert.True(irq.IsAsserted);

        // Enable only sprite-sprite: IRQ should still assert via bit 2.
        vic.Write(InterruptEnable, SpriteSpriteCollisionLatchBit);
        Assert.True(irq.IsAsserted);

        // Disable both: IRQ deasserts even though latch bits remain set.
        vic.Write(InterruptEnable, 0x00);
        Assert.False(irq.IsAsserted);
    }
}
