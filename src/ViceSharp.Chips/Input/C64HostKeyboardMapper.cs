namespace ViceSharp.Chips.Input;

public static class C64HostKeyboardMapper
{
    /// <summary>
    /// Load a VKM keyboard map file and return the resolved keyboard map.
    /// Throws <see cref="InvalidOperationException"/> if the file has parse
    /// errors; use <see cref="C64VkmParser.Load"/> directly for diagnostic
    /// access.
    /// VICE: keyboard map loaded from .vkm files distributed with VICE data.
    /// </summary>
    public static ViceSharp.Abstractions.IKeyboardInputMap LoadFromFile(string filePath)
    {
        var result = C64VkmParser.Load(filePath);
        if (result.HasErrors)
            throw new InvalidOperationException(
                $"VKM file '{filePath}' has parse errors: " +
                string.Join("; ", result.Diagnostics
                    .Where(d => d.Severity == C64VkmDiagnosticSeverity.Error)
                    .Select(d => d.Message)));
        return result.KeyboardMap;
    }

    private static readonly byte[] NoKeys = [];

    public static C64KeyboardMap DefaultFallbackMap { get; } = C64KeyboardMap.CreateDefaultFallback();

    public static bool TryMap(string key, out byte[] keyCodes)
        => TryMap(DefaultFallbackMap, key, out keyCodes);

    public static bool TryMap(ViceSharp.Abstractions.IKeyboardInputMap keyboardMap, string key, out byte[] keyCodes)
    {
        if (keyboardMap.TryResolve(key, out var resolvedKeyCodes))
        {
            keyCodes = resolvedKeyCodes.ToArray();
            return true;
        }

        keyCodes = NoKeys;
        return false;
    }
}
