namespace ViceSharp.Host.Services;

/// <summary>
/// Resolves the directory used for transient keymap (.vkm) write files created
/// when <see cref="InputServiceHost"/> applies a keyboard-map payload.
///
/// The default is the OS temp directory, preserving desktop behaviour exactly.
/// On the AppContainer / Xbox head the OS temp directory is not writable, so
/// <c>XboxDataPathBridge</c> redirects writes to the app's writable LocalFolder
/// root via <see cref="RedirectTo"/>. This is the single, minimal seam that keeps
/// the InputServiceHost write-path portable without touching the resolver.
/// </summary>
public static class HostKeymapWritePath
{
    private static volatile string? _overrideDirectory;

    /// <summary>
    /// Redirect transient keymap writes to <paramref name="directory"/>. A null or
    /// whitespace value restores the default OS temp directory.
    /// </summary>
    public static void RedirectTo(string? directory) =>
        _overrideDirectory = string.IsNullOrWhiteSpace(directory) ? null : directory;

    /// <summary>The directory transient keymap files are written to (override, else the OS temp dir).</summary>
    public static string ResolveDirectory() => _overrideDirectory ?? Path.GetTempPath();

    /// <summary>A fresh, unique transient .vkm path under <see cref="ResolveDirectory"/>.</summary>
    public static string CreateTransientVkmPath() =>
        Path.Combine(ResolveDirectory(), $"vice-sharp-{Guid.NewGuid():N}.vkm");
}
