namespace ViceSharp.Xbox.Input;

/// <summary>
/// The UI-navigation auto-repeat helper: turns a held direction into a stream of
/// <see cref="AppCommand.UiNavigateUp"/>/<c>Down</c>/<c>Left</c>/<c>Right</c>
/// intents with a keyboard-style "typematic" schedule (one on press, an initial
/// delay, then a steady repeat).
/// </summary>
/// <remarks>
/// <para>
/// PLAN-XBOXUWP S11 (IMPL-XBOXUWP-011), FR-CTX-003. Held state lives in INSTANCE
/// fields (the class is allowed instance state), so one repeater serves one
/// <see cref="XboxInputContext"/>. It holds NO static mutable state and reads NO
/// wall-clock: the elapsed time is INJECTED (<c>elapsedMs</c>) so the schedule is
/// fully deterministic and replayable.
/// </para>
/// <para>
/// Direction selection: the caller passes the held directions as D-pad flags (the
/// <see cref="XboxInputContext"/> merges the D-pad with the left thumbstick digitized
/// through <see cref="StickConverter"/>). When more than one direction bit is set,
/// a single dominant direction is chosen by the fixed priority
/// Up &gt; Down &gt; Left &gt; Right. A change of dominant direction (including
/// press from none) counts as a fresh press and re-arms the schedule.
/// </para>
/// </remarks>
public sealed class DirectionalRepeater
{
    /// <summary>Delay (ms) after the initial press before the first auto-repeat.</summary>
    public const double InitialDelayMs = 400.0;

    /// <summary>Interval (ms) between successive auto-repeats after the initial delay.</summary>
    public const double RepeatIntervalMs = 90.0;

    private const GamepadButtonFlags DirectionMask =
        GamepadButtonFlags.DPadUp | GamepadButtonFlags.DPadDown |
        GamepadButtonFlags.DPadLeft | GamepadButtonFlags.DPadRight;

    private GamepadButtonFlags _activeDirection;
    private double _heldMs;
    private int _repeatsEmitted;

    /// <summary>
    /// Advances the repeater one frame and returns the navigation command to emit this
    /// frame, or <see langword="null"/> for none.
    /// </summary>
    /// <param name="heldDirections">
    /// The directions currently held, as D-pad flags. Non-direction bits are ignored;
    /// no direction bits set means "released".
    /// </param>
    /// <param name="elapsedMs">
    /// Milliseconds elapsed since the previous call. Injected by the caller (never read
    /// from a clock). Ignored on a fresh press (the schedule restarts at zero).
    /// </param>
    /// <returns>
    /// The <see cref="AppCommand.UiNavigateUp"/>/<c>Down</c>/<c>Left</c>/<c>Right</c>
    /// for the active direction on a fresh press or a scheduled repeat; otherwise
    /// <see langword="null"/>.
    /// </returns>
    public AppCommand? Tick(GamepadButtonFlags heldDirections, double elapsedMs)
    {
        GamepadButtonFlags direction = PickDirection(heldDirections);

        if (direction == GamepadButtonFlags.None)
        {
            Reset();
            return null;
        }

        if (direction != _activeDirection)
        {
            // Fresh press (or a change of dominant direction): emit once, re-arm.
            _activeDirection = direction;
            _heldMs = 0.0;
            _repeatsEmitted = 0;
            return ToNavCommand(direction);
        }

        // Same direction still held: accumulate and honor the repeat schedule. At most
        // one repeat is emitted per Tick; a large elapsed only advances the phase.
        if (elapsedMs > 0.0)
        {
            _heldMs += elapsedMs;
        }

        int scheduledRepeats = _heldMs >= InitialDelayMs
            ? 1 + (int)((_heldMs - InitialDelayMs) / RepeatIntervalMs)
            : 0;

        if (scheduledRepeats > _repeatsEmitted)
        {
            _repeatsEmitted++;
            return ToNavCommand(direction);
        }

        return null;
    }

    /// <summary>
    /// Clears the held state so the next press is treated as fresh. Called on release
    /// and whenever the owning context leaves a menu (returns to Gameplay).
    /// </summary>
    public void Reset()
    {
        _activeDirection = GamepadButtonFlags.None;
        _heldMs = 0.0;
        _repeatsEmitted = 0;
    }

    /// <summary>
    /// Reduces a (possibly multi-bit) direction flag set to the single dominant
    /// direction by the fixed priority Up &gt; Down &gt; Left &gt; Right, or
    /// <see cref="GamepadButtonFlags.None"/> when no direction is held.
    /// </summary>
    private static GamepadButtonFlags PickDirection(GamepadButtonFlags held)
    {
        GamepadButtonFlags directions = held & DirectionMask;

        if ((directions & GamepadButtonFlags.DPadUp) != 0)
        {
            return GamepadButtonFlags.DPadUp;
        }

        if ((directions & GamepadButtonFlags.DPadDown) != 0)
        {
            return GamepadButtonFlags.DPadDown;
        }

        if ((directions & GamepadButtonFlags.DPadLeft) != 0)
        {
            return GamepadButtonFlags.DPadLeft;
        }

        if ((directions & GamepadButtonFlags.DPadRight) != 0)
        {
            return GamepadButtonFlags.DPadRight;
        }

        return GamepadButtonFlags.None;
    }

    /// <summary>Maps a single direction flag to its UI-navigation command.</summary>
    private static AppCommand ToNavCommand(GamepadButtonFlags direction) => direction switch
    {
        GamepadButtonFlags.DPadUp => AppCommand.UiNavigateUp,
        GamepadButtonFlags.DPadDown => AppCommand.UiNavigateDown,
        GamepadButtonFlags.DPadLeft => AppCommand.UiNavigateLeft,
        GamepadButtonFlags.DPadRight => AppCommand.UiNavigateRight,
        _ => AppCommand.None,
    };
}
