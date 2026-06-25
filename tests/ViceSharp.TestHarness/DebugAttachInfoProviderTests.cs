namespace ViceSharp.TestHarness;

using ViceSharp.Protocol;
using Xunit;

public sealed class DebugAttachInfoProviderTests
{
    /// <summary>
    /// FR-HOST-DIAG-001 / TR-HOST-DIAG-002 / TR-HOST-DIAG-003 / TEST-UI-DIAG-001.
    /// Use case: the Debug menu copy command gives a human or agent enough
    /// information to call grpcurl without re-discovering host state.
    /// Acceptance: clipboard text includes attach JSON fields plus current
    /// status details.
    /// </summary>
    [Fact]
    public void BuildClipboardText_IncludesEndpointSessionVersionAndStatus()
    {
        const string attachJson = """
        {
          "schemaVersion": 1,
          "processId": 123,
          "endpoint": "http://127.0.0.1:51723/",
          "currentSessionId": "diag-ui",
          "protocolPackage": "vice_sharp.v1",
          "appVersion": "1.2.3-test",
          "startedAtUtc": "2026-06-24T20:00:00Z",
          "updatedAtUtc": "2026-06-24T20:00:01Z",
          "authMode": "none"
        }
        """;
        var status = new EmulatorStatusDto(
            "diag-ui",
            "minimal",
            EmulatorRunState.Running,
            42,
            new MachineStateDto(0, 0, 0, 0, 0, 0xC000, 42),
            MeasuredFps: 50,
            EffectiveClockHz: 985248,
            EffectiveClockPercent: 100);
        var providerType = DiagnosticsReflectionTestHelpers.RequiredType(
            "ViceSharp.Avalonia.Host.DebugAttachInfoProvider, ViceSharp.Avalonia");
        var method = providerType.GetMethod("BuildClipboardText");
        Assert.NotNull(method);

        var text = Assert.IsType<string>(method.Invoke(null, [attachJson, status]));

        Assert.Contains("http://127.0.0.1:51723/", text);
        Assert.Contains("diag-ui", text);
        Assert.Contains("1.2.3-test", text);
        Assert.Contains("RunState: Running", text);
        Assert.Contains("FPS: 50", text);
        Assert.Contains("ClockHz: 985248", text);
    }
}
