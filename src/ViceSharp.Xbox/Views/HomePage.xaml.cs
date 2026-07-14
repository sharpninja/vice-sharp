// PLAN-XBOXUWP S34 (IMPL-XBOXUWP-034): Home page code-behind. #if HAS_UWP-guarded in full.
#if HAS_UWP
namespace ViceSharp.Xbox.Views;

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using ViceSharp.Xbox.ViewModels;

/// <summary>The launch page. Its buttons raise the HomeViewModel intents and push pages.</summary>
public sealed partial class HomePage : Page
{
    /// <summary>Creates the page and binds the shared HomeViewModel.</summary>
    public HomePage()
    {
        InitializeComponent();
        DataContext = App.Instance.Home;
    }

    private HomeViewModel ViewModel => App.Instance.Home;

    private void OnStart(object sender, RoutedEventArgs e) => ViewModel.StartNew();

    private void OnResume(object sender, RoutedEventArgs e) => ViewModel.Resume();

    // Mouse-friendly dismiss (operator 2026-07-14): leave the menu WITHOUT resetting or
    // resuming the emulator: pure UI (App.DismissMenu -> HideMenu), no host call at all.
    private void OnCloseMenu(object sender, RoutedEventArgs e) => App.Instance.DismissMenu();

    // Clicking the transparent background OUTSIDE the menu card dismisses too. Only fires
    // for the root itself: clicks on the card and its buttons have their OriginalSource
    // inside the Border subtree.
    private void OnBackgroundDismiss(object sender, PointerRoutedEventArgs e)
    {
        if (ReferenceEquals(e.OriginalSource, BackgroundRoot))
            App.Instance.DismissMenu();
    }

    private void OnSettings(object sender, RoutedEventArgs e) => Push(NavigationDestination.Settings, typeof(SettingsPage));

    private void OnDevices(object sender, RoutedEventArgs e) => Push(NavigationDestination.DeviceSetup, typeof(DeviceSetupPage));

    private void OnControls(object sender, RoutedEventArgs e) => Push(NavigationDestination.InputMapping, typeof(InputMappingPage));

    private void OnAbout(object sender, RoutedEventArgs e)
    {
        ViewModel.ShowAbout();
        Push(NavigationDestination.About, typeof(AboutPage));
    }

    private void Push(NavigationDestination destination, System.Type page)
    {
        App.Instance.Navigation.Push(destination);
        Frame?.Navigate(page);
    }
}
#endif
