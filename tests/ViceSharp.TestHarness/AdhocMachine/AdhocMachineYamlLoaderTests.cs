using FluentAssertions;
using ViceSharp.Abstractions;
using ViceSharp.Architectures.Adhoc;
using ViceSharp.Chips.Audio;
using ViceSharp.Chips.Cia;
using ViceSharp.Chips.Cpu;
using ViceSharp.Chips.VicIi;
using ViceSharp.Core;
using Xunit;

namespace ViceSharp.TestHarness.AdhocMachine;

public sealed class AdhocMachineYamlLoaderTests
{
    private const string ValidMinimalYaml = """
        schemaVersion: 1
        machine:
          name: "Tiny"
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
        """;

    private static string SampleC64Path =>
        Path.Combine(SolutionRoot.Find(), "docs", "samples", "c64.machine.yaml");

    [Fact]
    public void LoadFromString_ValidYaml_ProducesBlueprintWithDescriptor()
    {
        var loader = new AdhocMachineYamlLoader();

        var blueprint = loader.LoadFromString(ValidMinimalYaml);

        blueprint.Should().NotBeNull();
        blueprint.Descriptor.MachineName.Should().Be("Tiny");
        blueprint.Descriptor.MasterClockHz.Should().Be(1000000);
        blueprint.Descriptor.VideoStandard.Should().Be(VideoStandard.Ntsc);
    }

    [Fact]
    public void LoadFromFile_SampleC64Yaml_RoundTripsBuilderEquivalentToHardcodedC64()
    {
        var loader = new AdhocMachineYamlLoader();

        var blueprint = loader.LoadFromFile(SampleC64Path);
        var machine = blueprint.BuildMachine(new ArchitectureBuilder());

        // Architecture descriptor matches the existing C64 builder
        machine.Architecture.MachineName.Should().Be("Commodore 64 PAL");
        machine.Architecture.MasterClockHz.Should().Be(985248);
        machine.Architecture.VideoStandard.Should().Be(VideoStandard.Pal);

        // The same chips that the hardcoded Commodore64 builder registers
        // (CPU, VIC-II, CIA1, CIA2, SID) must be present.
        var devices = machine.Devices.All;
        devices.OfType<Mos6502>().Should().HaveCount(1);
        devices.OfType<Mos6569>().Should().HaveCount(1);
        devices.OfType<Mos6526>().Should().HaveCount(2);
        devices.OfType<Sid6581>().Should().HaveCount(1);

        // CIA base addresses match the hardcoded layout.
        var cias = devices.OfType<Mos6526>().OrderBy(c => c.BaseAddress).ToList();
        cias[0].BaseAddress.Should().Be(0xDC00);
        cias[1].BaseAddress.Should().Be(0xDD00);

        // Memory map round trips the hardcoded RAM/ROM regions.
        // 0x0000-0xFFFF main RAM should be readable, 0xA000 BASIC ROM should be present, etc.
        // We verify by checking the bus responds (BasicBus returns 0xFF for unhandled addresses;
        // any registered region returns its backing byte which defaults to 0x00).
        machine.Bus.Read(0x0001).Should().Be(0x00, "main RAM is registered");
        machine.Bus.Read(0xA000).Should().Be(0x00, "BASIC ROM region is registered");
        machine.Bus.Read(0xE000).Should().Be(0x00, "KERNAL ROM region is registered");
        machine.Bus.Read(0xD800).Should().Be(0x00, "Color RAM is registered");
    }

    [Fact]
    public void LoadFromString_InvalidYaml_ThrowsValidationError()
    {
        var loader = new AdhocMachineYamlLoader();

        // Not valid YAML at all (unterminated map indicator).
        const string garbage = ": : not a yaml document : :";

        Action act = () => loader.LoadFromString(garbage);

        act.Should().Throw<AdhocMachineValidationException>()
            .WithMessage("*yaml*");
    }

    [Fact]
    public void LoadFromString_MissingRequiredField_ReportsFieldName()
    {
        var loader = new AdhocMachineYamlLoader();

        // Missing machine.videoStandard.
        const string yaml = """
            schemaVersion: 1
            machine:
              name: "NoVideo"
              masterClockHz: 1000000
            memory:
              regions:
                - id: ram
                  kind: Ram
                  start: 0
                  end: 0xFFFF
            chips:
              - id: cpu
                type: Mos6502
            """;

        Action act = () => loader.LoadFromString(yaml);

        act.Should().Throw<AdhocMachineValidationException>()
            .WithMessage("*machine.videoStandard*");
    }

    [Fact]
    public void LoadFromString_CiaWithoutBaseAddress_ReportsFieldName()
    {
        var loader = new AdhocMachineYamlLoader();

        const string yaml = """
            schemaVersion: 1
            machine:
              name: "BadCia"
              videoStandard: Pal
              masterClockHz: 985248
            memory:
              regions:
                - id: ram
                  kind: Ram
                  start: 0
                  end: 0xFFFF
            chips:
              - id: cpu
                type: Mos6502
              - id: cia1
                type: Mos6526
            """;

        Action act = () => loader.LoadFromString(yaml);

        act.Should().Throw<AdhocMachineValidationException>()
            .WithMessage("*chips[1].baseAddress*");
    }

    [Fact]
    public void LoadFromString_RegionSizeMismatch_ReportsFieldName()
    {
        var loader = new AdhocMachineYamlLoader();

        const string yaml = """
            schemaVersion: 1
            machine:
              name: "BadSize"
              videoStandard: Pal
              masterClockHz: 985248
            memory:
              regions:
                - id: ram
                  kind: Ram
                  start: 0
                  end: 0xFFFF
                  size: 100
            chips:
              - id: cpu
                type: Mos6502
            """;

        Action act = () => loader.LoadFromString(yaml);

        act.Should().Throw<AdhocMachineValidationException>()
            .WithMessage("*memory.regions[0].size*");
    }

    [Fact]
    public void LoadFromString_UnsupportedSchemaVersion_Throws()
    {
        var loader = new AdhocMachineYamlLoader();

        const string yaml = """
            schemaVersion: 999
            machine:
              name: "Future"
              videoStandard: Pal
              masterClockHz: 1
            memory:
              regions:
                - id: ram
                  kind: Ram
                  start: 0
                  end: 0xFFFF
            chips:
              - id: cpu
                type: Mos6502
            """;

        Action act = () => loader.LoadFromString(yaml);

        act.Should().Throw<AdhocMachineValidationException>()
            .WithMessage("*schemaVersion*");
    }
}
