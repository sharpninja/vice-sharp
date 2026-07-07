namespace ViceSharp.TestHarness;

using System.Text;
using System.Text.Json;
using ViceSharp.Avalonia.Host;
using ViceSharp.Protocol;
using Xunit;

/// <summary>
/// Live settings bisect for the deployed-app 50 percent speed report: connects
/// to the running installed app, then toggles one pacing-relevant setting at a
/// time (limiter rate, pacing strategy, audio mode, warp) while sampling the
/// app's own EffectiveClockPercent, to isolate which knob carries the slowdown
/// in the REAL composition.
/// </summary>
public sealed class LiveDeployedAppSettingsBisectTests
{
    /// <summary>
    /// FR: FR-C64-Boot, TR: TR-AUDIO-PACE-001, TEST: TEST-AUDIO-PACE-08.
    /// Use case: identify the app-side pacing brake by live A/B of settings.
    /// Acceptance (diagnostic): always reports the per-phase clock percents in
    /// the failure message; the assert requires the baseline to reach 90.
    /// </summary>
    [Fact]
    public async Task DeployedApp_SettingsBisect_ReportsPhaseSpeeds()
    {
        Assert.SkipUnless(
            Environment.GetEnvironmentVariable("VICESHARP_LIVE_PROBE") == "1",
            "Live-app bisect (mutates the running deployed app's settings and always fails with its report); opt in with VICESHARP_LIVE_PROBE=1.");
        var attachPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ViceSharp",
            "debug-attach.json");
        Assert.SkipUnless(File.Exists(attachPath), "No running deployed app (debug-attach.json absent).");

        var ct = TestContext.Current.CancellationToken;

        using var doc = JsonDocument.Parse(File.ReadAllText(attachPath));
        var endpoint = new Uri(doc.RootElement.GetProperty("endpoint").GetString()!);
        var sessionId = doc.RootElement.GetProperty("currentSessionId").GetString() ?? string.Empty;

        // NOTE: deliberately never disposed - GrpcHostProtocolClient.Dispose shuts the
        // session down, which would kill the app instance under test.
        var client = new GrpcHostProtocolClient(endpoint, sessionId);
        var report = new StringBuilder();

        async Task PhaseAsync(string label, LimiterSettingsDto? limiter, AudioSettingsDto? audio)
        {
            if (limiter is not null || audio is not null)
            {
                var update = await client.UpdateSettingsAsync(
                    new UpdateSettingsRequest(sessionId, limiter, Audio: audio), ct);
                if (!update.Status.IsSuccess)
                {
                    report.Append($"[{label}: update FAILED {update.Status.Message}] ");
                    return;
                }
            }

            await Task.Delay(1500, ct); // let the new pacing settle
            double clockSum = 0;
            double fpsSum = 0;
            var taken = 0;
            for (var i = 0; i < 6; i++)
            {
                var response = await client.GetStatusAsync(ct);
                if (response.Status.IsSuccess && response.EmulatorStatus is { } status)
                {
                    clockSum += status.EffectiveClockPercent;
                    fpsSum += status.MeasuredFramesPerSecond;
                    taken++;
                }

                await Task.Delay(500, ct);
            }

            var clock = taken == 0 ? 0 : clockSum / taken;
            var fps = taken == 0 ? 0 : fpsSum / taken;
            report.Append($"[{label}: clock {clock:F1}% fps {fps:F1}] ");
        }

        await PhaseAsync("baseline(persisted)", null, null);
        await PhaseAsync("vice+100", new LimiterSettingsDto(100, true, "vice"), null);
        await PhaseAsync("semaphore+100", new LimiterSettingsDto(100, true, "semaphore"), null);
        await PhaseAsync("semaphore+1000", new LimiterSettingsDto(1000, true, "semaphore"), null);
        await PhaseAsync("vice+100+audioOff", new LimiterSettingsDto(100, true, "vice"), new AudioSettingsDto("disabled"));
        await PhaseAsync("vice+100+audioOn", new LimiterSettingsDto(100, true, "vice"), new AudioSettingsDto("enabled"));
        await PhaseAsync("vice+warp", new LimiterSettingsDto(100, false, "vice"), null);

        Assert.Fail($"DIAG live bisect: {report}");
    }
}
