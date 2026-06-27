namespace ViceSharp.TestHarness;

using ViceSharp.Core;
using ViceSharp.Host.Runtime;
using ViceSharp.Host.Services;
using ViceSharp.Protocol;
using Xunit;

/// <summary>
/// Direct (in-process, no gRPC) unit tests for the
/// <see cref="EmulatorHostService"/> RPC surface against the
/// <see cref="IEmulatorHost"/> contract. The host service exposes
/// 16 public methods covering session lifecycle (CreateSession /
/// CloseSession), status (GetStatus), run-state control (Start /
/// Pause / Resume), reset variants (Reset / ColdReset / WarmReset /
/// ResetAsync(ResetRequest) / ResetAndAutostartDrive8), execution
/// step + rewind (StepCycle / StepFrame / RewindCycle / RewindFrame),
/// and limiter configuration (SetLimiterRate). These tests cover a
/// representative subset, exercising the four cross-cutting patterns
/// shared by every RPC: missing-session resolution, valid-session
/// success path, pre-cancelled CancellationToken observation, and
/// request-validation edge cases (zero/negative counts, out-of-range
/// limiter rate). Uses <see cref="MinimalHostArchitectureDescriptor"/>
/// so the tests do not require C64 ROM assets on disk and do not
/// require a running gRPC server.
/// </summary>
public sealed class EmulatorHostServiceTests
{
    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 EmulatorHostService).
    /// Use case: A client constructs <see cref="EmulatorHostService"/>
    /// without supplying a runtime registry (DI misconfiguration).
    /// Acceptance: Constructor throws <see cref="ArgumentNullException"/>
    /// immediately, surfacing the misconfiguration at host startup.
    /// </summary>
    [Fact]
    public void Constructor_NullRegistry_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => new EmulatorHostService(null!, new DefaultEmulatorRuntimeFactory()));
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 EmulatorHostService).
    /// Use case: A client constructs <see cref="EmulatorHostService"/>
    /// without supplying a runtime factory.
    /// Acceptance: Constructor throws <see cref="ArgumentNullException"/>
    /// immediately, before any session would be created.
    /// </summary>
    [Fact]
    public void Constructor_NullFactory_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => new EmulatorHostService(new EmulatorRuntimeRegistry(), null!));
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 EmulatorHostService).
    /// Use case: A client requests a new session against the minimal
    /// architecture.
    /// Acceptance: RPC returns Ok with a non-empty session id, a
    /// populated <see cref="EmulatorStatusDto"/>, and the session id
    /// echoed in the status payload matches the response session id.
    /// </summary>
    [Fact]
    public async Task CreateSessionAsync_Valid_ReturnsOkAndSessionId()
    {
        var service = CreateService();

        var response = await service.CreateSessionAsync(
            new CreateEmulatorSessionRequest(MinimalHostArchitectureDescriptor.ArchitectureId),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, response.Status.Code);
        Assert.False(string.IsNullOrWhiteSpace(response.SessionId));
        Assert.NotNull(response.EmulatorStatus);
        Assert.Equal(response.SessionId, response.EmulatorStatus!.SessionId);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 EmulatorHostService).
    /// Use case: A client requests a session for an architecture id
    /// that the host's factory does not recognise.
    /// Acceptance: RPC returns InvalidArgument (not Ok), an empty
    /// session id, and a null status payload so the caller can detect
    /// the misconfiguration deterministically.
    /// </summary>
    [Fact]
    public async Task CreateSessionAsync_UnknownArchitecture_ReturnsInvalidArgument()
    {
        var service = CreateService();

        var response = await service.CreateSessionAsync(
            new CreateEmulatorSessionRequest("not-a-real-architecture-id"),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.InvalidArgument, response.Status.Code);
        Assert.Equal(string.Empty, response.SessionId);
        Assert.Null(response.EmulatorStatus);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 EmulatorHostService).
    /// Use case: A client queries status for an unknown session id.
    /// Acceptance: RPC returns the standard missing-session NotFound
    /// status (carrying the unknown id) and a null status payload.
    /// </summary>
    [Fact]
    public async Task GetStatusAsync_MissingSession_ReturnsMissingSessionStatus()
    {
        var service = CreateService();

        var response = await service.GetStatusAsync(
            new SessionRequest("ghost-session"),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.NotFound, response.Status.Code);
        Assert.Contains("ghost-session", response.Status.Message);
        Assert.Null(response.EmulatorStatus);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 EmulatorHostService).
    /// Use case: A client queries status for a freshly created session.
    /// Acceptance: RPC returns Ok with a non-null status payload that
    /// reports RunState.Stopped (the initial state of a session that
    /// has not been started yet).
    /// </summary>
    [Fact]
    public async Task GetStatusAsync_Valid_ReturnsOkWithState()
    {
        var service = CreateService();
        var sessionId = await CreateSessionAsync(service);

        var response = await service.GetStatusAsync(
            new SessionRequest(sessionId),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, response.Status.Code);
        Assert.NotNull(response.EmulatorStatus);
        Assert.Equal(EmulatorRunState.Stopped, response.EmulatorStatus!.RunState);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 EmulatorHostService).
    /// Use case: A client transitions a valid session through Start ->
    /// Pause -> Resume.
    /// Acceptance: Each call returns Ok and the reported RunState
    /// matches the expected transition (Running, Paused, Running).
    /// </summary>
    [Fact]
    public async Task StartPauseResumeAsync_Valid_DrivesRunState()
    {
        var service = CreateService();
        var sessionId = await CreateSessionAsync(service);
        var request = new SessionRequest(sessionId);

        var start = await service.StartAsync(request, TestContext.Current.CancellationToken);
        Assert.Equal(RpcStatusCode.Ok, start.Status.Code);
        Assert.Equal(EmulatorRunState.Running, start.EmulatorStatus!.RunState);

        var pause = await service.PauseAsync(request, TestContext.Current.CancellationToken);
        Assert.Equal(RpcStatusCode.Ok, pause.Status.Code);
        Assert.Equal(EmulatorRunState.Paused, pause.EmulatorStatus!.RunState);

        var resume = await service.ResumeAsync(request, TestContext.Current.CancellationToken);
        Assert.Equal(RpcStatusCode.Ok, resume.Status.Code);
        Assert.Equal(EmulatorRunState.Running, resume.EmulatorStatus!.RunState);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 EmulatorHostService).
    /// Use case: A client invokes any run-state RPC against an unknown
    /// session id.
    /// Acceptance: Every run-state RPC (Start / Pause / Resume) returns
    /// the standard missing-session NotFound status and a null status
    /// payload, regardless of which entry point was used.
    /// </summary>
    [Theory]
    [InlineData("Start")]
    [InlineData("Pause")]
    [InlineData("Resume")]
    public async Task RunStateRpcs_MissingSession_ReturnsMissingSessionStatus(string method)
    {
        var service = CreateService();
        var request = new SessionRequest("ghost-session");

        var response = method switch
        {
            "Start" => await service.StartAsync(request, TestContext.Current.CancellationToken),
            "Pause" => await service.PauseAsync(request, TestContext.Current.CancellationToken),
            "Resume" => await service.ResumeAsync(request, TestContext.Current.CancellationToken),
            _ => throw new InvalidOperationException(method),
        };

        Assert.Equal(RpcStatusCode.NotFound, response.Status.Code);
        Assert.Contains("ghost-session", response.Status.Message);
        Assert.Null(response.EmulatorStatus);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 EmulatorHostService).
    /// Use case: A client invokes a reset RPC against an unknown
    /// session id (cold and warm variants share the same dispatch).
    /// Acceptance: Both ColdResetAsync and WarmResetAsync return the
    /// standard missing-session NotFound status and a null payload.
    /// </summary>
    [Theory]
    [InlineData("Cold")]
    [InlineData("Warm")]
    [InlineData("Default")]
    public async Task ResetAsync_MissingSession_ReturnsMissingSessionStatus(string kind)
    {
        var service = CreateService();
        var request = new SessionRequest("ghost-session");

        var response = kind switch
        {
            "Cold" => await service.ColdResetAsync(request, TestContext.Current.CancellationToken),
            "Warm" => await service.WarmResetAsync(request, TestContext.Current.CancellationToken),
            _ => await service.ResetAsync(request, TestContext.Current.CancellationToken),
        };

        Assert.Equal(RpcStatusCode.NotFound, response.Status.Code);
        Assert.Contains("ghost-session", response.Status.Message);
        Assert.Null(response.EmulatorStatus);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 EmulatorHostService).
    /// Use case: A client invokes ColdReset / WarmReset against a
    /// valid session id and expects the machine to reboot and keep
    /// running, exactly as a real C64 reset reboots into a running
    /// BASIC - not halt at the reset vector.
    /// Acceptance: Each call returns Ok with a non-null status payload
    /// and leaves the session in RunState.Running, so the emulation pump
    /// resumes advancing the clock (previously it forced Stopped, which
    /// wedged the emulator until a manual Resume).
    /// </summary>
    [Theory]
    [InlineData("Cold")]
    [InlineData("Warm")]
    public async Task ResetAsync_Valid_ReturnsOkAndRunningState(string kind)
    {
        var service = CreateService();
        var sessionId = await CreateSessionAsync(service);
        await service.StartAsync(new SessionRequest(sessionId), TestContext.Current.CancellationToken);
        var request = new SessionRequest(sessionId);

        var response = kind switch
        {
            "Cold" => await service.ColdResetAsync(request, TestContext.Current.CancellationToken),
            "Warm" => await service.WarmResetAsync(request, TestContext.Current.CancellationToken),
            _ => throw new InvalidOperationException(kind),
        };

        Assert.Equal(RpcStatusCode.Ok, response.Status.Code);
        Assert.NotNull(response.EmulatorStatus);
        Assert.Equal(EmulatorRunState.Running, response.EmulatorStatus!.RunState);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 EmulatorHostService).
    /// Use case: A client invokes StepCycle / StepFrame with a
    /// non-positive count (zero or negative).
    /// Acceptance: The host returns InvalidArgument (not Ok or
    /// NotFound) before it even attempts a registry lookup, so the
    /// caller learns of the bad request regardless of session id.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task StepCycleAsync_NonPositiveCount_ReturnsInvalidArgument(int count)
    {
        var service = CreateService();
        var sessionId = await CreateSessionAsync(service);

        var response = await service.StepCycleAsync(
            new StepCycleRequest(sessionId, count),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.InvalidArgument, response.Status.Code);
        Assert.Null(response.EmulatorStatus);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 EmulatorHostService).
    /// Use case: A client invokes StepCycle for a session id that the
    /// host does not know about.
    /// Acceptance: The host returns the standard missing-session
    /// NotFound status (only when the cycle count is valid; otherwise
    /// InvalidArgument takes precedence per the implementation order).
    /// </summary>
    [Fact]
    public async Task StepCycleAsync_MissingSession_ReturnsMissingSessionStatus()
    {
        var service = CreateService();

        var response = await service.StepCycleAsync(
            new StepCycleRequest("ghost-session", 1),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.NotFound, response.Status.Code);
        Assert.Contains("ghost-session", response.Status.Message);
        Assert.Null(response.EmulatorStatus);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 EmulatorHostService).
    /// Use case: A client invokes StepCycle for a valid session id
    /// with a positive cycle count.
    /// Acceptance: The host returns Ok with a non-null status payload
    /// reporting the post-step state.
    /// </summary>
    [Fact]
    public async Task StepCycleAsync_Valid_ReturnsOk()
    {
        var service = CreateService();
        var sessionId = await CreateSessionAsync(service);

        var response = await service.StepCycleAsync(
            new StepCycleRequest(sessionId, 1),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, response.Status.Code);
        Assert.NotNull(response.EmulatorStatus);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 EmulatorHostService).
    /// Use case: A client invokes RewindCycle for an unknown session.
    /// Acceptance: Missing-session resolution wins over the
    /// NotImplemented response that a valid session would receive, so
    /// the host returns the standard NotFound status carrying the
    /// unknown id.
    /// </summary>
    [Fact]
    public async Task RewindCycleAsync_MissingSession_ReturnsMissingSessionStatus()
    {
        var service = CreateService();

        var response = await service.RewindCycleAsync(
            new RewindCycleRequest("ghost-session", 1),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.NotFound, response.Status.Code);
        Assert.Contains("ghost-session", response.Status.Message);
        Assert.Null(response.EmulatorStatus);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 EmulatorHostService).
    /// Use case: A client invokes RewindCycle / RewindFrame for a
    /// valid session.
    /// Acceptance: The host returns the well-known NotImplemented
    /// status (reverse stepping requires bounded execution history)
    /// alongside a non-null status payload so the caller can still
    /// display current state while reporting the feature is missing.
    /// </summary>
    [Theory]
    [InlineData("Cycle")]
    [InlineData("Frame")]
    public async Task RewindAsync_Valid_ReturnsNotImplemented(string kind)
    {
        var service = CreateService();
        var sessionId = await CreateSessionAsync(service);

        EmulatorCommandResponse response = kind switch
        {
            "Cycle" => await service.RewindCycleAsync(
                new RewindCycleRequest(sessionId, 1),
                TestContext.Current.CancellationToken),
            "Frame" => await service.RewindFrameAsync(
                new RewindFrameRequest(sessionId, 1),
                TestContext.Current.CancellationToken),
            _ => throw new InvalidOperationException(kind),
        };

        Assert.Equal(RpcStatusCode.NotImplemented, response.Status.Code);
        Assert.NotNull(response.EmulatorStatus);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 EmulatorHostService).
    /// Use case: A client invokes SetLimiterRate with an
    /// out-of-bounds rate (negative, above the ceiling, or NaN).
    /// Acceptance: The host returns InvalidArgument with a null
    /// status payload, regardless of whether the session exists, so
    /// the caller learns of the bad request deterministically.
    /// </summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(2000)]
    [InlineData(double.NaN)]
    public async Task SetLimiterRateAsync_OutOfRange_ReturnsInvalidArgument(double rate)
    {
        var service = CreateService();
        var sessionId = await CreateSessionAsync(service);

        var response = await service.SetLimiterRateAsync(
            new SetLimiterRateRequest(sessionId, rate),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.InvalidArgument, response.Status.Code);
        Assert.Null(response.EmulatorStatus);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 EmulatorHostService).
    /// Use case: A client invokes SetLimiterRate against a valid
    /// session with a positive rate inside the legal range.
    /// Acceptance: The host returns Ok with a non-null status payload
    /// reporting the new effective limiter rate.
    /// </summary>
    [Fact]
    public async Task SetLimiterRateAsync_Valid_ReturnsOk()
    {
        var service = CreateService();
        var sessionId = await CreateSessionAsync(service);

        var response = await service.SetLimiterRateAsync(
            new SetLimiterRateRequest(sessionId, 75.0),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, response.Status.Code);
        Assert.NotNull(response.EmulatorStatus);
        Assert.Equal(75.0, response.EmulatorStatus!.LimiterRatePercent);
    }

    /// <summary>
    /// FR: FR-WARP-001, TR: TR-WARP-STATUS-001.
    /// Use case: gRPC debuggers need a live warp toggle that does not require
    /// a session reset or the broader settings RPC.
    /// Acceptance: SetLimiterRate(0) disables the limiter and reports the
    /// status warp signal; a later positive SetLimiterRate re-enables the limiter.
    /// </summary>
    [Fact]
    public async Task SetLimiterRateAsync_ZeroTogglesWarpWithoutReset()
    {
        var service = CreateService();
        var sessionId = await CreateSessionAsync(service);

        var warp = await service.SetLimiterRateAsync(
            new SetLimiterRateRequest(sessionId, 0),
            TestContext.Current.CancellationToken);
        var limited = await service.SetLimiterRateAsync(
            new SetLimiterRateRequest(sessionId, 75),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, warp.Status.Code);
        Assert.NotNull(warp.EmulatorStatus);
        Assert.Equal(0, warp.EmulatorStatus!.LimiterRatePercent);

        Assert.Equal(RpcStatusCode.Ok, limited.Status.Code);
        Assert.NotNull(limited.EmulatorStatus);
        Assert.Equal(75, limited.EmulatorStatus!.LimiterRatePercent);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 EmulatorHostService).
    /// Use case: A client invokes CloseSession against an unknown
    /// session id.
    /// Acceptance: The host returns the standard missing-session
    /// NotFound status and a null payload, without touching any other
    /// session's run state.
    /// </summary>
    [Fact]
    public async Task CloseSessionAsync_MissingSession_ReturnsMissingSessionStatus()
    {
        var service = CreateService();

        var response = await service.CloseSessionAsync(
            new SessionRequest("ghost-session"),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.NotFound, response.Status.Code);
        Assert.Contains("ghost-session", response.Status.Message);
        Assert.Null(response.EmulatorStatus);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 EmulatorHostService).
    /// Use case: A client invokes CloseSession against a valid
    /// session id, then immediately retries GetStatus against the
    /// same id.
    /// Acceptance: CloseSession returns Ok, and the subsequent
    /// GetStatus returns the standard missing-session NotFound
    /// status, demonstrating the session was actually removed from
    /// the registry.
    /// </summary>
    [Fact]
    public async Task CloseSessionAsync_Valid_RemovesFromRegistry()
    {
        var service = CreateService();
        var sessionId = await CreateSessionAsync(service);

        var close = await service.CloseSessionAsync(
            new SessionRequest(sessionId),
            TestContext.Current.CancellationToken);
        var status = await service.GetStatusAsync(
            new SessionRequest(sessionId),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, close.Status.Code);
        Assert.Equal(RpcStatusCode.NotFound, status.Status.Code);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 EmulatorHostService).
    /// Use case: A client cancels each major RPC entry point before
    /// the host has a chance to service it (pre-cancelled token).
    /// Acceptance: Each RPC observes the token and throws
    /// <see cref="OperationCanceledException"/> immediately, before
    /// any registry lookup or state mutation occurs. This is the
    /// shared cancellation contract for the host-RPC boundary.
    /// </summary>
    [Theory]
    [InlineData("CreateSession")]
    [InlineData("GetStatus")]
    [InlineData("Start")]
    [InlineData("StepCycle")]
    [InlineData("RewindFrame")]
    [InlineData("SetLimiterRate")]
    [InlineData("CloseSession")]
    public async Task Rpcs_CancelledToken_Throws(string method)
    {
        var service = CreateService();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var sessionRequest = new SessionRequest("anything");

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            switch (method)
            {
                case "CreateSession":
                    await service.CreateSessionAsync(new CreateEmulatorSessionRequest(), cts.Token);
                    break;
                case "GetStatus":
                    await service.GetStatusAsync(sessionRequest, cts.Token);
                    break;
                case "Start":
                    await service.StartAsync(sessionRequest, cts.Token);
                    break;
                case "StepCycle":
                    await service.StepCycleAsync(new StepCycleRequest("anything", 1), cts.Token);
                    break;
                case "RewindFrame":
                    await service.RewindFrameAsync(new RewindFrameRequest("anything", 1), cts.Token);
                    break;
                case "SetLimiterRate":
                    await service.SetLimiterRateAsync(new SetLimiterRateRequest("anything", 100), cts.Token);
                    break;
                case "CloseSession":
                    await service.CloseSessionAsync(sessionRequest, cts.Token);
                    break;
                default:
                    throw new InvalidOperationException(method);
            }
        });
    }

    private static EmulatorHostService CreateService()
    {
        var factory = new DefaultEmulatorRuntimeFactory(
            new ArchitectureBuilder(),
            [MinimalHostArchitectureDescriptor.Instance],
            MinimalHostArchitectureDescriptor.ArchitectureId);
        return new EmulatorHostService(new EmulatorRuntimeRegistry(), factory);
    }

    private static async Task<string> CreateSessionAsync(EmulatorHostService service)
    {
        var response = await service.CreateSessionAsync(
            new CreateEmulatorSessionRequest(MinimalHostArchitectureDescriptor.ArchitectureId),
            TestContext.Current.CancellationToken);
        Assert.Equal(RpcStatusCode.Ok, response.Status.Code);
        return response.SessionId;
    }
}
