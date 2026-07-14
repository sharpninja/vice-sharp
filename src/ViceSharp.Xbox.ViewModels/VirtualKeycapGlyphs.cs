namespace ViceSharp.Xbox.ViewModels;

/// <summary>
/// FEAT-XKEYCAPCASE-001 (operator 2026-07-14: "virtual keyboard needs to know if
/// computer is in upper or lower case characters and use the appropriate glyphs").
/// Pure keycap-glyph selection for the virtual keyboard: composes the machine's ACTIVE
/// charset case (the VIC's character-base bit: <c>$1000</c> uppercase/graphics,
/// <c>$1800</c> lowercase/uppercase) with the effective SHIFT state
/// (FEAT-XKEYCAPSHIFT-001).
/// </summary>
/// <remarks>
/// Letters follow the machine: uppercase/graphics mode always shows "A" (its shifted
/// glyphs are PETSCII graphics, which the keycaps do not fake); lowercase mode shows
/// "a" unshifted and "A" shifted, exactly what typing inserts. Non-letter keys keep
/// their printed shifted legends. Portable (System only, TR-MVVM-001).
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

    /// <summary>
    /// The glyph a keycap shows for the current machine charset case and effective
    /// SHIFT state.
    /// </summary>
    /// <param name="entry">The keycap's layout entry.</param>
    /// <param name="shifted">Whether SHIFT is effective (trigger hold / latch / one-shot).</param>
    /// <param name="lowercaseMode">Whether the machine runs the lowercase charset.</param>
    /// <returns>The display string for the keycap.</returns>
    public static string For(VirtualKeyEntry entry, bool shifted, bool lowercaseMode)
    {
        if (entry.Kind != AppKeyKind.Key)
            return entry.DisplayLabel;

        if (shifted && entry.ShiftedLabel is not null)
            return entry.ShiftedLabel;

        // Single letters follow the machine's charset case.
        if (entry.KeyName.Length == 1 && entry.KeyName[0] is >= 'A' and <= 'Z')
        {
            return lowercaseMode && !shifted
                ? char.ToLowerInvariant(entry.KeyName[0]).ToString()
                : entry.KeyName;
        }

        return entry.DisplayLabel;
    }
}
