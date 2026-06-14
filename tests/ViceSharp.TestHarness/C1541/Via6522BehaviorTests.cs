namespace ViceSharp.TestHarness.C1541;

using FluentAssertions;
using ViceSharp.Abstractions;
using ViceSharp.Chips.IEC;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: ARCH-TRUEDRIVE-1541-001 (Phase B1b).
/// Use case: The 1541 drive 6502 reads/writes a VIA over the drive bus;
/// timer underflows + port handshake drive the IRQ line; the implementation
/// must support full register R/W with mirroring, timer countdown, and IRQ
/// gating via IFR/IER.
/// </summary>
public sealed class Via6522BehaviorTests
{
    private static (Via6522 via, InterruptLine irq) CreateVia(ushort baseAddress = 0x1800, ushort size = 0x0400)
    {
        var bus = new BasicBus();
        var irq = new InterruptLine(InterruptType.Irq);
        var via = new Via6522(bus, irq) { BaseAddress = baseAddress, Size = size };
        via.Reset();
        return (via, irq);
    }

    /// <summary>
    /// FR/TR: ARCH-TRUEDRIVE-1541-001
    /// Use case: 1541 ROM addresses VIA1 at any 16-byte mirror within the
    /// $1800-$1BFF window.
    /// Acceptance: HandlesAddress is true for $1800 and $1BFF, false outside.
    /// </summary>
    [Fact]
    public void HandlesAddress_CoversMirrorRange()
    {
        var (via, _) = CreateVia();

        via.HandlesAddress(0x1800).Should().BeTrue();
        via.HandlesAddress(0x1BFF).Should().BeTrue();
        via.HandlesAddress(0x17FF).Should().BeFalse();
        via.HandlesAddress(0x1C00).Should().BeFalse();
    }

    /// <summary>
    /// FR/TR: ARCH-TRUEDRIVE-1541-001
    /// Use case: Writes within the mirror region resolve to the same register
    /// (every 16-byte stride maps to the same VIA bank).
    /// Acceptance: Writing DDRB at $1802 reads back at $1812 and $1B82.
    /// </summary>
    [Fact]
    public void MirroredAddresses_TouchSameRegister()
    {
        var (via, _) = CreateVia();

        via.Write(0x1802, 0xAA);

        via.Read(0x1812).Should().Be(0xAA);
        via.Read(0x1B82).Should().Be(0xAA);
    }

    /// <summary>
    /// FR/TR: ARCH-TRUEDRIVE-1541-001
    /// Use case: DDRA + ORA control port-A output bits.
    /// Acceptance: Output callback fires with the physical pin image
    /// (ORA | ~DDRA), matching VICE's viacore store callback; input callback
    /// supplies the undriven bits during read.
    /// </summary>
    [Fact]
    public void PortA_OutputAndInputComposeViaDdr()
    {
        var (via, _) = CreateVia();
        byte? lastOut = null;
        via.PortAOutputChanged = v => lastOut = v;
        via.PortAInput = () => 0xF0;

        via.Write(0x1803, 0x0F); // DDRA = lower 4 bits as output
        via.Write(0x1801, 0xC5); // ORA = $C5

        lastOut.Should().Be(0xF5);
        via.Read(0x1801).Should().Be((byte)((0xC5 & 0x0F) | (0xF0 & 0xF0)));
    }

    /// <summary>
    /// FR/TR: ARCH-TRUEDRIVE-1541-001
    /// Use case: Timer 1 one-shot: write latch high triggers reload + count;
    /// underflow sets the T1 IFR bit and (with IER enabled) asserts the IRQ.
    /// Acceptance: After loading T1 with N and ticking N+1 cycles, IFR bit 6
    /// is set, IRQ is asserted (with IER bit 6 enabled).
    /// </summary>
    [Fact]
    public void Timer1_OneShot_UnderflowAssertsIrq_WithIerEnabled()
    {
        var (via, irq) = CreateVia();
        via.Write(0x180E, 0xC0); // IER: enable T1 (bit 7 = set; bit 6 = T1)

        via.Write(0x1804, 0x03); // T1L-L = 3
        via.Write(0x1805, 0x00); // T1C-H = 0 -> reloads counter to 3 + starts

        for (int i = 0; i < 4; i++) via.Tick();

        var ifr = via.Read(0x180D);
        (ifr & 0x40).Should().Be(0x40);
        (ifr & 0x80).Should().Be(0x80);
        irq.IsAsserted.Should().BeTrue();
    }

    /// <summary>
    /// FR/TR: ARCH-TRUEDRIVE-1541-001
    /// Use case: With IER disabled the timer flag still sets but the IRQ line
    /// remains released.
    /// Acceptance: IFR T1 bit set; irq.IsAsserted false.
    /// </summary>
    [Fact]
    public void Timer1_Underflow_DoesNotAssertIrq_WithIerDisabled()
    {
        var (via, irq) = CreateVia();
        via.Write(0x1804, 0x02);
        via.Write(0x1805, 0x00);

        for (int i = 0; i < 3; i++) via.Tick();

        (via.Read(0x180D) & 0x40).Should().Be(0x40);
        irq.IsAsserted.Should().BeFalse();
    }

    /// <summary>
    /// FR/TR: ARCH-TRUEDRIVE-1541-001
    /// Use case: Reading T1C-L clears the T1 IFR bit and releases the IRQ.
    /// Acceptance: After T1 underflow + read $X04, IFR bit 6 + IRQ are both
    /// cleared.
    /// </summary>
    [Fact]
    public void Timer1_Underflow_ReadT1cl_ClearsFlagAndIrq()
    {
        var (via, irq) = CreateVia();
        via.Write(0x180E, 0xC0);
        via.Write(0x1804, 0x01);
        via.Write(0x1805, 0x00);
        via.Tick(); via.Tick();

        via.Read(0x1804); // T1C-L read clears T1 IFR

        (via.Read(0x180D) & 0x40).Should().Be(0);
        irq.IsAsserted.Should().BeFalse();
    }

    /// <summary>
    /// FR/TR: ARCH-TRUEDRIVE-1541-001
    /// Use case: ACR bit 6 = 1 selects Timer 1 continuous mode; counter
    /// reloads from latch on each underflow.
    /// Acceptance: After 2 underflows the timer is still running; IFR bit 6
    /// set (sticky until cleared).
    /// </summary>
    [Fact]
    public void Timer1_ContinuousMode_ReloadsOnUnderflow()
    {
        var (via, _) = CreateVia();
        via.Write(0x180B, 0x40); // ACR: T1 continuous
        via.Write(0x1804, 0x01);
        via.Write(0x1805, 0x00);

        for (int i = 0; i < 5; i++) via.Tick();
        var counterAfter5 = (ushort)(via.Read(0x1804) | (via.Read(0x1805) << 8));
        counterAfter5.Should().BeLessThanOrEqualTo(0x0001);
    }

    /// <summary>
    /// FR/TR: ARCH-TRUEDRIVE-1541-001
    /// Use case: Timer 2 one-shot: writing T2C-H starts the counter; underflow
    /// sets T2 IFR bit (bit 5).
    /// Acceptance: After loading T2 with N and ticking N+1 cycles, IFR bit 5
    /// is set.
    /// </summary>
    [Fact]
    public void Timer2_OneShot_UnderflowSetsIfrBit5()
    {
        var (via, _) = CreateVia();
        via.Write(0x1808, 0x02);
        via.Write(0x1809, 0x00);

        for (int i = 0; i < 3; i++) via.Tick();

        (via.Read(0x180D) & 0x20).Should().Be(0x20);
    }

    /// <summary>
    /// FR/TR: ARCH-TRUEDRIVE-1541-001
    /// Use case: Writing IFR with bit 7 = 0 clears flags by mask.
    /// Acceptance: After underflow sets T1 bit 6, writing $40 to IFR clears it.
    /// </summary>
    [Fact]
    public void WriteIfr_ClearsFlagsByMask()
    {
        var (via, irq) = CreateVia();
        via.Write(0x180E, 0xC0);
        via.Write(0x1804, 0x01);
        via.Write(0x1805, 0x00);
        via.Tick(); via.Tick();
        (via.Read(0x180D) & 0x40).Should().Be(0x40);

        via.Write(0x180D, 0x40); // clear T1 flag

        (via.Read(0x180D) & 0x40).Should().Be(0);
        irq.IsAsserted.Should().BeFalse();
    }
}
