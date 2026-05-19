namespace ViceSharp.TestHarness.C1541;

using FluentAssertions;
using ViceSharp.Abstractions;
using ViceSharp.Chips.IEC;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: FR-VIA (BACKFILL-VIA CA2 handshake).
/// Use case: 1541 + IEC drivers control CA2 through PCR bits 1-3 to drive
/// handshake out, manual-level output, and to accept edge-triggered CA2
/// inputs. This slice validates manual low (110), manual high (111),
/// handshake output (100, including CA1 restore), and input positive-edge
/// (010) IRQ latching. Mirrors the CB2 slice landed in commit d6a703b.
/// </summary>
public sealed class Via6522Ca2ModesTests
{
    private const ushort Base = 0x1800;
    private const byte IfrCa1 = 0x02;
    private const byte IfrCa2 = 0x01;
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
    /// FR/TR: FR-VIA (BACKFILL-VIA CA2 handshake).
    /// Use case: PCR bits 1-3 = 110 selects manual-low output for CA2. CA2
    /// must be driven low immediately on the PCR write and remain low.
    /// Acceptance: After writing PCR = 0x0C, Ca2State reads false.
    /// </summary>
    [Fact]
    public void PcrManualLow_DrivesCa2Low()
    {
        var (via, _) = CreateVia();

        via.Write(Base + 0x0C, 0x0C); // PCR bits 3..1 = 110

        via.Ca2State.Should().BeFalse();
    }

    /// <summary>
    /// FR/TR: FR-VIA (BACKFILL-VIA CA2 handshake).
    /// Use case: PCR bits 1-3 = 111 selects manual-high output for CA2. CA2
    /// must be driven high immediately on the PCR write.
    /// Acceptance: After writing PCR = 0x0E, Ca2State reads true.
    /// </summary>
    [Fact]
    public void PcrManualHigh_DrivesCa2High()
    {
        var (via, _) = CreateVia();

        // Force a low state first so the manual-high write must flip it.
        via.Write(Base + 0x0C, 0x0C);
        via.Ca2State.Should().BeFalse();

        via.Write(Base + 0x0C, 0x0E); // PCR bits 3..1 = 111

        via.Ca2State.Should().BeTrue();
    }

    /// <summary>
    /// FR/TR: FR-VIA (BACKFILL-VIA CA2 handshake).
    /// Use case: PCR bits 1-3 = 100 selects handshake output. CA2 idles high
    /// and goes low when the CPU reads ORA/IRA ($01); it stays low until the
    /// next CA1 active edge.
    /// Acceptance: After PCR = 0x08 and a read of $1801, Ca2State reads false.
    /// </summary>
    [Fact]
    public void PcrHandshakeOut_OraReadDrivesCa2Low()
    {
        var (via, _) = CreateVia();
        via.Write(Base + 0x0C, 0x08); // PCR bits 3..1 = 100, handshake idles high

        via.Ca2State.Should().BeTrue();

        _ = via.Read(Base + 0x01); // read ORA/IRA triggers the handshake

        via.Ca2State.Should().BeFalse();
    }

    /// <summary>
    /// FR/TR: FR-VIA (BACKFILL-VIA CA2 handshake).
    /// Use case: In handshake output mode (PCR bits 1-3 = 100) a CA1 active
    /// edge restores CA2 to high. PCR bit 0 selects the CA1 active direction
    /// (1 = rising in this case).
    /// Acceptance: After ORA read drives CA2 low, TriggerCa1(rising) sets
    /// Ca2State back to true.
    /// </summary>
    [Fact]
    public void HandshakeOut_Ca1ActiveEdgeRestoresCa2High()
    {
        var (via, _) = CreateVia();
        via.Write(Base + 0x0C, 0x09); // bits 3..1 = 100 (handshake), bit 0 = 1 (CA1 rising)

        _ = via.Read(Base + 0x01); // ORA read -> CA2 low
        via.Ca2State.Should().BeFalse();

        via.TriggerCa1(rising: true); // CA1 active edge restores CA2 high

        via.Ca2State.Should().BeTrue();
    }

    /// <summary>
    /// FR/TR: FR-VIA (BACKFILL-VIA CA2 handshake).
    /// Use case: PCR bits 1-3 = 010 selects CA2 as an input with active rising
    /// edge. A rising CA2 transition latches IFR bit 0; IER bit 0 then gates
    /// the IRQ output, mirroring the CB2 input-edge path.
    /// Acceptance: After TriggerCa2(rising) IFR bit 0 is set, IRQ asserted,
    /// and a wrong-direction edge before the rising one leaves it clear.
    /// </summary>
    [Fact]
    public void PcrInputPositiveEdge_TriggerCa2_LatchesIfrAndIrq()
    {
        var (via, irq) = CreateVia();
        via.Write(Base + 0x0C, 0x04); // PCR bits 3..1 = 010 -> CA2 input, rising active
        via.Write(Base + 0x0E, IerSet | IfrCa2); // enable CA2 IRQ (0x81)

        via.TriggerCa2(rising: false); // wrong-direction edge: must not latch
        (via.Read(Base + 0x0D) & IfrCa2).Should().Be(0);
        irq.IsAsserted.Should().BeFalse();

        via.TriggerCa2(rising: true); // active edge

        (via.Read(Base + 0x0D) & IfrCa2).Should().Be(IfrCa2);
        (via.Read(Base + 0x0D) & IfrAny).Should().Be(IfrAny);
        irq.IsAsserted.Should().BeTrue();
    }
}
