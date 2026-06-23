namespace ViceSharp.TestHarness;

using ViceSharp.Host.Services;
using Xunit;

/// <summary>
/// TEST-SYSINDEP-001 (AC4) / FR-SYSINDEP-001 / TR-SYS-SCHED-001. The vsync pacer must advance
/// the real-time cycle DEFICIT (self-correcting), not a fixed chunk. With a fixed chunk, any
/// cycles the worker could not advance in a tick are lost, so under per-cycle load (audio on)
/// the emulated clock silently throttles below 100%. A deficit pacer keeps a persistent anchor,
/// so cycles not advanced this tick are caught up on following ticks and the system sustains
/// its own clock under load.
/// </summary>
public sealed class ViceEmulationGatePacingTests
{
    // 10 MHz stopwatch so wall math is exact; 1 MHz emulated clock so 1 wall-second == 1e6 cycles.
    private const long SwFreq = 10_000_000;
    private const long EmulatedClkPerSecond = 1_000_000;

    /// <summary>
    /// FR: FR-SYSINDEP-001, TR: TR-SYS-SCHED-001, TEST-SYSINDEP-001 (AC4).
    /// Use case: a freshly anchored system that has emulated nothing yet over a wall second
    ///   owes a full second of cycles, but a single tick must not hold the lock unbounded.
    /// Acceptance: the deficit equals the cycles that should have elapsed, clamped to the
    ///   per-tick cap (one quarter-second of cycles), with no resync for a one-second gap.
    /// </summary>
    [Fact]
    public void Deficit_OneSecondBehind_ReturnsCappedCatchUp()
    {
        var deficit = ViceEmulationGate.ComputeRealtimeDeficit(
            EmulatedClkPerSecond, SwFreq, anchorWall: 0, anchorCycle: 0,
            now: SwFreq /* 1.0s */, totalCycles: 0, out var resync);

        Assert.False(resync);
        Assert.Equal(EmulatedClkPerSecond / 4, deficit); // capped at 250k, not the full 1M
    }

    /// <summary>
    /// FR: FR-SYSINDEP-001, TR: TR-SYS-SCHED-001, TEST-SYSINDEP-001 (AC4).
    /// Use case: a system exactly on pace (emulated as many cycles as wall time allows) should
    ///   not be pushed further this tick.
    /// Acceptance: when done == should-have the deficit is zero; when ahead it is clamped to
    ///   zero (never negative), so the vsync sleep - not a negative advance - handles being early.
    /// </summary>
    [Fact]
    public void Deficit_OnPaceOrAhead_ReturnsZero()
    {
        var onPace = ViceEmulationGate.ComputeRealtimeDeficit(
            EmulatedClkPerSecond, SwFreq, anchorWall: 0, anchorCycle: 0,
            now: SwFreq / 10 /* 0.1s */, totalCycles: 100_000 /* exactly 0.1s of cycles */, out _);
        Assert.Equal(0, onPace);

        var ahead = ViceEmulationGate.ComputeRealtimeDeficit(
            EmulatedClkPerSecond, SwFreq, anchorWall: 0, anchorCycle: 0,
            now: SwFreq / 10, totalCycles: 200_000 /* ran ahead */, out _);
        Assert.Equal(0, ahead);
    }

    /// <summary>
    /// FR: FR-SYSINDEP-001, TR: TR-SYS-SCHED-001, TEST-SYSINDEP-001 (AC4).
    /// Use case: a long host stall (debugger break) leaves the system seconds behind; sprinting
    ///   to close a multi-second gap would freeze the UI, so the pacer must resync instead.
    /// Acceptance: a gap beyond the catastrophic threshold sets resync and still returns only
    ///   the capped amount (the caller re-anchors to now rather than sprinting).
    /// </summary>
    [Fact]
    public void Deficit_CatastrophicallyBehind_SignalsResyncAndCaps()
    {
        var deficit = ViceEmulationGate.ComputeRealtimeDeficit(
            EmulatedClkPerSecond, SwFreq, anchorWall: 0, anchorCycle: 0,
            now: 5 * SwFreq /* 5.0s behind */, totalCycles: 0, out var resync);

        Assert.True(resync);
        Assert.Equal(EmulatedClkPerSecond / 4, deficit);
    }

    /// <summary>
    /// FR: FR-SYSINDEP-001, TR: TR-SYS-SCHED-001, TEST-SYSINDEP-001 (AC4).
    /// Use case: under per-cycle load the worker can only advance the capped amount per tick;
    ///   the un-advanced remainder MUST NOT be lost (the fixed-chunk defect) - it must persist
    ///   so following ticks catch up. This is the core self-correction the fix delivers.
    /// Acceptance: with the SAME persistent anchor, after advancing the first tick's capped
    ///   amount the next computation still reports a positive catch-up deficit (cycles were not
    ///   dropped), and it keeps doing so until done reaches should-have.
    /// </summary>
    [Fact]
    public void Deficit_PersistsAcrossTicks_SelfCorrectsNotDropped()
    {
        // Tick 1: one second behind, advance the capped 250k.
        var t1 = ViceEmulationGate.ComputeRealtimeDeficit(
            EmulatedClkPerSecond, SwFreq, anchorWall: 0, anchorCycle: 0,
            now: SwFreq, totalCycles: 0, out _);
        Assert.Equal(250_000, t1);

        // Tick 2: same anchor + same wall time, done advanced by tick 1's 250k. A fixed-chunk
        // gate would now be "caught up" (it forgot the owed cycles); the deficit pacer still
        // owes 750k and returns another capped catch-up - cycles were NOT dropped.
        var t2 = ViceEmulationGate.ComputeRealtimeDeficit(
            EmulatedClkPerSecond, SwFreq, anchorWall: 0, anchorCycle: 0,
            now: SwFreq, totalCycles: 250_000, out _);
        Assert.Equal(250_000, t2);

        // Tick 4-ish: after advancing 750k total, only 250k remains - returned exactly, no cap.
        var t4 = ViceEmulationGate.ComputeRealtimeDeficit(
            EmulatedClkPerSecond, SwFreq, anchorWall: 0, anchorCycle: 0,
            now: SwFreq, totalCycles: 750_000, out _);
        Assert.Equal(250_000, t4);
    }
}
