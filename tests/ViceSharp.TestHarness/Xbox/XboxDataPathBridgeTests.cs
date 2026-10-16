namespace ViceSharp.TestHarness.Xbox;

using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using ViceSharp.Core.Configuration;
using ViceSharp.Host.Services;
using ViceSharp.Host.Startup;
using ViceSharp.RomFetch;
using Xunit;

/// <summary>
/// PLAN-XBOXUWP slice S14 (IMPL-XBOXUWP-014). FR-INPROC / FR-CFG (data-path
/// bootstrap), TEST-INPROC. The <see cref="XboxDataPathBridge"/> runs at process
/// entry (before the in-process host builds and before
/// <c>DefaultEmulatorRuntimeFactory</c> resolves ROMs and keymaps) to point the
/// emulator's data resolution at the AppContainer-writable LocalFolder, seed
/// packaged keymaps, and redirect the transient keymap write-path off the
/// read-only OS temp directory. All cases run off-console (Tier H) with a unique
/// temp directory standing in for the UWP LocalFolder.
///
/// Isolation: these cases mutate the process env var
/// <c>VICESHARP_ROM_PATH</c> and the static keymap write-path override; each case
/// snapshots and restores both through <see cref="ProcessStateGuard"/> so nothing
/// leaks into other tests (assembly-level parallelization is disabled).
/// </summary>
[Trait("Category", "Xbox")]
public sealed class XboxDataPathBridgeTests
{
    private const string RomEnvVar = "VICESHARP_ROM_PATH";

    /// <summary>
    /// FR: FR-INPROC (data-path bootstrap), TR: TR-INPROC. TEST-INPROC.
    /// Use case: On the Xbox head the emulator's ROM/keymap resolution must point at
    /// the AppContainer-writable LocalFolder, which the bridge wires up before the
    /// host builds.
    /// Acceptance: After Configure the VICESHARP_ROM_PATH env var equals the LocalFolder
    /// root, the C64 subdirectory has been created, and ViceDataPathResolver.FindDataRoots()
    /// now contains that root; the returned result echoes the data root and config directory.
    /// </summary>
    [Fact]
    public void Configure_PointsResolverAtLocalFolder_SetsEnvVarAndCreatesC64Dir()
    {
        using var guard = new ProcessStateGuard();
        using var local = new TempFolder();
        using var packaged = new TempFolder();

        var folder = new LocalDataFolder(local.Path);
        Assert.False(Directory.Exists(folder.C64Path));

        var result = XboxDataPathBridge.Configure(folder, packaged.Path);

        Assert.Equal(folder.RootPath, Environment.GetEnvironmentVariable(RomEnvVar));
        Assert.True(Directory.Exists(folder.C64Path));
        Assert.Contains(
            ViceDataPathResolver.FindDataRoots(),
            r => string.Equals(r, folder.RootPath, StringComparison.OrdinalIgnoreCase));
        Assert.Equal(folder.RootPath, result.DataRoot);
        Assert.Equal(folder.RootPath, result.ConfigDirectory);
    }

    /// <summary>
    /// FR: FR-INPROC (data-path bootstrap), TR: TR-INPROC. TEST-INPROC.
    /// Use case: The app ships keymaps as packaged assets; the bridge seeds them into
    /// the writable LocalFolder so the resolver can enumerate them at runtime.
    /// Acceptance: Given a packaged C64 asset directory containing foo.vkm, Configure
    /// copies it into the LocalFolder C64 directory with identical content and reports
    /// the seeded file name in the result.
    /// </summary>
    [Fact]
    public void Configure_SeedsPackagedVkm_IntoC64Dir_WhenAbsent()
    {
        using var guard = new ProcessStateGuard();
        using var local = new TempFolder();
        using var packaged = new TempFolder();

        File.WriteAllText(Path.Combine(packaged.Path, "foo.vkm"), "# packaged keymap\n");

        var folder = new LocalDataFolder(local.Path);
        var result = XboxDataPathBridge.Configure(folder, packaged.Path);

        var destVkm = Path.Combine(folder.C64Path, "foo.vkm");
        Assert.True(File.Exists(destVkm));
        Assert.Equal("# packaged keymap\n", File.ReadAllText(destVkm));
        Assert.Contains("foo.vkm", result.SeededKeymaps);
    }

    /// <summary>
    /// FR: FR-INPROC (data-path bootstrap), TR: TR-INPROC. TEST-INPROC.
    /// Use case: A user edits a seeded keymap; a later app launch (second Configure)
    /// must not clobber their edit with the packaged copy.
    /// Acceptance: The first Configure seeds foo.vkm; after the destination is edited a
    /// second Configure leaves the edited content intact and does not report foo.vkm as
    /// seeded (copy-only-when-absent).
    /// </summary>
    [Fact]
    public void Configure_DoesNotOverwrite_EditedDestinationVkm_OnSecondRun()
    {
        using var guard = new ProcessStateGuard();
        using var local = new TempFolder();
        using var packaged = new TempFolder();

        File.WriteAllText(Path.Combine(packaged.Path, "foo.vkm"), "# packaged keymap\n");
        var folder = new LocalDataFolder(local.Path);

        XboxDataPathBridge.Configure(folder, packaged.Path);

        var destVkm = Path.Combine(folder.C64Path, "foo.vkm");
        Assert.True(File.Exists(destVkm));
        File.WriteAllText(destVkm, "# USER EDIT\n");

        var second = XboxDataPathBridge.Configure(folder, packaged.Path);

        Assert.Equal("# USER EDIT\n", File.ReadAllText(destVkm));
        Assert.DoesNotContain("foo.vkm", second.SeededKeymaps);
    }

    /// <summary>
    /// FR: FR-INPROC (data-path bootstrap), TR: TR-INPROC. TEST-INPROC.
    /// Use case: InputServiceHost writes a transient .vkm when applying a keymap payload;
    /// on the AppContainer head the OS temp directory is not writable, so the write must
    /// land in the LocalFolder root instead.
    /// Acceptance: After Configure the keymap write-path helper resolves to the LocalFolder
    /// root (not Path.GetTempPath()), and a generated transient .vkm path is rooted there.
    /// </summary>
    [Fact]
    public void Configure_RedirectsKeymapWritePath_ToLocalFolderRoot_NotTemp()
    {
        using var guard = new ProcessStateGuard();
        using var local = new TempFolder();
        using var packaged = new TempFolder();

        var folder = new LocalDataFolder(local.Path);
        XboxDataPathBridge.Configure(folder, packaged.Path);

        Assert.Equal(folder.RootPath, HostKeymapWritePath.ResolveDirectory());
        Assert.NotEqual(Path.GetTempPath(), HostKeymapWritePath.ResolveDirectory());
        Assert.Equal(folder.RootPath, Path.GetDirectoryName(HostKeymapWritePath.CreateTransientVkmPath()));
    }

    /// <summary>
    /// FR: FR-INPROC (data-path bootstrap) / FR-CFG, TR: TR-INPROC. TEST-INPROC.
    /// Use case: The bridge must wire the config-directory key that ViceSettings uses so
    /// vice.ini / vice-sharp.ini land under the writable LocalFolder.
    /// Acceptance: When a builder is supplied, the built configuration exposes
    /// ViceSharp:ConfigDirectory = LocalFolder root, ViceConfigLocator resolves that root,
    /// and the result's config directory drives ViceSettings.OpenAt to the LocalFolder INI paths.
    /// </summary>
    [Fact]
    public void Configure_SetsConfigDirectoryKey_ResolvedByViceConfigLocatorAndViceSettings()
    {
        using var guard = new ProcessStateGuard();
        using var local = new TempFolder();
        using var packaged = new TempFolder();

        var folder = new LocalDataFolder(local.Path);
        var builder = new ConfigurationBuilder();

        var result = XboxDataPathBridge.Configure(folder, packaged.Path, builder);
        IConfigurationRoot config = builder.Build();

        Assert.Equal(folder.RootPath, config[ViceConfigLocator.ConfigDirectoryKey]);
        Assert.Equal(folder.RootPath, ViceConfigLocator.ResolveConfigDirectory(config));

        var settings = ViceSettings.OpenAt(result.ConfigDirectory);
        Assert.Equal(
            Path.Combine(folder.RootPath, ViceConfigLocator.ViceSharpIniFileName),
            settings.ViceSharpIniPath);
    }

    /// <summary>
    /// FR: FR-INPROC (data-path bootstrap), TR: TR-INPROC. TEST-INPROC.
    /// Use case: Startup ordering matters: the resolver reads VICESHARP_ROM_PATH, so the
    /// bridge must set it before any resolver call. This proves the negative and the fix.
    /// Acceptance: A well-formed data root (with a C64 subdir) is NOT discovered by
    /// FindDataRoots() while nothing has pointed the env var at it; after Configure runs the
    /// same root becomes discoverable, proving the env var is set before the resolver reads it.
    /// </summary>
    [Fact]
    public void WithoutConfigure_ResolverDoesNotFindLocalRoot_ThenConfigureMakesItDiscoverable()
    {
        using var guard = new ProcessStateGuard();
        using var local = new TempFolder();
        using var packaged = new TempFolder();

        var folder = new LocalDataFolder(local.Path);
        Directory.CreateDirectory(folder.C64Path);
        Environment.SetEnvironmentVariable(RomEnvVar, null);

        Assert.DoesNotContain(
            ViceDataPathResolver.FindDataRoots(),
            r => string.Equals(r, folder.RootPath, StringComparison.OrdinalIgnoreCase));

        XboxDataPathBridge.Configure(folder, packaged.Path);

        Assert.Contains(
            ViceDataPathResolver.FindDataRoots(),
            r => string.Equals(r, folder.RootPath, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Snapshots and restores process-global state mutated by the bridge.</summary>
    private sealed class ProcessStateGuard : IDisposable
    {
        private readonly string? _priorRomPath = Environment.GetEnvironmentVariable(RomEnvVar);

        public void Dispose()
        {
            Environment.SetEnvironmentVariable(RomEnvVar, _priorRomPath);
            HostKeymapWritePath.RedirectTo(null);
        }
    }

    /// <summary>A unique temp directory standing in for the UWP LocalFolder, deleted on dispose.</summary>
    private sealed class TempFolder : IDisposable
    {
        public TempFolder()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "ViceSharpXboxDataPathTests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(Path))
                    Directory.Delete(Path, recursive: true);
            }
            catch (IOException)
            {
                // Best-effort cleanup of a scratch directory.
            }
            catch (UnauthorizedAccessException)
            {
                // Best-effort cleanup of a scratch directory.
            }
        }
    }
}
