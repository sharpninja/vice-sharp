namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Chips.VicIi;
using Xunit;

/// <summary>
/// PLAN-VICEPARITY-001 remediation Phase 4 (audit
/// docs/audit-vicii-vs-vice-2026-07-06.md M8/L2/M7/M9/M13): sprite DMA state
/// across the frame wrap, the Phi2 bus latch feeding idle sprite s-accesses,
/// the BA-settle gate on the Phi2 sprite lanes, and the $D017 store-time
/// sprite crunch. VICE references: vicii-cycle.c:202-218
/// (vicii_cycle_start_of_frame), vicii-mem.c:338/:738 (last_bus_phi2 latch)
/// with vicii-cycle.c:604-605 (per-cycle 0xff reset), vicii-fetch.c:110-154
/// (sprite_dma_cycle_0/_2) and :282-299 (vicii_fetch_sprite_dma_1),
/// vicii-mem.c:183-214 (d017_store).
/// </summary>
public sealed class VicIiSpriteStateSideEffectTests
{
    private const ushort SpriteY0     = 0xD001;
    private const ushort SpriteEnable = 0xD015;
    private const ushort SpriteYExpand = 0xD017;
    private const ushort BorderColor  = 0xD020;

    private static Mos6569 CreateVic()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        return Assert.IsAssignableFrom<Mos6569>(machine.Devices.GetByRole(DeviceRole.VideoChip));
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
    /// FR: FR-VIC-SPRITE-DMA, TR: TR-VIC-SPRSTATE-001, TEST: TEST-VIC-SPRSTATE-01.
    /// Use case: VICE's start-of-frame reset (vicii_cycle_start_of_frame,
    /// vicii-cycle.c:202-218) touches only raster/refresh/vc/light-pen state;
    /// sprite DMA ends exclusively via sprite_mcbase_update when mcbase
    /// reaches 63 (vicii-cycle.c:81-93), so a DMA window that straddles the
    /// raster wrap (sprite Y matching a line above 255) keeps fetching into
    /// the next frame.
    /// Acceptance: a sprite with Y=$32 enabled at line 306 (306 &amp; $FF = $32)
    /// has DMA active on line 307 and STILL active at line 2 of the next
    /// frame (mcbase reaches 63 only around line 15).
    /// </summary>
    [Fact]
    public void SpriteDma_Straddling_FrameWrap_Is_Not_Reset_At_StartOfFrame()
    {
        var vic = CreateVic();
        vic.Write(SpriteY0, 0x32); // matches raster line 306 (306 & 0xFF = 0x32)
        vic.Write(SpriteEnable, 0x01);

        AdvanceTo(vic, 307, 10);
        Assert.True(vic.IsSpriteDmaActive(0));

        AdvanceTo(vic, 2, 20); // next frame, before mcbase hits 63
        Assert.True(vic.IsSpriteDmaActive(0));
    }

    /// <summary>
    /// FR: FR-VIC-FETCH, TR: TR-VIC-SPRSTATE-002, TEST: TEST-VIC-SPRSTATE-02.
    /// Use case: sprite_dma_cycle_0/_2 initialise the s-access byte from the
    /// Phi2 bus latch and merge it into the 24-bit data latch UNCONDITIONALLY
    /// (vicii-fetch.c:110-154); vicii_fetch_sprite_dma_1 merges the idle $3FFF
    /// byte when DMA is inactive (:291-296). A CPU access to a VIC register
    /// latches the transferred byte onto the bus (vicii-mem.c:338/:738) until
    /// the end of the next cycle (vicii-cycle.c:604-605).
    /// Acceptance: with sprite 0 DMA inactive, a $D020 write of $05 in the
    /// cycle before the sprite 0 pointer cycle leaves $05 merged into the
    /// data high lane; the following cycle merges the idle byte into the
    /// middle lane and the reset bus value $FF into the low lane.
    /// </summary>
    [Fact]
    public void IdleSprite_SAccess_Merges_BusByte_And_IdleByte()
    {
        var vic = CreateVic();

        // Line 100 (no bad line under yscroll 0), one cycle before the
        // sprite 0 pointer cycle (RasterX 57).
        AdvanceTo(vic, 100, 56);
        vic.Write(BorderColor, 0x05); // latches $05 onto the Phi2 bus

        vic.Tick(); // RasterX 57: SprPtr(0) Phi1 + SprDma0(0) Phi2 lane
        Assert.Equal(0x05u, (vic.GetSpriteData(0) >> 16) & 0xFF);

        vic.Tick(); // RasterX 58: SprDma1(0) Phi1 idle byte + SprDma2(0) Phi2 bus byte
        Assert.Equal(0x0500FFu, vic.GetSpriteData(0));
    }

    /// <summary>
    /// FR: FR-VIC-FETCH, TR: TR-VIC-SPRSTATE-002, TEST: TEST-VIC-SPRSTATE-03.
    /// Use case: the Phi2 sprite lanes perform the real RAM fetch only when
    /// the BA prefetch counter has settled (!vicii.prefetch_cycles,
    /// vicii-fetch.c:114-120/:137-143); mc still advances. A sprite enabled
    /// between the two DMA checks gets BA only from cycle 55, so its first
    /// s-access at cycle 57 falls inside the settle window and reads the bus
    /// value instead of RAM, exactly like VICE.
    /// Acceptance: enabling sprite 0 (Y matching) between cycles 54 and 55
    /// yields a high data lane of $FF (bus) with mc advanced to 1 after
    /// cycle 57, and $FF00FF after cycle 58 (Phi1 dma1 fetches real RAM
    /// unconditionally; the settled Phi2 dma2 fetches real RAM).
    /// </summary>
    [Fact]
    public void LateEnabled_SpriteDma_FirstSAccess_Is_BusGated_By_Prefetch()
    {
        var vic = CreateVic();
        vic.Write(SpriteY0, 100); // matches raster line 100

        // First DMA check (cycle 54) runs with the sprite disabled.
        AdvanceTo(vic, 100, 54);
        Assert.False(vic.IsSpriteDmaActive(0));

        // Enable between the checks: DMA turns on at cycle 55, BA falls at 55.
        vic.Write(SpriteEnable, 0x01);
        vic.Tick(); // 55: DMA on, prefetch 4->3
        Assert.True(vic.IsSpriteDmaActive(0));
        vic.Tick(); // 56: prefetch 3->2
        vic.Tick(); // 57: prefetch 2->1; SprDma0 Phi2 gated -> bus $FF, mc 0->1

        Assert.Equal(0xFFu, (vic.GetSpriteData(0) >> 16) & 0xFF);
        Assert.Equal(1, vic.GetSpriteMc(0));

        vic.Tick(); // 58: prefetch 1->0; dma1 Phi1 real, dma2 Phi2 real
        Assert.Equal(0xFF00FFu, vic.GetSpriteData(0));
    }

    /// <summary>
    /// FR: FR-VIC-SPRITE-DMA, TR: TR-VIC-SPRSTATE-003, TEST: TEST-VIC-SPRSTATE-04.
    /// Use case: d017_store (vicii-mem.c:183-214) early-outs on an unchanged
    /// value, and for each sprite whose Y-expand bit clears while exp_flop is
    /// low applies the sprite crunch when the store lands on the
    /// ChkSprCrunch cycle (the hw Phi2(15) flags = managed RasterX 14,
    /// vicii-chip-model.c:141): mc = (2A &amp; (mcbase &amp; mc)) | (15 &amp; (mcbase | mc)),
    /// and unconditionally sets exp_flop.
    /// Acceptance: with sprite 0 Y-expanded and exp_flop low on its second
    /// DMA line (mc=3, mcbase=0), an unchanged $D017 store at cycle 14 leaves
    /// mc untouched; clearing the bit at cycle 14 crunches mc to 1 and sets
    /// exp_flop.
    /// </summary>
    [Fact]
    public void D017Store_OnCrunchCycle_Rewrites_Mc_And_Sets_ExpFlop()
    {
        var vic = CreateVic();
        vic.Write(SpriteY0, 100);
        vic.Write(SpriteEnable, 0x01);
        vic.Write(SpriteYExpand, 0x01); // Y-expanded

        // Line 100: DMA on at cycle 54/55, exp_flop true then toggled false at
        // cycle 55's check_exp; s-accesses advance mc to 3. Line 101 cycle 14
        // is the crunch cycle with mc=3, mcbase=0, exp_flop false.
        AdvanceTo(vic, 101, 14);
        Assert.Equal(3, vic.GetSpriteMc(0));
        Assert.Equal(0, vic.GetSpriteMcBase(0));
        Assert.False(vic.GetSpriteExpFlop(0));

        // Unchanged value: d017_store early-out, no side effects.
        vic.Write(SpriteYExpand, 0x01);
        Assert.Equal(3, vic.GetSpriteMc(0));
        Assert.False(vic.GetSpriteExpFlop(0));

        // Clear the expand bit on the crunch cycle: mc = (2A & (0 & 3)) |
        // (15 & (0 | 3)) = 1, exp_flop set.
        vic.Write(SpriteYExpand, 0x00);
        Assert.Equal(1, vic.GetSpriteMc(0));
        Assert.True(vic.GetSpriteExpFlop(0));
    }

    /// <summary>
    /// FR: FR-VIC-SPRITE-DMA, TR: TR-VIC-SPRSTATE-003, TEST: TEST-VIC-SPRSTATE-05.
    /// Use case: away from the ChkSprCrunch cycle the d017_store crunch is
    /// skipped but exp_flop is still set unconditionally for every sprite
    /// whose expand bit clears while exp_flop is low (vicii-mem.c:195-210).
    /// Acceptance: the same setup as the crunch test, with the clearing store
    /// landing at cycle 20 instead: mc stays 3 and exp_flop becomes true.
    /// </summary>
    [Fact]
    public void D017Store_OffCrunchCycle_Sets_ExpFlop_Without_McRewrite()
    {
        var vic = CreateVic();
        vic.Write(SpriteY0, 100);
        vic.Write(SpriteEnable, 0x01);
        vic.Write(SpriteYExpand, 0x01);

        AdvanceTo(vic, 101, 20);
        Assert.Equal(3, vic.GetSpriteMc(0));
        Assert.False(vic.GetSpriteExpFlop(0));

        vic.Write(SpriteYExpand, 0x00);
        Assert.Equal(3, vic.GetSpriteMc(0));
        Assert.True(vic.GetSpriteExpFlop(0));
    }
}
