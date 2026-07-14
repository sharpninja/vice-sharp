// PLAN-XBOXUWP S34 (IMPL-XBOXUWP-034): settings page code-behind. #if HAS_UWP-guarded.
#if HAS_UWP
namespace ViceSharp.Xbox.Views;

using Microsoft.Extensions.Logging;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using ViceSharp.Xbox.ViewModels;

/// <summary>The settings page. Loads host-canonical settings on entry; applies/reverts on demand.</summary>
public sealed partial class SettingsPage : Page
{
    /// <summary>Creates the page and binds the shared XboxSettingsViewModel.</summary>
    public SettingsPage()
    {
        InitializeComponent();
        DataContext = App.Instance.SettingsVm;
    }

    private XboxSettingsViewModel? ViewModel => App.Instance.SettingsVm;

    /// <inheritdoc />
    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        // FEAT-XPERFHUD-001 toggle: reflect the persisted preference without firing Toggled
        // side effects (setting IsOn raises Toggled; SetPerfHudVisible is idempotent).
        PerfCountersToggle.IsOn = App.Instance.IsPerfHudVisible;

        if (ViewModel is not null)
        {
            await ViewModel.RefreshAsync();

            // FIX-XSETBLANK-001 diagnostics (operator 2026-07-14: "Settings broken", all
            // pickers blank): the refresh outcome is otherwise invisible when StatusText
            // sits below the scroll fold, so mirror the adopted state into the log.
            App.CreateLogger("Settings").LogInformation(
                "refresh: status='{Status}' computers={Computers} models={Models} renderers={Renderers} selComputer='{SelComputer}' selModel='{SelModel}' selRenderer='{SelRenderer}' volume={Volume}",
                ViewModel.StatusText,
                ViewModel.Computers.Count,
                ViewModel.Models.Count,
                ViewModel.Renderers.Count,
                ViewModel.SelectedComputer?.DisplayName,
                ViewModel.SelectedModel?.DisplayName,
                ViewModel.SelectedRenderer,
                ViewModel.MasterVolumePercent);
        }
        else
        {
            App.CreateLogger("Settings").LogWarning("refresh skipped: SettingsVm is null");
        }
    }

    // Applies + persists LIVE (head-local LocalSettings); independent of Apply/Revert.
    private void OnPerfCountersToggled(object sender, RoutedEventArgs e)
        => App.Instance.SetPerfHudVisible(PerfCountersToggle.IsOn);

    private async void OnApply(object sender, RoutedEventArgs e)
    {
        if (ViewModel is null)
            return;

        // RequiresRestart is measured against the last-APPLIED baseline, so it is the one
        // reliable "the session will be rebuilt" signal. (A prior guard also compared the
        // SelectedProfileId captured here against its post-apply value, but the picker has
        // ALREADY set the new profile before Apply is clicked, so the two always matched and
        // the rebuild hooks below never fired on a real model change: the operator's
        // PAL -> NTSC switch kept rendering with the PAL pixel aspect.)
        var restart = ViewModel.RequiresRestart;

        await ViewModel.ApplySettingsAsync(restartSession: restart);

        if (restart)
        {
            // The rebuilt session (same SessionId) invalidated the keyboard-input seam the
            // VirtualKeyboardViewModel cached at boot. Rebuild it; degrade on any failure:
            // this runs on the UI thread and must never throw. Idempotent when the restart
            // did not actually change the machine.
            try
            {
                App.Instance.RebuildKeyboardForCurrentSession();
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[ViceSharp.Xbox] keyboard rebuild after model change failed: {ex}");
            }

            // FIX-XASPECT-001: the recreated session may run a different video standard
            // (PAL <-> NTSC model change); re-apply the true composite pixel aspect + the
            // performance HUD's machine facts. (Internally guarded; never throws.)
            App.Instance.ApplyVideoAspectForCurrentSession();
        }
    }

    private void OnRevert(object sender, RoutedEventArgs e) => ViewModel?.RevertSettings();

    private void OnBack(object sender, RoutedEventArgs e)
    {
        App.Instance.Navigation.GoBack();
        if (Frame?.CanGoBack == true)
            Frame.GoBack();
    }
}
#endif
