namespace ViceSharp.Xbox.Input;

/// <summary>
/// The tuning parameters the mapper uses to quantize analog gamepad input into
/// C64 joystick directions. Immutable and allocation-free.
/// </summary>
/// <remarks>
/// The <see cref="Default"/> thresholds are exact literals that are frozen: the
/// S8/S9 golden quantization vectors are derived from them, so changing a value
/// changes those vectors. Treat the defaults as a stable contract.
/// </remarks>
/// <param name="StickDeadzone">
/// Radial deadzone magnitude (0..1). Stick deflections whose magnitude is at or
/// below this are treated as centered. Default 0.30.
/// </param>
/// <param name="DiagonalThreshold">
/// Per-axis magnitude (0..1) at or above which a diagonal component engages when
/// resolving 8-way direction. Default 0.5.
/// </param>
/// <param name="ActivateThreshold">
/// Per-axis magnitude (0..1) at or above which a direction turns on
/// (the upper edge of the hysteresis band). Default 0.55.
/// </param>
/// <param name="ReleaseThreshold">
/// Per-axis magnitude (0..1) at or below which an already-on direction turns off
/// (the lower edge of the hysteresis band). Default 0.40.
/// </param>
/// <param name="SwapPorts">
/// When true, the two joystick ports are swapped (left-stick/right-stick route to
/// the opposite ports). Default false.
/// </param>
public readonly record struct XboxInputConfig(
    double StickDeadzone,
    double DiagonalThreshold,
    double ActivateThreshold,
    double ReleaseThreshold,
    bool SwapPorts)
{
    /// <summary>
    /// The frozen default tuning: radial deadzone 0.30, diagonal threshold 0.5,
    /// activate 0.55, release 0.40, ports not swapped. Exact literals; the S8/S9
    /// golden vectors depend on these values.
    /// </summary>
    public static XboxInputConfig Default => new(0.30, 0.5, 0.55, 0.40, false);
}
