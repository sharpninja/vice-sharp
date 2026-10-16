namespace ViceSharp.Xbox.ViewModels;

using System.Collections.Generic;

/// <summary>
/// FEAT-XKEYCAPCASE-001 + FEAT-XKEYCAPPETSCII-001 (operator 2026-07-14: "When holding
/// SHIFT or C= modifiers, change the keycap to match the character to be inserted" and
/// "map the font glyphs to the keyboard"; corrected 2026-07-18 "The C= line is very, very
/// wrong"). Pure keycap-glyph selection for the virtual keyboard: composes the machine's
/// ACTIVE charset case with the effective SHIFT and C= chord state, and shows the TRUE
/// PETSCII graphic each chord inserts.
/// </summary>
/// <remarks>
/// Ground truth: the KERNAL keyboard decode tables (901227-03: unshifted <c>$EB81</c>,
/// SHIFT <c>$EBC2</c>, C= <c>$EC03</c>) give the PETSCII code per chord. The vendored
/// PetMe64 face maps the WHOLE PETSCII set at its private-use range <c>U+F100..U+F1FF</c>,
/// where <c>U+F1<i>nn</i></c> renders the C64 uppercase/graphics glyph for PETSCII code
/// <c>$<i>nn</i></c> (verified against the TTF cmap and by rendering). So every SHIFT/C=
/// graphic is just <c>U+F100 + petscii</c> - no hand-picked Unicode approximations, which
/// is what previously went wrong: the CUS entries were keyed to the punctuation/digit
/// PETSCII codes (<c>$22..$2C</c>, <c>$30..$38</c>) and so drew <c>" # $ % ...</c> and
/// <c>0 1 2 ...</c> instead of the block/corner graphics.
///
/// <para>SHIFT+letter is PETSCII <c>$C1-$DA</c> (graphics in uppercase mode; the plain
/// capital in lowercase mode, since those codes ARE the capitals there). C=+letter is the
/// scattered <c>$A1-$BF</c> graphics, identical in both charset sets. The handful of
/// charset-dependent punctuation chords keep an explicit lowercase glyph. US and UK C64
/// keyboards are identical (same KERNAL, same PETSCII), so NTSC and PAL share these
/// glyphs. Portable (System only, TR-MVVM-001).</para>
/// </remarks>
public static class VirtualKeycapGlyphs
{
    /// <summary>The PetMe64 private-use base: <c>U+F100 + petscii</c> renders PETSCII <c>$petscii</c>.</summary>
    private const int PetsciiGlyphBase = 0xF100;

    /// <summary>
    /// Whether a VIC character base selects the LOWERCASE ROM charset (bit 11: the
    /// <c>$1800</c> bank vs the <c>$1000</c> uppercase/graphics bank). Custom charsets
    /// make the notion moot; they report by the same bit and the keycaps stay sane.
    /// </summary>
    /// <param name="characterBase">The VIC character base (from <c>$D018</c>).</param>
    /// <returns><c>true</c> for the lowercase/uppercase charset.</returns>
    public static bool IsLowercaseCharacterBase(int characterBase)
        => (characterBase & 0x0800) != 0;

    // C=+letter -> the scattered PETSCII $A1-$BF codes from the KERNAL C= table $EC03
    // (the LEFT keycap graphic). Identical in both charset sets.
    private static readonly Dictionary<string, int> CbmLetterPetscii = new()
    {
        ["A"] = 0xB0, ["B"] = 0xBF, ["C"] = 0xBC, ["D"] = 0xAC, ["E"] = 0xB1, ["F"] = 0xBB,
        ["G"] = 0xA5, ["H"] = 0xB4, ["I"] = 0xA2, ["J"] = 0xB5, ["K"] = 0xA1, ["L"] = 0xB6,
        ["M"] = 0xA7, ["N"] = 0xAA, ["O"] = 0xB9, ["P"] = 0xAF, ["Q"] = 0xAB, ["R"] = 0xB2,
        ["S"] = 0xAE, ["T"] = 0xA3, ["U"] = 0xB8, ["V"] = 0xBE, ["W"] = 0xB3, ["X"] = 0xBD,
        ["Y"] = 0xB7, ["Z"] = 0xAD,
    };

    // SHIFT+punctuation graphics: the KERNAL SHIFT table $EBC2 PETSCII code (the RIGHT
    // keycap graphic) plus the lowercase-charset glyph for the few codes that differ
    // between the two charsets (the single-charset PetMe64 face has no lowercase graphic
    // for those, so a Unicode stand-in is used there; uppercase - the boot mode - is the
    // authentic PetMe64 glyph).
    private static readonly Dictionary<string, (int Petscii, string Lowercase)> ShiftSpecials = new()
    {
        ["@"] = (0xBA, "✓"),      // eighth block up-left / check mark
        ["*"] = (0xC0, "─"),      // horizontal line (same both sets)
        ["+"] = (0xDB, "┼"),      // cross (same both sets)
        ["-"] = (0xDD, "│"),      // vertical line (same both sets)
        ["Pound"] = (0xA9, ""),        // upper-left triangle / shade-slashed (no lowercase glyph)
        ["UpArrow"] = (0xDE, "▒"),// pi / medium shade
    };

    // C=+punctuation graphics: the KERNAL C= table $EC03 PETSCII code plus the
    // lowercase-charset stand-in for the codes that differ between charsets.
    private static readonly Dictionary<string, (int Petscii, string Lowercase)> CbmSpecials = new()
    {
        ["@"] = (0xA4, "▁"),      // lower one eighth block
        ["*"] = (0xDF, ""),            // upper-right triangle / shade-slashed (no lowercase glyph)
        ["+"] = (0xA6, "▒"),      // medium shade
        ["-"] = (0xDC, ""),            // left-half shade (no lowercase glyph)
        ["Pound"] = (0xA8, ""),        // lower-half shade (no lowercase glyph)
        ["UpArrow"] = (0xDE, "▒"),// same $DE as SHIFT+UpArrow
    };

    // Keys whose C= chord emits the SAME code as SHIFT (KERNAL C= table rows
    // 3C/3E/3F/5B/5D, C=+9 = ')' and C=+STOP = $83 RUN): show the shifted legend.
    private static readonly HashSet<string> CbmMatchesShiftPrintable = new()
    {
        ",", ".", "/", ":", ";", "9", "RunStop",
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
            if (IsLetterKey(entry))
                return Graphic(CbmLetterPetscii[entry.KeyName]);
            if (CbmSpecials.TryGetValue(entry.KeyName, out var cbm))
                return lowercaseMode ? cbm.Lowercase : Graphic(cbm.Petscii);
            if (CbmMatchesShiftPrintable.Contains(entry.KeyName) && entry.ShiftedLabel is not null)
                return entry.ShiftedLabel;

            // C=+digit is a color control; nothing printable to preview.
            return entry.DisplayLabel;
        }

        if (shifted)
        {
            if (IsLetterKey(entry))
            {
                // Uppercase/graphics mode: the PETSCII right-keycap graphic ($C1-$DA).
                // Lowercase mode: those same codes ARE the plain capitals.
                return lowercaseMode ? entry.KeyName : Graphic(0xC1 + (entry.KeyName[0] - 'A'));
            }

            if (ShiftSpecials.TryGetValue(entry.KeyName, out var shift))
                return lowercaseMode ? shift.Lowercase : Graphic(shift.Petscii);

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

    /// <summary>The PetMe64 keycap glyph for a PETSCII code: <c>U+F100 + petscii</c>.</summary>
    private static string Graphic(int petscii) => ((char)(PetsciiGlyphBase + petscii)).ToString();

    private static bool IsLetterKey(VirtualKeyEntry entry)
        => entry.KeyName.Length == 1 && entry.KeyName[0] is >= 'A' and <= 'Z';
}
