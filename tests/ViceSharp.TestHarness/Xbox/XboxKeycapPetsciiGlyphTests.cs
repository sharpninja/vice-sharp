namespace ViceSharp.TestHarness.Xbox;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ViceSharp.Xbox.ViewModels;
using Xunit;

/// <summary>
/// FEAT-XKEYCAPPETSCII-001 (operator 2026-07-14: "Don't forget to map the font glyphs
/// to the keyboard", completing "When holding SHIFT or C= modifiers, change the keycap
/// to match the character to be inserted"). The keycaps now show the TRUE PETSCII
/// graphics for SHIFT and C= chords, mode-aware. Ground truth is the KERNAL keyboard
/// decode tables read from the 901227-03 ROM (unshifted $EB81, SHIFT $EBC2, C= $EC03):
/// SHIFT+letter emits PETSCII $C1-$DA alphabetically; C= chords emit the scattered
/// $A1-$BF codes (A=$B0, Q=$AB, I=$A2, ...). PETSCII to Unicode follows the classic
/// C64 recode tables (uppercase and lowercase sets), whose CUS points (U+F1xx) the
/// vendored PetMe64 face maps directly.
/// </summary>
/// <remarks>
/// FR: FR-XINPUT-006 (virtual keyboard keycaps mirror what typing inserts).
/// TR: TR-MVVM-001 (portable glyph selection, no UI types).
/// Use case: the player holds LT (C=) or RT (SHIFT) over the virtual keyboard; every
/// keycap swaps to the exact glyph the chord will put on the C64 screen in the
/// machine's ACTIVE charset case.
/// Acceptance:
///   TEST-XKEYGLYPH-001a: SHIFT+letter in uppercase/graphics mode shows the PETSCII
///     right-keycap graphic (card suits, circles, arcs, diagonals) for all 26 letters.
///   TEST-XKEYGLYPH-001b: C=+letter shows the PETSCII left-keycap graphic (corners,
///     tees, partial blocks) for all 26 letters, in BOTH charset modes.
///   TEST-XKEYGLYPH-001c: lowercase mode: SHIFT+letter is the plain uppercase letter
///     (that is what typing inserts), unshifted follows the case poll.
///   TEST-XKEYGLYPH-001d: the graphic punctuation chords follow the mode-aware tables
///     (SHIFT+UpArrow pi vs shade, SHIFT+Pound triangle vs slashed shade, C=+* etc);
///     C= chords that emit the same printable as SHIFT (, . / : ; 9) show the printed
///     shifted legend; C=+digit color chords keep the base legend.
///   TEST-XKEYGLYPH-001e: EVERY glyph the keycaps can ever display resolves in the
///     vendored PetMe64.ttf cmap (parsed from the TTF, formats 4 and 12): no tofu on
///     device.
/// </remarks>
[Trait("Category", "Xbox")]
public sealed class XboxKeycapPetsciiGlyphTests
{
    // TEST-XKEYGLYPH-001a: ROM receipt: SHIFT table $EBC2 maps letters to $C1-$DA
    // ($C1 spade, $D3 heart, $D8 club, $DA diamond, $D1 black circle, $D7 white circle,
    // $C9/$CA/$CB/$D5 arcs, $CD/$CE diagonals). The keycap draws the PETSCII glyph the
    // vendored PetMe64 face maps at U+F100+code (Glyph), so the render is the exact C64
    // graphic - not a Unicode look-alike, and never the punctuation the CUS codes used to
    // point at.
    [Theory]
    [InlineData("A", 0xC1)] // spade
    [InlineData("S", 0xD3)] // heart
    [InlineData("X", 0xD8)] // club
    [InlineData("Z", 0xDA)] // diamond
    [InlineData("Q", 0xD1)] // black circle
    [InlineData("W", 0xD7)] // white circle
    [InlineData("U", 0xD5)] // arc down-right
    [InlineData("I", 0xC9)] // arc down-left
    [InlineData("J", 0xCA)] // arc up-right
    [InlineData("K", 0xCB)] // arc up-left
    [InlineData("M", 0xCD)] // diagonal upper-left to lower-right
    [InlineData("N", 0xCE)] // diagonal upper-right to lower-left
    [InlineData("V", 0xD6)] // diagonal cross
    [InlineData("B", 0xC2)] // vertical line
    [InlineData("C", 0xC3)] // horizontal line
    [InlineData("D", 0xC4)] // was blank/wrong before the U+F1xx fix
    [InlineData("Y", 0xD9)] // was blank/wrong before the U+F1xx fix
    public void ShiftedLetter_UppercaseMode_ShowsPetsciiRightGraphic(string key, int petscii)
    {
        var entry = Find(key);
        Assert.Equal(Glyph(petscii), VirtualKeycapGlyphs.For(entry, shifted: true, commodore: false, lowercaseMode: false));
    }

    // TEST-XKEYGLYPH-001b: ROM receipt: C= table $EC03: A=$B0, S=$AE, Z=$AD, X=$BD
    // (the four box corners), Q=$AB, W=$B3, E=$B1, R=$B2 (the tees), I=$A2, K=$A1.
    [Theory]
    [InlineData("A", 0xB0)] // down-right corner
    [InlineData("S", 0xAE)] // down-left corner
    [InlineData("Z", 0xAD)] // up-right corner
    [InlineData("X", 0xBD)] // up-left corner
    [InlineData("Q", 0xAB)] // vertical and right
    [InlineData("W", 0xB3)] // vertical and left
    [InlineData("E", 0xB1)] // up and horizontal
    [InlineData("R", 0xB2)] // down and horizontal
    [InlineData("I", 0xA2)] // lower half block
    [InlineData("K", 0xA1)] // left half block
    [InlineData("T", 0xA3)] // upper one eighth block
    [InlineData("G", 0xA5)] // left one eighth block
    [InlineData("B", 0xBF)] // was digit '8' before the U+F1xx fix
    [InlineData("N", 0xAA)] // was digit '0' before the U+F1xx fix
    public void CommodoreLetter_ShowsPetsciiLeftGraphic_BothModes(string key, int petscii)
    {
        var entry = Find(key);
        Assert.Equal(Glyph(petscii), VirtualKeycapGlyphs.For(entry, shifted: false, commodore: true, lowercaseMode: false));
        Assert.Equal(Glyph(petscii), VirtualKeycapGlyphs.For(entry, shifted: false, commodore: true, lowercaseMode: true));
    }

    /// <summary>All 26 letters carry a non-letter graphic for both chords in uppercase mode.</summary>
    [Fact]
    public void AllLetters_HaveGraphicsForBothChords()
    {
        foreach (var c in "ABCDEFGHIJKLMNOPQRSTUVWXYZ")
        {
            var entry = Find(c.ToString());
            var shiftGlyph = VirtualKeycapGlyphs.For(entry, shifted: true, commodore: false, lowercaseMode: false);
            var cbmGlyph = VirtualKeycapGlyphs.For(entry, shifted: false, commodore: true, lowercaseMode: false);

            Assert.False(shiftGlyph == entry.KeyName, $"SHIFT+{c} must show a graphic, not the letter.");
            Assert.False(cbmGlyph == entry.KeyName, $"C=+{c} must show a graphic, not the letter.");
            Assert.NotEqual(shiftGlyph, cbmGlyph);
        }
    }

    // TEST-XKEYGLYPH-001c.
    [Fact]
    public void LowercaseMode_ShiftedLetter_IsUppercaseLetter()
    {
        var entry = Find("A");
        Assert.Equal("a", VirtualKeycapGlyphs.For(entry, shifted: false, commodore: false, lowercaseMode: true));
        Assert.Equal("A", VirtualKeycapGlyphs.For(entry, shifted: true, commodore: false, lowercaseMode: true));
    }

    // TEST-XKEYGLYPH-001d: uppercase-mode punctuation chords draw the exact PETSCII glyph
    // (U+F100+code) from the KERNAL SHIFT $EBC2 / C= $EC03 tables. This is the fix for the
    // "C= line very wrong" report: the codes were previously punctuation/digit PETSCII.
    [Theory]
    [InlineData("UpArrow", true, false, 0xDE)]  // SHIFT+^ : pi
    [InlineData("UpArrow", false, true, 0xDE)]  // C=+^ : same $DE
    [InlineData("Pound", true, false, 0xA9)]    // SHIFT+pound : upper-left triangle
    [InlineData("Pound", false, true, 0xA8)]    // C=+pound : lower-half shade
    [InlineData("@", true, false, 0xBA)]        // SHIFT+@ : eighth block up-left
    [InlineData("@", false, true, 0xA4)]        // C=+@ : lower one eighth block
    [InlineData("*", true, false, 0xC0)]        // SHIFT+* : horizontal line
    [InlineData("*", false, true, 0xDF)]        // C=+* : filled triangle
    [InlineData("+", true, false, 0xDB)]        // SHIFT++ : cross
    [InlineData("+", false, true, 0xA6)]        // C=++ : medium shade
    [InlineData("-", true, false, 0xDD)]        // SHIFT+- : vertical line
    [InlineData("-", false, true, 0xDC)]        // C=+- : left-half shade
    public void PunctuationChords_UppercaseMode_ShowPetsciiGraphic(string key, bool shifted, bool commodore, int petscii)
    {
        var entry = Find(key);
        Assert.Equal(Glyph(petscii), VirtualKeycapGlyphs.For(entry, shifted, commodore, lowercaseMode: false));
    }

    // TEST-XKEYGLYPH-001d (lowercase): the charset-dependent codes keep a lowercase stand-in
    // on the single-charset PetMe64 face; the shade-slashed CUS ones have no lowercase glyph.
    [Theory]
    [InlineData("UpArrow", true, false, "▒")]   // SHIFT+^ lc: medium shade
    [InlineData("@", true, false, "✓")]          // SHIFT+@ lc: check mark
    [InlineData("Pound", true, false, "")]       // SHIFT+pound lc: no lowercase graphic
    [InlineData("*", false, true, "")]           // C=+* lc: no lowercase graphic
    public void PunctuationChords_LowercaseStandins(string key, bool shifted, bool commodore, string expected)
    {
        var entry = Find(key);
        Assert.Equal(expected, VirtualKeycapGlyphs.For(entry, shifted, commodore, lowercaseMode: true));
    }

    /// <summary>The PetMe64 keycap glyph for a PETSCII code: U+F100+code (the font's PETSCII PUA range).</summary>
    private static string Glyph(int petscii) => ((char)(0xF100 + petscii)).ToString();

    /// <summary>C= chords that emit the same printable as SHIFT show the printed legend; C=+digit color chords keep the digit.</summary>
    [Fact]
    public void CommodorePrintables_AndColorDigits_KeepSaneLegends()
    {
        // ROM receipt: C= table rows for , . / : ; match the SHIFT printables, and
        // C=+9 emits ')' exactly like SHIFT+9.
        Assert.Equal("<", VirtualKeycapGlyphs.For(Find(","), shifted: false, commodore: true, lowercaseMode: false));
        Assert.Equal(">", VirtualKeycapGlyphs.For(Find("."), shifted: false, commodore: true, lowercaseMode: false));
        Assert.Equal("?", VirtualKeycapGlyphs.For(Find("/"), shifted: false, commodore: true, lowercaseMode: false));
        Assert.Equal("[", VirtualKeycapGlyphs.For(Find(":"), shifted: false, commodore: true, lowercaseMode: false));
        Assert.Equal("]", VirtualKeycapGlyphs.For(Find(";"), shifted: false, commodore: true, lowercaseMode: false));
        Assert.Equal(")", VirtualKeycapGlyphs.For(Find("9"), shifted: false, commodore: true, lowercaseMode: false));

        // C=+1..8 emit color control codes: nothing printable to preview.
        Assert.Equal("1", VirtualKeycapGlyphs.For(Find("1"), shifted: false, commodore: true, lowercaseMode: false));
        Assert.Equal("8", VirtualKeycapGlyphs.For(Find("8"), shifted: false, commodore: true, lowercaseMode: false));

        // Non-key tiles never chord.
        var restore = Layout.AllKeys.First(k => k.KeyName == "Restore");
        Assert.Equal(restore.DisplayLabel, VirtualKeycapGlyphs.For(restore, shifted: true, commodore: true, lowercaseMode: false));
    }

    // TEST-XKEYGLYPH-001e: no tofu: every reachable keycap string resolves in the
    // vendored PetMe64 cmap.
    [Fact]
    public void EveryReachableKeycapGlyph_ResolvesInPetMe64()
    {
        var mapped = ReadPetMe64MappedCodepoints();
        Assert.True(mapped.Count > 1000, "sanity: the PetMe64 cmap parse found implausibly few codepoints.");

        var states = new (bool Shifted, bool Commodore, bool Lowercase)[]
        {
            (false, false, false), (false, false, true),
            (true, false, false), (true, false, true),
            (false, true, false), (false, true, true),
        };

        foreach (var entry in Layout.AllKeys)
        {
            foreach (var (shifted, commodore, lowercase) in states)
            {
                var glyph = VirtualKeycapGlyphs.For(entry, shifted, commodore, lowercase);
                foreach (var rune in glyph.EnumerateRunes())
                {
                    // Newlines in two-line legends (RUN\nSTOP, SHIFT\nLOCK) are line
                    // breaks the TextBlock lays out, not glyphs the font must map.
                    if (rune.Value == '\n')
                        continue;

                    Assert.True(
                        mapped.Contains(rune.Value),
                        $"Keycap '{entry.KeyName}' (shift={shifted} cbm={commodore} lc={lowercase}) " +
                        $"shows U+{rune.Value:X4} which PetMe64.ttf does not map.");
                }
            }
        }
    }

    private static readonly VirtualKeyboardLayout Layout = VirtualKeyboardLayout.CreateAuthentic();

    private static VirtualKeyEntry Find(string keyName) =>
        Layout.AllKeys.First(k => k.KeyName == keyName && k.Kind == AppKeyKind.Key);

    /// <summary>Parses the vendored PetMe64.ttf cmap (subtable formats 4 and 12) into the set of mapped codepoints.</summary>
    private static HashSet<int> ReadPetMe64MappedCodepoints()
    {
        var path = Path.Combine(RepoRoot, "src", "ViceSharp.Xbox", "Assets", "Fonts", "PetMe64.ttf");
        var data = File.ReadAllBytes(path);
        var points = new HashSet<int>();

        static int U16(byte[] d, int o) => (d[o] << 8) | d[o + 1];
        static long U32(byte[] d, int o) =>
            ((long)d[o] << 24) | ((long)d[o + 1] << 16) | ((long)d[o + 2] << 8) | d[o + 3];

        var numTables = U16(data, 4);
        var cmapOffset = -1L;
        for (var i = 0; i < numTables; i++)
        {
            var record = 12 + 16 * i;
            var tag = System.Text.Encoding.ASCII.GetString(data, record, 4);
            if (tag == "cmap")
                cmapOffset = U32(data, record + 8);
        }

        Assert.True(cmapOffset >= 0, "PetMe64.ttf has no cmap table.");

        var encodingCount = U16(data, (int)cmapOffset + 2);
        for (var e = 0; e < encodingCount; e++)
        {
            var record = (int)cmapOffset + 4 + 8 * e;
            var subtable = (int)(cmapOffset + U32(data, record + 4));
            var format = U16(data, subtable);
            if (format == 4)
            {
                var segCountX2 = U16(data, subtable + 6);
                var endBase = subtable + 14;
                var startBase = endBase + segCountX2 + 2;
                for (var s = 0; s < segCountX2 / 2; s++)
                {
                    var end = U16(data, endBase + 2 * s);
                    var start = U16(data, startBase + 2 * s);
                    if (start == 0xFFFF)
                        continue;
                    for (var c = start; c <= end; c++)
                        points.Add(c);
                }
            }
            else if (format == 12)
            {
                var groups = U32(data, subtable + 12);
                for (long g = 0; g < groups; g++)
                {
                    var groupOffset = subtable + 16 + 12 * (int)g;
                    var start = U32(data, groupOffset);
                    var end = U32(data, groupOffset + 4);
                    for (var c = start; c <= end; c++)
                        points.Add((int)c);
                }
            }
        }

        return points;
    }

    private static string RepoRoot
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "ViceSharp.slnx")))
                directory = directory.Parent;

            if (directory is null)
                throw new InvalidOperationException("Could not locate repository root.");

            return directory.FullName;
        }
    }
}
