namespace ViceSharp.Xbox.ViewModels;

using System;
using System.Collections.Generic;
using ViceSharp.Xbox.Input;

/// <summary>
/// PLAN-XBOXUWP S30 (IMPL-XBOXUWP-030), area XBOXUI. The read-only input-mapping page
/// ViewModel: a bindable, ordered display of the LOCKED controller mapping so the player
/// can see how the gamepad drives the C64.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="Rows"/> list is built once at construction and never changes; the page
/// is display-only (there is no remap here - remap persistence is owned by S12 / S26).
/// It combines two locked sources:
/// </para>
/// <list type="bullet">
///   <item><description>The S9 joystick bundle (never rebindable): the left stick + the
///   D-pad and the A button drive JOY2 (JOY2 fire); the right stick and the B button
///   drive JOY1 (JOY1 fire); the Guide button is reserved by the platform.</description></item>
///   <item><description>The S10 default system buttons, taken row-for-row from
///   <see cref="BindingProfile.Default"/> (Menu, View, X, Y, LB, RB, LT, L3) with each
///   input and its bound action shown as human-readable labels.</description></item>
/// </list>
/// <para>
/// From this page the player can jump to the on-screen virtual keyboard overlay via the
/// <see cref="RequestOpenVirtualKeyboard"/> intent. Pure MVVM (TR-MVVM-001): it consumes
/// only the portable <c>ViceSharp.Xbox.Input</c> binding types and holds no engine, host,
/// or XAML reference.
/// </para>
/// </remarks>
public sealed class InputMappingViewModel
{
    private readonly List<InputMappingRow> _rows;

    /// <summary>
    /// Creates the input-mapping ViewModel, building the fixed row list from the locked
    /// joystick scheme and <see cref="BindingProfile.Default"/>.
    /// </summary>
    public InputMappingViewModel()
    {
        _rows = BuildRows(BindingProfile.Default);
    }

    /// <summary>
    /// Raised by <see cref="RequestOpenVirtualKeyboard"/>: the shell should open the
    /// on-screen virtual-keyboard overlay. Carries no payload.
    /// </summary>
    public event EventHandler? OpenVirtualKeyboardRequested;

    /// <summary>
    /// The ordered, read-only mapping rows: the locked joystick bundle, the reserved
    /// Guide button, then the default system-button bindings. Fixed for the lifetime of
    /// the instance (stable across reads).
    /// </summary>
    public IReadOnlyList<InputMappingRow> Rows => _rows;

    /// <summary>Raises the <see cref="OpenVirtualKeyboardRequested"/> intent.</summary>
    public void RequestOpenVirtualKeyboard() =>
        OpenVirtualKeyboardRequested?.Invoke(this, EventArgs.Empty);

    private static List<InputMappingRow> BuildRows(BindingProfile profile)
    {
        var rows = new List<InputMappingRow>
        {
            // Locked S9 joystick bundle (not rebindable).
            new("Left stick + D-pad", "JOY2 (move)"),
            new("Right stick", "JOY1 (move)"),
            new("A", "JOY2 fire"),
            new("B", "JOY1 fire"),
            new("Guide", "Reserved (system button)"),
        };

        // Default S10 system buttons, in the locked BindingProfile.Default order.
        foreach (ButtonBinding binding in profile.Gameplay)
        {
            rows.Add(new InputMappingRow(InputLabel(binding.Input), ActionLabel(binding.Command)));
        }

        return rows;
    }

    /// <summary>Human-readable controller label for a bindable system input.</summary>
    private static string InputLabel(BindableInput input) => input switch
    {
        BindableInput.Menu => "Menu",
        BindableInput.View => "View",
        BindableInput.X => "X",
        BindableInput.Y => "Y",
        BindableInput.LeftShoulder => "LB",
        BindableInput.RightShoulder => "RB",
        BindableInput.LeftTrigger => "LT",
        BindableInput.RightTrigger => "RT",
        BindableInput.LeftThumbstick => "L3",
        BindableInput.RightThumbstick => "R3",
        _ => input.ToString(),
    };

    /// <summary>Human-readable action label for a bound application command.</summary>
    private static string ActionLabel(AppCommand command) => command switch
    {
        AppCommand.OpenMainMenu => "Open main menu",
        AppCommand.ToggleVirtualKeyboard => "Toggle virtual keyboard",
        AppCommand.AutostartDrive8 => "Autostart drive 8",
        AppCommand.WarmReset => "Warm reset",
        AppCommand.QuickSaveState => "Quick save state",
        AppCommand.QuickLoadState => "Quick load state",
        AppCommand.WarpHoldOn => "Warp (hold)",
        AppCommand.SwapJoystickPorts => "Swap joystick ports",
        _ => command.ToString(),
    };
}
