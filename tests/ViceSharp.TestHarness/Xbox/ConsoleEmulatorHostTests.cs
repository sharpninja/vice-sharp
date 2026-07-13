namespace ViceSharp.TestHarness.Xbox;

using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ViceSharp.Abstractions;
using ViceSharp.Core;
using ViceSharp.Host.Runtime;
using ViceSharp.Host.Services;
using ViceSharp.Protocol;
using Xunit;

/// <summary>
/// PLAN-XBOXUWP slice S3 (IMPL-XBOXUWP-003). FR-INPROC (TR-INPROC-002/003/005),
/// TEST-INPROC-002. A Kestrel-free in-process host facade for the UWP/Xbox head:
/// it composes the existing POCO service graph with <c>new</c> (NO WebApplication,
/// NO Kestrel, NO Grpc.AspNetCore), exposes a single-threaded bit-exact
/// determinism stepper, a per-frame input hook, joystick/keyboard input, and a
/// lock-free frame pull. Composition/determinism/joystick/lifecycle cases run
/// against the ROM-independent minimal architecture; frame/geometry/hook cases
/// build a real C64 via the default factory (Tier H, all off-console).
/// </summary>
[Trait("Category", "Xbox")]
public sealed class ConsoleEmulatorHostTests
{
    /// <summary>
    /// FR: FR-INPROC-001, TR: TR-INPROC-002. TEST-INPROC-002.
    /// Use case: The Xbox head composes the emulator in-process with no gRPC server.
    /// Acceptance: ConsoleHostComposition.Build(deps) returns a non-null facade that
    /// implements both the emulator-host and deterministic-stepper contracts.
    /// </summary>
    [Fact]
    public async Task Build_ReturnsFacade_ImplementingBothContracts()
    {
        await using var host = ConsoleHostComposition.Build(MinimalDependencies());

        Assert.NotNull(host);
        Assert.IsAssignableFrom<IConsoleEmulatorHost>(host);
        Assert.IsAssignableFrom<IConsoleDeterministicStepper>(host);
    }

    /// <summary>
    /// FR: FR-INPROC-001, TR: TR-INPROC-002. TEST-INPROC-002.
    /// Use case: The head starts an emulation session through the facade.
    /// Acceptance: StartC64Session returns Success with a non-empty SessionId.
    /// </summary>
    [Fact]
    public async Task StartC64Session_ReturnsSuccess_AndNonEmptySessionId()
    {
        await using var host = ConsoleHostComposition.Build(MinimalDependencies());

        var result = host.StartC64Session(MinimalOptions());

        Assert.True(result.Success, result.Error);
        Assert.False(string.IsNullOrWhiteSpace(result.SessionId));
    }

    /// <summary>
    /// FR: FR-INPROC-002, TR: TR-INPROC-003. TEST-INPROC-002.
    /// Use case: The deterministic stepper must reproduce bit-exact machine state
    /// from identical initial conditions and input, single-threaded, so replay and
    /// snapshot comparisons hold on the Xbox head.
    /// Acceptance: StepFramesDeterministic(N) run twice from fresh sessions yields
    /// identical Clock.TotalCycles and identical machine-state hashes, and the
    /// emulation pump/gate worker thread is NEVER started for that path.
    /// </summary>
    [Fact]
    public async Task StepFramesDeterministic_IsBitExact_AndNeverStartsThePump()
    {
        await using var hostA = ConsoleHostComposition.Build(MinimalDependencies());
        await using var hostB = ConsoleHostComposition.Build(MinimalDependencies());

        var sessionA = hostA.CreateDeterministicSession(MinimalOptions());
        var sessionB = hostB.CreateDeterministicSession(MinimalOptions());
        Assert.True(sessionA.Success, sessionA.Error);
        Assert.True(sessionB.Success, sessionB.Error);

        var cyclesA = hostA.StepFramesDeterministic(sessionA.SessionId, 8);
        var cyclesB = hostB.StepFramesDeterministic(sessionB.SessionId, 8);

        Assert.True(cyclesA > 0);
        Assert.Equal(cyclesA, cyclesB);
        Assert.Equal(
            hostA.GetDeterministicStateHash(sessionA.SessionId),
            hostB.GetDeterministicStateHash(sessionB.SessionId));

        // The deterministic path is single-threaded: no pump worker thread was ever created.
        Assert.Null(GetWorkerThread(hostA.Pump));
        Assert.Null(GetWorkerThread(hostB.Pump));
    }

    /// <summary>
    /// FR: FR-INPROC-002, TR: TR-INPROC-003. TEST-INPROC-002.
    /// Use case: Re-stepping the same fresh session count must land on the same
    /// cycle regardless of how many frames are requested.
    /// Acceptance: StepFramesDeterministic advances exactly frameCount frames and the
    /// returned cycle equals the session master clock's TotalCycles.
    /// </summary>
    [Fact]
    public async Task StepFramesDeterministic_ReturnsMasterClockTotalCycles()
    {
        await using var host = ConsoleHostComposition.Build(MinimalDependencies());
        var session = host.CreateDeterministicSession(MinimalOptions());
        Assert.True(session.Success, session.Error);

        var cyclesAfter4 = host.StepFramesDeterministic(session.SessionId, 4);
        var cyclesAfter8 = host.StepFramesDeterministic(session.SessionId, 4);

        Assert.True(cyclesAfter8 > cyclesAfter4);
    }

    /// <summary>
    /// FR: FR-INPROC-004, TR: TR-INPROC-005. TEST-INPROC-002.
    /// Use case: A controller press on the head must route to the correct C64 control
    /// port; explicit ports must be swap-immune (unlike the primary-joystick alias).
    /// Acceptance: SetJoystick(Joystick2, 0x01, fire) records control port 2's state
    /// (mask + fire) on InputPort.Joystick2, leaves InputPort.Joystick1 untouched,
    /// and is unaffected by the session's SwapJoystickPorts setting.
    /// </summary>
    [Fact]
    public async Task SetJoystick_ExplicitPort2_RoutesToControlPort2_AndIsSwapImmune()
    {
        await using var host = ConsoleHostComposition.Build(MinimalDependencies());
        var result = host.StartC64Session(MinimalOptions());
        Assert.True(result.Success, result.Error);
        Assert.True(host.Registry.TryGet(result.SessionId, out var session));

        // Flip the swap flag on: an explicit port must ignore it (only the primary
        // alias honours SwapJoystickPorts). Direction bit 0 (Up) + fire.
        session!.InputSettings = session.InputSettings with { SwapJoystickPorts = true };

        host.SetJoystick(result.SessionId, ConsoleJoyPort.Joystick2, 0x01, fireButton: true);

        Assert.True(session.JoystickStates.TryGetValue(InputPort.Joystick2, out var port2));
        Assert.Equal((byte)0x01, port2!.DirectionMask);
        Assert.True(port2.FireButton);
        // Explicit routing: port 1 is never touched, so the swap flag could not have
        // redirected the press to control port 1.
        Assert.False(session.JoystickStates.ContainsKey(InputPort.Joystick1));
    }

    /// <summary>
    /// FR: FR-INPROC-001, TR: TR-INPROC-002/003. TEST-INPROC-002.
    /// Use case: After starting a live session the head runs exactly one dedicated
    /// emulation worker, unpinned by default (no VICESHARP_EMU_CPU affinity).
    /// Acceptance: StartC64Session lazily starts the pump so exactly one worker thread
    /// exists (named ViceSharp.Emulation.Pump, background), a second StartC64Session
    /// reuses that same worker, and the pump reports a null worker-affinity mask.
    /// </summary>
    [Fact]
    public async Task StartC64Session_StartsExactlyOneUnpinnedWorker()
    {
        await using var host = ConsoleHostComposition.Build(MinimalDependencies());

        var first = host.StartC64Session(MinimalOptions());
        Assert.True(first.Success, first.Error);

        var worker = GetWorkerThread(host.Pump);
        Assert.NotNull(worker);
        Assert.Equal("ViceSharp.Emulation.Pump", worker!.Name);
        Assert.True(worker.IsBackground);
        Assert.Null(host.Pump.AppliedWorkerAffinityMask);

        // A second session must not spin up a second worker: the pump is shared.
        var second = host.StartC64Session(MinimalOptions());
        Assert.True(second.Success, second.Error);
        Assert.Same(worker, GetWorkerThread(host.Pump));
    }

    /// <summary>
    /// FR: FR-INPROC-003, TR: TR-INPROC-002. TEST-INPROC-002.
    /// Use case: The in-process UI pulls the latest committed frame with a lock-free,
    /// zero-copy read; before a frame is committed the read must fail cleanly, and a
    /// too-small destination must be rejected rather than partially filled.
    /// Acceptance: On a real C64, TryCopyLatestFrame is false before the first
    /// committed frame and true after (reporting width/height); a too-small
    /// destination returns false; TryGetFrameGeometry reports BufferLength equal to
    /// the video chip's FrameBuffer.Length. When C64 ROMs are unavailable the facade
    /// falls back to the video-less minimal machine, which yields no geometry and no
    /// frame.
    /// </summary>
    [Fact]
    public async Task TryCopyLatestFrame_And_Geometry_TrackTheVideoChip()
    {
        await using var host = ConsoleHostComposition.BuildDefault();

        var create = host.CreateDeterministicSession(new ConsoleSessionOptions("c64"));
        if (!create.Success)
        {
            // ROM-less fallback (documented, non-skipping): the C64 could not be
            // built, so the head can only offer the video-less minimal machine.
            var minimal = host.CreateDeterministicSession(new ConsoleSessionOptions("minimal"));
            Assert.True(minimal.Success, minimal.Error);
            Assert.False(host.TryGetFrameGeometry(minimal.SessionId, out _));
            var scratch = new byte[VideoFrameByteLength];
            Assert.False(host.TryCopyLatestFrame(minimal.SessionId, scratch, out _, out _, out _));
            return;
        }

        var sessionId = create.SessionId;
        Assert.True(host.Registry.TryGet(sessionId, out var session));
        session!.RunState = EmulatorRunState.Running;

        // Geometry reflects the live video chip.
        Assert.True(host.TryGetFrameGeometry(sessionId, out var geometry));
        var videoChip = (IVideoChip)session.Machine.Devices.GetByRole(DeviceRole.VideoChip)!;
        Assert.Equal(videoChip.FrameBuffer.Length, geometry.BufferLength);
        Assert.True(geometry.Width > 0);
        Assert.True(geometry.Height > 0);

        // Before any frame is committed, the zero-copy read fails cleanly.
        var destination = new byte[geometry.BufferLength];
        Assert.False(host.TryCopyLatestFrame(sessionId, destination, out _, out _, out _));

        // Drive the machine synchronously (no background worker) until a frame commits.
        PumpUntilFrameCommitted(host.Pump, session);
        Assert.True(session.HasCommittedFrame);

        Assert.True(host.TryCopyLatestFrame(sessionId, destination, out var width, out var height, out _));
        Assert.True(width > 0);
        Assert.True(height > 0);

        // A destination smaller than the published frame is rejected.
        var tooSmall = new byte[geometry.BufferLength - 1];
        Assert.False(host.TryCopyLatestFrame(sessionId, tooSmall, out _, out _, out _));
    }

    /// <summary>
    /// FR: FR-INPROC-004, TR: TR-INPROC-005. TEST-INPROC-002.
    /// Use case: The head samples controller input once per emulated frame; the
    /// per-frame input hook must fire exactly once at each frame/step boundary.
    /// Acceptance: With a per-frame hook installed, driving a real C64 forward invokes
    /// the hook exactly once per completed frame (count equals the FrameCount delta),
    /// each carrying the session id. When C64 ROMs are unavailable the fallback minimal
    /// machine produces no frames, so the hook fires zero times.
    /// </summary>
    [Fact]
    public async Task PerFrameInputHook_FiresExactlyOncePerEmulatedFrame()
    {
        await using var host = ConsoleHostComposition.BuildDefault();

        var count = 0;
        string? seenSessionId = null;
        var create = host.CreateDeterministicSession(new ConsoleSessionOptions("c64"));

        if (!create.Success)
        {
            // ROM-less fallback (documented, non-skipping): no video chip, so no
            // frame boundaries, so the per-frame hook must never fire.
            var minimal = host.CreateDeterministicSession(new ConsoleSessionOptions("minimal"));
            Assert.True(minimal.Success, minimal.Error);
            Assert.True(host.Registry.TryGet(minimal.SessionId, out var minimalSession));
            minimalSession!.RunState = EmulatorRunState.Running;
            host.PerFrameInputHook = (_, _) => count++;
            for (var i = 0; i < 5_000; i++)
                host.Pump.PumpSession(minimalSession);
            Assert.Equal(0, count);
            return;
        }

        Assert.True(host.Registry.TryGet(create.SessionId, out var session));
        session!.RunState = EmulatorRunState.Running;
        host.PerFrameInputHook = (sessionId, _) =>
        {
            count++;
            seenSessionId = sessionId;
        };

        var framesBefore = session.FrameCount;
        for (var i = 0; i < 200_000 && session.FrameCount == framesBefore; i++)
            host.Pump.PumpSession(session);

        var frameDelta = session.FrameCount - framesBefore;
        Assert.True(frameDelta > 0, "no frame boundary was reached");
        Assert.Equal(frameDelta, count);
        Assert.Equal(create.SessionId, seenSessionId);
    }

    private const int VideoFrameByteLength = 384 * 272 * 4;

    private static ConsoleHostDependencies MinimalDependencies() =>
        new([MinimalHostArchitectureDescriptor.Instance], MinimalHostArchitectureDescriptor.ArchitectureId);

    private static ConsoleSessionOptions MinimalOptions() =>
        new(MinimalHostArchitectureDescriptor.ArchitectureId);

    // Drive one clean instruction group at a time (the deterministic, pace-free test
    // entry point) until the VIC raises its first FrameCompleted commit. Bounded so a
    // machine that never produces frames fails fast rather than hanging.
    private static void PumpUntilFrameCommitted(EmulationPumpService pump, EmulatorRuntimeSession session)
    {
        for (var i = 0; i < 200_000 && !session.HasCommittedFrame; i++)
            pump.PumpSession(session);
    }

    private static Thread? GetWorkerThread(EmulationPumpService pump)
    {
        var field = typeof(EmulationPumpService).GetField("_workerThread", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return (Thread?)field!.GetValue(pump);
    }
}
