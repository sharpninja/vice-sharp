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
        if (ViewModel is not null)
            await ViewModel.ApplySettingsAsync(restartSession: ViewModel.RequiresRestart);
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
