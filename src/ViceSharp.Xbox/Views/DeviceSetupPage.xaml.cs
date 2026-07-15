// PLAN-XBOXUWP S34 (IMPL-XBOXUWP-034): device-setup page code-behind. #if HAS_UWP-guarded.
#if HAS_UWP
namespace ViceSharp.Xbox.Views;

using Microsoft.Extensions.Logging;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using ViceSharp.Xbox.ViewModels;

/// <summary>The device-setup page. Lists attached media on entry.</summary>
public sealed partial class DeviceSetupPage : Page
{
    /// <summary>Creates the page and binds the shared XboxDeviceSetupViewModel.</summary>
    public DeviceSetupPage()
    {
        InitializeComponent();
        DataContext = App.Instance.DeviceSetupVm;
    }

    private XboxDeviceSetupViewModel? ViewModel => App.Instance.DeviceSetupVm;

    /// <inheritdoc />
    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        try
        {
            if (ViewModel is not null)
                await ViewModel.RefreshAsync();
        }
        catch (System.Exception ex)
        {
            // async-void: an unguarded throw here kills the app (or the navigation);
            // a failed media listing must leave the page up with its empty cards.
            App.CreateLogger("Devices").LogError(ex, "device refresh failed");
        }
    }

    private void OnBack(object sender, RoutedEventArgs e)
    {
        App.Instance.Navigation.GoBack();
        if (Frame?.CanGoBack == true)
            Frame.GoBack();
    }
}
#endif
