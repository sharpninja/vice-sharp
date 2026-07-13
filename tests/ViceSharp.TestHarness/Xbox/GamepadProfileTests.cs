namespace ViceSharp.TestHarness.Xbox;

using ViceSharp.Xbox.Input;
using Xunit;

/// <summary>
/// PLAN-XBOXUWP slice S9 (IMPL-XBOXUWP-009). Profile/config guard for the pure
/// joystick mapper <see cref="XboxJoystickMapper.Map"/> in
/// <c>ViceSharp.Xbox.Input</c>: the <see cref="XboxInputConfig.SwapPorts"/>
/// remap (swap-immune-at-emit: the mapper decides which explicit port each bundle
/// drives), tuning-threshold profiles (deadzone/activate change the mask), and the
/// pre-SOCD per-stick hysteresis latch threaded through <see cref="MapperState"/>.
/// </summary>
/// <remarks>
/// Bits: Up=0x01, Down=0x02, Left=0x04, Right=0x08. LOCKED mapping (no swap):
/// left stick + D-pad + A -> JOY2 (primary); right stick + B -> JOY1 (secondary).
/// With SwapPorts the whole bundle (mask AND fire) follows to the opposite port.
/// </remarks>
[Trait("Category", "Xbox")]
public sealed class GamepadProfileTests
{
    private const byte Up = JoystickPortState.Up;       // 0x01
    private const byte Right = JoystickPortState.Right; // 0x08

    private static GamepadSnapshot Reading(
        double lx = 0,
        double ly = 0,
        double rx = 0,
        double ry = 0,
        GamepadButtonFlags buttons = GamepadButtonFlags.None) =>
        new(lx, ly, rx, ry, 0.0, 0.0, buttons, 0UL);

    /// <summary>
    /// FR-GAMEPAD-007 / TR-GAMEPAD-003 (IMPL-XBOXUWP-009), TEST-GAMEPAD-004
    /// swap-immune-at-emit guard.
    /// Use case: a "remap" profile that swaps the two ports must move the WHOLE
    /// primary bundle (direction mask AND fire together) to the opposite explicit
    /// port, so a downstream that always emits to InputPort.Joystick1/Joystick2
    /// needs no swap awareness.
    /// Acceptance: for the same input (left stick Right + DPadUp + A on the primary
    /// side; right stick Up + B on the secondary side), SwapPorts=false puts the
    /// primary bundle on Joy2 and secondary on Joy1; SwapPorts=true puts the
    /// primary bundle (0x09 mask, A fire) on Joy1 and the secondary bundle (Up
    /// mask, B fire) on Joy2 - every field follows the swap.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void SwapPorts_MovesWholeBundle_MaskAndFireFollow()
    {
        var reading = Reading(
            lx: 1.0, ly: 0.0,          // primary (left stick) -> Right
            rx: 0.0, ry: 1.0,          // secondary (right stick) -> Up
            buttons: GamepadButtonFlags.DPadUp | GamepadButtonFlags.A | GamepadButtonFlags.B);

        // No swap: primary bundle on Joy2, secondary bundle on Joy1.
        var noSwap = XboxJoystickMapper.Map(reading, XboxInputConfig.Default, MapperState.Initial);
        Assert.Equal((byte)(Up | Right), noSwap.Joy2.DirectionMask); // primary mask
        Assert.True(noSwap.Joy2.Fire);                               // A fires primary (Joy2)
        Assert.Equal(Up, noSwap.Joy1.DirectionMask);                 // secondary mask
        Assert.True(noSwap.Joy1.Fire);                               // B fires secondary (Joy1)

        // Swap: the whole primary bundle now drives Joy1, secondary drives Joy2.
        var swapped = XboxJoystickMapper.Map(
            reading,
            XboxInputConfig.Default with { SwapPorts = true },
            MapperState.Initial);

        Assert.Equal((byte)(Up | Right), swapped.Joy1.DirectionMask); // primary mask now on Joy1
        Assert.True(swapped.Joy1.Fire);                               // A now fires Joy1
        Assert.Equal(Up, swapped.Joy2.DirectionMask);                 // secondary mask now on Joy2
        Assert.True(swapped.Joy2.Fire);                              // B now fires Joy2
    }

    /// <summary>
    /// FR-GAMEPAD-007 / TR-GAMEPAD-003 (IMPL-XBOXUWP-009), TEST-GAMEPAD-004
    /// tuning-profile guard.
    /// Use case: swapping the tuning profile must change the quantization - a
    /// mid-magnitude push that is inside the Default 0.30 deadzone (and so reads as
    /// Neutral) becomes an active direction under a low-deadzone/low-activate
    /// profile.
    /// Acceptance: left stick (x=0.2,y=0) yields Joy2.DirectionMask == 0 under
    /// Default (0.2 &lt; 0.30 deadzone), but == 0x08 (Right) under a config with
    /// StickDeadzone 0.05, DiagonalThreshold 0.1, ActivateThreshold 0.15,
    /// ReleaseThreshold 0.10.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void Profile_MidPush_NeutralUnderDefault_ActiveUnderLowDeadzone()
    {
        var reading = Reading(lx: 0.2, ly: 0.0);

        var underDefault = XboxJoystickMapper.Map(reading, XboxInputConfig.Default, MapperState.Initial);
        Assert.Equal((byte)0, underDefault.Joy2.DirectionMask); // inside Default's 0.30 deadzone

        var sensitive = new XboxInputConfig(
            StickDeadzone: 0.05,
            DiagonalThreshold: 0.1,
            ActivateThreshold: 0.15,
            ReleaseThreshold: 0.10,
            SwapPorts: false);
        var underSensitive = XboxJoystickMapper.Map(reading, sensitive, MapperState.Initial);
        Assert.Equal(Right, underSensitive.Joy2.DirectionMask); // 0.2 clears the 0.05 deadzone + 0.15 activate
    }

    /// <summary>
    /// FR-GAMEPAD-006 / TR-GAMEPAD-003 (IMPL-XBOXUWP-009), TEST-GAMEPAD-004
    /// latch-output guard.
    /// Use case: the returned <see cref="MapperState"/> must carry each stick's
    /// PRE-SOCD, pre-D-pad raw converter output so the next frame can resolve
    /// hysteresis on the raw stick; the D-pad/SOCD merge must not pollute the
    /// latch.
    /// Acceptance: with left stick Up (0,1) + DPadDown (whose merge SOCD-clears the
    /// primary mask to 0), Next.LeftStickBits is still the raw Up bit (0x01), and
    /// Next.RightStickBits reflects the raw right-stick output (0 when centered).
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void Latch_StoresPreSocdRawStickBits_NotMergedResult()
    {
        var reading = Reading(lx: 0.0, ly: 1.0, buttons: GamepadButtonFlags.DPadDown);

        var (_, joy2, next) = XboxJoystickMapper.Map(reading, XboxInputConfig.Default, MapperState.Initial);

        Assert.Equal((byte)0, joy2.DirectionMask);  // merged+SOCD primary is cleared
        Assert.Equal(Up, next.LeftStickBits);       // but the latch keeps the raw stick Up bit
        Assert.Equal((byte)0, next.RightStickBits); // right stick centered
    }

    /// <summary>
    /// FR-GAMEPAD-006 / TR-GAMEPAD-003 (IMPL-XBOXUWP-009), TEST-GAMEPAD-004
    /// hysteresis-threading guard.
    /// Use case: threading the prior latch back in must preserve per-stick
    /// hysteresis frame to frame - a left stick hovering in the (Release, Activate)
    /// band holds its prior on/off state depending on the latch fed in.
    /// Acceptance: with left stick y=0.5 (inside the 0.40..0.55 band, magnitude
    /// clears the 0.30 deadzone), a prior latch of LeftStickBits=Up holds Up on
    /// Joy2, while a prior latch of 0 stays 0.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void Latch_PriorThreadedIn_PreservesStickHysteresis()
    {
        var reading = Reading(lx: 0.0, ly: 0.5);

        var heldOn = XboxJoystickMapper.Map(
            reading, XboxInputConfig.Default, new MapperState(LeftStickBits: Up, RightStickBits: 0));
        Assert.Equal(Up, heldOn.Joy2.DirectionMask);
        Assert.Equal(Up, heldOn.Next.LeftStickBits);

        var heldOff = XboxJoystickMapper.Map(
            reading, XboxInputConfig.Default, MapperState.Initial);
        Assert.Equal((byte)0, heldOff.Joy2.DirectionMask);
        Assert.Equal((byte)0, heldOff.Next.LeftStickBits);
    }
}
