namespace ViceSharp.TestHarness.Wiring;

using System.Linq;
using FluentAssertions;
using ViceSharp.Abstractions;
using ViceSharp.Architectures.C64;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: FR-IECLOAD-001 / TR-DRVLIFE-001 (DD-IEC-1).
/// Use case: A single-system C64 always exposes the IEC serial bus so drives
/// can attach against it, even before CIA2 is routed to the live bus. This
/// slice wires the always-on bus + drives and exposes it for the spy/monitor;
/// CIA2 still reads its native-matching idle mask (the live-bus read is a
/// separate, parity-gated step), so native parity is unchanged here.
/// </summary>
public sealed class ArchitectureBuilderIecBusTests
{
    private static IMachine BuildC64(string model = "c64")
    {
        var provider = MachineTestFactory.CreateC64RomProvider();
        return new ArchitectureBuilder(provider).Build(C64MachineProfiles.Resolve(model) is { } profile
            ? new C64Descriptor(profile)
            : new C64Descriptor());
    }

    /// <summary>
    /// Acceptance: A normal C64 registers exactly one IecBusDevice with a
    /// non-null shared bus.
    /// </summary>
    [Fact]
    public void BuiltC64_ExposesAlwaysOnIecBus()
    {
        var c64 = BuildC64();

        var busDevice = c64.Devices.GetAll<IecBusDevice>().Single();

        busDevice.Bus.Should().NotBeNull();
    }

    /// <summary>
    /// Acceptance: Drives 8 and 9 are present on the built C64 and attached to
    /// the always-on bus (the bus lists their endpoints).
    /// </summary>
    [Fact]
    public void BuiltC64_HasDrives8And9_OnTheBus()
    {
        var c64 = BuildC64();

        var driveNumbers = c64.Devices.GetAll<IecDrive>()
            .Select(d => d.DriveNumber)
            .OrderBy(n => n)
            .ToArray();
        driveNumbers.Should().Equal((byte)8, (byte)9);

        var bus = c64.Devices.GetAll<IecBusDevice>().Single().Bus;
        var endpoints = bus.Snapshot().Endpoints;
        endpoints.Should().Contain(e => e.Contains("8"));
        endpoints.Should().Contain(e => e.Contains("9"));
    }

    /// <summary>
    /// Acceptance: C64GS has no IEC bus (serial port not connected), so no
    /// IecBusDevice is registered.
    /// </summary>
    [Fact]
    public void C64Gs_HasNoIecBus()
    {
        var c64gs = BuildC64("c64gs");

        c64gs.Devices.GetAll<IecBusDevice>().Should().BeEmpty();
    }
}
