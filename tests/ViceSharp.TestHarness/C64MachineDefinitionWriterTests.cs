namespace ViceSharp.TestHarness;

using System.Linq;
using ViceSharp.Architectures.Adhoc;
using ViceSharp.Architectures.C64;
using Xunit;

/// <summary>
/// MACHINE-DEFS-001: C64MachineDefinitionWriter builds a machine-definition
/// entity from a C64 profile and serializes it with YamlDotNet's
/// source-generated context. These assert every variant serializes to a
/// schema-valid document and that variant-specific chips / bus / ROM selections
/// land in the output. Exercised by the console `--export-machines` feature.
/// </summary>
public sealed class C64MachineDefinitionWriterTests
{
    public static TheoryData<string> ProfileIds()
    {
        var data = new TheoryData<string>();
        foreach (var profile in C64MachineProfiles.All)
            data.Add(profile.Id);
        return data;
    }

    /// <summary>
    /// FR-CFG-001, TR-SYSTEM-CORE-001.
    /// Use case: Exported machine definitions must cover every C64 profile.
    /// Acceptance: Writer.All emits exactly the C64MachineProfiles.All count.
    /// </summary>
    [Fact]
    public void All_EmitsEveryC64Variant()
        => Assert.Equal(C64MachineProfiles.All.Count, C64MachineDefinitionWriter.All().Count());

    /// <summary>
    /// FR-CFG-001, TR-SYSTEM-CORE-001.
    /// Use case: Each generated YAML profile must load through the ad-hoc schema.
    /// Acceptance: Loaded descriptor name and master clock match the source profile.
    /// </summary>
    [Theory]
    [MemberData(nameof(ProfileIds))]
    public void ToYaml_IsSchemaValid_AndDescribesTheVariant(string id)
    {
        var profile = C64MachineProfiles.Resolve(id);

        var yaml = C64MachineDefinitionWriter.ToYaml(profile);
        var blueprint = new AdhocMachineYamlLoader().LoadFromString(yaml);

        Assert.Equal(profile.DisplayName, blueprint.Descriptor.MachineName);
        Assert.Equal(profile.NominalClockHz, blueprint.Descriptor.MasterClockHz);
    }

    /// <summary>
    /// FR-CFG-001, TR-SYSTEM-CORE-001.
    /// Use case: The C64C definition pins cost-reduced chip and ROM identities.
    /// Acceptance: YAML contains the 8565 VIC-II, 8580 SID, C64C board, and C64C KERNAL.
    /// </summary>
    [Fact]
    public void ToYaml_C64C_PinsCostReducedDies()
    {
        var yaml = C64MachineDefinitionWriter.ToYaml(C64MachineProfiles.C64CPal);

        Assert.Contains("model: Mos8565", yaml);   // 8565 VIC-II die
        Assert.Contains("model: Mos8580", yaml);   // 8580 SID die
        Assert.Contains("board: C64C", yaml);
        Assert.Contains("file: kernal-901227-03.bin", yaml);
    }

    /// <summary>
    /// FR-CFG-001, TR-SYSTEM-CORE-001.
    /// Use case: Ultimax profile output must model the cartridge-only MAX bus.
    /// Acceptance: YAML omits KERNAL and CIA2 and selects Max and Ultimax bus policies.
    /// </summary>
    [Fact]
    public void ToYaml_Ultimax_OmitsKernalAndCia2_UsesMaxBus()
    {
        var yaml = C64MachineDefinitionWriter.ToYaml(C64MachineProfiles.Ultimax);

        Assert.DoesNotContain("rom-kernal", yaml);          // no internal KERNAL (cartridge boot)
        Assert.DoesNotContain("role: Cia2", yaml);          // no CIA2 chip
        Assert.Contains("busPolicy: Max", yaml);
        Assert.Contains("addressDecoderPolicy: Ultimax", yaml);
    }

    /// <summary>
    /// FR-CFG-001, TR-SYSTEM-CORE-001.
    /// Use case: Japanese C64 profile output must select the Japanese character ROM.
    /// Acceptance: YAML references chargen-906143-02.bin.
    /// </summary>
    [Fact]
    public void ToYaml_Japanese_UsesJapaneseChargen()
    {
        var yaml = C64MachineDefinitionWriter.ToYaml(C64MachineProfiles.C64Japanese);

        Assert.Contains("file: chargen-906143-02.bin", yaml);
    }
}
