using System.Diagnostics;
using System.Runtime.CompilerServices;
using ViceSharp.Host.Runtime;
using ViceSharp.Protocol;

namespace ViceSharp.Host.Services;

/// <summary>
/// "Xbox" pacing strategy (PLAN-XBOXUWP S2): an AppContainer-safe managed <see cref="IEmulationGate"/>
/// for the UWP/Xbox head. Unlike <see cref="SemaphoreEmulationGate"/> it uses NO OS waitable timer,
/// NO auxiliary thread, and NO P/Invoke (winmm/kernel32 are unavailable/forbidden in the app
/// partition). Each <see cref="Tick"/> advances the real-time cycle deficit plus one paced quantum
/// (shared <see cref="PacingMath"/>, so pacing is identical to the desktop gate) then yields for a
/// small fixed managed quantum; warp yields 0 and runs flat out. The sleep and clock are injectable
/// seams so pacing is deterministically unit-testable off-console.
/// </summary>
public sealed class XboxManagedFrameGate : IEmulationGate
{
    private static readonly int PacedSleepMs = Math.Max(1, (int)Math.Round(1000.0 / PacingMath.PacingHz));

    private readonly Action<int> _sleep;
    private readonly Func<long> _nowTicks;
    private readonly long _swFreq;
    private readonly ConditionalWeakTable<EmulatorRuntimeSession, Anchor> _anchors = new();

    public XboxManagedFrameGate(Action<int>? sleep = null, Func<long>? nowTicks = null, long? stopwatchFrequency = null)
    {
        _sleep = sleep ?? (static ms => { if (ms > 0) Thread.Sleep(ms); });
        _nowTicks = nowTicks ?? Stopwatch.GetTimestamp;
        _swFreq = stopwatchFrequency ?? Stopwatch.Frequency;
    }

    public string Name => "Xbox";

    // No auxiliary timer thread: pacing is inline in Tick via the managed sleep seam, so
    // Start/Stop are idempotent no-ops (critical on a 2-4 core AppContainer partition).
    public void Start()
    {
    }

    public void Stop()
    {
    }

    public void Dispose()
    {
    }

    public bool Tick(EmulatorRuntimeRegistry registry, Func<EmulatorRuntimeSession, long, long> advance)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(advance);

        var ranAny = false;
        var anyWarp = false;
        var now = _nowTicks();

        foreach (var session in registry.Snapshot())
        {
            if (session.RunState != EmulatorRunState.Running)
                continue;

            ranAny = true;
            var anchor = _anchors.GetValue(session, static _ => new Anchor());

            if (!session.LimiterEnabled)
            {
                anyWarp = true;
                anchor.Primed = false; // re-prime from "now" when the limiter returns
                advance(session, WarpSliceCycles(session));
                continue;
            }

            PaceLimited(session, anchor, advance, now);
        }

        if (!ranAny)
            return false;

        // Limiter on: yield one small managed quantum (AppContainer-safe, no OS timer, no spin).
        // Warp: yield 0 so it runs flat out. The deficit term (PacingMath) makes the emulated
        // clock track real time regardless of the managed sleep's granularity.
        _sleep(anyWarp ? 0 : PacedSleepMs);
        return true;
    }

    private void PaceLimited(
        EmulatorRuntimeSession session,
        Anchor anchor,
        Func<EmulatorRuntimeSession, long, long> advance,
        long now)
    {
        long frequencyHz;
        long totalCycles;
        lock (session.SyncRoot)
        {
            var clock = session.Machine.Clock;
            frequencyHz = clock.FrequencyHz;
            totalCycles = clock.TotalCycles;
        }

        if (!anchor.Primed)
        {
            anchor.AnchorWall = now;
            anchor.AnchorCycle = totalCycles;
            anchor.Primed = true;
        }

        var cyclesToAdvance = PacingMath.ComputeLimitedAdvanceCycles(
            frequencyHz,
            session.LimiterRatePercent,
            _swFreq,
            anchor.AnchorWall,
            anchor.AnchorCycle,
            now,
            totalCycles,
            out var resync);

        if (resync)
        {
            anchor.AnchorWall = now;
            anchor.AnchorCycle = totalCycles;
        }

        if (cyclesToAdvance > 0)
            advance(session, cyclesToAdvance);
    }

    private static long WarpSliceCycles(EmulatorRuntimeSession session)
    {
        lock (session.SyncRoot)
            return PacingMath.WarpSliceCycles(session.Machine.Clock.FrequencyHz);
    }

    private sealed class Anchor
    {
        public bool Primed;
        public long AnchorWall;
        public long AnchorCycle;
    }
}
