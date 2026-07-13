namespace ViceSharp.Host.Settings;

/// <summary>
/// The Xbox-UI-only preferences that the 10-foot head persists to the
/// <c>[ViceSharpXbox]</c> section of <c>vice-sharp.ini</c> (never the canonical VICE
/// <c>vice.ini</c>). These are presentation/host concerns that have no VICE resource
/// equivalent, so they live in ViceSharp's companion file and are round-tripped by
/// <see cref="XboxUiStateStore"/>.
/// </summary>
/// <param name="SaveSettingsOnExit">
/// Persist changed settings when the app exits. Default <c>true</c>.
/// </param>
/// <param name="SaveTransientOnExit">
/// Persist transient/session-only state (e.g. last-inserted media) on exit. Default
/// <c>false</c>.
/// </param>
/// <param name="MasterVolumePercent">
/// Master output volume, 0-100. Default <c>100</c>.
/// </param>
/// <param name="Muted">Whether audio output is muted. Default <c>false</c>.</param>
/// <param name="TvSafeAreaInsetPercent">
/// TV-safe-area inset as a percentage of each edge, for 10-foot overscan margins.
/// Default <c>5.0</c>.
/// </param>
/// <param name="LeftStickDeadzonePercent">
/// Left analog stick radial deadzone, as a percentage. Default <c>30.0</c>.
/// </param>
/// <param name="RightStickDeadzonePercent">
/// Right analog stick radial deadzone, as a percentage. Default <c>30.0</c>.
/// </param>
/// <param name="RomProvisionAcknowledged">
/// Whether the operator has acknowledged the ROM-provisioning notice (so the
/// first-run prompt is not shown again). Default <c>false</c>.
/// </param>
public readonly record struct XboxUiPrefs(
    bool SaveSettingsOnExit,
    bool SaveTransientOnExit,
    int MasterVolumePercent,
    bool Muted,
    double TvSafeAreaInsetPercent,
    double LeftStickDeadzonePercent,
    double RightStickDeadzonePercent,
    bool RomProvisionAcknowledged)
{
    /// <summary>
    /// The locked defaults returned when no preference has been saved yet:
    /// SaveSettingsOnExit = true, SaveTransientOnExit = false, MasterVolumePercent = 100,
    /// Muted = false, TvSafeAreaInsetPercent = 5.0, LeftStickDeadzonePercent = 30.0,
    /// RightStickDeadzonePercent = 30.0, RomProvisionAcknowledged = false.
    /// </summary>
    public static XboxUiPrefs Default => new(
        SaveSettingsOnExit: true,
        SaveTransientOnExit: false,
        MasterVolumePercent: 100,
        Muted: false,
        TvSafeAreaInsetPercent: 5.0,
        LeftStickDeadzonePercent: 30.0,
        RightStickDeadzonePercent: 30.0,
        RomProvisionAcknowledged: false);
}
