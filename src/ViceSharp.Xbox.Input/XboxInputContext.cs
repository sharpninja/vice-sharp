namespace ViceSharp.Xbox.Input;

using System.Collections.Generic;

/// <summary>
/// The SINGLE-consumer input state machine: the one and only reader of the whole
/// <see cref="GamepadSnapshot"/>. It unifies the joystick mapper
/// (<see cref="XboxJoystickMapper"/>) and the system-button evaluator
/// (<see cref="XboxSystemButtons"/>), gates them by <see cref="InputContext"/>, and
/// produces UI-navigation intents in menus.
/// </summary>
/// <remarks>
/// <para>
/// PLAN-XBOXUWP S11 (IMPL-XBOXUWP-011), FR-CTX-001..004, FR-SYSBTN-002/003/007,
/// FR-GAMEPAD-009, TR-CTX-001. There is NO separate app-button path and NO
/// context-unaware pump: <see cref="Tick"/> is called exactly once per frame and is
/// the only code that reads the snapshot, driving BOTH the joystick mapping and the
/// system-button edges.
/// </para>
/// <para>
/// Determinism: <see cref="Tick"/> reads no wall-clock and no random source. All
/// cross-frame state (context, mapper hysteresis latch, trigger latch, prior
/// snapshot, the directional repeater, the prior frame index, and the pending
/// confirm command) lives in INSTANCE fields; the pure helpers it calls hold no
/// static mutable state. Elapsed time for the directional auto-repeat is derived
/// from the injected frame index as
/// <c>(frameIndex - priorFrameIndex) * <see cref="FrameDurationMs"/></c>, so the
/// repeat schedule is testable by driving the frame index.
/// </para>
/// <para>
/// Behavior by context:
/// <list type="bullet">
///   <item><description>
///   <b>Gameplay:</b> the mapper drives Joy1/Joy2 and the evaluator's gameplay
///   bindings emit commands. A Menu edge (OpenMainMenu) moves to
///   <see cref="InputContext.MainMenu"/>; a View edge (ToggleVirtualKeyboard) moves
///   to <see cref="InputContext.VirtualKeyboard"/>; a Y edge is CONFIRM-GATED and
///   moves to <see cref="InputContext.ConfirmDialog"/> WITHOUT emitting WarmReset
///   (the reset is deferred to ConfirmYes). All other gameplay commands pass
///   through and stay in Gameplay. On a Gameplay-&gt;non-Gameplay transition this
///   frame, both ports are forced to <see cref="JoystickPortState.Neutral"/>
///   (FR-CTX-004 one-shot neutral push).
///   </description></item>
///   <item><description>
///   <b>Non-Gameplay:</b> both ports are ALWAYS neutral and A does not fire
///   (FR-CTX-002). D-pad/left-stick drive UI navigation via
///   <see cref="DirectionalRepeater"/>; A =&gt; UiActivate, B =&gt; UiBack, Menu
///   =&gt; CloseMenu (back to Gameplay). In VirtualKeyboard, View toggles back to
///   Gameplay. In ConfirmDialog, A =&gt; ConfirmYes (plus the deferred command, e.g.
///   WarmReset) and B =&gt; ConfirmNo, both returning to Gameplay.
///   </description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class XboxInputContext
{
    /// <summary>
    /// Nominal frame duration (ms) used to convert a frame-index delta into elapsed
    /// time for the directional auto-repeat. 20.0 ms == ~50 Hz (PAL frame cadence).
    /// </summary>
    public const double FrameDurationMs = 20.0;

    private static readonly GamepadButtonFlags DirectionFlags =
        GamepadButtonFlags.DPadUp | GamepadButtonFlags.DPadDown |
        GamepadButtonFlags.DPadLeft | GamepadButtonFlags.DPadRight;

    private readonly XboxInputConfig _config;
    private readonly BindingProfile _profile;
    private readonly DirectionalRepeater _repeater;
    private readonly List<AppCommand> _evalBuffer;

    private InputContext _context;
    private GamepadSnapshot _priorSnapshot;
    private MapperState _mapperState;
    private SystemButtonLatch _latch;
    private long _priorFrameIndex;
    private bool _hasPriorFrame;
    private AppCommand _pendingConfirmCommand;

    // FIX-XKBDINPUT-001 trigger modifiers (operator: LT = C=, RT = SHIFT while the
    // virtual keyboard is open). Held phases latched across frames with the shared
    // XboxSystemButtons trigger hysteresis; any exit from the keyboard context emits
    // the paired releases so a modifier can never stick inside the machine.
    private bool _keyboardCommodoreHeld;
    private bool _keyboardShiftHeld;

    /// <summary>
    /// Creates a new input context machine.
    /// </summary>
    /// <param name="config">
    /// The joystick tuning/swap config. Defaults to <see cref="XboxInputConfig.Default"/>.
    /// </param>
    /// <param name="profile">
    /// The gameplay system-button binding profile. Defaults to
    /// <see cref="BindingProfile.Default"/>.
    /// </param>
    /// <param name="initialContext">
    /// The starting context. Defaults to <see cref="InputContext.Gameplay"/>.
    /// </param>
    public XboxInputContext(
        XboxInputConfig? config = null,
        BindingProfile? profile = null,
        InputContext initialContext = InputContext.Gameplay)
    {
        _config = config ?? XboxInputConfig.Default;
        _profile = profile ?? BindingProfile.Default;
        _repeater = new DirectionalRepeater();
        _evalBuffer = new List<AppCommand>();
        _context = initialContext;
        _priorSnapshot = GamepadSnapshot.Neutral;
        _mapperState = MapperState.Initial;
        _latch = SystemButtonLatch.Initial;
        _priorFrameIndex = 0;
        _hasPriorFrame = false;
        _pendingConfirmCommand = AppCommand.None;
    }

    /// <summary>The context the NEXT <see cref="Tick"/> will be evaluated in.</summary>
    public InputContext Context => _context;

    /// <summary>
    /// Sets the context the NEXT <see cref="Tick"/> will be evaluated in, WITHOUT
    /// consuming a gamepad snapshot. This is the UI-driven context path: when a page
    /// is navigated to or an overlay opens/closes, the shell (via the thin
    /// ViewModels observer) requests the matching context here, distinct from the
    /// gamepad Menu/View/Y button edges that <see cref="Tick"/> resolves. The machine
    /// remains the SINGLE context authority (FR-XBOXUI-003): both paths write the one
    /// <see cref="Context"/> field, so exactly one context is ever authoritative.
    /// </summary>
    /// <param name="context">The context to switch to for the next frame.</param>
    /// <remarks>
    /// PLAN-XBOXUWP S20 (IMPL-XBOXUWP-020), FR-XBOXUI-003 / TR-XBOXUI-003. When the
    /// requested context is not <see cref="InputContext.ConfirmDialog"/>, any pending
    /// confirm-gated command is dropped so a UI-driven context change cannot leave a
    /// stale deferred action armed. Reads no wall-clock and no random source, so
    /// determinism is preserved.
    /// </remarks>
    public void RequestContext(InputContext context)
    {
        _context = context;

        if (context != InputContext.ConfirmDialog)
        {
            _pendingConfirmCommand = AppCommand.None;
        }

        if (context != InputContext.VirtualKeyboard)
        {
            // UI-driven exit from the keyboard: no Tick runs to emit the paired releases,
            // so clear the trigger-modifier phases (the head's overlay-hide path releases
            // the actual machine keys) and let re-entry start clean.
            _keyboardCommodoreHeld = false;
            _keyboardShiftHeld = false;
        }
    }

    /// <summary>
    /// Consumes one gamepad snapshot for one frame and resolves it into commands and
    /// the two joystick ports, applying the context gating.
    /// </summary>
    /// <param name="frameIndex">
    /// The monotonically increasing frame index. The delta from the previous call
    /// (times <see cref="FrameDurationMs"/>) is the elapsed time fed to the
    /// directional auto-repeat; it is the only source of "time" and carries no
    /// wall-clock.
    /// </param>
    /// <param name="snapshot">The gamepad reading for this frame.</param>
    /// <returns>
    /// The <see cref="InputResolution"/> for this frame: the next context, the emitted
    /// commands (a fresh list), and the two resolved ports.
    /// </returns>
    public InputResolution Tick(long frameIndex, in GamepadSnapshot snapshot)
    {
        double elapsedMs = 0.0;
        if (_hasPriorFrame)
        {
            long delta = frameIndex - _priorFrameIndex;
            if (delta > 0)
            {
                elapsedMs = delta * FrameDurationMs;
            }
        }

        InputContext current = _context;
        InputResolution resolution = current == InputContext.Gameplay
            ? ResolveGameplay(in snapshot)
            : ResolveMenu(current, in snapshot, elapsedMs);

        // Commit cross-frame state.
        _context = resolution.NextContext;
        _priorSnapshot = snapshot;
        _priorFrameIndex = frameIndex;
        _hasPriorFrame = true;

        return resolution;
    }

    /// <summary>
    /// Resolves a Gameplay-context frame: joystick mapping plus the gameplay
    /// system-button bindings, with the context transitions and the one-shot neutral
    /// push applied.
    /// </summary>
    private InputResolution ResolveGameplay(in GamepadSnapshot snapshot)
    {
        // No auto-repeat while in Gameplay; re-arm cleanly on the next menu entry.
        _repeater.Reset();

        // Joystick ports (the mapper is the sole joystick authority in Gameplay).
        (JoystickPortState joy1, JoystickPortState joy2, MapperState nextMapper) =
            XboxJoystickMapper.Map(in snapshot, in _config, in _mapperState);
        _mapperState = nextMapper;

        // System-button edges via the S10 evaluator (reuse the scratch buffer).
        _evalBuffer.Clear();
        _latch = XboxSystemButtons.Evaluate(in _priorSnapshot, in snapshot, _profile, in _config, in _latch, _evalBuffer);

        var commands = new List<AppCommand>(_evalBuffer.Count);
        bool wantsConfirmReset = false;
        for (int i = 0; i < _evalBuffer.Count; i++)
        {
            AppCommand command = _evalBuffer[i];
            if (command == AppCommand.WarmReset)
            {
                // Confirm-gated: never emitted directly in Gameplay. Defer to ConfirmYes.
                wantsConfirmReset = true;
                continue;
            }

            commands.Add(command);
        }

        // Context transition (precedence: main menu > virtual keyboard > confirm-reset).
        InputContext next = InputContext.Gameplay;
        if (commands.Contains(AppCommand.OpenMainMenu))
        {
            next = InputContext.MainMenu;
        }
        else if (commands.Contains(AppCommand.ToggleVirtualKeyboard))
        {
            next = InputContext.VirtualKeyboard;
        }
        else if (wantsConfirmReset)
        {
            next = InputContext.ConfirmDialog;
            _pendingConfirmCommand = AppCommand.WarmReset;
        }

        // FR-CTX-004: one-shot neutral push on the Gameplay -> non-Gameplay edge so
        // the C64 immediately releases any held direction/fire.
        if (next != InputContext.Gameplay)
        {
            joy1 = JoystickPortState.Neutral;
            joy2 = JoystickPortState.Neutral;
        }

        return new InputResolution(next, commands, joy1, joy2);
    }

    /// <summary>
    /// Resolves a non-Gameplay frame: ports are forced neutral (FR-CTX-002), the
    /// D-pad/left-stick drive UI navigation, and the face/system buttons drive
    /// activate/back/close/confirm.
    /// </summary>
    private InputResolution ResolveMenu(InputContext current, in GamepadSnapshot snapshot, double elapsedMs)
    {
        var commands = new List<AppCommand>();

        // UI navigation from the D-pad merged with the digitized left stick.
        GamepadButtonFlags heldDirections = ComputeHeldDirections(in snapshot);
        AppCommand? nav = _repeater.Tick(heldDirections, elapsedMs);
        if (nav.HasValue && nav.Value != AppCommand.None)
        {
            commands.Add(nav.Value);
        }

        bool menuEdge = DownEdge(GamepadButtonFlags.Menu, in snapshot);
        bool viewEdge = DownEdge(GamepadButtonFlags.View, in snapshot);
        bool aEdge = DownEdge(GamepadButtonFlags.A, in snapshot);
        bool bEdge = DownEdge(GamepadButtonFlags.B, in snapshot);

        InputContext next = current;

        if (current == InputContext.ConfirmDialog)
        {
            if (aEdge)
            {
                commands.Add(AppCommand.ConfirmYes);
                if (_pendingConfirmCommand != AppCommand.None)
                {
                    commands.Add(_pendingConfirmCommand);
                }

                _pendingConfirmCommand = AppCommand.None;
                next = InputContext.Gameplay;
            }
            else if (bEdge)
            {
                commands.Add(AppCommand.ConfirmNo);
                _pendingConfirmCommand = AppCommand.None;
                next = InputContext.Gameplay;
            }
            else if (menuEdge)
            {
                // Menu also cancels the dialog (no reset performed).
                commands.Add(AppCommand.CloseMenu);
                _pendingConfirmCommand = AppCommand.None;
                next = InputContext.Gameplay;
            }
        }
        else if (current == InputContext.VirtualKeyboard)
        {
            // FIX-XKBDINPUT-001 (operator mapping, remapped 2026-07-14): while the
            // on-screen keyboard is open, the D-pad navigates the tiles (repeater above),
            // A activates the FOCUSED tile (that is how RETURN and every letter is
            // pressed), the chords are X=INST/DEL, Y=SPACE, B=RUN/STOP, LB=cursor-left,
            // RB=SHIFT+cursor-left, and the triggers are HELD modifiers (LT=C=, RT=SHIFT,
            // shared XboxSystemButtons hysteresis). View toggles the keyboard off; Menu
            // closes back to gameplay; both exits release any held modifier.
            if (menuEdge || viewEdge)
            {
                ReleaseKeyboardModifiers(commands);
                commands.Add(menuEdge ? AppCommand.CloseMenu : AppCommand.ToggleVirtualKeyboard);
                next = InputContext.Gameplay;
            }
            else
            {
                UpdateKeyboardModifier(
                    snapshot.LeftTrigger,
                    ref _keyboardCommodoreHeld,
                    AppCommand.KeyboardModifierCommodoreDown,
                    AppCommand.KeyboardModifierCommodoreUp,
                    commands);
                UpdateKeyboardModifier(
                    snapshot.RightTrigger,
                    ref _keyboardShiftHeld,
                    AppCommand.KeyboardModifierShiftDown,
                    AppCommand.KeyboardModifierShiftUp,
                    commands);

                if (aEdge)
                {
                    commands.Add(AppCommand.UiActivate);
                }

                if (DownEdge(GamepadButtonFlags.X, in snapshot))
                {
                    commands.Add(AppCommand.KeyboardKeyDelete);
                }

                if (DownEdge(GamepadButtonFlags.Y, in snapshot))
                {
                    commands.Add(AppCommand.KeyboardKeySpace);
                }

                if (bEdge)
                {
                    commands.Add(AppCommand.KeyboardKeyRunStop);
                }

                if (DownEdge(GamepadButtonFlags.LeftShoulder, in snapshot))
                {
                    commands.Add(AppCommand.KeyboardKeyCursorLeft);
                }

                if (DownEdge(GamepadButtonFlags.RightShoulder, in snapshot))
                {
                    commands.Add(AppCommand.KeyboardKeyShiftCursorLeft);
                }
            }
        }
        else
        {
            // MainMenu.
            if (menuEdge)
            {
                commands.Add(AppCommand.CloseMenu);
                next = InputContext.Gameplay;
            }
            else
            {
                if (aEdge)
                {
                    commands.Add(AppCommand.UiActivate);
                }

                if (bEdge)
                {
                    commands.Add(AppCommand.UiBack);
                }
            }
        }

        // FR-CTX-002: joystick is inert (and A never fires) in every non-Gameplay context.
        return new InputResolution(next, commands, JoystickPortState.Neutral, JoystickPortState.Neutral);
    }

    /// <summary>
    /// Advances one trigger-held modifier phase with the shared
    /// <see cref="XboxSystemButtons"/> hysteresis and emits the paired down/up command
    /// exactly on the phase edges (silent while held or released, including inside the
    /// hysteresis band).
    /// </summary>
    private static void UpdateKeyboardModifier(
        double triggerValue,
        ref bool held,
        AppCommand downCommand,
        AppCommand upCommand,
        List<AppCommand> commands)
    {
        bool heldNow = triggerValue >= XboxSystemButtons.TriggerActivate
            ? true
            : triggerValue <= XboxSystemButtons.TriggerRelease
                ? false
                : held;

        if (heldNow == held)
        {
            return;
        }

        commands.Add(heldNow ? downCommand : upCommand);
        held = heldNow;
    }

    /// <summary>
    /// Emits the paired release for every held trigger modifier and clears the phases
    /// (used on every exit from the VirtualKeyboard context so a modifier never sticks).
    /// </summary>
    private void ReleaseKeyboardModifiers(List<AppCommand> commands)
    {
        if (_keyboardCommodoreHeld)
        {
            commands.Add(AppCommand.KeyboardModifierCommodoreUp);
            _keyboardCommodoreHeld = false;
        }

        if (_keyboardShiftHeld)
        {
            commands.Add(AppCommand.KeyboardModifierShiftUp);
            _keyboardShiftHeld = false;
        }
    }

    /// <summary>
    /// Builds the held-direction flag set fed to the repeater: the raw D-pad bits
    /// OR-merged with the left thumbstick digitized through the shared
    /// <see cref="StickConverter"/> (no hysteresis carry: prior mask 0).
    /// </summary>
    private GamepadButtonFlags ComputeHeldDirections(in GamepadSnapshot snapshot)
    {
        GamepadButtonFlags directions = snapshot.Buttons & DirectionFlags;

        byte stickMask = StickConverter.ToDirectionMask(snapshot.LeftStickX, snapshot.LeftStickY, 0, in _config);
        if ((stickMask & JoystickPortState.Up) != 0)
        {
            directions |= GamepadButtonFlags.DPadUp;
        }

        if ((stickMask & JoystickPortState.Down) != 0)
        {
            directions |= GamepadButtonFlags.DPadDown;
        }

        if ((stickMask & JoystickPortState.Left) != 0)
        {
            directions |= GamepadButtonFlags.DPadLeft;
        }

        if ((stickMask & JoystickPortState.Right) != 0)
        {
            directions |= GamepadButtonFlags.DPadRight;
        }

        return directions;
    }

    /// <summary>
    /// Returns true when <paramref name="flag"/> transitions from not-set (prior
    /// frame) to set (this frame).
    /// </summary>
    private bool DownEdge(GamepadButtonFlags flag, in GamepadSnapshot current)
    {
        bool priorSet = (_priorSnapshot.Buttons & flag) != 0;
        bool currentSet = (current.Buttons & flag) != 0;
        return !priorSet && currentSet;
    }
}
