namespace ViceSharp.Xbox.Input;

/// <summary>
/// A discrete application-function command produced by the system-button
/// evaluator (S10) and the input-context state machine (S11) from held-frame
/// gamepad input. Each value is an intent the downstream dispatcher (S11) marshals
/// onto a host call; none of these are joystick movement (the D-pad, the two
/// sticks, and the A/B fire buttons are the locked joystick and never surface here).
/// </summary>
public enum AppCommand
{
    /// <summary>No command; the neutral / nothing-to-do result.</summary>
    None,

    /// <summary>Open the main menu (from Gameplay).</summary>
    OpenMainMenu,

    /// <summary>Close the current menu / overlay and return to Gameplay.</summary>
    CloseMenu,

    /// <summary>Toggle the on-screen virtual C64 keyboard overlay.</summary>
    ToggleVirtualKeyboard,

    /// <summary>Autostart the medium currently attached to drive 8.</summary>
    AutostartDrive8,

    /// <summary>Warm reset (RESET line) the emulated machine.</summary>
    WarmReset,

    /// <summary>Cold reset (power-cycle) the emulated machine.</summary>
    ColdReset,

    /// <summary>Toggle warp (unthrottled) mode on or off.</summary>
    ToggleWarp,

    /// <summary>Turn warp mode ON for the duration of a hold.</summary>
    WarpHoldOn,

    /// <summary>Turn warp mode OFF at the end of a hold.</summary>
    WarpHoldOff,

    /// <summary>Quick-save the machine state to the active slot.</summary>
    QuickSaveState,

    /// <summary>Quick-load the machine state from the active slot.</summary>
    QuickLoadState,

    /// <summary>Swap the two joystick ports (JOY1 &lt;-&gt; JOY2).</summary>
    SwapJoystickPorts,

    /// <summary>Request application exit (only ever reachable behind a confirm dialog).</summary>
    RequestExit,

    /// <summary>Confirm "yes" on a confirmation dialog.</summary>
    ConfirmYes,

    /// <summary>Confirm "no" / cancel on a confirmation dialog.</summary>
    ConfirmNo,

    /// <summary>Navigate the UI focus up.</summary>
    UiNavigateUp,

    /// <summary>Navigate the UI focus down.</summary>
    UiNavigateDown,

    /// <summary>Navigate the UI focus left.</summary>
    UiNavigateLeft,

    /// <summary>Navigate the UI focus right.</summary>
    UiNavigateRight,

    /// <summary>Activate the focused UI element.</summary>
    UiActivate,

    /// <summary>Go back one level in the UI.</summary>
    UiBack,

    /// <summary>
    /// Virtual-keyboard chord (FIX-XKBDINPUT-001, operator mapping 2026-07-14): inject
    /// INST/DEL (X button while the on-screen keyboard is open). UI-layer only.
    /// </summary>
    KeyboardKeyDelete,

    /// <summary>
    /// Virtual-keyboard chord: inject SPACE (Y button while the on-screen keyboard is
    /// open). UI-layer only.
    /// </summary>
    KeyboardKeySpace,

    /// <summary>
    /// Virtual-keyboard chord: inject RUN/STOP (B button while the on-screen keyboard
    /// is open). UI-layer only.
    /// </summary>
    KeyboardKeyRunStop,

    /// <summary>
    /// Virtual-keyboard chord: inject cursor-left (LB while the on-screen keyboard is
    /// open). UI-layer only.
    /// </summary>
    KeyboardKeyCursorLeft,

    /// <summary>
    /// Virtual-keyboard chord: inject SHIFT + cursor-left (RB while the on-screen
    /// keyboard is open). UI-layer only.
    /// </summary>
    KeyboardKeyShiftCursorLeft,
}
