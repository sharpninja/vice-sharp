namespace ViceSharp.Xbox.Input;

/// <summary>
/// The four states of the single-consumer input state machine
/// (<see cref="XboxInputContext"/>). The active context decides whether a
/// <see cref="GamepadSnapshot"/> drives the emulated C64 joystick (Gameplay) or the
/// on-screen UI (every other context).
/// </summary>
/// <remarks>
/// PLAN-XBOXUWP S11 (IMPL-XBOXUWP-011), FR-CTX-001. The ViewModels project maps any
/// non-Gameplay context to a single "UI navigation" mode for focus routing; the
/// distinction between <see cref="MainMenu"/>, <see cref="VirtualKeyboard"/> and
/// <see cref="ConfirmDialog"/> matters only to the context machine (e.g. A means
/// ConfirmYes in <see cref="ConfirmDialog"/> but UiActivate elsewhere).
/// </remarks>
public enum InputContext
{
    /// <summary>
    /// The emulator is in focus: the gamepad drives the C64 joystick ports and the
    /// gameplay system-button bindings are live.
    /// </summary>
    Gameplay,

    /// <summary>The main menu overlay is open; the gamepad drives UI navigation.</summary>
    MainMenu,

    /// <summary>The on-screen virtual C64 keyboard overlay is open.</summary>
    VirtualKeyboard,

    /// <summary>A confirmation dialog is open; A confirms, B cancels.</summary>
    ConfirmDialog,
}
