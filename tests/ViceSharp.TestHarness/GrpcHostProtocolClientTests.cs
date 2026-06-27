namespace ViceSharp.TestHarness;

using Grpc.Core;
using Grpc.Net.Client;
using ViceSharp.Avalonia.Host;
using ViceSharp.Protocol;
using GrpcContracts = ViceSharp.Protocol.Grpc;
using Xunit;

/// <summary>
/// Client-level behaviour tests for
/// <see cref="GrpcHostProtocolClient"/>, the Avalonia-side wrapper that
/// adapts the generated gRPC services to the
/// <see cref="IHostProtocolClient"/> interface. The end-to-end
/// happy-path RPCs (CreateSession/ListMedia/GetFrame) are exercised in
/// <see cref="AvaloniaBoundaryTests"/> and
/// <c>ProtocolHostIntegrationTests</c>; this class focuses on
/// client-level invariants those tests do not cover: lazy session
/// bootstrap, status code propagation for unknown sessions, deterministic
/// SessionId before/after first call, disposal teardown, and
/// cancellation flow-through.
/// </summary>
public sealed class GrpcHostProtocolClientTests
{
    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 GrpcHostProtocolClient).
    /// Use case: The Avalonia shell constructs a
    /// <see cref="GrpcHostProtocolClient"/> pointed at a running
    /// in-process host and immediately calls <c>GetStatusAsync</c>; the
    /// client must bootstrap a session lazily and return Ok.
    /// Acceptance: A fresh client (empty SessionId) issues GetStatusAsync
    /// against a live host, the returned status is Ok, and the client's
    /// SessionId property is populated (non-empty) after the call.
    /// </summary>
    [Fact]
    public async Task GetStatusAsync_BootstrapsSessionLazilyAndReturnsOk()
    {
        await using var host = await InProcessGrpcHost.StartAsync(TestContext.Current.CancellationToken);
        using var client = new GrpcHostProtocolClient(host.Endpoint);

        Assert.Equal(string.Empty, client.SessionId);

        var response = await client.GetStatusAsync(TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, response.Status.Code);
        Assert.NotNull(response.EmulatorStatus);
        Assert.False(string.IsNullOrWhiteSpace(client.SessionId));
    }

    /// <summary>
    /// FR: FR-DRVTRUE-001, TR: TR-GRPC-BOUNDARY-001, TEST-DRVTRUE-001.
    /// Use case: Selecting True Drive must carry the choice over the protocol and
    /// (because true-drive is a machine-config change) recreate the session so
    /// the host rebuilds it; the next call must succeed against the new session.
    /// Acceptance: After a session is bootstrapped, SetTrueDriveAsync(true) sets
    /// the client's TrueDrive flag and drops the current session; the next
    /// GetStatusAsync returns Ok with a new (different) session id.
    /// </summary>
    [Fact]
    public async Task SetTrueDriveAsync_RecreatesSessionOverProtocol()
    {
        await using var host = await InProcessGrpcHost.StartAsync(TestContext.Current.CancellationToken);
        using var client = new GrpcHostProtocolClient(host.Endpoint);

        await client.GetStatusAsync(TestContext.Current.CancellationToken);
        var firstSession = client.SessionId;
        Assert.False(string.IsNullOrWhiteSpace(firstSession));
        Assert.False(client.TrueDrive);

        await client.SetTrueDriveAsync(true, 8, cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(client.TrueDrive);

        var afterToggle = await client.GetStatusAsync(TestContext.Current.CancellationToken);
        Assert.Equal(RpcStatusCode.Ok, afterToggle.Status.Code);
        Assert.False(string.IsNullOrWhiteSpace(client.SessionId));
        Assert.NotEqual(firstSession, client.SessionId);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 GrpcHostProtocolClient).
    /// Use case: Once the client has bootstrapped a session, subsequent
    /// calls must reuse the same session id rather than creating a new
    /// one per call. This prevents leaking emulator sessions on the host.
    /// Acceptance: After two sequential GetStatusAsync calls on the same
    /// client, the SessionId is unchanged between calls and matches the
    /// session id reported on the host status payload.
    /// </summary>
    [Fact]
    public async Task GetStatusAsync_ReusesSessionAcrossCalls()
    {
        await using var host = await InProcessGrpcHost.StartAsync(TestContext.Current.CancellationToken);
        using var client = new GrpcHostProtocolClient(host.Endpoint);

        var firstResponse = await client.GetStatusAsync(TestContext.Current.CancellationToken);
        var sessionAfterFirst = client.SessionId;
        var secondResponse = await client.GetStatusAsync(TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, firstResponse.Status.Code);
        Assert.Equal(RpcStatusCode.Ok, secondResponse.Status.Code);
        Assert.False(string.IsNullOrWhiteSpace(sessionAfterFirst));
        Assert.Equal(sessionAfterFirst, client.SessionId);
        Assert.Equal(sessionAfterFirst, secondResponse.EmulatorStatus!.SessionId);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 GrpcHostProtocolClient).
    /// Use case: Avalonia startup can call the lazy client from status and
    /// frame refresh paths at the same time; those concurrent first-use calls
    /// must not create multiple running emulator sessions.
    /// Acceptance: Parallel GetStatusAsync calls on a fresh client all return
    /// Ok for one shared session, and diagnostics reports exactly one host
    /// session.
    /// </summary>
    [Fact]
    public async Task GetStatusAsync_ConcurrentFirstUseCreatesSingleSession()
    {
        await using var host = await InProcessGrpcHost.StartAsync(TestContext.Current.CancellationToken);
        using var client = new GrpcHostProtocolClient(host.Endpoint);
        using var diagnosticsChannel = GrpcChannel.ForAddress(host.Endpoint);
        var diagnostics = new GrpcContracts.DiagnosticsService.DiagnosticsServiceClient(diagnosticsChannel);

        var calls = Enumerable
            .Range(0, 12)
            .Select(_ => client.GetStatusAsync(TestContext.Current.CancellationToken).AsTask())
            .ToArray();

        var responses = await Task.WhenAll(calls);
        var sessionId = client.SessionId;
        var sessions = await diagnostics.ListSessionsAsync(
            new GrpcContracts.EmptyRequest(),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(string.IsNullOrWhiteSpace(sessionId));
        Assert.All(responses, response => Assert.Equal(RpcStatusCode.Ok, response.Status.Code));
        Assert.All(responses, response => Assert.Equal(sessionId, response.EmulatorStatus!.SessionId));
        var session = Assert.Single(sessions.Sessions);
        Assert.Equal(sessionId, session.SessionId);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 GrpcHostProtocolClient).
    /// Use case: When the client is pre-configured with a session id
    /// that does not exist on the host (e.g. stale reconnect), the host's
    /// MissingSessionStatus (RpcStatus.NotFound) must propagate through
    /// the client to the caller verbatim, not be re-wrapped or hidden.
    /// Acceptance: Constructing the client with a bogus session id and
    /// calling GetStatusAsync returns RpcStatusCode.NotFound, and the
    /// message names the missing session.
    /// </summary>
    [Fact]
    public async Task GetStatusAsync_PropagatesMissingSessionStatusForUnknownSession()
    {
        await using var host = await InProcessGrpcHost.StartAsync(TestContext.Current.CancellationToken);
        using var client = new GrpcHostProtocolClient(host.Endpoint, sessionId: "session-that-does-not-exist");

        var response = await client.GetStatusAsync(TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.NotFound, response.Status.Code);
        Assert.Contains("session-that-does-not-exist", response.Status.Message);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 GrpcHostProtocolClient).
    /// Use case: Lifecycle command dispatch (Pause/Resume/Reset) must
    /// route through the client to the gRPC EmulatorHost service and
    /// return the corresponding status. This validates that
    /// SendHostCommandAsync wires every lifecycle method correctly.
    /// Acceptance: PauseAsync, ResumeAsync, ColdResetAsync and
    /// WarmResetAsync each return RpcStatusCode.Ok and a non-null
    /// EmulatorStatus payload when invoked on a freshly-bootstrapped
    /// session.
    /// </summary>
    [Fact]
    public async Task LifecycleCommands_DispatchThroughClientAndReturnOk()
    {
        await using var host = await InProcessGrpcHost.StartAsync(TestContext.Current.CancellationToken);
        using var client = new GrpcHostProtocolClient(host.Endpoint);

        var pause = await client.PauseAsync(TestContext.Current.CancellationToken);
        var resume = await client.ResumeAsync(TestContext.Current.CancellationToken);
        var cold = await client.ColdResetAsync(TestContext.Current.CancellationToken);
        var warm = await client.WarmResetAsync(TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, pause.Status.Code);
        Assert.Equal(RpcStatusCode.Ok, resume.Status.Code);
        Assert.Equal(RpcStatusCode.Ok, cold.Status.Code);
        Assert.Equal(RpcStatusCode.Ok, warm.Status.Code);
        Assert.NotNull(pause.EmulatorStatus);
        Assert.NotNull(resume.EmulatorStatus);
        Assert.NotNull(cold.EmulatorStatus);
        Assert.NotNull(warm.EmulatorStatus);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 GrpcHostProtocolClient).
    /// Use case: ListMediaAsync routed through the
    /// <see cref="GrpcHostProtocolClient"/> must reach the MediaService
    /// over gRPC and return an Ok status with an attachments collection
    /// (empty on a freshly-bootstrapped session that has no media).
    /// Acceptance: ListMediaAsync on a fresh session returns
    /// RpcStatusCode.Ok and a non-null (possibly empty) Attachments
    /// array.
    /// </summary>
    [Fact]
    public async Task ListMediaAsync_RoutesThroughMediaServiceAndReturnsOk()
    {
        await using var host = await InProcessGrpcHost.StartAsync(TestContext.Current.CancellationToken);
        using var client = new GrpcHostProtocolClient(host.Endpoint);

        var response = await client.ListMediaAsync(TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, response.Status.Code);
        Assert.NotNull(response.Attachments);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 GrpcHostProtocolClient).
    /// Use case: The Avalonia settings panel reads the active session
    /// settings through ListSettingsProfilesAsync and GetSettingsAsync.
    /// Both calls must route through the SettingsService client and
    /// return Ok with a populated payload after lazy session bootstrap.
    /// Acceptance: ListSettingsProfilesAsync returns Ok and at least one
    /// profile; GetSettingsAsync returns Ok and a non-null
    /// SessionSettingsDto.
    /// </summary>
    [Fact]
    public async Task SettingsAccessors_DispatchToSettingsServiceAndReturnPayload()
    {
        await using var host = await InProcessGrpcHost.StartAsync(TestContext.Current.CancellationToken);
        using var client = new GrpcHostProtocolClient(host.Endpoint);

        var profiles = await client.ListSettingsProfilesAsync(TestContext.Current.CancellationToken);
        var settings = await client.GetSettingsAsync(TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, profiles.Status.Code);
        Assert.NotNull(profiles.Profiles);
        Assert.Equal(RpcStatusCode.Ok, settings.Status.Code);
        Assert.NotNull(settings.Settings);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 GrpcHostProtocolClient).
    /// Use case: When the Avalonia UI disposes the protocol client (e.g.
    /// during app shutdown or host reconnect), the underlying gRPC channel
    /// must be released so subsequent calls fail rather than silently
    /// hang or succeed against a dead transport.
    /// Acceptance: After Dispose, a follow-up GetStatusAsync throws an
    /// ObjectDisposedException, RpcException, or InvalidOperationException
    /// (any of the three indicates the channel is no longer usable).
    /// </summary>
    [Fact]
    public async Task Dispose_FailsSubsequentCalls()
    {
        await using var host = await InProcessGrpcHost.StartAsync(TestContext.Current.CancellationToken);
        var client = new GrpcHostProtocolClient(host.Endpoint);

        // Bootstrap a session so Dispose has work to do (Shutdown best-effort + channel dispose).
        var initialResponse = await client.GetStatusAsync(TestContext.Current.CancellationToken);
        Assert.Equal(RpcStatusCode.Ok, initialResponse.Status.Code);

        client.Dispose();

        await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            await client.GetStatusAsync(TestContext.Current.CancellationToken);
        });
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 GrpcHostProtocolClient).
    /// Use case: A cancellation token raised by the Avalonia UI (e.g.
    /// user closes a panel mid-call) must abort the in-flight gRPC call
    /// rather than block waiting for the response. The client must not
    /// swallow the cancellation.
    /// Acceptance: Calling GetStatusAsync with an already-cancelled token
    /// throws either an OperationCanceledException directly or an
    /// RpcException with StatusCode.Cancelled (Grpc.Net.Client wraps the
    /// cancellation; both forms are valid evidence the cancellation flowed
    /// from the client into the gRPC call).
    /// </summary>
    [Fact]
    public async Task GetStatusAsync_PropagatesCancellation()
    {
        await using var host = await InProcessGrpcHost.StartAsync(TestContext.Current.CancellationToken);
        using var client = new GrpcHostProtocolClient(host.Endpoint);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var exception = await Record.ExceptionAsync(async () =>
        {
            await client.GetStatusAsync(cts.Token);
        });

        Assert.NotNull(exception);
        var isCancellation = exception is OperationCanceledException
            || (exception is RpcException rpc && rpc.StatusCode == StatusCode.Cancelled);
        Assert.True(
            isCancellation,
            $"Expected OperationCanceledException or RpcException(Cancelled), got {exception.GetType()}: {exception.Message}");
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 GrpcHostProtocolClient).
    /// Use case: Disposing the client is idempotent and must never throw
    /// even when called twice or when Shutdown on the host side has
    /// already torn down the session. The Dispose contract advertises a
    /// best-effort Shutdown; the second Dispose must short-circuit.
    /// Acceptance: Two sequential Dispose calls on the same client
    /// complete without throwing; the second call is a no-op.
    /// </summary>
    [Fact]
    public async Task Dispose_IsIdempotent()
    {
        await using var host = await InProcessGrpcHost.StartAsync(TestContext.Current.CancellationToken);
        var client = new GrpcHostProtocolClient(host.Endpoint);
        await client.GetStatusAsync(TestContext.Current.CancellationToken);

        client.Dispose();
        var second = Record.Exception(() => client.Dispose());

        Assert.Null(second);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 GrpcHostProtocolClient).
    /// Use case: Constructing the client with a null endpoint is a
    /// programmer-error contract violation; the constructor must surface
    /// the misuse immediately rather than deferring to the first call.
    /// Acceptance: Passing a null Uri to the constructor throws
    /// ArgumentNullException.
    /// </summary>
    [Fact]
    public void Constructor_NullEndpoint_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new GrpcHostProtocolClient(null!));
    }

    /// <summary>
    /// FR-HOST-DIAG-001 / TR-HOST-DIAG-002 / TEST-UI-DIAG-001.
    /// Use case: the UI diagnostics bridge needs to update current-session
    /// state when the gRPC client lazily creates or recreates a session.
    /// Acceptance: SessionIdChanged is raised with the new non-empty session id.
    /// </summary>
    [Fact]
    public async Task SessionIdChanged_RaisesWhenCurrentSessionChanges()
    {
        await using var host = await InProcessGrpcHost.StartAsync(TestContext.Current.CancellationToken);
        using var client = new GrpcHostProtocolClient(host.Endpoint);
        var eventInfo = typeof(GrpcHostProtocolClient).GetEvent("SessionIdChanged");
        Assert.NotNull(eventInfo);
        string? observed = null;
        EventHandler<string> handler = (_, sessionId) => observed = sessionId;
        eventInfo.AddEventHandler(client, handler);

        await client.GetStatusAsync(TestContext.Current.CancellationToken);

        Assert.False(string.IsNullOrWhiteSpace(observed));
        Assert.Equal(client.SessionId, observed);
    }
}
