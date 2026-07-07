namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Chips.VicIi;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// PLAN-VICEPARITY-001 remediation Phase 5 (audit
/// docs/audit-vicii-vs-vice-2026-07-06.md M14/L3): the 8565/8562
/// (color_latency=0) g-fetch takes its whole mode from the one-cycle-delayed
/// $D011 copy with no RAM-to-charROM latch magic (vicii-fetch.c:234-262, idle
/// variant :213-232), and the light-pen X latch uses the active model's
/// cycle-table xpos (vicii-lightpen.c:75 with cycle_tab_ntsc,
/// vicii-chip-model.c:272-403, including the cycle-62/63 stall).
/// </summary>
public sealed class VicIiModelFetchVariantTests
{
    /// <summary>
    /// FR: FR-VIC-MATRIX-ADDR, TR: TR-VIC-MODELFETCH-001, TEST: TEST-VIC-MODELFETCH-01.
    /// Use case: on color_latency=0 chips (8565/8562) vicii_fetch_graphics
    /// computes the g-address purely from reg11_delay
    /// (vicii-fetch.c:260-262): a $D011 BMM write takes effect for fetches
    /// only one cycle later, and the 6569 RAM-to-charROM composition magic
    /// (:242-259) never runs.
    /// Acceptance: on a Mos8565 with vbuf[0]=$23 and $D018=0, writing BMM
    /// before the delay latches yields the TEXT address $0118
    /// ((0x23 &lt;&lt; 3) | rc), not the bitmap address.
    /// </summary>
    [Fact]
    public void Mos8565_GraphicsFetch_Uses_Reg11Delay_Without_Magic()
    {
        var vic = new Mos8565(new BasicBus(), new InterruptLine(InterruptType.Irq));
        vic.Reset();
        vic.LatchVideoMatrixFetch(0, 0x23, 0x01);

        vic.Write(0xD011, 0x20); // BMM on, live register only; reg11_delay still 0

        var address = vic.ConsumeGraphicsFetchAddress();
        Assert.Equal((ushort)0x0118, address);
    }

    /// <summary>
    /// FR: FR-VIC-MATRIX-ADDR, TR: TR-VIC-MODELFETCH-001, TEST: TEST-VIC-MODELFETCH-02.
    /// Use case: vicii_fetch_idle_gfx picks its $D011 source by color_latency
    /// (vicii-fetch.c:218-222): live regs on the 6569, reg11_delay on the
    /// 8565/8562, so a fresh ECM write reaches the idle address one cycle
    /// later on the newer chips.
    /// Acceptance: on a Mos8565 with ECM freshly written (delay copy still
    /// clear) the idle fetch address is $3FFF; on a Mos6569 it is $39FF.
    /// </summary>
    [Fact]
    public void IdleGraphicsFetch_Reg11Source_Follows_ColorLatency()
    {
        var vic8565 = new Mos8565(new BasicBus(), new InterruptLine(InterruptType.Irq));
        vic8565.Reset();
        vic8565.Write(0xD011, 0x40); // ECM live; reg11_delay still 0
        Assert.Equal((ushort)0x3FFF, vic8565.IdleGraphicsFetchAddress);

        var vic6569 = new Mos6569(new BasicBus(), new InterruptLine(InterruptType.Irq));
        vic6569.Reset();
        vic6569.Write(0xD011, 0x40);
        Assert.Equal((ushort)0x39FF, vic6569.IdleGraphicsFetchAddress);
    }

    /// <summary>
    /// FR: FR-VIC-LIGHTPEN, TR: TR-VIC-MODELFETCH-002, TEST: TEST-VIC-MODELFETCH-03.
    /// Use case: the light-pen X latch is
    /// cycle_get_xpos(cycle_table[raster_cycle]) / 2 (vicii-lightpen.c:75)
    /// from the ACTIVE model's table: NTSC starts at 0x19c, wraps at 0x200
    /// and holds 0x184 through the cycle-62/63 stall
    /// (vicii-chip-model.c:395-397), so a trigger at raster cycle 62 latches
    /// X = (0x184 &amp; ~7) / 2 = $C0, where the PAL formula would give $C4.
    /// Acceptance: a 6567 pen trigger at RasterX 62 latches $D013 = $C0.
    /// </summary>
    [Fact]
    public void Ntsc65_LightPenX_Uses_Model_Xpos_With_Stall()
    {
        var vic = new Mos6567(new BasicBus(), new InterruptLine(InterruptType.Irq));
        vic.Reset();

        int maxCycles = vic.TotalLines * vic.CyclesPerLine * 2;
        for (int cycle = 0; cycle < maxCycles; cycle++)
        {
            if (vic.CurrentRasterLine == 100 && vic.RasterX == 62)
                break;
            vic.Tick();
        }

        Assert.Equal(62, vic.RasterX);
        vic.TriggerLightPen();
        Assert.Equal(0xC0, vic.Read(0xD013));
    }
}
