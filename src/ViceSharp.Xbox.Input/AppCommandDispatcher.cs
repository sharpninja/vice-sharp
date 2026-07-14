namespace ViceSharp.Xbox.Input;

using System;
using System.Threading;
using System.Threading.Tasks;
using ViceSharp.Protocol;

/// <summary>
/// Translates the discrete <see cref="AppCommand"/>s emitted by the input-context
/// machine (<see cref="XboxInputContext"/>, S11) into the correct
/// <c>ViceSharp.Protocol</c> host-service calls, marshaling every state-mutating
/// command through the host's session-locked async methods.
/// </summary>
/// <remarks>
/// <para>
/// PLAN-XBOXUWP S13 (IMPL-XBOXUWP-013), TEST-SYSBTN-002. This is a deliberately
/// thin adapter: it owns no emulator state and performs no locking of its own. It
/// simply awaits the host services, each of which locks its session internally, so
/// invoking the dispatcher off the input-polling thread (which produced the command)
/// is itself the marshaling onto the emulation worker. State is NEVER mutated on the
/// input thread.
/// </para>
/// <para>
/// Command routing:
/// <list type="bullet">
///   <item><description>
///   <see cref="AppCommand.AutostartDrive8"/> -&gt;
///   <see cref="IEmulatorHost.ResetAndAutostartDrive8Async"/>.
///   </description></item>
///   <item><description>
///   <see cref="AppCommand.WarmReset"/> -&gt; <see cref="IEmulatorHost.WarmResetAsync"/>;
///   <see cref="AppCommand.ColdReset"/> -&gt; <see cref="IEmulatorHost.ColdResetAsync"/>.
///   </description></item>
///   <item><description>
///   <see cref="AppCommand.WarpHoldOn"/>/<see cref="AppCommand.WarpHoldOff"/> -&gt;
///   <see cref="IEmulatorHost.SetLimiterRateAsync"/> with
///   <see cref="WarpLimiterRatePercent"/> (0, the host's documented warp entry) and
///   <see cref="NormalLimiterRatePercent"/> (100) respectively. The stateless hold
///   pair is used, not <see cref="AppCommand.ToggleWarp"/> (which the Xbox input
///   pipeline never emits and the dispatcher treats as a no-op).
///   </description></item>
///   <item><description>
///   <see cref="AppCommand.QuickSaveState"/> -&gt;
///   <see cref="ISnapshotService.CaptureSnapshotAsync"/> (the captured snapshot is
///   held in an in-memory quick slot); <see cref="AppCommand.QuickLoadState"/> -&gt;
///   <see cref="ISnapshotService.RestoreSnapshotAsync"/> of that slot (a no-op when
///   nothing has been quick-saved yet).
///   </description></item>
///   <item><description>
///   <see cref="AppCommand.SwapJoystickPorts"/> -&gt; NO host or settings call; the
///   dispatcher reports <see cref="AppCommandDispatchResult.SwapPorts"/> so the caller
///   flips its local <see cref="XboxInputConfig.SwapPorts"/> flag.
///   </description></item>
///   <item><description>
///   <see cref="AppCommand.RequestExit"/> -&gt; the injected exit callback only
///   (the confirm-dialog gating that guarantees RequestExit only ever follows a
///   ConfirmYes lives in <see cref="XboxInputContext"/>, S11).
///   </description></item>
///   <item><description>
///   The UI-only commands (main menu, virtual keyboard, UI navigation, activate/back,
///   confirm yes/no, and <see cref="AppCommand.None"/>) make no host interaction; they
///   are handled by the UI layer.
///   </description></item>
/// </list>
/// </para>
/// <para>
/// The <see cref="ISettingsService"/> is part of the dispatcher's construction
/// contract but no S13 command routes through it: warp goes through the host limiter's
/// explicit warp entry and the joystick-port swap is a local config flip, both by
/// design. It is validated for non-null so a future settings-routed intent can be
/// added without changing the constructor shape.
/// </para>
/// </remarks>
public sealed class AppCommandDispatcher
{
    /// <summary>
    /// The limiter rate (percent) that enters warp (unthrottled) mode. The host
    /// documents rate 0 as its warp entry (it disables the limiter while preserving
    /// the prior rate).
    /// </summary>
    public const double WarpLimiterRatePercent = 0;

    /// <summary>The limiter rate (percent) that restores normal, real-time pacing.</summary>
    public const double NormalLimiterRatePercent = 100;

    private readonly IEmulatorHost _host;
    private readonly ISnapshotService _snapshots;
    private readonly Action _onExit;
    private readonly Action? _onOpenMenu;
    private readonly Action? _onCloseMenu;
    private readonly Action<AppCommand>? _onUiNavigate;

    private SnapshotDto? _quickSlot;

    /// <summary>
    /// Creates a dispatcher over the three Protocol host services and an exit callback.
    /// </summary>
    /// <param name="host">The emulator host (resets, autostart, warp limiter).</param>
    /// <param name="snapshots">The snapshot service (quick save/load).</param>
    /// <param name="settings">
    /// The settings service. Part of the S13 contract; no current command routes
    /// through it (see the type remarks), but it is validated for non-null.
    /// </param>
    /// <param name="onExit">The application-exit callback fired by RequestExit.</param>
    /// <param name="onOpenMenu">
    /// Optional UI callback fired by <see cref="AppCommand.OpenMainMenu"/> (the Menu button). The
    /// head uses it to reveal the shell menu over the always-running emulator. Null = no-op.
    /// </param>
    /// <param name="onCloseMenu">
    /// Optional UI callback fired by <see cref="AppCommand.CloseMenu"/> (dismiss the menu back to
    /// gameplay). The head uses it to hide the shell menu and expose the emulator. Null = no-op.
    /// </param>
    /// <param name="onUiNavigate">
    /// Optional UI callback fired by the shell-menu navigation commands
    /// (<see cref="AppCommand.UiNavigateUp"/>/<see cref="AppCommand.UiNavigateDown"/>/<see cref="AppCommand.UiNavigateLeft"/>/<see cref="AppCommand.UiNavigateRight"/>,
    /// <see cref="AppCommand.UiActivate"/>, and <see cref="AppCommand.UiBack"/>), receiving the exact
    /// command so the head can drive XAML focus navigation / activate the focused control / go back.
    /// Null = no-op (these commands remain UI-layer-only). Fires no host/snapshot/settings call.
    /// </param>
    public AppCommandDispatcher(
        IEmulatorHost host,
        ISnapshotService snapshots,
        ISettingsService settings,
        Action onExit,
        Action? onOpenMenu = null,
        Action? onCloseMenu = null,
        Action<AppCommand>? onUiNavigate = null)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(snapshots);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(onExit);

        _host = host;
        _snapshots = snapshots;
        _onExit = onExit;
        _onOpenMenu = onOpenMenu;
        _onCloseMenu = onCloseMenu;
        _onUiNavigate = onUiNavigate;
    }

    /// <summary>
    /// Dispatches one <see cref="AppCommand"/> against the given session, awaiting any
    /// host-service call it implies.
    /// </summary>
    /// <param name="sessionId">The target emulator session id.</param>
    /// <param name="command">The command to dispatch.</param>
    /// <param name="cancellationToken">Cancels a pending host-service call.</param>
    /// <returns>
    /// The local side effects for the caller to apply (currently only a joystick-port
    /// swap flip); <see cref="AppCommandDispatchResult.None"/> for every command whose
    /// entire effect is a host-service call, the exit callback, or nothing.
    /// </returns>
    public async ValueTask<AppCommandDispatchResult> DispatchAsync(
        string sessionId,
        AppCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(sessionId);

        switch (command)
        {
            case AppCommand.AutostartDrive8:
                await _host.ResetAndAutostartDrive8Async(
                    new ResetAndAutostartDrive8Request(sessionId),
                    cancellationToken).ConfigureAwait(false);
                break;

            case AppCommand.WarmReset:
                await _host.WarmResetAsync(
                    new SessionRequest(sessionId),
                    cancellationToken).ConfigureAwait(false);
                break;

            case AppCommand.ColdReset:
                await _host.ColdResetAsync(
                    new SessionRequest(sessionId),
                    cancellationToken).ConfigureAwait(false);
                break;

            case AppCommand.WarpHoldOn:
                await _host.SetLimiterRateAsync(
                    new SetLimiterRateRequest(sessionId, WarpLimiterRatePercent),
                    cancellationToken).ConfigureAwait(false);
                break;

            case AppCommand.WarpHoldOff:
                await _host.SetLimiterRateAsync(
                    new SetLimiterRateRequest(sessionId, NormalLimiterRatePercent),
                    cancellationToken).ConfigureAwait(false);
                break;

            case AppCommand.QuickSaveState:
                await QuickSaveAsync(sessionId, cancellationToken).ConfigureAwait(false);
                break;

            case AppCommand.QuickLoadState:
                await QuickLoadAsync(sessionId, cancellationToken).ConfigureAwait(false);
                break;

            case AppCommand.SwapJoystickPorts:
                // Local-only: no host or settings call - the caller flips its config.
                return AppCommandDispatchResult.SwapPorts;

            case AppCommand.RequestExit:
                _onExit();
                break;

            case AppCommand.OpenMainMenu:
                // UI-only: reveal the shell menu over the running emulator (head callback).
                _onOpenMenu?.Invoke();
                break;

            case AppCommand.CloseMenu:
                // UI-only: dismiss the menu back to gameplay (head callback).
                _onCloseMenu?.Invoke();
                break;

            // UI-only shell-menu navigation plus the virtual-keyboard commands
            // (FIX-XKBDINPUT-001: overlay toggle + the Y/X/LB/RB key chords): pass the
            // exact command to the head so it can drive XAML focus / activate / go back /
            // toggle the overlay / inject the chorded C64 key. Still no host call.
            case AppCommand.UiNavigateUp:
            case AppCommand.UiNavigateDown:
            case AppCommand.UiNavigateLeft:
            case AppCommand.UiNavigateRight:
            case AppCommand.UiActivate:
            case AppCommand.UiBack:
            case AppCommand.ToggleVirtualKeyboard:
            case AppCommand.KeyboardKeyDelete:
            case AppCommand.KeyboardKeySpace:
            case AppCommand.KeyboardKeyRunStop:
            case AppCommand.KeyboardKeyCursorLeft:
            case AppCommand.KeyboardKeyShiftCursorLeft:
            case AppCommand.KeyboardModifierCommodoreDown:
            case AppCommand.KeyboardModifierCommodoreUp:
            case AppCommand.KeyboardModifierShiftDown:
            case AppCommand.KeyboardModifierShiftUp:
                _onUiNavigate?.Invoke(command);
                break;

            // Neutral commands are handled by the UI layer, and ToggleWarp is never
            // emitted by the Xbox pipeline (the WarpHold pair is used instead): no host
            // interaction for any of these.
            case AppCommand.None:
            case AppCommand.ToggleWarp:
            case AppCommand.ConfirmYes:
            case AppCommand.ConfirmNo:
                break;
        }

        return AppCommandDispatchResult.None;
    }

    private async ValueTask QuickSaveAsync(string sessionId, CancellationToken cancellationToken)
    {
        CaptureSnapshotResponse response = await _snapshots
            .CaptureSnapshotAsync(new SessionRequest(sessionId), cancellationToken)
            .ConfigureAwait(false);

        if (response.Status.IsSuccess && response.Snapshot is not null)
        {
            _quickSlot = response.Snapshot;
        }
    }

    private async ValueTask QuickLoadAsync(string sessionId, CancellationToken cancellationToken)
    {
        SnapshotDto? slot = _quickSlot;
        if (slot is null)
        {
            // Nothing quick-saved yet: loading is a safe no-op.
            return;
        }

        await _snapshots
            .RestoreSnapshotAsync(new RestoreSnapshotRequest(sessionId, slot), cancellationToken)
            .ConfigureAwait(false);
    }
}
