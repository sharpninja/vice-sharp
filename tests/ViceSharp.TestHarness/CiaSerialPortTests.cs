namespace ViceSharp.TestHarness;

using FluentAssertions;
using ViceSharp.Abstractions;
using ViceSharp.Chips.Cia;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: FR-CIA-SDR (BACKFILL-CIA serial port).
/// Use case: MOS 6526 CIA Serial Data Register ($DC0C) and SP shift
/// behaviour driven by CRA bit 6 (SP direction). Output mode (CRA.6 = 1)
/// shifts one bit per Timer A underflow; after 8 shifts ICR bit 3 (SDR
/// IRQ) latches and, when masked-in via IMR, asserts the IRQ line. Input
/// mode (CRA.6 = 0) is silent in this slice because the CNT pin is not
/// yet plumbed.
/// </summary>
public sealed class CiaSerialPortTests
{
    private static (Mos6526 cia, InterruptLine irq) BuildCiaWithIrq()
    {
        var bus = new BasicBus();
        var irq = new InterruptLine(InterruptType.Irq);
        var cia = new Mos6526(bus, irq);
        return (cia, irq);
    }

    /// <summary>
    /// FR/TR: FR-CIA-SDR (BACKFILL-CIA serial port).
    /// Use case: $DC0C is the Serial Data Register. Software should be
    /// able to write a byte and read it back before any shifting starts.
    /// Acceptance: After writing 0xA5 to $DC0C, reading $DC0C returns
    /// 0xA5.
    /// </summary>
    [Fact]
    public void SdrRegister_RoundTripsValueBeforeShifting()
    {
        var (cia, _) = BuildCiaWithIrq();

        cia.Write(0xDC0C, 0xA5);

        cia.Read(0xDC0C).Should().Be(0xA5, "SDR read should return the value just written when no shift is in progress");
    }

    /// <summary>
    /// FR/TR: FR-CIA-SDR (BACKFILL-CIA serial port).
    /// Use case: With CRA bit 6 = 1 (SP output direction) and Timer A
    /// running, each Timer A underflow shifts one bit out of SDR. After
    /// 8 underflows the byte is fully shifted and ICR bit 3 (SDR IRQ)
    /// must latch even before any IMR enable.
    /// Acceptance: With Timer A latch=5 (fast underflows), CRA=0x51
    /// (start + force-load + SP output), and SDR = 0xA5, ticking past 8
    /// Timer A underflows causes ICR bit 3 to be set.
    /// </summary>
    [Fact]
    public void OutputMode_EightTimerAUnderflows_LatchesSdrIcrBit3()
    {
        var (cia, _) = BuildCiaWithIrq();

        // Timer A latch = 5 -> short underflow cycle for fast test.
        cia.Write(0xDC04, 0x05);
        cia.Write(0xDC05, 0x00);

        // SDR = 0xA5 (alternating bit pattern).
        cia.Write(0xDC0C, 0xA5);

        // CRA = 0x51: start (bit 0) + force-load (bit 4) + SP output (bit 6).
        cia.Write(0xDC0E, 0x51);

        // 8 underflows * ~5 cycles each + load/count pipeline overhead.
        // Tick generously (100 cycles) to ensure 8 underflows occur.
        for (var i = 0; i < 100; i++)
            cia.Tick();

        // ICR bit 3 (0x08) is the SDR / SP interrupt source.
        var icr = cia.Read(0xDC0D);
        (icr & 0x08).Should().Be(0x08, "after 8 Timer A underflows in SP output mode, ICR bit 3 must latch");
    }

    /// <summary>
    /// FR/TR: FR-CIA-SDR (BACKFILL-CIA serial port).
    /// Use case: When ICR mask bit 3 is enabled via the IMR (write 0x88
    /// to $DC0D: bit 7 = set, bit 3 = SP), the SDR-complete event must
    /// drive the CIA's IRQ line high. Reading $DC0D returns the SDR
    /// flag plus the master IR bit and releases the line.
    /// Acceptance: Same configuration as the prior test but with the
    /// IMR bit 3 enabled before starting; after 8 Timer A underflows the
    /// IRQ line is asserted and a single ICR read clears it.
    /// </summary>
    [Fact]
    public void OutputMode_AssertsIrqWhenSdrMaskEnabled()
    {
        var (cia, irq) = BuildCiaWithIrq();

        // Timer A latch = 5.
        cia.Write(0xDC04, 0x05);
        cia.Write(0xDC05, 0x00);

        // Enable IMR bit 3 (SP/SDR IRQ): 0x88 = set + SP bit.
        cia.Write(0xDC0D, 0x88);

        // SDR = 0xFF.
        cia.Write(0xDC0C, 0xFF);

        // CRA = 0x51: start + force-load + SP output.
        cia.Write(0xDC0E, 0x51);

        // Run enough cycles for 8 Timer A underflows.
        for (var i = 0; i < 100; i++)
            cia.Tick();

        irq.IsAsserted.Should().BeTrue("SDR-complete event must assert IRQ when IMR bit 3 is enabled");

        // ICR returns SDR bit (0x08) + master IR (0x80). Timer A bit
        // (0x01) is also latched because Timer A underflowed repeatedly,
        // even though Timer A IRQ is masked off.
        var icr = cia.Read(0xDC0D);
        (icr & 0x08).Should().Be(0x08, "ICR must include SDR flag");
        (icr & 0x80).Should().Be(0x80, "ICR must include master IR bit when an enabled IRQ fired");
        irq.IsAsserted.Should().BeFalse("reading ICR must release the IRQ line");
    }

    /// <summary>
    /// FR/TR: FR-CIA-SDR (BACKFILL-CIA serial port).
    /// Use case: CRA bit 6 = 0 selects SP input direction. The shift in
    /// this slice is driven by the CNT pin, which is not plumbed yet, so
    /// no shifts occur regardless of how long Timer A runs. ICR bit 3
    /// must remain clear and the IRQ line must remain low.
    /// Acceptance: With CRA in input direction and Timer A running for
    /// far longer than 8 underflows would take, ICR bit 3 stays clear.
    /// </summary>
    [Fact]
    public void InputMode_NoCntTransitions_LeavesSdrIcrBit3Clear()
    {
        var (cia, irq) = BuildCiaWithIrq();

        // Timer A latch = 5.
        cia.Write(0xDC04, 0x05);
        cia.Write(0xDC05, 0x00);

        // Enable IMR bit 3 so any unexpected shift would assert IRQ.
        cia.Write(0xDC0D, 0x88);

        // SDR = 0xA5 (won't matter in input mode but shouldn't break).
        cia.Write(0xDC0C, 0xA5);

        // CRA = 0x11: start + force-load + SP INPUT (bit 6 = 0).
        cia.Write(0xDC0E, 0x11);

        // Run plenty of cycles. Without CNT plumbing, no SP shifts must occur.
        for (var i = 0; i < 200; i++)
            cia.Tick();

        irq.IsAsserted.Should().BeFalse("input mode without CNT plumbing must not assert SDR IRQ");

        var icr = cia.Read(0xDC0D);
        (icr & 0x08).Should().Be(0x00, "ICR bit 3 must remain clear in input mode without CNT events");
    }

    /// <summary>
    /// FR/TR: FR-CIA-SDR (BACKFILL-CIA serial port).
    /// Use case: A partial shift (fewer than 8 Timer A underflows) must
    /// not yet latch ICR bit 3. The SP interrupt fires only when the
    /// full 8-bit byte has been shifted out.
    /// Acceptance: With Timer A latch=5 and CRA in SP output mode, after
    /// 7 Timer A underflows worth of ticks (~35 phi2 cycles) ICR bit 3
    /// must still be clear.
    /// </summary>
    [Fact]
    public void OutputMode_PartialShift_LessThanEightUnderflows_DoesNotLatchIcrBit3()
    {
        var (cia, _) = BuildCiaWithIrq();

        // Timer A latch = 5.
        cia.Write(0xDC04, 0x05);
        cia.Write(0xDC05, 0x00);

        // SDR = 0xA5.
        cia.Write(0xDC0C, 0xA5);

        // CRA = 0x51: start + force-load + SP output.
        cia.Write(0xDC0E, 0x51);

        // Force-load pipeline burns ~2 cycles, then 5 counts per
        // underflow * 6 underflows = 30 -> total 32 cycles guarantees
        // at least 6 underflows but stays well under 8. Keep at 35.
        for (var i = 0; i < 35; i++)
            cia.Tick();

        // ICR bit 3 must NOT be set yet because we're under 8 underflows.
        var icr = cia.Read(0xDC0D);
        (icr & 0x08).Should().Be(0x00, "ICR bit 3 must not latch until 8 Timer A underflows complete the byte shift");
    }
}
