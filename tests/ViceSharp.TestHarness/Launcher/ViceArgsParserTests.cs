namespace ViceSharp.TestHarness.Launcher;

using FluentAssertions;
using ViceSharp.Launcher;
using Xunit;

/// <summary>
/// FR/TR: CLI-LAUNCHER-001.
/// Use case: VICE-style command-line invocations like
/// "x64sc -8 disk.d64 +truedrive --cycles 100000" must parse correctly
/// into a ViceArgs bundle that the substrate can act on.
/// </summary>
public sealed class ViceArgsParserTests
{
    /// <summary>
    /// FR/TR: CLI-LAUNCHER-001
    /// Use case: Binary name is normalised to lowercase, extension stripped.
    /// Acceptance: "x64sc.exe" -> "x64sc"; "C1541" -> "c1541".
    /// </summary>
    [Theory]
    [InlineData("x64sc.exe", "x64sc")]
    [InlineData("C1541", "c1541")]
    [InlineData("/usr/local/bin/x64", "x64")]
    public void BinaryName_Normalised_LowercaseNoExtension(string input, string expected)
    {
        var args = ViceArgsParser.Parse(input, Array.Empty<string>());
        args.BinaryName.Should().Be(expected);
    }

    /// <summary>
    /// FR/TR: CLI-LAUNCHER-001
    /// Use case: -8 + -9 attach drives.
    /// Acceptance: Parser captures both image paths.
    /// </summary>
    [Fact]
    public void DriveFlags_AttachImages()
    {
        var args = ViceArgsParser.Parse("x64sc",
            new[] { "-8", "game.d64", "-9", "tools.d64" });
        args.Drive8Image.Should().Be("game.d64");
        args.Drive9Image.Should().Be("tools.d64");
    }

    /// <summary>
    /// FR/TR: CLI-LAUNCHER-001
    /// Use case: +/-truedrive flips the TrueDrive setting.
    /// Acceptance: +truedrive -> true; -truedrive -> false; absent -> null.
    /// </summary>
    [Theory]
    [InlineData(new[] { "+truedrive" }, true)]
    [InlineData(new[] { "-truedrive" }, false)]
    [InlineData(new string[0], null)]
    public void TrueDrive_FlagToggle(string[] args, bool? expected)
    {
        var parsed = ViceArgsParser.Parse("x64sc", args);
        parsed.TrueDrive.Should().Be(expected);
    }

    /// <summary>
    /// FR/TR: CLI-LAUNCHER-001
    /// Use case: --machine-yaml accepts both space-separated and = forms.
    /// Acceptance: Both forms set the path.
    /// </summary>
    [Theory]
    [InlineData(new[] { "--machine-yaml", "topo.yaml" }, "topo.yaml")]
    [InlineData(new[] { "--machine-yaml=topo.yaml" }, "topo.yaml")]
    [InlineData(new[] { "-m", "topo.yaml" }, "topo.yaml")]
    public void MachineYaml_AcceptsSpaceAndEquals(string[] args, string expected)
    {
        var parsed = ViceArgsParser.Parse("x64sc", args);
        parsed.MachineYamlPath.Should().Be(expected);
    }

    /// <summary>
    /// FR/TR: CLI-LAUNCHER-001
    /// Use case: --cycles N + --cycles=N parse the number.
    /// Acceptance: Both forms produce equal Cycles values; non-numeric goes
    /// to Unknown.
    /// </summary>
    [Fact]
    public void Cycles_NumericFlag_Parses()
    {
        ViceArgsParser.Parse("x64sc", new[] { "--cycles", "100000" }).Cycles.Should().Be(100_000);
        ViceArgsParser.Parse("x64sc", new[] { "--cycles=2000" }).Cycles.Should().Be(2000);
        ViceArgsParser.Parse("x64sc", new[] { "--cycles", "abc" }).Unknown.Should().Contain(s => s.Contains("--cycles"));
    }

    /// <summary>
    /// FR/TR: CLI-LAUNCHER-001
    /// Use case: Help flags collected for downstream Program to render usage.
    /// Acceptance: --help, -h, -? all set ShowHelp.
    /// </summary>
    [Theory]
    [InlineData("--help")]
    [InlineData("-h")]
    [InlineData("-?")]
    public void HelpFlags_SetShowHelp(string flag)
    {
        var parsed = ViceArgsParser.Parse("x64sc", new[] { flag });
        parsed.ShowHelp.Should().BeTrue();
    }

    /// <summary>
    /// FR/TR: CLI-LAUNCHER-001
    /// Use case: Unknown flags are collected, not thrown.
    /// Acceptance: -junk lands in Unknown; parsing succeeds.
    /// </summary>
    [Fact]
    public void UnknownFlags_AreCollected()
    {
        var parsed = ViceArgsParser.Parse("x64sc", new[] { "-junk", "--also-junk", "-cart", "ROM.crt" });
        parsed.Unknown.Should().Contain(new[] { "-junk", "--also-junk" });
        parsed.CartridgeImage.Should().Be("ROM.crt");
    }

    /// <summary>
    /// FR/TR: CLI-LAUNCHER-001
    /// Use case: Verbose alias.
    /// Acceptance: -v and --verbose both set Verbose.
    /// </summary>
    [Theory]
    [InlineData("-v")]
    [InlineData("--verbose")]
    public void Verbose_AliasFlags(string flag)
    {
        ViceArgsParser.Parse("x64sc", new[] { flag }).Verbose.Should().BeTrue();
    }
}
