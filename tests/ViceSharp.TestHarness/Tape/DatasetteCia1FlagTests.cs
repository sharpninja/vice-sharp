namespace ViceSharp.TestHarness.Tape;

using FluentAssertions;
using ViceSharp.Abstractions;
using ViceSharp.Chips.Cia;
using ViceSharp.Chips.Tape;
using ViceSharp.Core;
using ViceSharp.Core.Wiring;
using Xunit;

/// <summary>
/// FR/TR: FR-TAPE + FR-CIA (BACKFILL-TAPE Datasette CIA1 FLAG).
/// Use case: On real C64 hardware the Datasette READ line is physically
/// wired to CIA1 FLAG (pin 24). Each tape pulse (a high-to-low edge on
/// READ) latches CIA1 ICR bit 4 and, when IMR bit 4 is enabled, asserts
/// the CIA1 IRQ output. This slice wires the Datasette's pull-model
/// TryReadNextPulse stream to Mos6526.TriggerFlagPin via a binding
/// owned by ViceSharp.Core.Wiring.
/// Acceptance: No-tape datasette never fires FLAG; motor-off + play-off
/// never fires FLAG; with motor + play, each pulse latches ICR bit 4
/// and successive pulses re-latch after the destructive ICR read; and
/// enabling IMR bit 4 routes the latched FLAG to the CIA IRQ line.
/// </summary>
public sealed class DatasetteCia1FlagTests
{
    /// <summary>
    /// Standard 20-byte TAP v0 header used by helpers below; "C64-TAPE-RAW"
    /// signature, version byte, then a 32-bit LE pulse-data length.
    /// </summary>
    private const string Signature = "C64-TAPE-RAW";

    private static (Mos6526 cia, InterruptLine irq, BasicBus bus) BuildCia1()
    {
        var bus = new BasicBus();
        var irq = new InterruptLine(InterruptType.Irq);
        var cia = new Mos6526(bus, irq) { BaseAddress = 0xDC00 };
        return (cia, irq, bus);
    }

    private static byte[] BuildTapImage(byte version, byte[] pulseData)
    {
        var buffer = new byte[20 + pulseData.Length];
        var sig = System.Text.Encoding.ASCII.GetBytes(Signature);
        sig.CopyTo(buffer, 0);
        buffer[12] = version;
        var len = pulseData.Length;
        buffer[16] = (byte)(len & 0xFF);
        buffer[17] = (byte)((len >> 8) & 0xFF);
        buffer[18] = (byte)((len >> 16) & 0xFF);
        buffer[19] = (byte)((len >> 24) & 0xFF);
        pulseData.CopyTo(buffer, 20);
        return buffer;
    }

    /// <summary>
    /// FR/TR: FR-TAPE + FR-CIA (BACKFILL-TAPE Datasette CIA1 FLAG).
    /// Use case: A datasette with no tape inserted must never trigger
    /// CIA1 FLAG no matter how many host cycles tick by; otherwise
    /// KERNAL tape routines would see spurious bytes from an empty
    /// drive.
    /// Acceptance: After 100_000 binding ticks, CIA1 ICR bit 4 stays
    /// clear and the IRQ line stays low even with IMR bit 4 enabled.
    /// </summary>
    [Fact]
    public void NoTape_FlagNeverTriggers()
    {
        var (cia, irq, _) = BuildCia1();
        var datasette = new Datasette();
        datasette.MotorEnabled = true;
        datasette.PlayPressed = true;
        var binding = new DatasetteCia1FlagBinding(datasette, cia);

        cia.Write(0xDC0D, 0x90);

        for (var i = 0; i < 100_000; i++)
            binding.Tick();

        var icr = cia.Read(0xDC0D);
        (icr & 0x10).Should().Be(0x00, "no tape inserted means no pulses to latch FLAG");
        irq.IsAsserted.Should().BeFalse("FLAG never fires so IRQ stays low");
    }

    /// <summary>
    /// FR/TR: FR-TAPE + FR-CIA (BACKFILL-TAPE Datasette CIA1 FLAG).
    /// Use case: A datasette with a tape but motor off (or play not
    /// pressed) must not emit pulses, so the CIA1 FLAG line stays
    /// quiet. This mirrors the physical motor + play gating the
    /// KERNAL relies on for tape control.
    /// Acceptance: After 100_000 binding ticks with MotorEnabled=false,
    /// CIA1 ICR bit 4 stays clear.
    /// </summary>
    [Fact]
    public void TapeInsertedButMotorOff_FlagNeverTriggers()
    {
        var (cia, irq, _) = BuildCia1();
        var datasette = new Datasette();
        datasette.InsertTape(BuildTapImage(version: 0, pulseData: new byte[] { 0x04, 0x04, 0x04 }));
        datasette.MotorEnabled = false;
        datasette.PlayPressed = true;
        var binding = new DatasetteCia1FlagBinding(datasette, cia);

        cia.Write(0xDC0D, 0x90);

        for (var i = 0; i < 100_000; i++)
            binding.Tick();

        var icr = cia.Read(0xDC0D);
        (icr & 0x10).Should().Be(0x00, "motor off means no pulses reach FLAG");
        irq.IsAsserted.Should().BeFalse("FLAG never fires so IRQ stays low");
    }

    /// <summary>
    /// FR/TR: FR-TAPE + FR-CIA (BACKFILL-TAPE Datasette CIA1 FLAG).
    /// Use case: With motor + play on, the binding pulls each pulse
    /// from the tape after its cycle interval elapses and triggers
    /// CIA1 FLAG. Software polls ICR (destructive read clears the
    /// latch); the next pulse re-latches. Three pulses in a row are
    /// observed independently.
    /// Acceptance: For pulse data [0x04, 0x08, 0x10] (32, 64, 128
    /// cycles), three FLAG latches are observed sequentially after
    /// the binding accumulates each pulse's cycle budget.
    /// </summary>
    [Fact]
    public void MotorOnPlayPressed_FlagFiresPerPulse()
    {
        var (cia, _, _) = BuildCia1();
        var datasette = new Datasette();
        // Three short pulses: 4 * 8 = 32 cycles, 8 * 8 = 64, 16 * 8 = 128.
        datasette.InsertTape(BuildTapImage(version: 0, pulseData: new byte[] { 0x04, 0x08, 0x10 }));
        datasette.MotorEnabled = true;
        datasette.PlayPressed = true;
        var binding = new DatasetteCia1FlagBinding(datasette, cia);

        // Pulse 1: 32 cycles.
        for (var i = 0; i < 32; i++)
            binding.Tick();
        var icr1 = cia.Read(0xDC0D);
        (icr1 & 0x10).Should().Be(0x10, "first pulse latches FLAG after its cycle budget elapses");
        (cia.Read(0xDC0D) & 0x10).Should().Be(0x00, "ICR read clears the FLG latch");

        // Pulse 2: another 64 cycles.
        for (var i = 0; i < 64; i++)
            binding.Tick();
        var icr2 = cia.Read(0xDC0D);
        (icr2 & 0x10).Should().Be(0x10, "second pulse re-latches FLAG");
        (cia.Read(0xDC0D) & 0x10).Should().Be(0x00, "ICR read clears the FLG latch again");

        // Pulse 3: another 128 cycles.
        for (var i = 0; i < 128; i++)
            binding.Tick();
        var icr3 = cia.Read(0xDC0D);
        (icr3 & 0x10).Should().Be(0x10, "third pulse re-latches FLAG");
    }

    /// <summary>
    /// FR/TR: FR-TAPE + FR-CIA (BACKFILL-TAPE Datasette CIA1 FLAG).
    /// Use case: When the host has enabled FLG in CIA1's IMR (bit 4),
    /// the FLAG transition from a tape pulse must drive the CIA1 IRQ
    /// output - this is how the KERNAL's tape ISR gets called. Without
    /// the IMR enable, the latch still happens (polled mode) but no
    /// IRQ fires.
    /// Acceptance: After enabling IMR bit 4 ($90 to $DC0D), ticking
    /// the binding long enough to consume the first pulse drives the
    /// IRQ line asserted. The ICR read returns $90 (IR master + FLG).
    /// </summary>
    [Fact]
    public void MotorOnPlayPressed_WithImrEnabled_AssertsIrq()
    {
        var (cia, irq, _) = BuildCia1();
        var datasette = new Datasette();
        datasette.InsertTape(BuildTapImage(version: 0, pulseData: new byte[] { 0x04, 0x08 }));
        datasette.MotorEnabled = true;
        datasette.PlayPressed = true;
        var binding = new DatasetteCia1FlagBinding(datasette, cia);

        cia.Write(0xDC0D, 0x90);

        // 32 cycles consumes the first pulse (4 * 8).
        for (var i = 0; i < 32; i++)
            binding.Tick();

        irq.IsAsserted.Should().BeTrue("with IMR bit 4 enabled, pulse-driven FLAG must assert IRQ");
        cia.Read(0xDC0D).Should().Be(0x90, "ICR returns IR master bit plus FLG bit");
    }
}
