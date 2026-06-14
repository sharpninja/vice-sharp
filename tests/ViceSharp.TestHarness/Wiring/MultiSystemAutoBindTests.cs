namespace ViceSharp.TestHarness.Wiring;

using FluentAssertions;
using ViceSharp.Abstractions;
using ViceSharp.Architectures.C1541;
using ViceSharp.Architectures.C64;
using ViceSharp.Architectures.Multisystem;
using ViceSharp.Chips.Cia;
using ViceSharp.Chips.IEC;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: ARCH-WIRING-003 (Phase F3).
/// Use case: A multi-system YAML topology declares a C64 host + a 1541
/// drive attached to an IEC bus. Loading the topology + calling
/// BuildCoordinator auto-wires CIA2 (host) and VIA1 (drive) to their
/// respective IEC endpoints without any manual Bind() call. The user
/// gets a fully-functional substrate from YAML alone.
/// </summary>
public sealed class MultiSystemAutoBindTests
{
    private static string Topology(string busId = "IEC") =>
        $$"""
        schemaVersion: 1
        coordinator:
          host:
            id: c64-host
            yamlInline: |
              schemaVersion: 1
              machine:
                name: x
                videoStandard: Pal
                masterClockHz: 985248
            busAttachments:
              - busId: {{busId}}
                endpointName: c64
          peripherals:
            - id: drive-8
              yamlInline: |
                schemaVersion: 1
                machine:
                  name: x
                  videoStandard: Pal
                  masterClockHz: 1000000
              busAttachments:
                - busId: {{busId}}
                  endpointName: drive-8
          buses:
            - id: {{busId}}
              signals: [ATN, CLK, DATA, SRQ]
        """;

    private static MultiSystemBuildResult LoadWithRealMachines(string yaml)
    {
        var provider = MachineTestFactory.CreateC64RomProvider();
        var bp = new MultiSystemYamlLoader().LoadFromString(yaml);
        var builder = new ArchitectureBuilder(provider);
        return bp.BuildCoordinator(builder, (systemId, _) => systemId switch
        {
            "c64-host" => new ArchitectureBuilder(provider).Build(new C64Descriptor()),
            "drive-8" => new ArchitectureBuilder(provider).Build(new C1541Descriptor(deviceNumber: 8)),
            "drive-9" => new ArchitectureBuilder(provider).Build(new C1541Descriptor(deviceNumber: 9)),
            _ => throw new InvalidOperationException($"unmapped system id '{systemId}'"),
        });
    }

    /// <summary>
    /// FR/TR: ARCH-WIRING-003
    /// Use case: After BuildCoordinator on a YAML topology with a real C64
    /// + 1541 + IEC bus, the host CIA2 + drive VIA1 are auto-bound to the
    /// shared IEC endpoint without any explicit Bind call.
    /// Acceptance: Driving CIA2 PA3 high on the host (asserts ATN via CIA2)
    /// causes the drive's VIA1 PB7 to read 1 (drive sees ATN).
    /// </summary>
    [Fact]
    public void AutoBind_C64Plus1541_HostAssertsAtn_DriveSeesIt()
    {
        var build = LoadWithRealMachines(Topology());
        var hostCia2 = (Mos6526)build.SystemsById["c64-host"].Devices.GetByRole(DeviceRole.Cia2)!;
        var driveVia1 = build.SystemsById["drive-8"].Devices.GetAll<Via6522>()
            .OrderBy(v => v.BaseAddress).First();

        hostCia2.Write(0xDD00, 0x08);
        hostCia2.Write(0xDD02, 0x38);

        (driveVia1.Read(0x1800) & 0x80).Should().Be(0x80);
    }

    /// <summary>
    /// FR/TR: ARCH-WIRING-003
    /// Use case: Drive responds by asserting DATA via VIA1 PB1; host CIA2
    /// reads DATA pulled (PA7 = 0, matching VICE cpu_port polarity).
    /// Acceptance: VIA1 write $1800 = $02 -> CIA2 PA7 reads 0.
    /// </summary>
    [Fact]
    public void AutoBind_DriveAssertsData_HostCia2SeesIt()
    {
        var build = LoadWithRealMachines(Topology());
        var hostCia2 = (Mos6526)build.SystemsById["c64-host"].Devices.GetByRole(DeviceRole.Cia2)!;
        var driveVia1 = build.SystemsById["drive-8"].Devices.GetAll<Via6522>()
            .OrderBy(v => v.BaseAddress).First();
        driveVia1.Write(0x1802, 0x1A); // DDRB: PB1/PB3/PB4 outputs

        driveVia1.Write(0x1800, 0x02); // PB1 = 1 -> assert DATA

        (hostCia2.Read(0xDD00) & 0x80).Should().Be(0x00);
    }

    /// <summary>
    /// FR/TR: ARCH-WIRING-003
    /// Use case: The auto-bind reads device number from the drive's
    /// IDriveArchitectureDescriptor; a drive-9 has device-address jumper
    /// bits encoded as 01 in PB5..PB6.
    /// Acceptance: With drive-9, drive's VIA1 PB & $60 = $20.
    /// </summary>
    [Fact]
    public void AutoBind_HonorsDeviceNumberFromDescriptor()
    {
        var topology = Topology().Replace("drive-8", "drive-9");
        var build = LoadWithRealMachines(topology);
        var driveVia1 = build.SystemsById["drive-9"].Devices.GetAll<Via6522>()
            .OrderBy(v => v.BaseAddress).First();
        driveVia1.Write(0x1802, 0x1A);

        var pb = driveVia1.Read(0x1800);

        (pb & 0x60).Should().Be(0x20);
    }

    /// <summary>
    /// FR/TR: ARCH-WIRING-003
    /// Use case: After auto-bind, the coordinator drives both machines
    /// concurrently; a 100k host step run advances both machines on their
    /// own clocks and the IEC bus stays operable.
    /// Acceptance: After Step(100_000), host = 100_000, drive within 1 of
    /// 101_499 (1000000 * 100000 / 985248), and CIA2 -> drive ATN still works.
    /// </summary>
    [Fact]
    public void AutoBind_CoordinatorDrivesBoth_AndBusStaysOperable()
    {
        var build = LoadWithRealMachines(Topology());
        var c64 = build.SystemsById["c64-host"];
        var drive = build.SystemsById["drive-8"];
        c64.Reset();
        drive.Reset();

        build.Coordinator.Step(100_000);

        c64.Clock.TotalCycles.Should().Be(100_000);
        var expectedDrive = 100_000L * 1_000_000 / 985_248;
        System.Math.Abs(drive.Clock.TotalCycles - expectedDrive).Should().BeLessThanOrEqualTo(1);

        var hostCia2 = (Mos6526)c64.Devices.GetByRole(DeviceRole.Cia2)!;
        var driveVia1 = drive.Devices.GetAll<Via6522>().OrderBy(v => v.BaseAddress).First();
        hostCia2.Write(0xDD00, 0x08);
        hostCia2.Write(0xDD02, 0x38);
        (driveVia1.Read(0x1800) & 0x80).Should().Be(0x80);
    }

    /// <summary>
    /// FR/TR: ARCH-WIRING-003
    /// Use case: A bus with a non-canonical name (not "IEC") doesn't trigger
    /// auto-bind. Lets users define custom buses without surprise wiring.
    /// Acceptance: With a "MyCustomBus" bus, the drive's VIA1 PortBInput
    /// stays the unwired default (returns 0xFF) regardless of host CIA2
    /// writes - toggling CIA2 PA does NOT change what the drive reads.
    /// </summary>
    [Fact]
    public void AutoBind_DoesNotFire_OnNonCanonicalBusName()
    {
        var build = LoadWithRealMachines(Topology(busId: "MyCustomBus"));
        var hostCia2 = (Mos6526)build.SystemsById["c64-host"].Devices.GetByRole(DeviceRole.Cia2)!;
        var driveVia1 = build.SystemsById["drive-8"].Devices.GetAll<Via6522>()
            .OrderBy(v => v.BaseAddress).First();
        driveVia1.Write(0x1802, 0x00); // DDRB all inputs

        hostCia2.Write(0xDD00, 0x00);
        hostCia2.Write(0xDD02, 0x38);
        var pbIdle = driveVia1.Read(0x1800);
        hostCia2.Write(0xDD00, 0x08);
        var pbAfterAtnWrite = driveVia1.Read(0x1800);

        pbIdle.Should().Be(pbAfterAtnWrite);
    }
}
