// PLAN-XBOXUWP S34 (IMPL-XBOXUWP-034): input-mapping page code-behind. #if HAS_UWP-guarded.
#if HAS_UWP
namespace ViceSharp.Xbox.Views;

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using ViceSharp.Xbox.ViewModels;

/// <summary>The read-only controller-mapping page. Its Rows come from InputMappingViewModel.</summary>
public sealed partial class InputMappingPage : Page
{
    /// <summary>The shared input-mapping ViewModel, bound by compiled {x:Bind}.</summary>
    public InputMappingViewModel ViewModel { get; }

    /// <summary>Creates the page and binds the shared InputMappingViewModel.</summary>
    public InputMappingPage()
    {
        ViewModel = App.Instance.InputMappingVm;
        DataContext = ViewModel;
        InitializeComponent();
    }

    private void OnVirtualKeyboard(object sender, RoutedEventArgs e)
    {
        App.Instance.InputMappingVm.RequestOpenVirtualKeyboard();
        App.Instance.Navigation.IsVirtualKeyboardOpen = true;
    }

    private void OnBack(object sender, RoutedEventArgs e)
    {
        App.Instance.Navigation.GoBack();
        if (Frame?.CanGoBack == true)
            Frame.GoBack();
    }
}
#endif
