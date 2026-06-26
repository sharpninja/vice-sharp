namespace ViceSharp.TestHarness;

using ViceSharp.Core;
using ViceSharp.Host.Runtime;
using ViceSharp.Host.Services;
using ViceSharp.Protocol;
using Xunit;

/// <summary>
/// Direct (in-process) unit tests for <see cref="LocalVideoFrameSource"/>.
/// Per the decoupling design (docs/Decoupling.md, BUG-THROTTLE-001), emulation is
/// driven by the host worker (<see cref="EmulationPumpService"/>) and
/// <see cref="LocalVideoFrameSource.GetFrameAsync"/> is a PURE PULL of the
/// committed framebuffer - it never advances the machine.
/// </summary>
public sealed class LocalVideoFrameSourceTests
{
    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary / TR-GRPC-BOUNDARY-001.
    /// Use case: A misconfigured DI container constructs the frame source without
    /// the runtime registry.
    /// Acceptance: The constructor throws ArgumentNullException immediately.
    /// </summary>
    [Fact]
    public void Constructor_NullRegistry_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new LocalVideoFrameSource(null!));
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary / TR-GRPC-BOUNDARY-001.
    /// Use case: A frame poll arrives for a session id the registry never saw.
    /// Acceptance: The response carries NotFound with the id in the message and a
    /// null frame payload.
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
    /// FR/TR: FR-Host-UI-Boundary / TR-GRPC-BOUNDARY-001.
    /// Use case: A poll targets a session whose architecture exposes no video chip.
    /// Acceptance: The response carries Unavailable, a null frame, and a message
    /// naming the missing video chip.
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
    /// FR/TR: FR-Host-UI-Boundary / TR-GRPC-BOUNDARY-001.
    /// Use case: A caller cancels the poll before the service runs.
    /// Acceptance: GetFrameAsync with an already-cancelled token throws
    /// OperationCanceledException before any session lookup or copy.
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
    /// FR/TR: FR-Host-UI-Boundary / TR-GRPC-BOUNDARY-001.
    /// Use case: DI consumers resolve the source through the ILocalVideoFrameSource
    /// interface, not the concrete type.
    /// Acceptance: Invoked through the interface, GetFrameAsync delivers the same
    /// NotFound contract for an unknown session.
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
    /// FR/TR: FR-Host-UI-Boundary / BUG-THROTTLE-001.
    /// Use case: A Stopped session is polled for a frame.
    /// Acceptance: GetFrameAsync returns Ok with the current framebuffer and does
    /// NOT advance the machine (FrameCount unchanged) - it is a pure pull.
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
    /// FR/TR: FR-Host-UI-Boundary / BUG-THROTTLE-001, TR-CYCLE-PACE-001.
    /// Use case: Advancing the emulation is the host worker's job and is driven by
    /// the CPU clock, not the video poll. The poll is a pure pull.
    /// Acceptance: One PumpSession tick advances the master clock (TotalCycles) on a
    /// Running session; a following GetFrameAsync pull returns the committed frame
    /// WITHOUT advancing the clock further.
    /// </summary>
    [Fact]
    public async Task Pump_AdvancesCpuClock_AndGetFrameAsyncPullsWithoutAdvancing()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = TryCreateC64Session("running-session");
        Assert.SkipWhen(session is null, RomsUnavailableSkipReason);
        registry.Add(session);
        session.RunState = EmulatorRunState.Running;
        var pump = new EmulationPumpService(registry);
        var source = new LocalVideoFrameSource(registry);

        var clock = session.Machine.Clock;
        var before = clock.TotalCycles;
        pump.PumpSession(session);
        Assert.True(clock.TotalCycles > before, "pump did not advance the CPU clock");

        var afterPump = clock.TotalCycles;
        var response = await source.GetFrameAsync(session.SessionId, TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, response.Status.Code);
        Assert.NotNull(response.Frame);
        Assert.Equal(afterPump, clock.TotalCycles);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary / BUG-THROTTLE-001, TR-CYCLE-PACE-001.
    /// Use case: The frame counter is decoupled from the pump's cycle pacing - it is
    /// driven by the VIC-II FrameCompleted event as the clock ticks, not by a count
    /// of pump calls. Pumping enough cycle slices to cover a video frame must roll
    /// the counter.
    /// Acceptance: After pumping well over one frame's worth of cycle slices,
    /// FrameCount has increased.
    /// </summary>
    [Fact]
    public void Pump_AccumulatesCompletedFrames_FromVideoChipEvent()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = TryCreateC64Session("repeat-session");
        Assert.SkipWhen(session is null, RomsUnavailableSkipReason);
        registry.Add(session);
        session.RunState = EmulatorRunState.Running;
        var pump = new EmulationPumpService(registry);

        var before = session.FrameCount;
        PumpUntilFrameCompletes(pump, session);

        Assert.True(session.FrameCount > before,
            $"Expected the VIC FrameCompleted event to advance FrameCount past {before}, got {session.FrameCount}.");
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary / BUG-THROTTLE-001, TR-CYCLE-PACE-001.
    /// Use case: A paused session must not advance behind the user's back.
    /// Acceptance: PumpSession advances the CPU clock while Running, then is a no-op
    /// (no cycles) once the session is Paused.
    /// </summary>
    [Fact]
    public void Pump_PausedSession_DoesNotAdvanceCpuClock()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = TryCreateC64Session("pause-session");
        Assert.SkipWhen(session is null, RomsUnavailableSkipReason);
        registry.Add(session);
        var pump = new EmulationPumpService(registry);

        var clock = session.Machine.Clock;
        session.RunState = EmulatorRunState.Running;
        var before = clock.TotalCycles;
        pump.PumpSession(session);
        var afterRunning = clock.TotalCycles;
        Assert.True(afterRunning > before, "pump did not advance the clock while Running");

        session.RunState = EmulatorRunState.Paused;
        pump.PumpSession(session);
        Assert.Equal(afterRunning, clock.TotalCycles);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary / BUG-THROTTLE-001.
    /// Use case: A debugger stamps frames in cycle order as the worker runs.
    /// Acceptance: After two PumpSession ticks the later pulled frame carries a
    /// strictly greater cycle stamp than the earlier one.
    /// </summary>
    [Fact]
    public async Task Pump_AdvancesCycle_ReflectedInPulledFrame()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = TryCreateC64Session("cycle-session");
        Assert.SkipWhen(session is null, RomsUnavailableSkipReason);
        registry.Add(session);
        session.RunState = EmulatorRunState.Running;
        var pump = new EmulationPumpService(registry);
        var source = new LocalVideoFrameSource(registry);

        pump.PumpSession(session);
        var first = await source.GetFrameAsync(session.SessionId, TestContext.Current.CancellationToken);
        pump.PumpSession(session);
        var second = await source.GetFrameAsync(session.SessionId, TestContext.Current.CancellationToken);

        Assert.NotNull(first.Frame);
        Assert.NotNull(second.Frame);
        Assert.True(second.Frame.Cycle > first.Frame.Cycle,
            $"Expected second cycle ({second.Frame.Cycle}) > first cycle ({first.Frame.Cycle}).");
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary / BUG-THROTTLE-001, TR-CYCLE-PACE-001.
    /// Use case: Because the worker advances sub-frame cycle slices, the UI pull must
    /// return the last COMPLETE frame (committed at the video chip's FrameCompleted
    /// boundary), never a half-rendered mid-frame buffer.
    /// Acceptance: After pumping past a frame boundary, the pulled frame's cycle
    /// equals the session's committed-frame cycle and is stable across consecutive
    /// pulls when no new frame has completed.
    /// </summary>
    [Fact]
    public async Task Pump_PullReturnsLastCompletedFrame_NotMidFrame()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = TryCreateC64Session("commit-session");
        Assert.SkipWhen(session is null, RomsUnavailableSkipReason);
        registry.Add(session);
        session.RunState = EmulatorRunState.Running;
        var pump = new EmulationPumpService(registry);
        var source = new LocalVideoFrameSource(registry);

        PumpUntilFrameCompletes(pump, session);
        Assert.True(session.HasCommittedFrame, "no complete frame was committed");

        var first = await source.GetFrameAsync(session.SessionId, TestContext.Current.CancellationToken);
        var second = await source.GetFrameAsync(session.SessionId, TestContext.Current.CancellationToken);

        Assert.NotNull(first.Frame);
        Assert.NotNull(second.Frame);
        Assert.Equal(session.CommittedFrameCycle, first.Frame.Cycle);
        Assert.Equal(first.Frame.Cycle, second.Frame.Cycle);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary / BUG-THROTTLE-001, TR-CYCLE-PACE-001, FR-1132.
    /// Use case: The in-process UI render path copies the emulation thread's latest
    /// published frame straight into its own buffer with no allocation and no
    /// emulation lock (TryCopyFrameInto).
    /// Acceptance: After a frame completes, TryCopyFrameInto fills the caller's
    /// destination span and reports the committed dimensions and cycle.
    /// </summary>
    [Fact]
    public void TryCopyFrameInto_AfterFrameCompletes_CopiesPublishedFrame()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = TryCreateC64Session("zerocopy-session");
        Assert.SkipWhen(session is null, RomsUnavailableSkipReason);
        registry.Add(session);
        session.RunState = EmulatorRunState.Running;
        var pump = new EmulationPumpService(registry);
        var source = new LocalVideoFrameSource(registry);

        PumpUntilFrameCompletes(pump, session);
        Assert.True(session.HasCommittedFrame, "no complete frame was published");

        var videoChip = (ViceSharp.Abstractions.IVideoChip)session.Machine.Devices.GetByRole(ViceSharp.Abstractions.DeviceRole.VideoChip)!;
        var dest = new byte[videoChip.FrameBuffer.Length];

        var ok = source.TryCopyFrameInto(session.SessionId, dest, out var width, out var height, out var cycle);

        Assert.True(ok);
        Assert.True(width > 0);
        Assert.True(height > 0);
        Assert.Equal(session.CommittedFrameCycle, cycle);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary / BUG-THROTTLE-001, TR-CYCLE-PACE-001.
    /// Use case: Before any frame is published (unknown session or one that never
    /// ran) the zero-copy read must fail cleanly rather than expose a partial buffer.
    /// Acceptance: TryCopyFrameInto returns false for an unknown session and for a
    /// freshly created session that has not published a frame.
    /// </summary>
    [Fact]
    public void TryCopyFrameInto_NoPublishedFrame_ReturnsFalse()
    {
        var registry = new EmulatorRuntimeRegistry();
        var source = new LocalVideoFrameSource(registry);
        var dest = new byte[VideoFrameByteLength];

        Assert.False(source.TryCopyFrameInto("nonexistent", dest, out _, out _, out _));

        var session = TryCreateC64Session("fresh-session");
        Assert.SkipWhen(session is null, RomsUnavailableSkipReason);
        registry.Add(session);

        Assert.False(source.TryCopyFrameInto(session.SessionId, dest, out _, out _, out _));
    }

    private const int VideoFrameByteLength = 384 * 272 * 4;

    // A PumpSession advances one clean ~5-instruction group (tens of cycles), so pump
    // until at least one full video frame (~19656 cycles) has completed. Bounded so a
    // machine that never produces frames fails fast instead of hanging.
    private static void PumpUntilFrameCompletes(EmulationPumpService pump, EmulatorRuntimeSession session)
    {
        var before = session.FrameCount;
        for (var i = 0; i < 100_000 && session.FrameCount == before; i++)
            pump.PumpSession(session);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary / TR-GRPC-BOUNDARY-001.
    /// Use case: The UI must not be able to mutate emulator state through the frame
    /// payload it receives.
    /// Acceptance: Mutating the returned Bgra array does not change the live video
    /// chip framebuffer (the payload is a defensive copy).
    /// </summary>
    [Fact]
    public async Task GetFrameAsync_FramePayloadIsDefensiveCopy()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = TryCreateC64Session("copy-session");
        Assert.SkipWhen(session is null, RomsUnavailableSkipReason);
        registry.Add(session);
        var source = new LocalVideoFrameSource(registry);

        var response = await source.GetFrameAsync(session.SessionId, TestContext.Current.CancellationToken);
        Assert.NotNull(response.Frame);

        var videoChip = (ViceSharp.Abstractions.IVideoChip)session.Machine.Devices.GetByRole(ViceSharp.Abstractions.DeviceRole.VideoChip)!;
        var liveBufferSnapshot = (byte[])videoChip.FrameBuffer.Clone();

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
            return null;
        }
    }
}
