namespace ViceSharp.TestHarness.Xbox;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ViceSharp.Protocol;
using ViceSharp.Xbox.Input;
using Xunit;

/// <summary>
/// PLAN-XBOXUWP slice S13 (IMPL-XBOXUWP-013), TEST-SYSBTN-002. Guards the thin
/// <see cref="AppCommandDispatcher"/> that translates the discrete
/// <see cref="AppCommand"/>s emitted by the input-context machine (S11) into the
/// correct <c>ViceSharp.Protocol</c> host-service calls.
/// </summary>
/// <remarks>
/// <para>
/// The dispatcher marshals every state-mutating command through the host's
/// session-locked async methods (it awaits them; the host locks the session
/// internally, so awaiting off the input-polling thread IS the marshaling) and
/// never mutates emulator state on the input thread. It uses:
/// <list type="bullet">
///   <item><description>AutostartDrive8 -&gt; <see cref="IEmulatorHost.ResetAndAutostartDrive8Async"/>.</description></item>
///   <item><description>WarmReset -&gt; <see cref="IEmulatorHost.WarmResetAsync"/>.</description></item>
///   <item><description>ColdReset -&gt; <see cref="IEmulatorHost.ColdResetAsync"/>.</description></item>
///   <item><description>WarpHoldOn/WarpHoldOff -&gt; <see cref="IEmulatorHost.SetLimiterRateAsync"/> (rate 0 = warp, rate 100 = normal).</description></item>
///   <item><description>QuickSaveState -&gt; <see cref="ISnapshotService.CaptureSnapshotAsync"/>; QuickLoadState -&gt; <see cref="ISnapshotService.RestoreSnapshotAsync"/>.</description></item>
///   <item><description>SwapJoystickPorts -&gt; NO host/settings call; the dispatcher reports a local <c>ToggleSwapPorts</c> flip only.</description></item>
///   <item><description>RequestExit -&gt; the injected exit callback only.</description></item>
///   <item><description>UI-only commands (menu/keyboard/navigation/confirm/None) -&gt; no host interaction (handled by the UI layer).</description></item>
/// </list>
/// </para>
/// <para>
/// Determinism/marshaling is asserted structurally: recording fakes of the three
/// Protocol host interfaces capture exactly which method (and session/args) was
/// invoked; unrelated interface members throw so an accidental call surfaces.
/// </para>
/// </remarks>
[Trait("Category", "Xbox")]
public sealed class XboxAppCommandDispatcherTests
{
    private const string Session = "session-42";

    private static (AppCommandDispatcher Dispatcher,
                    RecordingEmulatorHost Host,
                    RecordingSnapshotService Snapshots,
                    RecordingSettingsService Settings,
                    ExitProbe Exit) NewDispatcher()
    {
        var host = new RecordingEmulatorHost();
        var snapshots = new RecordingSnapshotService();
        var settings = new RecordingSettingsService();
        var exit = new ExitProbe();
        var dispatcher = new AppCommandDispatcher(host, snapshots, settings, exit.Fire);
        return (dispatcher, host, snapshots, settings, exit);
    }

    // ------------------------------------------------------------------
    // State-mutating host commands (emulator host)
    // ------------------------------------------------------------------

    /// <summary>
    /// TEST-SYSBTN-002 (IMPL-XBOXUWP-013): AutostartDrive8 marshals onto
    /// <see cref="IEmulatorHost.ResetAndAutostartDrive8Async"/> with the target
    /// session, and touches no other service.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public async Task AutostartDrive8_InvokesResetAndAutostartDrive8OnHost()
    {
        var (dispatcher, host, snapshots, settings, exit) = NewDispatcher();

        var result = await dispatcher.DispatchAsync(Session, AppCommand.AutostartDrive8, TestContext.Current.CancellationToken);

        Assert.Equal(new[] { nameof(IEmulatorHost.ResetAndAutostartDrive8Async) }, host.Calls);
        Assert.Equal(new[] { Session }, host.Sessions);
        Assert.False(result.ToggleSwapPorts);
        AssertNoOtherServices(snapshots, settings, exit);
    }

    /// <summary>
    /// TEST-SYSBTN-002 (IMPL-XBOXUWP-013): WarmReset marshals onto
    /// <see cref="IEmulatorHost.WarmResetAsync"/>.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public async Task WarmReset_InvokesWarmResetOnHost()
    {
        var (dispatcher, host, snapshots, settings, exit) = NewDispatcher();

        var result = await dispatcher.DispatchAsync(Session, AppCommand.WarmReset, TestContext.Current.CancellationToken);

        Assert.Equal(new[] { nameof(IEmulatorHost.WarmResetAsync) }, host.Calls);
        Assert.Equal(new[] { Session }, host.Sessions);
        Assert.False(result.ToggleSwapPorts);
        AssertNoOtherServices(snapshots, settings, exit);
    }

    /// <summary>
    /// TEST-SYSBTN-002 (IMPL-XBOXUWP-013): ColdReset marshals onto
    /// <see cref="IEmulatorHost.ColdResetAsync"/>.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public async Task ColdReset_InvokesColdResetOnHost()
    {
        var (dispatcher, host, snapshots, settings, exit) = NewDispatcher();

        var result = await dispatcher.DispatchAsync(Session, AppCommand.ColdReset, TestContext.Current.CancellationToken);

        Assert.Equal(new[] { nameof(IEmulatorHost.ColdResetAsync) }, host.Calls);
        Assert.Equal(new[] { Session }, host.Sessions);
        Assert.False(result.ToggleSwapPorts);
        AssertNoOtherServices(snapshots, settings, exit);
    }

    /// <summary>
    /// TEST-SYSBTN-002 (IMPL-XBOXUWP-013): WarpHoldOn sets the limiter to warp
    /// (<see cref="IEmulatorHost.SetLimiterRateAsync"/> rate 0, the host's documented
    /// warp entry).
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public async Task WarpHoldOn_SetsLimiterToWarpRate()
    {
        var (dispatcher, host, snapshots, settings, exit) = NewDispatcher();

        var result = await dispatcher.DispatchAsync(Session, AppCommand.WarpHoldOn, TestContext.Current.CancellationToken);

        Assert.Equal(new[] { nameof(IEmulatorHost.SetLimiterRateAsync) }, host.Calls);
        Assert.Equal(new[] { Session }, host.Sessions);
        Assert.Equal(AppCommandDispatcher.WarpLimiterRatePercent, host.LastLimiterRate);
        Assert.Equal(0, host.LastLimiterRate);
        Assert.False(result.ToggleSwapPorts);
        AssertNoOtherServices(snapshots, settings, exit);
    }

    /// <summary>
    /// TEST-SYSBTN-002 (IMPL-XBOXUWP-013): WarpHoldOff restores the limiter to the
    /// normal 100% rate (<see cref="IEmulatorHost.SetLimiterRateAsync"/> rate 100).
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public async Task WarpHoldOff_RestoresLimiterToFullRate()
    {
        var (dispatcher, host, snapshots, settings, exit) = NewDispatcher();

        var result = await dispatcher.DispatchAsync(Session, AppCommand.WarpHoldOff, TestContext.Current.CancellationToken);

        Assert.Equal(new[] { nameof(IEmulatorHost.SetLimiterRateAsync) }, host.Calls);
        Assert.Equal(new[] { Session }, host.Sessions);
        Assert.Equal(AppCommandDispatcher.NormalLimiterRatePercent, host.LastLimiterRate);
        Assert.Equal(100, host.LastLimiterRate);
        Assert.False(result.ToggleSwapPorts);
        AssertNoOtherServices(snapshots, settings, exit);
    }

    // ------------------------------------------------------------------
    // Snapshot commands (quick save / quick load)
    // ------------------------------------------------------------------

    /// <summary>
    /// TEST-SYSBTN-002 (IMPL-XBOXUWP-013): QuickSaveState captures a snapshot via
    /// <see cref="ISnapshotService.CaptureSnapshotAsync"/> for the target session and
    /// restores nothing.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public async Task QuickSaveState_CapturesSnapshotOnly()
    {
        var (dispatcher, host, snapshots, settings, exit) = NewDispatcher();

        var result = await dispatcher.DispatchAsync(Session, AppCommand.QuickSaveState, TestContext.Current.CancellationToken);

        Assert.Equal(1, snapshots.CaptureCount);
        Assert.Equal(0, snapshots.RestoreCount);
        Assert.Equal(Session, snapshots.LastSession);
        Assert.False(result.ToggleSwapPorts);
        Assert.Equal(0, host.TotalCalls);
        Assert.Equal(0, settings.TotalCalls);
        Assert.Equal(0, exit.Count);
    }

    /// <summary>
    /// TEST-SYSBTN-002 (IMPL-XBOXUWP-013): a QuickSaveState followed by a
    /// QuickLoadState restores the exact snapshot captured (the dispatcher holds a
    /// quick-save slot), via <see cref="ISnapshotService.RestoreSnapshotAsync"/>.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public async Task QuickLoadState_AfterSave_RestoresCapturedSnapshot()
    {
        var (dispatcher, host, snapshots, settings, exit) = NewDispatcher();

        await dispatcher.DispatchAsync(Session, AppCommand.QuickSaveState, TestContext.Current.CancellationToken);
        var result = await dispatcher.DispatchAsync(Session, AppCommand.QuickLoadState, TestContext.Current.CancellationToken);

        Assert.Equal(1, snapshots.CaptureCount);
        Assert.Equal(1, snapshots.RestoreCount);
        Assert.Equal(snapshots.CaptureReturn, snapshots.LastRestored);
        Assert.Equal(Session, snapshots.LastSession);
        Assert.False(result.ToggleSwapPorts);
        Assert.Equal(0, host.TotalCalls);
        Assert.Equal(0, settings.TotalCalls);
        Assert.Equal(0, exit.Count);
    }

    /// <summary>
    /// TEST-SYSBTN-002 (IMPL-XBOXUWP-013): QuickLoadState with no prior QuickSaveState
    /// is a safe no-op - nothing is captured and nothing is restored.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public async Task QuickLoadState_WithoutPriorSave_IsNoOp()
    {
        var (dispatcher, host, snapshots, settings, exit) = NewDispatcher();

        var result = await dispatcher.DispatchAsync(Session, AppCommand.QuickLoadState, TestContext.Current.CancellationToken);

        Assert.Equal(0, snapshots.CaptureCount);
        Assert.Equal(0, snapshots.RestoreCount);
        Assert.False(result.ToggleSwapPorts);
        Assert.Equal(0, host.TotalCalls);
        Assert.Equal(0, settings.TotalCalls);
        Assert.Equal(0, exit.Count);
    }

    // ------------------------------------------------------------------
    // Local-only + callback commands
    // ------------------------------------------------------------------

    /// <summary>
    /// TEST-SYSBTN-002 (IMPL-XBOXUWP-013): SwapJoystickPorts makes NO host and NO
    /// settings call; it only reports a local <c>ToggleSwapPorts</c> flip that the
    /// caller applies to its <see cref="XboxInputConfig"/>.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public async Task SwapJoystickPorts_TogglesLocalFlag_AndCallsNoService()
    {
        var (dispatcher, host, snapshots, settings, exit) = NewDispatcher();

        var result = await dispatcher.DispatchAsync(Session, AppCommand.SwapJoystickPorts, TestContext.Current.CancellationToken);

        Assert.True(result.ToggleSwapPorts);

        // The reported flip toggles the caller's config, with no settings round-trip.
        var config = XboxInputConfig.Default;
        var flipped = result.ToggleSwapPorts ? config with { SwapPorts = !config.SwapPorts } : config;
        Assert.True(flipped.SwapPorts);

        Assert.Equal(0, host.TotalCalls);
        Assert.Equal(0, snapshots.TotalCalls);
        Assert.Equal(0, settings.TotalCalls);
        Assert.Equal(0, exit.Count);
    }

    /// <summary>
    /// TEST-SYSBTN-002 (IMPL-XBOXUWP-013): RequestExit fires the injected exit
    /// callback exactly once and makes no host/snapshot/settings call.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public async Task RequestExit_FiresExitCallbackOnce_AndCallsNoService()
    {
        var (dispatcher, host, snapshots, settings, exit) = NewDispatcher();

        var result = await dispatcher.DispatchAsync(Session, AppCommand.RequestExit, TestContext.Current.CancellationToken);

        Assert.Equal(1, exit.Count);
        Assert.False(result.ToggleSwapPorts);
        Assert.Equal(0, host.TotalCalls);
        Assert.Equal(0, snapshots.TotalCalls);
        Assert.Equal(0, settings.TotalCalls);
    }

    /// <summary>
    /// PLAN-XBOXUWP S40: when the head supplies the optional menu callbacks, OpenMainMenu fires
    /// onOpenMenu and CloseMenu fires onCloseMenu (each exactly once) so the head can reveal/hide the
    /// shell menu over the running emulator - still with NO host/snapshot/settings call and no exit.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public async Task OpenMainMenu_And_CloseMenu_FireTheirCallbacks_AndCallNoService()
    {
        var host = new RecordingEmulatorHost();
        var snapshots = new RecordingSnapshotService();
        var settings = new RecordingSettingsService();
        var exit = new ExitProbe();
        var openCount = 0;
        var closeCount = 0;
        var dispatcher = new AppCommandDispatcher(
            host,
            snapshots,
            settings,
            exit.Fire,
            onOpenMenu: () => openCount++,
            onCloseMenu: () => closeCount++);

        await dispatcher.DispatchAsync(Session, AppCommand.OpenMainMenu, TestContext.Current.CancellationToken);
        await dispatcher.DispatchAsync(Session, AppCommand.CloseMenu, TestContext.Current.CancellationToken);

        Assert.Equal(1, openCount);
        Assert.Equal(1, closeCount);
        Assert.Equal(0, host.TotalCalls);
        Assert.Equal(0, snapshots.TotalCalls);
        Assert.Equal(0, settings.TotalCalls);
        Assert.Equal(0, exit.Count);
    }

    // ------------------------------------------------------------------
    // UI-only commands (handled by the UI layer, never the host)
    // ------------------------------------------------------------------

    public static IEnumerable<object[]> UiOnlyCommands()
    {
        yield return new object[] { AppCommand.None };
        yield return new object[] { AppCommand.OpenMainMenu };
        yield return new object[] { AppCommand.CloseMenu };
        yield return new object[] { AppCommand.ToggleVirtualKeyboard };
        yield return new object[] { AppCommand.ConfirmYes };
        yield return new object[] { AppCommand.ConfirmNo };
        yield return new object[] { AppCommand.UiNavigateUp };
        yield return new object[] { AppCommand.UiNavigateDown };
        yield return new object[] { AppCommand.UiNavigateLeft };
        yield return new object[] { AppCommand.UiNavigateRight };
        yield return new object[] { AppCommand.UiActivate };
        yield return new object[] { AppCommand.UiBack };
    }

    /// <summary>
    /// TEST-SYSBTN-002 (IMPL-XBOXUWP-013): the UI-only commands (menu, virtual
    /// keyboard, UI navigation, confirm dialog, and None) are handled entirely by the
    /// UI layer, so the dispatcher makes zero host/snapshot/settings calls, fires no
    /// exit, and reports no swap flip for any of them.
    /// </summary>
    [Theory]
    [MemberData(nameof(UiOnlyCommands))]
    [Trait("Category", "Xbox")]
    public async Task UiOnlyCommand_ProducesNoServiceCall(AppCommand command)
    {
        var (dispatcher, host, snapshots, settings, exit) = NewDispatcher();

        var result = await dispatcher.DispatchAsync(Session, command, TestContext.Current.CancellationToken);

        Assert.False(result.ToggleSwapPorts);
        Assert.Equal(0, host.TotalCalls);
        Assert.Equal(0, snapshots.TotalCalls);
        Assert.Equal(0, settings.TotalCalls);
        Assert.Equal(0, exit.Count);
    }

    private static void AssertNoOtherServices(
        RecordingSnapshotService snapshots,
        RecordingSettingsService settings,
        ExitProbe exit)
    {
        Assert.Equal(0, snapshots.TotalCalls);
        Assert.Equal(0, settings.TotalCalls);
        Assert.Equal(0, exit.Count);
    }

    // ------------------------------------------------------------------
    // Recording fakes of the Protocol host interfaces
    // ------------------------------------------------------------------

    private sealed class ExitProbe
    {
        public int Count { get; private set; }

        public void Fire() => Count++;
    }

    private sealed class RecordingEmulatorHost : IEmulatorHost
    {
        public List<string> Calls { get; } = new();

        public List<string> Sessions { get; } = new();

        public double? LastLimiterRate { get; private set; }

        public int TotalCalls => Calls.Count;

        private static ValueTask<EmulatorCommandResponse> Ok()
            => ValueTask.FromResult(new EmulatorCommandResponse(RpcStatus.Ok(), null));

        public ValueTask<EmulatorCommandResponse> ResetAndAutostartDrive8Async(
            ResetAndAutostartDrive8Request request,
            CancellationToken cancellationToken = default)
        {
            Calls.Add(nameof(ResetAndAutostartDrive8Async));
            Sessions.Add(request.SessionId);
            return Ok();
        }

        public ValueTask<EmulatorCommandResponse> WarmResetAsync(
            SessionRequest request,
            CancellationToken cancellationToken = default)
        {
            Calls.Add(nameof(WarmResetAsync));
            Sessions.Add(request.SessionId);
            return Ok();
        }

        public ValueTask<EmulatorCommandResponse> ColdResetAsync(
            SessionRequest request,
            CancellationToken cancellationToken = default)
        {
            Calls.Add(nameof(ColdResetAsync));
            Sessions.Add(request.SessionId);
            return Ok();
        }

        public ValueTask<EmulatorCommandResponse> SetLimiterRateAsync(
            SetLimiterRateRequest request,
            CancellationToken cancellationToken = default)
        {
            Calls.Add(nameof(SetLimiterRateAsync));
            Sessions.Add(request.SessionId);
            LastLimiterRate = request.LimiterRatePercent;
            return Ok();
        }

        // Members the dispatcher must never touch; throwing surfaces an accidental call.
        public ValueTask<CreateEmulatorSessionResponse> CreateSessionAsync(
            CreateEmulatorSessionRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException(nameof(CreateSessionAsync));

        public ValueTask<GetEmulatorStatusResponse> GetStatusAsync(
            SessionRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException(nameof(GetStatusAsync));

        public ValueTask<EmulatorCommandResponse> StartAsync(
            SessionRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException(nameof(StartAsync));

        public ValueTask<EmulatorCommandResponse> PauseAsync(
            SessionRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException(nameof(PauseAsync));

        public ValueTask<EmulatorCommandResponse> ResumeAsync(
            SessionRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException(nameof(ResumeAsync));

        public ValueTask<EmulatorCommandResponse> ResetAsync(
            SessionRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException(nameof(ResetAsync));

        public ValueTask<EmulatorCommandResponse> ResetAsync(
            ResetRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException(nameof(ResetAsync));

        public ValueTask<EmulatorCommandResponse> StepCycleAsync(
            StepCycleRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException(nameof(StepCycleAsync));

        public ValueTask<EmulatorCommandResponse> StepFrameAsync(
            StepFrameRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException(nameof(StepFrameAsync));

        public ValueTask<EmulatorCommandResponse> RewindCycleAsync(
            RewindCycleRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException(nameof(RewindCycleAsync));

        public ValueTask<EmulatorCommandResponse> RewindFrameAsync(
            RewindFrameRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException(nameof(RewindFrameAsync));

        public ValueTask<EmulatorCommandResponse> CloseSessionAsync(
            SessionRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException(nameof(CloseSessionAsync));
    }

    private sealed class RecordingSnapshotService : ISnapshotService
    {
        public List<string> Calls { get; } = new();

        public int CaptureCount { get; private set; }

        public int RestoreCount { get; private set; }

        public string? LastSession { get; private set; }

        public SnapshotDto? LastRestored { get; private set; }

        public SnapshotDto CaptureReturn { get; } = new("test-snapshot.v1", 42UL, new byte[] { 1, 2, 3, 4 });

        public int TotalCalls => Calls.Count;

        public ValueTask<CaptureSnapshotResponse> CaptureSnapshotAsync(
            SessionRequest request,
            CancellationToken cancellationToken = default)
        {
            Calls.Add(nameof(CaptureSnapshotAsync));
            CaptureCount++;
            LastSession = request.SessionId;
            return ValueTask.FromResult(new CaptureSnapshotResponse(RpcStatus.Ok(), CaptureReturn));
        }

        public ValueTask<RestoreSnapshotResponse> RestoreSnapshotAsync(
            RestoreSnapshotRequest request,
            CancellationToken cancellationToken = default)
        {
            Calls.Add(nameof(RestoreSnapshotAsync));
            RestoreCount++;
            LastRestored = request.Snapshot;
            LastSession = request.SessionId;
            return ValueTask.FromResult(new RestoreSnapshotResponse(RpcStatus.Ok(), null));
        }
    }

    private sealed class RecordingSettingsService : ISettingsService
    {
        public List<string> Calls { get; } = new();

        public int TotalCalls => Calls.Count;

        public ValueTask<GetSettingsResponse> GetSettingsAsync(
            SessionRequest request, CancellationToken cancellationToken = default)
        {
            Calls.Add(nameof(GetSettingsAsync));
            return ValueTask.FromResult(new GetSettingsResponse(RpcStatus.Ok(), null));
        }

        public ValueTask<UpdateSettingsResponse> UpdateSettingsAsync(
            UpdateSettingsRequest request, CancellationToken cancellationToken = default)
        {
            Calls.Add(nameof(UpdateSettingsAsync));
            return ValueTask.FromResult(new UpdateSettingsResponse(RpcStatus.Ok(), null, Array.Empty<SettingApplyDiagnosticDto>()));
        }

        public ValueTask<ListSettingsProfilesResponse> ListProfilesAsync(
            SessionRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException(nameof(ListProfilesAsync));

        public ValueTask<ValidateSettingsResourcesResponse> ValidateResourcesAsync(
            ValidateSettingsResourcesRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException(nameof(ValidateResourcesAsync));
    }
}
