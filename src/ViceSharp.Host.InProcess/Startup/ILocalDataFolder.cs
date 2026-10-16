namespace ViceSharp.Host.Startup;

/// <summary>
/// Abstracts the app's writable local data folder for the Xbox / UWP head, where
/// the concrete path comes from <c>ApplicationData.Current.LocalFolder.Path</c>.
/// Off-console tests supply a temp directory instead.
///
/// <see cref="RootPath"/> is the VICE data root (also the ViceSharp config
/// directory); <see cref="C64Path"/> is its <c>C64</c> subdirectory, which the
/// VICE data resolver requires to accept the root and where keymaps are seeded.
/// </summary>
public interface ILocalDataFolder
{
    /// <summary>The writable data root (VICE data root and ViceSharp config directory).</summary>
    string RootPath { get; }

    /// <summary>The <c>C64</c> subdirectory under <see cref="RootPath"/> holding C64 ROMs and keymaps.</summary>
    string C64Path { get; }

    /// <summary>The <c>VIC20</c> subdirectory under <see cref="RootPath"/> holding VIC-20 ROMs.</summary>
    string Vic20Path { get; }
}
