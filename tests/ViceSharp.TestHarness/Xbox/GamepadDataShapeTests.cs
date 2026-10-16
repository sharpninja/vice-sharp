namespace ViceSharp.TestHarness.Xbox;

using System;
using System.Linq;
using ViceSharp.Xbox.Input;
using Xunit;

/// <summary>
/// PLAN-XBOXUWP slice S7 (IMPL-XBOXUWP-007). Guards the portable input data
/// shapes in <c>ViceSharp.Xbox.Input</c> - the shared, unit-testable input
/// layer the mapper (S8/S9) and context machine (S10/S11) build on. These are
/// pure POCOs (records/enums) with zero <c>Windows.*</c>/WinRT/Grpc dependency
/// so they compile and run off-console under AOT + trim analysis.
/// </summary>
[Trait("Category", "Xbox")]
public sealed class GamepadDataShapeTests
{
    /// <summary>
    /// FR-GAMEPAD-003 / FR-GAMEPAD-005 / TR-GAMEPAD-002 (IMPL-XBOXUWP-007),
    /// TEST-GAMEPAD-001 bit-layout guard.
    /// Use case: the (later) UWP adapter casts <c>Windows.Gaming.Input.GamepadButtons</c>
    /// straight to <see cref="GamepadButtonFlags"/> with no per-bit remap, so the
    /// bit values MUST be WinRT-identical (S7 pins them with an assert).
    /// Acceptance: each named flag equals its WinRT-identical bit value
    /// (Menu=0x1, View=0x2, A=0x4, B=0x8, X=0x10, Y=0x20, DPadUp=0x40,
    /// LeftShoulder=0x400, ...); paddles occupy 0x4000..0x20000.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void GamepadButtonFlags_BitValues_AreWinRtIdentical()
    {
        Assert.Equal((GamepadButtonFlags)0x0, GamepadButtonFlags.None);
        Assert.Equal((GamepadButtonFlags)0x1, GamepadButtonFlags.Menu);
        Assert.Equal((GamepadButtonFlags)0x2, GamepadButtonFlags.View);
        Assert.Equal((GamepadButtonFlags)0x4, GamepadButtonFlags.A);
        Assert.Equal((GamepadButtonFlags)0x8, GamepadButtonFlags.B);
        Assert.Equal((GamepadButtonFlags)0x10, GamepadButtonFlags.X);
        Assert.Equal((GamepadButtonFlags)0x20, GamepadButtonFlags.Y);
        Assert.Equal((GamepadButtonFlags)0x40, GamepadButtonFlags.DPadUp);
        Assert.Equal((GamepadButtonFlags)0x80, GamepadButtonFlags.DPadDown);
        Assert.Equal((GamepadButtonFlags)0x100, GamepadButtonFlags.DPadLeft);
        Assert.Equal((GamepadButtonFlags)0x200, GamepadButtonFlags.DPadRight);
        Assert.Equal((GamepadButtonFlags)0x400, GamepadButtonFlags.LeftShoulder);
        Assert.Equal((GamepadButtonFlags)0x800, GamepadButtonFlags.RightShoulder);
        Assert.Equal((GamepadButtonFlags)0x1000, GamepadButtonFlags.LeftThumbstick);
        Assert.Equal((GamepadButtonFlags)0x2000, GamepadButtonFlags.RightThumbstick);
        Assert.Equal((GamepadButtonFlags)0x4000, GamepadButtonFlags.Paddle1);
        Assert.Equal((GamepadButtonFlags)0x8000, GamepadButtonFlags.Paddle2);
        Assert.Equal((GamepadButtonFlags)0x10000, GamepadButtonFlags.Paddle3);
        Assert.Equal((GamepadButtonFlags)0x20000, GamepadButtonFlags.Paddle4);
    }

    /// <summary>
    /// FR-GAMEPAD-009 / TR-GAMEPAD-001 (IMPL-XBOXUWP-007), TEST-GAMEPAD-001
    /// shell-reserved-member guard.
    /// Use case: the Nexus/Guide (Xbox) button is reserved by the shell and never
    /// surfaces to the app, so it MUST NOT be a member of the flags enum.
    /// Acceptance: <see cref="GamepadButtonFlags"/> declares no member named
    /// "Nexus" or "Guide"; it is backed by <see cref="uint"/>.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void GamepadButtonFlags_HasNoShellReservedMember()
    {
        var names = Enum.GetNames<GamepadButtonFlags>();
        Assert.DoesNotContain("Nexus", names);
        Assert.DoesNotContain("Guide", names);
        Assert.Equal(typeof(uint), Enum.GetUnderlyingType(typeof(GamepadButtonFlags)));
    }

    /// <summary>
    /// FR-GAMEPAD-004 / TR-GAMEPAD-002 (IMPL-XBOXUWP-007), TEST-GAMEPAD-001
    /// neutral-default guard.
    /// Use case: the fail-safe / no-input snapshot must be an all-zero reading so
    /// a centered pad quantizes to no movement and no buttons.
    /// Acceptance: <see cref="GamepadSnapshot.Neutral"/> has every axis 0.0,
    /// Buttons == None, Timestamp == 0, and equals <c>default</c>.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void GamepadSnapshot_Neutral_IsAllZero()
    {
        var n = GamepadSnapshot.Neutral;
        Assert.Equal(0.0, n.LeftStickX);
        Assert.Equal(0.0, n.LeftStickY);
        Assert.Equal(0.0, n.RightStickX);
        Assert.Equal(0.0, n.RightStickY);
        Assert.Equal(0.0, n.LeftTrigger);
        Assert.Equal(0.0, n.RightTrigger);
        Assert.Equal(GamepadButtonFlags.None, n.Buttons);
        Assert.Equal(0UL, n.Timestamp);
        Assert.Equal(default, n);
    }

    /// <summary>
    /// FR-GAMEPAD-001 / FR-GAMEPAD-008 / TR-GAMEPAD-002 (IMPL-XBOXUWP-007),
    /// TEST-GAMEPAD-001 neutral-port guard.
    /// Use case: the fail-safe centered port (disconnect/no-pad) must be no
    /// direction and no fire, matching the C64 joystick direction bits.
    /// Acceptance: <see cref="JoystickPortState.Neutral"/> equals
    /// <c>(DirectionMask: 0, Fire: false)</c> and the direction bit constants are
    /// Up=0x01, Down=0x02, Left=0x04, Right=0x08.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void JoystickPortState_Neutral_IsZeroAndFalse()
    {
        var n = JoystickPortState.Neutral;
        Assert.Equal((byte)0, n.DirectionMask);
        Assert.False(n.Fire);
        Assert.Equal(new JoystickPortState(0, false), n);
        Assert.Equal((byte)0x01, JoystickPortState.Up);
        Assert.Equal((byte)0x02, JoystickPortState.Down);
        Assert.Equal((byte)0x04, JoystickPortState.Left);
        Assert.Equal((byte)0x08, JoystickPortState.Right);
    }

    /// <summary>
    /// FR-GAMEPAD-004 / FR-GAMEPAD-007 / TR-GAMEPAD-002 (IMPL-XBOXUWP-007),
    /// TEST-GAMEPAD-001 frozen-defaults guard.
    /// Use case: S8/S9 golden quantization vectors depend on exact-literal default
    /// thresholds, so <see cref="XboxInputConfig.Default"/> must be frozen.
    /// Acceptance: Default == (StickDeadzone 0.30, DiagonalThreshold 0.5,
    /// ActivateThreshold 0.55, ReleaseThreshold 0.40, SwapPorts false).
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void XboxInputConfig_Default_HasFrozenLiterals()
    {
        var d = XboxInputConfig.Default;
        Assert.Equal(0.30, d.StickDeadzone);
        Assert.Equal(0.5, d.DiagonalThreshold);
        Assert.Equal(0.55, d.ActivateThreshold);
        Assert.Equal(0.40, d.ReleaseThreshold);
        Assert.False(d.SwapPorts);
        Assert.Equal(new XboxInputConfig(0.30, 0.5, 0.55, 0.40, false), d);
    }

    /// <summary>
    /// TR-GAMEPAD-001 / TR-GAMEPAD-002 (IMPL-XBOXUWP-007), TEST-GAMEPAD-001
    /// portability guard.
    /// Use case: the input library must be AOT-safe and free of any
    /// <c>Windows.*</c>/WinRT/Grpc/AspNetCore/Avalonia dependency so it links into
    /// the console head and runs off-console under trim analysis.
    /// Acceptance: the types live in assembly "ViceSharp.Xbox.Input", and that
    /// assembly's referenced assemblies contain no "Windows", "WinRT", "Grpc",
    /// "Microsoft.AspNetCore", or "Avalonia" assembly.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void XboxInput_Assembly_HasNoNonPortableReferences()
    {
        var assembly = typeof(GamepadButtonFlags).Assembly;
        Assert.Equal("ViceSharp.Xbox.Input", assembly.GetName().Name);

        var forbidden = new[] { "Windows", "WinRT", "Grpc", "Microsoft.AspNetCore", "Avalonia" };
        var referenced = assembly.GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToArray();

        foreach (var name in referenced)
        {
            foreach (var bad in forbidden)
            {
                Assert.False(
                    name.Contains(bad, StringComparison.OrdinalIgnoreCase),
                    $"ViceSharp.Xbox.Input must not reference '{bad}'-family assembly, but references '{name}'.");
            }
        }
    }
}
