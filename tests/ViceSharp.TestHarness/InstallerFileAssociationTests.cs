namespace ViceSharp.TestHarness;

using ViceSharp.Avalonia;
using Xunit;

/// <summary>
/// FR: FR-INSTALL-001, TR: TR-INSTALL-ASSOC-001, TEST-INSTALL-001.
/// Use case: Explorer Open on a supported Commodore image launches ViceSharp
/// with that path.
/// </summary>
public sealed class InstallerFileAssociationTests
{
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

    /// <summary>
    /// FR: FR-INSTALL-001, TR: TR-INSTALL-ASSOC-001, TEST-INSTALL-001.
    /// Acceptance: ViceSharp.wxs registers each Commodore extension ProgId and
    /// an open command with the published exe and %1.
    /// </summary>
    [Fact]
    public void Wxs_RegistersOpenVerb_ForEachSupportedExtension()
    {
        var wxs = File.ReadAllText(Path.Combine(RepoRoot, "installer", "ViceSharp.wxs"));
        Assert.Contains("Software\\RegisteredApplications", wxs, StringComparison.Ordinal);
        Assert.Contains("Capabilities\\FileAssociations", wxs, StringComparison.Ordinal);
        Assert.Contains("[INSTALLFOLDER]$(EntryExe)", wxs, StringComparison.Ordinal);
        Assert.Contains("%1", wxs, StringComparison.Ordinal);
        foreach (var entry in DesktopFileAssociations.Entries)
        {
            Assert.Contains($"Software\\Classes\\.{entry.Extension}", wxs, StringComparison.Ordinal);
            Assert.Contains($"Software\\Classes\\{entry.ProgId}\\shell\\open\\command", wxs, StringComparison.Ordinal);
            Assert.Contains($"Name=\".{entry.Extension}\"", wxs, StringComparison.Ordinal);
        }

        Assert.DoesNotContain("Software\\Classes\\.bin", wxs, StringComparison.Ordinal);
        Assert.DoesNotContain("Software\\Classes\\.rom", wxs, StringComparison.Ordinal);
    }

    /// <summary>
    /// FR: FR-INSTALL-001, TR: TR-INSTALL-ASSOC-001, TEST-INSTALL-001.
    /// Acceptance: argv parser takes the first existing registered path and
    /// skips the exe and flags.
    /// </summary>
    [Fact]
    public void TryGetOpenPath_PicksFirstExistingRegisteredFile()
    {
        var d64 = Path.Combine(Path.GetTempPath(), "vs-assoc-test.d64");
        var txt = Path.Combine(Path.GetTempPath(), "vs-assoc-test.txt");
        var exists = (string p) =>
            string.Equals(p, d64, StringComparison.OrdinalIgnoreCase)
            || string.Equals(p, txt, StringComparison.OrdinalIgnoreCase);

        var path = DesktopFileAssociations.TryGetOpenPath(
            ["C:\\Program Files\\ViceSharp\\ViceSharp.Avalonia.exe", "--something", txt, d64],
            exists);

        Assert.Equal(Path.GetFullPath(d64), path);
        Assert.Null(DesktopFileAssociations.TryGetOpenPath(["--help"], exists));
        Assert.Null(DesktopFileAssociations.TryGetOpenPath(["app.exe", txt], exists));
        Assert.True(DesktopFileAssociations.IsRegisteredExtension("game.PRG"));
        Assert.False(DesktopFileAssociations.IsRegisteredExtension("payload.bin"));
    }
}
