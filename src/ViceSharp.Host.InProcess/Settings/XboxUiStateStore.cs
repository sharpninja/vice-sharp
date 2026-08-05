using System.Globalization;
using ViceSharp.Core.Configuration;

namespace ViceSharp.Host.Settings;

/// <summary>
/// Persists the <see cref="XboxUiPrefs"/> for the Xbox / 10-foot head to the
/// <c>[ViceSharpXbox]</c> section of <c>vice-sharp.ini</c> and reads them back. It
/// routes every value through the ViceSharp-section API of
/// <see cref="ViceSettings"/> (<see cref="ViceSettings.SetViceSharp"/> +
/// <see cref="ViceSettings.Get"/>), so the canonical VICE <c>vice.ini</c> is never
/// the target of an Xbox-UI write: <see cref="ViceSettings.Save"/> only re-serializes
/// vice.ini's own resources (a lossless read-modify-write), leaving them intact.
///
/// The store is stateless apart from the config directory it was constructed with;
/// each <see cref="Load"/> / <see cref="Save"/> opens a fresh
/// <see cref="ViceSettings"/> rooted at that directory (via
/// <see cref="ViceSettings.OpenAt"/>), so an off-console test can inject a temp
/// directory and a caller always sees current on-disk state. Every value is
/// formatted and parsed with the invariant culture; a missing key falls back to the
/// matching <see cref="XboxUiPrefs.Default"/> field so a partially-written file never
/// corrupts the other preferences.
/// </summary>
public sealed class XboxUiStateStore
{
    /// <summary>The <c>vice-sharp.ini</c> section that holds the Xbox-UI preferences.</summary>
    public const string SectionName = "ViceSharpXbox";

    /// <summary>Key for <see cref="XboxUiPrefs.SaveSettingsOnExit"/>.</summary>
    public const string KeySaveSettingsOnExit = "SaveSettingsOnExit";

    /// <summary>Key for <see cref="XboxUiPrefs.SaveTransientOnExit"/>.</summary>
    public const string KeySaveTransientOnExit = "SaveTransientOnExit";

    /// <summary>Key for <see cref="XboxUiPrefs.MasterVolumePercent"/>.</summary>
    public const string KeyMasterVolumePercent = "MasterVolumePercent";

    /// <summary>Key for <see cref="XboxUiPrefs.Muted"/>.</summary>
    public const string KeyMuted = "Muted";

    /// <summary>Key for <see cref="XboxUiPrefs.TvSafeAreaInsetPercent"/>.</summary>
    public const string KeyTvSafeAreaInsetPercent = "TvSafeAreaInsetPercent";

    /// <summary>Key for <see cref="XboxUiPrefs.LeftStickDeadzonePercent"/>.</summary>
    public const string KeyLeftStickDeadzonePercent = "LeftStickDeadzonePercent";

    /// <summary>Key for <see cref="XboxUiPrefs.RightStickDeadzonePercent"/>.</summary>
    public const string KeyRightStickDeadzonePercent = "RightStickDeadzonePercent";

    /// <summary>Key for <see cref="XboxUiPrefs.RomProvisionAcknowledged"/>.</summary>
    public const string KeyRomProvisionAcknowledged = "RomProvisionAcknowledged";

    private readonly string _configDirectory;

    /// <summary>
    /// Create a store rooted at <paramref name="configDirectory"/> (the folder that
    /// holds <c>vice.ini</c> and <c>vice-sharp.ini</c>). On the Xbox head this is the
    /// AppContainer-writable LocalFolder root; off-console tests pass a temp directory.
    /// </summary>
    public XboxUiStateStore(string configDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configDirectory);
        _configDirectory = configDirectory;
    }

    /// <summary>
    /// Read the persisted <see cref="XboxUiPrefs"/> from <c>[ViceSharpXbox]</c>. Any
    /// key absent (or unparseable) falls back to the matching
    /// <see cref="XboxUiPrefs.Default"/> field, so an absent config directory returns
    /// <see cref="XboxUiPrefs.Default"/>.
    /// </summary>
    public XboxUiPrefs Load()
    {
        var settings = ViceSettings.OpenAt(_configDirectory);
        var d = XboxUiPrefs.Default;

        return new XboxUiPrefs(
            ParseBool(settings.Get(SectionName, KeySaveSettingsOnExit), d.SaveSettingsOnExit),
            ParseBool(settings.Get(SectionName, KeySaveTransientOnExit), d.SaveTransientOnExit),
            ParseInt(settings.Get(SectionName, KeyMasterVolumePercent), d.MasterVolumePercent),
            ParseBool(settings.Get(SectionName, KeyMuted), d.Muted),
            ParseDouble(settings.Get(SectionName, KeyTvSafeAreaInsetPercent), d.TvSafeAreaInsetPercent),
            ParseDouble(settings.Get(SectionName, KeyLeftStickDeadzonePercent), d.LeftStickDeadzonePercent),
            ParseDouble(settings.Get(SectionName, KeyRightStickDeadzonePercent), d.RightStickDeadzonePercent),
            ParseBool(settings.Get(SectionName, KeyRomProvisionAcknowledged), d.RomProvisionAcknowledged));
    }

    /// <summary>
    /// Write <paramref name="prefs"/> to <c>[ViceSharpXbox]</c> in <c>vice-sharp.ini</c>
    /// (one bare, invariant-formatted value per key) and persist. The canonical VICE
    /// <c>vice.ini</c> receives no Xbox-UI key.
    /// </summary>
    public void Save(XboxUiPrefs prefs)
    {
        var settings = ViceSettings.OpenAt(_configDirectory);

        settings.SetViceSharp(SectionName, KeySaveSettingsOnExit, FormatBool(prefs.SaveSettingsOnExit), quote: false);
        settings.SetViceSharp(SectionName, KeySaveTransientOnExit, FormatBool(prefs.SaveTransientOnExit), quote: false);
        settings.SetViceSharp(SectionName, KeyMasterVolumePercent, FormatInt(prefs.MasterVolumePercent), quote: false);
        settings.SetViceSharp(SectionName, KeyMuted, FormatBool(prefs.Muted), quote: false);
        settings.SetViceSharp(SectionName, KeyTvSafeAreaInsetPercent, FormatDouble(prefs.TvSafeAreaInsetPercent), quote: false);
        settings.SetViceSharp(SectionName, KeyLeftStickDeadzonePercent, FormatDouble(prefs.LeftStickDeadzonePercent), quote: false);
        settings.SetViceSharp(SectionName, KeyRightStickDeadzonePercent, FormatDouble(prefs.RightStickDeadzonePercent), quote: false);
        settings.SetViceSharp(SectionName, KeyRomProvisionAcknowledged, FormatBool(prefs.RomProvisionAcknowledged), quote: false);

        settings.Save();
    }

    private static string FormatBool(bool value) => value ? "true" : "false";

    private static string FormatInt(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static string FormatDouble(double value) => value.ToString(CultureInfo.InvariantCulture);

    private static bool ParseBool(string? text, bool fallback) =>
        bool.TryParse(text, out var value) ? value : fallback;

    private static int ParseInt(string? text, int fallback) =>
        int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : fallback;

    private static double ParseDouble(string? text, double fallback) =>
        double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : fallback;
}
