namespace ViceSharp.Abstractions;

/// <summary>
/// A bus participant that can hold the CPU for a master clock cycle.
/// </summary>
public interface ICpuCycleStealer
{
    /// <summary>
    /// True when the CPU must not advance on the current Phi2 edge.
    /// </summary>
    bool IsCpuCycleStolen { get; }

    /// <summary>
    /// True when the hold is unconditional for the current Phi2 edge.
    /// </summary>
    bool IsCpuCycleStealMandatory { get; }
}
