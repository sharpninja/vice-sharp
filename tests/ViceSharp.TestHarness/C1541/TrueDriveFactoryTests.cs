namespace ViceSharp.TestHarness.C1541;

using FluentAssertions;
using ViceSharp.Architectures.C64;
using ViceSharp.Core;
using ViceSharp.Host.Runtime;
using ViceSharp.Protocol;
using Xunit;

/// <summary>
/// FR/TR: FR-DRVTRUE-001 / TEST-DRVTRUE-001.
/// Use case: The runtime factory gates the cycle-accurate true-drive rig behind
/// an opt-in flag. With it off (the default) a session is the plain C64 machine
/// so the simulated-drive path and native lockstep parity are unchanged; with it
/// on, a C64 session runs a coordinator rig (C64 host + emulated 1541 over IEC)
/// that advances without error. The end-to-end true-drive LOAD itself is locked
/// by <see cref="TrueDriveLoadTests"/>.
/// </summary>
public sealed class TrueDriveFactoryTests
{
    private static DefaultEmulatorRuntimeFactory CreateFactory()
    {
        var profile = C64MachineProfiles.C64Pal;
        var builder = new ArchitectureBuilder(MachineTestFactory.CreateC64RomProvider());
        return new DefaultEmulatorRuntimeFactory(builder, new[] { new C64Descriptor(profile) }, profile.Id);
    }

    [Fact]
    public void Create_WithTrueDriveOff_BuildsPlainMachine()
    {
        var factory = CreateFactory();
        var request = new CreateEmulatorSessionRequest(C64MachineProfiles.C64Pal.Id);

        var session = factory.Create(request, trueDrive: false);

        session.Machine.Should().NotBeOfType<CoordinatorMachine>(
            "true-drive off must keep the existing simulated-drive machine so parity is unaffected");
    }

    [Fact]
    public void Create_WithTrueDriveOn_BuildsCoordinatorRigThatRuns()
    {
        var factory = CreateFactory();
        var request = new CreateEmulatorSessionRequest(C64MachineProfiles.C64Pal.Id);

        var session = factory.Create(request, trueDrive: true);

        var rig = session.Machine.Should().BeOfType<CoordinatorMachine>().Subject;
        rig.Architecture.MachineName.Should().Be(C64MachineProfiles.C64Pal.DisplayName);

        // The rig exposes its live IEC bus and the session monitors THAT bus
        // (not the host's unused always-on bus), so True Drive activity is real.
        rig.IecBus.Should().NotBeNull();
        session.IecBusActivity.Should().NotBeNull();

        // The rig advances a frame (host + drive in lockstep) without throwing.
        var step = () => rig.RunFrame();
        step.Should().NotThrow();
    }

    [Fact]
    public void Create_HonorsTrueDriveFieldOnTheRequest()
    {
        var factory = CreateFactory();

        var off = factory.Create(new CreateEmulatorSessionRequest(C64MachineProfiles.C64Pal.Id, TrueDrive: false));
        off.Machine.Should().NotBeOfType<CoordinatorMachine>();

        var on = factory.Create(new CreateEmulatorSessionRequest(C64MachineProfiles.C64Pal.Id, TrueDrive: true));
        on.Machine.Should().BeOfType<CoordinatorMachine>(
            "Create(request) must honor request.TrueDrive so the session request carries the selection end-to-end");
    }
}
