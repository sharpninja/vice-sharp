namespace ViceSharp.TestHarness.Vic20;

using ViceSharp.Abstractions;
using ViceSharp.Architectures.C1541;
using ViceSharp.Architectures.C64;
using ViceSharp.Architectures.Vic20;
using ViceSharp.Launcher;
using Xunit;

/// <summary>
/// FR-VIC20-006. Vic20 unit 8 defaults to 1540; C64 remains 1541.
/// </summary>
public sealed class Vic20DriveDefaultTests
{
    [Fact]
    public void Vic20Profile_DefaultDrive_IsC1540_FromProductionProfile()
    {
        var profile = Vic20MachineProfiles.Default;
        Assert.Equal(DriveModel.C1540, profile.DefaultDriveModel);
        Assert.Equal(C1541ViceRomNames.Dos1540, profile.DefaultDriveDosRomName);
        // Production constant must match the catalog name used by the drive builder.
        Assert.Equal("dos1540-325302+3-01.bin", profile.DefaultDriveDosRomName);
    }

    [Fact]
    public void C64Profile_DefaultDrivePath_Remains1541()
    {
        // C64 does not store DefaultDriveModel on profile; production drive default
        // for true-drive / topology is C1541 DOS name.
        Assert.Equal("dos1541-325302-01+901229-05.bin", C1541ViceRomNames.Dos1541);
        Assert.NotEqual(C1541ViceRomNames.Dos1540, C1541ViceRomNames.Dos1541);

        // Topology for x64sc drive attach does not inject dos1540.
        var args = ViceArgsParser.Parse("x64sc", new[] { "-8", "disk.d64" });
        var yaml = ViceTopologyBuilder.BuildYaml(args);
        Assert.Contains("kind: C64", yaml);
        Assert.DoesNotContain("dos1540", yaml);
        Assert.Contains("kind: C1541", yaml);
    }

    [Fact]
    public void XvicTopology_Drive8_InjectsDos1540()
    {
        var args = ViceArgsParser.Parse("xvic", new[] { "-8", "disk.d64" });
        var yaml = ViceTopologyBuilder.BuildYaml(args);
        Assert.Contains("kind: Vic20", yaml);
        Assert.Contains(C1541ViceRomNames.Dos1540, yaml);
    }

    [Fact]
    public void Vic20SystemCoreTraits_AdvertiseDefaultDrive()
    {
        var core = Vic20MachineProfiles.Default.SystemCore;
        Assert.True(core.Traits.TryGetValue("defaultDrive", out var drive));
        Assert.Equal(nameof(DriveModel.C1540), drive);
        Assert.True(core.Traits.TryGetValue("defaultDriveDos", out var dos));
        Assert.Equal(C1541ViceRomNames.Dos1540, dos);
    }
}
