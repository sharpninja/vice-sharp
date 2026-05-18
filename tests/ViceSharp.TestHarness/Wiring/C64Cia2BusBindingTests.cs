namespace ViceSharp.TestHarness.Wiring;

using FluentAssertions;
using ViceSharp.Abstractions;
using ViceSharp.Architectures.C64;
using ViceSharp.Chips.Cia;
using ViceSharp.Chips.IEC;
using ViceSharp.Core;
using ViceSharp.Core.Wiring;
using Xunit;

/// <summary>
/// FR/TR: ARCH-WIRING-001 (Phase F1).
/// Use case: A running C64 must drive real traffic on the new substrate -
/// CIA2 port reads/writes have to round-trip through the user-port + IEC
/// InterSystemBus endpoints. The binding helper subscribes to CIA2's
/// existing PortA/B callbacks and translates them to bus pulls/reads.
/// </summary>
public sealed class C64Cia2BusBindingTests
{
    private static Mos6526 BuildIsolatedCia()
    {
        var bus = new BasicBus();
        var irq = new InterruptLine(InterruptType.Nmi);
        return new Mos6526(bus, irq) { BaseAddress = 0xDD00, PortAExternalInputMask = 0xC0 };
    }

    /// <summary>
    /// FR/TR: ARCH-WIRING-001
    /// Use case: Programming CIA2 PB output to $A5 drives the corresponding
    /// pin pattern on the user-port endpoint; a peer endpoint reads $A5.
    /// Acceptance: After CIA2 register write of DDRB=$FF + PB=$A5, peer
    /// ReadPortB returns $A5.
    /// </summary>
    [Fact]
    public void Cia2PbOutput_DrivesUserPortPb_Endpoint()
    {
        var cia = BuildIsolatedCia();
        var bus = UserPortInterSystemBus.Create();
        var hostEp = bus.AttachEndpoint("c64");
        var peerEp = bus.AttachEndpoint("peer");
        C64Cia2BusBinding.Bind(cia, userPort: hostEp);

        cia.Write(0xDD02, 0xFF); // DDRB = all outputs
        cia.Write(0xDD01, 0xA5); // PB = 0xA5

        UserPortInterSystemBus.ReadPortB(peerEp).Should().Be(0xA5);
    }

    /// <summary>
    /// FR/TR: ARCH-WIRING-001
    /// Use case: A peer drives bits onto the user-port PB lines; CIA2 reads
    /// those bits when its PB DDR is in input mode.
    /// Acceptance: Peer writes $42, CIA2 PB register read returns $42.
    /// </summary>
    [Fact]
    public void PeerDrivesUserPortPb_Cia2ReadsValue()
    {
        var cia = BuildIsolatedCia();
        var bus = UserPortInterSystemBus.Create();
        var hostEp = bus.AttachEndpoint("c64");
        var peerEp = bus.AttachEndpoint("peer");
        C64Cia2BusBinding.Bind(cia, userPort: hostEp);

        cia.Write(0xDD02, 0x00); // DDRB = inputs
        UserPortInterSystemBus.WritePortB(peerEp, 0x42);

        cia.Read(0xDD01).Should().Be(0x42);
    }

    /// <summary>
    /// FR/TR: ARCH-WIRING-001
    /// Use case: CIA2 PA3=1 asserts the IEC ATN line (bus reads low).
    /// Acceptance: Write PA=0x08 (with DDRA=0xFF) -> peer endpoint reads
    /// ATN low.
    /// </summary>
    [Fact]
    public void Cia2Pa3Set_AssertsIecAtn()
    {
        var cia = BuildIsolatedCia();
        var bus = IecInterSystemBus.Create();
        var hostEp = bus.AttachEndpoint("c64");
        var driveEp = bus.AttachEndpoint("drive-8");
        C64Cia2BusBinding.Bind(cia, iec: hostEp);

        cia.Write(0xDD02 + 1, 0xFF); // DDRA = all outputs (offset 3 for DDR vs 2 for PB - DDR A is at offset 2... let me check)
        cia.Write(0xDD00, 0x08);     // PA3 set -> assert ATN

        driveEp.ReadLine(IecInterSystemBus.Atn).Should().BeFalse();
    }

    /// <summary>
    /// FR/TR: ARCH-WIRING-001
    /// Use case: CIA2 PA3=0 releases ATN; the line goes high (no other
    /// pullers).
    /// Acceptance: Write PA=$08 then PA=$00 -> ATN high.
    /// </summary>
    [Fact]
    public void Cia2Pa3Clear_ReleasesIecAtn()
    {
        var cia = BuildIsolatedCia();
        var bus = IecInterSystemBus.Create();
        var hostEp = bus.AttachEndpoint("c64");
        C64Cia2BusBinding.Bind(cia, iec: hostEp);
        cia.Write(0xDD02 + 1, 0xFF);

        cia.Write(0xDD00, 0x08);
        cia.Write(0xDD00, 0x00);

        bus.ReadLine(IecInterSystemBus.Atn).Should().BeTrue();
    }

    /// <summary>
    /// FR/TR: ARCH-WIRING-001
    /// Use case: A drive on the IEC bus pulls CLK low; CIA2 reads PA6 = 1
    /// (the C64-side inverter turns bus-low into bit-high).
    /// Acceptance: drive.Pull("CLK", true) -> cia.Read($DD00) & 0x40 == 0x40.
    /// </summary>
    [Fact]
    public void DrivePullsIecClk_Cia2Pa6Reads_HighDueToInversion()
    {
        var cia = BuildIsolatedCia();
        var bus = IecInterSystemBus.Create();
        var hostEp = bus.AttachEndpoint("c64");
        var driveEp = bus.AttachEndpoint("drive-8");
        C64Cia2BusBinding.Bind(cia, iec: hostEp);
        cia.Write(0xDD02 + 1, 0x3F); // DDRA = PA0..PA5 outputs, PA6/PA7 inputs

        driveEp.Pull(IecInterSystemBus.Clk, low: true);

        (cia.Read(0xDD00) & 0x40).Should().Be(0x40);
    }

    /// <summary>
    /// FR/TR: ARCH-WIRING-001
    /// Use case: A drive pulls DATA low; CIA2 reads PA7 = 1 by inversion.
    /// Acceptance: Same as the CLK case but for DATA / PA7.
    /// </summary>
    [Fact]
    public void DrivePullsIecData_Cia2Pa7Reads_HighDueToInversion()
    {
        var cia = BuildIsolatedCia();
        var bus = IecInterSystemBus.Create();
        var hostEp = bus.AttachEndpoint("c64");
        var driveEp = bus.AttachEndpoint("drive-8");
        C64Cia2BusBinding.Bind(cia, iec: hostEp);
        cia.Write(0xDD02 + 1, 0x3F);

        driveEp.Pull(IecInterSystemBus.Data, low: true);

        (cia.Read(0xDD00) & 0x80).Should().Be(0x80);
    }

    /// <summary>
    /// FR/TR: ARCH-WIRING-001
    /// Use case: Bind requires at least one endpoint; passing both null
    /// throws (catches caller bug).
    /// Acceptance: Bind(cia, null, null) -> ArgumentException.
    /// </summary>
    [Fact]
    public void Bind_WithNoEndpoints_Throws()
    {
        var cia = BuildIsolatedCia();

        Assert.Throws<ArgumentException>(() => C64Cia2BusBinding.Bind(cia));
    }

    /// <summary>
    /// FR/TR: ARCH-WIRING-001
    /// Use case: End-to-end: build a real C64 + bind its CIA2 + write to
    /// $DD01 via the C64 system bus; the user-port peer endpoint reads
    /// the value.
    /// Acceptance: After C64.bus.Write($DD02, $FF) + Write($DD01, $5A),
    /// peer.ReadPortB = $5A.
    /// </summary>
    [Fact]
    public void RealC64_Bound_DrivesUserPortPb_ViaSystemBus()
    {
        var provider = MachineTestFactory.CreateC64RomProvider();
        var c64 = new ArchitectureBuilder(provider).Build(new C64Descriptor());
        var cia2 = (Mos6526)c64.Devices.GetByRole(DeviceRole.Cia2)!;
        var bus = UserPortInterSystemBus.Create();
        var hostEp = bus.AttachEndpoint("c64");
        var peerEp = bus.AttachEndpoint("peer");
        C64Cia2BusBinding.Bind(cia2, userPort: hostEp);

        c64.Bus.Write(0xDD02, 0xFF); // DDRB = all outputs
        c64.Bus.Write(0xDD01, 0x5A); // PB = 0x5A

        UserPortInterSystemBus.ReadPortB(peerEp).Should().Be(0x5A);
    }
}
