namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Chips.VicIi;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: FR-VIC-005 / TEST-VIC-001 (BACKFILL-VIDEO-001 sprite collision).
/// Use case: $D01E (sprite-sprite) and $D01F (sprite-background) collision
/// registers accumulate per-frame based on opaque pixel overlap between
/// enabled sprites or between sprites and non-transparent foreground
/// background pixels. Bits stay set until READ, which atomically clears
/// the latch. Peek must be non-destructive.
/// </summary>
public sealed class SpriteCollisionTests
{
    private const ushort SpriteX0 = 0xD000;
    private const ushort SpriteY0 = 0xD001;
    private const ushort SpriteX1 = 0xD002;
    private const ushort SpriteY1 = 0xD003;
    private const ushort SpriteEnable = 0xD015;
    private const ushort ScreenControl1 = 0xD011;
    private const ushort ScreenControl2 = 0xD016;
    private const ushort MemoryPointers = 0xD018;
    private const ushort SpriteSpriteCollision = 0xD01E;
    private const ushort SpriteBackgroundCollision = 0xD01F;

    private const int CyclesPerLine = Mos6569.PalCyclesPerLine;

    /// <summary>
    /// Build a minimal VIC-II with a memory reader that returns:
    /// - 0xFF for sprite data block (giving fully opaque sprites)
    /// - The provided <paramref name="bgPattern"/> for the character bitmap fetch
    ///   so we can control whether the background has foreground pixels.
    /// Sprite data pointer (screen RAM $07F8+n) returns <paramref name="spriteDataBlock"/>.
    /// </summary>
    private static Mos6569 BuildVic(byte spriteDataBlock = 0x0D, byte bgPattern = 0x00)
    {
        var vic = new Mos6569(new BasicBus(), new InterruptLine(InterruptType.Irq));
        vic.VideoMemoryReader = addr =>
        {
            ushort masked = (ushort)(addr & 0x3FFF);

            // Sprite data pointers live at ScreenMemoryBase + $03F8..$03FF.
            // Default screen base is 0 (register $18 = 0) so pointers are at $03F8-$03FF.
            if (masked >= 0x03F8 && masked <= 0x03FF)
            {
                return spriteDataBlock;
            }

            // Sprite data block = pointer * 64. With pointer=0x0D, data sits at $0340-$037F.
            ushort spriteBase = (ushort)(spriteDataBlock * 64);
            if (masked >= spriteBase && masked < spriteBase + 64)
            {
                return 0xFF; // Fully opaque sprite row
            }

            // Character bitmap area: with $D018 = 0, character base = 0.
            // Return a configurable foreground bitmap pattern for background pixels.
            return bgPattern;
        };
        // Set DEN (display enable) for badline behaviour and standard text mode.
        vic.Write(ScreenControl1, 0x1B); // DEN=1, RSEL=1, YSCROLL=3
        vic.Write(ScreenControl2, 0x08); // CSEL=1 (40 cols)
        return vic;
    }

    /// <summary>
    /// Advance VIC ticks until the given raster line is reached at cycle 0,
    /// then step <paramref name="extra"/> more cycles. Used to ensure a full
    /// scanline of sprite raster passes through the collision detector.
    /// </summary>
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
    /// FR/TR: FR-VIC-005 / TEST-VIC-001 (BACKFILL-VIDEO-001 sprite collision).
    /// Use case: Two enabled sprites placed at identical coordinates with
    /// fully opaque shape data must latch bits 0 and 1 in $D01E once their
    /// raster line is processed.
    /// Acceptance: Reading $D01E returns 0x03 after one full sprite-height
    /// raster pass with both sprites overlapping.
    /// </summary>
    [Fact]
    public void SpriteSprite_Overlap_LatchesBothBitsInD01E()
    {
        var vic = BuildVic();
        // Place both sprites at the same on-screen location (well inside the visible area).
        vic.Write(SpriteX0, 100);
        vic.Write(SpriteY0, 100);
        vic.Write(SpriteX1, 100);
        vic.Write(SpriteY1, 100);
        // Enable sprites 0 and 1.
        vic.Write(SpriteEnable, 0x03);

        // Advance far enough to cover all 21 sprite raster lines plus a margin.
        AdvanceTo(vic, line: 90, extra: 0);
        AdvanceTo(vic, line: 130, extra: 0);

        byte collision = vic.Read(SpriteSpriteCollision);
        Assert.Equal(0x03, collision);
    }

    /// <summary>
    /// FR/TR: FR-VIC-005 / TEST-VIC-001 (BACKFILL-VIDEO-001 sprite collision).
    /// Use case: $D01E read-clear semantics: after reading the latch, a
    /// subsequent read must return 0 until new collisions accumulate.
    /// Acceptance: First read returns 0x03; immediate second read returns 0x00.
    /// </summary>
    [Fact]
    public void SpriteSprite_LatchClearsOnRead()
    {
        var vic = BuildVic();
        vic.Write(SpriteX0, 100);
        vic.Write(SpriteY0, 100);
        vic.Write(SpriteX1, 100);
        vic.Write(SpriteY1, 100);
        vic.Write(SpriteEnable, 0x03);

        AdvanceTo(vic, line: 90, extra: 0);
        AdvanceTo(vic, line: 130, extra: 0);

        Assert.Equal(0x03, vic.Read(SpriteSpriteCollision));
        Assert.Equal(0x00, vic.Read(SpriteSpriteCollision));
    }

    /// <summary>
    /// FR/TR: FR-VIC-005 / TEST-VIC-001 (BACKFILL-VIDEO-001 sprite collision).
    /// Use case: Non-overlapping enabled sprites must not produce false
    /// collisions; $D01E must remain 0.
    /// Acceptance: With sprite 0 and sprite 1 at well-separated X positions
    /// on different scanlines, $D01E reads 0x00 after a full frame's worth
    /// of raster processing.
    /// </summary>
    [Fact]
    public void SpriteSprite_Separated_NoFalseCollision()
    {
        var vic = BuildVic();
        // Sprite 0 at (50, 80); sprite 1 at (200, 150) - non-overlapping bboxes.
        vic.Write(SpriteX0, 50);
        vic.Write(SpriteY0, 80);
        vic.Write(SpriteX1, 200);
        vic.Write(SpriteY1, 150);
        vic.Write(SpriteEnable, 0x03);

        AdvanceTo(vic, line: 70, extra: 0);
        AdvanceTo(vic, line: 180, extra: 0);

        byte collision = vic.Read(SpriteSpriteCollision);
        Assert.Equal(0x00, collision);
    }

    /// <summary>
    /// FR/TR: FR-VIC-005 / TEST-VIC-001 (BACKFILL-VIDEO-001 sprite collision).
    /// Use case: A single enabled sprite drawn over a region whose
    /// underlying character bitmap has foreground pixels must latch bit 0
    /// of $D01F (sprite-background collision register).
    /// Acceptance: Reading $D01F returns 0x01 after the sprite's raster
    /// rows pass over the foreground-heavy character area.
    /// </summary>
    [Fact]
    public void SpriteBackground_OverForeground_LatchesBitInD01F()
    {
        // 0xFF background pattern -> every char pixel is foreground (opaque).
        var vic = BuildVic(bgPattern: 0xFF);
        // Place sprite well inside the visible character area (lines 51-250, x 24-343).
        vic.Write(SpriteX0, 100);
        vic.Write(SpriteY0, 100);
        vic.Write(SpriteEnable, 0x01);

        AdvanceTo(vic, line: 90, extra: 0);
        AdvanceTo(vic, line: 130, extra: 0);

        byte collision = vic.Read(SpriteBackgroundCollision);
        Assert.Equal(0x01, collision);
    }

    /// <summary>
    /// FR/TR: FR-VIC-005 / TEST-VIC-001 (BACKFILL-VIDEO-001 sprite collision).
    /// Use case: $D01F read-clear semantics: after reading the latch, a
    /// subsequent read must return 0 until new collisions accumulate.
    /// Acceptance: First read returns 0x01; immediate second read returns 0x00.
    /// </summary>
    [Fact]
    public void SpriteBackground_LatchClearsOnRead()
    {
        var vic = BuildVic(bgPattern: 0xFF);
        vic.Write(SpriteX0, 100);
        vic.Write(SpriteY0, 100);
        vic.Write(SpriteEnable, 0x01);

        AdvanceTo(vic, line: 90, extra: 0);
        AdvanceTo(vic, line: 130, extra: 0);

        Assert.Equal(0x01, vic.Read(SpriteBackgroundCollision));
        Assert.Equal(0x00, vic.Read(SpriteBackgroundCollision));
    }
}
