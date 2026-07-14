namespace ViceSharp.TestHarness.Xbox;

using System;
using System.Collections.Generic;
using System.Linq;
using ViceSharp.Abstractions;
using ViceSharp.Core.Input;
using ViceSharp.Xbox.ViewModels;
using Xunit;

/// <summary>
/// PLAN-XKEYBOARD-001 slice K1 (portable model), areas XBOXUI/XKBD, FR-XBOXUI-006 /
/// FR-XKBD-001. The virtual keyboard becomes a REAL C64 keyboard: the authentic 66-key
/// grid (five physical rows plus the four-key function column that sits on the RIGHT of
/// the physical machine), true key widths, momentary SHIFT keys, and the same
/// machine-resolvable key names as before.
/// </summary>
/// <remarks>
/// <para>
/// Operator constraints (2026-07-14): "UWP Virtual Keyboard should be a real C64
/// keyboard"; function keys move to a column on the RIGHT; the keyboard will slide up
/// from the bottom and SHRINK the emulator (head slice K2) - this slice is the portable
/// layout/behavior model only.
/// </para>
/// <para>
/// Physical reference (66 keys): row 1 = left-arrow 1..0 + - pound CLR/HOME INST/DEL (16);
/// row 2 = CTRL Q..P @ * up-arrow RESTORE (15); row 3 = RUN/STOP SHIFT-LOCK A..L : ; =
/// RETURN (15); row 4 = C= SHIFT Z..M , . / SHIFT CRSR-down CRSR-right (15); row 5 =
/// SPACE (1); function column = F1 F3 F5 F7 (4).
/// </para>
/// Acceptance:
///   TEST-XKBD-K1a: the authentic layout has exactly those rows, the right-side function
///     column, and 66 tiles total in the controller-focus index space.
///   TEST-XKBD-K1b: every tile's key name (except the RESTORE seam tile) resolves in the
///     REAL C64KeyboardMap, including the shifted function twins.
///   TEST-XKBD-K1c: authentic widths: RETURN and SPACE are wider than one unit; SPACE is
///     the widest tile.
///   TEST-XKBD-K1d: momentary SHIFT tiles arm a one-shot: the next ordinary key is
///     wrapped in that shift's down/up (giving CRSR-up/left and shifted glyphs exactly
///     like hardware), then the arm clears; function tiles map to their shifted twin
///     in place while armed; the SHIFT-LOCK latch behavior is unchanged.
/// </remarks>
[Trait("Category", "Xbox")]
public sealed class XboxAuthenticKeyboardTests
{
    [Fact]
    public void AuthenticLayout_HasThePhysicalRows_AndRightFunctionColumn()
    {
        var layout = VirtualKeyboardLayout.Default;

        // TEST-XKBD-K1a: five physical rows.
        Assert.Equal(5, layout.Rows.Count);
        Assert.Equal(new[] { 16, 15, 15, 15, 1 }, layout.Rows.Select(r => r.Count).ToArray());

        // Row anchors, exactly as on the machine.
        Assert.Equal("LeftArrow", layout.Rows[0][0].KeyName);
        Assert.Equal("Delete", layout.Rows[0][^1].KeyName);

        Assert.Equal("Ctrl", layout.Rows[1][0].KeyName);
        Assert.Equal(AppKeyKind.Restore, layout.Rows[1][^1].Kind);

        Assert.Equal("RunStop", layout.Rows[2][0].KeyName);
        Assert.Equal(AppKeyKind.ShiftLatch, layout.Rows[2][1].Kind);
        Assert.Equal("Return", layout.Rows[2][^1].KeyName);

        Assert.Equal("Commodore", layout.Rows[3][0].KeyName);
        Assert.Equal(AppKeyKind.ShiftMomentary, layout.Rows[3][1].Kind);
        Assert.Equal("LeftShift", layout.Rows[3][1].KeyName);
        Assert.Equal(AppKeyKind.ShiftMomentary, layout.Rows[3][^3].Kind);
        Assert.Equal("RightShift", layout.Rows[3][^3].KeyName);
        Assert.Equal("Down", layout.Rows[3][^2].KeyName);
        Assert.Equal("Right", layout.Rows[3][^1].KeyName);

        Assert.Equal("Space", layout.Rows[4][0].KeyName);

        // The function column: F1/F3/F5/F7, rendered on the RIGHT by the head.
        Assert.Equal(new[] { "F1", "F3", "F5", "F7" }, layout.FunctionKeys.Select(k => k.KeyName).ToArray());

        // 62 row tiles + 4 function tiles = the C64's 66 keys, all focusable.
        Assert.Equal(66, layout.AllKeys.Count);
        Assert.Equal(
            layout.Rows.SelectMany(r => r).Concat(layout.FunctionKeys).Select(k => k.KeyName).ToArray(),
            layout.AllKeys.Select(k => k.KeyName).ToArray());
    }

    [Fact]
    public void AuthenticLayout_EveryKeyName_ResolvesInTheRealMap()
    {
        // TEST-XKBD-K1b: same guarantee the S25 tests established, re-proven for the
        // authentic grid: the ViewModels layer hardcodes names; the REAL map must resolve
        // every one (except the RESTORE seam tile) or a tile would do nothing on hardware.
        var map = C64KeyboardMap.CreateDefaultFallback();

        var unresolved = VirtualKeyboardLayout.Default.AllKeys
            .Where(k => k.Kind != AppKeyKind.Restore)
            .Select(k => k.KeyName)
            .Concat(new[] { "F2", "F4", "F6", "F8" })
            .Where(name => !map.TryResolve(name, out _))
            .ToArray();

        Assert.Empty(unresolved);
    }

    [Fact]
    public void AuthenticLayout_HasTrueWidths()
    {
        var layout = VirtualKeyboardLayout.Default;
        var byName = layout.AllKeys.ToDictionary(k => k.KeyName + ":" + k.Kind, k => k);

        // TEST-XKBD-K1c: RETURN and SPACE are wider than a unit key; SPACE is the widest.
        var returnKey = layout.Rows[2][^1];
        var space = layout.Rows[4][0];
        Assert.True(returnKey.EffectiveWidthUnits > 1.0);
        Assert.True(space.EffectiveWidthUnits > returnKey.EffectiveWidthUnits);
        Assert.Equal(
            space.EffectiveWidthUnits,
            layout.AllKeys.Max(k => k.EffectiveWidthUnits));
    }

    [Fact]
    public void MomentaryShift_WrapsTheNextKey_ThenDisarms()
    {
        var spy = new AuthenticSpyKeyboard();
        var vm = new VirtualKeyboardViewModel(spy);
        var layout = vm.Layout;

        // TEST-XKBD-K1d: arm LEFT SHIFT (no emission), press CRSR-down -> hardware-true
        // shift wrap = CRSR-up; the arm then clears so the next press is plain.
        vm.Press(layout.Rows[3][1]);
        Assert.Empty(spy.KeyStates);
        Assert.True(vm.ShiftArmed);

        vm.Press(Single(layout, "Down"));
        Assert.Equal(
            new[] { ("LeftShift", true), ("Down", true), ("Down", false), ("LeftShift", false) },
            spy.KeyStates);
        Assert.False(vm.ShiftArmed);

        spy.KeyStates.Clear();
        vm.Press(Single(layout, "Down"));
        Assert.Equal(new[] { ("Down", true), ("Down", false) }, spy.KeyStates);
    }

    [Fact]
    public void MomentaryRightShift_WrapsWithRightShift()
    {
        var spy = new AuthenticSpyKeyboard();
        var vm = new VirtualKeyboardViewModel(spy);
        var layout = vm.Layout;

        vm.Press(layout.Rows[3][^3]);
        vm.Press(Single(layout, "Right"));

        Assert.Equal(
            new[] { ("RightShift", true), ("Right", true), ("Right", false), ("RightShift", false) },
            spy.KeyStates);
    }

    [Fact]
    public void MomentaryShift_MapsFunctionTwinInPlace_WithoutWrap()
    {
        var spy = new AuthenticSpyKeyboard();
        var vm = new VirtualKeyboardViewModel(spy);
        var layout = vm.Layout;

        // Armed shift + F1 = F2, emitted directly (the map resolves the twin name); no
        // modifier wrap needed, and the arm clears.
        vm.Press(layout.Rows[3][1]);
        vm.Press(Single(layout, "F1"));

        Assert.Equal(new[] { ("F2", true), ("F2", false) }, spy.KeyStates);
        Assert.False(vm.ShiftArmed);
    }

    private static VirtualKeyEntry Single(VirtualKeyboardLayout layout, string keyName)
        => layout.AllKeys.Single(k => k.KeyName == keyName && k.Kind == AppKeyKind.Key);

    private sealed class AuthenticSpyKeyboard : IMachineKeyboardInput
    {
        public DeviceId Id { get; } = new(0xF2);

        public string Name => "Authentic Keyboard Spy";

        public List<(string Key, bool Down)> KeyStates { get; } = [];

        public List<bool> RestoreStates { get; } = [];

        public void Reset()
        {
            KeyStates.Clear();
            RestoreStates.Clear();
        }

        public bool SetKeyState(string key, bool pressed)
        {
            KeyStates.Add((key, pressed));
            return true;
        }

        public bool SetRestoreState(bool pressed)
        {
            RestoreStates.Add(pressed);
            return true;
        }
    }
}
