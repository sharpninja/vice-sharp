namespace ViceSharp.TestHarness;

using System.Diagnostics;
using ViceSharp.Host.Runtime;
using ViceSharp.Host.Services;
using ViceSharp.Protocol;
using Xunit;

/// <summary>
/// Diagnostic probes for the deployed-app 50 percent speed report under the
/// REAL user workload: the Pieces of Light demo autostarted from Drive 8,
/// not the idle boot screen every earlier probe measured. The demo drives the
/// per-cycle VIC pipeline (VSP scroller, sprites) and the SID far harder than
/// the READY prompt, so the achievable core rate here is the honest ceiling
/// the deployed app lives under.
/// </summary>
public sealed class DemoWorkloadSpeedDiagTests : IClassFixture<Audio.WindowsAudioSessionMute>
{
    private const string DemoDiskPath =
        @"C:\Users\kingd\AppData\Local\Temp\ViceSharp\media\ce70ddf059e9474681016cdaa3772082-pieces_of_light.d64";

    /// <summary>
    /// FR: FR-C64-Boot, TR: TR-AUDIO-PACE-001, TEST: TEST-AUDIO-PACE-05.
    /// Use case: with audio disabled, warp during the demo measures the raw
    /// core headroom on the demo workload. The boot screen measures ~500
    /// percent; if the demo drops this far below real time the core itself is
    /// the bottleneck. Acceptance: (diagnostic) at least 120 percent of the
    /// PAL master clock - measured 245.7 percent standalone and 161.2 percent
    /// under a fully parallel suite on the dev machine (2026-07-07); the floor
    /// sits below contention noise but far above the ~50 percent Debug-build
    /// regression class this canary exists to catch. The failure message
    /// reports the measured rate.
    /// </summary>
    [Fact]
    public void Demo_SilentWarp_Measures_Core_Headroom()
    {
        var ambient = Environment.GetEnvironmentVariable("VICESHARP_AUDIO");
        Environment.SetEnvironmentVariable("VICESHARP_AUDIO", "0");
        var (registry, session, pump) = CreateDemoPipeline();
        try
        {
            session.SetLimiter(100, enabled: false);
            RunDemoAndMeasure(session, pump, warmupMs: 45_000, measureMs: 5_000, out var pct, out var fps);
            Assert.True(pct >= 120.0, $"DIAG demo silent warp = {pct:F1}% of real time, committed fps = {fps:F1}");
        }
        finally
        {
            pump.Dispose();
            Environment.SetEnvironmentVariable("VICESHARP_AUDIO", ambient);
            _ = registry;
        }
    }

    /// <summary>
    /// FR: FR-C64-Boot, TR: TR-AUDIO-PACE-001, TEST: TEST-AUDIO-PACE-06.
    /// Use case: the user's exact deployed configuration - live audio, the
    /// VICE pacing gate, limiter enabled at rate 1000 - during the demo. The
    /// user reports about 50 percent. Acceptance: (diagnostic) at least 90
    /// percent of the PAL master clock; the failure message reports the
    /// measured percentage for diagnosis.
    /// </summary>
    [Fact]
    public void Demo_UserConfig_LiveAudio_ViceGate_Sustains_RealTime()
    {
        Environment.SetEnvironmentVariable("VICESHARP_AUDIO", "1");
        var (registry, session, pump) = CreateDemoPipeline();
        try
        {
            session.SetLimiter(1000, enabled: true);
            RunDemoAndMeasure(session, pump, warmupMs: 45_000, measureMs: 8_000, out var pct, out var fps);
            Assert.True(pct >= 90.0, $"DIAG demo user-config pace = {pct:F1}% of real time, committed fps = {fps:F1}");
        }
        finally
        {
            pump.Dispose();
            Environment.SetEnvironmentVariable("VICESHARP_AUDIO", null);
            _ = registry;
        }
    }

    private static (EmulatorRuntimeRegistry Registry, EmulatorRuntimeSession Session, EmulationPumpService Pump) CreateDemoPipeline()
    {
        Assert.SkipUnless(File.Exists(DemoDiskPath), "Pieces of Light d64 not present on this machine.");

        var registry = new EmulatorRuntimeRegistry();
        var factory = new DefaultEmulatorRuntimeFactory();
        var session = factory.Create(new CreateEmulatorSessionRequest("c64", "", false));
        registry.Add(session);

        var media = new MediaServiceHost(registry);
        var attach = media.AttachMediaAsync(
            new AttachMediaRequest(session.SessionId, MediaSlot.Drive8, DemoDiskPath),
            CancellationToken.None).GetAwaiter().GetResult();
        Assert.True(attach.Status.IsSuccess, $"attach failed: {attach.Status.Message}");

        var pump = new EmulationPumpService(registry);
        return (registry, session, pump);
    }

    private static void RunDemoAndMeasure(
        EmulatorRuntimeSession session,
        EmulationPumpService pump,
        int warmupMs,
        int measureMs,
        out double pctRealtime,
        out double fps)
    {
        pump.StartAsync(CancellationToken.None).GetAwaiter().GetResult();

        var host = new EmulatorHostService(GetRegistry(pump, session), new DefaultEmulatorRuntimeFactory());
        var autostart = host.ResetAndAutostartDrive8Async(
            new ResetAndAutostartDrive8Request(session.SessionId),
            CancellationToken.None).GetAwaiter().GetResult();
        Assert.True(autostart.Status.IsSuccess, $"autostart failed: {autostart.Status.Message}");

        // Load + decrunch + demo intro: let the workload become the demo itself.
        Thread.Sleep(warmupMs);

        long startCycles;
        long startFrames;
        lock (session.SyncRoot)
        {
            startCycles = session.Machine.Clock.TotalCycles;
            startFrames = session.FrameCount;
        }

        var sw = Stopwatch.StartNew();
        Thread.Sleep(measureMs);
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

    // The pump owns no registry accessor; the session was added to the registry the
    // pipeline was built with, so rebuild a host over that same registry instance.
    private static EmulatorRuntimeRegistry GetRegistry(EmulationPumpService pump, EmulatorRuntimeSession session)
    {
        _ = pump;
        var registry = new EmulatorRuntimeRegistry();
        registry.Add(session);
        return registry;
    }
}
