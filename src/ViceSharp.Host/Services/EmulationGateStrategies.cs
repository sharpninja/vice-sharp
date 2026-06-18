namespace ViceSharp.Host.Services;

/// <summary>
/// Canonical names for the selectable emulation pacing strategies and the factory that
/// builds the matching <see cref="IEmulationGate"/>. Stored ids are lowercase ("semaphore",
/// "vice"); the gates' <see cref="IEmulationGate.Name"/> is the display form
/// ("Semaphore", "VICE"). Used by <see cref="EmulationPumpService"/> (selection + live
/// switch), the settings host (persistence + apply), and the UI dropdown.
/// </summary>
public static class EmulationGateStrategies
{
    /// <summary>Default strategy: high-res timer releases a semaphore the worker blocks on.</summary>
    public const string Semaphore = "semaphore";

    /// <summary>Faithful VICE Layer-3 throttle (sound back-pressure + vsync).</summary>
    public const string Vice = "vice";

    /// <summary>Canonicalize any input (display name, id, null, unknown) to a stored id,
    /// defaulting to <see cref="Semaphore"/>.</summary>
    public static string Normalize(string? strategy)
        => string.Equals(strategy?.Trim(), Vice, StringComparison.OrdinalIgnoreCase)
            ? Vice
            : Semaphore;

    /// <summary>The gate display name for a stored id ("vice" -> "VICE", else "Semaphore").</summary>
    public static string DisplayName(string? strategy)
        => Normalize(strategy) == Vice ? "VICE" : "Semaphore";

    /// <summary>Build the gate for a strategy id (unknown/null -> Semaphore).</summary>
    public static IEmulationGate CreateGate(string? strategy)
        => Normalize(strategy) == Vice
            ? new ViceEmulationGate()
            : new SemaphoreEmulationGate();
}
