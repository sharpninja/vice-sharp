namespace ViceSharp.Xbox.ViewModels;

/// <summary>
/// FIX-XKBDINPUT-001 (PLAN-XBOXUWP, area XKBD). Operator 2026-07-14: "Emulator not
/// receiving keyboard input." Translates Win32/UWP virtual-key codes from the head's
/// KeyDown/KeyUp events into the C64 keyboard-map names the machine seam resolves
/// (<c>IMachineKeyboardInput.SetKeyState</c>), so a physical keyboard types straight
/// into the running C64.
/// </summary>
/// <remarks>
/// <para>
/// Portable (System only, TR-MVVM-001): virtual keys are the STABLE Win32 codes
/// (Windows.System.VirtualKey shares them), so the table needs no UWP reference and the
/// tests validate every produced name against the REAL C64KeyboardMap.
/// </para>
/// <para>
/// Deliberately unmapped: Escape (the shell-menu toggle), Tab (XAML focus), F9-F12
/// (reserved for app shortcuts), and the gamepad virtual keys (0xC3+). Modifiers pass
/// through as their own C64 keys (SHIFT/CTRL; Alt is the C= Commodore key), matching
/// how the real machine treats them as ordinary matrix keys.
/// </para>
/// </remarks>
public static class PhysicalKeyMap
{
    /// <summary>
    /// Translates one virtual-key code into the C64 keyboard-map name to inject.
    /// </summary>
    /// <param name="virtualKey">The Win32/UWP virtual-key code of the physical key.</param>
    /// <param name="keyName">The C64 keyboard-map name, or empty when unmapped.</param>
    /// <returns><c>true</c> when the key maps to a C64 key.</returns>
    public static bool TryTranslate(int virtualKey, out string keyName)
    {
        keyName = virtualKey switch
        {
            // Letters and digits map to their own names.
            >= 65 and <= 90 => ((char)virtualKey).ToString(),
            >= 48 and <= 57 => ((char)virtualKey).ToString(),

            // Function keys F1-F8 are real C64 keys; F9+ stay app-reserved.
            >= 112 and <= 119 => "F" + (virtualKey - 111),

            13 => "Return",
            32 => "Space",
            8 => "Backspace",
            46 => "Delete",
            36 => "Home",

            // Arrows: the map's host aliases resolve the shifted CRSR combos.
            37 => "Left",
            38 => "Up",
            39 => "Right",
            40 => "Down",

            // Modifiers are ordinary C64 matrix keys; Alt plays the Commodore key.
            16 => "LeftShift",
            160 => "LeftShift",
            161 => "RightShift",
            17 => "Ctrl",
            162 => "Ctrl",
            163 => "Ctrl",
            18 => "Commodore",

            // OEM punctuation: the map's host aliases name these directly.
            186 => "Oem1",      // ;:
            187 => "OemPlus",   // =+
            188 => "OemComma",  // ,<
            189 => "OemMinus",  // -_
            190 => "OemPeriod", // .>
            191 => "Oem2",      // /?
            192 => "Oem3",      // `~ (left-arrow key)
            222 => "Oem7",      // '"

            _ => string.Empty,
        };

        return keyName.Length != 0;
    }
}
