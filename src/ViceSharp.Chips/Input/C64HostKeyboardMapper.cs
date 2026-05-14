namespace ViceSharp.Chips.Input;

public static class C64HostKeyboardMapper
{
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
