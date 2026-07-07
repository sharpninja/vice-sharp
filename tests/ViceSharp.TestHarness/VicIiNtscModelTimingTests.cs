namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Chips.VicIi;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// PLAN-VICEPARITY-001 remediation Phase 5 (audit
/// docs/audit-vicii-vs-vice-2026-07-06.md M3/M4/M11/L1): NTSC-65 (6567R8
/// family) cycle timing against the VICE cycle_tab_ntsc
/// (vicii-chip-model.c:272-403): ChkSprDma rides raster cycles 55/56
/// (Phi1(56)/Phi1(57), :383/:385), ChkSprDisp rides cycle 58 (Phi1(59),
/// :389), the Phi1 xpos starts at 0x19c wrapping at 0x200 with the
/// cycle-62/63 stall (:395-397), and the draw buffer spans 65*8=520 bytes
/// (VICII_DRAW_BUFFER_SIZE, viciitypes.h:60).
/// </summary>
public sealed class VicIiNtscModelTimingTests
{
    private const ushort SpriteX0Lo   = 0xD000;
    private const ushort SpriteY0     = 0xD001;
    private const ushort SpriteX0Hi   = 0xD010;
    private const ushort SpriteEnable = 0xD015;

    private static Mos6567 BuildNtscVic()
    {
        var vic = new Mos6567(new BasicBus(), new InterruptLine(InterruptType.Irq));
        vic.Reset();
        var mem = new byte[0x4000];
        System.Array.Fill(mem, (byte)0xFF, 0, 64);
        mem[0x3F8] = 0;
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
    /// FR: FR-VIC-SPRITE-DMA, TR: TR-VIC-NTSC-001, TEST: TEST-VIC-NTSC-01.
    /// Use case: on NTSC-65 the sprite DMA condition is checked at raster
    /// cycles 55 and 56 (ChkSprDma at Phi1(56)/Phi1(57),
    /// vicii-chip-model.c:383/:385), the same cycles as old NTSC; only PAL
    /// checks at 54/55.
    /// Acceptance: a Y-matching enabled sprite has DMA latched immediately
    /// after cycle 55 of the matching line on a 6567.
    /// </summary>
    [Fact]
    public void Ntsc65_SpriteDma_Latches_At_Cycle55()
    {
        var vic = BuildNtscVic();
        vic.Write(SpriteY0, 100);
        vic.Write(SpriteEnable, 0x01);

        AdvanceTo(vic, 100, 54);
        Assert.False(vic.IsSpriteDmaActive(0));

        vic.Tick(); // cycle 55: first ChkSprDma on NTSC-65
        Assert.True(vic.IsSpriteDmaActive(0));
    }

    /// <summary>
    /// FR: FR-VIC-SPRITE-DMA, TR: TR-VIC-NTSC-001, TEST: TEST-VIC-NTSC-02.
    /// Use case: on NTSC-65 check_sprite_display (mc = mcbase reload plus the
    /// sprite_display_bits latch) rides raster cycle 58 (ChkSprDisp at
    /// Phi1(59), vicii-chip-model.c:389), one cycle later than the PAL and
    /// old-NTSC cycle 57, matching the shifted sprite-0 p-access at cycle 58.
    /// Acceptance: on the DMA turn-on line the display bit is still clear
    /// after cycle 57 and latches during cycle 58.
    /// </summary>
    [Fact]
    public void Ntsc65_SpriteDisplay_Latches_At_Cycle58()
    {
        var vic = BuildNtscVic();
        vic.Write(SpriteY0, 100);
        vic.Write(SpriteEnable, 0x01);

        AdvanceTo(vic, 100, 57);
        Assert.False(vic.GetSpriteDisplayBit(0));

        vic.Tick(); // cycle 58: ChkSprDisp on NTSC-65
        Assert.True(vic.GetSpriteDisplayBit(0));
    }

    /// <summary>
    /// FR: FR-VIC-SPRITE-RENDER, TR: TR-VIC-NTSC-001, TEST: TEST-VIC-NTSC-03.
    /// Use case: the NTSC-65 Phi1 xpos starts at 0x19c, 8 higher than PAL's
    /// 0x194 (cycle_tab_ntsc, vicii-chip-model.c:273 vs :112), so before the
    /// wrap every floored draw window sits one position later: the cycle-6
    /// draw consumes the piped rc5 flags with floored xpos
    /// (0x19c + 8*5) &amp; ~7 = 0x1C0 = 448, while the PAL base reaches 448
    /// only one cycle later.
    /// Acceptance: on a 6567 a sprite at X=448 is inactive through cycle 5
    /// and active during the cycle-6 draw.
    /// </summary>
    [Fact]
    public void Ntsc65_Sprite_At_X448_Triggers_During_Cycle6_Draw()
    {
        var vic = BuildNtscVic();
        vic.Write(SpriteX0Hi, 0x01);        // X bit 8
        vic.Write(SpriteX0Lo, 448 & 0xFF);  // X = 448
        vic.Write(SpriteY0, 100);
        vic.Write(SpriteEnable, 0x01);

        AdvanceTo(vic, 101, 5);
        Assert.Equal(0, vic.GetSpriteActiveBits() & 0x01);

        vic.Tick(); // cycle 6 draw: piped NTSC xpos 448..455 covers X=448 at pixel 0
        Assert.NotEqual(0, vic.GetSpriteActiveBits() & 0x01);
    }

    /// <summary>
    /// FR: FR-VIC-DRAW-COLOR, TR: TR-VIC-NTSC-001, TEST: TEST-VIC-NTSC-04.
    /// Use case: VICE's draw buffer holds 65*8 = 520 bytes
    /// (VICII_DRAW_BUFFER_SIZE, viciitypes.h:60) and the draw_colors8 guard
    /// trips only above offset 512 (vicii-draw-cycle.c:631), so a 65-cycle
    /// NTSC line writes all 64 post-reset batches; a 504-byte buffer
    /// truncates the last two cycles of every NTSC line.
    /// Acceptance: after cycle 64 of an NTSC-65 line the draw-buffer offset
    /// is exactly 512 (64 increments since the cycle-1 reset).
    /// </summary>
    [Fact]
    public void Ntsc65_DrawBuffer_Covers_All_65_Cycles()
    {
        var vic = BuildNtscVic();
        AdvanceTo(vic, 100, 64);
        Assert.Equal(512, vic.PixelSequencer.DbufOffset);
    }
}
