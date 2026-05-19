namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Chips.VicIi;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: FR-VIC (BACKFILL-VIDEO-001 sprite DMA stall, follow-up to commit
/// 38b164f).
///
/// The prior sprite-DMA slice added the SpriteDmaCyclesThisFrame counter
/// but did NOT extend IsCpuCycleStolen / IsCpuCycleStealMandatory to the
/// sprite-DMA window. Today those are gated only on bad lines (cycles
/// 12-54 on bad-line scanlines). Real hardware also asserts BA during
/// the sprite-DMA cycles at the end of each scanline (cycles 55..62 plus
/// 0..7 of the next line on a 63-cycle PAL line) when any enabled sprite
/// intersects.
///
/// Simplification used here: a single wide window (RasterX &gt;= 55 OR
/// RasterX &lt; 8) is treated as the sprite-DMA window for IsCpuCycleStolen
/// whenever any enabled sprite intersects the current raster line. The
/// IsCpuCycleStealMandatory window is offset by one cycle on the leading
/// edge to mirror the existing bad-line offset (RasterX &gt;= 56 OR
/// RasterX &lt; 9). This is a coarse approximation of the real per-sprite
/// p-/s-access pairs; it is sufficient to model BA assertion at the
/// cycle-count granularity the cycle stealer cares about.
/// </summary>
public sealed class VicIISpriteDmaStallTests
{
    private const ushort SpriteY0 = 0xD001;
    private const ushort ScreenControl1 = 0xD011;
    private const ushort SpriteEnable = 0xD015;
    private const ushort SpriteYExpansion = 0xD017;

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
    /// FR/TR: FR-VIC (BACKFILL-VIDEO-001 sprite DMA stall).
    /// Use case: With $D015 = 0x00 no sprite is enabled, so sprite-DMA
    /// cycle stealing must never fire. On a non-bad-line, the cycle-steal
    /// predicate stays false across the entire scanline including the
    /// 55..62 and 0..7 end-of-line / start-of-line cycles that the
    /// sprite-DMA window normally covers.
    /// Acceptance: For every cycle 0..(CyclesPerLine-1) on a non-bad
    /// raster line (line 0x10 with DEN=1), IsCpuCycleStolen is false and
    /// IsCpuCycleStealMandatory is false. No false positives in the
    /// 55..62 or 0..7 sprite-DMA window.
    /// </summary>
    [Fact]
    public void SpriteDmaStall_NoSpritesEnabled_NoStallAnyCycle()
    {
        var vic = BuildVic();

        vic.Write(ScreenControl1, 0x10); // DEN=1, YSCROLL=0
        vic.Write(SpriteY0, 100);
        vic.Write(SpriteEnable, 0x00);

        // Pick a non-bad raster line (0x10 is well below the bad-line
        // range $30..$F7) so the bad-line predicate stays false.
        AdvanceTo(vic, 0x10, 0);

        Assert.False(vic.IsBadLine, "Line $10 must not be a bad line for this scenario.");

        for (byte c = 0; c < vic.CyclesPerLine; c++)
        {
            AdvanceTo(vic, 0x10, c);
            Assert.False(vic.IsCpuCycleStolen,
                $"No sprites enabled: IsCpuCycleStolen must be false on line $10 cycle {c}.");
            Assert.False(vic.IsCpuCycleStealMandatory,
                $"No sprites enabled: IsCpuCycleStealMandatory must be false on line $10 cycle {c}.");
        }
    }

    /// <summary>
    /// FR/TR: FR-VIC (BACKFILL-VIDEO-001 sprite DMA stall).
    /// Use case: Sprite 0 enabled at Y=0x10 intersects raster lines
    /// 0x10..0x24. On a non-bad-line within that span (line 0x10 with
    /// YSCROLL=0 -&gt; 0x10 is not a bad line because 0x10 &lt; $30),
    /// IsCpuCycleStolen must assert during the sprite-DMA window
    /// (cycles 55..62 of the line plus 0..7 of the next line wrap).
    /// Outside that window (cycles 8..54 of the non-bad line) the
    /// CPU must own the bus.
    /// Acceptance: IsCpuCycleStolen is true at cycles 55..62 on line
    /// 0x10 and at cycles 0..7 on line 0x11; false at cycles 8..54 on
    /// line 0x10 (no bad-line stall in play here).
    /// </summary>
    [Fact]
    public void SpriteDmaStall_OneSpriteIntersects_StallDuringSpriteDmaWindow()
    {
        var vic = BuildVic();

        vic.Write(ScreenControl1, 0x10); // DEN=1, YSCROLL=0
        vic.Write(SpriteY0, 0x10);
        vic.Write(SpriteEnable, 0x01);

        // Sanity: line 0x10 with YSCROLL=0 is below the $30..$F7 bad-line
        // range, so we are isolating the sprite-DMA stall path.
        AdvanceTo(vic, 0x10, 0);
        Assert.False(vic.IsBadLine);

        // Sprite-DMA window: cycles 55..62 on this line.
        for (byte c = 55; c < vic.CyclesPerLine; c++)
        {
            AdvanceTo(vic, 0x10, c);
            Assert.True(vic.IsCpuCycleStolen,
                $"Sprite-DMA stall expected at line $10 cycle {c}.");
        }

        // Cycles 0..7 on the next line (line $11) are still inside the
        // sprite-DMA window because the sprite still intersects line $11
        // (Y=$10 spans 21 lines = $10..$24).
        for (byte c = 0; c < 8; c++)
        {
            AdvanceTo(vic, 0x11, c);
            Assert.True(vic.IsCpuCycleStolen,
                $"Sprite-DMA stall expected at line $11 cycle {c} (window wraps).");
        }

        // Outside the sprite-DMA window the CPU should own the bus on a
        // non-bad-line. Probe a middle cycle.
        AdvanceTo(vic, 0x11, 30);
        Assert.False(vic.IsCpuCycleStolen,
            "Mid-line cycle 30 on non-bad-line with sprite enabled must not stall.");
    }

    /// <summary>
    /// FR/TR: FR-VIC (BACKFILL-VIDEO-001 sprite DMA stall).
    /// Use case: On a bad line that ALSO has an intersecting sprite, both
    /// stall windows fire. The bad-line window covers cycles 12..54 and
    /// the sprite-DMA window covers cycles 55..62 plus 0..7 wrap. The
    /// composition produces a near-continuous stall band across cycles
    /// 12..62 of the bad line plus 0..7 of the next line.
    /// Acceptance: With $D011=$10 (DEN=1, YSCROLL=0) and sprite 0 enabled
    /// at Y=$30 (the first bad line), IsCpuCycleStolen is true at cycle
    /// 12 (bad-line entry), at cycle 54 (bad-line exit), at cycle 55
    /// (sprite-DMA entry), at cycle 62 (last cycle of the line), and at
    /// cycle 0 of line $31 (still inside the sprite-DMA wrap).
    /// </summary>
    [Fact]
    public void SpriteDmaStall_ComposesWithBadLine_BothWindowsAssertStall()
    {
        var vic = BuildVic();

        vic.Write(ScreenControl1, 0x10); // DEN=1, YSCROLL=0
        vic.Write(SpriteY0, 0x30);
        vic.Write(SpriteEnable, 0x01);

        // Land on line $30 (the first bad line) and verify both windows.
        AdvanceTo(vic, 0x30, 12);
        Assert.True(vic.IsBadLine);
        Assert.True(vic.IsCpuCycleStolen,
            "Bad-line stall window: cycle 12 must steal.");

        AdvanceTo(vic, 0x30, 54);
        Assert.True(vic.IsCpuCycleStolen,
            "Bad-line stall window: cycle 54 must steal.");

        AdvanceTo(vic, 0x30, 55);
        // Bad-line window exits at 55; sprite-DMA window opens.
        Assert.True(vic.IsCpuCycleStolen,
            "Sprite-DMA stall window opens at cycle 55 of line $30.");

        AdvanceTo(vic, 0x30, 62);
        Assert.True(vic.IsCpuCycleStolen,
            "Sprite-DMA stall covers cycle 62 (last cycle of line $30).");

        // Wrap to the next line; sprite still intersects since Y=$30
        // spans $30..$44.
        AdvanceTo(vic, 0x31, 0);
        Assert.True(vic.IsCpuCycleStolen,
            "Sprite-DMA stall wrap: cycle 0 of line $31 still in window.");
    }

    /// <summary>
    /// FR/TR: FR-VIC (BACKFILL-VIDEO-001 sprite DMA stall).
    /// Use case: IsCpuCycleStealMandatory is the "BA has been low for 3+
    /// cycles" flavour of the cycle steal; this slice models it with a
    /// one-cycle leading-edge offset (window opens one cycle later than
    /// IsCpuCycleStolen, just like the existing bad-line semantics where
    /// IsCpuCycleStolen is RasterX 12..54 and IsCpuCycleStealMandatory is
    /// 13..55).
    /// Acceptance: With sprite 0 enabled at Y=$10 on non-bad-line $10,
    /// IsCpuCycleStealMandatory is false at cycle 55, true at cycles
    /// 56..62, true at cycles 0..8 of line $11, and false at cycle 9
    /// of line $11.
    /// </summary>
    [Fact]
    public void SpriteDmaStall_MandatoryFlag_OffsetByOneCycle()
    {
        var vic = BuildVic();

        vic.Write(ScreenControl1, 0x10); // DEN=1, YSCROLL=0
        vic.Write(SpriteY0, 0x10);
        vic.Write(SpriteEnable, 0x01);

        // Non-bad-line, sprite intersects.
        AdvanceTo(vic, 0x10, 55);
        Assert.True(vic.IsCpuCycleStolen, "Leading edge of stolen window at cycle 55.");
        Assert.False(vic.IsCpuCycleStealMandatory,
            "Mandatory flag lags by one cycle: cycle 55 is not mandatory yet.");

        AdvanceTo(vic, 0x10, 56);
        Assert.True(vic.IsCpuCycleStolen, "Cycle 56 still stolen.");
        Assert.True(vic.IsCpuCycleStealMandatory,
            "Mandatory flag asserts at cycle 56 (one cycle after the stolen window opens).");

        AdvanceTo(vic, 0x10, 62);
        Assert.True(vic.IsCpuCycleStealMandatory,
            "Mandatory flag covers last cycle of the line.");

        AdvanceTo(vic, 0x11, 0);
        Assert.True(vic.IsCpuCycleStealMandatory,
            "Mandatory flag stays asserted into cycle 0 of the next line.");

        AdvanceTo(vic, 0x11, 8);
        Assert.True(vic.IsCpuCycleStealMandatory,
            "Mandatory flag still asserted at cycle 8 of next line.");

        AdvanceTo(vic, 0x11, 9);
        Assert.False(vic.IsCpuCycleStealMandatory,
            "Mandatory flag releases at cycle 9 (one cycle after the stolen window closes).");
    }
}
