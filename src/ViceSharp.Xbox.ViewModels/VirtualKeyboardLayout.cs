namespace ViceSharp.Xbox.ViewModels;

using System.Collections.Generic;
using System.Linq;

/// <summary>
/// PLAN-XBOXUWP S25 (IMPL-XBOXUWP-025) + PLAN-XKEYBOARD-001 K1, area XBOXUI/XKBD. The
/// layout of the on-screen virtual C64 keyboard. The default is the AUTHENTIC 66-key
/// machine: five physical rows plus the four function keys that sit in a column on the
/// RIGHT of the real keyboard (operator constraint 2026-07-14). Every ordinary tile's
/// <see cref="VirtualKeyEntry.KeyName"/> is a string the real C64 keyboard map resolves
/// (validated by the tests, since this project cannot reference the engine).
/// </summary>
/// <remarks>
/// <para>
/// Physical reference (66 keys): row 1 = left-arrow 1..0 + - pound CLR/HOME INST/DEL;
/// row 2 = CTRL Q..P @ * up-arrow RESTORE; row 3 = RUN/STOP SHIFT-LOCK A..L : ; = RETURN;
/// row 4 = C= SHIFT Z..M , . / SHIFT CRSR-down CRSR-right; row 5 = SPACE; function column
/// = F1 F3 F5 F7 (shifted twins F2/F4/F6/F8 are produced in place by the shift latch or a
/// momentary shift).
/// </para>
/// <para>
/// This type holds no engine, host, or XAML reference (TR-MVVM-001): it is pure data the
/// <see cref="VirtualKeyboardViewModel"/> walks and the XAML focus layer renders.
/// </para>
/// </remarks>
public sealed class VirtualKeyboardLayout
{
    private VirtualKeyboardLayout(
        IReadOnlyList<VirtualKeyRow> rows,
        IReadOnlyList<VirtualKeyEntry> functionKeys)
    {
        Rows = rows;
        FunctionKeys = functionKeys;
        AllKeys = rows.SelectMany(row => row).Concat(functionKeys).ToArray();
    }

    /// <summary>
    /// The shared default layout: the authentic 66-key C64 keyboard. Immutable and safe
    /// to reuse across every <see cref="VirtualKeyboardViewModel"/> instance.
    /// </summary>
    public static VirtualKeyboardLayout Default { get; } = CreateAuthentic();

    /// <summary>
    /// The tiles grouped into their display rows, top to bottom, left to right. The
    /// function keys are NOT part of the rows: they live in <see cref="FunctionKeys"/>
    /// and render as a column on the right, as on the physical machine.
    /// Each row is a <see cref="VirtualKeyRow"/> so XAML can <c>{x:Bind Keys}</c>
    /// without reflection (FEAT-XAOTBIND-001).
    /// </summary>
    public IReadOnlyList<VirtualKeyRow> Rows { get; }

    /// <summary>
    /// The function-key column (F1/F3/F5/F7), rendered on the RIGHT of the keyboard
    /// (PLAN-XKEYBOARD-001). Empty on layouts that fold the function keys into the rows.
    /// </summary>
    public IReadOnlyList<VirtualKeyEntry> FunctionKeys { get; }

    /// <summary>
    /// Every tile flattened: the rows in row-major order followed by the function column.
    /// This is the order controller focus and
    /// <see cref="VirtualKeyboardViewModel.SelectedIndex"/> index into.
    /// </summary>
    public IReadOnlyList<VirtualKeyEntry> AllKeys { get; }

    /// <summary>Builds the authentic 66-key C64 layout (the <see cref="Default"/>).</summary>
    /// <returns>A new <see cref="VirtualKeyboardLayout"/> mirroring the physical machine.</returns>
    public static VirtualKeyboardLayout CreateAuthentic()
    {
        static VirtualKeyEntry Key(string keyName, string? label = null, double width = 0, string? shifted = null) =>
            new(keyName, label ?? keyName, IsWide: width > 1, AppKeyKind.Key, width, shifted);

        static IReadOnlyList<VirtualKeyEntry> Letters(string letters) =>
            letters.Select(c => Key(c.ToString())).ToArray();

        static VirtualKeyRow Row(params VirtualKeyEntry[] keys) => new(keys);
        static VirtualKeyRow RowFrom(IEnumerable<VirtualKeyEntry> keys) => new(keys.ToArray());

        var rows = new List<VirtualKeyRow>
        {
            // Row 1: left-arrow, 1-0, +, -, pound, CLR/HOME, INST/DEL (16 keys). The
            // digit keys carry their printed shifted legends (FEAT-XKEYCAPSHIFT-001);
            // SHIFT+0 is 0 on the machine, so 0 has none.
            Row(
                Key("LeftArrow", "←"),
                Key("1", shifted: "!"), Key("2", shifted: "\""), Key("3", shifted: "#"),
                Key("4", shifted: "$"), Key("5", shifted: "%"),
                Key("6", shifted: "&"), Key("7", shifted: "'"), Key("8", shifted: "("),
                Key("9", shifted: ")"), Key("0"),
                Key("+"), Key("-"),
                Key("Pound", "£"),
                Key("Home", "CLR HOME"),
                Key("Delete", "INST DEL")),

            // Row 2: CTRL, Q-P, @, *, up-arrow, RESTORE (15 keys). RESTORE sits at the
            // row's right end on the machine and drives the NMI seam, never the matrix.
            RowFrom(new[] { Key("Ctrl", "CTRL", 1.5) }
                .Concat(Letters("QWERTYUIOP"))
                .Concat(new[]
                {
                    Key("@"),
                    Key("*"),
                    // SHIFT + up-arrow types pi (the legend on the physical keycap).
                    Key("UpArrow", "↑", shifted: "π"),
                    new VirtualKeyEntry("Restore", "RESTORE", IsWide: false, AppKeyKind.Restore),
                })),

            // Row 3: RUN/STOP, SHIFT-LOCK, A-L, :, ;, =, RETURN (15 keys). RETURN is the
            // classic wide key closing the home row.
            RowFrom(new[]
                {
                    // Authentic two-line caps (operator 2026-07-14): the key is STOP,
                    // the shifted state is RUN, and SHIFT LOCK is the mechanical toggle.
                    Key("RunStop", "RUN\nSTOP", shifted: "RUN"),
                    new VirtualKeyEntry("Shift", "SHIFT\nLOCK", IsWide: false, AppKeyKind.ShiftLatch),
                }
                .Concat(Letters("ASDFGHJKL"))
                .Concat(new[]
                {
                    Key(":", shifted: "["),
                    Key(";", shifted: "]"),
                    Key("="),
                    Key("Return", "RETURN", 2.0),
                })),

            // Row 4: C=, SHIFT, Z-M, comma, period, slash, SHIFT, CRSR down, CRSR right
            // (15 keys). The two momentary SHIFTs wrap the NEXT key press hardware-style,
            // which is also how CRSR-up (SHIFT+down) and CRSR-left (SHIFT+right) work.
            RowFrom(new[]
                {
                    // C= is a sticky modifier tile (FEAT-XKBDSTICKY-001), not a keystroke.
                    new VirtualKeyEntry("Commodore", "C=", IsWide: false, AppKeyKind.CommodoreMomentary),
                    new VirtualKeyEntry("LeftShift", "SHIFT", IsWide: false, AppKeyKind.ShiftMomentary, 1.5),
                }
                .Concat(Letters("ZXCVBNM"))
                .Concat(new[]
                {
                    Key(",", shifted: "<"),
                    Key(".", shifted: ">"),
                    Key("/", shifted: "?"),
                    new VirtualKeyEntry("RightShift", "SHIFT", IsWide: false, AppKeyKind.ShiftMomentary, 1.5),
                    Key("Down", "CRSR ⇕"),
                    Key("Right", "CRSR ⇔"),
                })),

            // Row 5: the space bar.
            Row(Key("Space", "SPACE", 9.0)),
        };

        // The function column, rendered on the RIGHT like the physical machine's F-keys.
        var functionKeys = new[]
        {
            Key("F1", "F1  F2"),
            Key("F3", "F3  F4"),
            Key("F5", "F5  F6"),
            Key("F7", "F7  F8"),
        };

        return new VirtualKeyboardLayout(rows, functionKeys);
    }
}
