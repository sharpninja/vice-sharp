namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Chips.VicIi;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: FR-VIC-004 / TEST-VIC-001 (BACKFILL-VIDEO-001 sprite-enable + X-MSB).
///
/// Backfill register-decode coverage for two VIC-II sprite control
/// registers not directly exercised by the sprite-collision and
/// expansion suites:
///
/// - $D015 sprite enable: 8-bit bitmask, bit n = sprite n visible.
///   Full 8-bit read/write round-trip. A disabled sprite must not
///   contribute to collision latches (regression test for the
///   collision gating in <c>ProcessSpriteCollisionsForRasterLine</c>).
///
/// - $D010 sprite X-MSB: 8-bit bitmask, bit n extends sprite n's
///   X coordinate from 8-bit ($D000/$D002/.../$D00E low byte) to a
///   9-bit value. Round-trip plus the cross-register composition:
///   $D000 = 0, $D010 bit 0 = 1 -> sprite 0 effective X = 256. Bit
///   routing must not bleed into sprite Y (the odd-offset Y bytes
///   live in $D001/$D003/...).
/// </summary>
public sealed class VicIISpriteRegisterTests
{
    private const ushort SpriteX0 = 0xD000;
    private const ushort SpriteY0 = 0xD001;
    private const ushort SpriteX1 = 0xD002;
    private const ushort SpriteY1 = 0xD003;
    private const ushort SpriteXMsb = 0xD010;
    private const ushort ScreenControl1 = 0xD011;
    private const ushort SpriteEnable = 0xD015;
    private const ushort ScreenControl2 = 0xD016;
    private const ushort SpriteSpriteCollision = 0xD01E;

    private const int CyclesPerLine = Mos6569.PalCyclesPerLine;

    /// <summary>
    /// Build a VIC-II with a memory reader that returns fully opaque
    /// sprite data (0xFF) and a transparent background. Mirrors the
    /// helper in <see cref="SpriteCollisionTests"/> but kept local to
    /// avoid cross-test coupling.
    /// </summary>
    private static Mos6569 BuildVic(byte spriteDataBlock = 0x0D)
    {
        var vic = new Mos6569(new BasicBus(), new InterruptLine(InterruptType.Irq));
        vic.VideoMemoryReader = addr =>
        {
            ushort masked = (ushort)(addr & 0x3FFF);

            // Sprite data pointers live at ScreenMemoryBase + $03F8..$03FF.
            // Screen base is 0 (register $18 = 0) so pointers sit at $03F8-$03FF.
            if (masked >= 0x03F8 && masked <= 0x03FF)
            {
                return spriteDataBlock;
            }

            // Sprite data block = pointer * 64. Each row is 3 bytes.
            ushort spriteBase = (ushort)(spriteDataBlock * 64);
            if (masked >= spriteBase && masked < spriteBase + 64)
            {
                return 0xFF; // Fully opaque sprite row.
            }

            return 0x00; // Transparent background (no foreground bits).
        };
        // DEN=1, RSEL=1, YSCROLL=3 (matches existing sprite test config).
        vic.Write(ScreenControl1, 0x1B);
        // CSEL=1 (40 cols).
        vic.Write(ScreenControl2, 0x08);
        return vic;
    }

    /// <summary>
    /// Advance VIC ticks until the given raster line is reached at
    /// cycle 0, then step <paramref name="extra"/> more cycles.
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
    /// FR/TR: FR-VIC-004 / TEST-VIC-001 (BACKFILL-VIDEO-001 sprite-enable + X-MSB).
    /// Use case: $D015 holds the 8-bit sprite-enable bitmask with no
    /// stuck or floating bits; write/read round-trip must be bit-exact
    /// for the full byte (distinct from $D016 / $D018 which have
    /// floating-high upper bits).
    /// Acceptance: After Write($D015, 0x55) the next Read returns 0x55.
    /// </summary>
    [Fact]
    public void SpriteEnable_RoundTrip_PreservesAllEightBits()
    {
        var vic = BuildVic();

        vic.Write(SpriteEnable, 0x55);

        Assert.Equal(0x55, vic.Read(SpriteEnable));
    }

    /// <summary>
    /// FR/TR: FR-VIC-004 / TEST-VIC-001 (BACKFILL-VIDEO-001 sprite-enable + X-MSB).
    /// Use case: A sprite whose enable bit in $D015 is 0 must not
    /// contribute to the sprite-sprite collision latch even when its
    /// X/Y coordinates would overlap another sprite. Regression guard
    /// for the gating in <c>ProcessSpriteCollisionsForRasterLine</c>:
    /// disabled sprites are skipped before the opacity mask is built.
    /// Acceptance: With sprites 0 and 1 placed at identical coords but
    /// both disabled ($D015 = 0x00), reading $D01E after a full sprite
    /// raster pass returns 0x00.
    /// </summary>
    [Fact]
    public void SpriteEnable_DisabledSprites_ProduceNoCollision()
    {
        var vic = BuildVic();

        // Identical placement that would collide if both were enabled.
        vic.Write(SpriteX0, 100);
        vic.Write(SpriteY0, 100);
        vic.Write(SpriteX1, 100);
        vic.Write(SpriteY1, 100);
        // Both sprites DISABLED.
        vic.Write(SpriteEnable, 0x00);

        AdvanceTo(vic, line: 90, extra: 0);
        AdvanceTo(vic, line: 130, extra: 0);

        byte collision = vic.Read(SpriteSpriteCollision);
        Assert.Equal(0x00, collision);
    }

    /// <summary>
    /// FR/TR: FR-VIC-004 / TEST-VIC-001 (BACKFILL-VIDEO-001 sprite-enable + X-MSB).
    /// Use case: $D010 bit n is the 9th (high) bit of sprite n's
    /// X coordinate, extending the 8-bit $D000/$D002/... low byte
    /// into a 9-bit value covering 0..511. Setting $D010 bit 0 = 1
    /// with $D000 = 0 must yield an effective X coord of 256 for
    /// sprite 0 (queried via GetSpriteX which already returns the
    /// composed 9-bit value).
    /// Acceptance: After Write($D000, 0x00) + Write($D010, 0x01),
    /// GetSpriteX(0) == 256.
    /// </summary>
    [Fact]
    public void SpriteXMsb_ExtendsXCoordinatePastByteRange()
    {
        var vic = BuildVic();

        vic.Write(SpriteX0, 0x00);
        vic.Write(SpriteXMsb, 0x01); // bit 0 set -> sprite 0 high X bit.

        Assert.Equal(256, vic.GetSpriteX(0));
    }

    /// <summary>
    /// FR/TR: FR-VIC-004 / TEST-VIC-001 (BACKFILL-VIDEO-001 sprite-enable + X-MSB).
    /// Use case: $D010 is a plain 8-bit register (no floating-high
    /// upper bits, no shared semantics with neighbouring registers).
    /// Round-trip must be bit-exact.
    /// Acceptance: After Write($D010, 0x05) the next Read returns 0x05.
    /// </summary>
    [Fact]
    public void SpriteXMsb_RoundTrip_PreservesValue()
    {
        var vic = BuildVic();

        vic.Write(SpriteXMsb, 0x05);

        Assert.Equal(0x05, vic.Read(SpriteXMsb));
    }

    /// <summary>
    /// FR/TR: FR-VIC-004 / TEST-VIC-001 (BACKFILL-VIDEO-001 sprite-enable + X-MSB).
    /// Use case: $D010 routes ONLY to the X-coordinate MSB and must
    /// not touch the Y position (odd offsets $D001/$D003/... carry Y).
    /// Toggling $D010 between writes to $D001 must leave sprite 0's
    /// Y position untouched.
    /// Acceptance: After Write($D001, 0x80), Write($D010, 0xFF),
    /// Write($D010, 0x00) the value returned by GetSpriteY(0) is
    /// still 0x80.
    /// </summary>
    [Fact]
    public void SpriteXMsb_DoesNotAffectSpriteYPosition()
    {
        var vic = BuildVic();

        vic.Write(SpriteY0, 0x80);
        vic.Write(SpriteXMsb, 0xFF); // set every X-MSB bit.
        vic.Write(SpriteXMsb, 0x00); // clear every X-MSB bit.

        Assert.Equal(0x80, vic.GetSpriteY(0));
    }
}
