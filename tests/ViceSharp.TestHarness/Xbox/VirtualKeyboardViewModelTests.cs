namespace ViceSharp.TestHarness.Xbox;

using System;
using System.Collections.Generic;
using System.Linq;
using ViceSharp.Abstractions;
using ViceSharp.Core.Input;
using ViceSharp.Xbox.ViewModels;
using Xunit;

/// <summary>
/// PLAN-XBOXUWP slice S25 (IMPL-XBOXUWP-025), area XBOXUI/XKBD. TEST-XBOXUI-006 (with
/// the RESTORE-tile leg of TEST-XKBD-001): the on-screen, controller-navigable virtual
/// C64 keyboard model in <c>ViceSharp.Xbox.ViewModels</c>
/// (<see cref="VirtualKeyboardViewModel"/> + <see cref="VirtualKeyboardLayout"/> +
/// <see cref="VirtualKeyEntry"/>).
/// </summary>
/// <remarks>
/// <para>
/// The ViewModels project cannot reference the engine (Core/Chips/Architectures), so it
/// HARDCODES the exact <c>SetKeyState</c> strings. This test project DOES reference Core,
/// so it validates every emitted key name against the REAL
/// <see cref="C64KeyboardMap"/> resolvable set (the same map the running machine uses),
/// proving the hardcoded strings can never drift from what the machine resolves.
/// </para>
/// <para>
/// Representation under test (as documented on the source types):
/// <list type="bullet">
///   <item><description>Ordinary matrix keys are <see cref="AppKeyKind.Key"/>: pressing
///   emits <c>SetKeyState(resolvedKey, true)</c> then <c>false</c>, where
///   <c>resolvedKey</c> applies the shift-latch (F1-&gt;F2, F3-&gt;F4, F5-&gt;F6,
///   F7-&gt;F8 in place).</description></item>
///   <item><description>The Shift tile is <see cref="AppKeyKind.ShiftLatch"/>: pressing
///   it TOGGLES <see cref="VirtualKeyboardViewModel.ShiftLatched"/> and emits NO key. The
///   latch is a true C64 SHIFT-LOCK style latch: it PERSISTS across key presses until
///   toggled off.</description></item>
///   <item><description>The RESTORE tile is <see cref="AppKeyKind.Restore"/>: pressing it
///   drives the dedicated RESTORE/NMI seam <see cref="IMachineKeyboardInput.SetRestoreState(bool)"/>
///   (true then false) and NEVER <see cref="IMachineKeyboardInput.SetKeyState(string, bool)"/>.
///   This is the ONLY tile whose <c>KeyName</c> is not a C64KeyboardMap key.</description></item>
/// </list>
/// </para>
/// </remarks>
[Trait("Category", "Xbox")]
public sealed class VirtualKeyboardViewModelTests
{
    /// <summary>
    /// FR-XBOXUI-006, TR-XBOXUI-006, TEST-XBOXUI-006 (FEAT-XKBDSTICKY-001: strokes are
    /// now held across real scan time).
    /// Use case: the couch UI presses the on-screen RETURN tile to send a carriage return
    /// to the running C64, which must arrive as the exact map key "Return" and stay DOWN
    /// until the stroke completes so the KERNAL matrix scan sees it.
    /// Acceptance: pressing the RETURN tile emits exactly <c>SetKeyState("Return", true)</c>;
    /// <see cref="VirtualKeyboardViewModel.CompletePress"/> then emits
    /// <c>SetKeyState("Return", false)</c>; no other keyboard call is made.
    /// </summary>
    [Fact]
    public void PressReturnTile_EmitsReturnDownThenUp_AndNothingElse()
    {
        var spy = new SpyKeyboardInput();
        var vm = new VirtualKeyboardViewModel(spy);

        vm.Press(SingleByKeyName(vm, "Return"));
        Assert.Equal(new[] { ("Return", true) }, spy.KeyStates);

        vm.CompletePress();

        Assert.Equal(new[] { ("Return", true), ("Return", false) }, spy.KeyStates);
        Assert.Empty(spy.RestoreStates);
    }

    /// <summary>
    /// FR-XBOXUI-006, TR-XBOXUI-006, TEST-XBOXUI-006.
    /// Use case: on a C64 the shifted function keys F2/F4/F6/F8 share a physical key with
    /// F1/F3/F5/F7; with the shift-latch engaged, pressing a function tile must emit the
    /// shifted twin in place rather than the base key.
    /// Acceptance: with <see cref="VirtualKeyboardViewModel.ShiftLatched"/> true, pressing
    /// the F1 tile emits <c>SetKeyState("F2", true/false)</c>; F3-&gt;"F4"; F5-&gt;"F6";
    /// F7-&gt;"F8"; and no RESTORE call is made.
    /// </summary>
    [Theory]
    [InlineData("F1", "F2")]
    [InlineData("F3", "F4")]
    [InlineData("F5", "F6")]
    [InlineData("F7", "F8")]
    public void ShiftLatched_PressingFunctionTile_EmitsShiftedTwinInPlace(string baseKey, string shifted)
    {
        var spy = new SpyKeyboardInput();

        // FEAT-XKBDSTICKY-001: engaging the latch holds the LeftShift matrix line.
        var vm = new VirtualKeyboardViewModel(spy) { ShiftLatched = true };

        vm.Press(SingleByKeyName(vm, baseKey));
        vm.CompletePress();

        Assert.Equal(
            new[] { ("LeftShift", true), (shifted, true), (shifted, false) },
            spy.KeyStates);
        Assert.Empty(spy.RestoreStates);
    }

    /// <summary>
    /// FR-XBOXUI-006, TR-XBOXUI-006, TEST-XBOXUI-006.
    /// Use case: because the ViewModels layer hardcodes its <c>SetKeyState</c> strings
    /// (it cannot reference the engine), every tile's key name must be one the real C64
    /// keyboard map resolves, or the tile would silently do nothing on hardware.
    /// Acceptance: for every default-layout <see cref="VirtualKeyEntry"/> EXCEPT the
    /// single RESTORE tile, <see cref="C64KeyboardMap.TryResolve"/> succeeds; the shifted
    /// function twins F2/F4/F6/F8 also resolve; the layout is non-empty.
    /// </summary>
    [Fact]
    public void EveryLayoutKeyName_ExcludingRestore_ResolvesInRealC64KeyboardMap()
    {
        var map = C64KeyboardMap.CreateDefaultFallback();
        var vm = new VirtualKeyboardViewModel(new SpyKeyboardInput());

        var resolvable = vm.Rows
            .SelectMany(row => row)
            .Where(entry => entry.Kind != AppKeyKind.Restore)
            .ToArray();

        Assert.NotEmpty(resolvable);

        var unresolved = resolvable
            .Where(entry => !map.TryResolve(entry.KeyName, out _))
            .Select(entry => entry.KeyName)
            .ToArray();

        Assert.Empty(unresolved);

        foreach (var shifted in new[] { "F2", "F4", "F6", "F8" })
        {
            Assert.True(map.TryResolve(shifted, out _), $"Shifted function twin '{shifted}' must resolve.");
        }
    }

    /// <summary>
    /// FR-XKBD-001, TR-XKBD-001, TEST-XKBD-001 (RESTORE-tile leg); FR-XBOXUI-006.
    /// Use case: RESTORE on a real C64 is a hardware NMI wired straight to the CPU, not a
    /// key-matrix cell, so the RESTORE tile must drive the dedicated seam and never inject
    /// a matrix key.
    /// Acceptance: pressing the RESTORE tile emits <c>SetRestoreState(true)</c> then
    /// <c>SetRestoreState(false)</c> in that order and makes ZERO <c>SetKeyState</c> calls.
    /// </summary>
    [Fact]
    public void PressRestoreTile_DrivesRestoreSeam_AndNeverSetKeyState()
    {
        var spy = new SpyKeyboardInput();
        var vm = new VirtualKeyboardViewModel(spy);

        var restore = vm.Rows.SelectMany(row => row).Single(entry => entry.Kind == AppKeyKind.Restore);
        vm.Press(restore);

        Assert.Equal(new[] { true, false }, spy.RestoreStates);
        Assert.Empty(spy.KeyStates);
    }

    /// <summary>
    /// FR-XBOXUI-006, TR-XBOXUI-006, TEST-XBOXUI-006 (FEAT-XKBDSTICKY-001: the latch is
    /// scanned in real time).
    /// Use case: the Shift tile is the shift-latch control; like the mechanical
    /// SHIFT-LOCK it holds the LeftShift matrix line while engaged so the machine scan
    /// sees it continuously.
    /// Acceptance: the single <see cref="AppKeyKind.ShiftLatch"/> tile starts with
    /// <see cref="VirtualKeyboardViewModel.ShiftLatched"/> false; pressing it once sets it
    /// true and holds LeftShift down; pressing it again sets it false and releases the
    /// line; no RESTORE call is made.
    /// </summary>
    [Fact]
    public void PressShiftTile_TogglesLatch_WithoutEmittingAnyKeyOrRestore()
    {
        var spy = new SpyKeyboardInput();
        var vm = new VirtualKeyboardViewModel(spy);

        var shift = vm.Rows.SelectMany(row => row).Single(entry => entry.Kind == AppKeyKind.ShiftLatch);

        Assert.False(vm.ShiftLatched);
        vm.Press(shift);
        Assert.True(vm.ShiftLatched);
        vm.Press(shift);
        Assert.False(vm.ShiftLatched);

        Assert.Equal(new[] { ("LeftShift", true), ("LeftShift", false) }, spy.KeyStates);
        Assert.Empty(spy.RestoreStates);
    }

    /// <summary>
    /// FR-XBOXUI-006, TR-XBOXUI-006, TEST-XBOXUI-006.
    /// Use case: the shift-latch mirrors the C64 SHIFT-LOCK, which stays engaged across
    /// multiple keystrokes; it must not silently clear itself after one key.
    /// Acceptance: with the latch engaged, pressing F1 then F3 emits "F2" then "F4" and
    /// the latch remains engaged after each press (documented SHIFT-LOCK persistence, not
    /// one-shot).
    /// </summary>
    [Fact]
    public void ShiftLatch_Persists_AcrossKeyPresses_UntilToggledOff()
    {
        var spy = new SpyKeyboardInput();
        var vm = new VirtualKeyboardViewModel(spy) { ShiftLatched = true };

        // FEAT-XKBDSTICKY-001: a new press finishes the previous stroke, and the latch
        // (unlike a sticky momentary) survives every stroke.
        vm.Press(SingleByKeyName(vm, "F1"));
        Assert.True(vm.ShiftLatched);

        vm.Press(SingleByKeyName(vm, "F3"));
        Assert.True(vm.ShiftLatched);

        vm.CompletePress();
        Assert.True(vm.ShiftLatched);

        Assert.Equal(
            new[] { ("LeftShift", true), ("F2", true), ("F2", false), ("F4", true), ("F4", false) },
            spy.KeyStates);
    }

    /// <summary>
    /// FR-XBOXUI-006, TR-XBOXUI-006, TEST-XBOXUI-006.
    /// Use case: controller focus lands on one tile at a time; activating (A button)
    /// presses whatever tile is currently selected via <c>PressCurrent</c>.
    /// Acceptance: selecting the flattened index of the "A" tile makes
    /// <see cref="VirtualKeyboardViewModel.Selected"/> that tile, and
    /// <see cref="VirtualKeyboardViewModel.PressCurrent"/> emits
    /// <c>SetKeyState("A", true/false)</c>.
    /// </summary>
    [Fact]
    public void PressCurrent_EmitsTheSelectedTile()
    {
        var spy = new SpyKeyboardInput();
        var vm = new VirtualKeyboardViewModel(spy);

        var index = IndexOfKeyName(vm, "A");
        vm.SelectedIndex = index;

        Assert.Equal("A", vm.Selected.KeyName);

        vm.PressCurrent();
        vm.CompletePress();

        Assert.Equal(new[] { ("A", true), ("A", false) }, spy.KeyStates);
        Assert.Empty(spy.RestoreStates);
    }

    /// <summary>
    /// FR-XBOXUI-006, TR-XBOXUI-006, TEST-XBOXUI-006. Widths reconciled with the real machine
    /// (operator 2026-07-18): RETURN (1.5u) and SPACE (9u) are wide; RUN/STOP is a plain 1u key
    /// again (so rows 3-4 finish left of rows 1-2), like ordinary keys.
    /// Acceptance: "Return" and "Space" report <see cref="VirtualKeyEntry.IsWide"/> true;
    /// "RunStop" and a representative ordinary key ("A") report false.
    /// </summary>
    [Fact]
    public void WideTiles_AreFlagged_ForReturnAndSpace()
    {
        var vm = new VirtualKeyboardViewModel(new SpyKeyboardInput());

        Assert.True(SingleByKeyName(vm, "Return").IsWide);
        Assert.False(SingleByKeyName(vm, "RunStop").IsWide);  // 1u key again
        Assert.True(SingleByKeyName(vm, "Space").IsWide);
        Assert.False(SingleByKeyName(vm, "A").IsWide);
    }

    // PLAN-XKEYBOARD-001 K1: the authentic layout moved the function keys out of the rows
    // into the right-hand FunctionKeys column, so tiles are looked up across ALL keys.
    private static VirtualKeyEntry SingleByKeyName(VirtualKeyboardViewModel vm, string keyName) =>
        vm.AllKeys.Single(entry => entry.KeyName == keyName && entry.Kind == AppKeyKind.Key);

    private static int IndexOfKeyName(VirtualKeyboardViewModel vm, string keyName)
    {
        var flat = vm.AllKeys;
        for (var i = 0; i < flat.Count; i++)
        {
            if (flat[i].KeyName == keyName)
            {
                return i;
            }
        }

        throw new InvalidOperationException($"No tile with KeyName '{keyName}' in the default layout.");
    }

    /// <summary>
    /// Spy keyboard input that records SetKeyState and SetRestoreState calls on separate
    /// ordered channels, so a test can assert both the exact order within a channel and
    /// that the other channel was never touched.
    /// </summary>
    private sealed class SpyKeyboardInput : IMachineKeyboardInput
    {
        public DeviceId Id => new(0x9B25);

        public string Name => "Spy Virtual Keyboard Input";

        public List<(string Key, bool Pressed)> KeyStates { get; } = new();

        public List<bool> RestoreStates { get; } = new();

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

        public void Reset()
        {
            KeyStates.Clear();
            RestoreStates.Clear();
        }
    }
}
