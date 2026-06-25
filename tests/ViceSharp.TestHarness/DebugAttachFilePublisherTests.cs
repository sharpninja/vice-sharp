namespace ViceSharp.TestHarness;

using System.Text.Json;
using Xunit;

public sealed class DebugAttachFilePublisherTests
{
    /// <summary>
    /// FR-HOST-DIAG-001 / TR-HOST-DIAG-003 / TEST-HOST-DIAG-001.
    /// Use case: an external debugger needs one deterministic local file to
    /// attach without scanning ports.
    /// Acceptance: WriteAsync emits the documented JSON fields.
    /// </summary>
    [Fact]
    public async Task WriteAsync_WritesSchemaPidEndpointProtocolAndTimestamps()
    {
        var path = CreateTempPath();
        var state = CreateState("http://127.0.0.1:51723/", "diag-json");
        var publisher = CreatePublisher(path, state);
        await using var cleanup = new AsyncDisposableAdapter(publisher);

        await DiagnosticsReflectionTestHelpers.InvokeAsync(
            publisher,
            "WriteAsync",
            TestContext.Current.CancellationToken);
        using var document = JsonDocument.Parse(await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken));
        var root = document.RootElement;

        Assert.Equal(1, root.GetProperty("schemaVersion").GetInt32());
        Assert.Equal(Environment.ProcessId, root.GetProperty("processId").GetInt32());
        Assert.Equal("http://127.0.0.1:51723/", root.GetProperty("endpoint").GetString());
        Assert.Equal("diag-json", root.GetProperty("currentSessionId").GetString());
        Assert.Equal("vice_sharp.v1", root.GetProperty("protocolPackage").GetString());
        Assert.Equal("none", root.GetProperty("authMode").GetString());
        Assert.True(root.TryGetProperty("startedAtUtc", out _));
        Assert.True(root.TryGetProperty("updatedAtUtc", out _));
    }

    /// <summary>
    /// FR-HOST-DIAG-001 / TR-HOST-DIAG-002 / TR-HOST-DIAG-003 / TEST-HOST-DIAG-001.
    /// Use case: once the UI creates or recreates a session, the attach file
    /// must point external tools at the new active session.
    /// Acceptance: UpdateCurrentSessionAsync rewrites currentSessionId.
    /// </summary>
    [Fact]
    public async Task UpdateCurrentSession_RewritesCurrentSessionIdAtomically()
    {
        var path = CreateTempPath();
        var state = CreateState("http://127.0.0.1:51723/", "before-session");
        var publisher = CreatePublisher(path, state);
        await using var cleanup = new AsyncDisposableAdapter(publisher);

        await DiagnosticsReflectionTestHelpers.InvokeAsync(
            publisher,
            "WriteAsync",
            TestContext.Current.CancellationToken);
        await DiagnosticsReflectionTestHelpers.InvokeAsync(
            publisher,
            "UpdateCurrentSessionAsync",
            "after-session",
            TestContext.Current.CancellationToken);
        using var document = JsonDocument.Parse(await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken));

        Assert.Equal("after-session", document.RootElement.GetProperty("currentSessionId").GetString());
    }

    /// <summary>
    /// FR-HOST-DIAG-001 / TR-HOST-DIAG-003 / TEST-HOST-DIAG-001.
    /// Use case: stale attach files must not survive a clean app shutdown.
    /// Acceptance: DisposeAsync deletes the file best-effort.
    /// </summary>
    [Fact]
    public async Task DisposeAsync_RemovesAttachFileBestEffort()
    {
        var path = CreateTempPath();
        var state = CreateState("http://127.0.0.1:51723/", "diag-delete");
        var publisher = CreatePublisher(path, state);

        await DiagnosticsReflectionTestHelpers.InvokeAsync(
            publisher,
            "WriteAsync",
            TestContext.Current.CancellationToken);
        Assert.True(File.Exists(path));
        await DiagnosticsReflectionTestHelpers.InvokeAsync(publisher, "DisposeAsync");

        Assert.False(File.Exists(path));
    }

    private static object CreateState(string endpoint, string sessionId)
    {
        var state = DiagnosticsReflectionTestHelpers.CreateInstance(DiagnosticsReflectionTestHelpers.RequiredType(
            "ViceSharp.Host.Diagnostics.HostDiagnosticsState, ViceSharp.Host"));
        DiagnosticsReflectionTestHelpers.Invoke(state, "UpdateEndpoint", new Uri(endpoint));
        DiagnosticsReflectionTestHelpers.Invoke(state, "UpdateCurrentSession", sessionId);
        return state;
    }

    private static object CreatePublisher(string path, object state)
    {
        return DiagnosticsReflectionTestHelpers.CreateInstance(
            DiagnosticsReflectionTestHelpers.RequiredType("ViceSharp.Avalonia.Host.DebugAttachFilePublisher, ViceSharp.Avalonia"),
            path,
            state);
    }

    private static string CreateTempPath()
    {
        var directory = Path.Combine(Path.GetTempPath(), "vicesharp-diagnostics-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return Path.Combine(directory, "debug-attach.json");
    }

    private sealed class AsyncDisposableAdapter(object target) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            var method = target.GetType().GetMethod("DisposeAsync");
            if (method?.Invoke(target, null) is { } result)
                await DiagnosticsReflectionTestHelpers.AwaitAsync(result);
        }
    }
}
