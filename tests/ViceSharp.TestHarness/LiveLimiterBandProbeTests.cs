namespace ViceSharp.TestHarness;

using System.Text;
using System.Text.Json;
using ViceSharp.Avalonia.Host;
using ViceSharp.Protocol;
using Xunit;

/// <summary>
/// Live probe: measures the deployed app's achieved speed at several limiter
/// rates inside the live-audio band (rate at or below 200) plus one
/// fast-forward rate, via the app's own EffectiveClockPercent. Diagnostic for
/// TR-AUDIO-SPEED-001 ("actual speed must scale with the limiter").
/// </summary>
public sealed class LiveLimiterBandProbeTests
{
    /// <summary>
    /// FR: FR-Audio-SID, TR: TR-AUDIO-SPEED-001, TEST: TEST-AUDIO-SPEED-03.
    /// Use case: report achieved speed at 50/100/150/200/500 percent on the
    /// running installed app. Acceptance: always fails with the measured
    /// report (diagnostic probe, opt-in).
    /// </summary>
    [Fact]
    public async Task DeployedApp_LimiterBand_ReportsAchievedSpeeds()
    {
        Assert.SkipUnless(
            Environment.GetEnvironmentVariable("VICESHARP_LIVE_PROBE") == "1",
            "Live-app probe (mutates the running deployed app's limiter); opt in with VICESHARP_LIVE_PROBE=1.");
        var attachPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ViceSharp",
            "debug-attach.json");
        Assert.SkipUnless(File.Exists(attachPath), "No running deployed app (debug-attach.json absent).");

        var ct = TestContext.Current.CancellationToken;

        using var doc = JsonDocument.Parse(File.ReadAllText(attachPath));
        var endpoint = new Uri(doc.RootElement.GetProperty("endpoint").GetString()!);
        var sessionId = doc.RootElement.GetProperty("currentSessionId").GetString() ?? string.Empty;

        // Never disposed: Dispose shuts the probed session down.
        var client = new GrpcHostProtocolClient(endpoint, sessionId);
        var report = new StringBuilder();

        foreach (var rate in new[] { 50.0, 100.0, 150.0, 200.0, 500.0 })
        {
            var update = await client.UpdateSettingsAsync(
                new UpdateSettingsRequest(sessionId, new LimiterSettingsDto(rate, true, "vice")), ct);
            if (!update.Status.IsSuccess)
            {
                report.Append($"[{rate:0}%: update FAILED {update.Status.Message}] ");
                continue;
            }

            await Task.Delay(1500, ct);
            double clockSum = 0;
            var taken = 0;
            for (var i = 0; i < 6; i++)
            {
                var response = await client.GetStatusAsync(ct);
                if (response.Status.IsSuccess && response.EmulatorStatus is { } status)
                {
                    clockSum += status.EffectiveClockPercent;
                    taken++;
                }

                await Task.Delay(500, ct);
            }

            report.Append($"[target {rate:0}%: actual {(taken == 0 ? 0 : clockSum / taken):F1}%] ");
        }

        Assert.Fail($"DIAG limiter band: {report}");
    }
}
