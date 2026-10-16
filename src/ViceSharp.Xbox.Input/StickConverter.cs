namespace ViceSharp.Xbox.Input;

using System;

/// <summary>
/// The pure analog-stick -> C64 8-way direction-mask converter: the core
/// quantization the full gamepad mapper (S9) builds on. Turns one thumbstick's
/// (x, y) deflection into a C64 joystick direction mask over the low-nibble bits
/// <see cref="JoystickPortState.Up"/> (0x01), <see cref="JoystickPortState.Down"/>
/// (0x02), <see cref="JoystickPortState.Left"/> (0x04) and
/// <see cref="JoystickPortState.Right"/> (0x08).
/// </summary>
/// <remarks>
/// <para>
/// FR-GAMEPAD-006 / TR-GAMEPAD-003 (PLAN-XBOXUWP S8, IMPL-XBOXUWP-008). The
/// converter is deterministic, allocation-free, and holds <b>no</b> static mutable
/// state: it is a <c>static</c> class with only methods, so identical
/// <c>(x, y, priorMask, config)</c> yields identical output on every call. The
/// per-direction hysteresis latch is threaded through the <c>priorMask</c>
/// parameter (the previous frame's mask for this same stick); nothing is stored
/// between calls.
/// </para>
/// <para>
/// Sign convention matches <c>Windows.Gaming.Input</c> thumbsticks and the C64
/// active-HIGH low nibble: <c>+Y</c> is up, <c>-Y</c> is down, <c>+X</c> is right,
/// <c>-X</c> is left.
/// </para>
/// </remarks>
public static class StickConverter
{
    /// <summary>
    /// Quantizes one thumbstick deflection into a C64 joystick direction mask.
    /// </summary>
    /// <param name="x">Horizontal axis, -1.0 (left) to 1.0 (right).</param>
    /// <param name="y">Vertical axis, -1.0 (down) to 1.0 (up).</param>
    /// <param name="priorMask">
    /// The direction mask this stick produced on the previous frame. Used purely as
    /// the hysteresis latch: a direction whose component sits inside the
    /// (Release, Activate) band holds its prior on/off state. Only the four
    /// direction bits (0x01/0x02/0x04/0x08) are read; other bits are ignored.
    /// </param>
    /// <param name="config">The tuning thresholds (see <see cref="XboxInputConfig"/>).</param>
    /// <returns>
    /// A mask over <see cref="JoystickPortState.Up"/>/<see cref="JoystickPortState.Down"/>/
    /// <see cref="JoystickPortState.Left"/>/<see cref="JoystickPortState.Right"/>;
    /// 0 = centered. Up+Down and Left+Right are mutually exclusive by construction.
    /// </returns>
    /// <remarks>
    /// Algorithm (in order):
    /// <list type="number">
    /// <item>
    /// <b>Radial deadzone gate.</b> If <c>sqrt(x*x + y*y) &lt; config.StickDeadzone</c>
    /// the stick is treated as centered and 0 is returned, regardless of
    /// <paramref name="priorMask"/>.
    /// </item>
    /// <item>
    /// <b>Per-direction hysteresis.</b> For each direction the signed component
    /// toward it is computed: <c>up=+y</c>, <c>down=-y</c>, <c>right=+x</c>,
    /// <c>left=-x</c>. The bit is ON when the component is
    /// <c>&gt;= config.ActivateThreshold</c>, OFF when <c>&lt;= config.ReleaseThreshold</c>,
    /// and HOLDS its prior value (from <paramref name="priorMask"/>) inside the band.
    /// Because a large <c>+y</c> forces down's component <c>-y</c> below Release (and
    /// likewise for X), Up+Down and Left+Right can never both be set.
    /// </item>
    /// <item>
    /// <b>Diagonal gate.</b> The <i>dominant axis</i> is the one with the larger
    /// absolute deflection (<c>|x|</c> vs <c>|y|</c>); the other is the
    /// <i>secondary axis</i>. The secondary axis contributes its direction bit only
    /// when its own magnitude is <c>&gt;= config.DiagonalThreshold</c>; otherwise the
    /// secondary axis's bits are cleared. This is the exact rule: a mostly-cardinal
    /// push (secondary magnitude below <c>DiagonalThreshold</c>) stays cardinal
    /// instead of registering a weak spurious diagonal. When <c>|x| == |y|</c>
    /// neither axis is secondary, so a true diagonal keeps both bits.
    /// </item>
    /// </list>
    /// </remarks>
    public static byte ToDirectionMask(double x, double y, byte priorMask, in XboxInputConfig config)
    {
        // Step 1: radial deadzone gate. A released/barely-deflected stick centers.
        double magnitude = Math.Sqrt((x * x) + (y * y));
        if (magnitude < config.StickDeadzone)
        {
            return 0;
        }

        // Step 2: per-direction Activate/Release hysteresis against priorMask.
        // The signed component toward each direction is the axis value with the
        // direction's sign; only one of each opposing pair can be positive.
        byte mask = 0;
        mask |= ResolveBit(y, JoystickPortState.Up, priorMask, in config);     // up    = +y
        mask |= ResolveBit(-y, JoystickPortState.Down, priorMask, in config);  // down  = -y
        mask |= ResolveBit(x, JoystickPortState.Right, priorMask, in config);  // right = +x
        mask |= ResolveBit(-x, JoystickPortState.Left, priorMask, in config);  // left  = -x

        // Step 3: diagonal gate. Suppress the secondary (non-dominant) axis's bits
        // unless that axis's magnitude reaches DiagonalThreshold.
        double absX = Math.Abs(x);
        double absY = Math.Abs(y);
        if (absX > absY)
        {
            // Horizontal dominant; vertical is secondary.
            if (absY < config.DiagonalThreshold)
            {
                mask &= (byte)(JoystickPortState.Left | JoystickPortState.Right); // keep L/R only
            }
        }
        else if (absY > absX)
        {
            // Vertical dominant; horizontal is secondary.
            if (absX < config.DiagonalThreshold)
            {
                mask &= (byte)(JoystickPortState.Up | JoystickPortState.Down); // keep U/D only
            }
        }

        // |x| == |y|: a true diagonal - neither axis is secondary, keep both bits.
        return mask;
    }

    /// <summary>
    /// Resolves a single direction bit from its signed component using the
    /// Activate/Release hysteresis band. ON at/above Activate, OFF at/below
    /// Release, and holds the prior state (from <paramref name="priorMask"/>)
    /// inside the band. Pure; reads no shared state.
    /// </summary>
    private static byte ResolveBit(double component, byte bit, byte priorMask, in XboxInputConfig config)
    {
        if (component >= config.ActivateThreshold)
        {
            return bit; // above the upper edge -> ON
        }

        if (component <= config.ReleaseThreshold)
        {
            return 0; // at/below the lower edge -> OFF
        }

        return (byte)(priorMask & bit); // inside the band -> hold prior
    }
}
