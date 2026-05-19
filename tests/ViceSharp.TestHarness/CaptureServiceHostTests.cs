namespace ViceSharp.TestHarness;

using ViceSharp.Core;
using ViceSharp.Host.Runtime;
using ViceSharp.Host.Services;
using ViceSharp.Protocol;
using Xunit;

/// <summary>
/// Direct (in-process, no gRPC) unit tests for the
/// <see cref="CaptureServiceHost"/> RPC surface
/// (<see cref="ICaptureService.StartCaptureAsync"/> /
/// <see cref="ICaptureService.StopCaptureAsync"/> /
/// <see cref="ICaptureService.CaptureFrameAsync"/>) against a minimal
/// in-memory architecture. Complements the chip-layer frame-capture
/// coverage (#94, RUNTIME-CAPTURE-001 / RUNTIME-CAPTURE-002) by
/// exercising the host-RPC layer that wraps it: session resolution,
/// status mapping, argument validation, and cancellation contracts.
/// Uses <see cref="MinimalHostArchitectureDescriptor"/> so the tests
/// do not require C64 ROM assets on disk and can run in any worktree.
/// </summary>
public sealed class CaptureServiceHostTests
{
    /// <summary>
    /// FR/TR: FR-MED (BACKFILL-HOSTUI-001 Capture RPC).
    /// Use case: A client calls StartCapture with a session id that the
    /// host runtime registry does not know about.
    /// Acceptance: The RPC returns the standard missing-session NotFound
    /// status (carrying the unknown session id) and no capture payload.
    /// </summary>
    [Fact]
    public async Task StartCaptureAsync_MissingSession_ReturnsMissingSessionStatus()
    {
        var registry = new EmulatorRuntimeRegistry();
        var captureService = new CaptureServiceHost(registry);

        var response = await captureService.StartCaptureAsync(
            new StartCaptureRequest("does-not-exist", CaptureKind.Screenshot, "out.bmp"),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.NotFound, response.Status.Code);
        Assert.Contains("does-not-exist", response.Status.Message);
        Assert.Null(response.Capture);
    }

    /// <summary>
    /// FR/TR: FR-MED (BACKFILL-HOSTUI-001 Capture RPC).
    /// Use case: A client calls StopCapture with a session id that the
    /// host runtime registry does not know about.
    /// Acceptance: The RPC returns the standard missing-session NotFound
    /// status and no capture payload, without consulting any session's
    /// capture map.
    /// </summary>
    [Fact]
    public async Task StopCaptureAsync_MissingSession_ReturnsMissingSessionStatus()
    {
        var registry = new EmulatorRuntimeRegistry();
        var captureService = new CaptureServiceHost(registry);

        var response = await captureService.StopCaptureAsync(
            new StopCaptureRequest("does-not-exist", "capture-id"),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.NotFound, response.Status.Code);
        Assert.Contains("does-not-exist", response.Status.Message);
        Assert.Null(response.Capture);
    }

    /// <summary>
    /// FR/TR: FR-MED (BACKFILL-HOSTUI-001 Capture RPC).
    /// Use case: A client calls CaptureFrame with a session id that the
    /// host runtime registry does not know about.
    /// Acceptance: The RPC returns the standard missing-session NotFound
    /// status and no artifact, without touching the filesystem.
    /// </summary>
    [Fact]
    public async Task CaptureFrameAsync_MissingSession_ReturnsMissingSessionStatus()
    {
        var registry = new EmulatorRuntimeRegistry();
        var captureService = new CaptureServiceHost(registry);

        var response = await captureService.CaptureFrameAsync(
            new CaptureFrameRequest("does-not-exist", Path.Combine(Path.GetTempPath(), "frame.bmp")),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.NotFound, response.Status.Code);
        Assert.Contains("does-not-exist", response.Status.Message);
        Assert.Null(response.Artifact);
    }

    /// <summary>
    /// FR/TR: FR-MED (BACKFILL-HOSTUI-001 Capture RPC).
    /// Use case: A client calls StartCapture against a freshly
    /// registered session, expecting a logical capture handle it can
    /// later pass to StopCapture.
    /// Acceptance: Status is Ok, the returned <see cref="CaptureSessionDto"/>
    /// echoes the requested <see cref="CaptureKind"/> and target path,
    /// flags the capture as active, and its CaptureId is registered in
    /// the session's capture map so subsequent Stop calls can find it.
    /// </summary>
    [Fact]
    public async Task StartCaptureAsync_ValidSession_ReturnsOkAndRegistersCapture()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var captureService = new CaptureServiceHost(registry);
        var targetPath = Path.Combine(Path.GetTempPath(), $"vice-sharp-{Guid.NewGuid():N}.bmp");

        var response = await captureService.StartCaptureAsync(
            new StartCaptureRequest(session.SessionId, CaptureKind.Screenshot, targetPath),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, response.Status.Code);
        Assert.NotNull(response.Capture);
        Assert.Equal(CaptureKind.Screenshot, response.Capture.Kind);
        Assert.Equal(targetPath, response.Capture.TargetPath);
        Assert.True(response.Capture.IsActive);
        Assert.NotEmpty(response.Capture.CaptureId);
        Assert.True(session.CaptureSessions.ContainsKey(response.Capture.CaptureId));
    }

    /// <summary>
    /// FR/TR: FR-MED (BACKFILL-HOSTUI-001 Capture RPC).
    /// Use case: A client starts a capture, then calls StopCapture with
    /// the returned CaptureId to finalise it.
    /// Acceptance: StopCapture returns Ok, the returned capture DTO
    /// preserves the original kind and target path but is no longer
    /// active, and the session's stored entry is likewise marked
    /// inactive so further Stop calls reflect the terminal state.
    /// </summary>
    [Fact]
    public async Task StopCaptureAsync_ValidSession_StopsActiveCapture()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var captureService = new CaptureServiceHost(registry);
        var targetPath = Path.Combine(Path.GetTempPath(), $"vice-sharp-{Guid.NewGuid():N}.bmp");

        var start = await captureService.StartCaptureAsync(
            new StartCaptureRequest(session.SessionId, CaptureKind.Video, targetPath),
            TestContext.Current.CancellationToken);
        Assert.Equal(RpcStatusCode.Ok, start.Status.Code);
        Assert.NotNull(start.Capture);

        var stop = await captureService.StopCaptureAsync(
            new StopCaptureRequest(session.SessionId, start.Capture.CaptureId),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, stop.Status.Code);
        Assert.NotNull(stop.Capture);
        Assert.Equal(start.Capture.CaptureId, stop.Capture.CaptureId);
        Assert.Equal(CaptureKind.Video, stop.Capture.Kind);
        Assert.Equal(targetPath, stop.Capture.TargetPath);
        Assert.False(stop.Capture.IsActive);
        Assert.False(session.CaptureSessions[start.Capture.CaptureId].IsActive);
    }

    /// <summary>
    /// FR/TR: FR-MED (BACKFILL-HOSTUI-001 Capture RPC).
    /// Use case: A client calls StopCapture with a session it owns but a
    /// CaptureId that was never started (or has already been removed).
    /// Acceptance: StopCapture returns NotFound, the message identifies
    /// the offending CaptureId, and no capture payload is returned.
    /// </summary>
    [Fact]
    public async Task StopCaptureAsync_UnknownCaptureId_ReturnsNotFound()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var captureService = new CaptureServiceHost(registry);

        var response = await captureService.StopCaptureAsync(
            new StopCaptureRequest(session.SessionId, "capture-does-not-exist"),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.NotFound, response.Status.Code);
        Assert.Contains("capture-does-not-exist", response.Status.Message);
        Assert.Null(response.Capture);
    }

    /// <summary>
    /// FR/TR: FR-MED (BACKFILL-HOSTUI-001 Capture RPC).
    /// Use case: A client calls StartCapture for a valid session but
    /// forgets to supply a target path (or supplies whitespace).
    /// Acceptance: StartCapture returns InvalidArgument, no capture is
    /// registered in the session's capture map, and no capture payload
    /// is returned.
    /// </summary>
    [Fact]
    public async Task StartCaptureAsync_EmptyTargetPath_ReturnsInvalidArgument()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var captureService = new CaptureServiceHost(registry);

        var response = await captureService.StartCaptureAsync(
            new StartCaptureRequest(session.SessionId, CaptureKind.Screenshot, "   "),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.InvalidArgument, response.Status.Code);
        Assert.Null(response.Capture);
        Assert.Empty(session.CaptureSessions);
    }

    /// <summary>
    /// FR/TR: FR-MED (BACKFILL-HOSTUI-001 Capture RPC).
    /// Use case: A client calls CaptureFrame for a valid session but
    /// supplies an empty/whitespace file path. The host must reject the
    /// request before doing any frame-buffer work or filesystem I/O.
    /// Acceptance: CaptureFrame returns InvalidArgument and no artifact.
    /// </summary>
    [Fact]
    public async Task CaptureFrameAsync_EmptyFilePath_ReturnsInvalidArgument()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var captureService = new CaptureServiceHost(registry);

        var response = await captureService.CaptureFrameAsync(
            new CaptureFrameRequest(session.SessionId, "   "),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.InvalidArgument, response.Status.Code);
        Assert.Null(response.Artifact);
    }

    /// <summary>
    /// FR/TR: FR-MED (BACKFILL-HOSTUI-001 Capture RPC).
    /// Use case: A client calls CaptureFrame against a session whose
    /// architecture exposes no video chip (the minimal host descriptor
    /// has no devices). The host must refuse gracefully rather than NRE
    /// or write a zero-byte file.
    /// Acceptance: CaptureFrame returns Unavailable, no artifact is
    /// produced, and the target path on disk does not exist after the
    /// call (no filesystem side-effect occurred).
    /// </summary>
    [Fact]
    public async Task CaptureFrameAsync_NoVideoChip_ReturnsUnavailable()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var captureService = new CaptureServiceHost(registry);
        var targetPath = Path.Combine(Path.GetTempPath(), $"vice-sharp-{Guid.NewGuid():N}.bmp");

        var response = await captureService.CaptureFrameAsync(
            new CaptureFrameRequest(session.SessionId, targetPath),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Unavailable, response.Status.Code);
        Assert.Null(response.Artifact);
        Assert.False(File.Exists(targetPath));
    }

    /// <summary>
    /// FR/TR: FR-MED (BACKFILL-HOSTUI-001 Capture RPC).
    /// Use case: A client cancels a StartCapture RPC before the host
    /// has a chance to service it; the host must observe the
    /// cancellation rather than registering a half-initialised capture.
    /// Acceptance: Invoking StartCaptureAsync with an already-cancelled
    /// <see cref="CancellationToken"/> throws
    /// <see cref="OperationCanceledException"/> (matching the
    /// <c>ThrowIfCancellationRequested</c> contract).
    /// </summary>
    [Fact]
    public async Task StartCaptureAsync_CancelledToken_Throws()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var captureService = new CaptureServiceHost(registry);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await captureService.StartCaptureAsync(
                new StartCaptureRequest(session.SessionId, CaptureKind.Screenshot, "out.bmp"),
                cts.Token));
        Assert.Empty(session.CaptureSessions);
    }

    /// <summary>
    /// FR/TR: FR-MED (BACKFILL-HOSTUI-001 Capture RPC).
    /// Use case: A client cancels a StopCapture RPC before the host
    /// has a chance to service it.
    /// Acceptance: Invoking StopCaptureAsync with an already-cancelled
    /// <see cref="CancellationToken"/> throws
    /// <see cref="OperationCanceledException"/> before any session
    /// lookup or capture-map mutation occurs.
    /// </summary>
    [Fact]
    public async Task StopCaptureAsync_CancelledToken_Throws()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var captureService = new CaptureServiceHost(registry);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await captureService.StopCaptureAsync(
                new StopCaptureRequest(session.SessionId, "any-capture-id"),
                cts.Token));
    }

    /// <summary>
    /// FR/TR: FR-MED (BACKFILL-HOSTUI-001 Capture RPC).
    /// Use case: A client cancels a CaptureFrame RPC before the host
    /// has a chance to service it; the host must observe the
    /// cancellation rather than starting a frame-buffer copy or BMP
    /// write.
    /// Acceptance: Invoking CaptureFrameAsync with an already-cancelled
    /// <see cref="CancellationToken"/> throws
    /// <see cref="OperationCanceledException"/>, and no file is created
    /// at the target path.
    /// </summary>
    [Fact]
    public async Task CaptureFrameAsync_CancelledToken_Throws()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var captureService = new CaptureServiceHost(registry);
        var targetPath = Path.Combine(Path.GetTempPath(), $"vice-sharp-{Guid.NewGuid():N}.bmp");

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await captureService.CaptureFrameAsync(
                new CaptureFrameRequest(session.SessionId, targetPath),
                cts.Token));
        Assert.False(File.Exists(targetPath));
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
