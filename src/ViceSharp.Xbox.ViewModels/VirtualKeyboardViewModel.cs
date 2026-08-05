namespace ViceSharp.Xbox.ViewModels;

using System;
using System.Collections.Generic;
using ViceSharp.Abstractions;

/// <summary>
/// PLAN-XBOXUWP S25 (IMPL-XBOXUWP-025), area XBOXUI/XKBD, FR-XBOXUI-006 / FR-XKBD-001,
/// TR-XBOXUI-006 / TR-XKBD-001, TEST-XBOXUI-006 / TEST-XKBD-001. The controller-navigable
/// on-screen virtual C64 keyboard: it walks a <see cref="VirtualKeyboardLayout"/> and
/// injects each pressed tile through the machine-owned
/// <see cref="IMachineKeyboardInput"/> seam.
/// </summary>
/// <remarks>
/// <para>
/// The ViewModels project cannot reference the emulation engine, so the key strings live
/// in the layout as hardcoded values; the S25 tests validate every one of them against
/// the real C64 keyboard map. FEAT-XKBDSTICKY-001 (operator 2026-07-14) made the keyboard
/// STATE-DRIVEN so the machine's matrix scan sees it in real time. Behaviour by tile kind:
/// </para>
/// <list type="bullet">
///   <item><description><see cref="AppKeyKind.Key"/>: finishes any previous stroke, then
///   presses the shift-resolved key name DOWN and holds it until
///   <see cref="CompletePress"/> releases the key and the armed sticky
///   modifiers.</description></item>
///   <item><description><see cref="AppKeyKind.ShiftMomentary"/> /
///   <see cref="AppKeyKind.CommodoreMomentary"/>: STICKY modifiers - arming presses the
///   machine key immediately and holds its line; the next completed key stroke (or a
///   second press of the tile) releases it.</description></item>
///   <item><description><see cref="AppKeyKind.ShiftLatch"/>: toggles
///   <see cref="ShiftLatched"/>, which HOLDS the LeftShift line while engaged (the
///   mechanical SHIFT-LOCK).</description></item>
///   <item><description><see cref="AppKeyKind.Restore"/>: drives the dedicated RESTORE/NMI
///   seam <see cref="IMachineKeyboardInput.SetRestoreState(bool)"/> asserted then released,
///   and never <see cref="IMachineKeyboardInput.SetKeyState(string, bool)"/>.</description></item>
/// </list>
/// <para>
/// Under the latch or an armed sticky shift, the function keys map to their twins in
/// place (F1-&gt;F2, F3-&gt;F4, F5-&gt;F6, F7-&gt;F8); every other key emits its base
/// name, with the shift line physically held so the machine resolves the chord exactly
/// like hardware.
/// </para>
/// <para>
/// This type holds no engine, host, or XAML reference beyond the narrow Abstractions input
/// contract (TR-MVVM-001).
/// </para>
/// </remarks>
public sealed class VirtualKeyboardViewModel
{
    private readonly IMachineKeyboardInput _keyboard;
    private int _selectedIndex;

    /// <summary>
    /// Creates the view-model over a machine keyboard-input seam, using the shared default
    /// C64 layout.
    /// </summary>
    /// <param name="keyboard">The machine-owned keyboard-input seam pressed tiles inject through.</param>
    /// <exception cref="ArgumentNullException"><paramref name="keyboard"/> is <c>null</c>.</exception>
    public VirtualKeyboardViewModel(IMachineKeyboardInput keyboard)
        : this(keyboard, VirtualKeyboardLayout.Default)
    {
    }

    /// <summary>
    /// Creates the view-model over a machine keyboard-input seam and an explicit layout.
    /// </summary>
    /// <param name="keyboard">The machine-owned keyboard-input seam pressed tiles inject through.</param>
    /// <param name="layout">The keyboard layout to present.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="keyboard"/> or <paramref name="layout"/> is <c>null</c>.
    /// </exception>
    public VirtualKeyboardViewModel(IMachineKeyboardInput keyboard, VirtualKeyboardLayout layout)
    {
        _keyboard = keyboard ?? throw new ArgumentNullException(nameof(keyboard));
        Layout = layout ?? throw new ArgumentNullException(nameof(layout));
    }

    /// <summary>
    /// Creates the view-model by resolving the keyboard-input seam for a session from the
    /// emulator-session facade.
    /// </summary>
    /// <param name="facade">The emulator-session host facade.</param>
    /// <param name="sessionId">The session whose keyboard input the tiles drive.</param>
    /// <exception cref="ArgumentNullException"><paramref name="facade"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">
    /// The session has no keyboard-input device.
    /// </exception>
    public VirtualKeyboardViewModel(IEmulatorSessionFacade facade, string sessionId)
        : this(
            (facade ?? throw new ArgumentNullException(nameof(facade))).GetKeyboardInput(sessionId)
                ?? throw new InvalidOperationException(
                    $"Session '{sessionId}' has no keyboard-input device."),
            VirtualKeyboardLayout.Default)
    {
    }

    /// <summary>The layout this view-model presents.</summary>
    public VirtualKeyboardLayout Layout { get; }

    /// <summary>The layout tiles grouped into display rows (top to bottom, left to right).</summary>
    /// <summary>
    /// Display rows as <see cref="VirtualKeyRow"/> (FEAT-XAOTBIND-001: named type for
    /// compiled <c>{x:Bind Keys}</c> on the keyboard overlay).
    /// </summary>
    public IReadOnlyList<VirtualKeyRow> Rows => Layout.Rows;

    /// <summary>The layout tiles flattened in row-major order; the index space of
    /// <see cref="SelectedIndex"/>.</summary>
    public IReadOnlyList<VirtualKeyEntry> AllKeys => Layout.AllKeys;

    /// <summary>
    /// Whether the SHIFT-LOCK latch is engaged. Like the mechanical SHIFT-LOCK it HOLDS
    /// the LeftShift matrix line while engaged (FEAT-XKBDSTICKY-001: the keyboard is
    /// scanned in real time), and function tiles emit their shifted twin in place
    /// (F1-&gt;F2, etc.). Setting it directly is equivalent to toggling the SHIFT-LOCK
    /// tile. The latch persists across key strokes until toggled off.
    /// </summary>
    public bool ShiftLatched
    {
        get => _shiftLatched;
        set
        {
            if (_shiftLatched == value)
                return;

            _shiftLatched = value;
            _keyboard.SetKeyState("LeftShift", value);
        }
    }

    private bool _shiftLatched;

    /// <summary>
    /// Whether a sticky SHIFT modifier is engaged (FEAT-XKBDSTICKY-001, operator: "C=
    /// and SHIFT keys are modifiers and should be sticky when clicked until the next
    /// key press which releases them"). The arming click presses the machine shift
    /// IMMEDIATELY (live in the scanned matrix); completing the next ordinary key
    /// stroke releases it, as does pressing the tile again.
    /// </summary>
    public bool ShiftArmed => _armedStickies.Contains("LeftShift") || _armedStickies.Contains("RightShift");

    /// <summary>
    /// Whether the sticky C= modifier is engaged. Same semantics as
    /// <see cref="ShiftArmed"/>, on the machine key "Commodore".
    /// </summary>
    public bool CommodoreArmed => _armedStickies.Contains("Commodore");

    private readonly HashSet<string> _armedStickies = new(StringComparer.Ordinal);
    private string? _pendingKeyName;

    /// <summary>
    /// The index into <see cref="AllKeys"/> of the currently focused tile, pressed by
    /// <see cref="PressCurrent"/>. Actual up/down/left/right focus movement is owned by the
    /// UI focus layer, which sets this index.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The value is negative or not less than <see cref="AllKeys"/> count.
    /// </exception>
    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            if (value < 0 || value >= AllKeys.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            _selectedIndex = value;
        }
    }

    /// <summary>The currently focused tile (the one <see cref="PressCurrent"/> presses).</summary>
    public VirtualKeyEntry Selected => AllKeys[_selectedIndex];

    /// <summary>Presses the currently focused tile (see <see cref="SelectedIndex"/>).</summary>
    public void PressCurrent() => Press(Selected);

    /// <summary>
    /// Presses a tile: injects an ordinary key (with the shift-latch applied), toggles the
    /// SHIFT-LOCK latch, or fires the RESTORE/NMI seam, depending on the tile's
    /// <see cref="VirtualKeyEntry.Kind"/>.
    /// </summary>
    /// <param name="entry">The tile to press.</param>
    /// <exception cref="ArgumentNullException"><paramref name="entry"/> is <c>null</c>.</exception>
    public void Press(VirtualKeyEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        switch (entry.Kind)
        {
            case AppKeyKind.Restore:
                // RESTORE is a hardware NMI, not a matrix key: never SetKeyState.
                _keyboard.SetRestoreState(true);
                _keyboard.SetRestoreState(false);
                break;

            case AppKeyKind.ShiftLatch:
                // SHIFT-LOCK is a latching modifier; the setter holds/releases the line.
                ShiftLatched = !ShiftLatched;
                break;

            case AppKeyKind.ShiftMomentary:
            case AppKeyKind.CommodoreMomentary:
                ToggleSticky(entry.KeyName);
                break;

            case AppKeyKind.Key:
            default:
                PressKey(entry.KeyName);
                break;
        }
    }

    /// <summary>
    /// Finishes the current key stroke (FEAT-XKBDSTICKY-001): releases the held key,
    /// then every armed sticky modifier (hardware order: fingers leave the key before
    /// the modifiers), clearing the arms. No-op when no stroke is pending, so sticky
    /// modifiers stay engaged until an actual key press consumes them.
    /// </summary>
    public void CompletePress()
    {
        if (_pendingKeyName is null)
            return;

        _keyboard.SetKeyState(_pendingKeyName, false);
        _pendingKeyName = null;

        ReleaseStickies();
    }

    /// <summary>
    /// Releases everything this keyboard holds on the machine: the pending key stroke,
    /// the sticky modifiers, and the SHIFT-LOCK line. Called by the head whenever the
    /// dock closes or the shell menu takes over, so nothing stays pressed behind the
    /// emulator's back.
    /// </summary>
    public void ReleaseAll()
    {
        if (_pendingKeyName is not null)
        {
            _keyboard.SetKeyState(_pendingKeyName, false);
            _pendingKeyName = null;
        }

        ReleaseStickies();
        ShiftLatched = false;
    }

    /// <summary>
    /// Toggles one sticky modifier (FEAT-XKBDSTICKY-001): arming presses the machine
    /// key immediately so the matrix scan sees it in real time; toggling off (or the
    /// next completed key stroke) releases it.
    /// </summary>
    private void ToggleSticky(string modifierKeyName)
    {
        if (_armedStickies.Remove(modifierKeyName))
        {
            _keyboard.SetKeyState(modifierKeyName, false);
            return;
        }

        _armedStickies.Add(modifierKeyName);
        _keyboard.SetKeyState(modifierKeyName, true);
    }

    private void ReleaseStickies()
    {
        if (_armedStickies.Count == 0)
            return;

        foreach (var modifier in _armedStickies)
            _keyboard.SetKeyState(modifier, false);

        _armedStickies.Clear();
    }

    /// <summary>
    /// Starts one ordinary key stroke: any previous stroke completes first (no stuck
    /// keys under fast typing), the function twins map in place under the latch or an
    /// armed sticky shift, and the resolved key goes DOWN and STAYS down - the sticky
    /// modifiers are already holding their lines - until <see cref="CompletePress"/>.
    /// </summary>
    private void PressKey(string baseKeyName)
    {
        CompletePress();

        var keyName = ResolveKeyName(baseKeyName, shifted: ShiftLatched || ShiftArmed);

        _keyboard.SetKeyState(keyName, true);
        _pendingKeyName = keyName;
    }

    /// <summary>
    /// Applies shift to an ordinary key name: when shifted, the function keys map to their
    /// twins in place; every other key returns the SAME string instance unchanged (the
    /// caller uses reference identity to detect that no twin remap occurred).
    /// </summary>
    private static string ResolveKeyName(string keyName, bool shifted)
    {
        if (!shifted)
        {
            return keyName;
        }

        return keyName switch
        {
            "F1" => "F2",
            "F3" => "F4",
            "F5" => "F6",
            "F7" => "F8",
            _ => keyName,
        };
    }
}
