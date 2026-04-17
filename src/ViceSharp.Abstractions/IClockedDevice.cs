namespace ViceSharp.Abstractions;

/// <summary>
/// A device that receives clock ticks from the system clock.
/// </summary>
public interface IClockedDevice : IDevice
{
    /// <summary>
    /// Process a single clock tick.
    /// </summary>
    /// <remarks>
    /// This is called on every cycle for devices running at master clock speed.
    /// For slower devices this is called only on cycles where the divisor matches.
    /// 
    /// This method is on the hot path - must allocate zero objects.
    /// </remarks>
    void Tick();

    /// <summary>
    /// Clock divisor relative to master system clock.
    /// </summary>
    /// <value>1 = full speed, 2 = half speed, etc.</value>
    uint ClockDivisor { get; }
}