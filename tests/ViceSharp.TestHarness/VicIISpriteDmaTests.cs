namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Chips.VicIi;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: FR-VIC-006 / FR-VIC-010 / TR-CYCLE-001 / TEST-VIC-001
/// (BACKFILL-VIDEO-001 sprite DMA cycle stealing).
///
/// Sprite DMA on the VIC-II steals two CPU cycles per sprite per raster
/// line that the sprite intersects. Each sprite is 21 source rows tall
/// (42 lines if Y-expanded via $D017). Up to eight sprites can intersect
/// the same raster line for a worst case of 16 stolen cycles per line,
/// stacking with any bad-line theft (40 cycles) for a worst-case
/// 16 + 40 = 56 stolen cycles on a single line.
///
/// This suite verifies the per-frame SpriteDmaCyclesThisFrame counter:
///
/// 1. No sprite enabled - counter stays at zero.
/// 2. One sprite enabled - 21 lines * 2 cycles = 42 stolen cycles per
///    frame.
/// 3. Y-expanded sprite - 42 lines * 2 cycles = 84 stolen cycles per
///    frame.
/// 4. Eight sprites enabled, all at same Y - 8 * 42 = 336 stolen cycles
///    per frame (8 sprites * 21 rows * 2 cycles each).
/// 5. Bad-line + sprite DMA composition - both counters tick
///    independently on a line that is both a bad line and has a sprite
///    intersecting it.
/// </summary>
public sealed class VicIISpriteDmaTests
{
    private const ushort SpriteY0 = 0xD001;
    private const ushort SpriteY1 = 0xD003;
    private const ushort SpriteY2 = 0xD005;
    private const ushort SpriteY3 = 0xD007;
    private const ushort SpriteY4 = 0xD009;
    private const ushort SpriteY5 = 0xD00B;
    private const ushort SpriteY6 = 0xD00D;
    private const ushort SpriteY7 = 0xD00F;
    private const ushort ScreenControl1 = 0xD011;
    private const ushort SpriteEnable = 0xD015;
    private const ushort SpriteYExpansion = 0xD017;

    private static Mos6569 BuildVic()
    {
        var irq = new InterruptLine(InterruptType.Irq);
        return new Mos6569(new BasicBus(), irq);
    }

    /// <summary>
    /// Advance the chip through exactly one full PAL frame, ending at
    /// the last cycle of the last raster line. Counters are sampled
    /// before the frame boundary wraps (which would reset them).
    /// </summary>
    private static void AdvanceOneFullFrame(Mos6569 vic)
    {
        // Align to the frame start (line 0 cycle 1: VICE applies
        // vicii_cycle_start_of_frame at raster cycle 1 per
        // viciisc/vicii-cycle.c:453-456, so line 0 cycle 0 does not exist;
        // PLAN-VICEPARITY-001 slice V2 / TEST-VIC-CYCLE-12).
        AdvanceTo(vic, 0, 1);
        AdvanceTo(vic, (ushort)(vic.TotalLines - 1), (byte)(vic.CyclesPerLine - 1));
    }

    private static void AdvanceTo(Mos6569 vic, ushort rasterLine, byte rasterCycle)
    {
        var maxCycles = vic.TotalLines * vic.CyclesPerLine * 3;
        for (var cycle = 0; cycle < maxCycles; cycle++)
        {
            if (vic.CurrentRasterLine == rasterLine && vic.RasterX == rasterCycle)
                return;

            vic.Tick();
        }

        throw new InvalidOperationException($"VIC did not reach line ${rasterLine:X3}, cycle {rasterCycle}.");
    }

    /// <summary>
    /// FR/TR: FR-VIC-006 / FR-VIC-010 / TR-CYCLE-001 / TEST-VIC-001
    /// (BACKFILL-VIDEO-001 sprite DMA cycle stealing).
    /// Use case: With $D015 = 0x00 no sprite is enabled, so sprite DMA
    /// never triggers regardless of sprite Y positions or expansion.
    /// Acceptance: After a full PAL frame, SpriteDmaCyclesThisFrame == 0.
    /// </summary>
    [Fact]
    public void SpriteDma_NoSpriteEnabled_ZeroStolenCycles()
    {
        var vic = BuildVic();

        // Place sprites at on-screen positions but leave them all disabled.
        vic.Write(SpriteY0, 100);
        vic.Write(SpriteY1, 100);
        vic.Write(SpriteEnable, 0x00);

        AdvanceOneFullFrame(vic);

        Assert.Equal(0, vic.SpriteDmaCyclesThisFrame);
    }

    /// <summary>
    /// FR/TR: FR-VIC-006 / FR-VIC-010 / TR-CYCLE-001 / TEST-VIC-001
    /// (BACKFILL-VIDEO-001 sprite DMA cycle stealing).
    /// Use case: Enable sprite 0 at Y=100. A normal sprite occupies 21
    /// raster lines (Y=100..120). Each intersecting line steals 2 CPU
    /// cycles for the s-data fetches.
    /// Acceptance: After a full PAL frame, SpriteDmaCyclesThisFrame == 42
    /// (21 lines * 2 cycles).
    /// </summary>
    [Fact]
    public void SpriteDma_OneSpriteEnabled_StealsTwoCyclesPerLine()
    {
        var vic = BuildVic();

        vic.Write(SpriteY0, 100);
        vic.Write(SpriteEnable, 0x01);

        AdvanceOneFullFrame(vic);

        Assert.Equal(21 * 2, vic.SpriteDmaCyclesThisFrame);
    }

    /// <summary>
    /// FR/TR: FR-VIC-006 / FR-VIC-010 / TR-CYCLE-001 / TEST-VIC-001
    /// (BACKFILL-VIDEO-001 sprite DMA cycle stealing).
    /// Use case: Y-expansion ($D017 bit n = 1) doubles the vertical
    /// extent of sprite n to 42 raster lines. Each of the 42 lines
    /// still costs 2 CPU cycles.
    /// Acceptance: After a full PAL frame with sprite 0 enabled +
    /// Y-expanded at Y=100, SpriteDmaCyclesThisFrame == 84
    /// (42 lines * 2 cycles).
    /// </summary>
    [Fact]
    public void SpriteDma_YExpandedSprite_DoublesStolenCycles()
    {
        var vic = BuildVic();

        vic.Write(SpriteY0, 100);
        vic.Write(SpriteEnable, 0x01);
        vic.Write(SpriteYExpansion, 0x01);

        AdvanceOneFullFrame(vic);

        Assert.Equal(42 * 2, vic.SpriteDmaCyclesThisFrame);
    }

    /// <summary>
    /// FR/TR: FR-VIC-006 / FR-VIC-010 / TR-CYCLE-001 / TEST-VIC-001
    /// (BACKFILL-VIDEO-001 sprite DMA cycle stealing).
    /// Use case: With all 8 sprites enabled at the same Y position, each
    /// of the 21 shared intersection lines incurs 8 * 2 = 16 stolen
    /// cycles. Total across the frame: 21 * 16 = 336 stolen cycles.
    /// Acceptance: After a full PAL frame with all sprites enabled at
    /// Y=100, SpriteDmaCyclesThisFrame == 336.
    /// </summary>
    [Fact]
    public void SpriteDma_EightSpritesEnabledSameY_SixteenCyclesPerSharedLine()
    {
        var vic = BuildVic();

        vic.Write(SpriteY0, 100);
        vic.Write(SpriteY1, 100);
        vic.Write(SpriteY2, 100);
        vic.Write(SpriteY3, 100);
        vic.Write(SpriteY4, 100);
        vic.Write(SpriteY5, 100);
        vic.Write(SpriteY6, 100);
        vic.Write(SpriteY7, 100);
        vic.Write(SpriteEnable, 0xFF);

        AdvanceOneFullFrame(vic);

        Assert.Equal(21 * 8 * 2, vic.SpriteDmaCyclesThisFrame);
    }

    /// <summary>
    /// FR/TR: FR-VIC-006 / FR-VIC-010 / TR-CYCLE-001 / TEST-VIC-001
    /// (BACKFILL-VIDEO-001 sprite DMA cycle stealing).
    /// Use case: Sprite DMA and bad-line cycle theft compose
    /// independently. Place sprite 0 over the bad-line band ($30..$F7)
    /// at a Y value that overlaps at least one bad line. With DEN=1 and
    /// YSCROLL=0, bad lines occur every 8 raster lines from $30 to $F0
    /// inclusive (25 bad lines). With sprite 0 enabled at Y=$30, the
    /// sprite intersects lines $30..$44 (21 lines), three of which
    /// ($30, $38, $40) are also bad lines. The counters increment
    /// independently.
    /// Acceptance: After a full frame, BadLineCountThisFrame == 25 and
    /// SpriteDmaCyclesThisFrame == 42 (21 lines * 2 cycles). Both must
    /// be observable - the sprite cycles do not subsume the bad-line
    /// count, and vice versa.
    /// </summary>
    [Fact]
    public void SpriteDma_ComposesWithBadLineCycles()
    {
        var vic = BuildVic();

        vic.Write(ScreenControl1, 0x10); // DEN=1, YSCROLL=0
        vic.Write(SpriteY0, 0x30);
        vic.Write(SpriteEnable, 0x01);

        AdvanceOneFullFrame(vic);

        Assert.Equal(25, vic.BadLineCountThisFrame);
        Assert.Equal(21 * 2, vic.SpriteDmaCyclesThisFrame);
    }
}
