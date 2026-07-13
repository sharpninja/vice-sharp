// PLAN-XBOXUWP S34 (IMPL-XBOXUWP-034): device-setup page code-behind. #if HAS_UWP-guarded.
#if HAS_UWP
namespace ViceSharp.Xbox.Views;

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
        if (ViewModel is not null)
            await ViewModel.RefreshAsync();
    }

    private void OnBack(object sender, RoutedEventArgs e)
    {
        App.Instance.Navigation.GoBack();
        if (Frame?.CanGoBack == true)
            Frame.GoBack();
    }
}
#endif
