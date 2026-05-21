namespace ViceSharp.TestHarness;

using ViceSharp.Core;
using ViceSharp.Host.Runtime;
using ViceSharp.Host.Services;
using ViceSharp.Protocol;
using Xunit;

/// <summary>
/// Direct (in-process) unit tests for <see cref="LocalVideoFrameSource"/>,
/// the in-process video-frame producer registered as
/// <see cref="ILocalVideoFrameSource"/> in the host composition root.
/// This service is the bridge between the host's gRPC
/// <see cref="VideoServiceHost.GetFrameAsync"/> surface (which is a
/// stateless pull-only adapter) and the runtime registry, with one
/// critical behavioural difference: when the session's
/// <see cref="EmulatorRuntimeSession.RunState"/> is
/// <see cref="EmulatorRunState.Running"/>, calling
/// <see cref="LocalVideoFrameSource.GetFrameAsync"/> drives the
/// emulator forward by one frame (RunFrame + RecordFrame +
/// AdvanceHostAutomationFrame) before snapshotting the framebuffer.
/// In every other run-state, GetFrameAsync is a pure read of the
/// current framebuffer. Tests cover constructor guards, the cancellation
/// contract, the missing-session NotFound path, the no-video-chip
/// Unavailable path, and the running-vs-non-running frame-advance
/// distinction (the part that is unique to this service).
/// </summary>
public sealed class LocalVideoFrameSourceTests
{
    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 LocalVideoFrameSource).
    /// Use case: A client constructs a <see cref="LocalVideoFrameSource"/>
    /// without supplying the runtime registry (a misconfigured DI
    /// container is the most common failure mode).
    /// Acceptance: The constructor throws
    /// <see cref="ArgumentNullException"/> immediately so the
    /// composition error surfaces at host startup rather than at first
    /// RPC.
    /// </summary>
    [Fact]
    public void Constructor_NullRegistry_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new LocalVideoFrameSource(null!));
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 LocalVideoFrameSource).
    /// Use case: A caller invokes <see cref="LocalVideoFrameSource.GetFrameAsync"/>
    /// with a session id that the runtime registry has never seen (e.g.
    /// the session was already closed before the video poll arrived).
    /// Acceptance: The response carries the standard missing-session
    /// NotFound status with the unknown id surfaced in the message and
    /// a null frame payload (no allocation).
    /// </summary>
    [Fact]
    public async Task GetFrameAsync_MissingSession_ReturnsNotFoundStatus()
    {
        var registry = new EmulatorRuntimeRegistry();
        var source = new LocalVideoFrameSource(registry);

        var response = await source.GetFrameAsync("does-not-exist", TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.NotFound, response.Status.Code);
        Assert.Contains("does-not-exist", response.Status.Message);
        Assert.Null(response.Frame);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 LocalVideoFrameSource).
    /// Use case: A caller invokes <see cref="LocalVideoFrameSource.GetFrameAsync"/>
    /// against a freshly registered session whose architecture exposes
    /// no video chip (the minimal-host descriptor has zero devices). The
    /// service must refuse gracefully without allocating a phantom
    /// framebuffer.
    /// Acceptance: The response carries
    /// <see cref="RpcStatusCode.Unavailable"/>, a null frame payload,
    /// and a message that identifies the missing video chip.
    /// </summary>
    [Fact]
    public async Task GetFrameAsync_ValidSessionNoVideoChip_ReturnsUnavailable()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var source = new LocalVideoFrameSource(registry);

        var response = await source.GetFrameAsync(session.SessionId, TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Unavailable, response.Status.Code);
        Assert.Null(response.Frame);
        Assert.Contains("video chip", response.Status.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 LocalVideoFrameSource).
    /// Use case: A caller cancels a video-frame poll before the service
    /// has a chance to run; the service must observe the cancellation
    /// before any session lookup, frame advance, or framebuffer copy.
    /// Acceptance: Invoking
    /// <see cref="LocalVideoFrameSource.GetFrameAsync"/> with an
    /// already-cancelled <see cref="CancellationToken"/> throws
    /// <see cref="OperationCanceledException"/>.
    /// </summary>
    [Fact]
    public async Task GetFrameAsync_CancelledToken_Throws()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var source = new LocalVideoFrameSource(registry);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await source.GetFrameAsync(session.SessionId, cts.Token));
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 LocalVideoFrameSource).
    /// Use case: The contract surface that DI consumers depend on is
    /// the <see cref="ILocalVideoFrameSource"/> interface, not the
    /// concrete class. Hosts that wire the registry should be able to
    /// resolve the source through the interface and the resulting
    /// behaviour must be identical to invoking the concrete type.
    /// Acceptance: The concrete <see cref="LocalVideoFrameSource"/> can
    /// be assigned to <see cref="ILocalVideoFrameSource"/> and
    /// GetFrameAsync delivers the same NotFound status when invoked
    /// through the interface for an unknown session.
    /// </summary>
    [Fact]
    public async Task GetFrameAsync_InvokedThroughInterface_PreservesContract()
    {
        var registry = new EmulatorRuntimeRegistry();
        ILocalVideoFrameSource source = new LocalVideoFrameSource(registry);

        var response = await source.GetFrameAsync("ghost-session", TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.NotFound, response.Status.Code);
        Assert.Contains("ghost-session", response.Status.Message);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 LocalVideoFrameSource).
    /// Use case: A C64 session is registered but never marked as
    /// running (RunState defaults to Stopped). A client polls for a
    /// frame: the source must return the current framebuffer state
    /// without advancing the machine - polling a stopped session
    /// MUST NOT silently execute cycles.
    /// Acceptance: The response carries
    /// <see cref="RpcStatusCode.Ok"/>, a populated
    /// <see cref="VideoFrameDto"/> with the chip's frame dimensions,
    /// and the session's <see cref="EmulatorRuntimeSession.FrameCount"/>
    /// remains zero (no RecordFrame call was made because RunState was
    /// not Running).
    /// </summary>
    [Fact]
    public async Task GetFrameAsync_StoppedSession_ReturnsFrameWithoutAdvancingMachine()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = TryCreateC64Session("stopped-session");
        Assert.SkipWhen(session is null, RomsUnavailableSkipReason);
        registry.Add(session);
        var source = new LocalVideoFrameSource(registry);

        Assert.Equal(EmulatorRunState.Stopped, session.RunState);
        var startingFrameCount = session.FrameCount;

        var response = await source.GetFrameAsync(session.SessionId, TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, response.Status.Code);
        Assert.NotNull(response.Frame);
        Assert.Equal(startingFrameCount, session.FrameCount);
        Assert.True(response.Frame.Width > 0);
        Assert.True(response.Frame.Height > 0);
        Assert.Equal(response.Frame.Width * response.Frame.Height * 4, response.Frame.Bgra.Length);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 LocalVideoFrameSource).
    /// Use case: A C64 session is in the
    /// <see cref="EmulatorRunState.Running"/> state. The host's video
    /// poll is the work-pump for the emulator: each call must run
    /// exactly one frame and record the advance on the session
    /// performance counters so callers (the UI) drive emulation
    /// throughput by polling at the desired FPS.
    /// Acceptance: A single GetFrameAsync call increments
    /// <see cref="EmulatorRuntimeSession.FrameCount"/> by exactly one
    /// and the response carries a populated frame payload whose
    /// dimensions match the video chip.
    /// </summary>
    [Fact]
    public async Task GetFrameAsync_RunningSession_AdvancesOneFrameAndRecordsCounter()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = TryCreateC64Session("running-session");
        Assert.SkipWhen(session is null, RomsUnavailableSkipReason);
        registry.Add(session);
        session.RunState = EmulatorRunState.Running;
        var source = new LocalVideoFrameSource(registry);

        var beforeFrameCount = session.FrameCount;

        var response = await source.GetFrameAsync(session.SessionId, TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, response.Status.Code);
        Assert.NotNull(response.Frame);
        Assert.Equal(beforeFrameCount + 1, session.FrameCount);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 LocalVideoFrameSource).
    /// Use case: A running C64 session is polled repeatedly (the UI's
    /// frame-display loop). Each poll must independently advance the
    /// emulator so a UI that polls N times in a second drives N frames
    /// of work.
    /// Acceptance: After three successive GetFrameAsync calls against a
    /// Running session, <see cref="EmulatorRuntimeSession.FrameCount"/>
    /// has advanced by exactly three and every response carries an Ok
    /// status with a populated frame payload.
    /// </summary>
    [Fact]
    public async Task GetFrameAsync_RunningSession_RepeatedPollsAdvanceFrameCount()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = TryCreateC64Session("repeat-session");
        Assert.SkipWhen(session is null, RomsUnavailableSkipReason);
        registry.Add(session);
        session.RunState = EmulatorRunState.Running;
        var source = new LocalVideoFrameSource(registry);

        var beforeFrameCount = session.FrameCount;

        for (var i = 0; i < 3; i++)
        {
            var response = await source.GetFrameAsync(session.SessionId, TestContext.Current.CancellationToken);
            Assert.Equal(RpcStatusCode.Ok, response.Status.Code);
            Assert.NotNull(response.Frame);
        }

        Assert.Equal(beforeFrameCount + 3, session.FrameCount);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 LocalVideoFrameSource).
    /// Use case: A session is created, marked Running so the source
    /// will execute machine work, then switched back to Paused before
    /// the next poll (UI pause). The source must respect the latest
    /// run state and stop pumping frames - polling a Paused session
    /// MUST NOT continue emulation behind the user's back.
    /// Acceptance: The Running poll advances the frame counter by one,
    /// then after RunState is set to Paused a subsequent poll returns
    /// Ok with a populated frame but leaves the frame counter at the
    /// post-running value (zero further advances).
    /// </summary>
    [Fact]
    public async Task GetFrameAsync_PausedSessionAfterRunning_StopsAdvancing()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = TryCreateC64Session("pause-session");
        Assert.SkipWhen(session is null, RomsUnavailableSkipReason);
        registry.Add(session);
        session.RunState = EmulatorRunState.Running;
        var source = new LocalVideoFrameSource(registry);

        var beforeFrameCount = session.FrameCount;
        await source.GetFrameAsync(session.SessionId, TestContext.Current.CancellationToken);
        var afterRunningFrameCount = session.FrameCount;

        session.RunState = EmulatorRunState.Paused;
        var pausedResponse = await source.GetFrameAsync(session.SessionId, TestContext.Current.CancellationToken);

        Assert.Equal(beforeFrameCount + 1, afterRunningFrameCount);
        Assert.Equal(RpcStatusCode.Ok, pausedResponse.Status.Code);
        Assert.NotNull(pausedResponse.Frame);
        Assert.Equal(afterRunningFrameCount, session.FrameCount);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 LocalVideoFrameSource).
    /// Use case: After running one or more frames in a session, a UI
    /// caller polls again. The cycle reported in the
    /// <see cref="VideoFrameDto.Cycle"/> field must monotonically
    /// reflect the machine's live cycle counter so a debugger can stamp
    /// frames in cycle order.
    /// Acceptance: For a Running session, the cycle on a later poll is
    /// strictly greater than the cycle on the first poll (the machine
    /// has executed cycles in between).
    /// </summary>
    [Fact]
    public async Task GetFrameAsync_RunningSession_CycleIncreasesAcrossPolls()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = TryCreateC64Session("cycle-session");
        Assert.SkipWhen(session is null, RomsUnavailableSkipReason);
        registry.Add(session);
        session.RunState = EmulatorRunState.Running;
        var source = new LocalVideoFrameSource(registry);

        var first = await source.GetFrameAsync(session.SessionId, TestContext.Current.CancellationToken);
        var second = await source.GetFrameAsync(session.SessionId, TestContext.Current.CancellationToken);

        Assert.NotNull(first.Frame);
        Assert.NotNull(second.Frame);
        Assert.True(second.Frame.Cycle > first.Frame.Cycle,
            $"Expected second cycle ({second.Frame.Cycle}) > first cycle ({first.Frame.Cycle}).");
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 LocalVideoFrameSource).
    /// Use case: A C64 session is polled while Running and the response
    /// carries the framebuffer bytes back to the caller. The byte array
    /// in the DTO must be an independent copy (defensive copy) of the
    /// chip's live framebuffer so the caller cannot mutate emulator
    /// state by writing into the response payload.
    /// Acceptance: Mutating the returned <see cref="VideoFrameDto.Bgra"/>
    /// array does NOT change the underlying video chip framebuffer
    /// bytes (the response payload is decoupled from the live buffer).
    /// </summary>
    [Fact]
    public async Task GetFrameAsync_RunningSession_FramePayloadIsDefensiveCopy()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = TryCreateC64Session("copy-session");
        Assert.SkipWhen(session is null, RomsUnavailableSkipReason);
        registry.Add(session);
        session.RunState = EmulatorRunState.Running;
        var source = new LocalVideoFrameSource(registry);

        var response = await source.GetFrameAsync(session.SessionId, TestContext.Current.CancellationToken);
        Assert.NotNull(response.Frame);

        var videoChip = (ViceSharp.Abstractions.IVideoChip)session.Machine.Devices.GetByRole(ViceSharp.Abstractions.DeviceRole.VideoChip)!;
        var liveBufferSnapshot = (byte[])videoChip.FrameBuffer.Clone();

        // Mutate the response payload - the live buffer must remain unchanged.
        for (var i = 0; i < response.Frame.Bgra.Length; i++)
            response.Frame.Bgra[i] = (byte)~response.Frame.Bgra[i];

        Assert.Equal(liveBufferSnapshot, videoChip.FrameBuffer);
    }

    private static EmulatorRuntimeSession CreateMinimalSession()
    {
        var factory = new DefaultEmulatorRuntimeFactory(
            new ArchitectureBuilder(),
            [MinimalHostArchitectureDescriptor.Instance],
            MinimalHostArchitectureDescriptor.ArchitectureId);
        return factory.Create(new CreateEmulatorSessionRequest());
    }

    private const string RomsUnavailableSkipReason =
        "C64 ROMs are not available through the VICE data resolver; running/paused video-frame source tests require BASIC/KERNAL/character ROMs to build a machine with a video chip.";

    private static EmulatorRuntimeSession? TryCreateC64Session(string sessionId)
    {
        try
        {
            var machine = MachineTestFactory.CreateC64Machine();
            return new EmulatorRuntimeSession(sessionId, machine.Architecture, machine);
        }
        catch (DirectoryNotFoundException)
        {
            // No C64 ROMs available in this worktree; the test is skipped
            // so that ROM-free CI runs are still green while ROM-equipped
            // runs exercise the full video frame source.
            return null;
        }
    }
}
