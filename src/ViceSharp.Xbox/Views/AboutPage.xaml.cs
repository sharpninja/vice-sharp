// PLAN-XBOXUWP S34 (IMPL-XBOXUWP-034): About page code-behind. #if HAS_UWP-guarded in full.
#if HAS_UWP
namespace ViceSharp.Xbox.Views;

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

/// <summary>The About / GPL disclosure page. Read-only over AboutViewModel.</summary>
public sealed partial class AboutPage : Page
{
    /// <summary>Creates the page and binds the shared AboutViewModel.</summary>
    public AboutPage()
    {
        InitializeComponent();
        DataContext = App.Instance.AboutVm;
    }

    private void OnBack(object sender, RoutedEventArgs e)
    {
        App.Instance.Navigation.GoBack();
        if (Frame?.CanGoBack == true)
            Frame.GoBack();
    }
}
#endif
