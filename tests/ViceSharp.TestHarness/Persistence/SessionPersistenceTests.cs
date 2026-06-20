namespace ViceSharp.TestHarness.Persistence;

using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using ViceSharp.Avalonia.Persistence;
using Xunit;

/// <summary>
/// FR/TR: FR-UIPERSIST-001 / TEST-UIPERSIST-001.
/// Use case: the two "save on exit" toggles persist UI settings and transient
/// session state (attached disks + keyboard map) to vice-sharp.ini and restore
/// them on the next launch. The toggles gate BOTH save and load: a disabled
/// toggle yields a null section on load even if stale keys remain in the file.
/// </summary>
public sealed class SessionPersistenceTests : IDisposable
{
    private readonly string _dir;

    public SessionPersistenceTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "vicesharp-persist-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch { /* best effort */ }
    }

    private static PersistedSettings SampleSettings() => new(
        LimiterRatePercent: 250,
        LimiterEnabled: false,
        MachineProfileId: "ntsc",
        Renderer: "Software",
        DisplayScale: "3x",
        CropMode: "Borderless",
        AspectMode: "Force 4:3",
        Palette: "Pepto",
        AudioMode: "Muted",
        InputMode: "Keyboard only",
        PrimaryJoystickPort: "Joystick 1",
        SwapJoystickPorts: true,
        ResourceMode: "Use configured paths",
        DockSide: 1,
        PacingStrategy: "VICE",
        MasterVolumePercent: 65,
        Muted: true);

    private static PersistedTransient SampleTransient() => new(
        new List<PersistedAttachment>
        {
            new("Drive8", @"C:\games\boulderdash.d64", IsReadOnly: false, TrueDrive: true),
            new("Drive9", @"C:\games\utils.d64", IsReadOnly: true, TrueDrive: false),
        },
        KeyboardMapId: "c64:gtk3_sym",
        KeyboardMapSourcePath: null);

    [Fact]
    public void Save_ThenLoad_RoundTripsBothToggles()
    {
        new SessionPersistence(_dir).Save(new PersistedState(true, true, SampleSettings(), SampleTransient()));

        var loaded = new SessionPersistence(_dir).Load();

        loaded.SaveSettingsOnExit.Should().BeTrue();
        loaded.SaveTransientValuesOnExit.Should().BeTrue();
    }

    [Fact]
    public void Save_ThenLoad_RoundTripsSettings()
    {
        new SessionPersistence(_dir).Save(new PersistedState(true, false, SampleSettings(), null));

        var loaded = new SessionPersistence(_dir).Load();

        loaded.Settings.Should().Be(SampleSettings());
    }

    [Fact]
    public void PersistedSettings_DefaultsMasterVolumeTo100_AndUnmuted()
    {
        var defaults = new PersistedSettings(
            LimiterRatePercent: 100,
            LimiterEnabled: true,
            MachineProfileId: "c64",
            Renderer: "Host direct",
            DisplayScale: "2x",
            CropMode: "Visible area",
            AspectMode: "VICE pixel aspect",
            Palette: "VICE default",
            AudioMode: "Enabled",
            InputMode: "Keyboard + joystick",
            PrimaryJoystickPort: "Joystick 2",
            SwapJoystickPorts: false,
            ResourceMode: "Auto detect",
            DockSide: 0);

        defaults.MasterVolumePercent.Should().Be(100);
        defaults.Muted.Should().BeFalse();
    }

    [Fact]
    public void Save_ThenLoad_RoundTripsTransientAttachmentsAndKeyboard()
    {
        new SessionPersistence(_dir).Save(new PersistedState(false, true, null, SampleTransient()));

        var loaded = new SessionPersistence(_dir).Load();

        loaded.Transient.Should().NotBeNull();
        loaded.Transient!.KeyboardMapId.Should().Be("c64:gtk3_sym");
        loaded.Transient.Attachments.Should().HaveCount(2);
        loaded.Transient.Attachments[0].Should().Be(
            new PersistedAttachment("Drive8", @"C:\games\boulderdash.d64", false, true));
        loaded.Transient.Attachments[1].Should().Be(
            new PersistedAttachment("Drive9", @"C:\games\utils.d64", true, false));
    }

    [Fact]
    public void Load_WithSettingsToggleOff_ReturnsNullSettings_EvenWithStaleKeys()
    {
        // First save settings (writes the settings keys).
        new SessionPersistence(_dir).Save(new PersistedState(true, false, SampleSettings(), null));
        // Then save again with the toggle OFF and no settings section.
        new SessionPersistence(_dir).Save(new PersistedState(false, false, null, null));

        var loaded = new SessionPersistence(_dir).Load();

        loaded.SaveSettingsOnExit.Should().BeFalse();
        loaded.Settings.Should().BeNull("a disabled toggle must ignore stale settings keys");
    }

    [Fact]
    public void Load_FreshDirectory_ReturnsDisabledTogglesAndNullSections()
    {
        var loaded = new SessionPersistence(_dir).Load();

        loaded.SaveSettingsOnExit.Should().BeFalse();
        loaded.SaveTransientValuesOnExit.Should().BeFalse();
        loaded.Settings.Should().BeNull();
        loaded.Transient.Should().BeNull();
    }

    [Fact]
    public void Save_WritesUiOwnedFile_WithoutTouchingCanonicalViceFiles()
    {
        new SessionPersistence(_dir).Save(new PersistedState(true, true, SampleSettings(), SampleTransient()));

        // UI state lives in its own vice-sharp-ui.ini; the canonical VICE
        // resource files (vice.ini / vice-sharp.ini) are never written by the UI.
        File.Exists(Path.Combine(_dir, "vice-sharp-ui.ini")).Should().BeTrue();
        File.Exists(Path.Combine(_dir, "vice.ini")).Should().BeFalse();
        File.Exists(Path.Combine(_dir, "vice-sharp.ini")).Should().BeFalse();
    }
}
