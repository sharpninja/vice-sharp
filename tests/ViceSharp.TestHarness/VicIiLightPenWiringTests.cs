namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Chips.VicIi;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// PLAN-VICEPARITY-001 remediation Phase 6 (audit
/// docs/audit-vicii-vs-vice-2026-07-06.md L5/L4): the machine-level light-pen
/// input paths. VICE drives the VIC pen line from CIA1 port B bit 4 on every
/// port store (c64/c64cia1.c:143-176, cia1_internal_lightpen_check ->
/// vicii_set_light_pen), and a pointing device schedules its trigger from
/// beam coordinates via vicii_lightpen_timing (vicii-lightpen.c:111-130).
/// </summary>
public sealed class VicIiLightPenWiringTests
{
    /// <summary>
    /// FR: FR-VIC-LIGHTPEN, TR: TR-VIC-LPWIRE-001, TEST: TEST-VIC-LPWIRE-01.
    /// Use case: pulling CIA1 PB4 low (DDRB bit 4 output, PRB bit 4 zero)
    /// must assert the VIC pen line through the CIA glue
    /// (c64cia1.c:153/:163/:176) and latch $D013/$D014 with the LP IRQ bit
    /// one cycle later (vicii_set_light_pen, vicii-lightpen.c:38-47).
    /// Acceptance: after writing $DC03=$10 and $DC01=$00 on a booted-line
    /// machine, one VIC tick latches $D019 bit 3 and $D014 reports the
    /// current raster line low byte.
    /// </summary>
    [Fact]
    public void Cia1_PortB_Bit4_Low_Triggers_The_LightPen()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        var vic = Assert.IsAssignableFrom<Mos6569>(machine.Devices.GetByRole(DeviceRole.VideoChip));

        // Park the raster somewhere stable mid-frame.
        while (!(vic.CurrentRasterLine == 100 && vic.RasterX == 20))
            vic.Tick();

        Assert.Equal(0, vic.Read(0xD019) & 0x08);

        machine.Bus.Write(0xDC03, 0x10); // DDRB: PB4 output
        machine.Bus.Write(0xDC01, 0x00); // PRB: PB4 low -> pen asserted

        vic.Tick(); // trigger_cycle = mclk + 1 fires at the end of the next cycle
        Assert.Equal(0x08, vic.Read(0xD019) & 0x08);
        Assert.Equal(vic.CurrentRasterLine & 0xFF, vic.Read(0xD014));
    }

    /// <summary>
    /// FR: FR-VIC-LIGHTPEN, TR: TR-VIC-LPWIRE-001, TEST: TEST-VIC-LPWIRE-02.
    /// Use case: vicii_lightpen_timing (vicii-lightpen.c:111-130) converts a
    /// visible-window pen position to beam coordinates (x += 0x80 - 0x20,
    /// y += first displayed line), suppresses off-screen positions
    /// (x &lt; 104 schedules nothing) and otherwise fires the trigger after
    /// exactly x/8 + y*cycles_per_line cycles with the sub-CLK bits latched
    /// from the pixel position.
    /// Acceptance: an off-screen call (x=0) never latches; an on-screen call
    /// at (200,100) on PAL latches $D019 bit 3 exactly at the
    /// (296/8 + 116*63)th tick after scheduling.
    /// </summary>
    [Fact]
    public void LightPenTiming_Schedules_By_Beam_Position_And_Suppresses_OffScreen()
    {
        var offVic = new Mos6569(new BasicBus(), new InterruptLine(InterruptType.Irq));
        offVic.Reset();
        offVic.SetLightPenTiming(0, 0); // beam x = 96 < 104: off screen
        for (int i = 0; i < 200; i++)
            offVic.Tick();
        Assert.Equal(0, offVic.Read(0xD019) & 0x08);

        var vic = new Mos6569(new BasicBus(), new InterruptLine(InterruptType.Irq));
        vic.Reset();
        vic.SetLightPenTiming(200, 100); // beam x = 296, y = 116

        int delta = (296 / 8) + (116 * 63);
        for (int i = 0; i < delta - 1; i++)
            vic.Tick();
        Assert.Equal(0, vic.Read(0xD019) & 0x08);

        vic.Tick();
        Assert.Equal(0x08, vic.Read(0xD019) & 0x08);
    }
}
