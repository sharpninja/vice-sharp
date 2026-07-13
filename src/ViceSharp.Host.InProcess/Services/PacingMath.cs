namespace ViceSharp.Host.Services;

/// <summary>
/// Pure, allocation-free, P/Invoke-free pacing math shared by the emulation gates
/// (PLAN-XBOXUWP S2). Extracted verbatim from <see cref="SemaphoreEmulationGate"/>
/// (BUG-THROTTLE-001) so the desktop timer gate and the AppContainer-safe
/// <see cref="XboxManagedFrameGate"/> compute the same real-time cycle deficit plus
/// paced quantum. No OS timer, no threads, no marshalling: safe for the UWP/Xbox
/// Native-AOT app partition, and directly unit-testable off-console.
/// </summary>
internal static class PacingMath
{
    /// <summary>Nominal pacing frequency: one paced quantum is one clock-second divided by this.</summary>
    public const double PacingHz = 500.0;

    /// <summary>A warp tick advances this many paced quanta.</summary>
    public const long WarpBurstMultiplier = 64;

    /// <summary>
    /// Real-time cycle deficit since the anchor plus one paced quantum. The fixed quantum keeps
    /// the host/drive protocol path fed even when the anchor says the clock is exactly on pace,
    /// while the deficit term catches up timer jitter. <paramref name="resync"/> is true only on a
    /// catastrophic gap (debugger break / machine sleep), which the caller re-anchors instead of
    /// catching up.
    /// </summary>
    public static long ComputeLimitedAdvanceCycles(
        long frequencyHz,
        double limiterRatePercent,
        long swFreq,
        long anchorWall,
        long anchorCycle,
        long now,
        long totalCycles,
        out bool resync)
    {
        var deficit = ComputeRealtimeDeficit(
            frequencyHz, limiterRatePercent, swFreq, anchorWall, anchorCycle, now, totalCycles, out resync);

        return deficit + ComputePacedQuantumCycles(frequencyHz, limiterRatePercent);
    }

    /// <summary>
    /// Cycles the emulated clock is behind real time since the anchor, clamped to a per-step cap
    /// (a quarter clock-second) and never negative. A deficit above four clock-seconds is treated
    /// as catastrophic: <paramref name="resync"/> is set and the cap is returned so the caller
    /// re-anchors rather than fast-forwarding minutes of skipped time.
    /// </summary>
    public static long ComputeRealtimeDeficit(
        long frequencyHz,
        double limiterRatePercent,
        long swFreq,
        long anchorWall,
        long anchorCycle,
        long now,
        long totalCycles,
        out bool resync)
    {
        resync = false;
        var speed = Math.Clamp(limiterRatePercent, 1.0, 100_000.0) / 100.0;
        var emulatedClkPerSecond = Math.Max(1, (long)(frequencyHz * speed));
        var elapsedSeconds = (now - anchorWall) / (double)swFreq;
        var deficit = (long)(emulatedClkPerSecond * elapsedSeconds) - (totalCycles - anchorCycle);

        var stepCap = Math.Max(1, emulatedClkPerSecond / 4);
        var catastrophic = emulatedClkPerSecond * 4;

        if (deficit > catastrophic)
        {
            resync = true;
            return stepCap;
        }

        if (deficit > stepCap)
            return stepCap;

        return deficit < 0 ? 0 : deficit;
    }

    /// <summary>One paced quantum: a clock-second (scaled by the limiter rate) divided by <see cref="PacingHz"/>.</summary>
    public static long ComputePacedQuantumCycles(long frequencyHz, double limiterRatePercent)
    {
        var speed = Math.Clamp(limiterRatePercent, 1.0, 100_000.0) / 100.0;
        return Math.Max(1, (long)(frequencyHz * speed / PacingHz));
    }

    /// <summary>Cycles advanced per warp tick: one paced quantum times <see cref="WarpBurstMultiplier"/>.</summary>
    public static long WarpSliceCycles(long frequencyHz)
        => Math.Max(1, (long)(frequencyHz / PacingHz)) * WarpBurstMultiplier;
}
