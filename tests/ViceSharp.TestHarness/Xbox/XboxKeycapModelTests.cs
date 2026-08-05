namespace ViceSharp.TestHarness.Xbox;

using System.Linq;
using ViceSharp.Xbox.ViewModels;
using Xunit;

/// <summary>
/// FEAT-XKEYCAPMODEL-001 (operator 2026-07-18: "keycap size, shape and color should match
/// the exact model of computer being emulated"; and the special keys "RESTORE, RETURN,
/// SPACE and CONTROL don't match up to the virtual keyboard"). Covers the authentic
/// MechBoard64 key widths, the function-cap tagging the per-model skin uses, and the
/// profile -> <see cref="KeycapSkin"/> resolver.
/// </summary>
[Trait("Category", "Xbox")]
public sealed class XboxKeycapModelTests
{
    // Authentic widths: CTRL, RESTORE and the F-keys are 1.5u (MechBoard64); RUN/STOP,
    // SHIFT-LOCK and C= are 1u, RETURN is 1.5u, and the two bottom-row SHIFTs are 1.25u, so
    // row 3 (15.5u) and row 4 (15.5u) finish slightly LEFT of the full-width rows 1-2 (16u)
    // (operator 2026-07-18); SPACE is 9u; ordinary keys 1u.
    [Theory]
    [InlineData("Ctrl", 1.5)]
    [InlineData("RunStop", 1.0)]
    [InlineData("Commodore", 1.0)]
    [InlineData("LeftShift", 1.25)]
    [InlineData("RightShift", 1.25)]
    [InlineData("Restore", 1.5)]
    [InlineData("Return", 1.5)]
    [InlineData("Space", 9.0)]
    [InlineData("F1", 1.5)]
    [InlineData("F7", 1.5)]
    [InlineData("A", 1.0)]
    [InlineData("1", 1.0)]
    public void SpecialKeys_UseAuthenticWidths(string keyName, double expectedUnits)
    {
        var key = VirtualKeyboardLayout.Default.AllKeys.First(k => k.KeyName == keyName);
        Assert.Equal(expectedUnits, key.EffectiveWidthUnits);
    }

    [Fact]
    public void ShiftLock_IsAOneUnitLatch()
    {
        var shiftLock = VirtualKeyboardLayout.Default.AllKeys.Single(k => k.Kind == AppKeyKind.ShiftLatch);
        Assert.Equal(1.0, shiftLock.EffectiveWidthUnits);
    }

    // The right-edge stagger (operator 2026-07-18): the number and QWERTY rows are the full
    // width (16u) and line up on the right; the home and bottom rows finish slightly LEFT
    // of them (15.5u) and line up with each other, as on the real machine.
    [Fact]
    public void RowWidths_Rows1And2AreWidest_Rows3And4Inset()
    {
        var rowUnits = VirtualKeyboardLayout.Default.Rows
            .Select(row => row.Sum(k => k.EffectiveWidthUnits))
            .ToArray();

        Assert.Equal(16.0, rowUnits[0]);   // number row
        Assert.Equal(16.0, rowUnits[1]);   // QWERTY row
        Assert.Equal(15.5, rowUnits[2]);   // home row (RETURN), inset
        Assert.Equal(15.5, rowUnits[3]);   // bottom row (SHIFT/cursor), inset and aligned to row 3
        Assert.True(rowUnits[0] > rowUnits[2], "rows 1-2 must be wider than rows 3-4");
    }

    [Fact]
    public void OnlyTheFunctionKeys_AreTaggedFunctionCaps()
    {
        var functionCaps = VirtualKeyboardLayout.Default.AllKeys
            .Where(k => k.IsFunctionCap)
            .Select(k => k.KeyName)
            .OrderBy(n => n)
            .ToArray();

        Assert.Equal(new[] { "F1", "F3", "F5", "F7" }, functionCaps);
    }

    // "reconcile with real C64 keyboard": the legend fits the longest line to the CAP WIDTH,
    // so single-char legends stay large and multi-char legends (INST/DEL on a 1u cap, RESTORE
    // on a 1.5u cap) shrink to fit instead of clipping.
    [Theory]
    [InlineData("A", 1.0, 22.0)]          // single char, clamped large
    [InlineData("£", 1.0, 22.0)]
    [InlineData("INST\nDEL", 1.0, 10.0)]  // 4-char line on a 1u cap
    [InlineData("CTRL", 1.5, 15.0)]       // 4 chars on 1.5u
    [InlineData("RESTORE", 1.5, 9.0)]     // 7 chars on 1.5u, at the floor
    [InlineData("RETURN", 2.0, 14.0)]     // 6 chars on 2u
    [InlineData("SPACE", 9.0, 22.0)]      // wide bar, clamped large
    public void DisplayFontSize_FitsLongestLineToCapWidth(string label, double units, double expected)
        => Assert.Equal(expected, new VirtualKeyEntry(label, label, IsWide: units > 1, WidthUnits: units).DisplayFontSize);

    [Theory]
    [InlineData("c64c", null, KeycapSkin.C64C)]
    [InlineData("c64", "C64C PAL", KeycapSkin.C64C)]
    [InlineData("sx64", "SX-64", KeycapSkin.Sx64)]
    [InlineData("c64gs", "C64 Game System", KeycapSkin.C64Gs)]
    [InlineData(null, "Game System", KeycapSkin.C64Gs)]
    [InlineData("c64", "C64 PAL", KeycapSkin.Breadbin)]
    [InlineData("breadbox", "C64 breadbin", KeycapSkin.Breadbin)]
    [InlineData(null, null, KeycapSkin.Breadbin)]
    [InlineData("", "", KeycapSkin.Breadbin)]
    public void Resolver_PicksSkinFromProfileTokens(string? id, string? name, KeycapSkin expected)
        => Assert.Equal(expected, KeycapSkinResolver.Resolve(id, name));
}
