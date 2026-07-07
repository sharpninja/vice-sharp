namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Chips.VicIi;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// PLAN-VICEPARITY-001 audit Phase 5 (M11 xpos derivation): the sprite draw's
/// beam position comes from the piped cycle flags, whose xpos field stores the
/// PHI1 xpos floored to 8 (vicii-chip-model.c:767,
/// <c>entry |= ((xpos_phi[0] &gt;&gt; 3) &lt;&lt; XPOS_B)</c>, read back by
/// cycle_get_xpos, vicii-chip-model.h:164-167). For PAL that is
/// ((0x194 + 8*rc) % 0x1F8) &amp; ~7 of the PREVIOUS cycle. The previous managed
/// formula used the Phi2 xpos of the piped cycle, placing every sprite 8px
/// left of VICE.
/// </summary>
public sealed class VicIiSpriteXposParityTests
{
    private const ushort SpriteX0Lo   = 0xD000;
    private const ushort SpriteY0     = 0xD001;
    private const ushort SpriteX0Hi   = 0xD010;
    private const ushort SpriteEnable = 0xD015;

    private static Mos6569 BuildVicWithSprite()
    {
        var vic = new Mos6569(new BasicBus(), new InterruptLine(InterruptType.Irq));
        vic.Reset();
        var mem = new byte[0x4000];
        System.Array.Fill(mem, (byte)0xFF, 0, 64); // sprite 0 data opaque
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
    /// FR: FR-VIC-SPRITE-RENDER, TR: TR-VIC-SPRXPOS-001, TEST: TEST-VIC-SPRXPOS-01.
    /// Use case: sprite X=$18 (24) is the classic left edge of the CSEL=1
    /// display window. VICE's draw at cycle N spans beam positions
    /// cycle_get_xpos(flags(N-1)) .. +7 (floored Phi1 xpos,
    /// vicii-chip-model.c:767 with vicii-draw-cycle.c:477/:679-687), so beam
    /// 24 falls in the PAL draw of cycle 17: ((0x194 + 8*16) % 0x1F8) &amp; ~7
    /// = 24 - the same cycle that renders the first display pixels.
    /// Acceptance: a sprite at X=24 is not yet active after the cycle-16 draw
    /// and becomes active during the cycle-17 draw (pixel 0).
    /// </summary>
    [Fact]
    public void Sprite_At_X24_Triggers_During_Cycle17_Draw_The_Display_Edge()
    {
        var vic = BuildVicWithSprite();
        vic.Write(SpriteX0Hi, 0);
        vic.Write(SpriteX0Lo, 24);
        vic.Write(SpriteY0, 100);
        vic.Write(SpriteEnable, 0x01);

        AdvanceTo(vic, 101, 16);
        Assert.Equal(0, vic.GetSpriteActiveBits() & 0x01);

        vic.Tick(); // cycle 17 draw: xpos 24..31 covers X=24 at pixel 0
        Assert.NotEqual(0, vic.GetSpriteActiveBits() & 0x01);
    }
}
