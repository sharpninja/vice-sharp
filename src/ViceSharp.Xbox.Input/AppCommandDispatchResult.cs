namespace ViceSharp.Xbox.Input;

/// <summary>
/// The local (non-host) side effects a single
/// <see cref="AppCommandDispatcher.DispatchAsync"/> asks its caller to apply.
/// </summary>
/// <remarks>
/// PLAN-XBOXUWP S13 (IMPL-XBOXUWP-013). Every emulator-state mutation a command
/// implies is marshaled by the dispatcher onto the session-locked host services;
/// what remains are effects the input loop owns because they are pure local
/// configuration, not host state. The joystick-port swap is exactly this: it flips
/// the caller's <see cref="XboxInputConfig.SwapPorts"/> and must NOT round-trip
/// through the settings service (FR-SYSBTN mapping), so the dispatcher reports the
/// requested flip here instead of calling any service.
/// </remarks>
/// <param name="ToggleSwapPorts">
/// True when the caller should toggle its <see cref="XboxInputConfig.SwapPorts"/>
/// flag for this frame (produced only by <see cref="AppCommand.SwapJoystickPorts"/>).
/// </param>
public readonly record struct AppCommandDispatchResult(bool ToggleSwapPorts)
{
    /// <summary>The no-local-effect result (nothing for the caller to apply).</summary>
    public static AppCommandDispatchResult None => default;

    /// <summary>The result that asks the caller to flip its joystick-port swap flag.</summary>
    public static AppCommandDispatchResult SwapPorts => new(true);
}
