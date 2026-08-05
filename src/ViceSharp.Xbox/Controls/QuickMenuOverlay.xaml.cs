// PLAN-XBOXUWP S34 (IMPL-XBOXUWP-034): quick-menu overlay code-behind. #if HAS_UWP-guarded.
#if HAS_UWP
namespace ViceSharp.Xbox.Controls;

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

/// <summary>The in-game quick-menu overlay. Closing clears the navigation overlay flag.</summary>
public sealed partial class QuickMenuOverlay : UserControl
{
    /// <summary>Creates the overlay.</summary>
    public QuickMenuOverlay() => InitializeComponent();

    private void OnResume(object sender, RoutedEventArgs e) => Close();

    private void OnClose(object sender, RoutedEventArgs e) => Close();

    private static void Close() => App.Instance.Navigation.IsQuickMenuOpen = false;
}
#endif
