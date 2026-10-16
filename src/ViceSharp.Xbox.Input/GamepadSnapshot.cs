namespace ViceSharp.Xbox.Input;

/// <summary>
/// An immutable, allocation-free reading of a single gamepad at one instant.
/// Mirrors <c>Windows.Gaming.Input.GamepadReading</c> so the UWP adapter can map
/// a device reading field-for-field, but carries no WinRT dependency itself.
/// </summary>
/// <param name="LeftStickX">Left thumbstick horizontal position, -1.0 (left) to 1.0 (right).</param>
/// <param name="LeftStickY">Left thumbstick vertical position, -1.0 (down) to 1.0 (up).</param>
/// <param name="RightStickX">Right thumbstick horizontal position, -1.0 (left) to 1.0 (right).</param>
/// <param name="RightStickY">Right thumbstick vertical position, -1.0 (down) to 1.0 (up).</param>
/// <param name="LeftTrigger">Left trigger travel, 0.0 (released) to 1.0 (fully pressed).</param>
/// <param name="RightTrigger">Right trigger travel, 0.0 (released) to 1.0 (fully pressed).</param>
/// <param name="Buttons">The set of digital buttons currently held.</param>
/// <param name="Timestamp">Device timestamp of the reading (monotonic, source-defined units).</param>
public readonly record struct GamepadSnapshot(
    double LeftStickX,
    double LeftStickY,
    double RightStickX,
    double RightStickY,
    double LeftTrigger,
    double RightTrigger,
    GamepadButtonFlags Buttons,
    ulong Timestamp)
{
    /// <summary>
    /// The neutral reading: every axis 0.0, no buttons, timestamp 0. Equal to
    /// <c>default</c>; used as the fail-safe / no-pad snapshot.
    /// </summary>
    public static GamepadSnapshot Neutral => default;
}
