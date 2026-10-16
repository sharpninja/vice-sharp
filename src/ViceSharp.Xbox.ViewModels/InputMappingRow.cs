namespace ViceSharp.Xbox.ViewModels;

using ViceSharp.Xbox.Input;

/// <summary>
/// One row of the Controls page: a controller input label paired with the action it
/// drives. FEAT-XCTRLBIND-001 adds remappable rows (unlocked system buttons) while the
/// locked joystick / Menu / View / Guide rows stay display-only.
/// </summary>
/// <param name="InputLabel">The controller input as shown to the player (e.g. "Menu", "LB", "A").</param>
/// <param name="ActionLabel">The action the input drives (e.g. "Open main menu", "JOY2 fire").</param>
/// <param name="IsLocked">True when the player cannot rebind this row.</param>
/// <param name="Input">The bindable input for remappable rows; null for locked display rows.</param>
public sealed record InputMappingRow(
    string InputLabel,
    string ActionLabel,
    bool IsLocked = true,
    BindableInput? Input = null);
