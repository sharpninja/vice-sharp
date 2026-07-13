namespace ViceSharp.Xbox.ViewModels;

using System.Collections.Generic;
using System.Linq;

/// <summary>
/// PLAN-XBOXUWP S25 (IMPL-XBOXUWP-025), area XBOXUI/XKBD, FR-XBOXUI-006 / FR-XKBD-001.
/// The row-by-row layout of the on-screen, controller-navigable virtual C64 keyboard.
/// Every ordinary tile's <see cref="VirtualKeyEntry.KeyName"/> is a string the real C64
/// keyboard map resolves (validated by the S25 tests, since this project cannot reference
/// the engine); the layout also carries the single SHIFT-LOCK latch tile and the single
/// RESTORE/NMI tile.
/// </summary>
/// <remarks>
/// <para>
/// The rows follow the physical C64 keyboard: a function-key column (F1/F3/F5/F7, whose
/// shifted twins F2/F4/F6/F8 are produced in place by the shift-latch), the top number
/// row, the two letter rows (with RETURN closing the home row), the bottom letter row,
/// a modifier row (RUN/STOP, Commodore, SHIFT-LOCK, SPACE, RESTORE), and a cursor row.
/// </para>
/// <para>
/// This type holds no engine, host, or XAML reference (TR-MVVM-001): it is pure data the
/// <see cref="VirtualKeyboardViewModel"/> walks and the XAML focus layer renders.
/// </para>
/// </remarks>
public sealed class VirtualKeyboardLayout
{
    private VirtualKeyboardLayout(IReadOnlyList<IReadOnlyList<VirtualKeyEntry>> rows)
    {
        Rows = rows;
        AllKeys = rows.SelectMany(row => row).ToArray();
    }

    /// <summary>
    /// The shared default C64 layout. Immutable and safe to reuse across every
    /// <see cref="VirtualKeyboardViewModel"/> instance.
    /// </summary>
    public static VirtualKeyboardLayout Default { get; } = CreateDefault();

    /// <summary>
    /// The tiles grouped into their display rows, top to bottom, left to right.
    /// </summary>
    public IReadOnlyList<IReadOnlyList<VirtualKeyEntry>> Rows { get; }

    /// <summary>
    /// Every tile flattened in row-major order. This is the order controller focus and the
    /// <see cref="VirtualKeyboardViewModel.SelectedIndex"/> index into.
    /// </summary>
    public IReadOnlyList<VirtualKeyEntry> AllKeys { get; }

    /// <summary>Builds the default C64 virtual-keyboard layout.</summary>
    /// <returns>A new <see cref="VirtualKeyboardLayout"/> with the standard C64 rows.</returns>
    public static VirtualKeyboardLayout CreateDefault()
    {
        static VirtualKeyEntry Key(string keyName, string? label = null) =>
            new(keyName, label ?? keyName, IsWide: false);

        static VirtualKeyEntry Wide(string keyName, string label) =>
            new(keyName, label, IsWide: true);

        static IReadOnlyList<VirtualKeyEntry> Letters(string letters) =>
            letters.Select(c => Key(c.ToString())).ToArray();

        var rows = new List<IReadOnlyList<VirtualKeyEntry>>
        {
            // Function-key column. Shifted twins F2/F4/F6/F8 are produced in place by the
            // shift-latch, so the tiles carry only the base key name.
            new[]
            {
                Key("F1", "F1/F2"),
                Key("F3", "F3/F4"),
                Key("F5", "F5/F6"),
                Key("F7", "F7/F8"),
            },

            // Top number row: left-arrow, 1-9 0, plus/minus, pound, HOME, DEL.
            new[]
            {
                Key("LeftArrow", "←"),
                Key("1"), Key("2"), Key("3"), Key("4"), Key("5"),
                Key("6"), Key("7"), Key("8"), Key("9"), Key("0"),
                Key("+"), Key("-"),
                Key("Pound", "£"),
                Key("Home", "CLR HOME"),
                Key("Delete", "INST DEL"),
            },

            // Q row, closing with @, *, and the up-arrow key.
            Letters("QWERTYUIOP").Concat(new[]
            {
                Key("@"),
                Key("*"),
                Key("UpArrow", "↑"),
            }).ToArray(),

            // Home row: A-L, colon/semicolon/equal, then RETURN (double-width).
            Letters("ASDFGHJKL").Concat(new[]
            {
                Key(":"),
                Key(";"),
                Key("="),
                Wide("Return", "RETURN"),
            }).ToArray(),

            // Bottom letter row: Z-M, comma/period/slash.
            Letters("ZXCVBNM").Concat(new[]
            {
                Key(","),
                Key("."),
                Key("/"),
            }).ToArray(),

            // Modifier row: RUN/STOP, Commodore, SHIFT-LOCK latch, SPACE, RESTORE.
            new[]
            {
                Wide("RunStop", "RUN/STOP"),
                Key("Commodore", "C="),
                new VirtualKeyEntry("Shift", "SHIFT LOCK", IsWide: false, AppKeyKind.ShiftLatch),
                Wide("Space", "SPACE"),
                new VirtualKeyEntry("Restore", "RESTORE", IsWide: false, AppKeyKind.Restore),
            },

            // Cursor row.
            new[]
            {
                Key("Up", "▲"),
                Key("Down", "▼"),
                Key("Left", "◀"),
                Key("Right", "▶"),
            },
        };

        return new VirtualKeyboardLayout(rows);
    }
}
