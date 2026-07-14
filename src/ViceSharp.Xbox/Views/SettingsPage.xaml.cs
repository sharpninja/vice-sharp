// PLAN-XBOXUWP S34 (IMPL-XBOXUWP-034): settings page code-behind. #if HAS_UWP-guarded.
#if HAS_UWP
namespace ViceSharp.Xbox.Views;

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
        if (ViewModel is not null)
            await ViewModel.RefreshAsync();
    }

    private async void OnApply(object sender, RoutedEventArgs e)
    {
        if (ViewModel is null)
            return;

        // Capture the model id BEFORE apply: a model change requests a session rebuild (same
        // SessionId), which invalidates the keyboard-input seam VirtualKeyboardViewModel cached
        // at boot. Adopt-back may re-canonicalize the profile, so compare against the post-apply
        // value.
        var previousProfileId = ViewModel.SelectedProfileId;
        var restart = ViewModel.RequiresRestart;

        await ViewModel.ApplySettingsAsync(restartSession: restart);

        if (restart
            && !string.Equals(previousProfileId, ViewModel.SelectedProfileId, System.StringComparison.Ordinal))
        {
            // Rebuild the on-screen keyboard against the recreated session. Degrade on any
            // failure: this runs on the UI thread and must never throw.
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
            // (PAL <-> NTSC model change); re-apply the true composite pixel aspect.
            // (Internally guarded; never throws.)
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
