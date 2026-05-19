namespace ViceSharp.TestHarness.C1541;

using FluentAssertions;
using ViceSharp.Abstractions;
using ViceSharp.Chips.IEC;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: FR-VIA (BACKFILL-VIA CA1/CB1 edge IRQ).
/// Use case: 1541 disk controller drives BYTE-READY on CA1; some demos drive
/// cassette pulses on CB1. The 6522 latches an IFR bit on the configured
/// active edge (PCR bit 0 for CA1, bit 4 for CB1) and gates the IRQ output
/// through the corresponding IER bits.
/// </summary>
public sealed class Via6522EdgeIrqTests
{
    private const ushort Base = 0x1800;
    private const byte IfrCa1 = 0x02;
    private const byte IfrCb1 = 0x10;
    private const byte IfrAny = 0x80;
    private const byte IerSet = 0x80;

    private static (Via6522 via, InterruptLine irq) CreateVia()
    {
        var bus = new BasicBus();
        var irq = new InterruptLine(InterruptType.Irq);
        var via = new Via6522(bus, irq) { BaseAddress = Base, Size = 0x0400 };
        via.Reset();
        return (via, irq);
    }

    /// <summary>
    /// FR/TR: FR-VIA (BACKFILL-VIA CA1/CB1 edge IRQ).
    /// Use case: With PCR bit 0 = 1 the chip latches CA1 on the rising edge;
    /// a TriggerCa1(rising: true) call sets IFR bit 1.
    /// Acceptance: IFR bit 1 reads back as set after the active edge.
    /// </summary>
    [Fact]
    public void TriggerCa1_ActiveEdge_SetsIfrBit1()
    {
        var (via, _) = CreateVia();
        via.Write(Base + 0x0C, 0x01); // PCR bit 0 = 1 -> CA1 active edge is rising

        via.TriggerCa1(rising: true);

        (via.Read(Base + 0x0D) & IfrCa1).Should().Be(IfrCa1);
    }

    /// <summary>
    /// FR/TR: FR-VIA (BACKFILL-VIA CA1/CB1 edge IRQ).
    /// Use case: With PCR bit 0 = 1 (active rising) a falling edge on CA1 must
    /// not latch the IFR bit; the chip is edge-direction sensitive.
    /// Acceptance: IFR bit 1 remains clear after TriggerCa1(rising: false).
    /// </summary>
    [Fact]
    public void TriggerCa1_WrongDirectionEdge_DoesNotSetIfr()
    {
        var (via, _) = CreateVia();
        via.Write(Base + 0x0C, 0x01); // PCR bit 0 = 1 -> active edge is rising

        via.TriggerCa1(rising: false); // falling edge while active is rising

        (via.Read(Base + 0x0D) & IfrCa1).Should().Be(0);
    }

    /// <summary>
    /// FR/TR: FR-VIA (BACKFILL-VIA CA1/CB1 edge IRQ).
    /// Use case: When IER bit 1 is set, the CA1 active edge asserts the IRQ
    /// output; clearing the IFR bit via $0D write releases the line.
    /// Acceptance: IRQ asserts on edge; IRQ de-asserts after IFR clear.
    /// </summary>
    [Fact]
    public void TriggerCa1_WithIerEnabled_AssertsAndClearsIrq()
    {
        var (via, irq) = CreateVia();
        via.Write(Base + 0x0C, 0x01); // PCR bit 0 = 1
        via.Write(Base + 0x0E, IerSet | IfrCa1); // IER: enable CA1 (0x82)

        via.TriggerCa1(rising: true);

        irq.IsAsserted.Should().BeTrue();
        (via.Read(Base + 0x0D) & IfrAny).Should().Be(IfrAny);

        via.Write(Base + 0x0D, IfrCa1); // clear CA1 flag (write 1 to bit, with bit 7 = 0)

        irq.IsAsserted.Should().BeFalse();
        (via.Read(Base + 0x0D) & IfrCa1).Should().Be(0);
    }

    /// <summary>
    /// FR/TR: FR-VIA (BACKFILL-VIA CA1/CB1 edge IRQ).
    /// Use case: CB1 mirrors the CA1 path on IFR bit 4 and PCR bit 4; the IRQ
    /// gates through IER bit 4 (0x90 with bit 7 = set-mode).
    /// Acceptance: IFR bit 4 set and IRQ asserted after the active edge.
    /// </summary>
    [Fact]
    public void TriggerCb1_ActiveEdge_SetsIfrBit4AndAssertsIrqWhenGated()
    {
        var (via, irq) = CreateVia();
        via.Write(Base + 0x0C, 0x10); // PCR bit 4 = 1 -> CB1 active edge is rising
        via.Write(Base + 0x0E, IerSet | IfrCb1); // IER: enable CB1 (0x90)

        via.TriggerCb1(rising: true);

        (via.Read(Base + 0x0D) & IfrCb1).Should().Be(IfrCb1);
        irq.IsAsserted.Should().BeTrue();
    }

    /// <summary>
    /// FR/TR: FR-VIA (BACKFILL-VIA CA1/CB1 edge IRQ).
    /// Use case: The IFR latch is a single bit; repeated active edges without
    /// an intervening clear leave the bit set without any additional
    /// side-effects.
    /// Acceptance: Two consecutive active-edge triggers leave IFR bit 1 set
    /// (the bit is idempotent under re-triggering).
    /// </summary>
    [Fact]
    public void TriggerCa1_MultipleActiveEdges_AreIdempotent()
    {
        var (via, _) = CreateVia();
        via.Write(Base + 0x0C, 0x01); // PCR bit 0 = 1

        via.TriggerCa1(rising: true);
        via.TriggerCa1(rising: true);

        (via.Read(Base + 0x0D) & IfrCa1).Should().Be(IfrCa1);
    }
}
