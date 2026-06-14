namespace ViceSharp.TestHarness;

using FluentAssertions;
using ViceSharp.Abstractions;
using ViceSharp.Chips.Cia;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: FR-CIA (BACKFILL-CIA FLAG pin).
/// Use case: MOS 6526 CIA FLAG pin (FLG, pin 24) is an external input
/// whose high-to-low transition latches ICR bit 4 (FLG_INT). The IRQ
/// line then asserts when IMR bit 4 is enabled. On real C64 hardware
/// CIA1 FLAG is wired to the datasette READ line and CIA2 FLAG is on
/// the user port; this slice models only the pin-trigger and ICR/IRQ
/// path (the peripheral wiring is a separate slice).
/// Acceptance: TriggerFlagPin sets ICR bit 4 latch; the ICR read clears
/// the latch; IRQ output asserts only when IMR bit 4 is enabled; the
/// latch is idempotent across multiple triggers prior to the read.
/// </summary>
public sealed class CiaFlagPinTests
{
    private static (Mos6526 cia, InterruptLine irq) BuildCiaWithIrq()
    {
        var bus = new BasicBus();
        var irq = new InterruptLine(InterruptType.Irq);
        var cia = new Mos6526(bus, irq) { BaseAddress = 0xDC00 };
        return (cia, irq);
    }

    /// <summary>
    /// FR/TR: FR-CIA (BACKFILL-CIA FLAG pin).
    /// Use case: A high-to-low transition on the FLAG pin latches ICR
    /// bit 4 (FLG) in the interrupt control register, independently of
    /// any IMR enable.
    /// Acceptance: After TriggerFlagPin, reading $DC0D returns a value
    /// with bit 4 set (the FLG latch).
    /// </summary>
    [Fact]
    public void TriggerFlagPin_SetsIcrBit4Latch()
    {
        var (cia, _) = BuildCiaWithIrq();

        cia.TriggerFlagPin();

        var icr = cia.Read(0xDC0D);
        (icr & 0x10).Should().Be(0x10, "FLG pin transition latches ICR bit 4");
    }

    /// <summary>
    /// FR/TR: FR-CIA (BACKFILL-CIA FLAG pin).
    /// Use case: Reading the ICR ($DC0D) is destructive on a real 6526:
    /// the read returns the latched flags and then clears them. After a
    /// single FLAG transition the next ICR read must observe a cleared
    /// bit 4.
    /// Acceptance: First ICR read returns bit 4 set; the second ICR
    /// read returns bit 4 clear.
    /// </summary>
    [Fact]
    public void FlagLatch_IsClearedByIcrRead()
    {
        var (cia, _) = BuildCiaWithIrq();

        cia.TriggerFlagPin();

        var first = cia.Read(0xDC0D);
        var second = cia.Read(0xDC0D);

        (first & 0x10).Should().Be(0x10, "first read observes the FLG latch");
        (second & 0x10).Should().Be(0x00, "ICR read clears the FLG latch on real hardware");
    }

    /// <summary>
    /// FR/TR: FR-CIA (BACKFILL-CIA FLAG pin).
    /// Use case: With IMR bit 4 enabled (write $90 to $DC0D: bit 7 = set
    /// mode, bit 4 = FLG enable), a FLAG transition must assert the IRQ
    /// output line.
    /// Acceptance: After enabling FLG and calling TriggerFlagPin, the
    /// IRQ line reports IsAsserted = true and reading the ICR returns
    /// the IR master bit ($80) plus the FLG bit ($10) = $90.
    /// </summary>
    [Fact]
    public void FlagIrq_AssertsWhenIcrBit4Enabled()
    {
        var (cia, irq) = BuildCiaWithIrq();

        cia.Write(0xDC0D, 0x90);

        cia.TriggerFlagPin();

        irq.IsAsserted.Should().BeTrue("FLG IRQ enable + FLG transition must drive the IRQ output");
        cia.Read(0xDC0D).Should().Be(0x90, "ICR read returns IR master bit plus FLG bit");
    }

    /// <summary>
    /// FR/TR: FR-CIA (BACKFILL-CIA FLAG pin).
    /// Use case: With IMR bit 4 disabled, a FLAG transition must still
    /// latch ICR bit 4 (so software can poll) but must not assert the
    /// IRQ line.
    /// Acceptance: After TriggerFlagPin with default IMR, the IRQ line
    /// reports IsAsserted = false and reading the ICR returns $10 (FLG
    /// set, IR master bit clear).
    /// </summary>
    [Fact]
    public void FlagIrq_DoesNotAssertWhenIcrBit4Disabled()
    {
        var (cia, irq) = BuildCiaWithIrq();

        cia.TriggerFlagPin();

        irq.IsAsserted.Should().BeFalse("FLG transition without IMR enable must not raise IRQ");
        cia.Read(0xDC0D).Should().Be(0x10, "FLG latches even without IMR enable, IR bit stays clear");
    }

    /// <summary>
    /// FR/TR: FR-CIA (BACKFILL-CIA FLAG pin).
    /// Use case: The FLG latch in the ICR is idempotent on repeated
    /// transitions before software reads the ICR. Two TriggerFlagPin
    /// calls in a row must leave bit 4 set, not toggled.
    /// Acceptance: After two TriggerFlagPin calls without an intervening
    /// read, the next ICR read returns bit 4 set.
    /// </summary>
    [Fact]
    public void TriggerFlagPin_IsIdempotentUntilIcrRead()
    {
        var (cia, _) = BuildCiaWithIrq();

        cia.TriggerFlagPin();
        cia.TriggerFlagPin();

        var icr = cia.Read(0xDC0D);
        (icr & 0x10).Should().Be(0x10, "repeated FLG transitions keep the latch set; read clears it");
    }
}
