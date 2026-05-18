namespace ViceSharp.TestHarness.C1541;

using FluentAssertions;
using ViceSharp.Abstractions;
using ViceSharp.Architectures.C1541;
using ViceSharp.Architectures.C64;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: ARCH-TRUEDRIVE-1541-001 (Phase B1d-3).
/// Use case: A C64 host and a 1541 drive run concurrently under one
/// SystemCoordinator, each clocking at its own rate, and exchange signals
/// over an InterSystemBus modeling the IEC serial bus (ATN/CLK/DATA).
/// Phase A's coordinator + bus primitives are the substrate; Phase B's
/// drive machine + VIA + ROM provide the drive endpoint. Phase B1d-2 will
/// add a 3-wire handshake state machine on top; this slice proves the
/// substrate carries the load.
/// </summary>
public sealed class C64Plus1541IntegrationTests
{
    private static readonly string[] IecSignals = { "ATN", "CLK", "DATA" };

    private static (SystemCoordinator coord, IMachineWithHandle host, IMachineWithHandle drive,
        InterSystemBus iec, IBusEndpoint hostEp, IBusEndpoint driveEp) BuildTwoMachineRig()
    {
        var provider = MachineTestFactory.CreateC64RomProvider();
        var c64 = new ArchitectureBuilder(provider).Build(new C64Descriptor());
        var d8 = new ArchitectureBuilder(provider).Build(new C1541Descriptor());
        var iec = new InterSystemBus("IEC", IecSignals);
        var coord = new SystemCoordinator();
        coord.AttachSystem(c64);
        coord.AttachSystem(d8);
        coord.AttachBus(iec);
        var hostEp = iec.AttachEndpoint("c64");
        var driveEp = iec.AttachEndpoint("drive-8");
        return (coord,
            new IMachineWithHandle(c64),
            new IMachineWithHandle(d8),
            iec, hostEp, driveEp);
    }

    /// <summary>
    /// FR/TR: ARCH-TRUEDRIVE-1541-001
    /// Use case: Two machines at independent rates (985_248 vs 1_000_000 Hz)
    /// advance under one coordinator. Drift bounded by 1 host cycle per the
    /// fractional accumulator invariant from Phase A.
    /// Acceptance: After Step(985_248), host clock = 985_248; drive within
    /// +/- 1 of 1_000_000.
    /// </summary>
    [Fact]
    public void Coordinator_AdvancesC64AndDrive_AtTheirOwnRates()
    {
        var rig = BuildTwoMachineRig();
        rig.host.Machine.Clock.FrequencyHz.Should().Be(985_248);
        rig.drive.Machine.Clock.FrequencyHz.Should().Be(1_000_000);

        rig.coord.Step(985_248);

        rig.host.Machine.Clock.TotalCycles.Should().Be(985_248);
        var driveDrift = System.Math.Abs(rig.drive.Machine.Clock.TotalCycles - 1_000_000);
        driveDrift.Should().BeLessThanOrEqualTo(1);
    }

    /// <summary>
    /// FR/TR: ARCH-TRUEDRIVE-1541-001
    /// Use case: IEC bus endpoints registered through coordinator + bus
    /// propagate signal state between the host and drive endpoints.
    /// Acceptance: Host pulls ATN low; drive endpoint reads ATN low and the
    /// bus reports the resolved state low.
    /// </summary>
    [Fact]
    public void IecBus_PropagatesAtn_BetweenHostAndDrive()
    {
        var rig = BuildTwoMachineRig();

        rig.hostEp.Pull("ATN", low: true);

        rig.driveEp.ReadLine("ATN").Should().BeFalse();
        rig.iec.ReadLine("ATN").Should().BeFalse();
        rig.iec.ReadLine("CLK").Should().BeTrue();
        rig.iec.ReadLine("DATA").Should().BeTrue();
    }

    /// <summary>
    /// FR/TR: ARCH-TRUEDRIVE-1541-001
    /// Use case: Wired-OR semantics on the IEC bus - either side can pull
    /// DATA low to signal "not ready", and the line stays low until both
    /// release.
    /// Acceptance: Drive pulls DATA, host also pulls; drive releases; line
    /// still low; host releases; line goes high.
    /// </summary>
    [Fact]
    public void IecBus_WiredOr_BothSidesPullDataLow_DriveReleasesFirst()
    {
        var rig = BuildTwoMachineRig();

        rig.driveEp.Pull("DATA", low: true);
        rig.hostEp.Pull("DATA", low: true);
        rig.iec.ReadLine("DATA").Should().BeFalse();

        rig.driveEp.Pull("DATA", low: false);
        rig.iec.ReadLine("DATA").Should().BeFalse();

        rig.hostEp.Pull("DATA", low: false);
        rig.iec.ReadLine("DATA").Should().BeTrue();
    }

    /// <summary>
    /// FR/TR: ARCH-TRUEDRIVE-1541-001
    /// Use case: Long coordinator run with both real machines executing
    /// their ROMs - no exception, no wedge, both PCs advance from their
    /// respective reset vectors.
    /// Acceptance: After Step(50_000) both host + drive have advanced PCs.
    /// </summary>
    [Fact]
    public void Coordinator_LongRun_BothMachinesExecuteWithoutCrash()
    {
        var rig = BuildTwoMachineRig();
        rig.host.Machine.Reset();
        rig.drive.Machine.Reset();
        var hostResetPc = rig.host.Machine.GetState().PC;
        var driveResetPc = rig.drive.Machine.GetState().PC;

        rig.coord.Step(50_000);

        rig.host.Machine.GetState().PC.Should().NotBe(hostResetPc);
        rig.drive.Machine.GetState().PC.Should().NotBe(driveResetPc);
    }

    /// <summary>
    /// FR/TR: ARCH-TRUEDRIVE-1541-001
    /// Use case: Coordinator.Reset propagates to every attached machine.
    /// Acceptance: After Step(1000) + Reset(), both machines have cycle
    /// count 0 and PCs back at their reset vectors.
    /// </summary>
    [Fact]
    public void Coordinator_Reset_ResetsBothMachines()
    {
        var rig = BuildTwoMachineRig();
        rig.host.Machine.Reset();
        rig.drive.Machine.Reset();
        var hostResetPc = rig.host.Machine.GetState().PC;
        var driveResetPc = rig.drive.Machine.GetState().PC;
        rig.coord.Step(1000);

        rig.coord.Reset();

        rig.host.Machine.Clock.TotalCycles.Should().Be(0);
        rig.drive.Machine.Clock.TotalCycles.Should().Be(0);
        rig.host.Machine.GetState().PC.Should().Be(hostResetPc);
        rig.drive.Machine.GetState().PC.Should().Be(driveResetPc);
    }

    private readonly struct IMachineWithHandle
    {
        public IMachineWithHandle(ViceSharp.Abstractions.IMachine machine) => Machine = machine;
        public ViceSharp.Abstractions.IMachine Machine { get; }
    }
}
