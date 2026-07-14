namespace ViceSharp.TestHarness.Xbox;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ViceSharp.Core.Input;
using ViceSharp.Protocol;
using ViceSharp.Xbox.Input;
using ViceSharp.Xbox.ViewModels;
using Xunit;

/// <summary>
/// FIX-XKBDINPUT-001 (PLAN-XBOXUWP, areas XKBD/XBOXUI). Operator 2026-07-14: "Emulator
/// not receiving keyboard input" and "The virtual keyboard needs to be navigable using
/// the dpad, map A to RETURN, B to close the keyboard, Y to DEL, X to RUN/STOP, LB to
/// left cursor, RB to Shift Left Cursor."
/// </summary>
/// <remarks>
/// Acceptance:
///   TEST-XKBDIN-001a: the portable physical-key translation maps Win32 virtual-key
///     codes to C64 keyboard-map names (letters, digits, RETURN, cursor, F1-F8, OEM
///     punctuation, modifiers), leaves app-reserved and unknown keys unmapped, and
///     every produced name resolves in the REAL C64KeyboardMap.
///   TEST-XKBDIN-001b: in the VirtualKeyboard input context, B emits
///     ToggleVirtualKeyboard and returns to Gameplay (close), Y/X/LB/RB emit the
///     dedicated key commands (INST/DEL, RUN/STOP, cursor-left, shift+cursor-left),
///     the D-pad still navigates, and A still activates the focused tile. The MainMenu
///     context emits NONE of the keyboard key commands.
///   TEST-XKBDIN-001c: the dispatcher passes the keyboard key commands and
///     ToggleVirtualKeyboard to the UI callback and makes no host call for them.
///   TEST-XKBDIN-001d (structural): the head injects translated keys down AND up,
///     releases all pressed keys when the menu opens, and toggles/focuses the overlay.
/// </remarks>
[Trait("Category", "Xbox")]
public sealed class XboxKeyboardInputTests
{
    [Theory]
    [InlineData(65, "A")]
    [InlineData(90, "Z")]
    [InlineData(48, "0")]
    [InlineData(57, "9")]
    [InlineData(13, "Return")]
    [InlineData(32, "Space")]
    [InlineData(8, "Backspace")]
    [InlineData(46, "Delete")]
    [InlineData(36, "Home")]
    [InlineData(37, "Left")]
    [InlineData(38, "Up")]
    [InlineData(39, "Right")]
    [InlineData(40, "Down")]
    [InlineData(112, "F1")]
    [InlineData(119, "F8")]
    [InlineData(16, "LeftShift")]
    [InlineData(160, "LeftShift")]
    [InlineData(161, "RightShift")]
    [InlineData(17, "Ctrl")]
    [InlineData(18, "Commodore")]
    [InlineData(186, "Oem1")]
    [InlineData(187, "OemPlus")]
    [InlineData(188, "OemComma")]
    [InlineData(189, "OemMinus")]
    [InlineData(190, "OemPeriod")]
    [InlineData(191, "Oem2")]
    [InlineData(192, "Oem3")]
    [InlineData(222, "Oem7")]
    public void PhysicalKeyMap_TranslatesHostVirtualKeys(int virtualKey, string expected)
    {
        Assert.True(PhysicalKeyMap.TryTranslate(virtualKey, out var name));
        Assert.Equal(expected, name);
    }

    [Theory]
    [InlineData(27)]   // Escape: reserved for the shell menu toggle.
    [InlineData(9)]    // Tab: left to XAML focus.
    [InlineData(120)]  // F9-F12: reserved for app shortcuts.
    [InlineData(123)]
    [InlineData(195)]  // GamepadA: never a C64 key.
    [InlineData(0)]
    public void PhysicalKeyMap_LeavesReservedAndUnknownKeysUnmapped(int virtualKey)
    {
        Assert.False(PhysicalKeyMap.TryTranslate(virtualKey, out _));
    }

    [Fact]
    public void PhysicalKeyMap_EveryTranslation_ResolvesInTheRealMap()
    {
        var map = C64KeyboardMap.CreateDefaultFallback();
        var unresolved = new List<string>();

        for (var vk = 0; vk < 256; vk++)
        {
            if (PhysicalKeyMap.TryTranslate(vk, out var name) && !map.TryResolve(name, out _))
                unresolved.Add($"vk={vk} -> {name}");
        }

        Assert.Empty(unresolved);
    }

    [Fact]
    public void KeyboardContext_ChordButtons_EmitTheOperatorMapping()
    {
        // TEST-XKBDIN-001b (remapped by the operator 2026-07-14: "X button goes to DEL,
        // Y button goes to space bar, B button goes to RUN/STOP").
        var context = new XboxInputContext();
        context.RequestContext(InputContext.VirtualKeyboard);

        Assert.Equal(
            new[] { AppCommand.KeyboardKeyDelete },
            Commands(context, 1, GamepadButtonFlags.X));
        Assert.Equal(
            new[] { AppCommand.KeyboardKeySpace },
            Commands(context, 3, GamepadButtonFlags.Y));
        Assert.Equal(
            new[] { AppCommand.KeyboardKeyRunStop },
            Commands(context, 5, GamepadButtonFlags.B));
        Assert.Equal(
            new[] { AppCommand.KeyboardKeyCursorLeft },
            Commands(context, 7, GamepadButtonFlags.LeftShoulder));
        Assert.Equal(
            new[] { AppCommand.KeyboardKeyShiftCursorLeft },
            Commands(context, 9, GamepadButtonFlags.RightShoulder));

        // A still activates the focused tile (that is how RETURN and every letter is
        // pressed); View (not B, which now types RUN/STOP) closes the keyboard.
        Assert.Equal(new[] { AppCommand.UiActivate }, Commands(context, 11, GamepadButtonFlags.A));
        var resolution = TickPair(context, 13, GamepadButtonFlags.View);
        Assert.Equal(new[] { AppCommand.ToggleVirtualKeyboard }, resolution.Commands);
        Assert.Equal(InputContext.Gameplay, resolution.NextContext);
    }

    [Fact]
    public void MainMenuContext_DoesNotEmitKeyboardKeyCommands()
    {
        var context = new XboxInputContext();
        context.RequestContext(InputContext.MainMenu);

        Assert.Empty(Commands(context, 1, GamepadButtonFlags.Y));
        Assert.Empty(Commands(context, 3, GamepadButtonFlags.X));
        Assert.Empty(Commands(context, 5, GamepadButtonFlags.LeftShoulder));
        Assert.Empty(Commands(context, 7, GamepadButtonFlags.RightShoulder));
    }

    [Fact]
    public void Head_InjectsSpace_ForTheYChord()
    {
        // TEST-XKBDIN-001d (remap): the head routes KeyboardKeySpace to a SPACE injection.
        var app = ReadLower("src", "ViceSharp.Xbox", "App.xaml.cs");
        Assert.Contains("keyboardkeyspace", app);
    }

    [Fact]
    public void KeyboardContext_Triggers_HoldCommodoreAndShift()
    {
        // TEST-XKBDIN-001e (operator 2026-07-14: "Left Trigger = C= key, Right Trigger
        // is SHIFT"): HOLD semantics with the shared trigger hysteresis - down once at
        // the activate edge, silent while held (including inside the band), up once at
        // the release edge.
        var context = new XboxInputContext();
        context.RequestContext(InputContext.VirtualKeyboard);
        context.Tick(1, Snapshot(GamepadButtonFlags.None));

        var res = context.Tick(2, Triggers(lt: 0.9));
        Assert.Equal(new[] { AppCommand.KeyboardModifierCommodoreDown }, res.Commands);
        Assert.Empty(context.Tick(3, Triggers(lt: 0.9)).Commands);
        Assert.Empty(context.Tick(4, Triggers(lt: 0.5)).Commands);

        res = context.Tick(5, Triggers(lt: 0.1));
        Assert.Equal(new[] { AppCommand.KeyboardModifierCommodoreUp }, res.Commands);

        res = context.Tick(6, Triggers(rt: 0.9));
        Assert.Equal(new[] { AppCommand.KeyboardModifierShiftDown }, res.Commands);

        res = context.Tick(7, Triggers());
        Assert.Equal(new[] { AppCommand.KeyboardModifierShiftUp }, res.Commands);
    }

    [Fact]
    public void KeyboardContext_ExitWhileTriggerHeld_ReleasesTheModifier()
    {
        // TEST-XKBDIN-001e: closing the keyboard (View) while a trigger modifier is held
        // must release it on the way out - a modifier may never stick inside the machine.
        var context = new XboxInputContext();
        context.RequestContext(InputContext.VirtualKeyboard);
        context.Tick(1, Snapshot(GamepadButtonFlags.None));
        context.Tick(2, Triggers(rt: 0.9));

        var res = context.Tick(
            3, new GamepadSnapshot(0.0, 0.0, 0.0, 0.0, 0.0, 0.9, GamepadButtonFlags.View, 0UL));

        Assert.Contains(AppCommand.KeyboardModifierShiftUp, res.Commands);
        Assert.Contains(AppCommand.ToggleVirtualKeyboard, res.Commands);
        Assert.Equal(InputContext.Gameplay, res.NextContext);
    }

    [Fact]
    public void View_ReopensTheKeyboard_FromTheMainMenuContext()
    {
        // Operator 2026-07-14: "The button you assigned for closing the keyboard should
        // bring it back." Regression: the keyboard opened from the Controls page (a
        // PUSHED page), View closed it, the observer re-derived MainMenu (stack still
        // non-empty), and the MainMenu branch ignored View - the keyboard could never
        // return. View now toggles the keyboard from MainMenu as well.
        var context = new XboxInputContext();

        // Closing left us in MainMenu (a page is still pushed under the keyboard).
        context.RequestContext(InputContext.MainMenu);
        var res = TickPair(context, 1, GamepadButtonFlags.View);

        Assert.Equal(new[] { AppCommand.ToggleVirtualKeyboard }, res.Commands);
        Assert.Equal(InputContext.VirtualKeyboard, res.NextContext);

        // And View from inside the keyboard still closes it.
        res = TickPair(context, 3, GamepadButtonFlags.View);
        Assert.Equal(new[] { AppCommand.ToggleVirtualKeyboard }, res.Commands);
        Assert.Equal(InputContext.Gameplay, res.NextContext);
    }

    [Fact]
    public void MainMenuContext_Triggers_EmitNothing()
    {
        var context = new XboxInputContext();
        context.RequestContext(InputContext.MainMenu);
        context.Tick(1, Snapshot(GamepadButtonFlags.None));

        Assert.Empty(context.Tick(2, Triggers(lt: 0.9)).Commands);
        Assert.Empty(context.Tick(3, Triggers(rt: 0.9)).Commands);
    }

    private static GamepadSnapshot Triggers(double lt = 0.0, double rt = 0.0)
        => new(0.0, 0.0, 0.0, 0.0, lt, rt, GamepadButtonFlags.None, 0UL);

    [Fact]
    public void Head_WiresPhysicalKeyboard_AndOverlayToggle()
    {
        // TEST-XKBDIN-001d: structural wiring of the #if HAS_UWP head.
        var app = ReadLower("src", "ViceSharp.Xbox", "App.xaml.cs");
        Assert.Contains("physicalkeymap.trytranslate", app);
        Assert.Contains("onrootkeyup", app);
        Assert.Contains("releaseallpressedkeys", app);
        Assert.Contains("togglekeyboardoverlay", app);
        Assert.Contains("keyboardkeydelete", app);
        Assert.Contains("keyboardkeyshiftcursorleft", app);
    }

    private static IReadOnlyList<AppCommand> Commands(
        XboxInputContext context, long frameIndex, GamepadButtonFlags button)
        => TickPair(context, frameIndex, button).Commands;

    /// <summary>Ticks a neutral frame then a button-down frame (a clean down edge).</summary>
    private static InputResolution TickPair(
        XboxInputContext context, long frameIndex, GamepadButtonFlags button)
    {
        context.Tick(frameIndex, Snapshot(GamepadButtonFlags.None));
        return context.Tick(frameIndex + 1, Snapshot(button));
    }

    private static GamepadSnapshot Snapshot(GamepadButtonFlags buttons)
        => new(0.0, 0.0, 0.0, 0.0, 0.0, 0.0, buttons, 0UL);

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
