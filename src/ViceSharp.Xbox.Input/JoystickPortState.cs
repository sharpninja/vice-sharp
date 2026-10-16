namespace ViceSharp.Xbox.Input;

/// <summary>
/// The resolved state of one C64 joystick port: a 4-bit direction mask plus a
/// separate fire flag. The mapper (S8/S9) produces this from a
/// <see cref="GamepadSnapshot"/>; the host applies it to a runtime port.
/// </summary>
/// <remarks>
/// The direction bits (<see cref="Up"/>/<see cref="Down"/>/<see cref="Left"/>/
/// <see cref="Right"/>) match <c>C64JoystickPort.JoystickButtons</c>. Fire is a
/// separate <see cref="bool"/> and is <b>never</b> a mask bit: the C64 core ORs
/// its internal Fire (0x10) itself, so the mask carries direction only.
/// </remarks>
/// <param name="DirectionMask">
/// Bitwise-OR of the direction bits (<see cref="Up"/>, <see cref="Down"/>,
/// <see cref="Left"/>, <see cref="Right"/>); 0 = centered.
/// </param>
/// <param name="Fire">The fire button, tracked separately from the direction mask.</param>
public readonly record struct JoystickPortState(byte DirectionMask, bool Fire)
{
    /// <summary>Direction bit: up. Matches <c>C64JoystickPort.JoystickButtons.Up</c>.</summary>
    public const byte Up = 0x01;

    /// <summary>Direction bit: down. Matches <c>C64JoystickPort.JoystickButtons.Down</c>.</summary>
    public const byte Down = 0x02;

    /// <summary>Direction bit: left. Matches <c>C64JoystickPort.JoystickButtons.Left</c>.</summary>
    public const byte Left = 0x04;

    /// <summary>Direction bit: right. Matches <c>C64JoystickPort.JoystickButtons.Right</c>.</summary>
    public const byte Right = 0x08;

    /// <summary>
    /// The neutral port: no direction, no fire. Equal to <c>default</c>; used as
    /// the fail-safe centered state (disconnect / no-pad / non-Gameplay context).
    /// </summary>
    public static JoystickPortState Neutral => default;
}
