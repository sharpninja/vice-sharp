using FluentAssertions;
using ViceSharp.Architectures.Adhoc;
using ViceSharp.Core;
using Xunit;

namespace ViceSharp.TestHarness.AdhocMachine;

/// <summary>
/// Smoke tests that exercise the same code path the Console CLI uses when
/// `--machine-yaml <path>` is supplied. These verify the loader integrates with
/// `ArchitectureBuilder` and produces a runnable machine.
/// </summary>
public sealed class ConsoleAdhocCliTests
{
    private static string SampleC64Path =>
        Path.Combine(SolutionRoot.Find(), "docs", "samples", "c64.machine.yaml");

    [Fact]
    public void LoadAndBuild_SampleC64_RunsFewClockTicks()
    {
        var loader = new AdhocMachineYamlLoader();
        var blueprint = loader.LoadFromFile(SampleC64Path);

        var machine = blueprint.BuildMachine(new ArchitectureBuilder());

        // Just exercise the wired clock: nothing should throw.
        for (var i = 0; i < 100; i++)
        {
            machine.Clock.Step();
        }

        machine.Clock.TotalCycles.Should().Be(100);
    }

    [Fact]
    public void LoadFromFile_MissingPath_ThrowsFileNotFound()
    {
        var loader = new AdhocMachineYamlLoader();

        Action act = () => loader.LoadFromFile("does-not-exist.yaml");

        act.Should().Throw<FileNotFoundException>();
    }
}
