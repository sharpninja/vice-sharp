namespace ViceSharp.Xbox.Input;

using System.Collections.Generic;

/// <summary>
/// The pure, allocation-friendly system-button evaluator (S10): turns a pair of
/// held-frame gamepad snapshots into discrete <see cref="AppCommand"/>s under a
/// <see cref="BindingProfile"/>. This is the edge-activation half of the input
/// context machine; the context transitions and host dispatch land in S11.
/// </summary>
/// <remarks>
/// <para>
/// FR-SYSBTN-001 / FR-SYSBTN-005 / FR-SYSBTN-006, TR-SYSBTN-001 / TR-SYSBTN-002
/// (PLAN-XBOXUWP S10, IMPL-XBOXUWP-010). The evaluator is deterministic and holds
/// <b>no</b> static mutable state: identical
/// <c>(prior, current, profile, config, latch)</c> yields identical output on every
/// call. The only cross-frame state is the trigger-hold hysteresis phase, threaded
/// through the <see cref="SystemButtonLatch"/> parameter and return value.
/// </para>
/// <para>
/// <b>Output shape.</b> Commands are appended to a caller-provided
/// <see cref="List{T}"/> so a per-frame caller can reuse one buffer (clear then pass)
/// and avoid steady-state allocation; the method does not clear the list. The next
/// latch is returned by value (a small struct, no allocation).
/// </para>
/// <para>
/// <b>Edge rules.</b> A digital button is active when its flag is set
/// (<c>(Buttons &amp; flag) != 0</c>); its down edge is prior-not-set &amp;&amp;
/// current-set. <see cref="BindingActivation.Press"/> and
/// <see cref="BindingActivation.Toggle"/> both emit the binding's command exactly
/// once on that down edge (they differ only downstream). An analog trigger uses
/// hysteresis (activate at &gt;= <see cref="TriggerActivate"/> 0.6, release at
/// &lt;= <see cref="TriggerRelease"/> 0.4); a value strictly inside the band holds
/// the latched phase, so no chatter is emitted.
/// </para>
/// <para>
/// <b>Hold on/off mapping.</b> A <see cref="BindingActivation.Hold"/> binding carries
/// a single command, taken as the ON (activate-edge) intent. On the activate edge the
/// evaluator emits that command; on the release edge it emits the paired OFF command
/// from a small fixed convention map (<see cref="PairedReleaseCommand"/>:
/// <see cref="AppCommand.WarpHoldOn"/> pairs with <see cref="AppCommand.WarpHoldOff"/>).
/// A hold whose command has no paired release emits only on the activate edge.
/// </para>
/// </remarks>
public static class XboxSystemButtons
{
    /// <summary>Trigger travel at or above which a hold turns ON (upper hysteresis edge).</summary>
    public const double TriggerActivate = 0.60;

    /// <summary>Trigger travel at or below which a hold turns OFF (lower hysteresis edge).</summary>
    public const double TriggerRelease = 0.40;

    /// <summary>
    /// Evaluates one frame: appends the emitted commands to <paramref name="output"/>
    /// and returns the next hysteresis latch.
    /// </summary>
    /// <param name="prior">The previous frame's gamepad snapshot.</param>
    /// <param name="current">This frame's gamepad snapshot.</param>
    /// <param name="profile">The active binding profile (its Gameplay rows are applied).</param>
    /// <param name="config">
    /// The input tuning/config. Unused by the digital and trigger edge logic here
    /// (trigger thresholds are dedicated constants); threaded for a consistent call
    /// shape and consumed by <see cref="ApplySwap"/> downstream.
    /// </param>
    /// <param name="latch">The trigger-hold hysteresis phase from the previous frame.</param>
    /// <param name="output">
    /// The caller-owned buffer that emitted commands are appended to. Not cleared by
    /// this method.
    /// </param>
    /// <returns>The latch to thread into the next frame.</returns>
    public static SystemButtonLatch Evaluate(
        in GamepadSnapshot prior,
        in GamepadSnapshot current,
        BindingProfile profile,
        in XboxInputConfig config,
        in SystemButtonLatch latch,
        List<AppCommand> output)
    {
        _ = config; // reserved for parity of call shape; trigger thresholds are constants.

        SystemButtonLatch next = latch;
        IReadOnlyList<ButtonBinding> gameplay = profile.Gameplay;

        for (int i = 0; i < gameplay.Count; i++)
        {
            ButtonBinding binding = gameplay[i];

            if (TryGetButtonFlag(binding.Input, out GamepadButtonFlags flag))
            {
                EvaluateDigital(binding, flag, in prior, in current, output);
            }
            else
            {
                next = EvaluateTrigger(binding, in current, next, output);
            }
        }

        return next;
    }

    /// <summary>
    /// Applies a <see cref="AppCommand.SwapJoystickPorts"/> command to the config by
    /// flipping <see cref="XboxInputConfig.SwapPorts"/>; returns the config unchanged
    /// for any other command. Pure.
    /// </summary>
    /// <param name="command">The command to apply.</param>
    /// <param name="config">The current input config.</param>
    /// <returns>
    /// The config with <see cref="XboxInputConfig.SwapPorts"/> toggled when
    /// <paramref name="command"/> is <see cref="AppCommand.SwapJoystickPorts"/>,
    /// otherwise the config unchanged.
    /// </returns>
    public static XboxInputConfig ApplySwap(AppCommand command, in XboxInputConfig config) =>
        command == AppCommand.SwapJoystickPorts
            ? config with { SwapPorts = !config.SwapPorts }
            : config;

    /// <summary>Emits for a digital-button binding using prior/current button flags.</summary>
    private static void EvaluateDigital(
        ButtonBinding binding,
        GamepadButtonFlags flag,
        in GamepadSnapshot prior,
        in GamepadSnapshot current,
        List<AppCommand> output)
    {
        bool priorSet = (prior.Buttons & flag) != 0;
        bool currentSet = (current.Buttons & flag) != 0;
        bool downEdge = !priorSet && currentSet;
        bool upEdge = priorSet && !currentSet;

        switch (binding.Activation)
        {
            case BindingActivation.Press:
            case BindingActivation.Toggle:
                if (downEdge)
                {
                    output.Add(binding.Command);
                }

                break;

            case BindingActivation.Hold:
                // A digital hold: command on the down edge, paired release on the up edge.
                if (downEdge)
                {
                    output.Add(binding.Command);
                }
                else if (upEdge)
                {
                    AppCommand release = PairedReleaseCommand(binding.Command);
                    if (release != AppCommand.None)
                    {
                        output.Add(release);
                    }
                }

                break;
        }
    }

    /// <summary>
    /// Emits for an analog-trigger binding using hysteresis over the current trigger
    /// value plus the latched phase; returns the updated latch.
    /// </summary>
    private static SystemButtonLatch EvaluateTrigger(
        ButtonBinding binding,
        in GamepadSnapshot current,
        SystemButtonLatch latch,
        List<AppCommand> output)
    {
        double value = AnalogValue(binding.Input, in current);
        bool heldPrior = TriggerHeld(binding.Input, in latch);

        bool heldNow = value >= TriggerActivate
            ? true
            : value <= TriggerRelease
                ? false
                : heldPrior; // inside the band -> hold the latched phase.

        bool activateEdge = !heldPrior && heldNow;
        bool releaseEdge = heldPrior && !heldNow;

        switch (binding.Activation)
        {
            case BindingActivation.Hold:
                if (activateEdge)
                {
                    output.Add(binding.Command);
                }
                else if (releaseEdge)
                {
                    AppCommand release = PairedReleaseCommand(binding.Command);
                    if (release != AppCommand.None)
                    {
                        output.Add(release);
                    }
                }

                break;

            case BindingActivation.Press:
            case BindingActivation.Toggle:
                // Treat the activate edge as the trigger's "down edge".
                if (activateEdge)
                {
                    output.Add(binding.Command);
                }

                break;
        }

        return WithTriggerHeld(binding.Input, latch, heldNow);
    }

    /// <summary>
    /// The paired OFF command for a <see cref="BindingActivation.Hold"/> ON command.
    /// The convention map is intentionally tiny; a command with no pairing returns
    /// <see cref="AppCommand.None"/> (no release is emitted).
    /// </summary>
    private static AppCommand PairedReleaseCommand(AppCommand holdCommand) => holdCommand switch
    {
        AppCommand.WarpHoldOn => AppCommand.WarpHoldOff,
        _ => AppCommand.None,
    };

    /// <summary>
    /// Maps a bindable input to its digital button flag. Returns false for the analog
    /// triggers (<see cref="BindableInput.LeftTrigger"/> /
    /// <see cref="BindableInput.RightTrigger"/>), which are not digital.
    /// </summary>
    private static bool TryGetButtonFlag(BindableInput input, out GamepadButtonFlags flag)
    {
        switch (input)
        {
            case BindableInput.Menu: flag = GamepadButtonFlags.Menu; return true;
            case BindableInput.View: flag = GamepadButtonFlags.View; return true;
            case BindableInput.X: flag = GamepadButtonFlags.X; return true;
            case BindableInput.Y: flag = GamepadButtonFlags.Y; return true;
            case BindableInput.LeftShoulder: flag = GamepadButtonFlags.LeftShoulder; return true;
            case BindableInput.RightShoulder: flag = GamepadButtonFlags.RightShoulder; return true;
            case BindableInput.LeftThumbstick: flag = GamepadButtonFlags.LeftThumbstick; return true;
            case BindableInput.RightThumbstick: flag = GamepadButtonFlags.RightThumbstick; return true;
            default: flag = GamepadButtonFlags.None; return false;
        }
    }

    /// <summary>Reads the trigger travel for an analog input; 0 for a non-trigger.</summary>
    private static double AnalogValue(BindableInput input, in GamepadSnapshot snapshot) => input switch
    {
        BindableInput.LeftTrigger => snapshot.LeftTrigger,
        BindableInput.RightTrigger => snapshot.RightTrigger,
        _ => 0.0,
    };

    /// <summary>Reads the latched hold phase for a trigger input; false for a non-trigger.</summary>
    private static bool TriggerHeld(BindableInput input, in SystemButtonLatch latch) => input switch
    {
        BindableInput.LeftTrigger => latch.LeftTriggerHeld,
        BindableInput.RightTrigger => latch.RightTriggerHeld,
        _ => false,
    };

    /// <summary>Returns the latch with a trigger input's hold phase set; unchanged for a non-trigger.</summary>
    private static SystemButtonLatch WithTriggerHeld(BindableInput input, SystemButtonLatch latch, bool held) => input switch
    {
        BindableInput.LeftTrigger => latch with { LeftTriggerHeld = held },
        BindableInput.RightTrigger => latch with { RightTriggerHeld = held },
        _ => latch,
    };
}
