using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using ViceSharp.Host.Runtime;
using ViceSharp.Protocol;

namespace ViceSharp.Host.Services;

/// <summary>
/// "Semaphore" pacing strategy (BUG-THROTTLE-001): a high-resolution OS waitable timer
/// fires at <see cref="PacingHz"/> and releases a <see cref="SemaphoreSlim"/>; the worker
/// blocks on it (yields the CPU, never spins) and advances the real-time cycle deficit
/// since its anchor each tick, so the emulated clock tracks real time regardless of timer
/// jitter. Warp passes the semaphore non-blocking (Wait(0)) and runs flat out.
/// </summary>
public sealed partial class SemaphoreEmulationGate : IEmulationGate
{
    private const uint CreateWaitableTimerHighResolution = 0x00000002;
    private const uint TimerAllAccess = 0x1F0003;
    private const uint WaitTimeoutInterval = 50; // ms; re-check _running if the timer stalls

    private readonly SemaphoreSlim _tick = new(0, 1);
    private readonly ConditionalWeakTable<EmulatorRuntimeSession, Anchor> _anchors = new();
    private Thread? _timerThread;
    private volatile bool _running;
    private bool _raisedTimerResolution;

    public string Name => "Semaphore";

    public void Start()
    {
        if (_running)
            return;

        _running = true;
        RaiseTimerResolution(true);
        _timerThread = new Thread(TimerLoop)
        {
            IsBackground = true,
            Name = "ViceSharp.Emulation.SemaphoreTimer",
            Priority = ThreadPriority.AboveNormal,
        };
        _timerThread.Start();
    }

    public void Stop()
    {
        if (!_running && _timerThread is null)
            return;

        _running = false;
        try { _tick.Release(); } catch (SemaphoreFullException) { }
        _timerThread?.Join(TimeSpan.FromSeconds(2));
        _timerThread = null;
        RaiseTimerResolution(false);
    }

    public void Dispose()
    {
        Stop();
        _tick.Dispose();
    }

    public bool Tick(EmulatorRuntimeRegistry registry, Func<EmulatorRuntimeSession, long, long> advance)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(advance);

        var ranAny = false;
        var anyWarp = false;
        var now = Stopwatch.GetTimestamp();
        var swFreq = Stopwatch.Frequency;

        foreach (var session in registry.Snapshot())
        {
            if (session.RunState != EmulatorRunState.Running)
            {
                // FIX-XMENUWARP-001 (shared with XboxManagedFrameGate): a paused
                // session accrues no real-time debt; drop the anchor so resume
                // re-primes from "now" instead of sprinting the pause away.
                if (_anchors.TryGetValue(session, out var idleAnchor))
                    idleAnchor.Primed = false;
                continue;
            }

            ranAny = true;
            var anchor = _anchors.GetValue(session, static _ => new Anchor());

            if (!session.LimiterEnabled)
            {
                anyWarp = true;
                anchor.Primed = false; // re-prime from "now" when the limiter returns
                advance(session, WarpSliceCycles(session));
                continue;
            }

            PaceLimited(session, anchor, advance, now, swFreq);
        }

        if (!ranAny)
            return false;

        // Limiter on: block (yield) until the next high-resolution tick - never spins.
        // Warp: pass through without blocking (Wait 0) so it always runs flat out.
        _tick.Wait(anyWarp ? 0 : (int)WaitTimeoutInterval);
        return true;
    }

    private static void PaceLimited(
        EmulatorRuntimeSession session,
        Anchor anchor,
        Func<EmulatorRuntimeSession, long, long> advance,
        long now,
        long swFreq)
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

        var cyclesToAdvance = ComputeLimitedAdvanceCycles(
            frequencyHz,
            session.LimiterRatePercent,
            swFreq,
            anchor.AnchorWall,
            anchor.AnchorCycle,
            now,
            totalCycles,
            out var resync);

        // Advance the real-time deficit plus one paced quantum. The fixed quantum keeps
        // the host/drive protocol path fed even when the persistent anchor says the master
        // clock is exactly on pace, while the deficit term still catches up timer jitter.
        // Bound one Step's wall time (~250 ms emulated) so the lock is not held too long;
        // carry the remainder forward (anchor stays put) so cycles are never dropped and
        // the worker reaches 100% even under irregular wakeups. Only a catastrophic gap
        // (debugger break / sleep) is resynced rather than caught up.
        if (resync)
        {
            anchor.AnchorWall = now;
            anchor.AnchorCycle = totalCycles;
        }

        if (cyclesToAdvance > 0)
            advance(session, cyclesToAdvance);
    }

    // Pacing math is shared with XboxManagedFrameGate via PacingMath (PLAN-XBOXUWP S2).
    // These thin delegates preserve the internal test surface (SemaphoreEmulationGatePacingTests)
    // while both gates compute from one implementation.
    internal static long ComputeLimitedAdvanceCycles(
        long frequencyHz,
        double limiterRatePercent,
        long swFreq,
        long anchorWall,
        long anchorCycle,
        long now,
        long totalCycles,
        out bool resync)
        => PacingMath.ComputeLimitedAdvanceCycles(
            frequencyHz, limiterRatePercent, swFreq, anchorWall, anchorCycle, now, totalCycles, out resync);

    internal static long ComputeRealtimeDeficit(
        long frequencyHz,
        double limiterRatePercent,
        long swFreq,
        long anchorWall,
        long anchorCycle,
        long now,
        long totalCycles,
        out bool resync)
        => PacingMath.ComputeRealtimeDeficit(
            frequencyHz, limiterRatePercent, swFreq, anchorWall, anchorCycle, now, totalCycles, out resync);

    private static long WarpSliceCycles(EmulatorRuntimeSession session)
    {
        lock (session.SyncRoot)
            return PacingMath.WarpSliceCycles(session.Machine.Clock.FrequencyHz);
    }

    private void TimerLoop()
    {
        if (OperatingSystem.IsWindows() && RunHighResolutionTimer())
            return;

        RunFallbackTimer();
    }

    [SupportedOSPlatform("windows")]
    private bool RunHighResolutionTimer()
    {
        var periodMs = Math.Max(1, (int)Math.Round(1000.0 / PacingMath.PacingHz));
        var handle = TryCreateHighResolutionTimer(periodMs);
        if (handle == IntPtr.Zero)
            return false;

        try
        {
            while (_running)
            {
                WaitForSingleObject(handle, WaitTimeoutInterval);
                ReleaseTick();
            }
        }
        finally
        {
            CloseHandle(handle);
        }

        return true;
    }

    private void RunFallbackTimer()
    {
        var periodTicks = Stopwatch.Frequency / (long)PacingMath.PacingHz;
        var next = Stopwatch.GetTimestamp() + periodTicks;
        while (_running)
        {
            while (_running && Stopwatch.GetTimestamp() < next)
                Thread.Sleep(1);
            next += periodTicks;
            ReleaseTick();
        }
    }

    private void ReleaseTick()
    {
        if (_tick.CurrentCount == 0)
        {
            try { _tick.Release(); }
            catch (SemaphoreFullException) { }
        }
    }

    [SupportedOSPlatform("windows")]
    private static IntPtr TryCreateHighResolutionTimer(int periodMs)
    {
        try
        {
            var handle = CreateWaitableTimerExW(IntPtr.Zero, IntPtr.Zero, CreateWaitableTimerHighResolution, TimerAllAccess);
            if (handle == IntPtr.Zero)
                return IntPtr.Zero;

            long dueTime = -10_000L * periodMs;
            if (!SetWaitableTimer(handle, in dueTime, periodMs, IntPtr.Zero, IntPtr.Zero, false))
            {
                CloseHandle(handle);
                return IntPtr.Zero;
            }

            return handle;
        }
        catch
        {
            return IntPtr.Zero;
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
            // Timer-resolution control is best-effort.
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

    [SupportedOSPlatform("windows")]
    [LibraryImport("kernel32.dll", EntryPoint = "CreateWaitableTimerExW", SetLastError = true)]
    private static partial IntPtr CreateWaitableTimerExW(IntPtr lpTimerAttributes, IntPtr lpTimerName, uint dwFlags, uint dwDesiredAccess);

    [SupportedOSPlatform("windows")]
    [LibraryImport("kernel32.dll", EntryPoint = "SetWaitableTimer", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetWaitableTimer(IntPtr hTimer, in long lpDueTime, int lPeriod, IntPtr pfnCompletionRoutine, IntPtr lpArgToCompletionRoutine, [MarshalAs(UnmanagedType.Bool)] bool fResume);

    [SupportedOSPlatform("windows")]
    [LibraryImport("kernel32.dll", EntryPoint = "WaitForSingleObject", SetLastError = true)]
    private static partial uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

    [SupportedOSPlatform("windows")]
    [LibraryImport("kernel32.dll", EntryPoint = "CloseHandle", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CloseHandle(IntPtr hObject);
}
