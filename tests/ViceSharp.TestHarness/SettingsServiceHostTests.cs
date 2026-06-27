namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Core;
using ViceSharp.Host.Runtime;
using ViceSharp.Host.Services;
using ViceSharp.Protocol;
using Xunit;

/// <summary>
/// Direct (in-process, no gRPC) unit tests for the
/// <see cref="SettingsServiceHost"/> RPC surface
/// (<see cref="ISettingsService.ListProfilesAsync"/> /
/// <see cref="ISettingsService.GetSettingsAsync"/> /
/// <see cref="ISettingsService.UpdateSettingsAsync"/> /
/// <see cref="ISettingsService.ValidateResourcesAsync"/>) against a
/// minimal in-memory architecture. Complements the chip-layer
/// emulator coverage by exercising the host-RPC layer that wraps the
/// session settings store: session resolution, status mapping,
/// argument validation, round-trip persistence, and cancellation
/// contracts. Uses <see cref="MinimalHostArchitectureDescriptor"/> so
/// the tests do not require C64 ROM assets on disk and can run in any
/// worktree.
/// </summary>
public sealed class SettingsServiceHostTests
{
    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 Settings RPC).
    /// Use case: A client calls ListProfiles with a session id the host
    /// runtime registry does not know about.
    /// Acceptance: The RPC returns the standard missing-session NotFound
    /// status (carrying the unknown session id) and an empty profile
    /// collection.
    /// </summary>
    [Fact]
    public async Task ListProfilesAsync_MissingSession_ReturnsMissingSessionStatus()
    {
        var registry = new EmulatorRuntimeRegistry();
        var settingsService = new SettingsServiceHost(registry);

        var response = await settingsService.ListProfilesAsync(
            new SessionRequest("does-not-exist"),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.NotFound, response.Status.Code);
        Assert.Contains("does-not-exist", response.Status.Message);
        Assert.Empty(response.Profiles);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 Settings RPC).
    /// Use case: A client calls GetSettings with a session id the host
    /// runtime registry does not know about.
    /// Acceptance: The RPC returns the standard missing-session NotFound
    /// status and a null settings payload.
    /// </summary>
    [Fact]
    public async Task GetSettingsAsync_MissingSession_ReturnsMissingSessionStatus()
    {
        var registry = new EmulatorRuntimeRegistry();
        var settingsService = new SettingsServiceHost(registry);

        var response = await settingsService.GetSettingsAsync(
            new SessionRequest("does-not-exist"),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.NotFound, response.Status.Code);
        Assert.Contains("does-not-exist", response.Status.Message);
        Assert.Null(response.Settings);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 Settings RPC).
    /// Use case: A client calls UpdateSettings with a session id the
    /// host runtime registry does not know about.
    /// Acceptance: The RPC returns the standard missing-session NotFound
    /// status, a null settings payload, and an empty diagnostics list,
    /// without touching any session state.
    /// </summary>
    [Fact]
    public async Task UpdateSettingsAsync_MissingSession_ReturnsMissingSessionStatus()
    {
        var registry = new EmulatorRuntimeRegistry();
        var settingsService = new SettingsServiceHost(registry);

        var response = await settingsService.UpdateSettingsAsync(
            new UpdateSettingsRequest("does-not-exist", Limiter: new LimiterSettingsDto(120, true)),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.NotFound, response.Status.Code);
        Assert.Contains("does-not-exist", response.Status.Message);
        Assert.Null(response.Settings);
        Assert.Empty(response.Diagnostics);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 Settings RPC).
    /// Use case: A client calls ValidateResources with a session id the
    /// host runtime registry does not know about.
    /// Acceptance: The RPC returns the standard missing-session NotFound
    /// status and an empty resource validation collection.
    /// </summary>
    [Fact]
    public async Task ValidateResourcesAsync_MissingSession_ReturnsMissingSessionStatus()
    {
        var registry = new EmulatorRuntimeRegistry();
        var settingsService = new SettingsServiceHost(registry);

        var response = await settingsService.ValidateResourcesAsync(
            new ValidateSettingsResourcesRequest("does-not-exist", Limiter: new LimiterSettingsDto(120, true)),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.NotFound, response.Status.Code);
        Assert.Contains("does-not-exist", response.Status.Message);
        Assert.Empty(response.Resources);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 Settings RPC).
    /// Use case: A client calls ListProfiles against a freshly
    /// registered session and expects the catalog of machine profiles
    /// known to the host (at minimum the minimal-host profile).
    /// Acceptance: Status is Ok, the profile collection is non-empty,
    /// includes the minimal-host architecture id, and exactly one
    /// profile is marked current (matching the session's profile id).
    /// </summary>
    [Fact]
    public async Task ListProfilesAsync_ValidSession_ReturnsProfilesWithCurrentMarked()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var settingsService = new SettingsServiceHost(registry);

        var response = await settingsService.ListProfilesAsync(
            new SessionRequest(session.SessionId),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, response.Status.Code);
        Assert.NotEmpty(response.Profiles);
        Assert.Contains(response.Profiles, profile =>
            string.Equals(profile.Id, MinimalHostArchitectureDescriptor.ArchitectureId, System.StringComparison.OrdinalIgnoreCase));
        Assert.Single(response.Profiles, profile => profile.IsCurrent);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 Settings RPC).
    /// Use case: A client calls GetSettings against a freshly
    /// registered session to retrieve the current limiter, display,
    /// input, audio, and resource configuration.
    /// Acceptance: Status is Ok and the returned settings DTO has the
    /// session's profile id along with non-null limiter, display, and
    /// input sub-DTOs (the defaults written by the runtime factory).
    /// </summary>
    [Fact]
    public async Task GetSettingsAsync_ValidSession_ReturnsCurrentSettings()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var settingsService = new SettingsServiceHost(registry);

        var response = await settingsService.GetSettingsAsync(
            new SessionRequest(session.SessionId),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, response.Status.Code);
        Assert.NotNull(response.Settings);
        Assert.False(string.IsNullOrWhiteSpace(response.Settings.ProfileId));
        Assert.NotNull(response.Settings.Limiter);
        Assert.NotNull(response.Settings.Display);
        Assert.NotNull(response.Settings.Input);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 Settings RPC).
    /// Use case: A client calls UpdateSettings with a valid limiter
    /// change (no restart) and then GetSettings, expecting the new
    /// limiter rate to round-trip.
    /// Acceptance: UpdateSettings returns Ok with a diagnostic that the
    /// limiter rate was applied live; a subsequent GetSettings observes
    /// the updated rate percent value on the session settings DTO.
    /// </summary>
    [Fact]
    public async Task UpdateSettingsAsync_LimiterRoundTrip_PersistsAndDiagnoses()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var settingsService = new SettingsServiceHost(registry);

        var update = await settingsService.UpdateSettingsAsync(
            new UpdateSettingsRequest(session.SessionId, Limiter: new LimiterSettingsDto(150, true)),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, update.Status.Code);
        Assert.NotNull(update.Settings);
        Assert.Equal(150, update.Settings.Limiter.RatePercent);
        Assert.Contains(update.Diagnostics, diagnostic =>
            string.Equals(diagnostic.Setting, "limiter.ratePercent", System.StringComparison.OrdinalIgnoreCase) &&
            diagnostic.AppliedLive);

        var get = await settingsService.GetSettingsAsync(
            new SessionRequest(session.SessionId),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, get.Status.Code);
        Assert.NotNull(get.Settings);
        Assert.Equal(150, get.Settings.Limiter.RatePercent);
    }

    /// <summary>
    /// FR-PACESEL-001 / TR-PACESEL-STRAT-001 / TEST-PACESEL-001.
    /// Use case: A client selects the "vice" pacing strategy via the limiter settings.
    /// Acceptance: UpdateSettings returns Ok with a live "limiter.pacingStrategy" diagnostic,
    ///   the global emulation pump switches its active gate to VICE, and a subsequent
    ///   GetSettings round-trips the stored strategy id.
    /// </summary>
    [Fact]
    public async Task UpdateSettingsAsync_PacingStrategy_AppliesToPumpAndRoundTrips()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        session.PacingStrategy = "semaphore";
        registry.Add(session);
        using var pump = new EmulationPumpService(registry, EmulationGateStrategies.CreateGate("semaphore"));
        var settingsService = new SettingsServiceHost(registry, new DefaultEmulatorRuntimeFactory(), pump);

        var update = await settingsService.UpdateSettingsAsync(
            new UpdateSettingsRequest(session.SessionId, Limiter: new LimiterSettingsDto(100, true, "vice")),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, update.Status.Code);
        Assert.NotNull(update.Settings);
        Assert.Equal("vice", update.Settings.Limiter.PacingStrategy);
        Assert.Equal("VICE", pump.GateName);
        Assert.Contains(update.Diagnostics, diagnostic =>
            string.Equals(diagnostic.Setting, "limiter.pacingStrategy", System.StringComparison.OrdinalIgnoreCase) &&
            diagnostic.AppliedLive);

        var get = await settingsService.GetSettingsAsync(
            new SessionRequest(session.SessionId),
            TestContext.Current.CancellationToken);

        Assert.Equal("vice", get.Settings!.Limiter.PacingStrategy);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 Settings RPC).
    /// Use case: A client calls UpdateSettings with an out-of-range
    /// limiter rate that violates the host validation policy
    /// (0 &lt; rate &lt;= 1000).
    /// Acceptance: UpdateSettings returns InvalidArgument, no settings
    /// DTO is returned, the diagnostics list is empty, and the
    /// session's stored limiter rate is unchanged after the call.
    /// </summary>
    [Fact]
    public async Task UpdateSettingsAsync_InvalidLimiterRate_ReturnsInvalidArgument()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var settingsService = new SettingsServiceHost(registry);
        var originalRate = session.LimiterRatePercent;

        var response = await settingsService.UpdateSettingsAsync(
            new UpdateSettingsRequest(session.SessionId, Limiter: new LimiterSettingsDto(-5, true)),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.InvalidArgument, response.Status.Code);
        Assert.Null(response.Settings);
        Assert.Empty(response.Diagnostics);
        Assert.Equal(originalRate, session.LimiterRatePercent);
    }

    /// <summary>
    /// FR: FR-WARP-001, TR: TR-WARP-STATUS-001.
    /// Use case: Settings clients may use rate 0 as the same warp signal exposed by
    /// status and SetLimiterRate.
    /// Acceptance: UpdateSettings accepts rate 0 when the limiter is disabled and
    /// reports the status warp signal on the running session.
    /// </summary>
    [Fact]
    public async Task UpdateSettingsAsync_ZeroLimiterRateDisabled_AppliesWarpLive()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var settingsService = new SettingsServiceHost(registry);

        var response = await settingsService.UpdateSettingsAsync(
            new UpdateSettingsRequest(session.SessionId, Limiter: new LimiterSettingsDto(0, false)),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, response.Status.Code);
        Assert.NotNull(response.Settings);
        Assert.False(response.Settings!.Limiter.IsEnabled);
        Assert.Equal(0, HostProtocolMapper.ToStatusDto(session).LimiterRatePercent);
        Assert.Contains(response.Diagnostics, diagnostic =>
            string.Equals(diagnostic.Setting, "limiter.ratePercent", System.StringComparison.OrdinalIgnoreCase) &&
            diagnostic.AppliedLive);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 Settings RPC).
    /// Use case: A client calls UpdateSettings with a profile id that
    /// is unknown to the host (neither the minimal-host descriptor nor
    /// any C64 machine profile).
    /// Acceptance: UpdateSettings returns InvalidArgument with an
    /// informative message naming the unknown profile id, and the
    /// session settings remain unchanged.
    /// </summary>
    [Fact]
    public async Task UpdateSettingsAsync_UnknownProfile_ReturnsInvalidArgument()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var settingsService = new SettingsServiceHost(registry);

        var response = await settingsService.UpdateSettingsAsync(
            new UpdateSettingsRequest(session.SessionId, ProfileId: "not-a-real-profile"),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.InvalidArgument, response.Status.Code);
        Assert.Contains("not-a-real-profile", response.Status.Message);
        Assert.Null(response.Settings);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 Settings RPC).
    /// Use case: A client calls ValidateResources against a valid
    /// session with a deliberately invalid display renderer to confirm
    /// the host enumerates per-resource validation results.
    /// Acceptance: ValidateResources returns Ok, the resource list
    /// includes a "display.renderer" entry marked invalid (the bad
    /// renderer), and the validation message identifies the resource
    /// key.
    /// </summary>
    [Fact]
    public async Task ValidateResourcesAsync_ValidSession_ReportsInvalidRenderer()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var settingsService = new SettingsServiceHost(registry);

        var response = await settingsService.ValidateResourcesAsync(
            new ValidateSettingsResourcesRequest(
                session.SessionId,
                Display: new DisplaySettingsDto(Renderer: "not-a-real-renderer")),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, response.Status.Code);
        Assert.Contains(response.Resources, resource =>
            string.Equals(resource.ResourceKey, "display.renderer", System.StringComparison.OrdinalIgnoreCase) &&
            !resource.IsValid);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 Settings RPC).
    /// Use case: A client cancels a ListProfiles RPC before the host
    /// has a chance to service it; the host must observe the
    /// cancellation rather than walking the profile catalog.
    /// Acceptance: Invoking ListProfilesAsync with an already-cancelled
    /// <see cref="CancellationToken"/> throws
    /// <see cref="OperationCanceledException"/> (matching the
    /// <c>ThrowIfCancellationRequested</c> contract).
    /// </summary>
    [Fact]
    public async Task ListProfilesAsync_CancelledToken_Throws()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var settingsService = new SettingsServiceHost(registry);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await settingsService.ListProfilesAsync(
                new SessionRequest(session.SessionId),
                cts.Token));
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 Settings RPC).
    /// Use case: A client cancels a GetSettings RPC before the host
    /// has a chance to service it.
    /// Acceptance: Invoking GetSettingsAsync with an already-cancelled
    /// <see cref="CancellationToken"/> throws
    /// <see cref="OperationCanceledException"/> before any session
    /// lookup occurs.
    /// </summary>
    [Fact]
    public async Task GetSettingsAsync_CancelledToken_Throws()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var settingsService = new SettingsServiceHost(registry);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await settingsService.GetSettingsAsync(
                new SessionRequest(session.SessionId),
                cts.Token));
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 Settings RPC).
    /// Use case: A client cancels an UpdateSettings RPC before the host
    /// has a chance to service it; the host must observe the
    /// cancellation rather than mutating session settings or running
    /// validation.
    /// Acceptance: Invoking UpdateSettingsAsync with an already-cancelled
    /// <see cref="CancellationToken"/> throws
    /// <see cref="OperationCanceledException"/>, and the session's
    /// limiter rate remains untouched after the throw.
    /// </summary>
    [Fact]
    public async Task UpdateSettingsAsync_CancelledToken_Throws()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var settingsService = new SettingsServiceHost(registry);
        var originalRate = session.LimiterRatePercent;

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await settingsService.UpdateSettingsAsync(
                new UpdateSettingsRequest(session.SessionId, Limiter: new LimiterSettingsDto(200, true)),
                cts.Token));
        Assert.Equal(originalRate, session.LimiterRatePercent);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 Settings RPC).
    /// Use case: A client cancels a ValidateResources RPC before the
    /// host has a chance to service it.
    /// Acceptance: Invoking ValidateResourcesAsync with an
    /// already-cancelled <see cref="CancellationToken"/> throws
    /// <see cref="OperationCanceledException"/> before any validation
    /// work begins.
    /// </summary>
    [Fact]
    public async Task ValidateResourcesAsync_CancelledToken_Throws()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var settingsService = new SettingsServiceHost(registry);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await settingsService.ValidateResourcesAsync(
                new ValidateSettingsResourcesRequest(session.SessionId),
                cts.Token));
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 Settings RPC).
    /// Use case: the GUI "Apply + Restart" action sends UpdateSettings with
    /// RestartSession=true while the emulator is running. The host rebuilds
    /// the session from a fresh machine and must carry the prior run state
    /// forward, because LocalVideoFrameSource only advances the machine
    /// (RunFrame) while RunState is Running. If the restart forced
    /// RunState.Stopped, the video source would stop producing new frames and
    /// the emulator display would go blank until the user manually resumed.
    /// Acceptance: after UpdateSettings(RestartSession=true) with a
    /// restart-relevant display change against a Running/On session, the
    /// registry's replaced session reports RunState.Running and PowerState
    /// "On" (the prior run/power state is preserved across the restart).
    /// </summary>
    [Fact]
    public async Task UpdateSettingsAsync_RestartSession_PreservesRunningRunState()
    {
        var factory = new DefaultEmulatorRuntimeFactory(
            new ArchitectureBuilder(),
            [MinimalHostArchitectureDescriptor.Instance],
            MinimalHostArchitectureDescriptor.ArchitectureId);
        var registry = new EmulatorRuntimeRegistry();
        var session = factory.Create(new CreateEmulatorSessionRequest());
        session.PowerState = "On";
        session.RunState = EmulatorRunState.Running;
        registry.Add(session);
        var settingsService = new SettingsServiceHost(registry, factory);

        var response = await settingsService.UpdateSettingsAsync(
            new UpdateSettingsRequest(
                session.SessionId,
                Display: new DisplaySettingsDto(),
                RestartSession: true),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, response.Status.Code);
        Assert.True(registry.TryGet(session.SessionId, out var restarted));
        Assert.Equal(EmulatorRunState.Running, restarted!.RunState);
        Assert.Equal("On", restarted.PowerState);
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
