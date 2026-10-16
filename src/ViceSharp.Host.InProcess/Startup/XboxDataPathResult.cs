namespace ViceSharp.Host.Startup;

/// <summary>
/// Outcome of <see cref="XboxDataPathBridge.Configure"/>: the data root the VICE
/// resolver was pointed at, its C64 subdirectory, the config directory usable by
/// <c>ViceSettings.OpenAt</c>, and the keymap file names seeded on this call
/// (empty when destinations already existed).
/// </summary>
/// <param name="DataRoot">The VICE data root (the value written to VICESHARP_ROM_PATH).</param>
/// <param name="C64Path">The <c>C64</c> subdirectory under <paramref name="DataRoot"/>.</param>
/// <param name="ConfigDirectory">The ViceSharp config directory for <c>ViceSettings.OpenAt</c>.</param>
/// <param name="SeededKeymaps">The keymap file names copied from packaged assets on this call.</param>
public sealed record XboxDataPathResult(
    string DataRoot,
    string C64Path,
    string ConfigDirectory,
    IReadOnlyList<string> SeededKeymaps);
