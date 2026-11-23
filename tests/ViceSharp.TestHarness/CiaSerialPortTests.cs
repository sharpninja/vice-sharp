namespace ViceSharp.TestHarness;

using FluentAssertions;
using ViceSharp.Abstractions;
using ViceSharp.Chips.Cia;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: FR-CIA-005 (CIA serial port).
/// Use case: MOS 6526 CIA Serial Data Register ($DC0C) and SP shift
/// behaviour driven by CRA bit 6 (SP direction). Output mode (CRA.6 = 1)
/// toggles CNT on each Timer A underflow and emits one bit on each
/// falling CNT edge. After the eighth bit, ICR bit 3 latches and
/// asserts IRQ if enabled. Input shifts require external CNT edges.
/// </summary>
public sealed class CiaSerialPortTests
{
    private static (Mos6526 cia, InterruptLine irq) BuildCiaWithIrq()
    {
        var bus = new BasicBus();
        var irq = new InterruptLine(InterruptType.Irq);
        var cia = new Mos6526(bus, irq) { BaseAddress = 0xDC00 };
        return (cia, irq);
    }

    /// <summary>
    /// FR/TR: FR-CIA-005.
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
    /// FR/TR: FR-CIA-005 ac-01, ac-03, ac-04 and ac-06.
    /// Use case: Software transmits one byte and polls for completion.
    /// Acceptance: An SDR write before enabling output triggers no
    /// transfer. In output mode the eight falling CNT edges carry $A5
    /// in order. ICR bit 3 latches after the eighth bit with IRQ masked.
    /// 2026-11-23, D6/R18: use the CNT half-cycles and SDR arming order
    /// from VICE ciacore.c.
    /// </summary>
    [Fact]
    public void OutputMode_EighthTransmittedBit_LatchesSdrIcrBit3()
    {
        var (cia, irq) = BuildCiaWithIrq();

        // Timer A latch = 5 -> short underflow cycle for fast test.
        cia.Write(0xDC04, 0x05);
        cia.Write(0xDC05, 0x00);

        cia.Write(0xDC0C, 0xA5);
        cia.Write(0xDC0E, 0x51);
        for (var i = 0; i < 24; i++)
        {
            cia.Tick();
            cia.SerialClockPin.Should().BeTrue("an SDR write in input mode does not start output");
        }

        cia.Write(0xDC0E, 0x50); // stop and reload Timer A, leaving SP in output mode
        cia.Write(0xDC0C, 0xA5);
        cia.Write(0xDC0E, 0x51);

        for (var bit = 7; bit >= 0; bit--)
        {
            (cia.Peek(0xDC0D) & 0x08).Should().Be(0, "SDR must remain clear before the last falling edge");
            AdvanceToNextCntTransition(cia).Should().BeFalse();
            cia.SerialPortPin.Should().Be(((0xA5 >> bit) & 1) != 0);
            if (bit > 0)
                AdvanceToNextCntTransition(cia).Should().BeTrue();
        }

        // Allow VICE's post-edge interrupt pipeline to publish before
        // another Timer A underflow is due.
        for (var i = 0; i < 4; i++)
            cia.Tick();
        (cia.Read(0xDC0D) & 0x08).Should().Be(0x08);
        irq.IsAsserted.Should().BeFalse("the SDR interrupt is masked");
    }

    /// <summary>
    /// FR/TR: FR-CIA-005 ac-03 and FR-CIA-007.
    /// Use case: When ICR mask bit 3 is enabled via the IMR (write 0x88
    /// to $DC0D: bit 7 = set, bit 3 = SP), the SDR-complete event must
    /// drive the CIA's IRQ line high. Reading $DC0D returns the SDR
    /// flag plus the master IR bit and releases the line.
    /// Acceptance: IRQ stays clear through the first fourteen CNT
    /// transitions. After the last falling edge, SDR asserts IRQ and a
    /// single ICR read clears it.
    /// 2026-11-23, D6/R18: program CRA before writing SDR and check
    /// the IRQ across the eighth bit rather than counting eight underflows.
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

        cia.Write(0xDC0E, 0x50); // select output before writing SDR
        cia.Write(0xDC0C, 0xFF);
        cia.Write(0xDC0E, 0x51);

        for (var i = 0; i < 14; i++)
        {
            AdvanceToNextCntTransition(cia);
            irq.IsAsserted.Should().BeFalse("a partial byte cannot raise the SDR interrupt");
        }

        AdvanceToNextCntTransition(cia).Should().BeFalse("the eighth bit is emitted on the falling edge");
        for (var i = 0; i < 4; i++)
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
    /// FR/TR: FR-CIA-005 ac-02 and ac-05.
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
    /// FR/TR: FR-CIA-005 ac-01 and ac-03.
    /// Use case: A partial transmission cannot signal SDR completion.
    /// Acceptance: ICR bit 3 is clear with only four bits transmitted,
    /// after eight Timer A underflows.
    /// 2026-11-23, D6/R18: arm a real transfer before checking the
    /// incomplete shift.
    /// </summary>
    [Fact]
    public void OutputMode_PartialShift_EightUnderflows_DoesNotLatchIcrBit3()
    {
        var (cia, _) = BuildCiaWithIrq();

        // Timer A latch = 5.
        cia.Write(0xDC04, 0x05);
        cia.Write(0xDC05, 0x00);

        cia.Write(0xDC0E, 0x50);
        cia.Write(0xDC0C, 0xA5);
        cia.Write(0xDC0E, 0x51);

        for (var i = 0; i < 8; i++)
            AdvanceToNextCntTransition(cia);

        var icr = cia.Read(0xDC0D);
        (icr & 0x08).Should().Be(0x00);
    }

    /// <summary>
    /// FR-CIA-005 ac-01 and ac-06, 2026-11-23. Waits for one CNT half-cycle
    /// with the tests' Timer A latch of five.
    /// </summary>
    private static bool AdvanceToNextCntTransition(Mos6526 cia)
    {
        var previousLevel = cia.SerialClockPin;
        for (var cycles = 0; cycles < 12; cycles++)
        {
            cia.Tick();
            if (cia.SerialClockPin != previousLevel)
                return cia.SerialClockPin;
        }

        throw new InvalidOperationException("CNT did not toggle within one Timer A interval.");
    }
}
