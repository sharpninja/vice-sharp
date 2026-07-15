namespace ViceSharp.TestHarness.Xbox;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ViceSharp.Abstractions;
using ViceSharp.Xbox.ViewModels;
using Xunit;

/// <summary>
/// FEAT-XKBDSTICKY-001 (operator 2026-07-14: "C= and SHIFT keys are modifiers and
/// should be sticky when clicked until the next key press which releases them" and
/// "The virtual keyboard should be scanned in real time"). The virtual keyboard is now
/// STATE-DRIVEN: modifier tiles hold their machine matrix line DOWN from the click
/// (so the KERNAL scan sees them continuously), an ordinary key press goes DOWN and
/// stays down across real scan time until <see cref="VirtualKeyboardViewModel.CompletePress"/>
/// releases the key and the armed sticky modifiers, exactly like lifting fingers off
/// the hardware.
/// </summary>
/// <remarks>
/// FR: FR-XKBD-001, FR-XINPUT-005. TR: TR-XKBD-001, TR-MVVM-001.
/// Use case: the player clicks C= (or SHIFT), sees the keycaps flip to the chord
/// glyphs, clicks a letter: the machine scans modifier+key together for the whole
/// stroke, then everything releases.
/// Acceptance:
///   TEST-XKBDSTICKY-001a: clicking SHIFT/C= presses the machine modifier immediately
///     and arms sticky state; clicking again releases and disarms.
///   TEST-XKBDSTICKY-001b: an ordinary key press is DOWN-only; CompletePress releases
///     the key THEN the armed modifiers (hardware order), clearing the arm.
///   TEST-XKBDSTICKY-001c: a second key press before CompletePress finishes the first
///     stroke first (no stuck keys under fast typing).
///   TEST-XKBDSTICKY-001d: SHIFT-LOCK holds the LeftShift line while latched.
///   TEST-XKBDSTICKY-001e: ReleaseAll clears pending key, stickies, and the latch line.
///   TEST-XKBDSTICKY-001f: the Commodore tile is a modifier kind; the overlay schedules
///     the stroke completion and the head releases the VM state on menu/close paths.
/// </remarks>
[Trait("Category", "Xbox")]
public sealed class XboxKeyboardStickyModifierTests
{
    [Fact]
    public void ModifierTiles_HoldTheMatrixLine_UntilToggledOff()
    {
        var spy = new SpyKeyboard();
        var vm = new VirtualKeyboardViewModel(spy);

        var leftShift = Tile(vm, AppKeyKind.ShiftMomentary, "LeftShift");
        var commodore = Tile(vm, AppKeyKind.CommodoreMomentary, "Commodore");

        vm.Press(leftShift);
        Assert.Equal(new[] { ("LeftShift", true) }, spy.KeyStates);
        Assert.True(vm.ShiftArmed);

        vm.Press(commodore);
        Assert.Equal(new[] { ("LeftShift", true), ("Commodore", true) }, spy.KeyStates);
        Assert.True(vm.CommodoreArmed);

        // Clicking an armed modifier releases it (toggle off), the other stays held.
        vm.Press(leftShift);
        Assert.Equal(
            new[] { ("LeftShift", true), ("Commodore", true), ("LeftShift", false) },
            spy.KeyStates);
        Assert.False(vm.ShiftArmed);
        Assert.True(vm.CommodoreArmed);
    }

    [Fact]
    public void KeyStroke_HoldsAcrossScanTime_ThenReleasesKeyAndStickies()
    {
        var spy = new SpyKeyboard();
        var vm = new VirtualKeyboardViewModel(spy);

        vm.Press(Tile(vm, AppKeyKind.CommodoreMomentary, "Commodore"));
        vm.Press(Key(vm, "A"));

        // Real-time scan: modifier AND key are both DOWN right now; nothing released.
        Assert.Equal(new[] { ("Commodore", true), ("A", true) }, spy.KeyStates);
        Assert.True(vm.CommodoreArmed);

        vm.CompletePress();

        // Hardware order: key up first, then the sticky modifier; the arm clears.
        Assert.Equal(
            new[] { ("Commodore", true), ("A", true), ("A", false), ("Commodore", false) },
            spy.KeyStates);
        Assert.False(vm.CommodoreArmed);

        // The next stroke is plain.
        spy.KeyStates.Clear();
        vm.Press(Key(vm, "A"));
        vm.CompletePress();
        Assert.Equal(new[] { ("A", true), ("A", false) }, spy.KeyStates);
    }

    [Fact]
    public void SecondKeyPress_FinishesTheFirstStroke_NoStuckKeys()
    {
        var spy = new SpyKeyboard();
        var vm = new VirtualKeyboardViewModel(spy);

        vm.Press(Key(vm, "A"));
        vm.Press(Key(vm, "B"));
        vm.CompletePress();

        Assert.Equal(
            new[] { ("A", true), ("A", false), ("B", true), ("B", false) },
            spy.KeyStates);
    }

    [Fact]
    public void ShiftLock_HoldsTheLeftShiftLine_WhileLatched()
    {
        var spy = new SpyKeyboard();
        var vm = new VirtualKeyboardViewModel(spy);
        var shiftLock = Tile(vm, AppKeyKind.ShiftLatch, "Shift");

        vm.Press(shiftLock);
        Assert.True(vm.ShiftLatched);
        Assert.Equal(new[] { ("LeftShift", true) }, spy.KeyStates);

        vm.Press(shiftLock);
        Assert.False(vm.ShiftLatched);
        Assert.Equal(new[] { ("LeftShift", true), ("LeftShift", false) }, spy.KeyStates);
    }

    [Fact]
    public void ReleaseAll_ClearsPendingKey_Stickies_AndLatch()
    {
        var spy = new SpyKeyboard();
        var vm = new VirtualKeyboardViewModel(spy);

        vm.Press(Tile(vm, AppKeyKind.ShiftLatch, "Shift"));
        vm.Press(Tile(vm, AppKeyKind.CommodoreMomentary, "Commodore"));
        vm.Press(Key(vm, "Q"));
        spy.KeyStates.Clear();

        vm.ReleaseAll();

        Assert.Contains(("Q", false), spy.KeyStates);
        Assert.Contains(("Commodore", false), spy.KeyStates);
        Assert.Contains(("LeftShift", false), spy.KeyStates);
        Assert.False(vm.ShiftArmed);
        Assert.False(vm.CommodoreArmed);
        Assert.False(vm.ShiftLatched);
        Assert.DoesNotContain(spy.KeyStates, s => s.Down);
    }

    [Fact]
    public void CommodoreTile_IsAModifierKind_InTheAuthenticLayout()
    {
        var layout = VirtualKeyboardLayout.Default;
        var commodore = layout.AllKeys.Single(k => k.KeyName == "Commodore");
        Assert.Equal(AppKeyKind.CommodoreMomentary, commodore.Kind);
    }

    [Fact]
    public void Head_SchedulesStrokeCompletion_AndReleasesOnExits()
    {
        // TEST-XKBDSTICKY-001f structural: the overlay holds each clicked key across a
        // real scan window (timer -> CompletePress), and the head clears the VM state
        // on the menu/close paths so nothing stays held behind the emulator's back.
        var overlay = ReadLower("src", "ViceSharp.Xbox", "Controls", "VirtualKeyboardOverlay.xaml.cs");
        Assert.Contains("completepress", overlay);
        Assert.Contains("createtimer", overlay);

        var app = ReadLower("src", "ViceSharp.Xbox", "App.xaml.cs");
        Assert.Contains("releaseall", app);
    }

    private static VirtualKeyEntry Tile(VirtualKeyboardViewModel vm, AppKeyKind kind, string keyName)
        => vm.AllKeys.First(k => k.Kind == kind && k.KeyName == keyName);

    private static VirtualKeyEntry Key(VirtualKeyboardViewModel vm, string keyName)
        => vm.AllKeys.Single(k => k.Kind == AppKeyKind.Key && k.KeyName == keyName);

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

    private sealed class SpyKeyboard : IMachineKeyboardInput
    {
        public DeviceId Id { get; } = new(0xF3);

        public string Name => "Sticky Modifier Spy";

        public List<(string Key, bool Down)> KeyStates { get; } = [];

        public void Reset() => KeyStates.Clear();

        public bool SetKeyState(string key, bool pressed)
        {
            KeyStates.Add((key, pressed));
            return true;
        }

        public bool SetRestoreState(bool pressed) => true;
    }
}
