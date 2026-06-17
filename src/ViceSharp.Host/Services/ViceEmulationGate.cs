using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using ViceSharp.Host.Runtime;
using ViceSharp.Protocol;

namespace ViceSharp.Host.Services;

/// <summary>
/// "VICE" pacing strategy - faithful to VICE's Layer-3 outer throttle (vsync.c / sound.c).
/// Two regulators, in precedence order:
///
///  1. Sound-buffer back-pressure (sound.c sound_flush): when the audio device is the
///     timing source, the emulator thread blocks while the device's sample buffer is full
///     and resumes as it drains, so the fixed cycle:sample ratio paces the CPU. This
///     regulator is currently DISABLED for ViceSharp because the SID sample rate is wrong
///     (BUG-SIDAUDIO-001) - enabling it would pace the CPU at the wrong rate. It will be
///     turned on once the SID emits at the correct rate.
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
    // Advance ~10 ms of emulated time per tick, then run the regulators (VICE checks them
    // about once per frame; a sub-frame chunk keeps the sleep remainder small and smooth).
    private const double ChunkHz = 100.0;
    private const long WarpBurstMultiplier = 64;

    private readonly ConditionalWeakTable<EmulatorRuntimeSession, Anchor> _anchors = new();
    private volatile bool _running;
    private bool _raisedTimerResolution;

    public string Name => "VICE";

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
                // warp_enabled: skip both regulators, run flat out.
                anchor.Primed = false;
                advance(session, chunk * WarpBurstMultiplier);
                continue;
            }

            // Run one chunk of emulated time (samples are emitted to the audio device here;
            // the sound regulator would back-pressure on them once the SID rate is correct).
            advance(session, chunk);

            // Regulator 2 - vsync: sleep so wall-clock tracks emulated-cycle progress.
            long totalCycles;
            lock (session.SyncRoot)
                totalCycles = session.Machine.Clock.TotalCycles;

            var speed = Math.Clamp(session.LimiterRatePercent, 1.0, 100_000.0) / 100.0;
            var emulatedClkPerSecond = frequencyHz * speed;
            var now = Stopwatch.GetTimestamp();

            if (!anchor.Primed)
            {
                anchor.AnchorWall = now;
                anchor.AnchorCycle = totalCycles;
                anchor.Primed = true;
                continue;
            }

            var cyclesSinceAnchor = totalCycles - anchor.AnchorCycle;
            var targetWall = anchor.AnchorWall + (long)(cyclesSinceAnchor / emulatedClkPerSecond * swFreq);
            var maxDriftTicks = swFreq / 4; // 250 ms behind => resync rather than sprint

            if (targetWall < now - maxDriftTicks)
            {
                // Fell far behind (host stall / debugger break): resync to now.
                anchor.AnchorWall = now;
                anchor.AnchorCycle = totalCycles;
            }
            else
            {
                SleepUntil(targetWall);
            }

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
