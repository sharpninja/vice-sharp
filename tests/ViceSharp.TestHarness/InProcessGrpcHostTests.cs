namespace ViceSharp.TestHarness;

using System.Net;
using Grpc.Core;
using Grpc.Net.Client;
using ViceSharp.Avalonia.Host;
using ViceSharp.Host.Services;
using GrpcContracts = ViceSharp.Protocol.Grpc;
using Xunit;

/// <summary>
/// Lifecycle and boundary tests for <see cref="InProcessGrpcHost"/>, the
/// Avalonia-side wrapper that owns the in-process Kestrel + gRPC server
/// used when the desktop UI hosts the emulator inside its own process.
/// The end-to-end happy-path (CreateSession -> ListMedia -> GetFrame)
/// is exercised by
/// <see cref="AvaloniaBoundaryTests.InProcessGrpcHost_GeneratedClientCreatesSessionAndDirectFrameSourceReturnsFrame"/>
/// and by the GrpcHostProtocolClient integration tests in
/// <c>ProtocolHostIntegrationTests</c>; this class focuses on lifecycle
/// invariants those tests do not cover (start contract, dispose
/// teardown, multi-instance isolation, repeated lifecycles, and
/// cancellation observation during StartAsync).
/// </summary>
public sealed class InProcessGrpcHostTests
{
    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 InProcessGrpcHost).
    /// Use case: The Avalonia shell calls
    /// <see cref="InProcessGrpcHost.StartAsync"/> at app startup and
    /// expects to receive a fully-bound listening endpoint without having
    /// to inspect Kestrel internals.
    /// Acceptance: The returned host exposes a non-null
    /// <see cref="InProcessGrpcHost.Endpoint"/> whose scheme is http,
    /// whose host is the loopback address, and whose port is a positive
    /// dynamic-range value (Kestrel binds to port 0 then publishes the
    /// chosen port).
    /// </summary>
    [Fact]
    public async Task StartAsync_PublishesLoopbackHttpEndpointWithDynamicPort()
    {
        await using var host = await InProcessGrpcHost.StartAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(host.Endpoint);
        Assert.Equal(Uri.UriSchemeHttp, host.Endpoint.Scheme);
        Assert.True(IPAddress.IsLoopback(IPAddress.Parse(host.Endpoint.Host)));
        Assert.InRange(host.Endpoint.Port, 1, 65535);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 InProcessGrpcHost).
    /// Use case: The Avalonia UI resolves
    /// <see cref="ILocalVideoFrameSource"/> directly off the host (the
    /// fast-path that bypasses the gRPC wire for local frame polling).
    /// The host must publish that service through its DI container as
    /// part of StartAsync, not as a deferred resolve.
    /// Acceptance: <see cref="InProcessGrpcHost.VideoFrameSource"/>
    /// returns a non-null, resolvable instance immediately after
    /// StartAsync completes.
    /// </summary>
    [Fact]
    public async Task StartAsync_VideoFrameSourceIsResolvableImmediately()
    {
        await using var host = await InProcessGrpcHost.StartAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(host.VideoFrameSource);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 InProcessGrpcHost).
    /// Use case: A gRPC client connects to the host endpoint and issues
    /// a CreateSession call. The host must accept the connection and
    /// return a populated session id, proving the entire pipeline
    /// (Kestrel listener + HTTP/2 + gRPC routing + service registration)
    /// is wired by StartAsync.
    /// Acceptance: A
    /// <see cref="GrpcContracts.EmulatorHost.EmulatorHostClient"/> built
    /// over a fresh <see cref="GrpcChannel"/> to the host endpoint
    /// returns <see cref="GrpcContracts.RpcStatusCode.Ok"/> and a
    /// non-empty session id.
    /// </summary>
    [Fact]
    public async Task StartAsync_AcceptsGrpcClientConnections()
    {
        await using var host = await InProcessGrpcHost.StartAsync(TestContext.Current.CancellationToken);
        using var channel = GrpcChannel.ForAddress(host.Endpoint);
        var client = new GrpcContracts.EmulatorHost.EmulatorHostClient(channel);

        var response = await client.CreateSessionAsync(
            new GrpcContracts.CreateEmulatorSessionRequest(),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(GrpcContracts.RpcStatusCode.Ok, response.Status.Code);
        Assert.False(string.IsNullOrWhiteSpace(response.SessionId));
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 InProcessGrpcHost).
    /// Use case: Two independent hosts (e.g. one foreground UI process
    /// already running and a second instance spinning up alongside it,
    /// or a test fixture running multiple parallel hosts) must each
    /// receive a distinct dynamic port. The factory must never bind two
    /// hosts to the same address.
    /// Acceptance: Two concurrent <see cref="InProcessGrpcHost"/>
    /// instances expose different <see cref="InProcessGrpcHost.Endpoint"/>
    /// ports.
    /// </summary>
    [Fact]
    public async Task StartAsync_TwoHostsBindToDistinctEndpoints()
    {
        await using var first = await InProcessGrpcHost.StartAsync(TestContext.Current.CancellationToken);
        await using var second = await InProcessGrpcHost.StartAsync(TestContext.Current.CancellationToken);

        Assert.NotEqual(first.Endpoint.Port, second.Endpoint.Port);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 InProcessGrpcHost).
    /// Use case: A UI restart (settings change, profile switch) tears
    /// down the host and immediately starts a fresh one. The factory
    /// must support this pattern: there is no shared static state that
    /// would prevent a clean second startup.
    /// Acceptance: A first host is started and disposed; a second
    /// StartAsync then succeeds and accepts a CreateSession call from a
    /// new gRPC channel.
    /// </summary>
    [Fact]
    public async Task StartAsync_SupportsSequentialStartDisposeCycles()
    {
        Uri firstEndpoint;
        await using (var first = await InProcessGrpcHost.StartAsync(TestContext.Current.CancellationToken))
        {
            firstEndpoint = first.Endpoint;
            Assert.NotNull(firstEndpoint);
        }

        await using var second = await InProcessGrpcHost.StartAsync(TestContext.Current.CancellationToken);
        using var channel = GrpcChannel.ForAddress(second.Endpoint);
        var client = new GrpcContracts.EmulatorHost.EmulatorHostClient(channel);

        var response = await client.CreateSessionAsync(
            new GrpcContracts.CreateEmulatorSessionRequest(),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(GrpcContracts.RpcStatusCode.Ok, response.Status.Code);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 InProcessGrpcHost).
    /// Use case: After the UI disposes the host (app shutdown), any
    /// late gRPC client that still holds the endpoint must fail to
    /// reach the listener. Leaking a stale port to a new client is a
    /// silent-misuse hazard.
    /// Acceptance: A CreateSession call issued via a fresh
    /// <see cref="GrpcChannel"/> to the captured endpoint after
    /// <see cref="InProcessGrpcHost.DisposeAsync"/> throws an
    /// <see cref="RpcException"/> (transport failure) rather than
    /// silently succeeding.
    /// </summary>
    [Fact]
    public async Task DisposeAsync_RefusesNewGrpcConnectionsAfterTeardown()
    {
        Uri capturedEndpoint;
        await using (var host = await InProcessGrpcHost.StartAsync(TestContext.Current.CancellationToken))
        {
            capturedEndpoint = host.Endpoint;
        }

        using var channel = GrpcChannel.ForAddress(capturedEndpoint);
        var client = new GrpcContracts.EmulatorHost.EmulatorHostClient(channel);

        await Assert.ThrowsAsync<RpcException>(async () =>
            await client.CreateSessionAsync(
                new GrpcContracts.CreateEmulatorSessionRequest(),
                cancellationToken: TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 InProcessGrpcHost).
    /// Use case: The Avalonia bootstrap pipeline passes a cancellation
    /// token through to <see cref="InProcessGrpcHost.StartAsync"/>; if
    /// the UI is closed before startup completes (rapid app exit) the
    /// host must observe the cancellation and abort rather than block
    /// or leak a half-started Kestrel.
    /// Acceptance: Calling StartAsync with an already-cancelled token
    /// throws <see cref="OperationCanceledException"/> before the host
    /// is published.
    /// </summary>
    [Fact]
    public async Task StartAsync_PreCancelledTokenAbortsStartup()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await using var host = await InProcessGrpcHost.StartAsync(cts.Token);
        });
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 InProcessGrpcHost).
    /// Use case: A single host endpoint should accept concurrent gRPC
    /// channels (one per UI panel, e.g. the video panel and the
    /// monitor panel each open their own client). The host must not
    /// gate connections behind an exclusive owner.
    /// Acceptance: Two independent <see cref="GrpcChannel"/> instances
    /// each successfully issue a CreateSession call against the same
    /// host endpoint and each receive an Ok status with a distinct
    /// session id.
    /// </summary>
    [Fact]
    public async Task StartAsync_AcceptsMultipleConcurrentClients()
    {
        await using var host = await InProcessGrpcHost.StartAsync(TestContext.Current.CancellationToken);

        using var channelA = GrpcChannel.ForAddress(host.Endpoint);
        using var channelB = GrpcChannel.ForAddress(host.Endpoint);
        var clientA = new GrpcContracts.EmulatorHost.EmulatorHostClient(channelA);
        var clientB = new GrpcContracts.EmulatorHost.EmulatorHostClient(channelB);

        var responseA = await clientA.CreateSessionAsync(
            new GrpcContracts.CreateEmulatorSessionRequest(),
            cancellationToken: TestContext.Current.CancellationToken);
        var responseB = await clientB.CreateSessionAsync(
            new GrpcContracts.CreateEmulatorSessionRequest(),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(GrpcContracts.RpcStatusCode.Ok, responseA.Status.Code);
        Assert.Equal(GrpcContracts.RpcStatusCode.Ok, responseB.Status.Code);
        Assert.NotEqual(responseA.SessionId, responseB.SessionId);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 InProcessGrpcHost).
    /// Use case: The Avalonia UI accesses
    /// <see cref="InProcessGrpcHost.VideoFrameSource"/> repeatedly (the
    /// frame-display loop). The accessor must be a stable singleton
    /// resolved from the host's DI container, not a transient instance
    /// that allocates per call.
    /// Acceptance: Two consecutive reads of the
    /// <see cref="InProcessGrpcHost.VideoFrameSource"/> property return
    /// the same instance reference.
    /// </summary>
    [Fact]
    public async Task VideoFrameSource_ReturnsStableSingletonInstance()
    {
        await using var host = await InProcessGrpcHost.StartAsync(TestContext.Current.CancellationToken);

        var first = host.VideoFrameSource;
        var second = host.VideoFrameSource;

        Assert.Same(first, second);
    }
}
