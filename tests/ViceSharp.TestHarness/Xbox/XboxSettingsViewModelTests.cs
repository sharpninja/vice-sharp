namespace ViceSharp.TestHarness.Xbox;

using System.Linq;
using System.Threading.Tasks;
using ViceSharp.Protocol;
using ViceSharp.TestHarness.Xbox.Fakes;
using ViceSharp.Xbox.ViewModels;
using Xunit;

/// <summary>
/// PLAN-XBOXUWP S26 (IMPL-XBOXUWP-026), area XSET / XMVVM. TEST-XSET-001 off-console
/// coverage for <see cref="XboxSettingsViewModel"/>: the 10-foot settings ViewModel
/// that reproduces the desktop settings behaviors against the portable
/// <see cref="IXboxSettingsGateway"/> seam. Every test drives a
/// <see cref="FakeXboxSettingsGateway"/> with canned Get/Update/Validate/ListProfiles
/// DTOs, so the suite runs with no engine, host, console, or XAML dependency
/// (TR-MVVM-001) and uses plain <see cref="FactAttribute"/> (no console gate).
/// </summary>
[Trait("Category", "Xbox")]
public sealed class XboxSettingsViewModelTests
{
    private const string SessionId = "xbox-session-1";

    /// <summary>
    /// A fully-populated host-canonical settings payload with distinctive stored ids
    /// (palette "pepto", pacing "vice", primary joystick Joystick2, etc.) so the tests
    /// can assert the catalog id-&gt;label binding and label-&gt;id application.
    /// </summary>
    private static SessionSettingsDto CanonicalSettings() => new(
        "c64",
        new LimiterSettingsDto(100, true, "vice"),
        new DisplaySettingsDto("host", "pepto", true, true, "2x", "visible-area", "vice-pixel-aspect"),
        new InputSettingsDto("c64:gtk3_pos", InputPort.Joystick2, false, "keyboard-joystick"),
        new AudioSettingsDto("enabled"),
        new ResourceSettingsDto("auto-detect"));

    /// <summary>
    /// FR-XSET-001, TR-XMVVM-001. TEST-XSET-001.
    /// Use case: opening the 10-foot Settings page loads the host-canonical settings and
    /// the available profiles, binding every stored id to its display label.
    /// Acceptance: after <see cref="XboxSettingsViewModel.RefreshAsync"/>, GetSettings and
    /// ListSettingsProfiles were each called, the stored palette id "pepto" is bound as the
    /// selected label "Pepto" (and renderer/joystick/resource/pacing likewise), the profile
    /// list is populated with the current profile selected, and IsDirty is false.
    /// </summary>
    [Fact]
    public async Task RefreshAsync_LoadsSettingsAndProfiles_BindsLabels_NotDirty()
    {
        var ct = TestContext.Current.CancellationToken;
        var fake = new FakeXboxSettingsGateway { CannedSettings = CanonicalSettings() };
        var vm = new XboxSettingsViewModel(fake, SessionId);

        await vm.RefreshAsync(ct);

        Assert.Equal(1, fake.GetSettingsCount);

        // Stored ids bound to their display labels via the catalog From* maps.
        Assert.Equal("Pepto", vm.SelectedPalette);
        Assert.Equal("Host direct", vm.SelectedRenderer);
        Assert.Equal("2x", vm.SelectedDisplayScale);
        Assert.Equal("Visible area", vm.SelectedCropMode);
        Assert.Equal("VICE pixel aspect", vm.SelectedAspectMode);
        Assert.Equal("Enabled", vm.SelectedAudioMode);
        Assert.Equal("Keyboard + joystick", vm.SelectedInputMode);
        Assert.Equal("Joystick 2", vm.SelectedPrimaryJoystickPort);
        Assert.False(vm.SwapJoystickPorts);
        Assert.Equal("Auto detect", vm.SelectedResourceMode);
        Assert.Equal("VICE", vm.SelectedPacingStrategy);
        Assert.Equal(100, vm.LimiterRatePercent);

        // Pickers expose the shared catalog option lists.
        Assert.Same(SettingsOptionCatalog.PaletteModes, vm.Palettes);
        Assert.Same(SettingsOptionCatalog.ResourceModes, vm.ResourceModes);

        // Profiles populated + current selected.
        Assert.NotEmpty(vm.Profiles);
        Assert.Equal("c64", vm.SelectedProfileId);

        Assert.False(vm.IsDirty);
        Assert.False(vm.RequiresRestart);
    }

    /// <summary>
    /// FR-XSET-001, FR-XSET-005, TR-XMVVM-001. TEST-XSET-001.
    /// Use case: the operator changes a setting and applies; the ViewModel sends canonical
    /// stored ids (never display labels) through the host and adopts whatever the host
    /// canonicalized back.
    /// Acceptance: after dirtying the renderer and calling ApplySettingsAsync, the recorded
    /// UpdateSettingsRequest carries the canonical id "software" for the changed field and
    /// "pepto" (not "Pepto") for the unchanged palette; the ViewModel adopts the host-returned
    /// renderer ("Host direct") even though "Software" was sent; IsDirty clears; and the
    /// Xbox-only mute preference never leaks into the audio-mode DTO.
    /// </summary>
    [Fact]
    public async Task ApplySettingsAsync_SendsCanonicalIds_AdoptsHostCanonical_ClearsDirty()
    {
        var ct = TestContext.Current.CancellationToken;
        var fake = new FakeXboxSettingsGateway
        {
            CannedSettings = CanonicalSettings(),
            // Host canonicalizes the renderer back to "host" (rejecting the "software" edit)
            // and echoes the rest, proving the ViewModel re-binds from the response.
            UpdateResponseOverride = CanonicalSettings(),
        };
        var vm = new XboxSettingsViewModel(fake, SessionId);
        await vm.RefreshAsync(ct);

        vm.SelectedRenderer = "Software";
        vm.Muted = true;                 // Xbox-only pref: must not enter the audio-mode DTO.
        Assert.True(vm.IsDirty);

        await vm.ApplySettingsAsync(restartSession: false, ct);

        Assert.NotNull(fake.LastUpdateRequest);
        Assert.Equal(SessionId, fake.LastUpdateRequest!.SessionId);
        Assert.Equal("software", fake.LastUpdateRequest.Display!.Renderer); // label -> canonical id
        Assert.Equal("pepto", fake.LastUpdateRequest.Display!.Palette);     // "pepto", not "Pepto"
        Assert.False(fake.LastUpdateRequest.RestartSession);
        Assert.Equal("enabled", fake.LastUpdateRequest.Audio!.Mode);        // mute did not leak in

        // Host-canonical adoption: the response renderer ("host") wins over the sent "Software".
        Assert.Equal("Host direct", vm.SelectedRenderer);
        Assert.False(vm.IsDirty);
    }

    /// <summary>
    /// FR-XSET-002, TR-XMVVM-001. TEST-XSET-001.
    /// Use case: rebuild-required changes (machine profile or resource mode) are flagged for
    /// restart, while live display changes are not.
    /// Acceptance: changing the resource mode or the profile sets RequiresRestart true; a plain
    /// renderer (display) change leaves RequiresRestart false; and Apply(restartSession:true)
    /// forwards RestartSession=true.
    /// </summary>
    [Fact]
    public async Task RequiresRestart_GatedByProfileAndResourceMode_NotByDisplay()
    {
        var ct = TestContext.Current.CancellationToken;
        var fake = new FakeXboxSettingsGateway
        {
            CannedSettings = CanonicalSettings(),
            CannedProfiles = new[]
            {
                new SettingsProfileDto("c64", "C64 PAL", "x64sc", true, true, "pal"),
                new SettingsProfileDto("c64ntsc", "C64 NTSC", "x64sc", false, true, "ntsc"),
            },
        };
        var vm = new XboxSettingsViewModel(fake, SessionId);
        await vm.RefreshAsync(ct);

        Assert.False(vm.RequiresRestart);

        // A live display change does not require restart.
        vm.SelectedRenderer = "Software";
        Assert.True(vm.IsDirty);
        Assert.False(vm.RequiresRestart);

        // A resource-mode change requires restart.
        vm.SelectedResourceMode = "Use configured paths";
        Assert.True(vm.RequiresRestart);

        // Revert to baseline, then a profile change also requires restart.
        vm.RevertSettings();
        Assert.False(vm.RequiresRestart);
        vm.SelectedProfileId = "c64ntsc";
        Assert.True(vm.RequiresRestart);

        // Apply forwards the restart flag verbatim.
        await vm.ApplySettingsAsync(restartSession: true, ct);
        Assert.True(fake.LastUpdateRequest!.RestartSession);
    }

    /// <summary>
    /// FR-XSET-003, TR-XMVVM-001. TEST-XSET-001.
    /// Use case: the operator validates the referenced resources without committing the change.
    /// Acceptance: ValidateSettingsAsync sends a ValidateSettingsResourcesRequest (with canonical
    /// ids), populates the per-resource ValidationResults, sends NO UpdateSettings, and does NOT
    /// clear the dirty flag; a subsequent edit clears the stale validation results.
    /// </summary>
    [Fact]
    public async Task ValidateSettingsAsync_PopulatesResults_SendsNoUpdate_KeepsDirty()
    {
        var ct = TestContext.Current.CancellationToken;
        var fake = new FakeXboxSettingsGateway
        {
            CannedSettings = CanonicalSettings(),
            CannedValidationResults = new[]
            {
                new SettingsResourceValidationDto("kernal", SettingsResourceKind.File, true, false, "ok"),
                new SettingsResourceValidationDto("basic", SettingsResourceKind.File, false, false, "missing"),
            },
        };
        var vm = new XboxSettingsViewModel(fake, SessionId);
        await vm.RefreshAsync(ct);

        vm.SelectedRenderer = "Software";
        Assert.True(vm.IsDirty);

        await vm.ValidateSettingsAsync(ct);

        Assert.NotNull(fake.LastValidateRequest);
        Assert.Equal("software", fake.LastValidateRequest!.Display!.Renderer); // canonical id validated
        Assert.Null(fake.LastUpdateRequest);                                   // no apply happened
        Assert.Equal(2, vm.ValidationResults.Count);
        Assert.True(vm.HasValidationResults);
        Assert.True(vm.IsDirty);                                               // validate does not clear dirty

        // Editing after validation clears the stale results.
        vm.SelectedPalette = "Amber";
        Assert.Empty(vm.ValidationResults);
        Assert.False(vm.HasValidationResults);
    }

    /// <summary>
    /// FR-XSET-004, FR-XSET-005, TR-XMVVM-001. TEST-XSET-001.
    /// Use case: the operator makes several changes then reverts to the last-applied local state.
    /// Acceptance: after dirtying host-settings fields plus the Xbox-only prefs (master volume,
    /// mute, TV-safe inset, stick deadzone), RevertSettings restores every field to the last
    /// refreshed/applied baseline, clears IsDirty and RequiresRestart, and clears validation
    /// results.
    /// </summary>
    [Fact]
    public async Task RevertSettings_RestoresBaseline_ClearsDirtyAndValidation()
    {
        var ct = TestContext.Current.CancellationToken;
        var fake = new FakeXboxSettingsGateway
        {
            CannedSettings = CanonicalSettings(),
            CannedValidationResults = new[]
            {
                new SettingsResourceValidationDto("kernal", SettingsResourceKind.File, false, false, "missing"),
            },
        };
        var vm = new XboxSettingsViewModel(fake, SessionId);
        await vm.RefreshAsync(ct);

        var baselineRenderer = vm.SelectedRenderer;
        var baselineResource = vm.SelectedResourceMode;
        var baselineVolume = vm.MasterVolumePercent;
        var baselineInset = vm.TvSafeAreaInsetPercent;
        var baselineLeftDeadzone = vm.LeftStickDeadzonePercent;

        vm.SelectedRenderer = "Software";
        vm.SelectedResourceMode = "Use configured paths";
        vm.MasterVolumePercent = 42;
        vm.Muted = true;
        vm.TvSafeAreaInsetPercent = 7;
        vm.LeftStickDeadzonePercent = 12;
        await vm.ValidateSettingsAsync(ct);

        Assert.True(vm.IsDirty);
        Assert.True(vm.RequiresRestart);
        Assert.NotEmpty(vm.ValidationResults);

        vm.RevertSettings();

        Assert.Equal(baselineRenderer, vm.SelectedRenderer);
        Assert.Equal(baselineResource, vm.SelectedResourceMode);
        Assert.Equal(baselineVolume, vm.MasterVolumePercent);
        Assert.False(vm.Muted);
        Assert.Equal(baselineInset, vm.TvSafeAreaInsetPercent);
        Assert.Equal(baselineLeftDeadzone, vm.LeftStickDeadzonePercent);
        Assert.False(vm.IsDirty);
        Assert.False(vm.RequiresRestart);
        Assert.Empty(vm.ValidationResults);
    }
}
