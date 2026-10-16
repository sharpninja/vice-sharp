namespace ViceSharp.Xbox.Input;

/// <summary>
/// A gamepad input that MAY be bound to an <see cref="AppCommand"/> in a
/// <see cref="ButtonBinding"/>.
/// </summary>
/// <remarks>
/// Only the non-joystick SYSTEM inputs are bindable. The A and B face buttons are
/// deliberately absent: they are the LOCKED JOY2/JOY1 fire in the joystick mapper
/// (S9), and the D-pad and the two thumbsticks' deflection are joystick movement,
/// also locked. A thumbstick's <b>press</b> (click) is a system button and IS
/// bindable (<see cref="LeftThumbstick"/> / <see cref="RightThumbstick"/>); its
/// deflection is not. The analog triggers (<see cref="LeftTrigger"/> /
/// <see cref="RightTrigger"/>) are bindable as hold-with-hysteresis inputs.
/// </remarks>
public enum BindableInput
{
    /// <summary>The Menu ("start") button.</summary>
    Menu,

    /// <summary>The View ("back") button.</summary>
    View,

    /// <summary>The X face button.</summary>
    X,

    /// <summary>The Y face button.</summary>
    Y,

    /// <summary>The left shoulder ("bumper") button.</summary>
    LeftShoulder,

    /// <summary>The right shoulder ("bumper") button.</summary>
    RightShoulder,

    /// <summary>The left analog trigger (bound as a hold-with-hysteresis input).</summary>
    LeftTrigger,

    /// <summary>The right analog trigger (bound as a hold-with-hysteresis input).</summary>
    RightTrigger,

    /// <summary>The left thumbstick pressed as a button (the click, not the deflection).</summary>
    LeftThumbstick,

    /// <summary>The right thumbstick pressed as a button (the click, not the deflection).</summary>
    RightThumbstick,
}
