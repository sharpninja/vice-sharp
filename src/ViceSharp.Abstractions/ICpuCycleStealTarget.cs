namespace ViceSharp.Abstractions;

/// <summary>
/// A CPU that can report whether its current cycle may be held by external bus ownership.
/// </summary>
public interface ICpuCycleStealTarget
{
    /// <summary>
    /// True when a pending external CPU hold should defer the next CPU tick.
    /// </summary>
    bool CanStealCurrentCycle { get; }

    /// <summary>
    /// True when a mandatory external hold may defer the next CPU tick even if the conditional hold would not.
    /// </summary>
    bool CanForceStealCurrentCycle { get; }
}
