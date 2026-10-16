namespace ViceSharp.Xbox.Input;

using System.Collections.Generic;

/// <summary>
/// The complete result of one <see cref="XboxInputContext.Tick(long, in GamepadSnapshot)"/>:
/// the context to run the NEXT frame in, the discrete commands emitted this frame,
/// and the two resolved C64 joystick ports.
/// </summary>
/// <remarks>
/// PLAN-XBOXUWP S11 (IMPL-XBOXUWP-011), FR-CTX-001..004. In every non-Gameplay
/// context, and on the one-shot Gameplay-&gt;non-Gameplay transition frame,
/// <see cref="Joy1"/> and <see cref="Joy2"/> are forced to
/// <see cref="JoystickPortState.Neutral"/> (FR-CTX-002 / FR-CTX-004).
/// <para>
/// <see cref="Commands"/> is a fresh, per-frame list (input runs at frame cadence,
/// not on the per-cycle emulation hot path). Note that record-struct equality on
/// this type compares <see cref="Commands"/> by reference, not by element; compare
/// the command sequences explicitly when asserting equality.
/// </para>
/// </remarks>
/// <param name="NextContext">The context the next frame will be evaluated in.</param>
/// <param name="Commands">The discrete <see cref="AppCommand"/>s emitted this frame, in order.</param>
/// <param name="Joy1">The resolved JOY1 port state (Neutral in any non-Gameplay context).</param>
/// <param name="Joy2">The resolved JOY2 port state (Neutral in any non-Gameplay context).</param>
public readonly record struct InputResolution(
    InputContext NextContext,
    IReadOnlyList<AppCommand> Commands,
    JoystickPortState Joy1,
    JoystickPortState Joy2);
