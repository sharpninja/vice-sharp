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
        else
        {
            // MainMenu or VirtualKeyboard.
            if (menuEdge)
            {
                commands.Add(AppCommand.CloseMenu);
                next = InputContext.Gameplay;
            }
            else if (viewEdge && current == InputContext.VirtualKeyboard)
            {
                // View toggles the virtual keyboard back off (Gameplay <-> VirtualKeyboard).
                commands.Add(AppCommand.ToggleVirtualKeyboard);
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
