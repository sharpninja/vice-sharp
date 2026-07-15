namespace ViceSharp.Xbox.ViewModels;

using System.Collections.Generic;

/// <summary>
/// FEAT-XKEYCAPCASE-001 + FEAT-XKEYCAPPETSCII-001 (operator 2026-07-14: "When holding
/// SHIFT or C= modifiers, change the keycap to match the character to be inserted" and
/// "map the font glyphs to the keyboard"). Pure keycap-glyph selection for the virtual
/// keyboard: composes the machine's ACTIVE charset case (the video chip's
/// character-base bit: <c>$1000</c> uppercase/graphics, <c>$1800</c>
/// lowercase/uppercase) with the effective SHIFT and C= chord state, and shows the
/// TRUE PETSCII graphic each chord inserts.
/// </summary>
/// <remarks>
/// Ground truth: the KERNAL keyboard decode tables (901227-03: unshifted <c>$EB81</c>,
/// SHIFT <c>$EBC2</c>, C= <c>$EC03</c>) give the PETSCII code per chord; the classic
/// C64 PETSCII-to-Unicode recode tables (uppercase and lowercase sets) give the glyph,
/// using the CUS points (U+F1xx) that the shipped PetMe64 face maps directly. SHIFT
/// letters are PETSCII <c>$C1-$DA</c>: graphics in uppercase mode, plain capitals in
/// lowercase mode. C= letters are the scattered <c>$A1-$BF</c> graphics, identical in
/// both modes. Portable (System only, TR-MVVM-001).
/// </remarks>
public static class VirtualKeycapGlyphs
{
    /// <summary>
    /// Whether a VIC character base selects the LOWERCASE ROM charset (bit 11: the
    /// <c>$1800</c> bank vs the <c>$1000</c> uppercase/graphics bank). Custom charsets
    /// make the notion moot; they report by the same bit and the keycaps stay sane.
    /// </summary>
    /// <param name="characterBase">The VIC character base (from <c>$D018</c>).</param>
    /// <returns><c>true</c> for the lowercase/uppercase charset.</returns>
    public static bool IsLowercaseCharacterBase(int characterBase)
        => (characterBase & 0x0800) != 0;

    // SHIFT+letter, uppercase/graphics mode: PETSCII $C1-$DA in letter order (the
    // RIGHT graphic printed on the physical keycap fronts).
    private static readonly Dictionary<string, string> ShiftLetterGraphics = new()
    {
        ["A"] = "♠", // $C1 black spade
        ["B"] = "│", // $C2 vertical line
        ["C"] = "─", // $C3 horizontal line
        ["D"] = "", // $C4 horizontal one quarter up (CUS)
        ["E"] = "", // $C5 horizontal two quarters up (CUS)
        ["F"] = "", // $C6 horizontal one quarter down (CUS)
        ["G"] = "", // $C7 vertical one quarter left (CUS)
        ["H"] = "", // $C8 vertical one quarter right (CUS)
        ["I"] = "╮", // $C9 arc down-left
        ["J"] = "╰", // $CA arc up-right
        ["K"] = "╯", // $CB arc up-left
        ["L"] = "", // $CC eighth block up and right (CUS)
        ["M"] = "╲", // $CD diagonal upper-left to lower-right
        ["N"] = "╱", // $CE diagonal upper-right to lower-left
        ["O"] = "", // $CF eighth block down and right (CUS)
        ["P"] = "", // $D0 eighth block down and left (CUS)
        ["Q"] = "●", // $D1 black circle
        ["R"] = "", // $D2 horizontal two quarters down (CUS)
        ["S"] = "♥", // $D3 black heart
        ["T"] = "", // $D4 vertical two quarters left (CUS)
        ["U"] = "╭", // $D5 arc down-right
        ["V"] = "╳", // $D6 diagonal cross
        ["W"] = "○", // $D7 white circle
        ["X"] = "♣", // $D8 black club
        ["Y"] = "", // $D9 vertical two quarters right (CUS)
        ["Z"] = "♦", // $DA black diamond
    };

    // C=+letter: the scattered PETSCII $A1-$BF codes from the KERNAL C= table (the
    // LEFT keycap graphic). The $A1-$BF glyphs are the same in both charset sets.
    private static readonly Dictionary<string, string> CbmLetterGraphics = new()
    {
        ["A"] = "┌", // $B0 down-right corner
        ["B"] = "", // $BF two small squares diagonal (CUS)
        ["C"] = "", // $BC small square upper right (CUS)
        ["D"] = "", // $AC small square lower right (CUS)
        ["E"] = "┴", // $B1 up and horizontal
        ["F"] = "", // $BB small square lower left (CUS)
        ["G"] = "▏", // $A5 left one eighth block
        ["H"] = "▎", // $B4 left one quarter block
        ["I"] = "▄", // $A2 lower half block
        ["J"] = "▍", // $B5 left three eighths block
        ["K"] = "▌", // $A1 left half block
        ["L"] = "", // $B6 right three eighths block (CUS)
        ["M"] = "▕", // $A7 right one eighth block
        ["N"] = "", // $AA right one quarter block (CUS)
        ["O"] = "▃", // $B9 lower three eighths block
        ["P"] = "▂", // $AF lower one quarter block
        ["Q"] = "├", // $AB vertical and right
        ["R"] = "┬", // $B2 down and horizontal
        ["S"] = "┐", // $AE down-left corner
        ["T"] = "▔", // $A3 upper one eighth block
        ["U"] = "", // $B8 upper three eighths block (CUS)
        ["V"] = "", // $BE small square upper left (CUS)
        ["W"] = "┤", // $B3 vertical and left
        ["X"] = "┘", // $BD up-left corner
        ["Y"] = "", // $B7 upper one quarter block (CUS)
        ["Z"] = "└", // $AD up-right corner
    };

    // SHIFT punctuation graphics, (uppercase-set, lowercase-set) per key: PETSCII
    // $BA/$C0/$DB/$DD/$A9/$DE render differently across the two charsets.
    private static readonly Dictionary<string, (string Uppercase, string Lowercase)> ShiftSpecialGraphics = new()
    {
        ["@"] = ("", "✓"),      // $BA: eighth block up-left (CUS) / check mark
        ["*"] = ("─", "─"),      // $C0: horizontal line
        ["+"] = ("┼", "┼"),      // $DB: cross
        ["-"] = ("│", "│"),      // $DD: vertical line
        ["Pound"] = ("◤", ""),  // $A9: upper-left triangle / shade slashed right (CUS)
        ["UpArrow"] = ("π", "▒"),// $DE: pi / medium shade
    };

    // C= punctuation graphics, (uppercase-set, lowercase-set) per key.
    private static readonly Dictionary<string, (string Uppercase, string Lowercase)> CbmSpecialGraphics = new()
    {
        ["@"] = ("▁", "▁"),      // $A4: lower one eighth block
        ["*"] = ("◥", ""),      // $DF: upper-right triangle / shade slashed left (CUS)
        ["+"] = ("▒", "▒"),      // $A6: medium shade
        ["-"] = ("", ""),      // $DC: left-half shade (CUS)
        ["Pound"] = ("", ""),  // $A8: lower-half shade (CUS)
        ["UpArrow"] = ("π", "▒"),// $DE: same code as SHIFT+^
    };

    // Keys whose C= chord emits the SAME printable as SHIFT (KERNAL C= table rows
    // 3C/3E/3F/5B/5D and C=+9 = ')'): show the printed shifted legend.
    private static readonly HashSet<string> CbmMatchesShiftPrintable = new()
    {
        ",", ".", "/", ":", ";", "9",
    };

    /// <summary>
    /// The glyph a keycap shows for the current machine charset case and effective
    /// chord state: the exact character the press will insert.
    /// </summary>
    /// <param name="entry">The keycap's layout entry.</param>
    /// <param name="shifted">Whether SHIFT is effective (trigger hold / latch / one-shot).</param>
    /// <param name="commodore">Whether the C= modifier is effective (trigger hold). Wins over SHIFT.</param>
    /// <param name="lowercaseMode">Whether the machine runs the lowercase charset.</param>
    /// <returns>The display string for the keycap.</returns>
    public static string For(VirtualKeyEntry entry, bool shifted, bool commodore, bool lowercaseMode)
    {
        if (entry.Kind != AppKeyKind.Key)
            return entry.DisplayLabel;

        if (commodore)
        {
            if (CbmLetterGraphics.TryGetValue(entry.KeyName, out var cbmGraphic))
                return cbmGraphic;
            if (CbmSpecialGraphics.TryGetValue(entry.KeyName, out var cbmSpecial))
                return lowercaseMode ? cbmSpecial.Lowercase : cbmSpecial.Uppercase;
            if (CbmMatchesShiftPrintable.Contains(entry.KeyName) && entry.ShiftedLabel is not null)
                return entry.ShiftedLabel;

            // C=+digit is a color control; nothing printable to preview.
            return entry.DisplayLabel;
        }

        if (shifted)
        {
            if (IsLetterKey(entry))
            {
                // Uppercase/graphics mode: the PETSCII right-keycap graphic. Lowercase
                // mode: SHIFT types the plain capital.
                return lowercaseMode ? entry.KeyName : ShiftLetterGraphics[entry.KeyName];
            }

            if (ShiftSpecialGraphics.TryGetValue(entry.KeyName, out var shiftSpecial))
                return lowercaseMode ? shiftSpecial.Lowercase : shiftSpecial.Uppercase;

            if (entry.ShiftedLabel is not null)
                return entry.ShiftedLabel;

            return entry.DisplayLabel;
        }

        // Single letters follow the machine's charset case.
        if (IsLetterKey(entry))
        {
            return lowercaseMode
                ? char.ToLowerInvariant(entry.KeyName[0]).ToString()
                : entry.KeyName;
        }

        return entry.DisplayLabel;
    }

    private static bool IsLetterKey(VirtualKeyEntry entry)
        => entry.KeyName.Length == 1 && entry.KeyName[0] is >= 'A' and <= 'Z';
}
