namespace ViceSharp.TestHarness;

using System.IO;
using System.Linq;
using ViceSharp.Architectures.Adhoc;
using Xunit;

/// <summary>
/// BRANDING/MACHINE-DEFS-001: there is one machine-definition YAML per C64
/// variant under docs/samples/machines/, each pinning a single specific ROM
/// set, the variant's specific chips, its PAL/NTSC selection, and its bus /
/// system-core config. These assert every generated definition is schema-valid
/// (loads via AdhocMachineYamlLoader) and that the set is complete.
///
/// Files are produced by tools/generate-c64-machines.py from C64MachineProfiles.
/// </summary>
public sealed class C64VariantMachineDefinitionsTests
{
    private static string MachinesDirectory => Path.Combine(RepoRoot, "docs", "samples", "machines");

    public static TheoryData<string> VariantFiles()
    {
        var data = new TheoryData<string>();
        foreach (var path in Directory.EnumerateFiles(MachinesDirectory, "*.machine.yaml").OrderBy(p => p, System.StringComparer.Ordinal))
            data.Add(Path.GetFileName(path));
        return data;
    }

    /// <summary>
    /// FR-CFG-001, TR-SYSTEM-CORE-001.
    /// Use case: Sample machine YAML files must be present for every C64 variant.
    /// Acceptance: The machine sample file set equals the expected 14 profile files.
    /// </summary>
    [Fact]
    public void EveryC64Variant_HasAMachineDefinition()
    {
        var files = Directory.EnumerateFiles(MachinesDirectory, "*.machine.yaml")
            .Select(Path.GetFileNameWithoutExtension)
            .ToArray();

        // The 14 variants in C64MachineProfiles.All.
        string[] expected =
        [
            "c64.machine", "c64c.machine", "c64old.machine", "ntsc.machine", "newntsc.machine",
            "oldntsc.machine", "paln.machine", "sx64pal.machine", "sx64ntsc.machine",
            "pet64pal.machine", "pet64ntsc.machine", "ultimax.machine", "c64gs.machine", "c64jap.machine",
        ];

        Assert.Equal(expected.OrderBy(s => s), files.OrderBy(s => s));
    }

    /// <summary>
    /// FR-CFG-001, TR-SYSTEM-CORE-001.
    /// Use case: Each checked-in variant machine YAML must load as an ad-hoc machine.
    /// Acceptance: Loader returns a blueprint with a non-empty name and positive clock.
    /// </summary>
    [Theory]
    [MemberData(nameof(VariantFiles))]
    public void VariantDefinition_LoadsAndDescribesAMachine(string fileName)
    {
        var loader = new AdhocMachineYamlLoader();

        var blueprint = loader.LoadFromFile(Path.Combine(MachinesDirectory, fileName));

        Assert.NotNull(blueprint);
        Assert.False(string.IsNullOrWhiteSpace(blueprint.Descriptor.MachineName));
        Assert.True(blueprint.Descriptor.MasterClockHz > 0);
    }

    private static string RepoRoot
    {
        get
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "ViceSharp.slnx")))
                dir = dir.Parent;
            Assert.NotNull(dir);
            return dir!.FullName;
        }
    }
}
