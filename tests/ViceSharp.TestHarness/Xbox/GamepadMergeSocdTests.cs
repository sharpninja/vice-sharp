namespace ViceSharp.TestHarness.Xbox;

using System.Linq;
using System.Reflection;
using ViceSharp.Xbox.Input;
using Xunit;

/// <summary>
/// PLAN-XBOXUWP slice S9 (IMPL-XBOXUWP-009). Behavior guard for the pure joystick
/// mapper <see cref="XboxJoystickMapper.Map"/> in <c>ViceSharp.Xbox.Input</c>:
/// the D-pad + left-stick merge, SOCD-Neutral resolution, and the A/B fire
/// routing under the LOCKED control scheme (left stick + D-pad + A -> JOY2,
/// right stick + B -> JOY1). The mapper must be pure, allocation-free, and hold
/// NO static mutable state - the hysteresis latch is threaded as the
/// <see cref="MapperState"/> parameter.
/// </summary>
/// <remarks>
/// Bits: Up=0x01, Down=0x02, Left=0x04, Right=0x08 (C64 active-HIGH low nibble).
/// Sign convention matches Windows.Gaming.Input thumbsticks: +Y = up, -Y = down,
/// +X = right, -X = left. Vectors use <see cref="XboxInputConfig.Default"/>
/// (StickDeadzone 0.30, DiagonalThreshold 0.5, ActivateThreshold 0.55,
/// ReleaseThreshold 0.40) and <see cref="MapperState.Initial"/> unless stated.
/// </remarks>
[Trait("Category", "Xbox")]
public sealed class GamepadMergeSocdTests
{
    private const byte Up = JoystickPortState.Up;       // 0x01
    private const byte Down = JoystickPortState.Down;   // 0x02
    private const byte Left = JoystickPortState.Left;   // 0x04
    private const byte Right = JoystickPortState.Right; // 0x08

    private static GamepadSnapshot Reading(
        double lx = 0,
        double ly = 0,
        double rx = 0,
        double ry = 0,
        GamepadButtonFlags buttons = GamepadButtonFlags.None) =>
        new(lx, ly, rx, ry, 0.0, 0.0, buttons, 0UL);

    /// <summary>
    /// FR-GAMEPAD-006 / TR-GAMEPAD-003 (IMPL-XBOXUWP-009), TEST-GAMEPAD-003
    /// D-pad + left-stick merge guard.
    /// Use case: the D-pad must OR-merge into the left-stick target on JOY2 so a
    /// player can push the stick right and tap D-pad up for a diagonal, and the
    /// right port stays untouched.
    /// Acceptance: left stick full Right (x=1,y=0) + DPadUp -> Joy2.DirectionMask
    /// == 0x09 (Up|Right) and Joy1 == Neutral.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void Merge_LeftStickRightPlusDpadUp_RoutesUpRightToJoy2()
    {
        var reading = Reading(lx: 1.0, ly: 0.0, buttons: GamepadButtonFlags.DPadUp);

        var (joy1, joy2, _) = XboxJoystickMapper.Map(reading, XboxInputConfig.Default, MapperState.Initial);

        Assert.Equal((byte)(Up | Right), joy2.DirectionMask); // 0x09
        Assert.Equal(JoystickPortState.Neutral, joy1);
    }

    /// <summary>
    /// FR-GAMEPAD-006 / TR-GAMEPAD-003 (IMPL-XBOXUWP-009), TEST-GAMEPAD-003
    /// right-stick routing guard.
    /// Use case: the right stick drives the secondary port (JOY1) only and never
    /// bleeds into the left-stick/D-pad primary port.
    /// Acceptance: right stick full Up (rx=0,ry=1) -> Joy1.DirectionMask == 0x01
    /// (Up) and Joy2.DirectionMask == 0 (unaffected by the right stick).
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void Merge_RightStickUp_RoutesToJoy1Only()
    {
        var reading = Reading(rx: 0.0, ry: 1.0);

        var (joy1, joy2, _) = XboxJoystickMapper.Map(reading, XboxInputConfig.Default, MapperState.Initial);

        Assert.Equal(Up, joy1.DirectionMask);
        Assert.Equal((byte)0, joy2.DirectionMask);
    }

    /// <summary>
    /// FR-GAMEPAD-006 / TR-GAMEPAD-003 (IMPL-XBOXUWP-009), TEST-GAMEPAD-003
    /// fire-routing guard.
    /// Use case: A must fire the primary port (JOY2) and B the secondary port
    /// (JOY1), each independently, so a player can fire on both ports at once.
    /// Acceptance: A alone -> Joy2.Fire true, Joy1.Fire false; B alone ->
    /// Joy1.Fire true, Joy2.Fire false; A+B -> both ports fire.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void Fire_AtoJoy2_BtoJoy1_Independently()
    {
        var (a1, a2, _) = XboxJoystickMapper.Map(
            Reading(buttons: GamepadButtonFlags.A), XboxInputConfig.Default, MapperState.Initial);
        Assert.True(a2.Fire);
        Assert.False(a1.Fire);

        var (b1, b2, _) = XboxJoystickMapper.Map(
            Reading(buttons: GamepadButtonFlags.B), XboxInputConfig.Default, MapperState.Initial);
        Assert.True(b1.Fire);
        Assert.False(b2.Fire);

        var (ab1, ab2, _) = XboxJoystickMapper.Map(
            Reading(buttons: GamepadButtonFlags.A | GamepadButtonFlags.B),
            XboxInputConfig.Default,
            MapperState.Initial);
        Assert.True(ab2.Fire);
        Assert.True(ab1.Fire);
    }

    /// <summary>
    /// FR-GAMEPAD-006 / TR-GAMEPAD-003 (IMPL-XBOXUWP-009), TEST-GAMEPAD-003
    /// SOCD Up/Down guard.
    /// Use case: the D-pad can create an opposing bit against the stick (stick up
    /// while D-pad down); SOCD-Neutral must run AFTER the merge and clear both, so
    /// the C64 never sees an impossible Up+Down state.
    /// Acceptance: left stick full Up (0,1) + DPadDown -> the Up|Down pair clears,
    /// so Joy2.DirectionMask has neither Up nor Down set.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void Socd_StickUpVsDpadDown_ClearsBothOnJoy2()
    {
        var reading = Reading(lx: 0.0, ly: 1.0, buttons: GamepadButtonFlags.DPadDown);

        var (_, joy2, _) = XboxJoystickMapper.Map(reading, XboxInputConfig.Default, MapperState.Initial);

        Assert.Equal((byte)0, (byte)(joy2.DirectionMask & (Up | Down)));
        Assert.Equal((byte)0, joy2.DirectionMask); // no other bits leak in either
    }

    /// <summary>
    /// FR-GAMEPAD-006 / TR-GAMEPAD-003 (IMPL-XBOXUWP-009), TEST-GAMEPAD-003
    /// SOCD Left/Right guard.
    /// Use case: same opposing-pair rule on the horizontal axis - stick left while
    /// D-pad right must resolve to neither, not to a jittering left-or-right.
    /// Acceptance: left stick full Left (-1,0) + DPadRight -> the Left|Right pair
    /// clears, so Joy2.DirectionMask has neither Left nor Right set.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void Socd_StickLeftVsDpadRight_ClearsBothOnJoy2()
    {
        var reading = Reading(lx: -1.0, ly: 0.0, buttons: GamepadButtonFlags.DPadRight);

        var (_, joy2, _) = XboxJoystickMapper.Map(reading, XboxInputConfig.Default, MapperState.Initial);

        Assert.Equal((byte)0, (byte)(joy2.DirectionMask & (Left | Right)));
        Assert.Equal((byte)0, joy2.DirectionMask);
    }

    /// <summary>
    /// FR-GAMEPAD-006 / TR-GAMEPAD-003 (IMPL-XBOXUWP-009), TEST-GAMEPAD-003
    /// determinism guard.
    /// Use case: identical (reading, config, prior) MUST yield an identical output
    /// tuple on every call so lockstep replay and snapshot comparison stay
    /// bit-exact; the mapper reads no time, random, or shared state.
    /// Acceptance: two Map calls with the same arguments return equal Joy1, Joy2,
    /// and Next.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void Purity_SameArgs_SameTuple()
    {
        var reading = Reading(lx: 0.7, ly: -0.6, rx: -0.9, ry: 0.2, buttons: GamepadButtonFlags.A | GamepadButtonFlags.DPadLeft);

        var first = XboxJoystickMapper.Map(reading, XboxInputConfig.Default, MapperState.Initial);
        var second = XboxJoystickMapper.Map(reading, XboxInputConfig.Default, MapperState.Initial);

        Assert.Equal(first.Joy1, second.Joy1);
        Assert.Equal(first.Joy2, second.Joy2);
        Assert.Equal(first.Next, second.Next);
    }

    /// <summary>
    /// FR-GAMEPAD-006 / TR-GAMEPAD-003 (IMPL-XBOXUWP-009), TEST-GAMEPAD-003
    /// no-static-state guard.
    /// Use case: the mapper must hold NO static mutable state (the hysteresis latch
    /// is threaded as a parameter, not a field) so it is thread-safe and
    /// deterministic.
    /// Acceptance: <see cref="XboxJoystickMapper"/> declares no static field that
    /// is not const (literal) or readonly (init-only).
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void Purity_MapperType_HasNoStaticMutableFields()
    {
        var mutable = typeof(XboxJoystickMapper)
            .GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(f => !f.IsLiteral && !f.IsInitOnly)
            .Select(f => f.Name)
            .ToArray();

        Assert.Empty(mutable);
    }
}
