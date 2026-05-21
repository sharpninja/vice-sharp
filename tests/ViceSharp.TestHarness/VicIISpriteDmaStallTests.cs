namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Chips.VicIi;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: FR-VIC-006 / FR-VIC-010 / TR-CYCLE-001 / TEST-VIC-001
/// (BACKFILL-VIDEO-001 sprite DMA stall, follow-up to commit 38b164f).
///
/// The prior sprite-DMA slice added the SpriteDmaCyclesThisFrame counter
/// but did NOT extend IsCpuCycleStolen / IsCpuCycleStealMandatory to the
/// sprite-DMA window. These tests pin the PAL x64sc table-driven BA mask:
/// sprite 0 asserts on cycles 54..58, sprite 2 ends at cycle 62, and
/// sprite 3 owns the early-line wrap after the VICE cycle 55/56
/// sprite_dma latch.
/// </summary>
public sealed class VicIISpriteDmaStallTests
{
    private const ushort SpriteY0 = 0xD001;
    private const ushort SpriteY2 = 0xD005;
    private const ushort SpriteY3 = 0xD007;
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
    /// FR/TR: FR-VIC-006 / FR-VIC-010 / TR-CYCLE-001 / TEST-VIC-001
    /// (BACKFILL-VIDEO-001 sprite DMA stall).
    /// Use case: With $D015 = 0x00 no sprite is enabled, so sprite-DMA
    /// cycle stealing must never fire. On a non-bad-line, the cycle-steal
    /// predicate stays false across the entire scanline including the
    /// PAL sprite BA slots normally cover.
    /// Acceptance: For every cycle 0..(CyclesPerLine-1) on a non-bad
    /// raster line (line 0x10 with DEN=1), IsCpuCycleStolen is false and
    /// IsCpuCycleStealMandatory is false. No false positives in the
    /// sprite-DMA BA slots.
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
    /// FR/TR: FR-VIC-006 / FR-VIC-010 / TR-CYCLE-001 / TEST-VIC-001
    /// (BACKFILL-VIDEO-001 sprite DMA stall).
    /// Use case: Sprite 0 enabled at Y=0x10 intersects raster lines
    /// 0x10..0x24. On a non-bad-line within that span (line 0x10 with
    /// YSCROLL=0 -&gt; 0x10 is not a bad line because 0x10 &lt; $30),
    /// IsCpuCycleStolen must assert during sprite 0's PAL BA mask,
    /// cycles 54..58. Outside that per-sprite mask the CPU owns the bus.
    /// Acceptance: IsCpuCycleStolen is true at cycles 54..58 on line
    /// 0x10; false at cycle 59 on line 0x10 and cycle 0 on line 0x11.
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

        // Sprite 0 PAL BA mask: cycles 54..58 on this line.
        for (byte c = 54; c <= 58; c++)
        {
            AdvanceTo(vic, 0x10, c);
            Assert.True(vic.IsCpuCycleStolen,
                $"Sprite-DMA stall expected at line $10 cycle {c}.");
        }

        AdvanceTo(vic, 0x10, 59);
        Assert.False(vic.IsCpuCycleStolen,
            "Sprite 0 PAL BA mask must release at line $10 cycle 59.");

        AdvanceTo(vic, 0x11, 0);
        Assert.False(vic.IsCpuCycleStolen,
            "Sprite 0 PAL BA mask does not wrap into next-line cycle 0.");

        // Outside the sprite-DMA window the CPU should own the bus on a
        // non-bad-line. Probe a middle cycle.
        AdvanceTo(vic, 0x11, 30);
        Assert.False(vic.IsCpuCycleStolen,
            "Mid-line cycle 30 on non-bad-line with sprite enabled must not stall.");
    }

    /// <summary>
    /// FR/TR: FR-VIC-006 / FR-VIC-010 / TR-CYCLE-001 / TEST-VIC-001
    /// (BACKFILL-VIDEO-001 sprite DMA stall).
    /// Use case: Sprite 2's PAL x64sc p-/s-access pair occurs at VICE
    /// table cycles 62/63, which map to vice-sharp cycles 61/62.
    /// Acceptance: IsCpuCycleStolen is true at cycles 58..62 on line
    /// 0x10; false at line $10 cycle 57 and line $11 cycle 0.
    /// </summary>
    [Fact]
    public void SpriteDmaStall_Sprite2EndsAtLineEnd()
    {
        var vic = BuildVic();

        vic.Write(ScreenControl1, 0x10); // DEN=1, YSCROLL=0
        vic.Write(SpriteY2, 0x10);
        vic.Write(SpriteEnable, 0x04);

        AdvanceTo(vic, 0x10, 57);
        Assert.False(vic.IsCpuCycleStolen,
            "Sprite 2 PAL BA mask must not start before cycle 58.");

        for (byte c = 58; c < vic.CyclesPerLine; c++)
        {
            AdvanceTo(vic, 0x10, c);
            Assert.True(vic.IsCpuCycleStolen,
                $"Sprite 2 PAL BA mask expected at line $10 cycle {c}.");
        }

        AdvanceTo(vic, 0x11, 0);
        Assert.False(vic.IsCpuCycleStolen,
            "Sprite 2 PAL BA mask releases before next-line cycle 0.");
    }

    /// <summary>
    /// FR/TR: FR-VIC-006 / FR-VIC-010 / TR-CYCLE-001 / TEST-VIC-001
    /// (BACKFILL-VIDEO-001 sprite DMA stall).
    /// Use case: Sprite 3's PAL x64sc access pair occurs at VICE table
    /// cycles 1/2, which map to vice-sharp cycles 0/1. VICE latches
    /// sprite_dma at public cycles 55/56 (vice-sharp 54/55), so sprite
    /// 3's BA lead starts later on that same line and continues through
    /// cycles 0..1 of the following line.
    /// Acceptance: IsCpuCycleStolen is false at line $10 cycle 59,
    /// true at line $10 cycles 60..62 and line $11 cycles 0..1, then
    /// false at line $11 cycle 2.
    /// </summary>
    [Fact]
    public void SpriteDmaStall_Sprite3UsesEarlyLineTableSlot()
    {
        var vic = BuildVic();

        vic.Write(ScreenControl1, 0x10); // DEN=1, YSCROLL=0
        vic.Write(SpriteY3, 0x10);
        vic.Write(SpriteEnable, 0x08);

        AdvanceTo(vic, 0x10, 59);
        Assert.False(vic.IsCpuCycleStolen,
            "Sprite 3 PAL BA mask must not start before cycle 60 after the DMA latch.");

        AdvanceTo(vic, 0x10, 60);
        Assert.True(vic.IsCpuCycleStolen,
            "Sprite 3 PAL BA mask starts three cycles before its following-line access slot.");

        AdvanceTo(vic, 0x10, 61);
        Assert.True(vic.IsCpuCycleStolen,
            "Sprite 3 PAL BA mask covers the next-to-last cycle before its following-line access slot.");

        AdvanceTo(vic, 0x10, 62);
        Assert.True(vic.IsCpuCycleStolen,
            "Sprite 3 PAL BA mask covers the last cycle before its following-line access slot.");

        for (byte c = 0; c <= 1; c++)
        {
            AdvanceTo(vic, 0x11, c);
            Assert.True(vic.IsCpuCycleStolen,
                $"Sprite 3 PAL BA mask expected at line $11 cycle {c}.");
        }

        AdvanceTo(vic, 0x11, 2);
        Assert.False(vic.IsCpuCycleStolen,
            "Sprite 3 PAL BA mask releases after cycle 1.");
    }

    /// <summary>
    /// FR/TR: FR-VIC-006 / FR-VIC-010 / TR-CYCLE-001 / TEST-VIC-001
    /// (BACKFILL-VIDEO-001 sprite DMA stall).
    /// Use case: VICE samples $D015 during the PAL public 55/56
    /// sprite-DMA checks (vice-sharp cycles 54/55). Clearing sprite 0's
    /// enable bit before those checks prevents sprite_dma from latching.
    /// Acceptance: After clearing $D015 at line $10 cycle 53, sprite 0's
    /// would-be cycles 54..58 are not stolen.
    /// </summary>
    [Fact]
    public void SpriteDmaStall_DisableBeforeCheck_PreventsDmaWindow()
    {
        var vic = BuildVic();

        vic.Write(ScreenControl1, 0x10); // DEN=1, YSCROLL=0
        vic.Write(SpriteY0, 0x10);
        vic.Write(SpriteEnable, 0x01);

        AdvanceTo(vic, 0x10, 53);
        vic.Write(SpriteEnable, 0x00);

        for (byte c = 54; c <= 58; c++)
        {
            AdvanceTo(vic, 0x10, c);
            Assert.False(vic.IsCpuCycleStolen,
                $"Clearing $D015 before sprite-DMA check must prevent line $10 cycle {c} from stealing.");
        }
    }

    /// <summary>
    /// FR/TR: FR-VIC-006 / FR-VIC-010 / TR-CYCLE-001 / TEST-VIC-001
    /// (BACKFILL-VIDEO-001 sprite DMA stall).
    /// Use case: Once VICE has latched sprite_dma, a later $D015 clear
    /// does not cancel BA/data DMA for the already-active sprite.
    /// Acceptance: Clearing $D015 after reaching line $10 cycle 54 keeps
    /// sprite 0's remaining cycles 54..58 stolen and releases at cycle 59.
    /// </summary>
    [Fact]
    public void SpriteDmaStall_DisableAfterCheck_DoesNotCancelLatchedWindow()
    {
        var vic = BuildVic();

        vic.Write(ScreenControl1, 0x10); // DEN=1, YSCROLL=0
        vic.Write(SpriteY0, 0x10);
        vic.Write(SpriteEnable, 0x01);

        AdvanceTo(vic, 0x10, 54);
        vic.Write(SpriteEnable, 0x00);

        for (byte c = 54; c <= 58; c++)
        {
            AdvanceTo(vic, 0x10, c);
            Assert.True(vic.IsCpuCycleStolen,
                $"Latched sprite-DMA window must survive $D015 clear at line $10 cycle {c}.");
        }

        AdvanceTo(vic, 0x10, 59);
        Assert.False(vic.IsCpuCycleStolen,
            "Sprite 0 PAL BA mask must still release at line $10 cycle 59.");
    }

    /// <summary>
    /// FR/TR: FR-VIC-006 / FR-VIC-010 / TR-CYCLE-001 / TEST-VIC-001
    /// (BACKFILL-VIDEO-001 sprite DMA stall).
    /// Use case: Re-enabling a sprite and writing a Y value before the
    /// PAL public 55/56 checks lets VICE latch sprite_dma. For sprites
    /// 3-7, the fetch slot is at the start of the following line, with
    /// the BA lead beginning three cycles earlier.
    /// Acceptance: Enabling sprite 3 at line $10 cycle 53 with Y=$10
    /// steals line $10 cycles 60..62 and line $11 cycles 0..1.
    /// </summary>
    [Fact]
    public void SpriteDmaStall_ReenableBeforeCheck_TriggersFollowingLineEarlySlot()
    {
        var vic = BuildVic();

        vic.Write(ScreenControl1, 0x10); // DEN=1, YSCROLL=0
        vic.Write(SpriteY3, 0x00);
        vic.Write(SpriteEnable, 0x00);

        AdvanceTo(vic, 0x10, 53);
        vic.Write(SpriteY3, 0x10);
        vic.Write(SpriteEnable, 0x08);

        for (byte c = 60; c < vic.CyclesPerLine; c++)
        {
            AdvanceTo(vic, 0x10, c);
            Assert.True(vic.IsCpuCycleStolen,
                $"Sprite 3 BA lead expected at line $10 cycle {c} after pre-check enable.");
        }

        for (byte c = 0; c <= 1; c++)
        {
            AdvanceTo(vic, 0x11, c);
            Assert.True(vic.IsCpuCycleStolen,
                $"Sprite 3 following-line access expected at line $11 cycle {c}.");
        }
    }

    /// <summary>
    /// FR/TR: FR-VIC-006 / FR-VIC-010 / TR-CYCLE-001 / TEST-VIC-001
    /// (BACKFILL-VIDEO-001 sprite DMA stall).
    /// Use case: Re-enabling a sprite after both PAL public 55/56 checks
    /// must not retroactively create the current line's BA/data window.
    /// The same sprite can latch on a later line once its Y value matches
    /// before that later line's checks.
    /// Acceptance: Enabling sprite 3 after line $10 cycle 55 does not
    /// steal line $10 cycles 60..62 or line $11 cycles 0..1. Rewriting
    /// Y=$11 before line $11's checks then steals line $11 cycles 60..62
    /// and line $12 cycles 0..1.
    /// </summary>
    [Fact]
    public void SpriteDmaStall_ReenableAfterCheck_WaitsForNextMatchingLine()
    {
        var vic = BuildVic();

        vic.Write(ScreenControl1, 0x10); // DEN=1, YSCROLL=0
        vic.Write(SpriteY3, 0x00);
        vic.Write(SpriteEnable, 0x00);

        AdvanceTo(vic, 0x10, 56);
        vic.Write(SpriteY3, 0x10);
        vic.Write(SpriteEnable, 0x08);

        for (byte c = 60; c < vic.CyclesPerLine; c++)
        {
            AdvanceTo(vic, 0x10, c);
            Assert.False(vic.IsCpuCycleStolen,
                $"Post-check enable must not retroactively steal line $10 cycle {c}.");
        }

        for (byte c = 0; c <= 1; c++)
        {
            AdvanceTo(vic, 0x11, c);
            Assert.False(vic.IsCpuCycleStolen,
                $"Post-check enable must not retroactively steal line $11 cycle {c}.");
        }

        AdvanceTo(vic, 0x11, 53);
        vic.Write(SpriteY3, 0x11);

        for (byte c = 60; c < vic.CyclesPerLine; c++)
        {
            AdvanceTo(vic, 0x11, c);
            Assert.True(vic.IsCpuCycleStolen,
                $"Next matching line should steal at line $11 cycle {c}.");
        }

        for (byte c = 0; c <= 1; c++)
        {
            AdvanceTo(vic, 0x12, c);
            Assert.True(vic.IsCpuCycleStolen,
                $"Next matching line should carry into line $12 cycle {c}.");
        }
    }

    /// <summary>
    /// FR/TR: FR-VIC-006 / FR-VIC-010 / TR-CYCLE-001 / TEST-VIC-001
    /// (BACKFILL-VIDEO-001 sprite DMA stall).
    /// Use case: On a bad line that ALSO has an intersecting sprite, both
    /// stall windows fire. The bad-line window covers cycles 12..54 and
    /// sprite 0's PAL BA mask covers cycles 54..58. The composition
    /// produces a continuous stall band across cycles 12..58.
    /// Acceptance: With $D011=$10 (DEN=1, YSCROLL=0) and sprite 0 enabled
    /// at Y=$30 (the first bad line), IsCpuCycleStolen is true at cycle
    /// 12 (bad-line entry), at cycle 54 (bad-line exit and sprite BA),
    /// and at cycle 58; false at cycle 59 because sprite 0's PAL BA
    /// mask has released and no other sprite is enabled.
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
        // Bad-line window exits at 55; sprite-DMA BA keeps the bus.
        Assert.True(vic.IsCpuCycleStolen,
            "Sprite-DMA stall window covers cycle 55 of line $30.");

        AdvanceTo(vic, 0x30, 58);
        Assert.True(vic.IsCpuCycleStolen,
            "Sprite 0 PAL BA mask covers cycle 58 of line $30.");

        AdvanceTo(vic, 0x30, 59);
        Assert.False(vic.IsCpuCycleStolen,
            "Sprite 0 PAL BA mask releases at cycle 59 once the bad-line window has ended.");
    }

    /// <summary>
    /// FR/TR: FR-VIC-006 / FR-VIC-010 / TR-CYCLE-001 / TEST-VIC-001
    /// (BACKFILL-VIDEO-001 sprite DMA stall).
    /// Use case: IsCpuCycleStealMandatory is the "BA has been low for 3+
    /// cycles" flavour of the cycle steal; this slice models it with a
    /// one-cycle leading-edge offset (window opens one cycle later than
    /// IsCpuCycleStolen, just like the existing bad-line semantics where
    /// IsCpuCycleStolen is RasterX 12..54 and IsCpuCycleStealMandatory is
    /// 13..55).
    /// Acceptance: With sprite 0 enabled at Y=$10 on non-bad-line $10,
    /// IsCpuCycleStealMandatory is false at cycle 54, true at cycles
    /// 55..59, and false at cycle 60.
    /// </summary>
    [Fact]
    public void SpriteDmaStall_MandatoryFlag_OffsetByOneCycle()
    {
        var vic = BuildVic();

        vic.Write(ScreenControl1, 0x10); // DEN=1, YSCROLL=0
        vic.Write(SpriteY0, 0x10);
        vic.Write(SpriteEnable, 0x01);

        // Non-bad-line, sprite intersects.
        AdvanceTo(vic, 0x10, 54);
        Assert.True(vic.IsCpuCycleStolen, "Leading edge of stolen window at cycle 54.");
        Assert.False(vic.IsCpuCycleStealMandatory,
            "Mandatory flag lags by one cycle: cycle 54 is not mandatory yet.");

        AdvanceTo(vic, 0x10, 55);
        Assert.True(vic.IsCpuCycleStolen, "Cycle 55 still stolen.");
        Assert.True(vic.IsCpuCycleStealMandatory,
            "Mandatory flag asserts at cycle 55 (one cycle after the stolen window opens).");

        AdvanceTo(vic, 0x10, 59);
        Assert.True(vic.IsCpuCycleStealMandatory,
            "Mandatory flag covers the one-cycle-delayed tail of sprite 0's PAL BA mask.");

        AdvanceTo(vic, 0x10, 60);
        Assert.False(vic.IsCpuCycleStealMandatory,
            "Mandatory flag releases after sprite 0's delayed PAL BA mask closes.");
    }
}
