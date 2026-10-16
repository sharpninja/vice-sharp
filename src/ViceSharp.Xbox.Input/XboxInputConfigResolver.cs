namespace ViceSharp.Xbox.Input;

/// <summary>
/// Resolves the persisted <see cref="XboxInputPrefs"/> into the ONE
/// <see cref="XboxInputConfig"/> the converter (<see cref="StickConverter"/>) and
/// mapper (<see cref="XboxJoystickMapper"/>) read (PLAN-XBOXUWP S12,
/// IMPL-XBOXUWP-012). Having a single resolver is the point: the deadzone and swap
/// flags flow from prefs into ONE config, so there are never two competing stores
/// feeding the converter/mapper.
/// </summary>
public static class XboxInputConfigResolver
{
    /// <summary>
    /// Produces the single resolved <see cref="XboxInputConfig"/>: the stick deadzone
    /// and swap flag come from <paramref name="prefs"/>; the quantization thresholds
    /// (diagonal / activate / release) come from the frozen
    /// <see cref="XboxInputConfig.Default"/> (the S8/S9 golden vectors depend on those
    /// exact literals, so prefs do not override them).
    /// </summary>
    /// <param name="prefs">The persisted input preferences.</param>
    /// <returns>The one config the converter and mapper consume.</returns>
    public static XboxInputConfig Resolve(in XboxInputPrefs prefs)
    {
        XboxInputConfig defaults = XboxInputConfig.Default;
        return new XboxInputConfig(
            StickDeadzone: prefs.LeftStickDeadzone,
            DiagonalThreshold: defaults.DiagonalThreshold,
            ActivateThreshold: defaults.ActivateThreshold,
            ReleaseThreshold: defaults.ReleaseThreshold,
            SwapPorts: prefs.SwapPorts);
    }
}
