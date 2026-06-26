namespace ViceSharp.TestHarness;

using System.Buffers.Binary;
using ViceSharp.Core;
using ViceSharp.Core.Media;
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

    /// <summary>
    /// FR-MED-001 / TR-MEDIA-001 (x64sc screenshot parity).
    /// Use case: a client requests a screenshot in a format the host does not support
    ///   (x64sc offers BMP/PNG; we reject everything else, e.g. "gif").
    /// Acceptance: CaptureFrame returns InvalidArgument, produces no artifact, and writes
    ///   no file - the format is validated before any frame-buffer or filesystem work.
    /// </summary>
    [Fact]
    public async Task CaptureFrameAsync_UnsupportedFormat_ReturnsInvalidArgument()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var captureService = new CaptureServiceHost(registry);
        var targetPath = Path.Combine(Path.GetTempPath(), $"vice-sharp-{Guid.NewGuid():N}.gif");

        var response = await captureService.CaptureFrameAsync(
            new CaptureFrameRequest(session.SessionId, targetPath, "gif"),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.InvalidArgument, response.Status.Code);
        Assert.Null(response.Artifact);
        Assert.False(File.Exists(targetPath));
    }

    /// <summary>
    /// FR-MED-001 / TR-MEDIA-001 (x64sc media-capture parity - capability discovery).
    /// Use case: a (remote) client asks the gRPC control surface which capture formats the host
    ///   supports before offering them in a recording dialog.
    /// Acceptance: GetCaptureCapabilities returns Ok and advertises the implemented encoders -
    ///   png + bmp screenshots and wav sound.
    /// </summary>
    [Fact]
    public async Task GetCaptureCapabilitiesAsync_ValidSession_ReportsSupportedFormats()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var captureService = new CaptureServiceHost(registry);

        var response = await captureService.GetCaptureCapabilitiesAsync(
            new SessionRequest(session.SessionId), TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, response.Status.Code);
        Assert.Contains("png", response.ScreenshotFormats);
        Assert.Contains("bmp", response.ScreenshotFormats);
        Assert.Contains("wav", response.AudioFormats);
    }

    /// <summary>
    /// FR-MED-001 / TR-MEDIA-001 (x64sc media-capture parity - capability discovery).
    /// Use case: capability discovery for an unknown session.
    /// Acceptance: GetCaptureCapabilities returns the standard missing-session NotFound status.
    /// </summary>
    [Fact]
    public async Task GetCaptureCapabilitiesAsync_MissingSession_ReturnsMissingSessionStatus()
    {
        var registry = new EmulatorRuntimeRegistry();
        var captureService = new CaptureServiceHost(registry);

        var response = await captureService.GetCaptureCapabilitiesAsync(
            new SessionRequest("does-not-exist"), TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.NotFound, response.Status.Code);
        Assert.Contains("does-not-exist", response.Status.Message);
    }

    /// <summary>
    /// FR-MED-001 / TR-MEDIA-001 (x64sc media-capture parity - capture listing).
    /// Use case: a client enumerates the active captures for a session via the gRPC surface.
    /// Acceptance: after StartCapture, ListCaptures returns Ok and includes the started,
    ///   still-active capture by id.
    /// </summary>
    [Fact]
    public async Task ListCapturesAsync_AfterStart_IncludesActiveCapture()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        session.AudioCaptureTap = new CaptureAudioTap(downstream: null); // give the session a live audio path
        registry.Add(session);
        var captureService = new CaptureServiceHost(registry);
        var path = Path.Combine(Path.GetTempPath(), $"vice-sharp-{Guid.NewGuid():N}.wav");

        var start = await captureService.StartCaptureAsync(
            new StartCaptureRequest(session.SessionId, CaptureKind.Audio, path, "wav"),
            TestContext.Current.CancellationToken);
        Assert.Equal(RpcStatusCode.Ok, start.Status.Code);

        var list = await captureService.ListCapturesAsync(
            new SessionRequest(session.SessionId), TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, list.Status.Code);
        Assert.Contains(list.Captures, c => c.CaptureId == start.Capture!.CaptureId && c.IsActive);

        // Release the open recorder/file handle the started capture holds.
        await captureService.StopCaptureAsync(
            new StopCaptureRequest(session.SessionId, start.Capture!.CaptureId),
            TestContext.Current.CancellationToken);
        File.Delete(path);
    }

    /// <summary>
    /// FR-MED-002 / TR-MEDIA-VIDEO-001 (continuous video capture, BMP sequence).
    /// Use case: a client starts a video capture, the emulation worker tees each
    /// committed frame to the capture, and the client stops it.
    /// Acceptance: StartCapture(Video) returns Ok and arms the session; each
    /// frame teed while active is written as the next numbered BMP; StopCapture
    /// finalises the capture (inactive, no longer armed) and the output directory
    /// holds exactly one BMP per teed frame.
    /// </summary>
    [Fact]
    public async Task StartCapture_Video_TeesFramesToNumberedBmpsUntilStop()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var captureService = new CaptureServiceHost(registry);
        var dir = Path.Combine(Path.GetTempPath(), $"vice-sharp-vid-{Guid.NewGuid():N}");

        var start = await captureService.StartCaptureAsync(
            new StartCaptureRequest(session.SessionId, CaptureKind.Video, dir, "bmpseq"),
            TestContext.Current.CancellationToken);
        Assert.Equal(RpcStatusCode.Ok, start.Status.Code);
        Assert.True(session.IsVideoCaptureActive);

        var frame = new byte[2 * 2 * 4]; // 2x2 BGRA
        for (var i = 0; i < 3; i++)
            session.RecordVideoFrameIfActive(frame, 2, 2);

        var stop = await captureService.StopCaptureAsync(
            new StopCaptureRequest(session.SessionId, start.Capture!.CaptureId),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, stop.Status.Code);
        Assert.False(stop.Capture!.IsActive);
        Assert.False(session.IsVideoCaptureActive);

        try
        {
            var files = Directory.GetFiles(dir, "frame_*.bmp");
            Assert.Equal(3, files.Length);
            Assert.True(File.Exists(Path.Combine(dir, "frame_000001.bmp")));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    /// <summary>
    /// FR-MED-002 / FR-MED-004 (video capture format validation).
    /// Use case: a client requests a video format the host does not support at all
    /// (not bmpseq, and not one of the ffmpeg containers mp4/mkv/avi) - e.g. "wmv".
    /// Acceptance: StartCapture(Video) returns InvalidArgument, registers no
    /// capture, and does not arm the session.
    /// </summary>
    [Fact]
    public async Task StartCapture_Video_UnsupportedFormat_ReturnsInvalidArgument()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var captureService = new CaptureServiceHost(registry);
        var dir = Path.Combine(Path.GetTempPath(), $"vice-sharp-{Guid.NewGuid():N}");

        var response = await captureService.StartCaptureAsync(
            new StartCaptureRequest(session.SessionId, CaptureKind.Video, dir, "wmv"),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.InvalidArgument, response.Status.Code);
        Assert.Null(response.Capture);
        Assert.Empty(session.CaptureSessions);
        Assert.False(session.IsVideoCaptureActive);
    }

    /// <summary>
    /// FR-MED-003 / TR-MEDIA-SOUND-001 (WAV sound recording).
    /// Use case: a session with a live audio path starts a WAV recording, the SID
    /// submits samples through the tap, and the client stops the recording.
    /// Acceptance: StartCapture(Audio, wav) returns Ok and arms the session;
    /// samples submitted through the live tap are written; StopCapture finalises
    /// a valid RIFF/WAVE file whose data chunk size matches the submitted samples.
    /// </summary>
    [Fact]
    public async Task StartCapture_Audio_RecordsWavUntilStop()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        session.AudioCaptureTap = new CaptureAudioTap(downstream: null);
        registry.Add(session);
        var captureService = new CaptureServiceHost(registry);
        var path = Path.Combine(Path.GetTempPath(), $"vice-sharp-aud-{Guid.NewGuid():N}.wav");

        var start = await captureService.StartCaptureAsync(
            new StartCaptureRequest(session.SessionId, CaptureKind.Audio, path, "wav"),
            TestContext.Current.CancellationToken);
        Assert.Equal(RpcStatusCode.Ok, start.Status.Code);
        Assert.True(session.IsAudioCaptureActive);

        var samples = new float[100];
        for (var i = 0; i < samples.Length; i++)
            samples[i] = 0.5f;
        session.AudioCaptureTap.SubmitSamples(samples);

        var stop = await captureService.StopCaptureAsync(
            new StopCaptureRequest(session.SessionId, start.Capture!.CaptureId),
            TestContext.Current.CancellationToken);
        Assert.Equal(RpcStatusCode.Ok, stop.Status.Code);
        Assert.False(stop.Capture!.IsActive);
        Assert.False(session.IsAudioCaptureActive);

        try
        {
            var bytes = await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken);
            Assert.Equal((byte)'R', bytes[0]);
            Assert.Equal((byte)'I', bytes[1]);
            Assert.Equal((byte)'F', bytes[2]);
            Assert.Equal((byte)'F', bytes[3]);
            var dataBytes = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(40, 4));
            Assert.Equal(100 * 2, dataBytes);
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// FR-MED-003 (sound recording requires a live audio path).
    /// Use case: a client requests WAV recording on a session built without a
    /// live audio backend (headless / test rig - no tap installed).
    /// Acceptance: StartCapture(Audio) returns Unavailable, registers no capture,
    /// and writes no file.
    /// </summary>
    [Fact]
    public async Task StartCapture_Audio_NoLiveAudioPath_ReturnsUnavailable()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession(); // no AudioCaptureTap
        registry.Add(session);
        var captureService = new CaptureServiceHost(registry);
        var path = Path.Combine(Path.GetTempPath(), $"vice-sharp-{Guid.NewGuid():N}.wav");

        var response = await captureService.StartCaptureAsync(
            new StartCaptureRequest(session.SessionId, CaptureKind.Audio, path, "wav"),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Unavailable, response.Status.Code);
        Assert.Null(response.Capture);
        Assert.False(File.Exists(path));
        Assert.Empty(session.CaptureSessions);
    }

    /// <summary>
    /// FR-MED-002 / TR-MEDIA-VIDEO-001 (capability discovery - video).
    /// Use case: a client inspects which video formats the host can drive before
    /// offering them in a recording dialog.
    /// Acceptance: GetCaptureCapabilities advertises the numbered-BMP sequence
    /// format ("bmpseq"), which needs no external tooling.
    /// </summary>
    [Fact]
    public async Task GetCaptureCapabilitiesAsync_ReportsBmpSequenceVideoFormat()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var captureService = new CaptureServiceHost(registry);

        var response = await captureService.GetCaptureCapabilitiesAsync(
            new SessionRequest(session.SessionId), TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, response.Status.Code);
        Assert.Contains(response.VideoFormats, f => f.Id == "bmpseq" && !f.RequiresFfmpeg);
    }

    /// <summary>
    /// FR-MED-004 / TR-MEDIA-VIDEO-FFMPEG-001.
    /// Use case: microphone narration is a muxed ffmpeg feature, not a frame-sequence feature.
    /// Acceptance: StartCapture(Video, bmpseq, captureMicrophone: true) is rejected early.
    /// </summary>
    [Fact]
    public async Task StartCapture_BmpSequence_WithMicrophone_ReturnsInvalidArgument()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var captureService = new CaptureServiceHost(registry);
        var dir = Path.Combine(Path.GetTempPath(), $"vice-sharp-mic-bmp-{Guid.NewGuid():N}");

        var response = await captureService.StartCaptureAsync(
            new StartCaptureRequest(
                session.SessionId,
                CaptureKind.Video,
                dir,
                "bmpseq",
                CaptureMicrophone: true),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.InvalidArgument, response.Status.Code);
        Assert.Contains("Microphone", response.Status.Message);
        Assert.Null(response.Capture);
        Assert.False(Directory.Exists(dir));
    }

    /// <summary>
    /// FR-MED-002 (unique-frames BMP export via capture options).
    /// Use case: a client records a BMP sequence with options { frames: unique }
    /// so runs of identical frames collapse to single BMPs.
    /// Acceptance: feeding A, A, B, B teed frames writes exactly two BMPs (the two
    /// distinct frames); the duplicates are skipped.
    /// </summary>
    [Fact]
    public async Task StartCapture_Video_BmpUnique_DeduplicatesConsecutiveFrames()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var captureService = new CaptureServiceHost(registry);
        var dir = Path.Combine(Path.GetTempPath(), $"vice-sharp-uniq-{Guid.NewGuid():N}");
        var options = new Dictionary<string, string> { ["frames"] = "unique" };

        var start = await captureService.StartCaptureAsync(
            new StartCaptureRequest(session.SessionId, CaptureKind.Video, dir, "bmpseq", options),
            TestContext.Current.CancellationToken);
        Assert.Equal(RpcStatusCode.Ok, start.Status.Code);

        var a = new byte[2 * 2 * 4];
        var b = new byte[2 * 2 * 4];
        Array.Fill(b, (byte)0x7F);
        session.RecordVideoFrameIfActive(a, 2, 2);
        session.RecordVideoFrameIfActive(a, 2, 2); // duplicate -> skipped
        session.RecordVideoFrameIfActive(b, 2, 2);
        session.RecordVideoFrameIfActive(b, 2, 2); // duplicate -> skipped

        var stop = await captureService.StopCaptureAsync(
            new StopCaptureRequest(session.SessionId, start.Capture!.CaptureId),
            TestContext.Current.CancellationToken);
        Assert.Equal(RpcStatusCode.Ok, stop.Status.Code);

        try
        {
            var files = Directory.GetFiles(dir, "frame_*.bmp");
            Assert.Equal(2, files.Length);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    /// <summary>
    /// FR-MED-004 / TR-MEDIA-VIDEO-FFMPEG-001 (capability discovery - muxed video).
    /// Use case: when ffmpeg is installed, the host should advertise the muxed
    /// containers (mp4/mkv/avi) it can drive, each flagged RequiresFfmpeg.
    /// Acceptance: GetCaptureCapabilities includes an mp4 video format whose
    /// RequiresFfmpeg flag is set (alongside the always-available bmpseq).
    /// </summary>
    [Fact]
    public async Task GetCaptureCapabilitiesAsync_WhenFfmpegAvailable_AdvertisesMuxedVideoFormats()
    {
        if (!ViceSharp.Core.Media.FfmpegLocator.IsAvailable)
        {
            Assert.Skip("ffmpeg not installed - muxed video formats are not advertised.");
            return;
        }

        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var captureService = new CaptureServiceHost(registry);

        var response = await captureService.GetCaptureCapabilitiesAsync(
            new SessionRequest(session.SessionId), TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, response.Status.Code);
        Assert.Contains(response.VideoFormats, f => f.Id == "bmpseq" && !f.RequiresFfmpeg);
        Assert.Contains(response.VideoFormats, f => f.Id == "mp4" && f.RequiresFfmpeg && f.SupportsMicrophone);
    }

    /// <summary>
    /// FR-MED-004 (muxed video requires a real video chip).
    /// Use case: a client requests an mp4 recording on the minimal host
    /// architecture, which exposes no video chip.
    /// Acceptance: StartCapture(Video, mp4) returns Unavailable (no video chip, or
    /// ffmpeg missing) and registers no capture - never a half-open ffmpeg process.
    /// </summary>
    [Fact]
    public async Task StartCapture_VideoMp4_NoVideoChip_ReturnsUnavailable()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var captureService = new CaptureServiceHost(registry);
        var path = Path.Combine(Path.GetTempPath(), $"vice-sharp-{Guid.NewGuid():N}.mp4");

        var response = await captureService.StartCaptureAsync(
            new StartCaptureRequest(session.SessionId, CaptureKind.Video, path, "mp4"),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Unavailable, response.Status.Code);
        Assert.Null(response.Capture);
        Assert.Empty(session.CaptureSessions);
        Assert.False(session.IsVideoCaptureActive);
        Assert.False(File.Exists(path));
    }

    /// <summary>
    /// FR-MED (review finding: concurrent/duplicate video capture).
    /// Use case: a second StartCapture(Video) arrives while one is already active.
    /// Acceptance: the second returns FailedPrecondition, registers no capture, and
    /// the first capture stays active.
    /// </summary>
    [Fact]
    public async Task StartCapture_Video_WhenAlreadyActive_ReturnsFailedPrecondition()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var captureService = new CaptureServiceHost(registry);
        var dir1 = Path.Combine(Path.GetTempPath(), $"vice-v1-{Guid.NewGuid():N}");
        var dir2 = Path.Combine(Path.GetTempPath(), $"vice-v2-{Guid.NewGuid():N}");

        try
        {
            var first = await captureService.StartCaptureAsync(
                new StartCaptureRequest(session.SessionId, CaptureKind.Video, dir1, "bmpseq"),
                TestContext.Current.CancellationToken);
            Assert.Equal(RpcStatusCode.Ok, first.Status.Code);

            var second = await captureService.StartCaptureAsync(
                new StartCaptureRequest(session.SessionId, CaptureKind.Video, dir2, "bmpseq"),
                TestContext.Current.CancellationToken);

            Assert.Equal(RpcStatusCode.FailedPrecondition, second.Status.Code);
            Assert.Null(second.Capture);
            Assert.True(session.IsVideoCaptureActive);

            await captureService.StopCaptureAsync(
                new StopCaptureRequest(session.SessionId, first.Capture!.CaptureId),
                TestContext.Current.CancellationToken);
        }
        finally
        {
            if (Directory.Exists(dir1)) Directory.Delete(dir1, recursive: true);
            if (Directory.Exists(dir2)) Directory.Delete(dir2, recursive: true);
        }
    }

    /// <summary>
    /// FR-MED (review finding: single audio tap not arbitrated).
    /// Use case: a second audio-consuming capture starts while the tap is in use.
    /// Acceptance: the second WAV capture returns FailedPrecondition, writes no
    /// output file, and the first recording stays active.
    /// </summary>
    [Fact]
    public async Task StartCapture_Audio_WhenAlreadyRecording_ReturnsFailedPrecondition()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        session.AudioCaptureTap = new CaptureAudioTap(downstream: null);
        registry.Add(session);
        var captureService = new CaptureServiceHost(registry);
        var path1 = Path.Combine(Path.GetTempPath(), $"vice-a1-{Guid.NewGuid():N}.wav");
        var path2 = Path.Combine(Path.GetTempPath(), $"vice-a2-{Guid.NewGuid():N}.wav");

        try
        {
            var first = await captureService.StartCaptureAsync(
                new StartCaptureRequest(session.SessionId, CaptureKind.Audio, path1, "wav"),
                TestContext.Current.CancellationToken);
            Assert.Equal(RpcStatusCode.Ok, first.Status.Code);

            var second = await captureService.StartCaptureAsync(
                new StartCaptureRequest(session.SessionId, CaptureKind.Audio, path2, "wav"),
                TestContext.Current.CancellationToken);

            Assert.Equal(RpcStatusCode.FailedPrecondition, second.Status.Code);
            Assert.Null(second.Capture);
            Assert.False(File.Exists(path2)); // rejected before the output file was created

            await captureService.StopCaptureAsync(
                new StopCaptureRequest(session.SessionId, first.Capture!.CaptureId),
                TestContext.Current.CancellationToken);
        }
        finally
        {
            if (File.Exists(path1)) File.Delete(path1);
            if (File.Exists(path2)) File.Delete(path2);
        }
    }

    /// <summary>
    /// FR-MED (review finding: active captures not finalized on session close).
    /// Use case: a session with in-progress video AND audio captures is closed.
    /// Acceptance: EndAllCaptures finalises both (the recorders are disposed and the
    /// session reports neither capture active), so no ffmpeg process / file handle
    /// is leaked on teardown.
    /// </summary>
    [Fact]
    public void EndAllCaptures_FinalizesActiveVideoAndAudio()
    {
        var session = CreateMinimalSession();
        session.AudioCaptureTap = new CaptureAudioTap(downstream: null);
        var dir = Path.Combine(Path.GetTempPath(), $"vice-endall-{Guid.NewGuid():N}");
        var wavPath = Path.Combine(Path.GetTempPath(), $"vice-endall-{Guid.NewGuid():N}.wav");

        try
        {
            session.BeginVideoCapture("video-cap", new FrameSequenceCapture(dir));
            var stream = new FileStream(wavPath, FileMode.Create, FileAccess.Write);
            session.BeginAudioCapture("audio-cap", new WavAudioRecorder(stream, 44100, 1), stream);

            Assert.True(session.IsVideoCaptureActive);
            Assert.True(session.IsAudioCaptureActive);

            session.EndAllCaptures();

            Assert.False(session.IsVideoCaptureActive);
            Assert.False(session.IsAudioCaptureActive);
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
            if (File.Exists(wavPath)) File.Delete(wavPath);
        }
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
