namespace ViceSharp.TestHarness.Wiring;

using FluentAssertions;
using ViceSharp.Abstractions;
using ViceSharp.Chips.IEC;
using ViceSharp.Core;
using ViceSharp.Core.Wiring;
using Xunit;

/// <summary>
/// FR/TR: ARCH-WIRING-002 (Phase F2).
/// Use case: A 1541 drive's VIA1 reads ATN/CLK/DATA from the IEC bus and
/// drives CLK/DATA back; without the binding the VIA bits sit in isolation
/// and the drive can never respond to a host. This helper closes the loop
/// so a coordinator-driven C64 + 1541 actually exchange handshakes.
/// </summary>
public sealed class C1541Via1BusBindingTests
{
    private static Via6522 BuildIsolatedVia()
    {
        var bus = new BasicBus();
        var irq = new InterruptLine(InterruptType.Irq);
        return new Via6522(bus, irq) { BaseAddress = 0x1800, Size = 0x0400 };
    }

    /// <summary>
    /// FR/TR: ARCH-WIRING-002
    /// Use case: When the host asserts ATN on the bus, the drive reads
    /// VIA1 PB bit 7 = 1 (after the C64-side inverter the line is low and
    /// the drive's inverter turns it into 1 again).
    /// Acceptance: host.Pull(ATN, true) -> via1.Read(PB) bit 7 = 1.
    /// </summary>
    [Fact]
    public void HostAssertsAtn_DriveVia1Pb7Reads_One()
    {
        var via = BuildIsolatedVia();
        var bus = IecInterSystemBus.Create();
        var driveEp = bus.AttachEndpoint("drive-8");
        var hostEp = bus.AttachEndpoint("c64");
        C1541Via1BusBinding.Bind(via, driveEp, deviceNumber: 8);

        hostEp.Pull(IecInterSystemBus.Atn, low: true);

        (via.Read(0x1800) & 0x80).Should().Be(0x80);
    }

    /// <summary>
    /// FR/TR: ARCH-WIRING-002
    /// Use case: Drive's VIA1 PB1 = 1 should assert DATA on the IEC bus
    /// (line goes low).
    /// Acceptance: Setting DDR=$FF + PB=$02 -> bus DATA low.
    /// </summary>
    [Fact]
    public void DriveVia1Pb1Set_AssertsIecData()
    {
        var via = BuildIsolatedVia();
        var bus = IecInterSystemBus.Create();
        var driveEp = bus.AttachEndpoint("drive-8");
        var hostEp = bus.AttachEndpoint("c64");
        C1541Via1BusBinding.Bind(via, driveEp, deviceNumber: 8);

        via.Write(0x1802, 0xFF); // DDRB = all outputs
        via.Write(0x1800, 0x02); // PB1 set -> assert DATA

        hostEp.ReadLine(IecInterSystemBus.Data).Should().BeFalse();
    }

    /// <summary>
    /// FR/TR: ARCH-WIRING-002
    /// Use case: Drive's VIA1 PB3 = 1 should assert CLK.
    /// Acceptance: DDR=$FF + PB=$08 -> bus CLK low.
    /// </summary>
    [Fact]
    public void DriveVia1Pb3Set_AssertsIecClk()
    {
        var via = BuildIsolatedVia();
        var bus = IecInterSystemBus.Create();
        var driveEp = bus.AttachEndpoint("drive-8");
        var hostEp = bus.AttachEndpoint("c64");
        C1541Via1BusBinding.Bind(via, driveEp, deviceNumber: 8);
        via.Write(0x1802, 0xFF);

        via.Write(0x1800, 0x08);

        hostEp.ReadLine(IecInterSystemBus.Clk).Should().BeFalse();
    }

    /// <summary>
    /// FR/TR: ARCH-WIRING-002
    /// Use case: Host pulls CLK; drive reads VIA1 PB2 = 1 (inverted).
    /// Acceptance: host.Pull(CLK, true) -> via1.Read(PB) & 0x04 = 0x04.
    /// </summary>
    [Fact]
    public void HostPullsClk_DriveVia1Pb2Reads_One()
    {
        var via = BuildIsolatedVia();
        var bus = IecInterSystemBus.Create();
        var driveEp = bus.AttachEndpoint("drive-8");
        var hostEp = bus.AttachEndpoint("c64");
        C1541Via1BusBinding.Bind(via, driveEp, deviceNumber: 8);

        hostEp.Pull(IecInterSystemBus.Clk, low: true);

        (via.Read(0x1800) & 0x04).Should().Be(0x04);
    }

    /// <summary>
    /// FR/TR: ARCH-WIRING-002
    /// Use case: Device address jumper bits surface in VIA1 PB5..PB6.
    /// Acceptance: deviceNumber 9 -> bits 5..6 = 01; deviceNumber 11 -> 11.
    /// </summary>
    [Theory]
    [InlineData(8, 0x00)]
    [InlineData(9, 0x20)]
    [InlineData(10, 0x40)]
    [InlineData(11, 0x60)]
    public void DeviceAddressJumpers_SurfaceInPb5Pb6(int deviceNumber, int expectedBits)
    {
        var via = BuildIsolatedVia();
        var bus = IecInterSystemBus.Create();
        var driveEp = bus.AttachEndpoint($"drive-{deviceNumber}");
        C1541Via1BusBinding.Bind(via, driveEp, deviceNumber);

        var read = via.Read(0x1800);
        (read & 0x60).Should().Be((byte)expectedBits);
    }

    /// <summary>
    /// FR/TR: ARCH-WIRING-002
    /// Use case: Device number out of range (e.g. 12) is rejected.
    /// Acceptance: Bind with deviceNumber=12 throws ArgumentOutOfRangeException.
    /// </summary>
    [Fact]
    public void Bind_DeviceNumberOutOfRange_Throws()
    {
        var via = BuildIsolatedVia();
        var bus = IecInterSystemBus.Create();
        var ep = bus.AttachEndpoint("drive");

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            C1541Via1BusBinding.Bind(via, ep, deviceNumber: 12));
    }

    /// <summary>
    /// FR/TR: ARCH-WIRING-002
    /// Use case: End-to-end host-drive ATN handshake. Host (CIA2-bound)
    /// asserts ATN; drive (VIA1-bound) observes ATN low + replies by
    /// pulling DATA.
    /// Acceptance: After CIA2 PA3=1 + drive sees ATN -> drive sets PB1=1
    /// -> host CIA2 reads DATA pulled (PA7 = 1).
    /// </summary>
    [Fact]
    public void HostAssertsAtn_DriveAcksByPullingData_HostSeesData()
    {
        var bus = IecInterSystemBus.Create();
        var hostEp = bus.AttachEndpoint("c64");
        var driveEp = bus.AttachEndpoint("drive-8");

        // Host CIA2
        var cia = new ViceSharp.Chips.Cia.Mos6526(new BasicBus(), new InterruptLine(InterruptType.Nmi))
        { BaseAddress = 0xDD00, PortAExternalInputMask = 0xC0 };
        C64Cia2BusBinding.Bind(cia, iec: hostEp);
        cia.Write(0xDD02 + 1, 0xFF); // DDRA = all outputs (for ATN/CLK/DATA out)

        // Drive VIA1 - matches 1541 hardware: PB1, PB3, PB4 outputs; PB0,
        // PB2, PB7 inputs (DATA/CLK/ATN in); PB5/PB6 jumper inputs.
        var via = BuildIsolatedVia();
        C1541Via1BusBinding.Bind(via, driveEp, deviceNumber: 8);
        via.Write(0x1802, 0x1A); // DDRB = outputs on PB1, PB3, PB4 only

        // Host asserts ATN
        cia.Write(0xDD00, 0x08);
        // Drive reads ATN (PB7) and "responds" by asserting DATA (PB1)
        var driveSeesAtn = (via.Read(0x1800) & 0x80) == 0x80;
        driveSeesAtn.Should().BeTrue();
        via.Write(0x1800, 0x02);

        // Host CIA2 sees DATA low -> PA7 = 1
        (cia.Read(0xDD00) & 0x80).Should().Be(0x80);
    }
}
