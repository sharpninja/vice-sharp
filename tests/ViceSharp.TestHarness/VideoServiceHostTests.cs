namespace ViceSharp.TestHarness;

using ViceSharp.Core;
using ViceSharp.Host.Runtime;
using ViceSharp.Host.Services;
using ViceSharp.Protocol;
using Xunit;

/// <summary>
/// Direct (in-process, no gRPC) unit tests for the
/// <see cref="VideoServiceHost"/> RPC surface
/// (<see cref="IVideoService.GetVideoStatusAsync"/> /
/// <see cref="IVideoService.GetFrameAsync"/>) against a minimal
/// in-memory architecture. Complements the chip-layer video coverage
/// (VIC-II / framebuffer tests) by exercising the host-RPC layer that
/// wraps the session video chip: session resolution, status mapping,
/// chip-absence handling, and cancellation contracts. Uses
/// <see cref="MinimalHostArchitectureDescriptor"/> so the tests do not
/// require C64 ROM assets on disk and can run in any worktree (the
/// minimal architecture deliberately registers no video chip so the
/// host's "no chip available" code paths are exercised end-to-end).
/// </summary>
public sealed class VideoServiceHostTests
{
    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 Video RPC).
    /// Use case: A client calls GetVideoStatus with a session id that
    /// the host runtime registry does not know about.
    /// Acceptance: The RPC returns the standard missing-session NotFound
    /// status (carrying the unknown session id) and a null video status
    /// payload.
    /// </summary>
    [Fact]
    public async Task GetVideoStatusAsync_MissingSession_ReturnsMissingSessionStatus()
    {
        var registry = new EmulatorRuntimeRegistry();
        var videoService = new VideoServiceHost(registry);

        var response = await videoService.GetVideoStatusAsync(
            new SessionRequest("does-not-exist"),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.NotFound, response.Status.Code);
        Assert.Contains("does-not-exist", response.Status.Message);
        Assert.Null(response.VideoStatus);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 Video RPC).
    /// Use case: A client calls GetFrame with a session id that the
    /// host runtime registry does not know about.
    /// Acceptance: The RPC returns the standard missing-session NotFound
    /// status and a null frame payload, without touching any session
    /// framebuffer.
    /// </summary>
    [Fact]
    public async Task GetFrameAsync_MissingSession_ReturnsMissingSessionStatus()
    {
        var registry = new EmulatorRuntimeRegistry();
        var videoService = new VideoServiceHost(registry);

        var response = await videoService.GetFrameAsync(
            new SessionRequest("does-not-exist"),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.NotFound, response.Status.Code);
        Assert.Contains("does-not-exist", response.Status.Message);
        Assert.Null(response.Frame);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 Video RPC).
    /// Use case: A client calls GetVideoStatus against a freshly
    /// registered session whose architecture exposes no video chip
    /// (the minimal-host descriptor has no devices); the host must
    /// degrade gracefully rather than NRE.
    /// Acceptance: Status is Ok, the returned <see cref="VideoStatusDto"/>
    /// reports IsAvailable=false with zero width/height, and the cycle
    /// value comes from the live machine state (non-negative).
    /// </summary>
    [Fact]
    public async Task GetVideoStatusAsync_ValidSessionNoVideoChip_ReturnsOkWithUnavailableStatus()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var videoService = new VideoServiceHost(registry);

        var response = await videoService.GetVideoStatusAsync(
            new SessionRequest(session.SessionId),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, response.Status.Code);
        Assert.NotNull(response.VideoStatus);
        Assert.False(response.VideoStatus.IsAvailable);
        Assert.Equal(0, response.VideoStatus.Width);
        Assert.Equal(0, response.VideoStatus.Height);
        Assert.True(response.VideoStatus.Cycle >= 0);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 Video RPC).
    /// Use case: A client calls GetFrame against a freshly registered
    /// session whose architecture exposes no video chip. The host must
    /// refuse gracefully rather than allocate or copy a phantom frame
    /// buffer.
    /// Acceptance: GetFrame returns Unavailable (not Ok with a zero-byte
    /// frame), no frame payload is produced, and the message identifies
    /// the missing video chip.
    /// </summary>
    [Fact]
    public async Task GetFrameAsync_ValidSessionNoVideoChip_ReturnsUnavailable()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var videoService = new VideoServiceHost(registry);

        var response = await videoService.GetFrameAsync(
            new SessionRequest(session.SessionId),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Unavailable, response.Status.Code);
        Assert.Null(response.Frame);
        Assert.Contains("video chip", response.Status.Message, System.StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 Video RPC).
    /// Use case: A client cancels a GetVideoStatus RPC before the host
    /// has a chance to service it; the host must observe the
    /// cancellation rather than walking the device map.
    /// Acceptance: Invoking GetVideoStatusAsync with an already-cancelled
    /// <see cref="CancellationToken"/> throws
    /// <see cref="OperationCanceledException"/> (matching the
    /// <c>ThrowIfCancellationRequested</c> contract).
    /// </summary>
    [Fact]
    public async Task GetVideoStatusAsync_CancelledToken_Throws()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var videoService = new VideoServiceHost(registry);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await videoService.GetVideoStatusAsync(
                new SessionRequest(session.SessionId),
                cts.Token));
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 Video RPC).
    /// Use case: A client cancels a GetFrame RPC before the host has
    /// a chance to service it; the host must observe the cancellation
    /// before any framebuffer copy.
    /// Acceptance: Invoking GetFrameAsync with an already-cancelled
    /// <see cref="CancellationToken"/> throws
    /// <see cref="OperationCanceledException"/> before any session
    /// lookup or framebuffer allocation occurs.
    /// </summary>
    [Fact]
    public async Task GetFrameAsync_CancelledToken_Throws()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var videoService = new VideoServiceHost(registry);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await videoService.GetFrameAsync(
                new SessionRequest(session.SessionId),
                cts.Token));
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 Video RPC).
    /// Use case: A client constructs a <see cref="VideoServiceHost"/>
    /// without supplying a runtime registry (e.g. through a misconfigured
    /// DI container).
    /// Acceptance: The constructor throws
    /// <see cref="ArgumentNullException"/> immediately, surfacing the
    /// misconfiguration at host startup rather than at first RPC.
    /// </summary>
    [Fact]
    public void Constructor_NullRegistry_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new VideoServiceHost(null!));
    }

    private static EmulatorRuntimeSession CreateMinimalSession()
    {
        var factory = new DefaultEmulatorRuntimeFactory(
            new ArchitectureBuilder(),
            [MinimalHostArchitectureDescriptor.Instance],
            MinimalHostArchitectureDescriptor.ArchitectureId);
        return factory.Create(new CreateEmulatorSessionRequest());
    }
}
