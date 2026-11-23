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
    /// FR-INSTALL-001 / TR-INSTALL-ASSOC-001 / TEST-INSTALL-001.
    /// Use case: Open a supported media file from Windows Explorer.
    /// Acceptance: The installer registers its ProgID and an Open command containing the
    /// published executable and %1.
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
    /// FR-INSTALL-001 / TR-INSTALL-ASSOC-001 / TEST-INSTALL-001.
    /// Use case: Parse argv with flags and multiple file candidates.
    /// Acceptance: The first existing registered path wins, skipping the executable and
    /// flags.
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

    /// <summary>
    /// FR-INSTALL-001 / TR-INSTALL-ASSOC-001 / TEST-INSTALL-001.
    /// Use case: Open an absolute media path with spaces, quoted or unquoted.
    /// Acceptance: The argument parser accepts the file path, including a leading Unix
    /// slash.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TryGetOpenPath_DesktopAbsolutePath_IsAccepted(bool quoted)
    {
        var media = Path.Combine(Path.GetTempPath(), "ViceSharp media", "game.d64");
        var executable = Path.Combine(Path.GetTempPath(), "ViceSharp.Avalonia");
        var requestedPath = quoted ? $"\"{media}\"" : media;
        var examined = new List<string>();

        var path = DesktopFileAssociations.TryGetOpenPath(
            [executable, "--ignored.d64", "missing.d64", "notes.txt", requestedPath, "later.d64"],
            candidate =>
            {
                examined.Add(candidate);
                return candidate == media || candidate == "later.d64";
            });

        Assert.Equal(Path.GetFullPath(media), path);
        Assert.Equal(new[] { "missing.d64", media }, examined);
    }

    /// <summary>
    /// FR-INSTALL-001 / TR-INSTALL-ASSOC-001 / TEST-INSTALL-001.
    /// Use case: Parse media candidates and platform-specific option prefixes.
    /// Acceptance: Dash options are excluded. Slash switches keep their Windows meaning,
    /// while Unix-rooted registered files remain eligible.
    /// </summary>
    [Fact]
    public void TryGetOpenPath_OptionPrefixes_RespectPlatform()
    {
        Assert.Null(DesktopFileAssociations.TryGetOpenPath(["--option.d64"], _ => true));
        var slashPath = DesktopFileAssociations.TryGetOpenPath(["/game.d64"], _ => true);
        Assert.Equal(OperatingSystem.IsWindows() ? null : Path.GetFullPath("/game.d64"), slashPath);
    }
}
