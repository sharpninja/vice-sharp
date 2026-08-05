namespace ViceSharp.TestHarness.Xbox;

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ViceSharp.Protocol;
using ViceSharp.TestHarness.Xbox.Fakes;
using ViceSharp.Xbox.ViewModels;
using Xunit;

/// <summary>
/// PLAN-XBOXUWP (area XSET / XMVVM). TEST-XSET-001 off-console coverage for the
/// "pick a computer and model" selector on <see cref="XboxSettingsViewModel"/>: the
/// 10-foot Settings page exposes a Computer picker (only the implemented C64 family is
/// enabled; VIC-20 / C128 / PET / Plus4 are disabled placeholders) and a Model picker
/// (the C64 machine profiles, with the "Minimal host" pseudo-profile filtered out).
/// Selecting a model routes through the EXISTING restart-gated
/// <see cref="XboxSettingsViewModel.SelectedProfileId"/> so a model change flags a
/// session restart and is applied as the profile id. Every test drives a
/// <see cref="FakeXboxSettingsGateway"/> so the suite runs with no engine, host,
/// console, or XAML dependency (TR-MVVM-001) and uses plain <see cref="FactAttribute"/>.
/// </summary>
[Trait("Category", "Xbox")]
public sealed class XboxComputerModelPickerTests
{
    private const string SessionId = "xbox-session-picker";

    /// <summary>Host-canonical settings whose stored <c>ProfileId</c> is the given model id.</summary>
    private static SessionSettingsDto SettingsWithProfile(string profileId) => new(
        profileId,
        new LimiterSettingsDto(100, true, "vice"),
        new DisplaySettingsDto("host", "vice", true, true, "2x", "visible-area", "vice-pixel-aspect"),
        new InputSettingsDto("c64:gtk3_pos", InputPort.Joystick2, false, "keyboard-joystick"),
        new AudioSettingsDto("enabled"),
        new ResourceSettingsDto("auto-detect"));

    /// <summary>
    /// A "Minimal host" pseudo-profile (Machine=="minimal", MUST be filtered out) plus three
    /// selectable C64 profiles (Machine=="x64sc").
    /// </summary>
    private static SettingsProfileDto[] SeededProfiles() => new[]
    {
        new SettingsProfileDto("minimal", "Minimal host", "minimal", false, true),
        new SettingsProfileDto("c64", "Commodore 64 PAL", "x64sc", true, true),
        new SettingsProfileDto("c64c", "Commodore 64C PAL", "x64sc", false, true),
        new SettingsProfileDto("ntsc", "Commodore 64 NTSC", "x64sc", false, true),
    };

    private static XboxSettingsViewModel BuildSeededVm(FakeXboxSettingsGateway fake) =>
        new(fake, SessionId);

    /// <summary>
    /// TEST-XSET-001. Use case: the Model picker lists the selectable C64 machine profiles
    /// and never the non-selectable "Minimal host" pseudo-profile.
    /// Acceptance: after <see cref="XboxSettingsViewModel.RefreshAsync"/>, Models is exactly
    /// the three x64sc entries in order and contains no Machine=="minimal" entry.
    /// </summary>
    [Fact]
    public async Task Models_ExcludesMinimalHost_ListsC64Profiles()
    {
        var ct = TestContext.Current.CancellationToken;
        var fake = new FakeXboxSettingsGateway
        {
            CannedSettings = SettingsWithProfile("c64"),
            CannedProfiles = SeededProfiles(),
        };
        var vm = BuildSeededVm(fake);

        await vm.RefreshAsync(ct);

        Assert.Equal(3, vm.Models.Count);
        Assert.All(vm.Models, m => Assert.Equal("x64sc", m.Machine));
        Assert.DoesNotContain(vm.Models, m => m.Machine == "minimal");
        Assert.Equal(new[] { "c64", "c64c", "ntsc" }, vm.Models.Select(m => m.Id).ToArray());
    }

    /// <summary>
    /// TEST-XSET-001. Use case: the Computer picker offers the implemented C64 plus disabled
    /// placeholders for the not-yet-ported machines.
    /// Acceptance: Computers contains "Commodore 64" (x64sc) with IsAvailable true and the
    /// VIC-20 / C128 / PET / Plus4 families with IsAvailable false.
    /// </summary>
    [Fact]
    public void Computers_ListsC64EnabledPlusDisabledPlaceholders()
    {
        var vm = new XboxSettingsViewModel(new FakeXboxSettingsGateway(), SessionId);

        var c64 = Assert.Single(vm.Computers, c => c.FamilyId == "x64sc");
        Assert.Equal("Commodore 64", c64.DisplayName);
        Assert.True(c64.IsAvailable);

        Assert.All(
            vm.Computers.Where(c => c.FamilyId != "x64sc"),
            c => Assert.False(c.IsAvailable));

        Assert.Contains(vm.Computers, c => c.FamilyId == "xvic");
        Assert.Contains(vm.Computers, c => c.FamilyId == "x128");
        Assert.Contains(vm.Computers, c => c.FamilyId == "xpet");
        Assert.Contains(vm.Computers, c => c.FamilyId == "xplus4");
    }

    /// <summary>
    /// TEST-XSET-001. Use case: the Model combo reflects the currently selected profile id.
    /// Acceptance: after refresh (current "c64") SelectedModel.Id is "c64"; setting
    /// SelectedProfileId to "c64c" re-resolves SelectedModel to the "c64c" entry.
    /// </summary>
    [Fact]
    public async Task SelectedModel_ReflectsSelectedProfileId()
    {
        var ct = TestContext.Current.CancellationToken;
        var fake = new FakeXboxSettingsGateway
        {
            CannedSettings = SettingsWithProfile("c64"),
            CannedProfiles = SeededProfiles(),
        };
        var vm = BuildSeededVm(fake);
        await vm.RefreshAsync(ct);

        Assert.NotNull(vm.SelectedModel);
        Assert.Equal("c64", vm.SelectedModel!.Id);

        vm.SelectedProfileId = "c64c";
        Assert.Equal("c64c", vm.SelectedModel!.Id);
    }

    /// <summary>
    /// TEST-XSET-001. Use case: choosing a different model in the picker maps to the profile
    /// id and flags the required session restart.
    /// Acceptance: setting SelectedModel to the "c64c" DTO sets SelectedProfileId to "c64c"
    /// and turns IsDirty and RequiresRestart true (restart gating is the existing
    /// profile-change behavior).
    /// </summary>
    [Fact]
    public async Task SetSelectedModel_MapsToProfileId_AndFlagsRestart()
    {
        var ct = TestContext.Current.CancellationToken;
        var fake = new FakeXboxSettingsGateway
        {
            CannedSettings = SettingsWithProfile("c64"),
            CannedProfiles = SeededProfiles(),
        };
        var vm = BuildSeededVm(fake);
        await vm.RefreshAsync(ct);

        var c64c = vm.Models.Single(m => m.Id == "c64c");
        vm.SelectedModel = c64c;

        Assert.Equal("c64c", vm.SelectedProfileId);
        Assert.True(vm.IsDirty);
        Assert.True(vm.RequiresRestart);
    }

    /// <summary>
    /// TEST-XSET-001. Use case: applying after a model change sends the new profile id and
    /// requests the session rebuild.
    /// Acceptance: after selecting "c64c" and applying with restartSession==RequiresRestart,
    /// the recorded UpdateSettingsRequest carries ProfileId "c64c" and RestartSession true.
    /// </summary>
    [Fact]
    public async Task Apply_AfterModelChange_SendsProfileId_AndRestartTrue()
    {
        var ct = TestContext.Current.CancellationToken;
        var fake = new FakeXboxSettingsGateway
        {
            CannedSettings = SettingsWithProfile("c64"),
            CannedProfiles = SeededProfiles(),
        };
        var vm = BuildSeededVm(fake);
        await vm.RefreshAsync(ct);

        vm.SelectedModel = vm.Models.Single(m => m.Id == "c64c");
        Assert.True(vm.RequiresRestart);

        await vm.ApplySettingsAsync(restartSession: vm.RequiresRestart, ct);

        Assert.NotNull(fake.LastUpdateRequest);
        Assert.Equal("c64c", fake.LastUpdateRequest!.ProfileId);
        Assert.True(fake.LastUpdateRequest.RestartSession);
    }

    /// <summary>
    /// TEST-XSET-001. Use case: the model setter must not strand the session on a bad choice.
    /// Acceptance: setting SelectedModel to null leaves SelectedProfileId unchanged and does
    /// not dirty the ViewModel; setting it to a DTO whose Id is not among Models is ignored
    /// (no profile change, no dirty flip).
    /// </summary>
    [Fact]
    public async Task SetSelectedModel_NullOrUnknown_IsGuarded()
    {
        var ct = TestContext.Current.CancellationToken;
        var fake = new FakeXboxSettingsGateway
        {
            CannedSettings = SettingsWithProfile("c64"),
            CannedProfiles = SeededProfiles(),
        };
        var vm = BuildSeededVm(fake);
        await vm.RefreshAsync(ct);

        Assert.Equal("c64", vm.SelectedProfileId);
        Assert.False(vm.IsDirty);

        vm.SelectedModel = null;
        Assert.Equal("c64", vm.SelectedProfileId);
        Assert.False(vm.IsDirty);

        var unknown = new SettingsProfileDto("does-not-exist", "Ghost", "x64sc", false, true);
        vm.SelectedModel = unknown;
        Assert.Equal("c64", vm.SelectedProfileId);
        Assert.False(vm.IsDirty);
    }

    /// <summary>
    /// FEAT-XROMPICK-001. Use case: Settings model picker surfaces ROM readiness so the
    /// player knows a model will boot-block before they restart.
    /// Acceptance: empty C64 dir reports not provisioned; complete synthetic set reports
    /// ready; Ultimax profile id maps to kernal-optional; unconfigured evaluator shows
    /// the open-ROM-page guidance.
    /// </summary>
    [Fact]
    public void SelectedModelRomStatus_ReflectsProvisionEvaluator()
    {
        var fake = new FakeXboxSettingsGateway
        {
            CannedSettings = SettingsWithProfile("c64"),
            CannedProfiles = SeededProfiles(),
        };
        var vm = BuildSeededVm(fake);
        Assert.Contains("not configured", vm.SelectedModelRomStatus, StringComparison.OrdinalIgnoreCase);

        var empty = Path.Combine(Path.GetTempPath(), "vicesharp-rom-status-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(empty);
        try
        {
            var evaluator = new RomProvisionEvaluator(RomProvisionTestData.Catalog);
            vm.ConfigureRomReadiness(evaluator, empty);
            Assert.Contains("not provisioned", vm.SelectedModelRomStatus, StringComparison.OrdinalIgnoreCase);

            RomProvisionTestData.WriteValidSet(empty);
            vm.ConfigureRomReadiness(evaluator, empty);
            Assert.Contains("ready", vm.SelectedModelRomStatus, StringComparison.OrdinalIgnoreCase);

            Assert.Equal(RomProfile.Ultimax, XboxSettingsViewModel.ResolveRomProfile("c64-ultimax"));
            Assert.Equal(RomProfile.Standard, XboxSettingsViewModel.ResolveRomProfile("c64"));
        }
        finally
        {
            try { Directory.Delete(empty, recursive: true); } catch { /* best effort */ }
        }
    }
}
