namespace ViceSharp.TestHarness.Xbox;

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using ViceSharp.Xbox.Input;
using Xunit;

/// <summary>
/// PLAN-XBOXUWP slice S10 (IMPL-XBOXUWP-010). TEST-SYSBTN-001: the system-button
/// binding MODEL (<see cref="AppCommand"/>, <see cref="BindableInput"/>,
/// <see cref="BindingActivation"/>, <see cref="ButtonBinding"/>,
/// <see cref="BindingProfile"/>) and the pure edge-activation EVALUATOR
/// (<see cref="XboxSystemButtons"/>) in <c>ViceSharp.Xbox.Input</c>. The evaluator
/// turns held-frame gamepad snapshots into discrete <see cref="AppCommand"/>s.
/// </summary>
/// <remarks>
/// The evaluator is pure and holds NO static mutable state: the trigger-hold
/// hysteresis phase is threaded through the <see cref="SystemButtonLatch"/> latch
/// (prior-in / next-out), and commands are appended to a caller-provided buffer.
/// Digital activeness is <c>(Buttons &amp; flag) != 0</c>; a down edge is
/// prior-not-set &amp;&amp; current-set. LeftTrigger hold-to-warp uses hysteresis
/// (activate &gt;= 0.6, release &lt;= 0.4).
/// </remarks>
[Trait("Category", "Xbox")]
public sealed class XboxSystemButtonsBindingTests
{
    private static GamepadSnapshot Buttons(GamepadButtonFlags buttons) =>
        new(0.0, 0.0, 0.0, 0.0, 0.0, 0.0, buttons, 0UL);

    private static GamepadSnapshot LeftTrigger(double value) =>
        new(0.0, 0.0, 0.0, 0.0, value, 0.0, GamepadButtonFlags.None, 0UL);

    /// <summary>
    /// FR-SYSBTN-001 / TR-SYSBTN-001 (IMPL-XBOXUWP-010), TEST-SYSBTN-001
    /// default-table guard.
    /// Use case: shipping the LOCKED default gameplay binding set so the on-console
    /// control scheme is a stable, by-value contract.
    /// Acceptance: <see cref="BindingProfile.Default"/> enumerates exactly the eight
    /// locked rows (Input/Command/Activation each), in order, and neither RightTrigger
    /// nor RightThumbstick is bound.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void Default_MatchesLockedTable_ByValue()
    {
        var expected = new[]
        {
            new ButtonBinding(BindableInput.Menu, AppCommand.OpenMainMenu, BindingActivation.Toggle),
            new ButtonBinding(BindableInput.View, AppCommand.ToggleVirtualKeyboard, BindingActivation.Toggle),
            new ButtonBinding(BindableInput.X, AppCommand.AutostartDrive8, BindingActivation.Press),
            new ButtonBinding(BindableInput.Y, AppCommand.WarmReset, BindingActivation.Press),
            new ButtonBinding(BindableInput.LeftShoulder, AppCommand.QuickSaveState, BindingActivation.Press),
            new ButtonBinding(BindableInput.RightShoulder, AppCommand.QuickLoadState, BindingActivation.Press),
            new ButtonBinding(BindableInput.LeftTrigger, AppCommand.WarpHoldOn, BindingActivation.Hold),
            new ButtonBinding(BindableInput.LeftThumbstick, AppCommand.SwapJoystickPorts, BindingActivation.Toggle),
        };

        var gameplay = BindingProfile.Default.Gameplay;

        Assert.Equal(expected.Length, gameplay.Count);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], gameplay[i]); // ButtonBinding is a record: by-value
        }

        // RightTrigger and RightThumbstick are UNBOUND in Default.
        Assert.DoesNotContain(gameplay, b => b.Input == BindableInput.RightTrigger);
        Assert.DoesNotContain(gameplay, b => b.Input == BindableInput.RightThumbstick);
    }

    /// <summary>
    /// FR-SYSBTN-001 / TR-SYSBTN-002 (IMPL-XBOXUWP-010), TEST-SYSBTN-001
    /// Press-once guard.
    /// Use case: a Press binding (e.g. X -> AutostartDrive8) fires its action on the
    /// button-down edge and must not repeat while the button is held down.
    /// Acceptance: over a three-frame held sequence (release-&gt;down, down, down) the
    /// command is emitted exactly once, on the first frame only.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void Press_FiresOnceAcrossHeldSequence()
    {
        var profile = BindingProfile.Default;
        var config = XboxInputConfig.Default;
        var latch = SystemButtonLatch.Initial;

        var none = Buttons(GamepadButtonFlags.None);
        var xHeld = Buttons(GamepadButtonFlags.X);

        // Frame 1: down edge -> exactly one AutostartDrive8.
        var f1 = new List<AppCommand>();
        latch = XboxSystemButtons.Evaluate(none, xHeld, profile, config, latch, f1);
        Assert.Equal(new[] { AppCommand.AutostartDrive8 }, f1);

        // Frame 2: still held -> nothing.
        var f2 = new List<AppCommand>();
        latch = XboxSystemButtons.Evaluate(xHeld, xHeld, profile, config, latch, f2);
        Assert.Empty(f2);

        // Frame 3: still held -> nothing.
        var f3 = new List<AppCommand>();
        _ = XboxSystemButtons.Evaluate(xHeld, xHeld, profile, config, latch, f3);
        Assert.Empty(f3);
    }

    /// <summary>
    /// FR-SYSBTN-001 / TR-SYSBTN-002 (IMPL-XBOXUWP-010), TEST-SYSBTN-001
    /// Toggle-per-edge guard.
    /// Use case: a Toggle binding (e.g. Menu -> OpenMainMenu) emits its command once
    /// per button-down edge; a downstream flips the actual state, so at the evaluator
    /// it is one emit per down edge (held frames and the up edge emit nothing).
    /// Acceptance: down -> one OpenMainMenu; held -> none; up -> none; a second down
    /// -> one OpenMainMenu again.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void Toggle_EmitsPerDownEdge()
    {
        var profile = BindingProfile.Default;
        var config = XboxInputConfig.Default;
        var latch = SystemButtonLatch.Initial;

        var none = Buttons(GamepadButtonFlags.None);
        var menu = Buttons(GamepadButtonFlags.Menu);

        // Down edge -> one.
        var f1 = new List<AppCommand>();
        latch = XboxSystemButtons.Evaluate(none, menu, profile, config, latch, f1);
        Assert.Equal(new[] { AppCommand.OpenMainMenu }, f1);

        // Held -> none.
        var f2 = new List<AppCommand>();
        latch = XboxSystemButtons.Evaluate(menu, menu, profile, config, latch, f2);
        Assert.Empty(f2);

        // Up edge -> none (Toggle only fires on the down edge).
        var f3 = new List<AppCommand>();
        latch = XboxSystemButtons.Evaluate(menu, none, profile, config, latch, f3);
        Assert.Empty(f3);

        // Second down edge -> one again.
        var f4 = new List<AppCommand>();
        _ = XboxSystemButtons.Evaluate(none, menu, profile, config, latch, f4);
        Assert.Equal(new[] { AppCommand.OpenMainMenu }, f4);
    }

    /// <summary>
    /// FR-SYSBTN-005 / TR-SYSBTN-002 (IMPL-XBOXUWP-010), TEST-SYSBTN-001
    /// LeftTrigger hold-to-warp hysteresis guard.
    /// Use case: the analog LeftTrigger drives warp while held, with hysteresis so a
    /// trigger hovering near the threshold does not chatter warp on and off.
    /// Acceptance: rising 0.0 -&gt; 0.7 emits exactly one WarpHoldOn; then falling
    /// 0.7 -&gt; 0.3 emits exactly one WarpHoldOff; a sequence staying inside the
    /// 0.4..0.6 band emits nothing.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void LeftTrigger_Hysteresis_OnRising_OffFalling_NoneInBand()
    {
        var profile = BindingProfile.Default;
        var config = XboxInputConfig.Default;
        var latch = SystemButtonLatch.Initial;

        // Rising 0.0 -> 0.7 crosses UP through activate (0.6): one WarpHoldOn.
        var rise = new List<AppCommand>();
        latch = XboxSystemButtons.Evaluate(LeftTrigger(0.0), LeftTrigger(0.7), profile, config, latch, rise);
        Assert.Equal(new[] { AppCommand.WarpHoldOn }, rise);

        // Falling 0.7 -> 0.3 crosses DOWN through release (0.4): one WarpHoldOff.
        var fall = new List<AppCommand>();
        latch = XboxSystemButtons.Evaluate(LeftTrigger(0.7), LeftTrigger(0.3), profile, config, latch, fall);
        Assert.Equal(new[] { AppCommand.WarpHoldOff }, fall);

        // Now from a released latch, hover strictly inside the 0.4..0.6 band across
        // several frames: no crossing, so nothing is emitted.
        var band = SystemButtonLatch.Initial;
        double[] inBand = { 0.5, 0.45, 0.55, 0.5 };
        double prev = 0.5;
        foreach (double v in inBand)
        {
            var frame = new List<AppCommand>();
            band = XboxSystemButtons.Evaluate(LeftTrigger(prev), LeftTrigger(v), profile, config, band, frame);
            Assert.Empty(frame);
            prev = v;
        }
    }

    /// <summary>
    /// FR-SYSBTN-006 / TR-SYSBTN-002 (IMPL-XBOXUWP-010), TEST-SYSBTN-001
    /// swap-flag guard.
    /// Use case: the LeftThumbstick-press Toggle swaps the two joystick ports; at the
    /// evaluator it emits SwapJoystickPorts once on the down edge, and the
    /// <see cref="XboxSystemButtons.ApplySwap"/> helper flips
    /// <see cref="XboxInputConfig.SwapPorts"/> for that command.
    /// Acceptance: a LeftThumbstick down edge emits SwapJoystickPorts, and applying it
    /// flips SwapPorts false-&gt;true; applying it again flips it back true-&gt;false.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void LeftThumbstick_DownEdge_FlipsSwapPorts()
    {
        var profile = BindingProfile.Default;
        var config = XboxInputConfig.Default;
        Assert.False(config.SwapPorts);

        var none = Buttons(GamepadButtonFlags.None);
        var stick = Buttons(GamepadButtonFlags.LeftThumbstick);

        var commands = new List<AppCommand>();
        _ = XboxSystemButtons.Evaluate(none, stick, profile, config, SystemButtonLatch.Initial, commands);
        Assert.Contains(AppCommand.SwapJoystickPorts, commands);

        // ApplySwap flips the flag for SwapJoystickPorts (and only that command).
        var swapped = XboxSystemButtons.ApplySwap(AppCommand.SwapJoystickPorts, config);
        Assert.True(swapped.SwapPorts);

        var swappedBack = XboxSystemButtons.ApplySwap(AppCommand.SwapJoystickPorts, swapped);
        Assert.False(swappedBack.SwapPorts);

        // A non-swap command leaves SwapPorts untouched.
        var untouched = XboxSystemButtons.ApplySwap(AppCommand.OpenMainMenu, config);
        Assert.Equal(config.SwapPorts, untouched.SwapPorts);
    }

    /// <summary>
    /// FR-SYSBTN-001 / TR-SYSBTN-001 (IMPL-XBOXUWP-010), TEST-SYSBTN-001
    /// no-static-mutable-state guard.
    /// Use case: the evaluator must hold NO static mutable state (the hysteresis latch
    /// is a parameter, not a field) so it is thread-safe and deterministic.
    /// Acceptance: <see cref="XboxSystemButtons"/> declares no static field that is
    /// not const (literal) or readonly (init-only).
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void Evaluator_HasNoStaticMutableFields()
    {
        var mutable = typeof(XboxSystemButtons)
            .GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(f => !f.IsLiteral && !f.IsInitOnly)
            .Select(f => f.Name)
            .ToArray();

        Assert.Empty(mutable);
    }

    /// <summary>
    /// FR-SYSBTN-001 / TR-SYSBTN-001 (IMPL-XBOXUWP-010), TEST-SYSBTN-001
    /// no-P/Invoke guard.
    /// Use case: the portable input library must carry no native interop, so it links
    /// clean under Native AOT and inside the UWP AppContainer.
    /// Acceptance: no method declared on <see cref="XboxSystemButtons"/> carries a
    /// <c>DllImport</c> or <c>LibraryImport</c> attribute.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void Evaluator_DeclaresNoPInvoke()
    {
        var methods = typeof(XboxSystemButtons).GetMethods(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);

        foreach (var method in methods)
        {
            Assert.Null(method.GetCustomAttribute<DllImportAttribute>());
            Assert.DoesNotContain(method.GetCustomAttributes(), a => a.GetType().Name == "LibraryImportAttribute");
        }
    }
}
