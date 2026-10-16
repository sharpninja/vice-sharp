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

    /// <summary>
    /// True when <see cref="OnStolenCycle"/> should run on a skipped CPU
    /// tick even if <see cref="CanForceStealCurrentCycle"/> is false.
    /// </summary>
    bool NotifyOnStolenCycle => false;

    /// <summary>
    /// VICE <c>check_ba()</c> plus <c>vicii_steal_cycles()</c> run before the
    /// <c>CLK_INC</c> of the pending memory cycle. Notify so the CPU can emit
    /// that delayed FETCH clock without advancing its instruction phase.
    /// </summary>
    void OnStolenCycle()
    {
    }
}
