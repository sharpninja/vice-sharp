namespace ViceSharp.TestHarness.Wiring;

using FluentAssertions;
using ViceSharp.Abstractions;
using ViceSharp.Architectures.Multisystem;
using ViceSharp.Chips.Cia;
using ViceSharp.Chips.IEC;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: ARCH-WIRING-004 (Phase F4).
/// Use case: A YAML topology names architectures with `kind:` instead of
/// supplying inline machine YAML. BuildCoordinatorAuto dispatches to the
/// real ArchitectureBuilder route + auto-binds chips to canonical buses.
/// Result - a single YAML file fully describes a working host+drive
/// substrate with zero code in the caller.
/// </summary>
public sealed class MultiSystemKindDispatchTests
{
    private const string KindTopology = """
        schemaVersion: 1
        coordinator:
          host:
            id: c64-host
            kind: C64
            busAttachments:
              - busId: IEC
                endpointName: c64
          peripherals:
            - id: drive-8
              kind: C1541
              deviceNumber: 8
              busAttachments:
                - busId: IEC
                  endpointName: drive-8
            - id: drive-9
              kind: C1541
              deviceNumber: 9
              busAttachments:
                - busId: IEC
                  endpointName: drive-9
          buses:
            - id: IEC
              signals: [ATN, CLK, DATA, SRQ]
        """;

    private static MultiSystemBuildResult LoadAuto(string yaml)
    {
        var provider = MachineTestFactory.CreateC64RomProvider();
        var bp = new MultiSystemYamlLoader().LoadFromString(yaml);
        return bp.BuildCoordinatorAuto(new ArchitectureBuilder(provider));
    }

    /// <summary>
    /// FR/TR: ARCH-WIRING-004
    /// Use case: YAML names "C64" + "C1541" kinds; loader builds real
    /// architectures and the substrate auto-binds chips.
    /// Acceptance: After BuildCoordinatorAuto, systems by id contain real
    /// C64 + 1541 machines (architecture names match).
    /// </summary>
    [Fact]
    public void Kind_BuildsRealC64AndC1541Architectures()
    {
        var build = LoadAuto(KindTopology);

        build.SystemsById["c64-host"].Architecture.MachineName.Should().StartWith("Commodore 64");
        build.SystemsById["drive-8"].Architecture.MachineName.Should().Be("Commodore 1541");
        build.SystemsById["drive-9"].Architecture.MachineName.Should().Be("Commodore 1541");
    }

    /// <summary>
    /// FR/TR: ARCH-WIRING-004
    /// Use case: kind-built drive uses the deviceNumber from the YAML.
    /// Acceptance: drive-9 reports DeviceNumber=9; PB5/PB6 jumper encoding 01.
    /// </summary>
    [Fact]
    public void Kind_HonorsDeviceNumber_FromYaml()
    {
        var build = LoadAuto(KindTopology);

        var drive9 = build.SystemsById["drive-9"];
        ((IDriveArchitectureDescriptor)drive9.Architecture).DeviceNumber.Should().Be(9);

        var drive9Via1 = drive9.Devices.GetAll<Via6522>().OrderBy(v => v.BaseAddress).First();
        drive9Via1.Write(0x1802, 0x1A);
        (drive9Via1.Read(0x1800) & 0x60).Should().Be(0x20);
    }

    /// <summary>
    /// FR/TR: ARCH-WIRING-004
    /// Use case: Auto-bind still fires on the kind-built machines; the C64
    /// asserts ATN and both drives observe it.
    /// Acceptance: Host CIA2 PA3 high -> drive-8 PB7 = 1 + drive-9 PB7 = 1.
    /// </summary>
    [Fact]
    public void Kind_AutoBind_HostAssertsAtn_BothDrivesSeeIt()
    {
        var build = LoadAuto(KindTopology);
        var hostCia2 = (Mos6526)build.SystemsById["c64-host"].Devices.GetByRole(DeviceRole.Cia2)!;

        hostCia2.Write(0xDD00, 0x08);
        hostCia2.Write(0xDD02, 0x38);

        var drive8Pb = build.SystemsById["drive-8"].Devices.GetAll<Via6522>()
            .OrderBy(v => v.BaseAddress).First().Read(0x1800);
        var drive9Pb = build.SystemsById["drive-9"].Devices.GetAll<Via6522>()
            .OrderBy(v => v.BaseAddress).First().Read(0x1800);
        (drive8Pb & 0x80).Should().Be(0x80);
        (drive9Pb & 0x80).Should().Be(0x80);
    }

    /// <summary>
    /// FR/TR: ARCH-WIRING-004
    /// Use case: BuildCoordinatorAuto preserves the yaml-inline fallback -
    /// if a peripheral declares yamlInline without a kind, it builds via
    /// the adhoc loader.
    /// Acceptance: A peripheral with adhoc yamlInline still builds without
    /// throwing.
    /// </summary>
    [Fact]
    public void Auto_FallsBack_ToAdhocLoader_WhenNoKind()
    {
        var yaml = """
            schemaVersion: 1
            coordinator:
              host:
                id: c64-host
                kind: C64
                busAttachments:
                  - busId: IEC
                    endpointName: c64
              peripherals:
                - id: peer
                  yamlInline: |
                    schemaVersion: 1
                    machine:
                      name: Peer
                      videoStandard: Ntsc
                      masterClockHz: 1000000
                    memory:
                      regions:
                        - id: ram-main
                          kind: Ram
                          start: 0x0000
                          end:   0xFFFF
                    chips:
                      - id: cpu
                        type: Mos6502
                        role: Cpu
                  busAttachments:
                    - busId: IEC
                      endpointName: peer
              buses:
                - id: IEC
                  signals: [ATN, CLK, DATA, SRQ]
            """;

        var build = LoadAuto(yaml);

        build.SystemsById["peer"].Architecture.MachineName.Should().Be("Peer");
    }

    /// <summary>
    /// FR/TR: ARCH-WIRING-004
    /// Use case: A peripheral with neither kind nor yaml is rejected at
    /// load time (not at BuildCoordinatorAuto time).
    /// Acceptance: Loading a spec with empty peripheral throws clear error.
    /// </summary>
    [Fact]
    public void Validation_PeripheralWithoutKindOrYaml_Throws()
    {
        var yaml = """
            schemaVersion: 1
            coordinator:
              host:
                id: c64-host
                kind: C64
              peripherals:
                - id: nothing
              buses: []
            """;

        var loader = new MultiSystemYamlLoader();
        var ex = Assert.Throws<MultiSystemValidationException>(() => loader.LoadFromString(yaml));
        ex.Message.Should().Contain("nothing");
    }
}
