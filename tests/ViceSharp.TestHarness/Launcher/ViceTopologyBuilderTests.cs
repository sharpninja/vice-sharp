namespace ViceSharp.TestHarness.Launcher;

using FluentAssertions;
using ViceSharp.Launcher;
using Xunit;

/// <summary>
/// FR/TR: CLI-LAUNCHER-001.
/// Use case: VICE-style CLI invocations need to map to a multi-system
/// topology YAML the substrate can consume. ViceTopologyBuilder is the
/// translator.
/// </summary>
public sealed class ViceTopologyBuilderTests
{
    /// <summary>
    /// FR/TR: CLI-LAUNCHER-001
    /// Use case: x64sc with no peripherals builds a minimal host-only YAML.
    /// Acceptance: YAML contains kind: C64 + no peripherals + no buses.
    /// </summary>
    [Fact]
    public void X64sc_NoPeripherals_HostOnly()
    {
        var args = ViceArgsParser.Parse("x64sc", Array.Empty<string>());

        var yaml = ViceTopologyBuilder.BuildYaml(args);

        yaml.Should().Contain("kind: C64");
        yaml.Should().NotContain("peripherals:");
        yaml.Should().NotContain("buses:");
    }

    /// <summary>
    /// FR/TR: CLI-LAUNCHER-001
    /// Use case: x64sc -8 path/d64 attaches a drive 8 with the supplied
    /// disk image and an IEC bus.
    /// Acceptance: YAML mentions drive-8 + diskImagePath + IEC bus.
    /// </summary>
    [Fact]
    public void X64sc_With_Drive8_Image()
    {
        var args = ViceArgsParser.Parse("x64sc", new[] { "-8", "game.d64" });

        var yaml = ViceTopologyBuilder.BuildYaml(args);

        yaml.Should().Contain("- id: drive-8");
        yaml.Should().Contain("deviceNumber: 8");
        yaml.Should().Contain("diskImagePath: game.d64");
        yaml.Should().Contain("- id: IEC");
    }

    /// <summary>
    /// FR/TR: CLI-LAUNCHER-001
    /// Use case: x64sc with both -8 + -9 attaches two drives.
    /// Acceptance: YAML has drive-8 + drive-9 + IEC bus.
    /// </summary>
    [Fact]
    public void X64sc_With_BothDrives()
    {
        var args = ViceArgsParser.Parse("x64sc",
            new[] { "-8", "first.d64", "-9", "second.d64" });

        var yaml = ViceTopologyBuilder.BuildYaml(args);

        yaml.Should().Contain("drive-8");
        yaml.Should().Contain("drive-9");
        yaml.Should().Contain("first.d64");
        yaml.Should().Contain("second.d64");
    }

    /// <summary>
    /// FR/TR: CLI-LAUNCHER-001
    /// Use case: c1541 standalone tool mounts a D64 directly without an
    /// IEC bus or host.
    /// Acceptance: YAML host kind=C1541 with diskImagePath, no peripherals.
    /// </summary>
    [Fact]
    public void C1541_StandaloneTool_MountsImage()
    {
        var args = ViceArgsParser.Parse("c1541", new[] { "-8", "tools.d64" });

        var yaml = ViceTopologyBuilder.BuildYaml(args);

        yaml.Should().Contain("kind: C1541");
        yaml.Should().Contain("tools.d64");
    }

    /// <summary>
    /// FR/TR: CLI-LAUNCHER-001
    /// Use case: Unsupported binaries throw a clear error rather than
    /// generating broken YAML.
    /// Acceptance: x128 / xvic / etc. throw NotSupportedException.
    /// </summary>
    [Theory]
    [InlineData("x128")]
    [InlineData("xvic")]
    [InlineData("xpet")]
    public void UnsupportedBinary_Throws(string binaryName)
    {
        var args = ViceArgsParser.Parse(binaryName, Array.Empty<string>());

        Assert.Throws<NotSupportedException>(() => ViceTopologyBuilder.BuildYaml(args));
    }

    /// <summary>
    /// FR/TR: CLI-LAUNCHER-001
    /// Use case: When --machine-yaml is supplied, the topology is read from
    /// that file verbatim regardless of binary name.
    /// Acceptance: Parser + builder honour --machine-yaml.
    /// </summary>
    [Fact]
    public void MachineYamlFlag_OverridesBinaryDispatch()
    {
        var path = Path.Combine(Path.GetTempPath(), $"vice-yaml-{Guid.NewGuid():N}.yaml");
        File.WriteAllText(path, "schemaVersion: 1\ncoordinator:\n  host:\n    id: x\n    kind: C64");
        try
        {
            var args = ViceArgsParser.Parse("x64sc", new[] { "--machine-yaml", path });

            var yaml = ViceTopologyBuilder.BuildYaml(args);

            yaml.Should().Contain("id: x");
        }
        finally
        {
            File.Delete(path);
        }
    }

    // =====================================================================
    // Slice 6B: ARCH-TESTBENCH-001 - ParseDescriptor with debugcart/limitcycles
    // =====================================================================

    /// <summary>
    /// ARCH-TESTBENCH-001.
    /// Use case: A YAML topology with testbench keys (debugcart + limitcycles)
    /// should produce a descriptor with those fields populated.
    /// Acceptance: ViceTopologyBuilder.ParseDescriptor(yaml).DebugCart == true,
    /// .LimitCycles == 50000.
    /// </summary>
    [Fact]
    public void ParseDescriptor_WithDebugCartAndLimitCycles_PopulatesFields()
    {
        var yaml = """
            schemaVersion: 1
            debugcart: true
            limitcycles: 50000
            coordinator:
              host:
                id: c64-host
                kind: C64
            """;

        var descriptor = ViceTopologyBuilder.ParseDescriptor(yaml);

        descriptor.DebugCart.Should().BeTrue();
        descriptor.LimitCycles.Should().Be(50000);
    }

    /// <summary>
    /// ARCH-TESTBENCH-001.
    /// Use case: A YAML topology without testbench keys should have null
    /// DebugCart and LimitCycles fields.
    /// Acceptance: ParseDescriptor returns descriptor with null fields when
    /// no testbench keys present.
    /// </summary>
    [Fact]
    public void ParseDescriptor_WithoutTestbenchKeys_HasNullFields()
    {
        var yaml = """
            schemaVersion: 1
            coordinator:
              host:
                id: c64-host
                kind: C64
            """;

        var descriptor = ViceTopologyBuilder.ParseDescriptor(yaml);

        descriptor.DebugCart.Should().BeNull();
        descriptor.LimitCycles.Should().BeNull();
    }
}
