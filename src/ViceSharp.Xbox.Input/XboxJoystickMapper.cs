namespace ViceSharp.Xbox.Input;

/// <summary>
/// The SOCD (Simultaneous Opposing Cardinal Directions) resolution policy applied
/// after the D-pad is merged into a stick's direction mask.
/// </summary>
/// <remarks>
/// Only <see cref="Neutral"/> exists for now: when both directions of an opposing
/// pair (Up+Down or Left+Right) are set, both are cleared. This is the safe,
/// hardware-faithful choice for a C64 joystick, which can never express an
/// opposing pair.
/// </remarks>
public enum SocdMode
{
    /// <summary>Both directions of an opposing pair clear to neither.</summary>
    Neutral,
}

/// <summary>
/// The per-stick hysteresis latch threaded through
/// <see cref="XboxJoystickMapper.Map"/>: the previous frame's raw
/// <see cref="StickConverter.ToDirectionMask"/> output for each analog stick,
/// carried prior-in / next-out so the mapper stays free of static state.
/// </summary>
/// <remarks>
/// The latch stores the <b>pre-SOCD, pre-D-pad</b> converter output for each stick
/// so per-stick hysteresis is preserved frame to frame; the D-pad merge and SOCD
/// pass are recomputed fresh on top of the raw stick masks every frame.
/// </remarks>
/// <param name="LeftStickBits">Prior raw direction mask from the left thumbstick.</param>
/// <param name="RightStickBits">Prior raw direction mask from the right thumbstick.</param>
public readonly record struct MapperState(byte LeftStickBits, byte RightStickBits)
{
    /// <summary>
    /// The initial latch: both sticks centered (mask 0). Equal to <c>default</c>.
    /// </summary>
    public static MapperState Initial => default;
}

/// <summary>
/// The pure, allocation-free joystick mapper (S9): merges the two analog sticks,
/// the D-pad, and the A/B fire buttons of one <see cref="GamepadSnapshot"/> into
/// two resolved <see cref="JoystickPortState"/> ports under the LOCKED control
/// scheme.
/// </summary>
/// <remarks>
/// <para>
/// FR-GAMEPAD-006 / FR-GAMEPAD-007 / TR-GAMEPAD-003 (PLAN-XBOXUWP S9,
/// IMPL-XBOXUWP-009). LOCKED mapping: the <b>left stick + D-pad</b> and the
/// <b>A</b> button form the <i>primary</i> bundle; the <b>right stick</b> and the
/// <b>B</b> button form the <i>secondary</i> bundle. Without swap the primary
/// bundle drives JOY2 and the secondary drives JOY1; with
/// <see cref="XboxInputConfig.SwapPorts"/> the whole bundle (direction mask AND
/// fire together) drives the opposite explicit port. This is
/// <i>swap-immune-at-emit</i>: the mapper decides which physical port each bundle
/// drives here, so a downstream always emits Joy1/Joy2 to
/// <c>InputPort.Joystick1</c>/<c>InputPort.Joystick2</c> with no swap awareness.
/// </para>
/// <para>
/// The mapper is deterministic, allocation-free, and holds <b>no</b> static mutable
/// state: identical <c>(reading, config, prior)</c> yields an identical
/// <c>(Joy1, Joy2, Next)</c> tuple on every call. The hysteresis latch is threaded
/// through the <see cref="MapperState"/> parameter and return value; the latch
/// stores each stick's pre-SOCD/pre-D-pad converter output so raw-stick hysteresis
/// survives frame to frame.
/// </para>
/// </remarks>
public static class XboxJoystickMapper
{
    private const byte UpDown = (byte)(JoystickPortState.Up | JoystickPortState.Down);       // 0x03
    private const byte LeftRight = (byte)(JoystickPortState.Left | JoystickPortState.Right);  // 0x0C

    /// <summary>
    /// Maps one gamepad reading to the two joystick ports plus the next latch.
    /// </summary>
    /// <param name="reading">The gamepad snapshot for this frame.</param>
    /// <param name="config">The tuning thresholds and swap flag.</param>
    /// <param name="prior">The per-stick hysteresis latch from the previous frame.</param>
    /// <returns>
    /// The resolved <c>(Joy1, Joy2, Next)</c> tuple: the two explicit joystick
    /// ports and the latch to thread into the next frame.
    /// </returns>
    public static (JoystickPortState Joy1, JoystickPortState Joy2, MapperState Next) Map(
        in GamepadSnapshot reading,
        in XboxInputConfig config,
        in MapperState prior)
    {
        GamepadButtonFlags buttons = reading.Buttons;

        // 1. Left thumbstick -> raw direction mask (this is what the latch stores).
        byte leftMask = StickConverter.ToDirectionMask(
            reading.LeftStickX, reading.LeftStickY, prior.LeftStickBits, in config);
        byte nextLeftBits = leftMask;

        // 2. D-pad -> direction bits (digital, no hysteresis).
        byte dpadMask = 0;
        if ((buttons & GamepadButtonFlags.DPadUp) != 0)
        {
            dpadMask |= JoystickPortState.Up;
        }

        if ((buttons & GamepadButtonFlags.DPadDown) != 0)
        {
            dpadMask |= JoystickPortState.Down;
        }

        if ((buttons & GamepadButtonFlags.DPadLeft) != 0)
        {
            dpadMask |= JoystickPortState.Left;
        }

        if ((buttons & GamepadButtonFlags.DPadRight) != 0)
        {
            dpadMask |= JoystickPortState.Right;
        }

        // 3. Merge the D-pad into the left-stick target, THEN resolve SOCD. The
        //    D-pad can introduce an opposing bit against the stick, so SOCD runs
        //    after the merge.
        byte primaryMask = ApplySocd((byte)(leftMask | dpadMask), SocdMode.Neutral);

        // 4. A fires the primary bundle.
        bool primaryFire = (buttons & GamepadButtonFlags.A) != 0;

        // 5. Right thumbstick -> raw mask (latched), SOCD only (no D-pad on right).
        byte rightMask = StickConverter.ToDirectionMask(
            reading.RightStickX, reading.RightStickY, prior.RightStickBits, in config);
        byte nextRightBits = rightMask;
        byte secondaryMask = ApplySocd(rightMask, SocdMode.Neutral);
        bool secondaryFire = (buttons & GamepadButtonFlags.B) != 0;

        var primary = new JoystickPortState(primaryMask, primaryFire);
        var secondary = new JoystickPortState(secondaryMask, secondaryFire);

        // 6. Emit to explicit ports. No swap: JOY2 = primary, JOY1 = secondary.
        //    Swap: the whole bundle (mask AND fire) follows to the opposite port.
        JoystickPortState joy1;
        JoystickPortState joy2;
        if (config.SwapPorts)
        {
            joy1 = primary;
            joy2 = secondary;
        }
        else
        {
            joy1 = secondary;
            joy2 = primary;
        }

        // 7. The latch carries the pre-SOCD per-stick converter output.
        return (joy1, joy2, new MapperState(nextLeftBits, nextRightBits));
    }

    /// <summary>
    /// Applies the SOCD policy to a merged direction mask. Pure; reads no shared
    /// state.
    /// </summary>
    private static byte ApplySocd(byte mask, SocdMode mode) => mode switch
    {
        // Neutral: an opposing pair (Up+Down or Left+Right) resolves to neither.
        SocdMode.Neutral => ResolveNeutral(mask),
        _ => mask,
    };

    private static byte ResolveNeutral(byte mask)
    {
        if ((mask & UpDown) == UpDown)
        {
            mask &= unchecked((byte)~UpDown);
        }

        if ((mask & LeftRight) == LeftRight)
        {
            mask &= unchecked((byte)~LeftRight);
        }

        return mask;
    }
}
