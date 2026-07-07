namespace ViceSharp.TestHarness;

using System.Diagnostics;
using ViceSharp.Host.Runtime;
using ViceSharp.Host.Services;
using ViceSharp.Protocol;
using Xunit;

/// <summary>
/// Diagnostic probe for the deployed-app 50 percent speed report: drives the
/// REAL host pipeline (DefaultEmulatorRuntimeFactory session, the Semaphore
/// gate default, the EmulationPumpService worker) exactly like the MSI app
/// and measures the achieved emulated-cycle rate against the PAL master
/// clock. The headless core alone measures ~497 percent, so any large gap
/// here lives in the session/pump/gate/audio pipeline.
/// </summary>
public sealed class EmulationPipelineSpeedDiagTests
{
    /// <summary>
    /// FR: FR-C64-Boot, TR: TR-AUDIO-PACE-001, TEST: TEST-AUDIO-PACE-02.
    /// Use case: the app pipeline at 100 percent limiter must sustain close
    /// to real time on a machine whose bare core runs several times real
    /// time. Acceptance: (diagnostic) the achieved rate over 4 seconds is at
    /// least 90 percent of the PAL master clock; the failure message reports
    /// the measured percentage for diagnosis.
    /// </summary>
    [Fact]
    public void AppPipeline_Limited_Sustains_RealTime()
    {
        var (registry, session, pump) = CreateAppPipeline();
        try
        {
            RunAndMeasure(session, pump, out var pct, out var fps);
            Assert.True(pct >= 90.0, $"DIAG limited pace = {pct:F1}% of real time, committed fps = {fps:F1}");
        }
        finally
        {
            pump.Dispose();
            _ = registry;
        }
    }

    /// <summary>
    /// FR: FR-C64-Boot, TR: TR-AUDIO-PACE-001, TEST: TEST-AUDIO-PACE-03.
    /// Use case: warp (limiter off) must exceed real time on this hardware
    /// (bare core ~497 percent). Acceptance: (diagnostic) at least 150
    /// percent of the PAL master clock over 4 seconds.
    /// </summary>
    [Fact]
    public void AppPipeline_Warp_Exceeds_RealTime()
    {
        var (registry, session, pump) = CreateAppPipeline();
        try
        {
            session.SetLimiter(100, enabled: false);
            RunAndMeasure(session, pump, out var pct, out var fps);
            Assert.True(pct >= 150.0, $"DIAG warp pace = {pct:F1}% of real time, committed fps = {fps:F1}");
        }
        finally
        {
            pump.Dispose();
            _ = registry;
        }
    }

    /// <summary>
    /// FR: FR-C64-Boot, TR: TR-AUDIO-PACE-001, TEST: TEST-AUDIO-PACE-04.
    /// Use case: the True Drive session (a second full machine stepped with
    /// the C64) must also sustain real time at 100 percent limiter.
    /// Acceptance: (diagnostic) at least 90 percent of the PAL master clock.
    /// </summary>
    [Fact]
    public void AppPipeline_TrueDrive_Limited_Sustains_RealTime()
    {
        var (registry, session, pump) = CreateAppPipeline(trueDrive: true);
        try
        {
            RunAndMeasure(session, pump, out var pct, out var fps);
            Assert.True(pct >= 90.0, $"DIAG truedrive limited pace = {pct:F1}% of real time, committed fps = {fps:F1}");
        }
        finally
        {
            pump.Dispose();
            _ = registry;
        }
    }

    private static (EmulatorRuntimeRegistry Registry, EmulatorRuntimeSession Session, EmulationPumpService Pump) CreateAppPipeline(bool trueDrive = false)
    {
        var registry = new EmulatorRuntimeRegistry();
        var factory = new DefaultEmulatorRuntimeFactory();
        var session = factory.Create(new CreateEmulatorSessionRequest("c64", "", trueDrive));
        registry.Add(session);
        var pump = new EmulationPumpService(registry);
        return (registry, session, pump);
    }

    private static void RunAndMeasure(EmulatorRuntimeSession session, EmulationPumpService pump, out double pctRealtime, out double fps)
    {
        pump.StartAsync(CancellationToken.None).GetAwaiter().GetResult();
        session.RunState = EmulatorRunState.Running;

        // Warmup.
        Thread.Sleep(1000);

        long startCycles;
        long startFrames;
        lock (session.SyncRoot)
        {
            startCycles = session.Machine.Clock.TotalCycles;
            startFrames = session.FrameCount;
        }

        var sw = Stopwatch.StartNew();
        Thread.Sleep(4000);
        sw.Stop();

        long endCycles;
        long endFrames;
        lock (session.SyncRoot)
        {
            endCycles = session.Machine.Clock.TotalCycles;
            endFrames = session.FrameCount;
        }

        session.RunState = EmulatorRunState.Paused;
        pump.StopAsync(CancellationToken.None).GetAwaiter().GetResult();

        var cps = (endCycles - startCycles) / sw.Elapsed.TotalSeconds;
        pctRealtime = cps / 985_248.0 * 100.0;
        fps = (endFrames - startFrames) / sw.Elapsed.TotalSeconds;
    }
}
