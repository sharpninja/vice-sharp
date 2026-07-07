namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Chips.VicIi;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// PLAN-VICEPARITY-001 remediation Phase 6 (audit
/// docs/audit-vicii-vs-vice-2026-07-06.md L6): the ECM address mask in the
/// secondary invalid-mode foreground helper. VICE's g_fetch_addr applies
/// <c>a &amp;= 0x39ff</c> whenever ECM is set (vicii-fetch.c:176-179),
/// clearing address bits 9/10, so ECM+BMM (an invalid mode) fetches bitmap
/// data from the masked address.
/// </summary>
public sealed class VicIiInvalidEcmMaskTests
{
    /// <summary>
    /// FR: FR-VIC-MATRIX-ADDR, TR: TR-VIC-ECMMASK-001, TEST: TEST-VIC-ECMMASK-01.
    /// Use case: in the invalid ECM+BMM mode the priority/collision bit
    /// (px &amp; 2) comes from the bitmap byte at the ECM-masked g-address
    /// (g_fetch_addr, vicii-fetch.c:167-179): screenIndex 80 addresses
    /// bitmap offset $280, masked to $080 by the ECM &amp; $39FF.
    /// Acceptance: with $80 seeded at the MASKED address and $00 at the
    /// unmasked one, the sprite-priority foreground probe reports foreground
    /// at display cell (row 2, column 0) pixel 0.
    /// </summary>
    [Fact]
    public void InvalidEcmBmm_Foreground_Reads_EcmMasked_BitmapAddress()
    {
        var vic = new Mos6569(new BasicBus(), new InterruptLine(InterruptType.Irq));
        vic.Reset();
        var mem = new byte[0x4000];
        mem[0x0080] = 0x80; // masked g-address: foreground bit at pixel 0
        mem[0x0280] = 0x00; // unmasked address: background
        vic.VideoMemoryReader = addr => addr < mem.Length ? mem[addr] : (byte)0;

        vic.Write(0xD011, 0x7B); // ECM|BMM|DEN|RSEL|YSCROLL=3 -> invalid mode
        vic.Write(0xD016, 0x08); // CSEL=1
        vic.Write(0xD018, 0x00); // bitmap base $0000

        // Tick through the display start so the border/line bookkeeping the
        // probe consults reflects an open display window.
        int target = 68;
        int budget = vic.TotalLines * vic.CyclesPerLine * 2;
        while (budget-- > 0 && !(vic.CurrentRasterLine == target && vic.RasterX == 0))
            vic.Tick();

        // Raster line 67 = display row 2 charRow 0; x = LeftBorderPixel + 0.
        Assert.True(vic.IsGraphicsPixelForegroundForSpritePriority(24, 67));
    }
}
