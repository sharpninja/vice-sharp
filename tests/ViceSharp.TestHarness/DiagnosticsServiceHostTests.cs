namespace ViceSharp.TestHarness;

using Microsoft.Extensions.DependencyInjection;
using ViceSharp.Core;
using ViceSharp.Host.Runtime;
using ViceSharp.Host.Services;
using ViceSharp.Protocol;
using Xunit;

public sealed class DiagnosticsServiceHostTests
{
    /// <summary>
    /// FR-HOST-DIAG-001 / TR-HOST-DIAG-001 / TEST-HOST-DIAG-001.
    /// Use case: external tools need host process and protocol metadata before
    /// choosing which diagnostics calls to issue.
    /// Acceptance: GetHostInfo returns process id, endpoint, protocol package,
    /// version, and start time from the host diagnostics state.
    /// </summary>
    [Fact]
    public async Task GetHostInfo_ReturnsProcessEndpointVersionAndProtocol()
    {
        using var provider = BuildProvider();
        var state = provider.GetRequiredService(DiagnosticsReflectionTestHelpers.RequiredType(
            "ViceSharp.Host.Diagnostics.HostDiagnosticsState, ViceSharp.Host"));
        DiagnosticsReflectionTestHelpers.Invoke(state, "UpdateEndpoint", new Uri("http://127.0.0.1:51723/"));
        var service = ResolveDiagnosticsService(provider);

        var response = await DiagnosticsReflectionTestHelpers.InvokeAsync(
            service,
            "GetHostInfoAsync",
            TestContext.Current.CancellationToken);
        var hostInfo = DiagnosticsReflectionTestHelpers.RequiredProperty(response, "HostInfo");

        Assert.Equal(Environment.ProcessId, DiagnosticsReflectionTestHelpers.RequiredProperty<int>(hostInfo, "ProcessId"));
        Assert.Equal("http://127.0.0.1:51723/", DiagnosticsReflectionTestHelpers.RequiredProperty<string>(hostInfo, "Endpoint"));
        Assert.Equal(ViceSharpProtocol.Package, DiagnosticsReflectionTestHelpers.RequiredProperty<string>(hostInfo, "ProtocolPackage"));
        Assert.False(string.IsNullOrWhiteSpace(DiagnosticsReflectionTestHelpers.RequiredProperty<string>(hostInfo, "AppVersion")));
        Assert.True(DiagnosticsReflectionTestHelpers.RequiredProperty<DateTimeOffset>(hostInfo, "StartedAtUtc") <= DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// FR-HOST-DIAG-001 / TR-HOST-DIAG-001 / TEST-HOST-DIAG-001.
    /// Use case: diagnostics tools must enumerate the existing sessions rather
    /// than creating temporary sessions just to discover ids.
    /// Acceptance: ListSessions returns registry sessions and does not change
    /// the registry count.
    /// </summary>
    [Fact]
    public async Task ListSessions_ReturnsRegistrySessionsWithoutCreatingProbeSession()
    {
        using var provider = BuildProvider();
        var registry = provider.GetRequiredService<EmulatorRuntimeRegistry>();
        registry.Add(CreateSession("diag-list-a"));
        registry.Add(CreateSession("diag-list-b"));
        var service = ResolveDiagnosticsService(provider);

        var response = await DiagnosticsReflectionTestHelpers.InvokeAsync(
            service,
            "ListSessionsAsync",
            TestContext.Current.CancellationToken);
        var sessions = DiagnosticsReflectionTestHelpers.RequiredProperty(response, "Sessions");
        var ids = Assert.IsAssignableFrom<System.Collections.IEnumerable>(sessions)
            .Cast<object>()
            .Select(session => DiagnosticsReflectionTestHelpers.RequiredProperty<string>(session, "SessionId"))
            .Order()
            .ToArray();

        Assert.Equal(["diag-list-a", "diag-list-b"], ids);
        Assert.Equal(2, registry.Snapshot().Length);
    }

    /// <summary>
    /// FR-HOST-DIAG-001 / TR-HOST-DIAG-002 / TEST-HOST-DIAG-001.
    /// Use case: agents must attach to the same session the UI is rendering.
    /// Acceptance: GetCurrentSession resolves the UI-tracked session id.
    /// </summary>
    [Fact]
    public async Task GetCurrentSession_ReturnsTrackedUiSession()
    {
        using var provider = BuildProvider();
        var registry = provider.GetRequiredService<EmulatorRuntimeRegistry>();
        registry.Add(CreateSession("diag-current"));
        var state = provider.GetRequiredService(DiagnosticsReflectionTestHelpers.RequiredType(
            "ViceSharp.Host.Diagnostics.HostDiagnosticsState, ViceSharp.Host"));
        DiagnosticsReflectionTestHelpers.Invoke(state, "UpdateCurrentSession", "diag-current");
        var service = ResolveDiagnosticsService(provider);

        var response = await DiagnosticsReflectionTestHelpers.InvokeAsync(
            service,
            "GetCurrentSessionAsync",
            TestContext.Current.CancellationToken);
        var session = DiagnosticsReflectionTestHelpers.RequiredProperty(response, "Session");

        Assert.Equal("diag-current", DiagnosticsReflectionTestHelpers.RequiredProperty<string>(session, "SessionId"));
    }

    /// <summary>
    /// FR-HOST-DIAG-001 / TR-HOST-DIAG-001 / TR-HOST-DIAG-002 / TEST-HOST-DIAG-001.
    /// Use case: a diagnostics caller without a session id should get the UI
    /// session snapshot rather than a not-found error.
    /// Acceptance: empty PerformanceSnapshotRequest.SessionId resolves through
    /// current UI session tracking.
    /// </summary>
    [Fact]
    public async Task GetPerformanceSnapshot_EmptySessionUsesCurrentUiSession()
    {
        using var provider = BuildProvider();
        var registry = provider.GetRequiredService<EmulatorRuntimeRegistry>();
        registry.Add(CreateSession("diag-snapshot"));
        var state = provider.GetRequiredService(DiagnosticsReflectionTestHelpers.RequiredType(
            "ViceSharp.Host.Diagnostics.HostDiagnosticsState, ViceSharp.Host"));
        DiagnosticsReflectionTestHelpers.Invoke(state, "UpdateCurrentSession", "diag-snapshot");
        var request = DiagnosticsReflectionTestHelpers.CreateInstance(
            DiagnosticsReflectionTestHelpers.RequiredType("ViceSharp.Protocol.PerformanceSnapshotRequest, ViceSharp.Protocol"),
            string.Empty,
            1000);
        var service = ResolveDiagnosticsService(provider);

        var response = await DiagnosticsReflectionTestHelpers.InvokeAsync(
            service,
            "GetPerformanceSnapshotAsync",
            request,
            TestContext.Current.CancellationToken);
        var snapshot = DiagnosticsReflectionTestHelpers.RequiredProperty(response, "Snapshot");
        var emulatorStatus = DiagnosticsReflectionTestHelpers.RequiredProperty(snapshot, "EmulatorStatus");

        Assert.Equal("diag-snapshot", DiagnosticsReflectionTestHelpers.RequiredProperty<string>(emulatorStatus, "SessionId"));
    }

    /// <summary>
    /// FR-HOST-DIAG-001 / TR-HOST-DIAG-001 / TEST-HOST-DIAG-001.
    /// Use case: explicit session ids should fail loudly when stale.
    /// Acceptance: a missing explicit session returns RpcStatusCode.NotFound.
    /// </summary>
    [Fact]
    public async Task GetPerformanceSnapshot_UnknownExplicitSessionReturnsNotFound()
    {
        using var provider = BuildProvider();
        var request = DiagnosticsReflectionTestHelpers.CreateInstance(
            DiagnosticsReflectionTestHelpers.RequiredType("ViceSharp.Protocol.PerformanceSnapshotRequest, ViceSharp.Protocol"),
            "missing-session",
            1000);
        var service = ResolveDiagnosticsService(provider);

        var response = await DiagnosticsReflectionTestHelpers.InvokeAsync(
            service,
            "GetPerformanceSnapshotAsync",
            request,
            TestContext.Current.CancellationToken);
        var status = DiagnosticsReflectionTestHelpers.RequiredProperty<RpcStatus>(response, "Status");

        Assert.Equal(RpcStatusCode.NotFound, status.Code);
    }

    /// <summary>
    /// FR-HOST-DIAG-001 / TR-HOST-DIAG-001 / TEST-HOST-DIAG-001.
    /// Use case: live diagnostics dashboards need repeated snapshots without
    /// polling setup logic in every client.
    /// Acceptance: WatchPerformance yields at least one snapshot and observes
    /// cancellation.
    /// </summary>
    [Fact]
    public async Task WatchPerformance_StreamsSnapshotsUntilCancellation()
    {
        using var provider = BuildProvider();
        var registry = provider.GetRequiredService<EmulatorRuntimeRegistry>();
        registry.Add(CreateSession("diag-watch"));
        var state = provider.GetRequiredService(DiagnosticsReflectionTestHelpers.RequiredType(
            "ViceSharp.Host.Diagnostics.HostDiagnosticsState, ViceSharp.Host"));
        DiagnosticsReflectionTestHelpers.Invoke(state, "UpdateCurrentSession", "diag-watch");
        var request = DiagnosticsReflectionTestHelpers.CreateInstance(
            DiagnosticsReflectionTestHelpers.RequiredType("ViceSharp.Protocol.WatchPerformanceRequest, ViceSharp.Protocol"),
            string.Empty,
            10);
        var service = ResolveDiagnosticsService(provider);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var stream = DiagnosticsReflectionTestHelpers.Invoke(service, "WatchPerformanceAsync", request, cts.Token);
        var first = await ReadFirstAsync(stream, cts.Token);
        cts.Cancel();

        var snapshot = DiagnosticsReflectionTestHelpers.RequiredProperty(first, "Snapshot");
        Assert.NotNull(snapshot);
    }

    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddViceSharpGrpcHost();
        return services.BuildServiceProvider();
    }

    private static object ResolveDiagnosticsService(IServiceProvider provider)
    {
        return provider.GetRequiredService(DiagnosticsReflectionTestHelpers.RequiredType(
            "ViceSharp.Host.Services.DiagnosticsServiceHost, ViceSharp.Host"));
    }

    private static EmulatorRuntimeSession CreateSession(string sessionId)
    {
        var factory = new DefaultEmulatorRuntimeFactory(
            new ArchitectureBuilder(),
            [MinimalHostArchitectureDescriptor.Instance],
            MinimalHostArchitectureDescriptor.ArchitectureId);
        var created = factory.Create(new CreateEmulatorSessionRequest(MinimalHostArchitectureDescriptor.ArchitectureId));
        return new EmulatorRuntimeSession(sessionId, created.Architecture, created.Machine);
    }

    private static async Task<object> ReadFirstAsync(object asyncEnumerable, CancellationToken cancellationToken)
    {
        var typed = Assert.IsAssignableFrom<IAsyncEnumerable<PerformanceSnapshotResponse>>(asyncEnumerable);
        await using var enumerator = typed.GetAsyncEnumerator(cancellationToken);
        var hasCurrent = await enumerator.MoveNextAsync();
        Assert.True(hasCurrent);
        return enumerator.Current;
    }
}
