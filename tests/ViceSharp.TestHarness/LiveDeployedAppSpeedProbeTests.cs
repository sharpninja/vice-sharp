namespace ViceSharp.TestHarness;

using System.Text.Json;
using ViceSharp.Avalonia.Host;
using ViceSharp.Protocol;
using Xunit;

/// <summary>
/// Live probe for the deployed-app 50 percent speed report: attaches over the
/// running installed app's debug-attach gRPC endpoint (published in
/// %LOCALAPPDATA%/ViceSharp/debug-attach.json), autostarts the Pieces of
/// Light demo exactly like the user, and samples the app's own
/// EffectiveClockPercent and MeasuredFramesPerSecond. This measures the REAL
/// production composition - Avalonia UI, in-process gRPC host, live WinMM
/// audio, persisted user settings - which no headless probe reproduces.
/// </summary>
public sealed class LiveDeployedAppSpeedProbeTests
{
    private const string DemoDiskPath =
        @"C:\Users\kingd\AppData\Local\Temp\ViceSharp\media\ce70ddf059e9474681016cdaa3772082-pieces_of_light.d64";

    /// <summary>
    /// FR: FR-C64-Boot, TR: TR-AUDIO-PACE-001, TEST: TEST-AUDIO-PACE-07.
    /// Use case: the deployed app must sustain real time while running the
    /// demo the user reported at about 50 percent. Acceptance (diagnostic):
    /// the app's own effective clock percent averages at least 90 over the
    /// sampled window; the failure message reports the measured percentages
    /// at READY and during the demo.
    /// </summary>
    [Fact]
    public async Task DeployedApp_Demo_Sustains_RealTime()
    {
        Assert.SkipUnless(
            Environment.GetEnvironmentVariable("VICESHARP_LIVE_PROBE") == "1",
            "Live-app probe (attaches media and resets the running deployed app); opt in with VICESHARP_LIVE_PROBE=1.");
        var attachPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ViceSharp",
            "debug-attach.json");
        Assert.SkipUnless(File.Exists(attachPath), "No running deployed app (debug-attach.json absent).");
        Assert.SkipUnless(File.Exists(DemoDiskPath), "Pieces of Light d64 not present.");

        var ct = TestContext.Current.CancellationToken;

        using var doc = JsonDocument.Parse(File.ReadAllText(attachPath));
        var endpoint = new Uri(doc.RootElement.GetProperty("endpoint").GetString()!);
        var sessionId = doc.RootElement.GetProperty("currentSessionId").GetString() ?? string.Empty;

        // NOTE: deliberately never disposed - GrpcHostProtocolClient.Dispose shuts the
        // session down, which would freeze the app instance under test ("session was
        // not found" in its status bar) the moment the probe finishes.
        var client = new GrpcHostProtocolClient(endpoint, sessionId);
        {
            var readyStats = await SampleStatusAsync(client, samples: 8, intervalMs: 500, ct);

            var attach = await client.AttachMediaAsync(MediaSlot.Drive8, DemoDiskPath, isReadOnly: false, ct);
            Assert.True(attach.Status.IsSuccess, $"attach failed: {attach.Status.Message}");

            var autostart = await client.ResetAndAutostartDrive8Async(ct);
            Assert.True(autostart.Status.IsSuccess, $"autostart failed: {autostart.Status.Message}");

            // Load + decrunch + intro: sample once the demo is the workload.
            await Task.Delay(45_000, ct);
            var demoStats = await SampleStatusAsync(client, samples: 16, intervalMs: 500, ct);

            var report =
                $"READY: clock {readyStats.ClockPct:F1}% fps {readyStats.Fps:F1} | " +
                $"DEMO: clock {demoStats.ClockPct:F1}% fps {demoStats.Fps:F1} " +
                $"(limiter {demoStats.LimiterRate:F0}%, run {demoStats.RunState})";
            // Threshold overridable for diagnostics: setting it above any reachable
            // speed makes the probe fail-with-report, printing the measured numbers.
            var minPct = double.TryParse(
                Environment.GetEnvironmentVariable("VICESHARP_LIVE_PROBE_MIN"), out var min) ? min : 90.0;
            Assert.True(demoStats.ClockPct >= minPct, $"DIAG live app {report}");
        }
    }

    private static async Task<(double ClockPct, double Fps, double LimiterRate, string RunState)> SampleStatusAsync(
        GrpcHostProtocolClient client, int samples, int intervalMs, CancellationToken ct)
    {
        double clockSum = 0;
        double fpsSum = 0;
        double limiter = 0;
        string runState = "?";
        var taken = 0;

        for (var i = 0; i < samples; i++)
        {
            var response = await client.GetStatusAsync(ct);
            if (response.Status.IsSuccess && response.EmulatorStatus is { } status)
            {
                clockSum += status.EffectiveClockPercent;
                fpsSum += status.MeasuredFramesPerSecond;
                limiter = status.LimiterRatePercent;
                runState = status.RunState.ToString();
                taken++;
            }

            await Task.Delay(intervalMs, ct);
        }

        return taken == 0
            ? (0, 0, 0, "no-samples")
            : (clockSum / taken, fpsSum / taken, limiter, runState);
    }
}
