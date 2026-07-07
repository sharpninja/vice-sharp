namespace ViceSharp.TestHarness;

using System.Diagnostics;
using ViceSharp.Abstractions;
using ViceSharp.Core.Media;
using ViceSharp.Host.Runtime;
using ViceSharp.Host.Services;
using ViceSharp.Protocol;
using Xunit;

/// <summary>
/// FR: FR-Audio-SID, TR: TR-AUDIO-WARP-001. VICE discards live sound in warp:
/// sound_flush drops pending samples when warp is enabled and no recording is
/// active (vice sound.c:1528-1531) and the blocking device-write loop is gated
/// on <c>while (!warp_mode_enabled)</c> (sound.c:1573), while SID sample
/// calculation itself continues (SoundEmulateOnWarp defaults to 1,
/// sound.c:733). The managed port must mirror that: engaging warp pauses the
/// live audio leaf (fragments drop without blocking, so warp actually sprints)
/// while the capture tap still feeds any attached recorder; leaving warp
/// resumes live audio.
/// </summary>
public sealed class WarpAudioDiscardTests
{
    private sealed class FakeBackend : IAudioBackend
    {
        public int PauseCount;
        public int ResumeCount;
        public int SubmittedSamples;

        public int QueuedSampleCount => 0;

        public int AvailableSampleCount => int.MaxValue;

        public void SubmitSamples(ReadOnlySpan<float> samples) => SubmittedSamples += samples.Length;

        public void Pause() => PauseCount++;

        public void Resume() => ResumeCount++;

        public void Stop()
        {
        }
    }

    private static EmulatorRuntimeSession CreateMinimalSession()
    {
        var factory = new DefaultEmulatorRuntimeFactory();
        return factory.Create(new CreateEmulatorSessionRequest("minimal", "", false));
    }

    /// <summary>
    /// FR: FR-Audio-SID, TR: TR-AUDIO-WARP-001, TEST: TEST-AUDIO-WARP-01.
    /// Use case: engaging warp (limiter off) must pause the live audio leaf
    /// and re-enabling the limiter must resume it, through both SetLimiter
    /// and the LimiterEnabled property (settings restart path).
    /// Acceptance: the downstream backend sees Pause on warp entry and Resume
    /// on warp exit for each toggle path.
    /// </summary>
    [Fact]
    public void Warp_Toggles_Pause_And_Resume_The_Live_Audio_Leaf()
    {
        var session = CreateMinimalSession();
        var backend = new FakeBackend();
        session.AudioCaptureTap = new CaptureAudioTap(backend);

        session.SetLimiter(100, enabled: false);
        Assert.Equal(1, backend.PauseCount);

        session.SetLimiter(100, enabled: true);
        Assert.Equal(1, backend.ResumeCount);

        session.LimiterEnabled = false;
        Assert.Equal(2, backend.PauseCount);

        session.LimiterEnabled = true;
        Assert.Equal(2, backend.ResumeCount);
    }

    /// <summary>
    /// FR: FR-Audio-SID, TR: TR-AUDIO-WARP-001, TEST: TEST-AUDIO-WARP-03.
    /// Use case: fast-forward past 200 percent needs the live audio leaf
    /// suspended too - the SID cannot feed a 44100 Hz device at more than
    /// double speed without resampling, so above 200 percent the limiter
    /// pauses live output (discard, non-blocking) and the vsync regulator
    /// paces to the requested rate; at or below 200 percent live audio
    /// stays on. Warp behavior is unchanged.
    /// Acceptance: rate 251 pauses the leaf; returning to 100 resumes it;
    /// the 200 boundary itself never pauses.
    /// </summary>
    [Fact]
    public void FastForward_Past200_Suspends_Live_Audio_And_200_Does_Not()
    {
        var session = CreateMinimalSession();
        var backend = new FakeBackend();
        session.AudioCaptureTap = new CaptureAudioTap(backend);

        session.SetLimiter(200, enabled: true);
        Assert.Equal(0, backend.PauseCount);

        session.SetLimiter(251, enabled: true);
        Assert.Equal(1, backend.PauseCount);

        session.SetLimiter(100, enabled: true);
        Assert.Equal(1, backend.ResumeCount);

        session.LimiterRatePercent = 300;
        Assert.Equal(2, backend.PauseCount);

        session.LimiterRatePercent = 150;
        Assert.Equal(2, backend.ResumeCount);
    }

    /// <summary>
    /// FR: FR-Audio-SID, TR: TR-AUDIO-WARP-001, TEST: TEST-AUDIO-PACE-10.
    /// Use case: with live audio suspended above 200 percent, the pacing
    /// gate must fall through to the vsync regulator and actually reach the
    /// requested fast-forward rate instead of being held near 100 percent by
    /// the sound path. Acceptance (diagnostic): a 251 percent limiter with
    /// VICESHARP_AUDIO=1 sustains at least 200 percent of the PAL master
    /// clock over 4 seconds.
    /// </summary>
    [Fact]
    public async Task AppPipeline_FastForward251_WithLiveAudio_Reaches_Target()
    {
        var ct = TestContext.Current.CancellationToken;
        Environment.SetEnvironmentVariable("VICESHARP_AUDIO", "1");
        try
        {
            var registry = new EmulatorRuntimeRegistry();
            var factory = new DefaultEmulatorRuntimeFactory();
            var session = factory.Create(new CreateEmulatorSessionRequest("c64", "", false));
            registry.Add(session);
            using var pump = new EmulationPumpService(registry);

            session.SetLimiter(251, enabled: true);

            await pump.StartAsync(ct);
            session.RunState = EmulatorRunState.Running;
            await Task.Delay(1000, ct);

            long startCycles;
            lock (session.SyncRoot)
                startCycles = session.Machine.Clock.TotalCycles;

            var sw = Stopwatch.StartNew();
            await Task.Delay(4000, ct);
            sw.Stop();

            long endCycles;
            lock (session.SyncRoot)
                endCycles = session.Machine.Clock.TotalCycles;

            session.RunState = EmulatorRunState.Paused;
            await pump.StopAsync(ct);

            var pct = (endCycles - startCycles) / sw.Elapsed.TotalSeconds / 985_248.0 * 100.0;
            Assert.True(pct >= 200.0, $"DIAG fast-forward 251% pace = {pct:F1}% of real time");
        }
        finally
        {
            Environment.SetEnvironmentVariable("VICESHARP_AUDIO", null);
        }
    }

    /// <summary>
    /// FR: FR-Audio-SID, TR: TR-AUDIO-WARP-001, TEST: TEST-AUDIO-WARP-02.
    /// Use case: the restart path assigns AudioCaptureTap after LimiterEnabled
    /// (object-initializer order), so a session already in warp must pause a
    /// tap the moment it is attached.
    /// Acceptance: setting AudioCaptureTap on a warped session immediately
    /// pauses the downstream backend.
    /// </summary>
    [Fact]
    public void Tap_Attached_To_A_Warped_Session_Is_Paused_Immediately()
    {
        var session = CreateMinimalSession();
        session.SetLimiter(100, enabled: false);

        var backend = new FakeBackend();
        session.AudioCaptureTap = new CaptureAudioTap(backend);

        Assert.Equal(1, backend.PauseCount);
    }

    /// <summary>
    /// FR: FR-Audio-SID, TR: TR-AUDIO-WARP-001, TEST: TEST-AUDIO-PACE-09.
    /// Use case: the user symptom - warp with live audio was capped near 100
    /// percent because the SID's fragment writes blocked on the full WinMM
    /// ring. With warp discarding live audio (VICE sound.c:1573) the app
    /// pipeline must actually sprint. Acceptance (diagnostic): warp with
    /// VICESHARP_AUDIO=1 reaches at least 150 percent of the PAL master
    /// clock over 4 seconds; the failure message reports the measured rate.
    /// </summary>
    [Fact]
    public async Task AppPipeline_Warp_WithLiveAudio_Exceeds_RealTime()
    {
        var ct = TestContext.Current.CancellationToken;
        Environment.SetEnvironmentVariable("VICESHARP_AUDIO", "1");
        try
        {
            var registry = new EmulatorRuntimeRegistry();
            var factory = new DefaultEmulatorRuntimeFactory();
            var session = factory.Create(new CreateEmulatorSessionRequest("c64", "", false));
            registry.Add(session);
            using var pump = new EmulationPumpService(registry);

            session.SetLimiter(100, enabled: false);

            await pump.StartAsync(ct);
            session.RunState = EmulatorRunState.Running;
            await Task.Delay(1000, ct);

            long startCycles;
            lock (session.SyncRoot)
                startCycles = session.Machine.Clock.TotalCycles;

            var sw = Stopwatch.StartNew();
            await Task.Delay(4000, ct);
            sw.Stop();

            long endCycles;
            lock (session.SyncRoot)
                endCycles = session.Machine.Clock.TotalCycles;

            session.RunState = EmulatorRunState.Paused;
            await pump.StopAsync(ct);

            var pct = (endCycles - startCycles) / sw.Elapsed.TotalSeconds / 985_248.0 * 100.0;
            Assert.True(pct >= 150.0, $"DIAG warp+audio pace = {pct:F1}% of real time");
        }
        finally
        {
            Environment.SetEnvironmentVariable("VICESHARP_AUDIO", null);
        }
    }
}
