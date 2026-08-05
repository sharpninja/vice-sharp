namespace ViceSharp.TestHarness.Xbox;

using System;
using System.IO;
using ViceSharp.Xbox.ViewModels;
using Xunit;

/// <summary>
/// FEAT-XKEYCAPSHIFT-001 (PLAN-XKEYBOARD-001 follow-up). Operator 2026-07-14: "When
/// holding SHIFT or C= modifiers, change the keycap to match the character to be
/// inserted." Structural wiring of the #if HAS_UWP head: the overlay swaps each keycap
/// to its ShiftedLabel while SHIFT is effective (trigger hold, SHIFT-LOCK latch, or a
/// momentary one-shot arm) and back when it clears.
/// </summary>
/// <remarks>
/// Acceptance:
///   TEST-XKEYCAP-001a (in XboxAuthenticKeyboardTests): the layout carries the exact
///     printable shifted legends of the physical keycap tops; graphics-producing keys
///     stay null (never a wrong glyph).
///   TEST-XKEYCAP-001b: the overlay exposes the shift-visual refresh (RefreshKeycaps /
///     SetExternalShift) and applies ShiftedLabel over DisplayLabel.
///   TEST-XKEYCAP-001c: the head drives the overlay's external-shift flag from the
///     trigger-modifier commands, and the overlay re-syncs after tile presses (latch /
///     momentary arm changes).
/// </remarks>
[Trait("Category", "Xbox")]
public sealed class XboxKeycapShiftTests
{
    [Fact]
    public void KeycapGlyphs_FollowTheMachineCharsetCase()
    {
        // FEAT-XKEYCAPCASE-001 (operator 2026-07-14: "virtual keyboard needs to know if
        // computer is in upper or lower case characters and use the appropriate
        // glyphs"): the C64 charset mode is VIC $D018's charset-base bit (bit 11 of the
        // character base: $1000 uppercase/graphics, $1800 lowercase/uppercase).
        Assert.False(VirtualKeycapGlyphs.IsLowercaseCharacterBase(0x1000));
        Assert.True(VirtualKeycapGlyphs.IsLowercaseCharacterBase(0x1800));

        var letter = new VirtualKeyEntry("A", "A", IsWide: false);

        // Uppercase/graphics mode: unshifted letters render uppercase; SHIFT shows the
        // TRUE PETSCII right-keycap graphic (FEAT-XKEYCAPPETSCII-001 superseded the
        // earlier keep-the-letter behavior; the glyph tests own the full table).
        Assert.Equal("A", VirtualKeycapGlyphs.For(letter, shifted: false, commodore: false, lowercaseMode: false));
        // SHIFT+A is PETSCII $C1 (spade); the keycap draws the PetMe64 glyph at U+F100+$C1.
        Assert.Equal(((char)0xF1C1).ToString(), VirtualKeycapGlyphs.For(letter, shifted: true, commodore: false, lowercaseMode: false));

        // Lowercase/uppercase mode: unshifted types lowercase, shifted types uppercase.
        Assert.Equal("a", VirtualKeycapGlyphs.For(letter, shifted: false, commodore: false, lowercaseMode: true));
        Assert.Equal("A", VirtualKeycapGlyphs.For(letter, shifted: true, commodore: false, lowercaseMode: true));

        // Non-letter keys keep the FEAT-XKEYCAPSHIFT behavior in both modes.
        var digit = new VirtualKeyEntry("1", "1", IsWide: false, ShiftedLabel: "!");
        Assert.Equal("1", VirtualKeycapGlyphs.For(digit, shifted: false, commodore: false, lowercaseMode: true));
        Assert.Equal("!", VirtualKeycapGlyphs.For(digit, shifted: true, commodore: false, lowercaseMode: true));

        var wide = new VirtualKeyEntry("Return", "RETURN", IsWide: true);
        Assert.Equal("RETURN", VirtualKeycapGlyphs.For(wide, shifted: true, commodore: false, lowercaseMode: true));
    }

    [Fact]
    public void Head_PollsTheCharsetCase_IntoTheKeycaps()
    {
        // FEAT-XKEYCAPCASE-001 structural: the host/facade expose the live charset case
        // (from the VIC character base) and the overlay polls it while visible.
        var host = ReadLower("src", "ViceSharp.Host.InProcess", "Runtime", "ConsoleEmulatorHost.cs");
        Assert.Contains("getcharsetlowercase", host);
        Assert.Contains("characterbase", host);

        var facade = ReadLower("src", "ViceSharp.Xbox", "Platform", "InProcessSessionFacade.cs");
        Assert.Contains("getcharsetlowercase", facade);

        var overlay = ReadLower("src", "ViceSharp.Xbox", "Controls", "VirtualKeyboardOverlay.xaml.cs");
        Assert.Contains("ischarsetlowercase", overlay);
        Assert.Contains("virtualkeycapglyphs.for", overlay);

        var app = ReadLower("src", "ViceSharp.Xbox", "App.xaml.cs");
        Assert.Contains("getcharsetlowercase", app);
    }

    [Fact]
    public void Overlay_SwapsKeycaps_WithTheShiftVisual()
    {
        var overlay = ReadLower("src", "ViceSharp.Xbox", "Controls", "VirtualKeyboardOverlay.xaml.cs");
        Assert.Contains("refreshkeycaps", overlay);
        Assert.Contains("setexternalshift", overlay);

        // The shifted-legend selection lives in the shared glyph helper now
        // (FEAT-XKEYCAPCASE-001 composes it with the charset case).
        Assert.Contains("virtualkeycapglyphs.for", overlay);
    }

    [Fact]
    public void Head_DrivesTheOverlayShiftVisual_FromTheTriggerModifiers()
    {
        var app = ReadLower("src", "ViceSharp.Xbox", "App.xaml.cs");
        Assert.Contains("setexternalshift(true)", app);
        Assert.Contains("setexternalshift(false)", app);
    }

    private static string ReadLower(params string[] parts)
    {
        var path = Path.Combine(RepoRoot, Path.Combine(parts));
        Assert.True(File.Exists(path), $"Expected source file at '{path}'.");
        return File.ReadAllText(path).ToLowerInvariant();
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
