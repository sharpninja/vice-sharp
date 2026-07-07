namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Chips.VicIi;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// PLAN-VICEPARITY-001 slice V5 / TR-PARITY-GATE-001: DIVERGENT (red-now
/// remediation target) parity tests for FR-VIC-SPRITE-DMA from
/// artifacts/vice-parity-requirements/requirements.yaml (14 ACs).
///
/// These tests verify the VICE mc/mcbase/exp_flop sprite DMA state machine
/// (viciisc/vicii-cycle.c:62-128, vicii-fetch.c:105-154,275-299) against the
/// managed implementation in Mos6569.cs. Each test asserts the VICE-exact
/// observable behavior for one acceptance criterion.
///
/// Cycle numbering: VICE is 1-based (cycle 55 = RasterX 54 in managed, where
/// RasterX = VICII_PAL_CYCLE(n) = n - 1). Tick() increments RasterX before
/// returning, so after AdvanceTo(line, rx) the chip is at that (line, rx).
///
/// S-access simulation: in VIC-only tests (no C64MemoryMap), mc advances only
/// via explicit LatchSpriteData calls. Tests that require mc to reach 63 call
/// LatchSpriteData 3 times per simulated DMA line (after RasterX 57 so the
/// mc=mcbase reload has already fired).
///
/// VICE sources: native/vice/vice/src/viciisc/vicii-cycle.c,
/// native/vice/vice/src/viciisc/vicii-fetch.c.
/// </summary>
public sealed class VicSpriteDmaDivergentParityTests
{
    private const ushort SpriteY0 = 0xD001;
    private const ushort SpriteYExpansion = 0xD017;
    private const ushort SpriteEnable = 0xD015;

    // ----------------------------------------------------------------
    // FR-VIC-SPRITE-DMA: mc/mcbase/exp_flop state machine
    // ----------------------------------------------------------------

    /// <summary>
    /// FR-VIC-SPRITE-DMA AC-01.
    /// Use case: VICE check_sprite_dma fires only at PAL cycles 55 and 56
    /// (RasterX 54 and 55 in managed), gated by enable AND Y==raster_line
    /// AND NOT already dma. The managed code was cycle-agnostic before
    /// FR-VIC-FETCH AC-14 (finding 39).
    /// Acceptance: with sprite 0 at Y=100, IsSpriteDmaActive(0) is false at
    /// (line=100, RasterX=53) and true at (line=100, RasterX=54) exactly.
    /// At (line=99, RasterX=54) - same cycle on the wrong line - DMA is still
    /// false, confirming the Y-match gate.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-SPRITE-DMA-01", ParityTag.Divergent, pending: false)]
    public void SpriteDma_TurnOnAtCycle55_56_YMatchGated()
    {
        var vic = BuildVic();
        vic.Write(SpriteY0, 100);
        vic.Write(SpriteEnable, 0x01);

        // Line 99 cycle 54: correct cycle but wrong line (Y=100 != 99).
        AdvanceTo(vic, 99, 54);
        Assert.False(vic.IsSpriteDmaActive(0),
            "DMA must not activate on line 99 (Y=100 does not match raster_line 99).");

        // Line 100 cycle 53: correct line, one cycle before the first check.
        AdvanceTo(vic, 100, 53);
        Assert.False(vic.IsSpriteDmaActive(0),
            "DMA must not activate before RasterX 54 (cycle 55).");

        // Line 100 cycle 54: first check_sprite_dma opportunity.
        AdvanceTo(vic, 100, 54);
        Assert.True(vic.IsSpriteDmaActive(0),
            "DMA must activate at RasterX 54 (VICE cycle 55) when Y matches.");
    }

    /// <summary>
    /// FR-VIC-SPRITE-DMA AC-02.
    /// Use case: VICE turn_sprite_dma_on (vicii-cycle.c:108-113) initialises
    /// mcbase=0 and exp_flop=1 when DMA activates, seeding the fetch
    /// sequencer for the first p-access. The managed code was missing these
    /// before FR-VIC-FETCH AC-14 (finding 39).
    /// Acceptance: immediately after DMA activates at (line=100, RasterX=54),
    /// GetSpriteMcBase(0)==0 and GetSpriteExpFlop(0)==true.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-SPRITE-DMA-02", ParityTag.Divergent, pending: false)]
    public void SpriteDma_TurnOn_SetsMcbase0AndExpFlop1()
    {
        var vic = BuildVic();
        vic.Write(SpriteY0, 100);
        vic.Write(SpriteEnable, 0x01);

        AdvanceTo(vic, 100, 54);
        Assert.True(vic.IsSpriteDmaActive(0), "Precondition: DMA active.");
        Assert.Equal(0, vic.GetSpriteMcBase(0));
        Assert.True(vic.GetSpriteExpFlop(0), "exp_flop must be 1 after turn_sprite_dma_on.");
    }

    /// <summary>
    /// FR-VIC-SPRITE-DMA AC-03.
    /// Use case: VICE check_exp (vicii-cycle.c:95-105) toggles exp_flop at
    /// cycle 56 (RasterX 55) for every DMA-active Y-expanded sprite. This
    /// causes mcbase to advance only on alternate lines, doubling the sprite
    /// height to 42 rows. Non-Y-expanded sprites are not affected (finding 39).
    /// Acceptance: for a Y-expanded sprite 0 at Y=100, GetSpriteExpFlop(0) is
    /// false at (line=100, RasterX=55) - toggled from the initial true - and
    /// true again after the next RasterX=55 at (line=101, RasterX=55).
    /// A non-Y-expanded sprite's exp_flop stays true throughout.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-SPRITE-DMA-03", ParityTag.Divergent, pending: false)]
    public void SpriteDma_ExpFlopTogglesAtRasterX55_YExpandedOnly()
    {
        var vic = BuildVic();
        vic.Write(SpriteY0, 100);
        vic.Write(SpriteEnable, 0x01);
        vic.Write(SpriteYExpansion, 0x01); // Y-expand sprite 0

        // DMA activates at RasterX 54.
        AdvanceTo(vic, 100, 54);
        Assert.True(vic.GetSpriteExpFlop(0), "exp_flop starts true after turn_sprite_dma_on.");

        // check_exp at RasterX 55 toggles exp_flop for DMA-active Y-expanded sprites.
        AdvanceTo(vic, 100, 55);
        Assert.False(vic.GetSpriteExpFlop(0),
            "exp_flop must toggle to false at RasterX 55 for Y-expanded DMA sprite.");

        // Second toggle at RasterX 55 of line 101.
        AdvanceTo(vic, 101, 55);
        Assert.True(vic.GetSpriteExpFlop(0),
            "exp_flop must toggle back to true at next RasterX 55.");
    }

    /// <summary>
    /// FR-VIC-SPRITE-DMA AC-04.
    /// Use case: VICE check_sprite_display (vicii-cycle.c:62-79) latches
    /// sprite_display_bits at cycle 58 (RasterX 57) for each sprite whose
    /// DMA is active AND enable AND Y==raster_line. The managed code used a
    /// single _spriteDmaActiveMask for both fetch and display (finding 39).
    /// Acceptance: GetSpriteDisplayBit(0) is false before RasterX 57 of the
    /// DMA trigger line, true at RasterX 57 (Y matches), and still true at
    /// RasterX 57 of line 101 (DMA active but Y no longer matches - bit not
    /// cleared while DMA is running).
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-SPRITE-DMA-04", ParityTag.Divergent, pending: false)]
    public void SpriteDma_DisplayBitLatchedAtRasterX57_NotClearedWhileDmaActive()
    {
        var vic = BuildVic();
        vic.Write(SpriteY0, 100);
        vic.Write(SpriteEnable, 0x01);

        // Before RasterX 57 on DMA trigger line: display bit not yet set.
        AdvanceTo(vic, 100, 56);
        Assert.False(vic.GetSpriteDisplayBit(0),
            "Display bit must not be set before check_sprite_display at RasterX 57.");

        // At RasterX 57: Y==raster_line==100, DMA active, enable set -> display bit set.
        AdvanceTo(vic, 100, 57);
        Assert.True(vic.GetSpriteDisplayBit(0),
            "Display bit must be set at RasterX 57 when Y matches and DMA is active.");

        // At RasterX 57 of next line (101): Y=100 != raster_line=101, DMA still active.
        // Per VICE: bit is NOT cleared when DMA active but Y does not match.
        AdvanceTo(vic, 101, 57);
        Assert.True(vic.GetSpriteDisplayBit(0),
            "Display bit must stay set on subsequent lines while DMA is active (Y mismatch does not clear it).");
    }

    /// <summary>
    /// FR-VIC-SPRITE-DMA AC-05.
    /// Use case: VICE check_sprite_display (vicii-cycle.c:69) reloads mc from
    /// mcbase at cycle 58 (RasterX 57) for every sprite, resetting the data
    /// counter to the start of the current DMA row. The managed code
    /// recomputed sourceY from line geometry instead (finding 39).
    /// Acceptance: after one DMA line's 3 s-accesses advancing mc to 3, and
    /// the mcbase update at RasterX 15 of the next line setting mcbase=3,
    /// the mc=mcbase reload at RasterX 57 leaves GetSpriteMc(0)==3.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-SPRITE-DMA-05", ParityTag.Divergent, pending: false)]
    public void SpriteDma_McReloadedFromMcbaseAtRasterX57()
    {
        var vic = BuildVic();
        vic.Write(SpriteY0, 100);
        vic.Write(SpriteEnable, 0x01);

        // DMA activates at RasterX 54 of line 100. mc=0, mcbase=0.
        // Advance to RasterX 57 (mc=mcbase reload fires), then simulate the 3
        // s-accesses that would occur at cycles 57-58 on the real bus, then
        // advance to RasterX 58 so update_sprite_data uses DataValid=true (real path).
        AdvanceTo(vic, 100, 57);
        vic.LatchSpriteData(0, 0, 0xAA); // mc: 0->1
        vic.LatchSpriteData(0, 1, 0xBB); // mc: 1->2
        vic.LatchSpriteData(0, 2, 0xCC); // mc: 2->3, DataValid=true
        AdvanceTo(vic, 100, 58);         // real path: sbuf loaded, DataValid consumed, mc=3

        // Advance past RasterX 15 of line 101: mcbase = mc = 3.
        // Advance past RasterX 57 of line 101: mc = mcbase = 3 (reload).
        AdvanceTo(vic, 101, 57);
        Assert.Equal(3, vic.GetSpriteMc(0));
        Assert.Equal(3, vic.GetSpriteMcBase(0));
    }

    /// <summary>
    /// FR-VIC-SPRITE-DMA AC-06.
    /// Use case: VICE sprite_mcbase_update (vicii-cycle.c:81-93) sets
    /// mcbase=mc at cycle 16 (RasterX 15), gated by exp_flop. For
    /// non-Y-expanded sprites exp_flop is always true so mcbase advances
    /// every line. For Y-expanded sprites exp_flop alternates, so mcbase
    /// advances only every other line (doubling effective height, finding 39).
    /// Acceptance: after 3 s-accesses on line 100 (mc=3), GetSpriteMcBase(0)
    /// is 3 at RasterX 15 of line 101. For a Y-expanded sprite: exp_flop was
    /// toggled to false at RasterX 55 of line 100, so at RasterX 15 of
    /// line 101 mcbase stays 0 (not updated).
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-SPRITE-DMA-06", ParityTag.Divergent, pending: false)]
    public void SpriteDma_McbaseUpdateAtRasterX15_GatedByExpFlop()
    {
        // Non-Y-expanded: exp_flop=1 stays true, mcbase advances.
        var vic = BuildVic();
        vic.Write(SpriteY0, 100);
        vic.Write(SpriteEnable, 0x01);

        // Advance to RasterX 57 (mc=mcbase reload fires), simulate s-accesses,
        // then advance to RasterX 58 so update_sprite_data uses the real data path.
        AdvanceTo(vic, 100, 57);
        vic.LatchSpriteData(0, 0, 0); // mc: 0->1
        vic.LatchSpriteData(0, 1, 0); // mc: 1->2
        vic.LatchSpriteData(0, 2, 0); // mc: 2->3, DataValid=true
        AdvanceTo(vic, 100, 58);      // real path: DataValid consumed, mc stays 3

        // RasterX 15 of line 101: mcbase = mc = 3 (exp_flop=true).
        AdvanceTo(vic, 101, 15);
        Assert.Equal(3, vic.GetSpriteMcBase(0));

        // Y-expanded: exp_flop toggled to false at RasterX 55, so mcbase stays 0.
        var vicYexp = BuildVic();
        vicYexp.Write(SpriteY0, 100);
        vicYexp.Write(SpriteEnable, 0x01);
        vicYexp.Write(SpriteYExpansion, 0x01);

        AdvanceTo(vicYexp, 100, 58);
        vicYexp.LatchSpriteData(0, 0, 0);
        vicYexp.LatchSpriteData(0, 1, 0);
        vicYexp.LatchSpriteData(0, 2, 0);

        // exp_flop was toggled to false at RasterX 55 of line 100.
        // At RasterX 15 of line 101: exp_flop=false -> mcbase NOT updated (stays 0).
        AdvanceTo(vicYexp, 101, 15);
        Assert.Equal(0, vicYexp.GetSpriteMcBase(0));
    }

    /// <summary>
    /// FR-VIC-SPRITE-DMA AC-07.
    /// Use case: VICE sprite_mcbase_update (vicii-cycle.c:88-90) clears
    /// sprite_dma when mcbase reaches 63 at cycle 16 (RasterX 15). The
    /// managed code used a height-window expiry at RasterX 0 instead,
    /// clearing DMA 15 cycles too early on the final line (finding 39).
    /// Acceptance: after exactly 21 DMA lines (63 s-accesses, mc=63 advanced
    /// via LatchSpriteData), DMA is still active at (line=121, RasterX=0)
    /// and becomes inactive at (line=121, RasterX=15).
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-SPRITE-DMA-07", ParityTag.Divergent, pending: false)]
    public void SpriteDma_DmaOffAtMcbase63_AtRasterX15NotRasterX0()
    {
        var vic = BuildVic();
        vic.Write(SpriteY0, 100);
        vic.Write(SpriteEnable, 0x01);

        // DMA activates at RasterX 54 of line 100. mc=0, mcbase=0.
        AdvanceTo(vic, 100, 54);
        Assert.True(vic.IsSpriteDmaActive(0), "Precondition: DMA active.");

        // Simulate 21 DMA lines: advance to RasterX 57 (mc=mcbase reload fires),
        // simulate 3 s-accesses (mc advances), then advance to RasterX 58 so
        // update_sprite_data uses DataValid=true (real path, no spurious Mc bump).
        // mc sequence: 0->3->6->...->63 over lines 100-120.
        for (int line = 100; line <= 120; line++)
        {
            AdvanceTo(vic, (ushort)line, 57);
            vic.LatchSpriteData(0, 0, 0); // mc += 1
            vic.LatchSpriteData(0, 1, 0); // mc += 1
            vic.LatchSpriteData(0, 2, 0); // mc += 1, DataValid=true
            AdvanceTo(vic, (ushort)line, 58); // real path: sbuf loaded, DataValid consumed
        }
        // After line 120 s-accesses: mc = 63, mcbase = 60 (updated at RasterX 15 of line 120
        // from mc=57, before the line 120 s-accesses ran).

        // VICE: DMA still active at cycle 1 (RasterX 0) of line 121.
        // Managed height-window: clears DMA at RasterX 0 of line 121.
        AdvanceTo(vic, 121, 0);
        Assert.True(vic.IsSpriteDmaActive(0),
            "DMA must still be active at RasterX 0 of line 121 (VICE clears at cycle 16/RasterX 15).");

        // VICE: mcbase = mc = 63 at RasterX 15 -> sprite_dma &= ~bit -> DMA off.
        AdvanceTo(vic, 121, 15);
        Assert.False(vic.IsSpriteDmaActive(0),
            "DMA must be off at RasterX 15 of line 121 (mcbase=63, sprite_mcbase_update).");
    }

    /// <summary>
    /// FR-VIC-SPRITE-DMA AC-08.
    /// Use case: VICE sprite_dma_cycle_0/_2 (vicii-fetch.c:119-120,142-143)
    /// advance mc = (mc+1)&0x3f per s-access. Three s-accesses per DMA line
    /// advance mc by 3. After 63 total s-accesses (21 lines) mc wraps to 63
    /// then DMA turns off. The managed code computed sourceY from line geometry
    /// in the renderer instead of using the hardware mc counter (finding 39).
    /// Acceptance: calling LatchSpriteData advances mc by 1 per call; after
    /// 3 calls mc=3; after 63 calls mc=63; a 64th call wraps to 0.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-SPRITE-DMA-08", ParityTag.Divergent, pending: false)]
    public void SpriteDma_McAdvances1PerSAccess_WrapsAt64()
    {
        var vic = BuildVic();
        vic.Write(SpriteY0, 100);
        vic.Write(SpriteEnable, 0x01);
        AdvanceTo(vic, 100, 54); // DMA on, mc=0.

        Assert.Equal(0, vic.GetSpriteMc(0));

        vic.LatchSpriteData(0, 0, 0); Assert.Equal(1, vic.GetSpriteMc(0));
        vic.LatchSpriteData(0, 1, 0); Assert.Equal(2, vic.GetSpriteMc(0));
        vic.LatchSpriteData(0, 2, 0); Assert.Equal(3, vic.GetSpriteMc(0));

        // Advance to 63 total calls.
        for (int k = 3; k < 63; k++)
        {
            vic.LatchSpriteData(0, k % 3, 0);
        }
        Assert.Equal(63, vic.GetSpriteMc(0));

        // 64th call wraps to 0.
        vic.LatchSpriteData(0, 0, 0);
        Assert.Equal(0, vic.GetSpriteMc(0));
    }

    /// <summary>
    /// FR-VIC-SPRITE-DMA AC-09.
    /// Use case: VICE vicii_fetch_sprite_pointer (vicii-fetch.c:275-280)
    /// stores the p-access byte in vicii.sprite[i].pointer. The fetch address
    /// is (screen_base + 0x3F8 + sprite_num), data from Phi1 RAM. The managed
    /// code lacked the stored pointer field before FR-VIC-FETCH AC-14 (finding 39).
    /// Acceptance: after LatchSpritePointer(0, 0x0D), GetSpriteDataFetchAddress(0)
    /// equals (0x0D &lt;&lt; 6) + mc == 0x340 + 0 == 0x340 (mc=0 at DMA start).
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-SPRITE-DMA-09", ParityTag.Divergent, pending: false)]
    public void SpriteDma_PAccessPointerStoredAndUsedForFetchAddress()
    {
        var vic = BuildVic();
        vic.Write(SpriteY0, 100);
        vic.Write(SpriteEnable, 0x01);
        AdvanceTo(vic, 100, 54); // DMA on, mc=0.

        const byte pointer = 0x0D;
        vic.LatchSpritePointer(0, pointer);

        ushort expectedAddress = (ushort)((pointer << 6) + vic.GetSpriteMc(0));
        Assert.Equal(expectedAddress, vic.GetSpriteDataFetchAddress(0));
        Assert.Equal((ushort)0x340, expectedAddress);
    }

    /// <summary>
    /// FR-VIC-SPRITE-DMA AC-10.
    /// Use case: VICE sprite_dma_cycle_1 (vicii-fetch.c:282-299) fetches the
    /// middle (DMA1) byte and assembles it into bits [15:8] of the 24-bit
    /// sprite data latch. The managed code assembled all bytes in the renderer
    /// from memory reads instead (finding 39).
    /// Acceptance: calling LatchSpriteData(0, 1, 0xCD) stores 0xCD in the
    /// middle byte: (GetSpriteData(0) >> 8) &amp; 0xFF == 0xCD.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-SPRITE-DMA-10", ParityTag.Divergent, pending: false)]
    public void SpriteDma_Dma1MidByteAssembledIntoData15_8()
    {
        var vic = BuildVic();
        vic.Write(SpriteY0, 100);
        vic.Write(SpriteEnable, 0x01);
        AdvanceTo(vic, 100, 54);

        vic.LatchSpriteData(0, 1, 0xCD);

        Assert.Equal(0xCD, (int)((vic.GetSpriteData(0) >> 8) & 0xFF));
    }

    /// <summary>
    /// FR-VIC-SPRITE-DMA AC-11.
    /// Use case: VICE sprite_dma_cycle_0 (vicii-fetch.c:110-131) fetches the
    /// high (DMA0) byte into data[23:16], gated by check_sprite_dma
    /// (vicii-fetch.c:114: if check_sprite_dma(i)). The managed gate is
    /// IsSpriteDmaActive checked by the C64MemoryMap caller (finding 39).
    /// Acceptance: when DMA is not active, IsSpriteDmaActive returns false
    /// (caller gate prevents the fetch). When DMA is active, LatchSpriteData
    /// stores 0xAB into data[23:16]: (GetSpriteData(0) >> 16) == 0xAB.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-SPRITE-DMA-11", ParityTag.Divergent, pending: false)]
    public void SpriteDma_Dma0HighByteGatedByCheckSpriteDma()
    {
        var vic = BuildVic();
        vic.Write(SpriteY0, 100);
        vic.Write(SpriteEnable, 0x01);

        // Before DMA activates: gate returns false.
        AdvanceTo(vic, 100, 53);
        Assert.False(vic.IsSpriteDmaActive(0),
            "IsSpriteDmaActive must be false before cycle 55 (gate prevents DMA0 fetch).");

        // After DMA activates: gate returns true, high byte stored correctly.
        AdvanceTo(vic, 100, 54);
        Assert.True(vic.IsSpriteDmaActive(0));
        vic.LatchSpriteData(0, 0, 0xAB);
        Assert.Equal(0xABu, vic.GetSpriteData(0) >> 16);
    }

    /// <summary>
    /// FR-VIC-SPRITE-DMA AC-12.
    /// Use case: VICE sprite_dma_cycle_2 (vicii-fetch.c:133-154) fetches the
    /// low (DMA2) byte into data[7:0]. The managed code assembled bytes from
    /// memory reads in the renderer instead of using the 24-bit latch (finding 39).
    /// Acceptance: calling LatchSpriteData(0, 2, 0xEF) stores 0xEF in the low
    /// byte: GetSpriteData(0) &amp; 0xFF == 0xEF.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-SPRITE-DMA-12", ParityTag.Divergent, pending: false)]
    public void SpriteDma_Dma2LowByteAssembledIntoData7_0()
    {
        var vic = BuildVic();
        vic.Write(SpriteY0, 100);
        vic.Write(SpriteEnable, 0x01);
        AdvanceTo(vic, 100, 54);

        vic.LatchSpriteData(0, 2, 0xEF);

        Assert.Equal(0xEFu, vic.GetSpriteData(0) & 0xFF);
    }

    /// <summary>
    /// FR-VIC-SPRITE-DMA AC-13.
    /// Use case: VICE drives BA low from sprite_dma at the model sprite BA
    /// cycles (3 cycles before p-access through the last s-access). The
    /// managed code used a flat per-line 2-cycle charge via
    /// SpriteDmaCyclesThisFrame instead of the cycle-accurate BA windows
    /// (finding 39).
    /// Acceptance: sprite 0 at Y=100, BA-low (IsCpuCycleStolen) asserts at
    /// RasterX 54 (cycle 55, 3 cycles before p-access at cycle 58) and is
    /// false at RasterX 53 and RasterX 59 (after s-accesses end).
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-SPRITE-DMA-13", ParityTag.Divergent, pending: false)]
    public void SpriteDma_BaLowFromSpriteDmaAtModelBaCycles()
    {
        var vic = BuildVic();
        vic.Write(SpriteY0, 100);
        vic.Write(SpriteEnable, 0x01);

        // One cycle before BA window: must be false.
        AdvanceTo(vic, 100, 53);
        Assert.False(vic.IsCpuCycleStolen,
            "BA must not be low at RasterX 53 (before the 3-cycle BA pre-hold).");

        // First cycle of BA pre-hold: RasterX 54 (3 cycles before p-access at 57).
        AdvanceTo(vic, 100, 54);
        Assert.True(vic.IsCpuCycleStolen,
            "BA must be low at RasterX 54 (VICE sprite 0 BA window start).");

        // After s-accesses: RasterX 59 is outside the sprite 0 BA window.
        AdvanceTo(vic, 100, 59);
        Assert.False(vic.IsCpuCycleStolen,
            "BA must release at RasterX 59 (after sprite 0 p+s-access window).");
    }

    /// <summary>
    /// FR-VIC-SPRITE-DMA AC-14.
    /// Use case: VICE maintains sprite_dma (fetch active) and sprite_display_bits
    /// (render active) as separate masks (vicii-cycle.c:62-79). DMA fetch runs
    /// for 21 lines from the trigger; the display bit is latched only at
    /// cycle 58 when enable AND Y==raster_line. The managed code used a single
    /// _spriteDmaActiveMask for both (finding 39).
    /// Acceptance: at (line=100, RasterX=54) DMA is active but display bit is
    /// not yet set (latched at RasterX 57). At (line=100, RasterX=57) both are
    /// true. After DMA ends, the display bit is cleared at the next RasterX 57
    /// check (DMA=0 -> display bit cleared).
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-SPRITE-DMA-14", ParityTag.Divergent, pending: false)]
    public void SpriteDma_SpriteDmaDistinctFromSpriteDisplayBits()
    {
        var vic = BuildVic();
        vic.Write(SpriteY0, 100);
        vic.Write(SpriteEnable, 0x01);

        // DMA activated but before check_sprite_display: display bit not yet set.
        AdvanceTo(vic, 100, 54);
        Assert.True(vic.IsSpriteDmaActive(0), "Precondition: DMA active.");
        Assert.False(vic.GetSpriteDisplayBit(0),
            "Display bit must not be set before RasterX 57 (not yet latched by check_sprite_display).");

        // check_sprite_display at RasterX 57: Y=100==raster_line=100 -> display bit set.
        AdvanceTo(vic, 100, 57);
        Assert.True(vic.IsSpriteDmaActive(0));
        Assert.True(vic.GetSpriteDisplayBit(0),
            "Display bit must be set at RasterX 57 when DMA active and Y matches.");

        // Simulate 21 DMA lines (mc reaches 63, DMA turns off at RasterX 15 of line 121).
        // Each iteration: advance to RasterX 57 (mc=mcbase reload), simulate s-accesses,
        // then advance to RasterX 58 (real data path, DataValid consumed, mc preserved).
        for (int line = 100; line <= 120; line++)
        {
            AdvanceTo(vic, (ushort)line, 57);
            vic.LatchSpriteData(0, 0, 0);
            vic.LatchSpriteData(0, 1, 0);
            vic.LatchSpriteData(0, 2, 0); // DataValid=true
            AdvanceTo(vic, (ushort)line, 58);
        }
        AdvanceTo(vic, 121, 15); // DMA off (mcbase=63).
        Assert.False(vic.IsSpriteDmaActive(0), "DMA must be off at RasterX 15 of line 121.");

        // At RasterX 57 of line 121: DMA=0 -> display bit cleared.
        AdvanceTo(vic, 121, 57);
        Assert.False(vic.GetSpriteDisplayBit(0),
            "Display bit must be cleared at RasterX 57 when DMA is no longer active.");
    }

    // ----------------------------------------------------------------
    // Helpers
    // ----------------------------------------------------------------

    private static Mos6569 BuildVic()
    {
        var vic = new Mos6569(new BasicBus(), new InterruptLine(InterruptType.Irq));
        vic.Reset();
        return vic;
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

        throw new InvalidOperationException(
            $"VIC did not reach line ${rasterLine:X3}, cycle {rasterCycle}.");
    }
}
