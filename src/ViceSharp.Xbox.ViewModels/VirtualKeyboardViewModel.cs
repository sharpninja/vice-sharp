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
/// the real C64 keyboard map. Pressing behaviour by tile kind:
/// </para>
/// <list type="bullet">
///   <item><description><see cref="AppKeyKind.Key"/>: emits
///   <see cref="IMachineKeyboardInput.SetKeyState(string, bool)"/> down then up, once
///   each, on the shift-resolved key name.</description></item>
///   <item><description><see cref="AppKeyKind.ShiftLatch"/>: TOGGLES
///   <see cref="ShiftLatched"/> and emits nothing.</description></item>
///   <item><description><see cref="AppKeyKind.Restore"/>: drives the dedicated RESTORE/NMI
///   seam <see cref="IMachineKeyboardInput.SetRestoreState(bool)"/> asserted then released,
///   and never <see cref="IMachineKeyboardInput.SetKeyState(string, bool)"/>.</description></item>
/// </list>
/// <para>
/// Shift-latch semantics: the latch mirrors the C64 SHIFT-LOCK. It is a persistent latch,
/// NOT a one-shot: once engaged it stays engaged across key presses until it is toggled
/// off (via the SHIFT-LOCK tile or by setting <see cref="ShiftLatched"/>). While engaged,
/// the only remapping is the function keys in place: F1-&gt;F2, F3-&gt;F4, F5-&gt;F6,
/// F7-&gt;F8. All other keys emit their base name unchanged (a single key down/up, never a
/// wrapped shift-plus-key sequence).
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
    public IReadOnlyList<IReadOnlyList<VirtualKeyEntry>> Rows => Layout.Rows;

    /// <summary>The layout tiles flattened in row-major order; the index space of
    /// <see cref="SelectedIndex"/>.</summary>
    public IReadOnlyList<VirtualKeyEntry> AllKeys => Layout.AllKeys;

    /// <summary>
    /// Whether the SHIFT-LOCK latch is engaged. When engaged, pressing a function tile
    /// emits its shifted twin in place (F1-&gt;F2, etc.). Setting it directly is equivalent
    /// to toggling the SHIFT-LOCK tile. The latch persists across key presses until cleared.
    /// </summary>
    public bool ShiftLatched { get; set; }

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
                // SHIFT-LOCK is a latching modifier, not a keystroke.
                ShiftLatched = !ShiftLatched;
                break;

            case AppKeyKind.Key:
            default:
                var keyName = ResolveKeyName(entry.KeyName);
                _keyboard.SetKeyState(keyName, true);
                _keyboard.SetKeyState(keyName, false);
                break;
        }
    }

    /// <summary>
    /// Applies the shift-latch to an ordinary key name: with the latch engaged the function
    /// keys map to their shifted twins in place; every other key is unchanged.
    /// </summary>
    private string ResolveKeyName(string keyName)
    {
        if (!ShiftLatched)
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
