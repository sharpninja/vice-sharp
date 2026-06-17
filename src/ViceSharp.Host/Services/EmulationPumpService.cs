using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Extensions.Hosting;
using ViceSharp.Host.Runtime;
using ViceSharp.Protocol;

namespace ViceSharp.Host.Services;

/// <summary>
/// Host-owned emulation worker - the day-one decoupling design (docs/Decoupling.md):
/// the emulator runs on a dedicated background thread that continuously advances
/// each Running session's frames, paced to the machine's real-time refresh rate,
/// while the UI thread only renders the committed framebuffer
/// (<see cref="LocalVideoFrameSource"/> is a pure pull). This removes the previous
/// deviation where emulation ran on the UI thread via the render timer's
/// GetFrameAsync poll, which competed with rendering and ran sub-real-time.
///
/// Registered as an <see cref="IHostedService"/> so it starts/stops with the host
/// process; unit tests that build sessions directly (no host) drive
/// <see cref="PumpSession"/> deterministically instead.
/// </summary>
public sealed partial class EmulationPumpService : IHostedService, IDisposable
{
    private readonly EmulatorRuntimeRegistry _registry;
    private Thread? _thread;
    private volatile bool _running;
    private bool _raisedTimerResolution;

    public EmulationPumpService(EmulatorRuntimeRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        _registry = registry;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_running)
            return Task.CompletedTask;

        _running = true;
        RaiseTimerResolution(true);
        _thread = new Thread(Loop)
        {
            IsBackground = true,
            Name = "ViceSharp.Emulation",
        };
        _thread.Start();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Shutdown();
        return Task.CompletedTask;
    }

    public void Dispose() => Shutdown();

    private void Shutdown()
    {
        if (!_running && _thread is null)
            return;

        _running = false;
        _thread?.Join(TimeSpan.FromSeconds(2));
        _thread = null;
        RaiseTimerResolution(false);
    }

    private void Loop()
    {
        var deadline = Stopwatch.GetTimestamp();
        while (_running)
        {
            var ranAny = false;
            var warp = false;
            var refreshHz = 50.125;

            foreach (var session in _registry.Snapshot())
            {
                if (session.RunState != EmulatorRunState.Running)
                    continue;

                PumpSession(session);
                ranAny = true;
                if (!session.LimiterEnabled)
                    warp = true;
                refreshHz = session.RefreshRateHz;
            }

            if (!ranAny)
            {
                // Nothing running: idle without burning a core, and reset the
                // pacing clock so we do not "catch up" a burst when work resumes.
                Thread.Sleep(8);
                deadline = Stopwatch.GetTimestamp();
                continue;
            }

            if (warp)
            {
                // Warp already ran a burst inside PumpSession; do not pace.
                deadline = Stopwatch.GetTimestamp();
                continue;
            }

            deadline += (long)(Stopwatch.Frequency / Math.Max(1.0, refreshHz));

            // If we fell far behind real time (host stall, debugger break), resync
            // rather than sprint to catch up.
            var now = Stopwatch.GetTimestamp();
            if (deadline < now)
                deadline = now;

            SleepUntil(deadline);
        }
    }

    /// <summary>
    /// Run the frames due for one session this tick: exactly one with the limiter
    /// on, or a warp burst (as many as fit in a ~20 ms window) with it off. Holds
    /// the session lock so it serialises with host RPC mutations and the frame
    /// pull. Returns the number of frames advanced.
    /// </summary>
    public int PumpSession(EmulatorRuntimeSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        lock (session.SyncRoot)
        {
            if (session.RunState != EmulatorRunState.Running)
                return 0;

            if (session.LimiterEnabled)
            {
                RunOneFrame(session);
                return 1;
            }

            var ran = 0;
            var deadline = Stopwatch.GetTimestamp() + Stopwatch.Frequency / 50;
            do
            {
                RunOneFrame(session);
                ran++;
            }
            while (Stopwatch.GetTimestamp() < deadline && session.RunState == EmulatorRunState.Running);

            return ran;
        }
    }

    private static void RunOneFrame(EmulatorRuntimeSession session)
    {
        session.Machine.RunFrame();
        session.RecordFrame();
        session.AdvanceHostAutomationFrame();
    }

    private void SleepUntil(long deadlineTimestamp)
    {
        while (_running)
        {
            var remaining = deadlineTimestamp - Stopwatch.GetTimestamp();
            if (remaining <= 0)
                break;

            var ms = remaining * 1000.0 / Stopwatch.Frequency;
            if (ms > 2.0)
                Thread.Sleep(1); // ~1 ms with the raised timer resolution
            else
                Thread.SpinWait(64); // spin the final sub-millisecond for steadiness
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
            // Timer-resolution control is best-effort; pacing still works without it.
        }
    }

    [SupportedOSPlatform("windows")]
    [LibraryImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
    private static partial uint TimeBeginPeriod(uint period);

    [SupportedOSPlatform("windows")]
    [LibraryImport("winmm.dll", EntryPoint = "timeEndPeriod")]
    private static partial uint TimeEndPeriod(uint period);
}
