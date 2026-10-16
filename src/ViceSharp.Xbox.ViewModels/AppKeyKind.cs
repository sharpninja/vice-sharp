namespace ViceSharp.Xbox.ViewModels;

/// <summary>
/// PLAN-XBOXUWP S25 (IMPL-XBOXUWP-025), area XBOXUI/XKBD. Classifies what pressing a
/// <see cref="VirtualKeyEntry"/> on the on-screen virtual C64 keyboard DOES, so the
/// otherwise-identical tiles can drive three distinct machine seams.
/// </summary>
/// <remarks>
/// A real C64 keyboard is not a uniform grid of matrix keys: RESTORE is a hardware NMI
/// (wired straight to the CPU, not a matrix cell) and SHIFT-LOCK is a latching modifier,
/// not a momentary key. This enum keeps those three behaviours explicit on the layout so
/// the <see cref="VirtualKeyboardViewModel"/> can route each tile correctly and the tests
/// can identify the RESTORE and SHIFT-LOCK tiles without matching on fragile display text.
/// </remarks>
public enum AppKeyKind
{
    /// <summary>
    /// An ordinary C64 key-matrix key. Pressing it emits
    /// <see cref="ViceSharp.Abstractions.IMachineKeyboardInput.SetKeyState(string, bool)"/>
    /// down then up, with the shift-latch applied to the emitted key name.
    /// </summary>
    Key = 0,

    /// <summary>
    /// The SHIFT-LOCK latch tile. Pressing it TOGGLES
    /// <see cref="VirtualKeyboardViewModel.ShiftLatched"/> and emits no key at all: it is a
    /// modifier state, not a keystroke.
    /// </summary>
    ShiftLatch = 1,

    /// <summary>
    /// The RESTORE tile. Pressing it drives the dedicated RESTORE/NMI seam
    /// <see cref="ViceSharp.Abstractions.IMachineKeyboardInput.SetRestoreState(bool)"/>
    /// (asserted then released) and NEVER
    /// <see cref="ViceSharp.Abstractions.IMachineKeyboardInput.SetKeyState(string, bool)"/>,
    /// because RESTORE is a hardware NMI rather than a matrix key. This is the only kind
    /// whose <see cref="VirtualKeyEntry.KeyName"/> is not a C64 keyboard-map key.
    /// </summary>
    Restore = 2,

    /// <summary>
    /// A momentary SHIFT key (the authentic keyboard's left/right SHIFT,
    /// PLAN-XKEYBOARD-001 K1, revised by FEAT-XKBDSTICKY-001). Pressing it STICKS: the
    /// machine key named by <see cref="VirtualKeyEntry.KeyName"/>
    /// ("LeftShift"/"RightShift") goes DOWN immediately (live in the scanned matrix)
    /// and stays down until the next ordinary key stroke completes (which releases it)
    /// or the tile is pressed again (toggle off). Function tiles still map to their
    /// shifted twin in place.
    /// </summary>
    ShiftMomentary = 3,

    /// <summary>
    /// The C= (Commodore) modifier key (FEAT-XKBDSTICKY-001, operator 2026-07-14: "C=
    /// and SHIFT keys are modifiers and should be sticky when clicked until the next
    /// key press which releases them"). Same sticky hold-the-line semantics as
    /// <see cref="ShiftMomentary"/>, on the machine key "Commodore".
    /// </summary>
    CommodoreMomentary = 4,
}
