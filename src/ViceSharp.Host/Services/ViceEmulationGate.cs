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
///     emulator thread blocks while the device's sample buffer is at/over its high-water
///     mark and resumes as the device drains it, so the fixed cycle:sample ratio paces the
///     CPU. Enabled now that the SID emits at the correct phi2 rate (BUG-SIDAUDIO-001 fixed;
///     the sample rate was always 44.1 kHz - only pitch was wrong). Takes precedence over
///     vsync: when sound is the timing source, vsync is skipped for that session.
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

    // ~46 ms of audio at 44.1 kHz (~2.3 PAL frames): enough buffer to ride out scheduling
    // jitter without underruns, low enough latency to feel responsive, and well under the
    // WinMm ~32K-sample ceiling where SubmitSamples would start dropping batches.
    internal const int HighWaterSamples = 2048;

    private readonly ConditionalWeakTable<EmulatorRuntimeSession, Anchor> _anchors = new();
    private volatile bool _running;
    private bool _raisedTimerResolution;

    public string Name => "VICE";

    /// <summary>Which regulator was applied to the most recently processed running session
    /// in the last <see cref="Tick"/> (diagnostics / tests).</summary>
    internal PacingRegulator LastRegulator { get; private set; }

    /// <summary>Pause invoked once per tick when a device buffer is full, to avoid a hot
    /// spin while it drains in real time. Overridable so tests pace deterministically.</summary>
    internal Action BackPressurePause { get; set; } = static () => Thread.Sleep(1);

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

        /// <summary>The device buffer has room: emit one more chunk of samples.</summary>
        Advance,

        /// <summary>The device buffer is full: block the worker until it drains.</summary>
        BackPressure,
    }

    /// <summary>
    /// Regulator 1 decision: given the session's audio chip, decide whether sound is the
    /// timing source and, if so, whether the device buffer has room (advance) or is full
    /// (back-pressure). Pure - the caller performs the advance / block.
    /// </summary>
    internal static SoundAction EvaluateSound(IAudioChip? chip, int highWaterSamples)
    {
        if (chip is null || !chip.IsAudioTimingSource)
            return SoundAction.NotTimingSource;

        return chip.QueuedSampleCount >= highWaterSamples
            ? SoundAction.BackPressure
            : SoundAction.Advance;
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
        var backPressured = false;
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
                // warp_enabled: highest precedence - skip both regulators, run flat out.
                anchor.Primed = false;
                advance(session, chunk * WarpBurstMultiplier);
                LastRegulator = PacingRegulator.Warp;
                continue;
            }

            // Regulator 1 - sound back-pressure (takes precedence over vsync): when the SID
            // is the audio timing source, pace to the device draining its sample buffer.
            var audioChip = session.Machine.Devices.GetByRole(DeviceRole.AudioChip) as IAudioChip;
            var soundAction = EvaluateSound(audioChip, HighWaterSamples);
            if (soundAction != SoundAction.NotTimingSource)
            {
                LastRegulator = PacingRegulator.Sound;
                if (soundAction == SoundAction.BackPressure)
                {
                    // Device buffer full: block (advance nothing); it drains in real time.
                    backPressured = true;
                    continue;
                }

                // Buffer has room: emit one chunk of samples. Sound is the timing source,
                // so the vsync anchor stays parked.
                advance(session, chunk);
                anchor.Primed = false;
                continue;
            }

            // Regulator 2 - vsync: run a chunk, then sleep so wall-clock tracks
            // emulated-cycle progress.
            LastRegulator = PacingRegulator.Vsync;
            advance(session, chunk);

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

        if (backPressured)
            BackPressurePause(); // let a full device buffer drain instead of hot-spinning

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
