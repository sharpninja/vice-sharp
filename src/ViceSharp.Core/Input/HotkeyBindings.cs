namespace ViceSharp.Core.Input;

/// <summary>
/// FR-CFG-004: load a hotkey file and dispatch keys to host commands.
/// Monitor scope is ignored so monitor text entry is not treated as a hotkey.
/// Invalid lines stay in the loaded text and do not drop the other mappings.
/// </summary>
public static class HotkeyBindings
{
    /// <summary>Lines from the last load that are not <c>action: key</c> mappings.</summary>
    public static IReadOnlyList<string> Diagnostics { get; private set; } = [];

    /// <summary>
    /// Load hotkey text. The returned document keeps every line, including
    /// lines that are not <c>action: key</c> mappings.
    /// </summary>
    /// <param name="text">Hotkey file contents.</param>
    /// <returns>The loaded hotkey document.</returns>
    public static string LoadHotkeys(string text)
    {
        var diagnostics = new List<string>();
        var source = text ?? "";
        foreach (var rawLine in source.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var line = rawLine.Trim();
            if (line.Length == 0)
                continue;

            var colon = line.IndexOf(':');
            if (colon <= 0 || colon >= line.Length - 1)
            {
                diagnostics.Add(line);
                continue;
            }

            var action = line[..colon].Trim();
            var mappedKey = line[(colon + 1)..].Trim();
            if (action.Length == 0 || mappedKey.Length == 0)
                diagnostics.Add(line);
        }

        Diagnostics = diagnostics;
        return source;
    }

    /// <summary>
    /// Dispatch <paramref name="key"/> against <paramref name="loaded"/> when
    /// <paramref name="scope"/> is emulation. Any other scope, including
    /// monitor, returns an empty command.
    /// </summary>
    /// <param name="loaded">Document from <see cref="LoadHotkeys"/>.</param>
    /// <param name="key">Key name, for example F12.</param>
    /// <param name="scope">UI scope. Only "emulation" dispatches.</param>
    /// <returns>
    /// A host command such as <c>host:reset</c>, or empty when the key does
    /// not map in this scope.
    /// </returns>
    public static string DispatchHotkey(string loaded, string key, string scope)
    {
        if (!string.Equals(scope, "emulation", StringComparison.OrdinalIgnoreCase))
            return "";

        if (string.IsNullOrEmpty(loaded) || string.IsNullOrWhiteSpace(key))
            return "";

        var wanted = key.Trim();
        foreach (var rawLine in loaded.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var line = rawLine.Trim();
            var colon = line.IndexOf(':');
            if (colon <= 0 || colon >= line.Length - 1)
                continue;

            var action = line[..colon].Trim();
            var mappedKey = line[(colon + 1)..].Trim();
            if (action.Length == 0 || mappedKey.Length == 0)
                continue;
            if (!string.Equals(mappedKey, wanted, StringComparison.OrdinalIgnoreCase))
                continue;

            return "host:" + action;
        }

        return "";
    }
}
