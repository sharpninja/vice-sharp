namespace ViceSharp.Abstractions;

/// <summary>
/// Resolves host key names to machine keyboard matrix key codes.
/// </summary>
public interface IKeyboardInputMap
{
    /// <summary>Human-readable map name.</summary>
    string Name { get; }

    /// <summary>Resolves a host key name to one or more encoded row/column key codes.</summary>
    bool TryResolve(string key, out IReadOnlyList<byte> keyCodes);
}
