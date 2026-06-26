namespace ViceSharp.TestHarness;

using ViceSharp.Host.Services;
using Xunit;

/// <summary>
/// BUG-THROTTLE-001 / TEST-SYSINDEP-001.
/// The semaphore pacing fallback must compute real-time deficit from the
/// requested limiter rate, matching the VICE gate's speed semantics.
/// </summary>
public sealed class SemaphoreEmulationGatePacingTests
{
    /// <summary>
    /// FR-PACESEL-001 / TR-PACESEL-STRAT-001 / BUG-THROTTLE-001 / TEST-SYSINDEP-001.
    /// Use case: true-drive IEC LOAD is sensitive to pump fragmentation. The Semaphore gate
    ///   must advance one full paced quantum even when the persistent anchor says the master
    ///   clock is exactly on pace; otherwise the worker alternates tiny deficit-only advances
    ///   with waits and can under-feed the host/drive protocol path.
    /// Acceptance: at 1 MHz and 100%, an on-pace tick still advances the 2 ms quantum.
    /// </summary>
    [Fact]
    public void LimitedAdvance_OnPaceStillAdvancesOnePacedQuantum()
    {
        var advance = SemaphoreEmulationGate.ComputeLimitedAdvanceCycles(
            frequencyHz: 1_000_000,
            limiterRatePercent: 100,
            swFreq: 1_000,
            anchorWall: 0,
            anchorCycle: 0,
            now: 2,
            totalCycles: 2_000,
            out var resync);

        Assert.False(resync);
        Assert.Equal(2_000, advance);
    }

    /// <summary>
    /// FR-PACESEL-001 / TR-PACESEL-STRAT-001 / BUG-THROTTLE-001 / TEST-SYSINDEP-001.
    /// Use case: the Semaphore strategy must honor the limiter when computing its fixed
    ///   look-ahead quantum, matching the requested speed rather than always feeding a
    ///   full-speed slice.
    /// Acceptance: at 1 MHz and 50%, the 2 ms quantum is 1,000 cycles.
    /// </summary>
    [Fact]
    public void LimitedAdvance_QuantumUsesLimiterRatePercent()
    {
        var advance = SemaphoreEmulationGate.ComputeLimitedAdvanceCycles(
            frequencyHz: 1_000_000,
            limiterRatePercent: 50,
            swFreq: 1_000,
            anchorWall: 0,
            anchorCycle: 0,
            now: 2,
            totalCycles: 1_000,
            out var resync);

        Assert.False(resync);
        Assert.Equal(1_000, advance);
    }

    /// <summary>
    /// FR-PACESEL-001 / TR-PACESEL-STRAT-001 / BUG-THROTTLE-001 / TEST-SYSINDEP-001.
    /// Use case: if a user selects the Semaphore strategy and sets the limiter
    ///   below 100%, the emulated clock must advance at that requested fraction
    ///   rather than silently running at full speed.
    /// Acceptance: 100 ms of wall time at 1 MHz and a 50% limiter produces a
    ///   50,000-cycle deficit, not the 100,000-cycle full-speed deficit.
    /// </summary>
    [Fact]
    public void Deficit_UsesLimiterRatePercent()
    {
        var deficit = SemaphoreEmulationGate.ComputeRealtimeDeficit(
            frequencyHz: 1_000_000,
            limiterRatePercent: 50,
            swFreq: 1_000,
            anchorWall: 0,
            anchorCycle: 0,
            now: 100,
            totalCycles: 0,
            out var resync);

        Assert.False(resync);
        Assert.Equal(50_000, deficit);
    }
}
