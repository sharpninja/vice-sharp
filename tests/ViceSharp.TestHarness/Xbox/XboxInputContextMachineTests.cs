namespace ViceSharp.TestHarness.Xbox;

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ViceSharp.Xbox.Input;
using Xunit;

/// <summary>
/// PLAN-XBOXUWP slice S11 (IMPL-XBOXUWP-011). TEST-CTX-001: the single-consumer
/// input state machine <see cref="XboxInputContext"/> and its auto-repeat helper
/// <see cref="DirectionalRepeater"/> in <c>ViceSharp.Xbox.Input</c>.
/// </summary>
/// <remarks>
/// <para>
/// There is exactly ONE consumer of the whole <see cref="GamepadSnapshot"/>:
/// <see cref="XboxInputContext.Tick(long, in GamepadSnapshot)"/> unifies the
/// joystick mapper (<see cref="XboxJoystickMapper"/>) and the system-button
/// evaluator (<see cref="XboxSystemButtons"/>), gates them by
/// <see cref="InputContext"/>, and produces UI-navigation intents in menus. There
/// is NO separate app-button path and NO context-unaware pump (FR-GAMEPAD-009).
/// </para>
/// <para>
/// The machine is deterministic: no wall-clock, no random. Elapsed time for the
/// directional auto-repeat is DERIVED from the injected <c>frameIndex</c> as
/// <c>(frameIndex - priorFrameIndex) * <see cref="XboxInputContext.FrameDurationMs"/></c>
/// (20.0 ms, ~50 Hz PAL). The directional repeater is driven by the D-pad merged
/// with the left thumbstick digitized through <see cref="StickConverter"/>.
/// </para>
/// </remarks>
[Trait("Category", "Xbox")]
public sealed class XboxInputContextMachineTests
{
    private static GamepadSnapshot Snap(
        double lx = 0,
        double ly = 0,
        double rx = 0,
        double ry = 0,
        double lt = 0,
        double rt = 0,
        GamepadButtonFlags buttons = GamepadButtonFlags.None) =>
        new(lx, ly, rx, ry, lt, rt, buttons, 0UL);

    private static GamepadSnapshot Btn(GamepadButtonFlags buttons) => Snap(buttons: buttons);

    // ------------------------------------------------------------------
    // Transition table (FR-CTX-001, FR-SYSBTN-002/003/007)
    // ------------------------------------------------------------------

    /// <summary>
    /// FR-CTX-001 / FR-SYSBTN-002 / FR-SYSBTN-003 / FR-SYSBTN-007, TR-CTX-001
    /// (IMPL-XBOXUWP-011), TEST-CTX-001 transition-table guard.
    /// Use case: the four-context machine must move between Gameplay, MainMenu,
    /// VirtualKeyboard and ConfirmDialog only on defined edges, and never let an
    /// undefined (state,input) pair change the context.
    /// Acceptance: driving each (startContext, button down-edge) pair yields the
    /// expected NextContext, and the machine's own Context property equals it after
    /// the tick.
    /// </summary>
    [Theory]
    [Trait("Category", "Xbox")]
    // From Gameplay.
    [InlineData(InputContext.Gameplay, GamepadButtonFlags.None, InputContext.Gameplay)]
    [InlineData(InputContext.Gameplay, GamepadButtonFlags.Menu, InputContext.MainMenu)]
    [InlineData(InputContext.Gameplay, GamepadButtonFlags.View, InputContext.VirtualKeyboard)]
    [InlineData(InputContext.Gameplay, GamepadButtonFlags.Y, InputContext.ConfirmDialog)]
    [InlineData(InputContext.Gameplay, GamepadButtonFlags.X, InputContext.Gameplay)]
    // From MainMenu.
    [InlineData(InputContext.MainMenu, GamepadButtonFlags.None, InputContext.MainMenu)]
    [InlineData(InputContext.MainMenu, GamepadButtonFlags.Menu, InputContext.Gameplay)]
    [InlineData(InputContext.MainMenu, GamepadButtonFlags.A, InputContext.MainMenu)]
    [InlineData(InputContext.MainMenu, GamepadButtonFlags.B, InputContext.MainMenu)]
    // From VirtualKeyboard.
    [InlineData(InputContext.VirtualKeyboard, GamepadButtonFlags.None, InputContext.VirtualKeyboard)]
    [InlineData(InputContext.VirtualKeyboard, GamepadButtonFlags.View, InputContext.Gameplay)]
    [InlineData(InputContext.VirtualKeyboard, GamepadButtonFlags.Menu, InputContext.Gameplay)]
    // From ConfirmDialog.
    [InlineData(InputContext.ConfirmDialog, GamepadButtonFlags.None, InputContext.ConfirmDialog)]
    [InlineData(InputContext.ConfirmDialog, GamepadButtonFlags.A, InputContext.Gameplay)]
    [InlineData(InputContext.ConfirmDialog, GamepadButtonFlags.B, InputContext.Gameplay)]
    [InlineData(InputContext.ConfirmDialog, GamepadButtonFlags.Menu, InputContext.Gameplay)]
    public void TransitionTable_DefinedEdges_MoveContext(
        InputContext start,
        GamepadButtonFlags buttons,
        InputContext expectedNext)
    {
        var machine = new XboxInputContext(initialContext: start);

        var resolution = machine.Tick(0, Btn(buttons));

        Assert.Equal(expectedNext, resolution.NextContext);
        Assert.Equal(expectedNext, machine.Context);
    }

    // ------------------------------------------------------------------
    // Non-Gameplay joystick neutralization (FR-CTX-002)
    // ------------------------------------------------------------------

    /// <summary>
    /// FR-CTX-002, TR-CTX-001 (IMPL-XBOXUWP-011), TEST-CTX-001 menu-neutralization
    /// guard.
    /// Use case: in every non-Gameplay context the emulated joystick must be inert:
    /// a player mashing A with both sticks slammed to the corner while a menu is
    /// open must not move or fire the C64 joystick.
    /// Acceptance: in each of MainMenu, VirtualKeyboard and ConfirmDialog, a tick
    /// with A held and both sticks fully deflected returns Joy1 == Joy2 == Neutral
    /// and neither port fires.
    /// </summary>
    [Theory]
    [Trait("Category", "Xbox")]
    [InlineData(InputContext.MainMenu)]
    [InlineData(InputContext.VirtualKeyboard)]
    [InlineData(InputContext.ConfirmDialog)]
    public void NonGameplay_ForcesNeutral_AndSuppressesFire(InputContext context)
    {
        var machine = new XboxInputContext(initialContext: context);
        var snapshot = Snap(lx: 1.0, ly: 1.0, rx: 1.0, ry: 1.0, buttons: GamepadButtonFlags.A);

        var resolution = machine.Tick(0, snapshot);

        Assert.Equal(JoystickPortState.Neutral, resolution.Joy1);
        Assert.Equal(JoystickPortState.Neutral, resolution.Joy2);
        Assert.False(resolution.Joy1.Fire);
        Assert.False(resolution.Joy2.Fire);
    }

    // ------------------------------------------------------------------
    // One-shot neutral push on the Gameplay -> non-Gameplay edge (FR-CTX-004)
    // ------------------------------------------------------------------

    /// <summary>
    /// FR-CTX-004 / FR-SYSBTN-002, TR-CTX-001 (IMPL-XBOXUWP-011), TEST-CTX-001
    /// one-shot neutral-push guard.
    /// Use case: opening the menu while the stick is held must immediately release
    /// the held C64 input so the machine does not run away under a stuck joystick.
    /// Acceptance: with the left stick fully up (would map to JOY2 up), a Menu
    /// down-edge in Gameplay emits OpenMainMenu, moves to MainMenu, and forces
    /// Joy1 == Joy2 == Neutral on THIS frame (an emitted neutral, not a suppressed
    /// no-op).
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void GameplayToMainMenu_Edge_EmitsNeutralPush_AndOpenMainMenu()
    {
        var machine = new XboxInputContext();

        var resolution = machine.Tick(0, Snap(ly: 1.0, buttons: GamepadButtonFlags.Menu));

        Assert.Equal(InputContext.MainMenu, resolution.NextContext);
        Assert.Contains(AppCommand.OpenMainMenu, resolution.Commands);
        Assert.Equal(JoystickPortState.Neutral, resolution.Joy1);
        Assert.Equal(JoystickPortState.Neutral, resolution.Joy2);
    }

    /// <summary>
    /// FR-GAMEPAD-006 / FR-CTX-001, TR-CTX-001 (IMPL-XBOXUWP-011), TEST-CTX-001
    /// Gameplay joystick guard.
    /// Use case: in Gameplay the single consumer must actually drive the joystick
    /// (the mapper is consulted), otherwise the neutralization tests could pass
    /// trivially.
    /// Acceptance: left stick fully up with no context-changing button routes JOY2
    /// up and leaves the machine in Gameplay.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void Gameplay_LeftStickUp_DrivesJoy2_StaysGameplay()
    {
        var machine = new XboxInputContext();

        var resolution = machine.Tick(0, Snap(ly: 1.0));

        Assert.Equal(InputContext.Gameplay, resolution.NextContext);
        Assert.Equal(JoystickPortState.Up, resolution.Joy2.DirectionMask);
    }

    // ------------------------------------------------------------------
    // UI navigation / activation / back in menus (FR-CTX-003)
    // ------------------------------------------------------------------

    /// <summary>
    /// FR-CTX-003, TR-CTX-001 (IMPL-XBOXUWP-011), TEST-CTX-001 UI-nav guard.
    /// Use case: in a menu the D-pad drives focus navigation, A activates the
    /// focused item and B goes back, while the sticks produce no joystick effect.
    /// Acceptance: in MainMenu a DPadDown down-edge emits UiNavigateDown, an A
    /// down-edge emits UiActivate, and a B down-edge emits UiBack.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void Menu_Dpad_Navigates_A_Activates_B_Backs()
    {
        var nav = new XboxInputContext(initialContext: InputContext.MainMenu);
        Assert.Contains(AppCommand.UiNavigateDown, nav.Tick(0, Btn(GamepadButtonFlags.DPadDown)).Commands);

        var activate = new XboxInputContext(initialContext: InputContext.MainMenu);
        Assert.Contains(AppCommand.UiActivate, activate.Tick(0, Btn(GamepadButtonFlags.A)).Commands);

        var back = new XboxInputContext(initialContext: InputContext.MainMenu);
        Assert.Contains(AppCommand.UiBack, back.Tick(0, Btn(GamepadButtonFlags.B)).Commands);
    }

    /// <summary>
    /// FR-CTX-003, TR-CTX-001 (IMPL-XBOXUWP-011), TEST-CTX-001 left-stick-as-nav
    /// guard.
    /// Use case: the left thumbstick (digitized through the shared
    /// <see cref="StickConverter"/>) drives menu navigation just like the D-pad, so
    /// a player without a working D-pad can still navigate.
    /// Acceptance: in MainMenu a fully-left stick deflection emits UiNavigateLeft and
    /// produces no joystick output.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void Menu_LeftStick_DrivesUiNavigation()
    {
        var machine = new XboxInputContext(initialContext: InputContext.MainMenu);

        var resolution = machine.Tick(0, Snap(lx: -1.0));

        Assert.Contains(AppCommand.UiNavigateLeft, resolution.Commands);
        Assert.Equal(JoystickPortState.Neutral, resolution.Joy1);
        Assert.Equal(JoystickPortState.Neutral, resolution.Joy2);
    }

    // ------------------------------------------------------------------
    // Menu / View toggles (FR-SYSBTN-002 / FR-SYSBTN-003)
    // ------------------------------------------------------------------

    /// <summary>
    /// FR-SYSBTN-002, TR-CTX-001 (IMPL-XBOXUWP-011), TEST-CTX-001 Menu-toggle guard.
    /// Use case: the Menu button both opens the main menu from Gameplay and closes it
    /// back to Gameplay.
    /// Acceptance: a Menu down-edge in Gameplay emits OpenMainMenu and enters
    /// MainMenu; after releasing, a second Menu down-edge emits CloseMenu and returns
    /// to Gameplay.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void Menu_TogglesOpenThenClose()
    {
        var machine = new XboxInputContext();

        var open = machine.Tick(0, Btn(GamepadButtonFlags.Menu));
        Assert.Equal(InputContext.MainMenu, open.NextContext);
        Assert.Contains(AppCommand.OpenMainMenu, open.Commands);

        _ = machine.Tick(1, Btn(GamepadButtonFlags.None)); // release Menu

        var close = machine.Tick(2, Btn(GamepadButtonFlags.Menu));
        Assert.Equal(InputContext.Gameplay, close.NextContext);
        Assert.Contains(AppCommand.CloseMenu, close.Commands);
    }

    /// <summary>
    /// FR-SYSBTN-003, TR-CTX-001 (IMPL-XBOXUWP-011), TEST-CTX-001 View-toggle guard.
    /// Use case: the View button toggles the on-screen virtual keyboard on and off.
    /// Acceptance: a View down-edge in Gameplay emits ToggleVirtualKeyboard and enters
    /// VirtualKeyboard; after releasing, a second View down-edge emits
    /// ToggleVirtualKeyboard and returns to Gameplay.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void View_TogglesVirtualKeyboard()
    {
        var machine = new XboxInputContext();

        var on = machine.Tick(0, Btn(GamepadButtonFlags.View));
        Assert.Equal(InputContext.VirtualKeyboard, on.NextContext);
        Assert.Contains(AppCommand.ToggleVirtualKeyboard, on.Commands);

        _ = machine.Tick(1, Btn(GamepadButtonFlags.None)); // release View

        var off = machine.Tick(2, Btn(GamepadButtonFlags.View));
        Assert.Equal(InputContext.Gameplay, off.NextContext);
        Assert.Contains(AppCommand.ToggleVirtualKeyboard, off.Commands);
    }

    // ------------------------------------------------------------------
    // Confirm-gated WarmReset (FR-SYSBTN-007)
    // ------------------------------------------------------------------

    /// <summary>
    /// FR-SYSBTN-007, TR-CTX-001 (IMPL-XBOXUWP-011), TEST-CTX-001 confirm-gate guard.
    /// Use case: the destructive reset (Y -> WarmReset) must never fire directly; it
    /// opens a confirmation dialog and only a "yes" carries the reset out.
    /// Acceptance: a Y down-edge in Gameplay enters ConfirmDialog and emits NO
    /// WarmReset; a subsequent A (ConfirmYes) emits ConfirmYes AND WarmReset and
    /// returns to Gameplay.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void Y_OpensConfirmDialog_OnlyConfirmYes_PerformsWarmReset()
    {
        var machine = new XboxInputContext();

        var open = machine.Tick(0, Btn(GamepadButtonFlags.Y));
        Assert.Equal(InputContext.ConfirmDialog, open.NextContext);
        Assert.DoesNotContain(AppCommand.WarmReset, open.Commands);

        var yes = machine.Tick(1, Btn(GamepadButtonFlags.A));
        Assert.Equal(InputContext.Gameplay, yes.NextContext);
        Assert.Contains(AppCommand.ConfirmYes, yes.Commands);
        Assert.Contains(AppCommand.WarmReset, yes.Commands);
    }

    /// <summary>
    /// FR-SYSBTN-007, TR-CTX-001 (IMPL-XBOXUWP-011), TEST-CTX-001 confirm-cancel
    /// guard.
    /// Use case: cancelling the confirmation must abort the reset entirely.
    /// Acceptance: a Y down-edge in Gameplay enters ConfirmDialog; a subsequent B
    /// (ConfirmNo) emits ConfirmNo, emits NO WarmReset, and returns to Gameplay.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void ConfirmDialog_B_ConfirmsNo_NoReset()
    {
        var machine = new XboxInputContext();

        _ = machine.Tick(0, Btn(GamepadButtonFlags.Y));

        var no = machine.Tick(1, Btn(GamepadButtonFlags.B));
        Assert.Equal(InputContext.Gameplay, no.NextContext);
        Assert.Contains(AppCommand.ConfirmNo, no.Commands);
        Assert.DoesNotContain(AppCommand.WarmReset, no.Commands);
    }

    // ------------------------------------------------------------------
    // Gameplay pass-through commands (FR-SYSBTN-001)
    // ------------------------------------------------------------------

    /// <summary>
    /// FR-SYSBTN-001, TR-CTX-001 (IMPL-XBOXUWP-011), TEST-CTX-001 pass-through guard.
    /// Use case: non-context, non-confirm gameplay commands (e.g. X -> AutostartDrive8)
    /// pass straight through and leave the machine in Gameplay.
    /// Acceptance: an X down-edge in Gameplay emits AutostartDrive8 and NextContext
    /// stays Gameplay.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void Gameplay_X_PassesThroughAutostart_StaysGameplay()
    {
        var machine = new XboxInputContext();

        var resolution = machine.Tick(0, Btn(GamepadButtonFlags.X));

        Assert.Equal(InputContext.Gameplay, resolution.NextContext);
        Assert.Contains(AppCommand.AutostartDrive8, resolution.Commands);
    }

    // ------------------------------------------------------------------
    // DirectionalRepeater timing (FR-CTX-003)
    // ------------------------------------------------------------------

    /// <summary>
    /// FR-CTX-003, TR-CTX-001 (IMPL-XBOXUWP-011), TEST-CTX-001 auto-repeat guard.
    /// Use case: holding a direction in a menu emits the nav intent once on press,
    /// waits an initial delay, then auto-repeats so the user can scroll a long list
    /// without mashing; releasing resets the schedule.
    /// Acceptance: with injected elapsed-ms, a fresh press emits once; nothing
    /// repeats before <see cref="DirectionalRepeater.InitialDelayMs"/> (400 ms);
    /// crossing 400 ms emits again; then it repeats every
    /// <see cref="DirectionalRepeater.RepeatIntervalMs"/> (90 ms); release then
    /// re-press emits a fresh intent.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void DirectionalRepeater_InitialDelayThenRepeat_ResetOnRelease()
    {
        var repeater = new DirectionalRepeater();

        // Fresh press -> one immediate nav.
        Assert.Equal(AppCommand.UiNavigateUp, repeater.Tick(GamepadButtonFlags.DPadUp, 0));

        // Held but before the 400 ms initial delay -> nothing.
        Assert.Null(repeater.Tick(GamepadButtonFlags.DPadUp, 100)); // 100
        Assert.Null(repeater.Tick(GamepadButtonFlags.DPadUp, 100)); // 200
        Assert.Null(repeater.Tick(GamepadButtonFlags.DPadUp, 100)); // 300
        Assert.Null(repeater.Tick(GamepadButtonFlags.DPadUp, 99));  // 399

        // Crossing 400 ms -> first repeat.
        Assert.Equal(AppCommand.UiNavigateUp, repeater.Tick(GamepadButtonFlags.DPadUp, 1)); // 400

        // Not yet a full 90 ms since the first repeat -> nothing.
        Assert.Null(repeater.Tick(GamepadButtonFlags.DPadUp, 89)); // 489

        // Crossing the next 90 ms -> another repeat.
        Assert.Equal(AppCommand.UiNavigateUp, repeater.Tick(GamepadButtonFlags.DPadUp, 1)); // 490

        // Release resets the schedule.
        Assert.Null(repeater.Tick(GamepadButtonFlags.None, 1000));

        // Re-press -> fresh immediate nav again.
        Assert.Equal(AppCommand.UiNavigateUp, repeater.Tick(GamepadButtonFlags.DPadUp, 1000));
    }

    /// <summary>
    /// FR-CTX-003, TR-CTX-001 (IMPL-XBOXUWP-011), TEST-CTX-001 direction-change guard.
    /// Use case: changing direction while still holding must emit the new direction
    /// immediately (not wait out the old repeat schedule).
    /// Acceptance: a Left press emits UiNavigateLeft; switching to Right on the very
    /// next tick emits UiNavigateRight immediately.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void DirectionalRepeater_DirectionChange_EmitsFreshImmediately()
    {
        var repeater = new DirectionalRepeater();

        Assert.Equal(AppCommand.UiNavigateLeft, repeater.Tick(GamepadButtonFlags.DPadLeft, 0));
        Assert.Equal(AppCommand.UiNavigateRight, repeater.Tick(GamepadButtonFlags.DPadRight, 50));
    }

    /// <summary>
    /// FR-CTX-003, TR-CTX-001 (IMPL-XBOXUWP-011), TEST-CTX-001 frame-derived-elapsed
    /// guard.
    /// Use case: the context machine must feed the repeater elapsed-ms derived purely
    /// from the injected frame index (no wall-clock), so auto-repeat is deterministic
    /// and replayable.
    /// Acceptance: holding DPadDown across frames at 20 ms/frame emits once on the
    /// first frame, nothing through frame 19 (&lt; 400 ms), and repeats at frame 20
    /// (== 400 ms held).
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void ContextMachine_DerivesRepeatElapsed_FromFrameIndex()
    {
        var machine = new XboxInputContext(initialContext: InputContext.MainMenu);
        var down = Btn(GamepadButtonFlags.DPadDown);

        // Frame 0: fresh press -> one nav.
        Assert.Contains(AppCommand.UiNavigateDown, machine.Tick(0, down).Commands);

        // Frames 1..19 (20 ms each -> 20..380 ms held): no repeat before 400 ms.
        for (long frame = 1; frame <= 19; frame++)
        {
            Assert.DoesNotContain(AppCommand.UiNavigateDown, machine.Tick(frame, down).Commands);
        }

        // Frame 20 (== 400 ms held): first repeat.
        Assert.Contains(AppCommand.UiNavigateDown, machine.Tick(20, down).Commands);
    }

    // ------------------------------------------------------------------
    // Determinism (TR-CTX-001)
    // ------------------------------------------------------------------

    /// <summary>
    /// TR-CTX-001 (IMPL-XBOXUWP-011), TEST-CTX-001 determinism guard.
    /// Use case: identical input replayed on two fresh machines must produce an
    /// identical resolution sequence so lockstep replay and snapshot comparison stay
    /// bit-exact; Tick reads no time, random, or shared state.
    /// Acceptance: two fresh XboxInputContext instances driven by the same
    /// (frameIndex, snapshot) sequence return equal InputResolutions (NextContext,
    /// Joy1, Joy2, and the Commands sequence) at every step.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void Determinism_TwoFreshMachines_ReplayEqualSequence()
    {
        var sequence = new (long Frame, GamepadSnapshot Snapshot)[]
        {
            (0, Btn(GamepadButtonFlags.Menu)),                        // Gameplay -> MainMenu
            (1, Btn(GamepadButtonFlags.DPadDown)),                    // nav down
            (2, Btn(GamepadButtonFlags.A)),                           // activate
            (3, Btn(GamepadButtonFlags.Menu)),                        // close -> Gameplay
            (4, Snap(lx: 1.0, buttons: GamepadButtonFlags.A)),        // joystick + fire
            (5, Btn(GamepadButtonFlags.Y)),                           // -> ConfirmDialog
            (6, Btn(GamepadButtonFlags.A)),                           // ConfirmYes -> WarmReset
        };

        var machineA = new XboxInputContext();
        var machineB = new XboxInputContext();

        foreach (var (frame, snapshot) in sequence)
        {
            var a = machineA.Tick(frame, snapshot);
            var b = machineB.Tick(frame, snapshot);

            Assert.Equal(a.NextContext, b.NextContext);
            Assert.Equal(a.Joy1, b.Joy1);
            Assert.Equal(a.Joy2, b.Joy2);
            Assert.Equal(a.Commands, b.Commands); // xUnit sequence-equal for IEnumerable
        }
    }

    // ------------------------------------------------------------------
    // No static mutable state on the pure input helpers (TR-CTX-001)
    // ------------------------------------------------------------------

    /// <summary>
    /// TR-CTX-001 (IMPL-XBOXUWP-011), TEST-CTX-001 no-static-mutable-state guard.
    /// Use case: the pure input helpers must hold NO static mutable state (per-frame
    /// state is instance-scoped on the machine and repeater, or threaded as
    /// parameters) so they are thread-safe and deterministic.
    /// Acceptance: none of StickConverter, XboxJoystickMapper, XboxSystemButtons,
    /// XboxInputContext, or DirectionalRepeater declare a static field that is not
    /// const (literal) or readonly (init-only).
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void PureInputHelpers_HaveNoStaticMutableFields()
    {
        var types = new[]
        {
            typeof(StickConverter),
            typeof(XboxJoystickMapper),
            typeof(XboxSystemButtons),
            typeof(XboxInputContext),
            typeof(DirectionalRepeater),
        };

        foreach (var type in types)
        {
            var mutable = type
                .GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(f => !f.IsLiteral && !f.IsInitOnly)
                .Select(f => f.Name)
                .ToArray();

            Assert.True(mutable.Length == 0, $"{type.Name} has static mutable field(s): {string.Join(", ", mutable)}");
        }
    }
}
