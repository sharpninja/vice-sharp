namespace ViceSharp.TestHarness.Xbox;

using System;
using System.IO;
using ViceSharp.Host.Settings;
using Xunit;

/// <summary>
/// PLAN-XBOXUWP slice S29 (IMPL-XBOXUWP-029). FR-CFG / TR-CFG (Xbox-UI preference
/// persistence), TEST-CFG. The <see cref="XboxUiStateStore"/> persists the
/// Xbox-UI-only preferences to the <c>[ViceSharpXbox]</c> section of
/// <c>vice-sharp.ini</c> (never the canonical VICE <c>vice.ini</c>), reads them back
/// identically, and returns <see cref="XboxUiPrefs.Default"/> when a value is absent.
/// All cases run off-console (Tier H) against a unique temp config directory that
/// stands in for the UWP LocalFolder, so nothing touches the operator's shared VICE
/// config.
/// </summary>
[Trait("Category", "Xbox")]
public sealed class XboxUiStateStoreTests
{
    private static string NewTempConfigDir() =>
        Path.Combine(Path.GetTempPath(), "vicesharp-xboxui-" + Guid.NewGuid().ToString("N"));

    /// <summary>
    /// FR-CFG / TR-CFG (IMPL-XBOXUWP-029), TEST-CFG round-trip guard.
    /// Use case: an operator's edited Xbox-UI preferences must persist and reload
    /// exactly, every field independently.
    /// Acceptance: <see cref="XboxUiStateStore.Save"/> of a prefs record whose every
    /// field differs from its default, then <see cref="XboxUiStateStore.Load"/> from a
    /// fresh store, is value-equal to the saved record (so a dropped field is caught),
    /// and the values live under the <c>[ViceSharpXbox]</c> section of vice-sharp.ini.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void Save_ThenLoad_RoundTripsAllEightFields_Exactly()
    {
        var dir = NewTempConfigDir();
        Directory.CreateDirectory(dir);
        try
        {
            // Every field differs from XboxUiPrefs.Default so a dropped field is caught.
            var prefs = new XboxUiPrefs(
                SaveSettingsOnExit: false,        // default true
                SaveTransientOnExit: true,        // default false
                MasterVolumePercent: 73,          // default 100
                Muted: true,                      // default false
                TvSafeAreaInsetPercent: 7.5,      // default 5.0
                LeftStickDeadzonePercent: 22.5,   // default 30.0
                RightStickDeadzonePercent: 41.25, // default 30.0
                RomProvisionAcknowledged: true);  // default false

            Assert.NotEqual(XboxUiPrefs.Default, prefs);

            new XboxUiStateStore(dir).Save(prefs);
            var loaded = new XboxUiStateStore(dir).Load();

            // Field-by-field so a single dropped field is pinpointed.
            Assert.Equal(prefs.SaveSettingsOnExit, loaded.SaveSettingsOnExit);
            Assert.Equal(prefs.SaveTransientOnExit, loaded.SaveTransientOnExit);
            Assert.Equal(prefs.MasterVolumePercent, loaded.MasterVolumePercent);
            Assert.Equal(prefs.Muted, loaded.Muted);
            Assert.Equal(prefs.TvSafeAreaInsetPercent, loaded.TvSafeAreaInsetPercent, 10);
            Assert.Equal(prefs.LeftStickDeadzonePercent, loaded.LeftStickDeadzonePercent, 10);
            Assert.Equal(prefs.RightStickDeadzonePercent, loaded.RightStickDeadzonePercent, 10);
            Assert.Equal(prefs.RomProvisionAcknowledged, loaded.RomProvisionAcknowledged);

            // Whole-record equality: the chosen values round-trip exactly.
            Assert.Equal(prefs, loaded);

            // Values live under [ViceSharpXbox] in vice-sharp.ini.
            var viceSharpIni = File.ReadAllText(Path.Combine(dir, "vice-sharp.ini"));
            Assert.Contains("[ViceSharpXbox]", viceSharpIni);
            Assert.Contains("MasterVolumePercent=73", viceSharpIni);
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    /// <summary>
    /// FR-CFG / TR-CFG (IMPL-XBOXUWP-029), TEST-CFG isolation guard.
    /// Use case: persisting Xbox-UI preferences must never disturb the operator's
    /// shared canonical VICE <c>vice.ini</c>.
    /// Acceptance: with a pre-existing vice.ini holding known VICE resources, a
    /// <see cref="XboxUiStateStore.Save"/> leaves vice.ini byte-for-byte unchanged (no
    /// Xbox pref key leaks in, no VICE resource is dropped), and the Xbox prefs land in
    /// vice-sharp.ini under the <c>[ViceSharpXbox]</c> section.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void Save_WritesOnlyViceSharpIni_LeavingViceIniByteUnchanged()
    {
        var dir = NewTempConfigDir();
        Directory.CreateDirectory(dir);
        try
        {
            // Known VICE content in the settings-writer's canonical serialized form.
            var viceIniPath = Path.Combine(dir, "vice.ini");
            const string seededViceIni = "[C64SC]\nVICIIModel=3\nDrive8Type=1541\n\n";
            File.WriteAllText(viceIniPath, seededViceIni);
            var before = File.ReadAllBytes(viceIniPath);

            new XboxUiStateStore(dir).Save(XboxUiPrefs.Default with { MasterVolumePercent = 55 });

            // vice.ini is byte-unchanged: VICE resources preserved, no Xbox pref leaked.
            var after = File.ReadAllBytes(viceIniPath);
            Assert.Equal(before, after);

            var viceIniText = File.ReadAllText(viceIniPath);
            Assert.DoesNotContain("ViceSharpXbox", viceIniText);
            Assert.DoesNotContain("MasterVolumePercent", viceIniText);

            // The prefs landed in vice-sharp.ini, under [ViceSharpXbox].
            var viceSharpIniPath = Path.Combine(dir, "vice-sharp.ini");
            Assert.True(File.Exists(viceSharpIniPath), "vice-sharp.ini should have been written.");
            var viceSharpText = File.ReadAllText(viceSharpIniPath);
            Assert.Contains("[ViceSharpXbox]", viceSharpText);
            Assert.Contains("MasterVolumePercent=55", viceSharpText);
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    /// <summary>
    /// FR-CFG / TR-CFG (IMPL-XBOXUWP-029), TEST-CFG default guard.
    /// Use case: on first run, before anything is saved, the Xbox UI must see its locked
    /// defaults.
    /// Acceptance: <see cref="XboxUiStateStore.Load"/> from an absent config directory
    /// returns <see cref="XboxUiPrefs.Default"/>, each field defaulted.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void Load_FromAbsentConfigDir_ReturnsDefaults()
    {
        // A directory that does not exist: the settings providers are optional, so a
        // missing file yields empty configuration and every field defaults.
        var dir = NewTempConfigDir();
        Assert.False(Directory.Exists(dir));

        var loaded = new XboxUiStateStore(dir).Load();

        Assert.Equal(XboxUiPrefs.Default, loaded);
        Assert.True(loaded.SaveSettingsOnExit);
        Assert.False(loaded.SaveTransientOnExit);
        Assert.Equal(100, loaded.MasterVolumePercent);
        Assert.False(loaded.Muted);
        Assert.Equal(5.0, loaded.TvSafeAreaInsetPercent, 10);
        Assert.Equal(30.0, loaded.LeftStickDeadzonePercent, 10);
        Assert.Equal(30.0, loaded.RightStickDeadzonePercent, 10);
        Assert.False(loaded.RomProvisionAcknowledged);
    }

    /// <summary>
    /// FR-CFG / TR-CFG (IMPL-XBOXUWP-029), TEST-CFG per-field fallback guard.
    /// Use case: a partially-written config (a newer build added a key, or the file was
    /// hand-edited) must not corrupt the other preferences.
    /// Acceptance: with only one key present in <c>[ViceSharpXbox]</c>,
    /// <see cref="XboxUiStateStore.Load"/> returns that key's value while every other
    /// field falls back to its <see cref="XboxUiPrefs.Default"/>.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void Load_WithOnlyOneKeyPresent_DefaultsEveryOtherField()
    {
        var dir = NewTempConfigDir();
        Directory.CreateDirectory(dir);
        try
        {
            File.WriteAllText(
                Path.Combine(dir, "vice-sharp.ini"),
                "[ViceSharpXbox]\nMasterVolumePercent=42\n\n");

            var loaded = new XboxUiStateStore(dir).Load();
            var d = XboxUiPrefs.Default;

            Assert.Equal(42, loaded.MasterVolumePercent); // the one present key
            Assert.Equal(d.SaveSettingsOnExit, loaded.SaveSettingsOnExit);
            Assert.Equal(d.SaveTransientOnExit, loaded.SaveTransientOnExit);
            Assert.Equal(d.Muted, loaded.Muted);
            Assert.Equal(d.TvSafeAreaInsetPercent, loaded.TvSafeAreaInsetPercent, 10);
            Assert.Equal(d.LeftStickDeadzonePercent, loaded.LeftStickDeadzonePercent, 10);
            Assert.Equal(d.RightStickDeadzonePercent, loaded.RightStickDeadzonePercent, 10);
            Assert.Equal(d.RomProvisionAcknowledged, loaded.RomProvisionAcknowledged);
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }
}
