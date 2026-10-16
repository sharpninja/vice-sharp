namespace ViceSharp.Xbox.Input;

using System.Collections.Generic;

/// <summary>
/// A named set of gameplay <see cref="ButtonBinding"/>s: the system-button control
/// scheme the evaluator (S10) applies while in the Gameplay context.
/// </summary>
/// <param name="Id">Stable identifier used for persistence.</param>
/// <param name="DisplayName">Human-readable name shown in the UI.</param>
/// <param name="Gameplay">The ordered binding rows applied in the Gameplay context.</param>
public sealed record BindingProfile(
    string Id,
    string DisplayName,
    IReadOnlyList<ButtonBinding> Gameplay)
{
    /// <summary>
    /// The LOCKED default gameplay binding set. This table is a stable by-value
    /// contract (the S10 guard asserts it row by row):
    /// <list type="bullet">
    ///   <item><description>Menu -&gt; OpenMainMenu (Toggle)</description></item>
    ///   <item><description>View -&gt; ToggleVirtualKeyboard (Toggle)</description></item>
    ///   <item><description>X -&gt; AutostartDrive8 (Press)</description></item>
    ///   <item><description>Y -&gt; WarmReset (Press)</description></item>
    ///   <item><description>LeftShoulder -&gt; QuickSaveState (Press)</description></item>
    ///   <item><description>RightShoulder -&gt; QuickLoadState (Press)</description></item>
    ///   <item><description>LeftTrigger -&gt; WarpHoldOn (Hold; warp while held)</description></item>
    ///   <item><description>LeftThumbstick -&gt; SwapJoystickPorts (Toggle)</description></item>
    /// </list>
    /// RightTrigger and RightThumbstick are intentionally UNBOUND.
    /// </summary>
    public static BindingProfile Default => new(
        "default",
        "Default",
        new ButtonBinding[]
        {
            new(BindableInput.Menu, AppCommand.OpenMainMenu, BindingActivation.Toggle),
            new(BindableInput.View, AppCommand.ToggleVirtualKeyboard, BindingActivation.Toggle),
            new(BindableInput.X, AppCommand.AutostartDrive8, BindingActivation.Press),
            new(BindableInput.Y, AppCommand.WarmReset, BindingActivation.Press),
            new(BindableInput.LeftShoulder, AppCommand.QuickSaveState, BindingActivation.Press),
            new(BindableInput.RightShoulder, AppCommand.QuickLoadState, BindingActivation.Press),
            new(BindableInput.LeftTrigger, AppCommand.WarpHoldOn, BindingActivation.Hold),
            new(BindableInput.LeftThumbstick, AppCommand.SwapJoystickPorts, BindingActivation.Toggle),
        });
}
