namespace ViceSharp.TestHarness.Ui;

using System.Linq;
using System.Threading;
using NSubstitute;
using ViceSharp.Avalonia.Host;
using ViceSharp.Avalonia.ViewModels;
using ViceSharp.Protocol;
using Xunit;

/// <summary>
/// FR: FR-UISETVIS-001. TEST: TEST-UISET-003.
/// Acceptance: VIC-20-only settings visibility follows the selected machine family
/// for direct selection, host refresh, and local restore without discarding the
/// inactive family's draft values.
/// </summary>
public sealed class Vic20SettingsVisibilityNotificationTests
{
    [Fact]
    public void SelectedMachineProfile_FamilyChange_NotifiesVic20Visibility()
    {
        var vm = new AttachPanelViewModel(new DisconnectedHostProtocolClient());
        var changes = TrackPropertyChanges(vm);

        vm.SelectedMachineProfile = vm.MachineProfiles.Single(profile => profile.Id == "vic20");

        Assert.True(vm.IsVic20Selected);
        Assert.Contains(nameof(AttachPanelViewModel.IsVic20Selected), changes);

        changes.Clear();
        vm.SelectedMachineProfile = vm.MachineProfiles.Single(profile => profile.Id == "c64");

        Assert.False(vm.IsVic20Selected);
        Assert.Contains(nameof(AttachPanelViewModel.IsVic20Selected), changes);
    }

    [Fact]
    public async Task RefreshSettings_HostFamilyChange_NotifiesBeforeSettingsFailure()
    {
        var host = Substitute.For<IHostProtocolClient>();
        host.ListMediaAsync(Arg.Any<CancellationToken>())
            .Returns(new ValueTask<ListMediaResponse>(new ListMediaResponse(RpcStatus.Ok(), [])));
        host.ListKeyboardMapsAsync(Arg.Any<CancellationToken>())
            .Returns(new ValueTask<ListKeyboardMapsResponse>(new ListKeyboardMapsResponse(RpcStatus.Ok(), [])));
        host.ListSettingsProfilesAsync(Arg.Any<CancellationToken>())
            .Returns(new ValueTask<ListSettingsProfilesResponse>(new ListSettingsProfilesResponse(
                RpcStatus.Ok(),
                [
                    new SettingsProfileDto("c64", "C64 PAL", "x64sc", false, true),
                    new SettingsProfileDto("vic20", "VIC-20 PAL", "xvic", true, true),
                ])));
        host.GetSettingsAsync(Arg.Any<CancellationToken>())
            .Returns(new ValueTask<GetSettingsResponse>(new GetSettingsResponse(
                RpcStatus.Unavailable("settings unavailable"),
                null)));

        var vm = new AttachPanelViewModel(host);
        var changes = TrackPropertyChanges(vm);

        await vm.RefreshAsync(TestContext.Current.CancellationToken);

        Assert.True(vm.IsVic20Selected);
        Assert.Contains(nameof(AttachPanelViewModel.IsVic20Selected), changes);
    }

    [Fact]
    public void RevertSettings_FamilyChange_NotifiesVic20Visibility()
    {
        var vm = new AttachPanelViewModel(new DisconnectedHostProtocolClient());
        vm.SelectedMachineProfile = vm.MachineProfiles.Single(profile => profile.Id == "vic20ntsc");
        var changes = TrackPropertyChanges(vm);

        vm.RevertSettings();

        Assert.False(vm.IsVic20Selected);
        Assert.Contains(nameof(AttachPanelViewModel.IsVic20Selected), changes);
    }

    [Fact]
    public void SwitchingMachineFamily_PreservesVic20DraftAndVisibilityFlag()
    {
        var vm = new AttachPanelViewModel(new DisconnectedHostProtocolClient());
        var vic20 = vm.MachineProfiles.Single(profile => profile.Id == "vic20");
        var c64 = vm.MachineProfiles.Single(profile => profile.Id == "c64");

        Assert.False(vm.IsVic20Selected);
        vm.SelectedMachineProfile = vic20;
        vm.SelectedVic20MemoryPreset = "All";
        vm.SelectedExpansionCartKind = "fe3";
        vm.SelectedExpansionCartPreset = "super-rom";
        Assert.True(vm.IsVic20Selected);

        vm.SelectedMachineProfile = c64;
        Assert.False(vm.IsVic20Selected);
        vm.SelectedMachineProfile = vic20;

        Assert.True(vm.IsVic20Selected);
        Assert.Equal("All", vm.SelectedVic20MemoryPreset);
        Assert.Equal("fe3", vm.SelectedExpansionCartKind);
        Assert.Equal("super-rom", vm.SelectedExpansionCartPreset);
    }

    private static List<string> TrackPropertyChanges(AttachPanelViewModel vm)
    {
        var changes = new List<string>();
        vm.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is not null)
                changes.Add(args.PropertyName);
        };
        return changes;
    }
}
