namespace ViceSharp.TestHarness.C1541;

using FluentAssertions;
using ViceSharp.Abstractions;
using ViceSharp.Chips.IEC;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: FR-VIA (BACKFILL-VIA CB2 handshake).
/// Use case: 1541 + IEC fast-serial drivers control CB2 through PCR bits 5-7
/// to drive handshake out, manual-level output, and to accept edge-triggered
/// CB2 inputs. This slice validates manual low (110), manual high (111),
/// handshake output (100, including CB1 restore), and input positive-edge
/// (010) IRQ latching.
/// </summary>
public sealed class Via6522Cb2ModesTests
{
    private const ushort Base = 0x1800;
    private const byte IfrCb2 = 0x08;
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
    /// FR/TR: FR-VIA (BACKFILL-VIA CB2 handshake).
    /// Use case: PCR bits 5-7 = 110 selects manual-low output. CB2 must be
    /// driven low immediately on the PCR write and remain low.
    /// Acceptance: After writing PCR = 0xC0, Cb2State reads false.
    /// </summary>
    [Fact]
    public void PcrManualLow_DrivesCb2Low()
    {
        var (via, _) = CreateVia();

        via.Write(Base + 0x0C, 0xC0); // PCR bits 7..5 = 110

        via.Cb2State.Should().BeFalse();
    }

    /// <summary>
    /// FR/TR: FR-VIA (BACKFILL-VIA CB2 handshake).
    /// Use case: PCR bits 5-7 = 111 selects manual-high output. CB2 must be
    /// driven high immediately on the PCR write.
    /// Acceptance: After writing PCR = 0xE0, Cb2State reads true.
    /// </summary>
    [Fact]
    public void PcrManualHigh_DrivesCb2High()
    {
        var (via, _) = CreateVia();

        // Force a low state first so the manual-high write must flip it.
        via.Write(Base + 0x0C, 0xC0);
        via.Cb2State.Should().BeFalse();

        via.Write(Base + 0x0C, 0xE0); // PCR bits 7..5 = 111

        via.Cb2State.Should().BeTrue();
    }

    /// <summary>
    /// FR/TR: FR-VIA (BACKFILL-VIA CB2 handshake).
    /// Use case: PCR bits 5-7 = 100 selects handshake output. CB2 idles high
    /// and goes low when the CPU writes ORB ($00); it stays low until the
    /// next CB1 active edge.
    /// Acceptance: After PCR = 0x80 and a write to $1800, Cb2State reads false.
    /// </summary>
    [Fact]
    public void PcrHandshakeOut_OrbWriteDrivesCb2Low()
    {
        var (via, _) = CreateVia();
        via.Write(Base + 0x0C, 0x80); // PCR bits 7..5 = 100, handshake idles high

        via.Cb2State.Should().BeTrue();

        via.Write(Base + 0x00, 0x55); // write to ORB triggers the handshake

        via.Cb2State.Should().BeFalse();
    }

    /// <summary>
    /// FR/TR: FR-VIA (BACKFILL-VIA CB2 handshake).
    /// Use case: In handshake output mode (PCR bits 5-7 = 100) a CB1 active
    /// edge restores CB2 to high. PCR bit 4 selects the CB1 active direction
    /// (1 = rising in this case).
    /// Acceptance: After ORB drives CB2 low, TriggerCb1(rising) sets Cb2State
    /// back to true.
    /// </summary>
    [Fact]
    public void HandshakeOut_Cb1ActiveEdgeRestoresCb2High()
    {
        var (via, _) = CreateVia();
        via.Write(Base + 0x0C, 0x90); // bits 7..5 = 100 (handshake), bit 4 = 1 (CB1 rising)

        via.Write(Base + 0x00, 0xAA); // ORB write -> CB2 low
        via.Cb2State.Should().BeFalse();

        via.TriggerCb1(rising: true); // CB1 active edge restores CB2 high

        via.Cb2State.Should().BeTrue();
    }

    /// <summary>
    /// FR/TR: FR-VIA (BACKFILL-VIA CB2 handshake).
    /// Use case: PCR bits 5-7 = 010 selects CB2 as an input with active rising
    /// edge. A rising CB2 transition latches IFR bit 3; IER bit 3 then gates
    /// the IRQ output, mirroring the CA1/CB1 edge-IRQ path.
    /// Acceptance: After TriggerCb2(rising) IFR bit 3 is set, IRQ asserted,
    /// and a wrong-direction edge before the rising one leaves it clear.
    /// </summary>
    [Fact]
    public void PcrInputPositiveEdge_TriggerCb2_LatchesIfrAndIrq()
    {
        var (via, irq) = CreateVia();
        via.Write(Base + 0x0C, 0x40); // PCR bits 7..5 = 010 -> CB2 input, rising active
        via.Write(Base + 0x0E, IerSet | IfrCb2); // enable CB2 IRQ (0x88)

        via.TriggerCb2(rising: false); // wrong-direction edge: must not latch
        (via.Read(Base + 0x0D) & IfrCb2).Should().Be(0);
        irq.IsAsserted.Should().BeFalse();

        via.TriggerCb2(rising: true); // active edge

        (via.Read(Base + 0x0D) & IfrCb2).Should().Be(IfrCb2);
        (via.Read(Base + 0x0D) & IfrAny).Should().Be(IfrAny);
        irq.IsAsserted.Should().BeTrue();
    }
}
