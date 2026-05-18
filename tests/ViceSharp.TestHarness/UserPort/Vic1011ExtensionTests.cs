namespace ViceSharp.TestHarness.UserPort;

using FluentAssertions;
using ViceSharp.Chips.UserPort;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: ARCH-USERPORT-001 (Phase D1c).
/// Use case: A VIC-1011A RS232 cartridge attached to a C64's user port
/// translates user-port pins (PA2 + PB0..PB7) to RS232 logical lines
/// (TXD/RXD/RTS/CTS/DTR/DSR/DCD/RI). This test set proves the pin-to-line
/// mapping is correct and bidirectional.
/// </summary>
public sealed class Vic1011ExtensionTests
{
    /// <summary>
    /// FR/TR: ARCH-USERPORT-001
    /// Use case: Host drives TXD high; the peer's PA2 view reads high.
    /// Acceptance: WriteTxd(true) -> bus PA2 high; WriteTxd(false) -> low.
    /// </summary>
    [Fact]
    public void Txd_DrivesPa2()
    {
        var bus = UserPortInterSystemBus.Create();
        var host = new Vic1011Extension(bus.AttachEndpoint("c64"));
        var peer = bus.AttachEndpoint("peer");

        host.WriteTxd(true);
        peer.ReadLine(UserPortInterSystemBus.Pa2).Should().BeTrue();

        host.WriteTxd(false);
        peer.ReadLine(UserPortInterSystemBus.Pa2).Should().BeFalse();
    }

    /// <summary>
    /// FR/TR: ARCH-USERPORT-001
    /// Use case: Peer asserts RXD by pulling PB0 low; host's ReadRxd
    /// reflects the state.
    /// Acceptance: peer.Pull(PB0, true) -> host.ReadRxd() = false.
    /// </summary>
    [Fact]
    public void Rxd_ReadsPb0()
    {
        var bus = UserPortInterSystemBus.Create();
        var host = new Vic1011Extension(bus.AttachEndpoint("c64"));
        var peer = bus.AttachEndpoint("peer");

        peer.Pull(UserPortInterSystemBus.Pb0, low: true);
        host.ReadRxd().Should().BeFalse();

        peer.Pull(UserPortInterSystemBus.Pb0, low: false);
        host.ReadRxd().Should().BeTrue();
    }

    /// <summary>
    /// FR/TR: ARCH-USERPORT-001
    /// Use case: Host drives RTS + DTR; peer observes those pins on the bus.
    /// Acceptance: WriteRts + WriteDtr drive PB1 + PB2; peer reads them.
    /// </summary>
    [Fact]
    public void HostOutputs_RtsAndDtr_DrivePb1Pb2()
    {
        var bus = UserPortInterSystemBus.Create();
        var host = new Vic1011Extension(bus.AttachEndpoint("c64"));
        var peer = bus.AttachEndpoint("peer");

        host.WriteRts(false);
        host.WriteDtr(false);

        peer.ReadLine(UserPortInterSystemBus.Pb1).Should().BeFalse();
        peer.ReadLine(UserPortInterSystemBus.Pb2).Should().BeFalse();
    }

    /// <summary>
    /// FR/TR: ARCH-USERPORT-001
    /// Use case: Peer-driven RS232 inputs (CTS, DSR, DCD, RI) all surface
    /// via the corresponding ReadX accessors.
    /// Acceptance: Peer pulls PB6/PB7/PB4/PB3; host reads CTS/DSR/DCD/RI low.
    /// </summary>
    [Fact]
    public void HostInputs_Cts_Dsr_Dcd_Ri_AllMapToCorrectPbPins()
    {
        var bus = UserPortInterSystemBus.Create();
        var host = new Vic1011Extension(bus.AttachEndpoint("c64"));
        var peer = bus.AttachEndpoint("peer");

        peer.Pull(UserPortInterSystemBus.Pb6, low: true);
        peer.Pull(UserPortInterSystemBus.Pb7, low: true);
        peer.Pull(UserPortInterSystemBus.Pb4, low: true);
        peer.Pull(UserPortInterSystemBus.Pb3, low: true);

        host.ReadCts().Should().BeFalse();
        host.ReadDsr().Should().BeFalse();
        host.ReadDcd().Should().BeFalse();
        host.ReadRi().Should().BeFalse();
    }

    /// <summary>
    /// FR/TR: ARCH-USERPORT-001
    /// Use case: Two C64s with VIC-1011 carts on each end can exchange TXD
    /// at peer-RXD - the canonical "modem-to-modem" topology where each
    /// machine's TXD drives the other's RXD.
    /// Acceptance: A.WriteTxd(false) -> B.ReadRxd() = false. (Wiring caveat:
    /// real two-machine links cross TXD/RXD between cables; here both ends
    /// share the same bus, so a single pin pair carries the line. Test
    /// asserts the substrate plumbing, not the physical cabling.)
    /// </summary>
    [Fact]
    public void TwoVic1011_Across_UserPortBus_TxdAtAReachesPa2AtB()
    {
        var bus = UserPortInterSystemBus.Create();
        var a = new Vic1011Extension(bus.AttachEndpoint("c64-a"));
        var bEndpoint = bus.AttachEndpoint("c64-b");

        a.WriteTxd(false);

        bEndpoint.ReadLine(UserPortInterSystemBus.Pa2).Should().BeFalse();
    }
}
