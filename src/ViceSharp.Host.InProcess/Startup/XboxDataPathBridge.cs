using Microsoft.Extensions.Configuration;
using ViceSharp.Core.Configuration;
using ViceSharp.Host.Services;

namespace ViceSharp.Host.Startup;

/// <summary>
/// Process-entry bootstrap for the Xbox / UWP head. Runs before the in-process
/// host builds and before <c>DefaultEmulatorRuntimeFactory</c> resolves ROMs and
/// keymaps, pointing the emulator's data resolution at the AppContainer-writable
/// LocalFolder, seeding packaged keymaps, and redirecting the transient keymap
/// write-path off the read-only OS temp directory.
///
/// The VICE data resolver (<c>ViceDataPathResolver</c>) is intentionally left
/// untouched: the bridge only ever influences it through the
/// <c>VICESHARP_ROM_PATH</c> environment variable it reads.
/// </summary>
public static class XboxDataPathBridge
{
    /// <summary>
    /// The environment variable the VICE data resolver reads first; the bridge sets
    /// it to the LocalFolder root so ROM/keymap resolution targets writable storage.
    /// </summary>
    public const string DataRootEnvironmentVariable = "VICESHARP_ROM_PATH";

    /// <summary>
    /// Configure data-path resolution for the LocalFolder <paramref name="folder"/>. In
    /// order: (1) create the C64 subdirectory if absent; (2) point
    /// <see cref="DataRootEnvironmentVariable"/> at the root (before any resolver call);
    /// (3) set <c>ViceSharp:ConfigDirectory</c> on <paramref name="cfg"/> when supplied;
    /// (4) seed packaged <c>*.vkm</c> keymaps into the C64 directory, copy-only-when-absent
    /// so user edits survive; then redirect the transient keymap write-path to the root.
    /// </summary>
    /// <param name="folder">The app's writable local data folder.</param>
    /// <param name="packagedAssetsC64Path">Directory of packaged <c>*.vkm</c> assets to seed (may be absent).</param>
    /// <param name="cfg">Optional configuration builder to receive the config-directory key.</param>
    /// <returns>The resolved data root, C64 path, config directory, and any keymaps seeded on this call.</returns>
    public static XboxDataPathResult Configure(
        ILocalDataFolder folder,
        string packagedAssetsC64Path,
        IConfigurationBuilder? cfg = null)
    {
        ArgumentNullException.ThrowIfNull(folder);

        var root = folder.RootPath;
        var c64Path = folder.C64Path;

        // (1) The resolver only accepts a data root that already contains a C64 subdirectory.
        Directory.CreateDirectory(c64Path);

        // (2) Point the resolver at the writable root BEFORE anything resolves ROMs/keymaps.
        Environment.SetEnvironmentVariable(DataRootEnvironmentVariable, root);

        // (3) Route ViceSettings' config directory to the same writable root when a builder is given.
        cfg?.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [ViceConfigLocator.ConfigDirectoryKey] = root,
        });

        // (4) Seed packaged keymaps without ever clobbering a user-edited destination.
        var seeded = SeedKeymaps(packagedAssetsC64Path, c64Path);

        // Redirect the transient keymap write-path off the read-only OS temp directory.
        HostKeymapWritePath.RedirectTo(root);

        return new XboxDataPathResult(root, c64Path, root, seeded);
    }

    private static IReadOnlyList<string> SeedKeymaps(string packagedAssetsC64Path, string destinationC64Path)
    {
        if (string.IsNullOrWhiteSpace(packagedAssetsC64Path) || !Directory.Exists(packagedAssetsC64Path))
            return [];

        List<string>? seeded = null;
        foreach (var source in Directory.EnumerateFiles(packagedAssetsC64Path, "*.vkm"))
        {
            var fileName = Path.GetFileName(source);
            var destination = Path.Combine(destinationC64Path, fileName);
            if (File.Exists(destination))
                continue; // copy-only-when-absent: never overwrite user edits

            File.Copy(source, destination, overwrite: false);
            (seeded ??= []).Add(fileName);
        }

        return seeded is null ? [] : seeded;
    }
}
