namespace ViceSharp.TestHarness.Xbox;

using System;
using System.IO;
using ViceSharp.Protocol;
using ViceSharp.Xbox.ViewModels;
using Xunit;

/// <summary>
/// FEAT-XSETPERSIST-001 (PLAN-XBOXUWP, area XBOXSET): the UWP head persists settings changes
/// in REAL TIME and reuses them on app start.
///
/// FR: FR-XBOXSET (settings surface). Use case: operator request 2026-07-14: "UWP should
/// persist settings changes in real time and reuse on app start." Today every apply mutates
/// only the in-memory host session, so a PAL -> NTSC model switch is lost on relaunch and the
/// head always boots the default c64 profile. The head persists the host-CANONICAL
/// SessionSettingsDto returned by every successful UpdateSettings to
/// LocalState\settings.json (real time: at the moment of apply, not at exit), and
/// BuildHostAndSession reuses it: the persisted ProfileId boots the session directly and the
/// remaining settings are re-applied live.
/// Acceptance:
///   TEST-XSETPERSIST-001a: XboxSettingsStore round-trips a SessionSettingsDto through disk
///     byte-for-byte (record equality), including profile, limiter, display, input, audio,
///     and resources.
///   TEST-XSETPERSIST-001b: a missing file and a corrupt file both TryLoad=false (never
///     throw, never fabricate settings); TrySave to an invalid path returns false.
///   TEST-XSETPERSIST-001c: structural head wiring: the facade persists on successful
///     UpdateSettings, and the App loads the persisted settings, boots the persisted
///     profile, and re-applies the rest.
/// </summary>
public sealed class XboxSettingsPersistenceTests
{
    [Fact]
    [Trait("Category", "Xbox")]
    public void Store_RoundTrips_TheCanonicalSettings()
    {
        var path = Path.Combine(Path.GetTempPath(), $"vicesharp-settings-{Guid.NewGuid():N}.json");
        try
        {
            var settings = new SessionSettingsDto(
                "ntsc",
                new LimiterSettingsDto(RatePercent: 100, IsEnabled: true, PacingStrategy: "vice"),
                new DisplaySettingsDto(AspectMode: "vice-pixel-aspect", Scale: "3x"),
                new InputSettingsDto(KeyboardMapId: "c64:gtk3_pos", SwapJoystickPorts: true),
                new AudioSettingsDto(Mode: "enabled"),
                new ResourceSettingsDto(Mode: "auto-detect"));

            // TEST-XSETPERSIST-001a.
            Assert.True(XboxSettingsStore.TrySave(path, settings));
            Assert.True(XboxSettingsStore.TryLoad(path, out var loaded));
            Assert.Equal(settings, loaded);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    [Trait("Category", "Xbox")]
    public void Store_MissingCorruptAndInvalid_AreSafe()
    {
        // TEST-XSETPERSIST-001b: missing file.
        var missing = Path.Combine(Path.GetTempPath(), $"vicesharp-settings-{Guid.NewGuid():N}.json");
        Assert.False(XboxSettingsStore.TryLoad(missing, out var none));
        Assert.Null(none);

        // Corrupt file.
        var corrupt = Path.Combine(Path.GetTempPath(), $"vicesharp-settings-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(corrupt, "{ this is not json");
            Assert.False(XboxSettingsStore.TryLoad(corrupt, out _));
        }
        finally
        {
            File.Delete(corrupt);
        }

        // Unwritable path: TrySave reports false, never throws.
        var invalid = Path.Combine(Path.GetTempPath(), $"vicesharp-missing-dir-{Guid.NewGuid():N}", "nested", "x.json");
        Assert.False(XboxSettingsStore.TrySave(
            Path.Combine(invalid, "\0bad"), new SessionSettingsDto(
                "c64", new LimiterSettingsDto(), new DisplaySettingsDto(), new InputSettingsDto())));
    }

    [Fact]
    [Trait("Category", "Xbox")]
    public void Head_PersistsOnApply_AndReusesAtBoot()
    {
        // TEST-XSETPERSIST-001c: structural wiring of the #if HAS_UWP files the headless
        // fallback cannot execute.
        var facade = ReadLower("src", "ViceSharp.Xbox", "Platform", "InProcessSessionFacade.cs");
        Assert.Contains("settingspersistpath", facade);
        Assert.Contains("xboxsettingsstore.trysave", facade);

        var app = ReadLower("src", "ViceSharp.Xbox", "App.xaml.cs");
        Assert.Contains("xboxsettingsstore.tryload", app);
        Assert.Contains("consolesessionoptions", app);
        Assert.Contains("settingspersistpath", app);
    }

    private static string ReadLower(params string[] parts)
    {
        var path = Path.Combine(RepoRoot, Path.Combine(parts));
        Assert.True(File.Exists(path), $"Expected source file at '{path}'.");
        return File.ReadAllText(path).ToLowerInvariant();
    }

    private static string RepoRoot
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "ViceSharp.slnx")))
                directory = directory.Parent;

            if (directory is null)
                throw new InvalidOperationException("Could not locate repository root.");

            return directory.FullName;
        }
    }
}
