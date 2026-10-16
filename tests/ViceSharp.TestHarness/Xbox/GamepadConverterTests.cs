namespace ViceSharp.TestHarness.Xbox;

using System;
using System.Linq;
using System.Reflection;
using ViceSharp.Xbox.Input;
using Xunit;

/// <summary>
/// PLAN-XBOXUWP slice S8 (IMPL-XBOXUWP-008). Golden-vector guard for the pure
/// analog-stick -> C64 8-way direction-mask converter
/// (<see cref="StickConverter.ToDirectionMask"/>) in <c>ViceSharp.Xbox.Input</c>.
/// The converter is the core quantization the full mapper (S9) builds on: it must
/// be deterministic, allocation-free, and hold NO static mutable state - the
/// hysteresis latch is threaded through the <c>priorMask</c> parameter.
/// </summary>
/// <remarks>
/// Vectors use <see cref="XboxInputConfig.Default"/> (StickDeadzone 0.30,
/// DiagonalThreshold 0.5, ActivateThreshold 0.55, ReleaseThreshold 0.40). Sign
/// convention matches Windows.Gaming.Input thumbsticks and the C64 active-HIGH low
/// nibble: +Y = up, -Y = down, +X = right, -X = left; bits Up=0x01, Down=0x02,
/// Left=0x04, Right=0x08.
/// </remarks>
[Trait("Category", "Xbox")]
public sealed class GamepadConverterTests
{
    private const byte Up = JoystickPortState.Up;       // 0x01
    private const byte Down = JoystickPortState.Down;   // 0x02
    private const byte Left = JoystickPortState.Left;   // 0x04
    private const byte Right = JoystickPortState.Right; // 0x08

    /// <summary>
    /// FR-GAMEPAD-006 / TR-GAMEPAD-003 (IMPL-XBOXUWP-008), TEST-GAMEPAD-002
    /// radial-deadzone guard.
    /// Use case: a barely-deflected or centered stick must quantize to no movement
    /// so hand tremor and stick slop do not register as joystick pushes.
    /// Acceptance: any (x,y) whose magnitude is below <c>StickDeadzone</c> (0.30)
    /// returns 0 regardless of <c>priorMask</c>.
    /// </summary>
    [Theory]
    [Trait("Category", "Xbox")]
    [InlineData(0.1, 0.0)]   // small horizontal nudge: |v| = 0.10
    [InlineData(0.0, 0.0)]   // dead center
    [InlineData(0.2, 0.2)]   // just inside the deadzone radius: |v| = 0.283 < 0.30
    public void Deadzone_BelowRadius_ReturnsZero(double x, double y)
    {
        // priorMask set to a fully-pressed mask to prove the deadzone gate wins
        // over the hysteresis latch (a released stick always centers).
        byte result = StickConverter.ToDirectionMask(x, y, priorMask: 0x0F, XboxInputConfig.Default);
        Assert.Equal((byte)0, result);
    }

    /// <summary>
    /// FR-GAMEPAD-006 / TR-GAMEPAD-003 (IMPL-XBOXUWP-008), TEST-GAMEPAD-002
    /// axis-sign guard.
    /// Use case: a full cardinal push must map to exactly the matching C64
    /// direction bit with the correct sign convention.
    /// Acceptance: (0,1)->Up, (0,-1)->Down, (1,0)->Right, (-1,0)->Left.
    /// </summary>
    [Theory]
    [Trait("Category", "Xbox")]
    [InlineData(0.0, 1.0, Up)]
    [InlineData(0.0, -1.0, Down)]
    [InlineData(1.0, 0.0, Right)]
    [InlineData(-1.0, 0.0, Left)]
    public void AxisSign_FullCardinal_MapsToSingleBit(double x, double y, byte expected)
    {
        byte result = StickConverter.ToDirectionMask(x, y, priorMask: 0, XboxInputConfig.Default);
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// FR-GAMEPAD-006 / TR-GAMEPAD-003 (IMPL-XBOXUWP-008), TEST-GAMEPAD-002
    /// diagonal guard.
    /// Use case: a strong diagonal push must engage both axes' direction bits so
    /// the C64 sees a true 8-way diagonal.
    /// Acceptance: (0.9,0.9) -> Up|Right (0x09).
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void Diagonal_StrongPush_EngagesBothAxes()
    {
        byte result = StickConverter.ToDirectionMask(0.9, 0.9, priorMask: 0, XboxInputConfig.Default);
        Assert.Equal((byte)(Up | Right), result); // 0x01 | 0x08 = 0x09
    }

    /// <summary>
    /// FR-GAMEPAD-006 / TR-GAMEPAD-003 (IMPL-XBOXUWP-008), TEST-GAMEPAD-002
    /// diagonal-suppression guard.
    /// Use case: a mostly-cardinal push with a weak secondary axis must NOT
    /// register a spurious diagonal; the non-dominant axis only contributes its
    /// bit once its magnitude reaches <c>DiagonalThreshold</c> (0.5).
    /// Acceptance: (1.0, 0.45) (horizontal dominant, |y|=0.45 &lt; 0.5) -> Right
    /// only; (1.0, 0.6) (|y|=0.6 &gt;= 0.55 activate) -> Up|Right.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void Diagonal_WeakSecondaryAxis_IsSuppressed()
    {
        // |y| = 0.45 is above deadzone but below DiagonalThreshold 0.5: on the
        // horizontal-dominant stick the vertical bit is dropped.
        byte weak = StickConverter.ToDirectionMask(1.0, 0.45, priorMask: 0, XboxInputConfig.Default);
        Assert.Equal(Right, weak);

        // |y| = 0.6 clears both DiagonalThreshold (0.5) and ActivateThreshold
        // (0.55): the vertical bit now joins.
        byte strong = StickConverter.ToDirectionMask(1.0, 0.6, priorMask: 0, XboxInputConfig.Default);
        Assert.Equal((byte)(Up | Right), strong);
    }

    /// <summary>
    /// FR-GAMEPAD-006 / TR-GAMEPAD-003 (IMPL-XBOXUWP-008), TEST-GAMEPAD-002
    /// hysteresis band-hold guard.
    /// Use case: a stick hovering in the (Release, Activate) band must not chatter;
    /// each direction holds its prior on/off state until the axis crosses an edge.
    /// Acceptance: with a component in the band (y=0.5, band = (0.40,0.55)),
    /// priorMask=Up holds Up, priorMask=0 stays 0.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void Hysteresis_InBand_HoldsPrior()
    {
        // y = 0.5 is inside the band and magnitude 0.5 clears the deadzone.
        byte heldOn = StickConverter.ToDirectionMask(0.0, 0.5, priorMask: Up, XboxInputConfig.Default);
        Assert.Equal(Up, heldOn);

        byte heldOff = StickConverter.ToDirectionMask(0.0, 0.5, priorMask: 0, XboxInputConfig.Default);
        Assert.Equal((byte)0, heldOff);
    }

    /// <summary>
    /// FR-GAMEPAD-006 / TR-GAMEPAD-003 (IMPL-XBOXUWP-008), TEST-GAMEPAD-002
    /// hysteresis edge-crossing guard.
    /// Use case: crossing above the activate edge turns a direction on regardless
    /// of prior; dropping below the release edge turns it off regardless of prior.
    /// Acceptance: y=0.6 (&gt;=0.55) with priorMask=0 -> Up; y=0.35 (&lt;=0.40, but
    /// magnitude 0.35 &gt;= deadzone 0.30) with priorMask=Up -> 0.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void Hysteresis_CrossingEdges_TurnsOnAndOff()
    {
        // Above ActivateThreshold from a released prior: turns ON.
        byte turnedOn = StickConverter.ToDirectionMask(0.0, 0.6, priorMask: 0, XboxInputConfig.Default);
        Assert.Equal(Up, turnedOn);

        // At/below ReleaseThreshold from a held prior, but still above the
        // deadzone (0.35 >= 0.30) so release, not the deadzone, drives it: OFF.
        byte turnedOff = StickConverter.ToDirectionMask(0.0, 0.35, priorMask: Up, XboxInputConfig.Default);
        Assert.Equal((byte)0, turnedOff);
    }

    /// <summary>
    /// FR-GAMEPAD-006 / TR-GAMEPAD-003 (IMPL-XBOXUWP-008), TEST-GAMEPAD-002
    /// mutual-exclusion guard.
    /// Use case: a single stick can never drive Up and Down (or Left and Right) at
    /// once - that is an impossible C64 joystick state.
    /// Acceptance: no output over a sweep of angles/priors sets both Up|Down or
    /// both Left|Right.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void OpposingDirections_AreNeverBothSet()
    {
        var config = XboxInputConfig.Default;
        byte[] priors = { 0, Up, Down, Left, Right, (byte)(Up | Right), 0x0F };
        for (int deg = 0; deg < 360; deg += 5)
        {
            double rad = deg * Math.PI / 180.0;
            double x = Math.Cos(rad);
            double y = Math.Sin(rad);
            foreach (byte prior in priors)
            {
                byte mask = StickConverter.ToDirectionMask(x, y, prior, config);
                Assert.NotEqual((byte)(Up | Down), (byte)(mask & (Up | Down)));
                Assert.NotEqual((byte)(Left | Right), (byte)(mask & (Left | Right)));
            }
        }
    }

    /// <summary>
    /// FR-GAMEPAD-006 / TR-GAMEPAD-003 (IMPL-XBOXUWP-008), TEST-GAMEPAD-002
    /// determinism guard.
    /// Use case: identical (x, y, priorMask, config) MUST yield identical output on
    /// every call so lockstep replay and snapshot comparison stay bit-exact.
    /// Acceptance: two calls with the same arguments return the same mask.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void Purity_SameArgs_SameResult()
    {
        var config = XboxInputConfig.Default;
        byte first = StickConverter.ToDirectionMask(0.7, -0.3, priorMask: Down, config);
        byte second = StickConverter.ToDirectionMask(0.7, -0.3, priorMask: Down, config);
        Assert.Equal(first, second);
    }

    /// <summary>
    /// FR-GAMEPAD-006 / TR-GAMEPAD-003 (IMPL-XBOXUWP-008), TEST-GAMEPAD-002
    /// no-static-state guard.
    /// Use case: the converter must hold NO static mutable state (the hysteresis
    /// latch is a parameter, not a field) so it is thread-safe and deterministic.
    /// Acceptance: <see cref="StickConverter"/> declares no static field that is
    /// not const (literal) or readonly (init-only).
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void Purity_ConverterType_HasNoStaticMutableFields()
    {
        var mutable = typeof(StickConverter)
            .GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(f => !f.IsLiteral && !f.IsInitOnly)
            .Select(f => f.Name)
            .ToArray();

        Assert.Empty(mutable);
    }
}
