namespace ViceSharp.TestHarness.Multisystem;

using FluentAssertions;
using ViceSharp.Architectures.Multisystem;
using ViceSharp.Core;
using ViceSharp.TestHarness.AdhocMachine;
using Xunit;

/// <summary>
/// Contract tests for the multi-system YAML loader. Validates parse +
/// schema-validation + BuildCoordinator wiring against the topology graph.
///
/// FR/TR: ARCH-MULTISYSTEM-001 (Phase A3b - multi-system YAML schema).
/// Use case: An author writes a YAML topology with host + peripherals + bus;
/// the console host loads it and gets a fully-attached coordinator.
/// </summary>
public sealed class MultiSystemYamlLoaderTests
{
    private const string MinimalPeerInline = """
        schemaVersion: 1
        machine:
          name: "Peer"
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

    private static string SampleMultisystemPath =>
        Path.Combine(SolutionRoot.Find(), "docs", "samples", "c64-plus-peer.multisystem.yaml");

    /// <summary>
    /// FR/TR: ARCH-MULTISYSTEM-001
    /// Use case: Detect whether a YAML file is multi-system or single-machine.
    /// Acceptance: IsMultiSystemText returns true for documents with a
    /// coordinator: key, false otherwise.
    /// </summary>
    [Fact]
    public void IsMultiSystemText_DetectsCoordinatorKey()
    {
        var loader = new MultiSystemYamlLoader();

        loader.IsMultiSystemText("coordinator:\n  host: { id: x }").Should().BeTrue();
        loader.IsMultiSystemText("# comment\ncoordinator:").Should().BeTrue();
        loader.IsMultiSystemText("schemaVersion: 1\nmachine:\n  name: foo").Should().BeFalse();
        loader.IsMultiSystemText("").Should().BeFalse();
    }

    /// <summary>
    /// FR/TR: ARCH-MULTISYSTEM-001
    /// Use case: Loader parses a valid topology with host + one peripheral +
    /// one bus and surfaces the topology ids.
    /// Acceptance: Blueprint exposes host id, peripheral ids, bus ids.
    /// </summary>
    [Fact]
    public void LoadFromString_ValidTopology_ExposesTopologyIds()
    {
        var yaml = $$"""
            schemaVersion: 1
            coordinator:
              host:
                id: c64-host
                yamlInline: |
            {{Indent(MinimalPeerInline, 6)}}
                busAttachments:
                  - busId: iec
                    endpointName: c64
              peripherals:
                - id: drive-8
                  role: Independent
                  yamlInline: |
            {{Indent(MinimalPeerInline, 8)}}
                  busAttachments:
                    - busId: iec
                      endpointName: drive-8
              buses:
                - id: iec
                  signals: [ATN, CLK, DATA]
            """;
        var loader = new MultiSystemYamlLoader();

        var bp = loader.LoadFromString(yaml);

        bp.HostId.Should().Be("c64-host");
        bp.PeripheralIds.Should().BeEquivalentTo(new[] { "drive-8" });
        bp.CartExtensionIds.Should().BeEmpty();
        bp.BusIds.Should().BeEquivalentTo(new[] { "iec" });
    }

    /// <summary>
    /// FR/TR: ARCH-MULTISYSTEM-001
    /// Use case: Sample docs/samples/c64-plus-peer.multisystem.yaml round-trips
    /// through the loader and builds a coordinator with two attached machines
    /// + one IEC bus + two endpoints.
    /// Acceptance: After Step(100), both machines have advanced; endpoint
    /// lookups return the registered IBusEndpoint instances.
    /// </summary>
    [Fact]
    public void LoadFromFile_SampleMultisystem_BuildsCoordinatorAndAdvances()
    {
        var loader = new MultiSystemYamlLoader();

        var bp = loader.LoadFromFile(SampleMultisystemPath);
        var build = bp.BuildCoordinatorWithAdhocLoader(new ArchitectureBuilder());

        build.SystemsById.Should().ContainKeys("c64-host", "peer-drive");
        build.BusesById.Should().ContainKey("iec");
        build.Endpoints.Should().ContainKeys(
            ("c64-host", "iec"),
            ("peer-drive", "iec"));

        build.Coordinator.Step(100);
        build.SystemsById["c64-host"].Clock.TotalCycles.Should().Be(100);
        build.SystemsById["peer-drive"].Clock.TotalCycles.Should().BeGreaterThan(100);
    }

    /// <summary>
    /// FR/TR: ARCH-MULTISYSTEM-001
    /// Use case: Bus endpoints registered via the loader behave like
    /// directly-constructed InterSystemBus endpoints.
    /// Acceptance: Host endpoint pulls IEC DATA low; peer endpoint reads low.
    /// </summary>
    [Fact]
    public void Endpoints_FromLoadedTopology_PropagateSignals()
    {
        var loader = new MultiSystemYamlLoader();
        var bp = loader.LoadFromFile(SampleMultisystemPath);
        var build = bp.BuildCoordinatorWithAdhocLoader(new ArchitectureBuilder());

        var hostEp = build.Endpoints[("c64-host", "iec")];
        var peerEp = build.Endpoints[("peer-drive", "iec")];

        hostEp.Pull("DATA", low: true);

        peerEp.ReadLine("DATA").Should().BeFalse();
        build.BusesById["iec"].ReadLine("DATA").Should().BeFalse();
    }

    /// <summary>
    /// FR/TR: ARCH-MULTISYSTEM-001
    /// Use case: Missing schemaVersion is rejected.
    /// Acceptance: Throws MultiSystemValidationException.
    /// </summary>
    [Fact]
    public void Validation_MissingSchemaVersion_Throws()
    {
        var loader = new MultiSystemYamlLoader();
        var yaml = "coordinator:\n  host: { id: h, yamlInline: \"x\" }";

        Assert.Throws<MultiSystemValidationException>(() => loader.LoadFromString(yaml));
    }

    /// <summary>
    /// FR/TR: ARCH-MULTISYSTEM-001
    /// Use case: Missing coordinator section is rejected.
    /// Acceptance: Throws MultiSystemValidationException.
    /// </summary>
    [Fact]
    public void Validation_MissingCoordinator_Throws()
    {
        var loader = new MultiSystemYamlLoader();

        Assert.Throws<MultiSystemValidationException>(() => loader.LoadFromString("schemaVersion: 1"));
    }

    /// <summary>
    /// FR/TR: ARCH-MULTISYSTEM-001
    /// Use case: A peripheral references a bus that doesn't exist.
    /// Acceptance: Throws MultiSystemValidationException naming the offending
    /// system + bus.
    /// </summary>
    [Fact]
    public void Validation_UnknownBusReference_Throws()
    {
        var loader = new MultiSystemYamlLoader();
        var yaml = $$"""
            schemaVersion: 1
            coordinator:
              host:
                id: c64-host
                yamlInline: |
            {{Indent(MinimalPeerInline, 6)}}
              peripherals:
                - id: drive-8
                  yamlInline: |
            {{Indent(MinimalPeerInline, 8)}}
                  busAttachments:
                    - busId: nonexistent-bus
                      endpointName: drive-8
              buses:
                - id: iec
                  signals: [ATN, CLK, DATA]
            """;

        var ex = Assert.Throws<MultiSystemValidationException>(() => loader.LoadFromString(yaml));
        ex.Message.Should().Contain("drive-8");
        ex.Message.Should().Contain("nonexistent-bus");
    }

    /// <summary>
    /// FR/TR: ARCH-MULTISYSTEM-001
    /// Use case: Duplicate system ids would corrupt the topology graph.
    /// Acceptance: Throws MultiSystemValidationException.
    /// </summary>
    [Fact]
    public void Validation_DuplicateSystemId_Throws()
    {
        var loader = new MultiSystemYamlLoader();
        var yaml = $$"""
            schemaVersion: 1
            coordinator:
              host:
                id: same-id
                yamlInline: |
            {{Indent(MinimalPeerInline, 6)}}
              peripherals:
                - id: same-id
                  yamlInline: |
            {{Indent(MinimalPeerInline, 8)}}
              buses: []
            """;

        Assert.Throws<MultiSystemValidationException>(() => loader.LoadFromString(yaml));
    }

    /// <summary>
    /// FR/TR: ARCH-MULTISYSTEM-001
    /// Use case: A machine spec providing neither yamlPath nor yamlInline is
    /// invalid.
    /// Acceptance: Throws MultiSystemValidationException.
    /// </summary>
    [Fact]
    public void Validation_MachineSpecWithoutYaml_Throws()
    {
        var loader = new MultiSystemYamlLoader();
        var yaml = """
            schemaVersion: 1
            coordinator:
              host:
                id: c64-host
              buses: []
            """;

        Assert.Throws<MultiSystemValidationException>(() => loader.LoadFromString(yaml));
    }

    private static string Indent(string text, int spaces)
    {
        var pad = new string(' ', spaces);
        return string.Join('\n', text.Split('\n').Select(l => pad + l));
    }
}
