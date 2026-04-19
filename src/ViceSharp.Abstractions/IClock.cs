namespace ViceSharp.Abstractions;

/// <summary>
/// Master system clock that drives cycle-accurate emulation.
/// Distributes ticks to all registered IClockedDevice instances
/// respecting divisors and clock phases.
/// </summary>
public interface IClock
{
    /// <summary>Advances the clock by one master cycle, ticking all due devices.</summary>
    void Step();

    /// <summary>Advances the clock by the specified number of master cycles.</summary>
    void Step(long cycles);

    /// <summary>Total master cycles elapsed since reset.</summary>
    long TotalCycles { get; }

    /// <summary>Master clock frequency in Hz.</summary>
    long FrequencyHz { get; }

    /// <summary>Registers a device to receive clock ticks.</summary>
    void Register(IClockedDevice device);

    /// <summary>Unregisters a device from clock tick distribution.</summary>
    void Unregister(IClockedDevice device);

    /// <summary>Resets the clock to initial state.</summary>
    void Reset();
}

/// <summary>
/// Clock phase indicating which half of the clock cycle the device operates on.
/// </summary>
public enum ClockPhase
{
    /// <summary>Phase 1 (rising edge)</summary>
    Phi1 = 1,
    
    /// <summary>Phase 2 (falling edge)</summary>
    Phi2 = 2
}