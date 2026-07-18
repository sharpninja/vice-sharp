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
    // MechBoard64 spec: most keys 1u; CTRL/SHIFT/RESTORE/RUN-STOP/SHIFT-LOCK/C=/F-keys 1.5u;
    // RETURN 2u; SPACE 9u.
    [Theory]
    [InlineData("Ctrl", 1.5)]
    [InlineData("RunStop", 1.5)]
    [InlineData("Commodore", 1.5)]
    [InlineData("LeftShift", 1.5)]
    [InlineData("RightShift", 1.5)]
    [InlineData("Restore", 1.5)]
    [InlineData("Return", 2.0)]
    [InlineData("Space", 9.0)]
    [InlineData("F1", 1.5)]
    [InlineData("F7", 1.5)]
    [InlineData("A", 1.0)]
    [InlineData("1", 1.0)]
    public void SpecialKeys_UseAuthenticMechBoard64Widths(string keyName, double expectedUnits)
    {
        var key = VirtualKeyboardLayout.Default.AllKeys.First(k => k.KeyName == keyName);
        Assert.Equal(expectedUnits, key.EffectiveWidthUnits);
    }

    [Fact]
    public void ShiftLock_IsAOneAndAHalfUnitLatch()
    {
        var shiftLock = VirtualKeyboardLayout.Default.AllKeys.Single(k => k.Kind == AppKeyKind.ShiftLatch);
        Assert.Equal(1.5, shiftLock.EffectiveWidthUnits);
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

    // "reconcile with real C64 keyboard": single-char legends stay large; multi-char
    // legends shrink to their longest line so RESTORE/RETURN/CLR-HOME fit instead of clipping.
    [Theory]
    [InlineData("A", 24.0)]
    [InlineData("£", 24.0)]
    [InlineData("C=", 18.0)]
    [InlineData("CTRL", 14.0)]
    [InlineData("SHIFT", 14.0)]
    [InlineData("RETURN", 11.0)]
    [InlineData("RESTORE", 11.0)]
    [InlineData("CLR\nHOME", 14.0)]
    [InlineData("RUN\nSTOP", 14.0)]
    public void DisplayFontSize_ScalesToLongestLabelLine(string label, double expected)
        => Assert.Equal(expected, new VirtualKeyEntry(label, label, IsWide: false).DisplayFontSize);

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
