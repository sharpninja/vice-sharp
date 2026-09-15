namespace ViceSharp.Avalonia;

/// <summary>
/// FR-INSTALL-001: Commodore media extensions the MSI registers for Explorer Open,
/// and the argv parser used when the shell launches ViceSharp with "%1".
/// Generic <c>.bin</c>/<c>.rom</c> are supported in-app but not claimed as OS defaults.
/// </summary>
public static class DesktopFileAssociations
{
    public static readonly DesktopFileAssociation[] Entries =
    [
        new("d64", "ViceSharp.d64", "Commodore disk image"),
        new("g64", "ViceSharp.g64", "Commodore GCR disk image"),
        new("d71", "ViceSharp.d71", "Commodore 1571 disk image"),
        new("d81", "ViceSharp.d81", "Commodore 1581 disk image"),
        new("tap", "ViceSharp.tap", "Commodore tape image"),
        new("t64", "ViceSharp.t64", "Commodore tape archive"),
        new("crt", "ViceSharp.crt", "Commodore cartridge"),
        new("prg", "ViceSharp.prg", "Commodore program"),
    ];

    public static bool IsRegisteredExtension(string filePath)
    {
        var ext = Path.GetExtension(filePath);
        if (ext.Length < 2)
            return false;

        var id = ext[1..];
        foreach (var entry in Entries)
        {
            if (string.Equals(entry.Extension, id, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    /// <summary>
    /// First existing registered media path in <paramref name="args"/>, skipping the
    /// executable and dash/slash flags (Explorer Open: exe then "%1").
    /// </summary>
    public static string? TryGetOpenPath(IReadOnlyList<string> args, Func<string, bool>? fileExists = null)
    {
        if (args.Count == 0)
            return null;

        fileExists ??= File.Exists;
        var start = 0;
        if (args[0].EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            || args[0].EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
        {
            start = 1;
        }

        for (var i = start; i < args.Count; i++)
        {
            var candidate = args[i].Trim().Trim('"');
            if (candidate.Length == 0)
                continue;
            if (candidate[0] is '-' or '/')
                continue;
            if (!IsRegisteredExtension(candidate))
                continue;
            if (!fileExists(candidate))
                continue;
            return Path.GetFullPath(candidate);
        }

        return null;
    }
}

public readonly record struct DesktopFileAssociation(string Extension, string ProgId, string Description);
