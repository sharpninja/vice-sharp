using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using ViceSharp.Abstractions;
using ViceSharp.Host.Runtime;
using ViceSharp.Protocol;

namespace ViceSharp.Host.Services;

/// <summary>
/// "VICE" pacing strategy - faithful to VICE's Layer-3 outer throttle (vsync.c / sound.c).
/// Two regulators, in precedence order:
///
///  1. Sound-buffer back-pressure (sound.c sound_flush): when the audio device is the
///     timing source (the SID has a live backend and a configured audio clock), the
///     emulator advances to the next sync point and the audio backend blocks only when
///     a completed sound fragment is written to a full device queue. This matches VICE's
///     sound_flush loop: produce sound first, then write a whole fragment when one fits,
///     otherwise retry after a 1 ms sleep. Takes precedence over vsync: when sound is the
///     timing source, vsync is skipped for that session.
///
///  2. vsync (vsync.c set_timer_speed / vsync_do_vsync): precompute emulated_clk_per_second
///     = master clock x speed, then convert the emulated-cycle delta since the anchor into
///     how many host ticks should have passed and SLEEP the remainder (an OS waitable timer,
///     never a busy-spin). Crucially it targets a wall-clock time derived from CYCLE PROGRESS,
///     not a fixed frame rate. warp_enabled skips all of it.
///
/// The worker advances in clean whole-instruction groups (the pump owns that); this gate
/// only decides how much to run per tick and how to sleep.
/// </summary>
public sealed partial class ViceEmulationGate : IEmulationGate
{
    // Advance ~2 ms of emulated time per tick, then sleep to pace (vsync). The fine slice
    // matters for AUDIO: the SID flushes samples only while advancing, and the device drains a
    // 256-sample buffer every ~5.8 ms, so a coarse 10 ms slice starved it (underrun glitches).
    // 2 ms keeps submissions comfortably ahead of the drain - matching the Semaphore gate's
    // 1 ms granularity that plays cleanly - while the SleepUntil deadline keeps the pace at 100%.
    private const double ChunkHz = 500.0;
    private const long WarpBurstMultiplier = 64;

    private readonly ConditionalWeakTable<EmulatorRuntimeSession, Anchor> _anchors = new();
    private volatile bool _running;
    private bool _raisedTimerResolution;

    public string Name => "VICE";

    /// <summary>Which regulator was applied to the most recently processed running session
    /// in the last <see cref="Tick"/> (diagnostics / tests).</summary>
    internal PacingRegulator LastRegulator { get; private set; }

    /// <summary>Regulator that paced a session on a given tick.</summary>
    internal enum PacingRegulator
    {
        Warp,
        Vsync,
        Sound,
    }

    /// <summary>Outcome of the sound back-pressure decision for one session.</summary>
    internal enum SoundAction
    {
        /// <summary>No live audio: the gate should fall through to vsync.</summary>
        NotTimingSource,

        /// <summary>Sound is the timing source: advance and let the audio write block if needed.</summary>
        Advance,
    }

    /// <summary>
    /// Regulator 1 decision: given the session's audio chip, decide whether sound is the
    /// timing source. VICE produces sound before checking output capacity; the backend write
    /// performs the fragment-space wait if a completed fragment cannot be accepted.
    /// Pure - the caller performs the advance.
    /// </summary>
    internal static SoundAction EvaluateSound(IAudioChip? chip)
    {
        if (chip is null || !chip.IsAudioTimingSource)
            return SoundAction.NotTimingSource;

        return SoundAction.Advance;
    }

    /// <summary>
    /// vsync self-correction (TEST-SYSINDEP-001 AC4): the cycles still owed to real time since
    /// the anchor - shouldHave(now) minus already-done - clamped to a per-tick cap so one tick
    /// never holds the lock too long, with the remainder carried forward by the persistent
    /// anchor (so cycles are never dropped, unlike a fixed chunk). A gap beyond the
    /// catastrophic threshold sets <paramref name="resync"/> so the caller re-anchors to now
    /// instead of sprinting. Mirrors <see cref="SemaphoreEmulationGate"/>'s proven deficit pacer.
    /// </summary>
    internal static long ComputeRealtimeDeficit(
        long emulatedClkPerSecond,
        long swFreq,
        long anchorWall,
        long anchorCycle,
        long now,
        long totalCycles,
        out bool resync)
    {
        resync = false;
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

    public void Start()
    {
        _running = true;
        RaiseTimerResolution(true);
    }

    public void Stop()
    {
        _running = false;
        RaiseTimerResolution(false);
    }

    public void Dispose() => Stop();

    public bool Tick(EmulatorRuntimeRegistry registry, Func<EmulatorRuntimeSession, long, long> advance)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(advance);

        var ranAny = false;
        var swFreq = Stopwatch.Frequency;

        foreach (var session in registry.Snapshot())
        {
            if (session.RunState != EmulatorRunState.Running)
                continue;

            ranAny = true;
            var anchor = _anchors.GetValue(session, static _ => new Anchor());

            long frequencyHz;
            lock (session.SyncRoot)
                frequencyHz = session.Machine.Clock.FrequencyHz;

            var chunk = Math.Max(1, (long)(frequencyHz / ChunkHz));

            if (!session.LimiterEnabled)
            {
                // warp_enabled: highest precedence - run flat out, no pacing.
                anchor.Primed = false;
                advance(session, chunk * WarpBurstMultiplier);
                LastRegulator = PacingRegulator.Warp;
                continue;
            }

            IAudioChip? audioChip;
            lock (session.SyncRoot)
                audioChip = session.Machine.Devices.GetByRole(DeviceRole.AudioChip) as IAudioChip;

            var soundAction = EvaluateSound(audioChip);
            if (soundAction != SoundAction.NotTimingSource)
            {
                LastRegulator = PacingRegulator.Sound;
                advance(session, chunk);
                continue;
            }

            // vsync (vsync.c): when sound is not the timing source, run a chunk then sleep so
            // wall-clock tracks emulated-cycle progress - a true 100% pace.
            LastRegulator = PacingRegulator.Vsync;

            var speed = Math.Clamp(session.LimiterRatePercent, 1.0, 100_000.0) / 100.0;
            var emulatedClkPerSecond = (long)(frequencyHz * speed);

            long totalCycles;
            lock (session.SyncRoot)
                totalCycles = session.Machine.Clock.TotalCycles;

            var now = Stopwatch.GetTimestamp();
            if (!anchor.Primed)
            {
                anchor.AnchorWall = now;
                anchor.AnchorCycle = totalCycles;
                anchor.Primed = true;
            }

            // Advance the real-time cycle DEFICIT (self-correcting) plus one fine chunk of
            // look-ahead, instead of a fixed chunk: cycles the worker could not advance in a
            // tick persist in the deficit (the anchor stays put) and are caught up on following
            // ticks, so the system sustains its own clock under per-cycle load (audio on)
            // rather than silently throttling - the diagnosed fixed-chunk defect. The fine
            // look-ahead chunk keeps the SID fed and, with SleepUntil below, paces steady state
            // at ~ChunkHz. Mirrors the Semaphore gate's proven deficit pacer (PaceLimited).
            var deficit = ComputeRealtimeDeficit(emulatedClkPerSecond, swFreq, anchor.AnchorWall, anchor.AnchorCycle, now, totalCycles, out var resync);
            if (resync)
            {
                // Hopelessly behind (host stall / debugger break): re-anchor to now.
                anchor.AnchorWall = now;
                anchor.AnchorCycle = totalCycles;
            }

            advance(session, deficit + chunk);

            lock (session.SyncRoot)
                totalCycles = session.Machine.Clock.TotalCycles;

            // vsync: sleep until wall-clock reaches the time implied by emulated-cycle progress.
            // Sleep only while ahead of that target; when still behind, skip the sleep so the
            // next tick's deficit closes the gap (the self-correction).
            var cyclesSinceAnchor = totalCycles - anchor.AnchorCycle;
            var targetWall = anchor.AnchorWall + (long)(cyclesSinceAnchor / (double)emulatedClkPerSecond * swFreq);
            if (targetWall > Stopwatch.GetTimestamp())
                SleepUntil(targetWall);

            // Re-anchor about once per emulated second to bound double-precision drift.
            if (cyclesSinceAnchor > frequencyHz)
            {
                anchor.AnchorWall = Stopwatch.GetTimestamp();
                anchor.AnchorCycle = totalCycles;
            }
        }

        return ranAny;
    }

    private void SleepUntil(long deadlineTimestamp)
    {
        var swFreq = Stopwatch.Frequency;
        while (_running)
        {
            var remaining = deadlineTimestamp - Stopwatch.GetTimestamp();
            if (remaining <= 0)
                break;

            var ms = remaining * 1000.0 / swFreq;
            if (ms > 1.5)
                Thread.Sleep(1); // ~1 ms with the raised timer resolution; yields the CPU
            else
                Thread.SpinWait(64); // final sub-millisecond only
        }
    }

    private void RaiseTimerResolution(bool raise)
    {
        if (!OperatingSystem.IsWindows())
            return;

        try
        {
            if (raise)
            {
                TimeBeginPeriod(1);
                _raisedTimerResolution = true;
            }
            else if (_raisedTimerResolution)
            {
                TimeEndPeriod(1);
                _raisedTimerResolution = false;
            }
        }
        catch
        {
            // Best-effort; pacing still works without it.
        }
    }

    private sealed class Anchor
    {
        public bool Primed;
        public long AnchorWall;
        public long AnchorCycle;
    }

    [SupportedOSPlatform("windows")]
    [LibraryImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
    private static partial uint TimeBeginPeriod(uint period);

    [SupportedOSPlatform("windows")]
    [LibraryImport("winmm.dll", EntryPoint = "timeEndPeriod")]
    private static partial uint TimeEndPeriod(uint period);
}
