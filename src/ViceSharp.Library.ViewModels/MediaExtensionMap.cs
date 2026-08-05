using ViceSharp.Protocol;

namespace ViceSharp.Library.ViewModels;

/// <summary>
/// FR-ROMM-LAUNCH-001 (AC-LAUNCH-02, AC-LAUNCH-03). Maps a downloaded file's extension to how it
/// attaches to the emulator: its default <see cref="MediaSlot"/>, its <see cref="MediaKind"/>, and
/// whether it is launchable. Use case: the library picks the right slot for a selected game and
/// disables Attach for content that cannot boot (e.g. a raw <c>.prg</c>).
/// </summary>
public static class MediaExtensionMap
{
    private static readonly Dictionary<string, MediaMapping> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        [".d64"] = new(MediaKind.Disk, MediaSlot.Drive8, IsLaunchable: true),
        [".g64"] = new(MediaKind.Disk, MediaSlot.Drive8, IsLaunchable: true),
        [".d71"] = new(MediaKind.Disk, MediaSlot.Drive8, IsLaunchable: true),
        [".d81"] = new(MediaKind.Disk, MediaSlot.Drive8, IsLaunchable: true),
        [".tap"] = new(MediaKind.Tape, MediaSlot.Tape, IsLaunchable: true),
        [".t64"] = new(MediaKind.Tape, MediaSlot.Tape, IsLaunchable: true),
        [".crt"] = new(MediaKind.Cartridge, MediaSlot.Cartridge, IsLaunchable: true),
        [".bin"] = new(MediaKind.Cartridge, MediaSlot.Cartridge, IsLaunchable: true),
        [".rom"] = new(MediaKind.Cartridge, MediaSlot.Cartridge, IsLaunchable: true),
        [".prg"] = new(MediaKind.Program, Slot: null, IsLaunchable: false),
    };

    /// <summary>
    /// AC-LAUNCH-02, AC-LAUNCH-03. Resolves a file name (e.g. <c>game.d64</c>) or bare extension
    /// (<c>.d64</c>) to its <see cref="MediaMapping"/>, or <c>null</c> when the extension is not
    /// recognized.
    /// </summary>
    /// <param name="fileNameOrExtension">A file name or an extension including the leading dot.</param>
    public static MediaMapping? Resolve(string fileNameOrExtension)
    {
        if (string.IsNullOrWhiteSpace(fileNameOrExtension))
        {
            return null;
        }

        string ext = Path.GetExtension(fileNameOrExtension);
        return ext.Length > 0 && Map.TryGetValue(ext, out MediaMapping mapping) ? mapping : null;
    }

    /// <summary>
    /// AC-LAUNCH-03. Whether a file can be attached and booted. Returns <c>false</c> for unknown
    /// extensions and for a raw <c>.prg</c>.
    /// </summary>
    /// <param name="fileName">The file name to test.</param>
    public static bool IsLaunchable(string fileName) => Resolve(fileName)?.IsLaunchable ?? false;
}
