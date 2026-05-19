namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Chips.VicIi;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: FR-VIC (BACKFILL-VIDEO-001 sprite Y-expansion + multicolor).
/// Use case: VIC-II sprite features Y-expansion ($D017 bit n), multicolor
/// ($D01C bit n), and X-expansion ($D01D bit n) are per-sprite bitmasks
/// that affect the sprite raster's vertical reach, pixel pair semantics,
/// and horizontal reach respectively. Collision detection ($D01E /
/// $D01F) must honour these flags so that:
/// - Y-expansion doubles the vertical reach (21 source rows -> 42 displayed rows).
/// - Multicolor treats the 24-bit row as 12 2-bit pairs with pair == 00 transparent.
/// - X-expansion doubles horizontal extent (24 -> 48 pixels). Pairs in
///   multicolor + X-expansion produce 4-pixel pair groups (not single bits doubled).
/// Acceptance: Tests below assert collision register state matches the
/// pair-semantic and expansion rules described in FR-VIC.
/// </summary>
public sealed class SpriteYExpansionMulticolorTests
{
    private const ushort SpriteX0 = 0xD000;
    private const ushort SpriteY0 = 0xD001;
    private const ushort SpriteX1 = 0xD002;
    private const ushort SpriteY1 = 0xD003;
    private const ushort SpriteEnable = 0xD015;
    private const ushort ScreenControl1 = 0xD011;
    private const ushort ScreenControl2 = 0xD016;
    private const ushort SpriteYExpansion = 0xD017;
    private const ushort SpriteMulticolor = 0xD01C;
    private const ushort SpriteXExpansion = 0xD01D;
    private const ushort SpriteSpriteCollision = 0xD01E;
    private const ushort SpriteBackgroundCollision = 0xD01F;

    private const int CyclesPerLine = Mos6569.PalCyclesPerLine;

    /// <summary>
    /// Build a VIC-II with a pluggable per-sprite data fetcher. The
    /// fetcher receives the byte index inside the sprite (0..62) and the
    /// sprite number, and returns the byte to feed into the collision raster.
    /// </summary>
    private static Mos6569 BuildVic(
        Func<int, int, byte>? spriteByteSupplier = null,
        byte spriteDataBlock = 0x0D,
        byte bgPattern = 0x00)
    {
        spriteByteSupplier ??= (_, _) => 0xFF;
        var vic = new Mos6569(new BasicBus(), new InterruptLine(InterruptType.Irq));
        vic.VideoMemoryReader = addr =>
        {
            ushort masked = (ushort)(addr & 0x3FFF);

            // Sprite data pointers live at ScreenMemoryBase + $03F8..$03FF.
            if (masked >= 0x03F8 && masked <= 0x03FF)
            {
                return spriteDataBlock;
            }

            // Sprite data block = pointer * 64. Each sprite row is 3 bytes.
            ushort spriteBase = (ushort)(spriteDataBlock * 64);
            if (masked >= spriteBase && masked < spriteBase + 64)
            {
                int byteOffset = masked - spriteBase;
                // We don't have a per-sprite block here so the supplier receives
                // (byteOffset, -1) when called from the unified block. Tests that
                // need per-sprite patterns should override on the sprite-data
                // pointer path (see PerSprite builder).
                return spriteByteSupplier(byteOffset, -1);
            }

            return bgPattern;
        };
        // DEN=1, RSEL=1, YSCROLL=3 (standard test config).
        vic.Write(ScreenControl1, 0x1B);
        // CSEL=1 (40 cols)
        vic.Write(ScreenControl2, 0x08);
        return vic;
    }

    /// <summary>
    /// Build a VIC-II where each sprite has its own data block and each
    /// sprite's raw row bytes are supplied by <paramref name="perSpriteRowSupplier"/>
    /// invoked as (spriteNum, rowIndex 0..20, byteInRow 0..2).
    /// </summary>
    private static Mos6569 BuildVicPerSprite(
        Func<int, int, int, byte> perSpriteRowSupplier,
        byte bgPattern = 0x00)
    {
        var vic = new Mos6569(new BasicBus(), new InterruptLine(InterruptType.Irq));
        // Per-sprite data blocks: sprite n -> block (0x20 + n). Each block
        // is at byte address (0x20 + n) * 64.
        const byte BasePointer = 0x20;
        vic.VideoMemoryReader = addr =>
        {
            ushort masked = (ushort)(addr & 0x3FFF);

            // Sprite pointers $03F8..$03FF: pointer for sprite n returns BasePointer+n.
            if (masked >= 0x03F8 && masked <= 0x03FF)
            {
                int n = masked - 0x03F8;
                return (byte)(BasePointer + n);
            }

            // Look up which sprite this address belongs to.
            for (int n = 0; n < 8; n++)
            {
                ushort spriteBase = (ushort)((BasePointer + n) * 64);
                if (masked >= spriteBase && masked < spriteBase + 64)
                {
                    int byteOffset = masked - spriteBase;
                    int row = byteOffset / 3;
                    int byteInRow = byteOffset % 3;
                    if (row >= 21)
                    {
                        return 0;
                    }
                    return perSpriteRowSupplier(n, row, byteInRow);
                }
            }

            return bgPattern;
        };
        vic.Write(ScreenControl1, 0x1B);
        vic.Write(ScreenControl2, 0x08);
        return vic;
    }

    /// <summary>
    /// Advance VIC until the given raster line is reached at cycle 0,
    /// then step <paramref name="extra"/> more cycles.
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
    /// FR/TR: FR-VIC (BACKFILL-VIDEO-001 sprite Y-expansion + multicolor).
    /// Use case: With sprite 0 at Y=100 (21 rows -> ends row 120) and sprite 1
    /// at Y=125 (21 rows -> 125..145), no Y-expansion = no overlap. Enabling
    /// Y-expansion on sprite 0 ($D017 bit 0 = 1) doubles its reach to rows
    /// 100..141 which now overlaps sprite 1. The collision latch must reflect
    /// this difference.
    /// Acceptance: $D01E reads 0x00 without Y-expansion, 0x03 with Y-expansion.
    /// </summary>
    [Fact]
    public void YExpansion_DoublesVerticalReach_AffectsCollision()
    {
        // First pass: no Y-expansion - sprites do not overlap.
        var vicNo = BuildVic();
        vicNo.Write(SpriteX0, 100);
        vicNo.Write(SpriteY0, 100);
        vicNo.Write(SpriteX1, 100);
        vicNo.Write(SpriteY1, 125);
        vicNo.Write(SpriteEnable, 0x03);
        AdvanceTo(vicNo, line: 90, extra: 0);
        AdvanceTo(vicNo, line: 160, extra: 0);
        Assert.Equal(0x00, vicNo.Read(SpriteSpriteCollision));

        // Second pass: Y-expansion on sprite 0 - now reaches row 141, overlapping sprite 1.
        var vicYes = BuildVic();
        vicYes.Write(SpriteX0, 100);
        vicYes.Write(SpriteY0, 100);
        vicYes.Write(SpriteX1, 100);
        vicYes.Write(SpriteY1, 125);
        vicYes.Write(SpriteEnable, 0x03);
        vicYes.Write(SpriteYExpansion, 0x01); // sprite 0 Y-expanded
        AdvanceTo(vicYes, line: 90, extra: 0);
        AdvanceTo(vicYes, line: 160, extra: 0);
        Assert.Equal(0x03, vicYes.Read(SpriteSpriteCollision));
    }

    /// <summary>
    /// FR/TR: FR-VIC (BACKFILL-VIDEO-001 sprite Y-expansion + multicolor).
    /// Use case: Sprite 0 in multicolor mode ($D01C bit 0 = 1) with first-row
    /// bytes (b0,b1,b2) = (0x40, 0x00, 0x00). In binary that is
    /// 0100 0000 0000 0000 0000 0000 - twelve 2-bit pairs:
    ///   pair0=01, pair1=00, ..., pair11=00.
    /// Only pair 0 (pixels 0..1 of the sprite) is opaque. A second non-multicolor
    /// sprite 1 placed so it ONLY overlaps the pair-0 column range must
    /// produce a collision; if moved to a pair-1 (transparent) column it must NOT.
    /// Acceptance: With sprite 1 X overlapping pair 0 -> $D01E = 0x03;
    /// with sprite 1 X overlapping ONLY pair 1 (transparent) -> $D01E = 0x00.
    /// </summary>
    [Fact]
    public void Multicolor_PairOpacitySemantics_Sprite1AlignedWithPair()
    {
        // Sprite 0: only pair-0 (top-left 2 source pixels, i.e. VIC pixels 0..1) is opaque.
        // Sprite 1: a single-bit non-multicolor pattern that only sets bit 7 of byte 0
        // (pixel 0 only). Place sprite 1 X so its pixel 0 lands on sprite 0's pair-0.
        Func<int, int, int, byte> supplier = (n, row, byteInRow) =>
        {
            if (n == 0 && byteInRow == 0)
            {
                return 0x40; // pair-0 = 01 (opaque)
            }
            if (n == 1 && byteInRow == 0)
            {
                return 0x80; // sprite 1: bit 7 only -> 1 opaque pixel at sprite-x +0
            }
            return 0;
        };

        // Case A: sprite 1 X aligned with sprite 0 (so sprite-1 pixel 0 hits sprite-0 pair 0).
        var vicHit = BuildVicPerSprite(supplier);
        vicHit.Write(SpriteX0, 100);
        vicHit.Write(SpriteY0, 100);
        vicHit.Write(SpriteX1, 100);
        vicHit.Write(SpriteY1, 100);
        vicHit.Write(SpriteMulticolor, 0x01); // sprite 0 multicolor
        vicHit.Write(SpriteEnable, 0x03);
        AdvanceTo(vicHit, line: 90, extra: 0);
        AdvanceTo(vicHit, line: 130, extra: 0);
        Assert.Equal(0x03, vicHit.Read(SpriteSpriteCollision));

        // Case B: shift sprite 1 right so its pixel 0 lands on sprite 0's pair-1 (pixels 2..3),
        // which is transparent (00). No collision should occur.
        var vicMiss = BuildVicPerSprite(supplier);
        vicMiss.Write(SpriteX0, 100);
        vicMiss.Write(SpriteY0, 100);
        vicMiss.Write(SpriteX1, 102); // sprite 0's pixel 2 = pair-1 position (transparent)
        vicMiss.Write(SpriteY1, 100);
        vicMiss.Write(SpriteMulticolor, 0x01);
        vicMiss.Write(SpriteEnable, 0x03);
        AdvanceTo(vicMiss, line: 90, extra: 0);
        AdvanceTo(vicMiss, line: 130, extra: 0);
        Assert.Equal(0x00, vicMiss.Read(SpriteSpriteCollision));
    }

    /// <summary>
    /// FR/TR: FR-VIC (BACKFILL-VIDEO-001 sprite Y-expansion + multicolor).
    /// Use case: Multicolor sprite over a foreground-filled background must
    /// only register sprite-bg collision when the multicolor pair is non-zero.
    /// Using sprite-0 row bytes (0x40, 0x00, 0x00) only pair-0 is opaque, so
    /// even though every background pixel is foreground (bgPattern=0xFF), bit 0
    /// of $D01F must be set (any opaque pixel of sprite n over any foreground
    /// pixel sets bit n).
    /// Acceptance: $D01F reads 0x01 after sprite 0 passes over foreground bg.
    /// With an all-transparent sprite (every pair = 00) $D01F reads 0x00.
    /// </summary>
    [Fact]
    public void Multicolor_SpriteBackgroundCollision_RespectsPairOpacity()
    {
        // Opaque case: pair-0 opaque, rest transparent. Over a 0xFF bg, bit 0 should latch.
        Func<int, int, int, byte> opaque = (n, row, byteInRow) =>
            (n == 0 && byteInRow == 0) ? (byte)0x40 : (byte)0x00;
        var vicHit = BuildVicPerSprite(opaque, bgPattern: 0xFF);
        vicHit.Write(SpriteX0, 100);
        vicHit.Write(SpriteY0, 100);
        vicHit.Write(SpriteMulticolor, 0x01);
        vicHit.Write(SpriteEnable, 0x01);
        AdvanceTo(vicHit, line: 90, extra: 0);
        AdvanceTo(vicHit, line: 130, extra: 0);
        Assert.Equal(0x01, vicHit.Read(SpriteBackgroundCollision));

        // Transparent case: every byte 0x00 -> every pair = 00 -> no bg collision.
        Func<int, int, int, byte> transparent = (n, row, byteInRow) => 0x00;
        var vicMiss = BuildVicPerSprite(transparent, bgPattern: 0xFF);
        vicMiss.Write(SpriteX0, 100);
        vicMiss.Write(SpriteY0, 100);
        vicMiss.Write(SpriteMulticolor, 0x01);
        vicMiss.Write(SpriteEnable, 0x01);
        AdvanceTo(vicMiss, line: 90, extra: 0);
        AdvanceTo(vicMiss, line: 130, extra: 0);
        Assert.Equal(0x00, vicMiss.Read(SpriteBackgroundCollision));
    }

    /// <summary>
    /// FR/TR: FR-VIC (BACKFILL-VIDEO-001 sprite Y-expansion + multicolor).
    /// Use case: Y-expansion combined with multicolor. Sprite 0 is Y-expanded
    /// AND multicolor with the same first-row pattern (pair-0 opaque only).
    /// Sprite 1 is a single-pixel non-multicolor sprite placed at the
    /// expanded-vertical-reach band that would only be reachable WITH expansion.
    /// Sprite 0 at Y=100 expanded covers rows 100..141. Sprite 1 at Y=130 (within
    /// expanded reach but outside 21-row reach). X aligned to pair-0.
    /// Acceptance: $D01E = 0x03 with Y-expansion + multicolor + pair-0 alignment.
    /// </summary>
    [Fact]
    public void Multicolor_PlusYExpansion_StillCollidesAtExtendedReach()
    {
        Func<int, int, int, byte> supplier = (n, row, byteInRow) =>
        {
            // Sprite 0: multicolor pair-0 opaque on EVERY source row.
            if (n == 0 && byteInRow == 0)
            {
                return 0x40;
            }
            // Sprite 1: bit-7 only on row 0.
            if (n == 1 && byteInRow == 0 && row == 0)
            {
                return 0x80;
            }
            return 0;
        };

        var vic = BuildVicPerSprite(supplier);
        vic.Write(SpriteX0, 100);
        vic.Write(SpriteY0, 100);
        vic.Write(SpriteX1, 100);
        // Sprite 1 at Y=130 - within Y-expanded reach (100..141) but outside 21-row reach (100..120).
        vic.Write(SpriteY1, 130);
        vic.Write(SpriteMulticolor, 0x01);
        vic.Write(SpriteYExpansion, 0x01);
        vic.Write(SpriteEnable, 0x03);
        AdvanceTo(vic, line: 90, extra: 0);
        AdvanceTo(vic, line: 160, extra: 0);
        Assert.Equal(0x03, vic.Read(SpriteSpriteCollision));
    }

    /// <summary>
    /// FR/TR: FR-VIC (BACKFILL-VIDEO-001 sprite Y-expansion + multicolor).
    /// Use case: X-expansion + multicolor. Each PAIR doubles to 4 pixels
    /// (not each bit doubled). With multicolor + X-expansion, sprite 0's
    /// first row (0x40, 0x00, 0x00) has pair-0 = 01 (opaque) and pair-1..11 = 00,
    /// producing 4 opaque VIC pixels (at sprite_x..sprite_x+3) followed by all
    /// transparent. Place sprite 1 (non-multicolor, bit-7-only) at offsets that
    /// land inside the 4-pixel pair-0 block vs. just outside it.
    /// Acceptance: Sprite 1 at sprite_x +3 still hits pair-0 (collision = 0x03);
    /// at sprite_x +4 lands in pair-1 region (transparent) so no collision.
    /// </summary>
    [Fact]
    public void XExpansion_PlusMulticolor_PairsAreFourPixelsWide()
    {
        Func<int, int, int, byte> supplier = (n, row, byteInRow) =>
        {
            if (n == 0 && byteInRow == 0)
            {
                return 0x40; // pair-0 = 01 opaque
            }
            if (n == 1 && byteInRow == 0)
            {
                return 0x80; // bit-7 single pixel
            }
            return 0;
        };

        // Hit case: sprite 1 at sprite_x+3 lands inside the 4-pixel pair-0 block.
        var vicHit = BuildVicPerSprite(supplier);
        vicHit.Write(SpriteX0, 100);
        vicHit.Write(SpriteY0, 100);
        vicHit.Write(SpriteX1, 103); // inside the 4-pixel pair-0 (100..103)
        vicHit.Write(SpriteY1, 100);
        vicHit.Write(SpriteMulticolor, 0x01); // sprite 0 multicolor
        vicHit.Write(SpriteXExpansion, 0x01); // sprite 0 X-expanded
        vicHit.Write(SpriteEnable, 0x03);
        AdvanceTo(vicHit, line: 90, extra: 0);
        AdvanceTo(vicHit, line: 130, extra: 0);
        Assert.Equal(0x03, vicHit.Read(SpriteSpriteCollision));

        // Miss case: sprite 1 at sprite_x+4 lands in pair-1 region (transparent).
        var vicMiss = BuildVicPerSprite(supplier);
        vicMiss.Write(SpriteX0, 100);
        vicMiss.Write(SpriteY0, 100);
        vicMiss.Write(SpriteX1, 104); // pair-1 region of X-expanded sprite 0 (104..107)
        vicMiss.Write(SpriteY1, 100);
        vicMiss.Write(SpriteMulticolor, 0x01);
        vicMiss.Write(SpriteXExpansion, 0x01);
        vicMiss.Write(SpriteEnable, 0x03);
        AdvanceTo(vicMiss, line: 90, extra: 0);
        AdvanceTo(vicMiss, line: 130, extra: 0);
        Assert.Equal(0x00, vicMiss.Read(SpriteSpriteCollision));
    }
}
