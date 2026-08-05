// PLAN-XBOXUWP S34 (IMPL-XBOXUWP-034): Home page code-behind. #if HAS_UWP-guarded in full.
#if HAS_UWP
namespace ViceSharp.Xbox.Views;

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using ViceSharp.Xbox.ViewModels;

/// <summary>The launch page. Its buttons raise the HomeViewModel intents and push pages.</summary>
public sealed partial class HomePage : Page
{
    /// <summary>The compiled-binding source for this page (also the DataContext).</summary>
    public HomeViewModel ViewModel { get; }

    /// <summary>Creates the page and binds the shared HomeViewModel.</summary>
    public HomePage()
    {
        // FEAT-XAOTBIND-001: assign ViewModel before InitializeComponent so {x:Bind}
        // can resolve at first load (same pattern as Settings/About/InputMapping).
        ViewModel = App.Instance.Home;
        DataContext = ViewModel;
        InitializeComponent();

        // FEAT-XMENUFOCUS-001: programmatic focus can no-op before the first layout
        // pass, so re-assert once the tree is live.
        Loaded += (_, _) => FocusCloseMenu();
    }

    /// <summary>
    /// FEAT-XMENUFOCUS-001 (operator 2026-07-14: "When opening the menu, always set
    /// focus to the 'Close Menu' button"): programmatic focus on the TOP button so a
    /// single press of A dismisses. Called on navigation here and by App.ShowMenu for
    /// re-shows that only flip the Frame's visibility.
    /// </summary>
    internal void FocusCloseMenu() => CloseMenuButton.Focus(FocusState.Programmatic);

    /// <inheritdoc />
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        FocusCloseMenu();
    }

    // Menu redesign (operator 2026-07-14): RESTART (cold reset, the last button) reuses
    // the StartNew intent; RESUME is gone (dismissing the menu resumes the paused machine).
    private void OnRestart(object sender, RoutedEventArgs e) => ViewModel.StartNew();

    // FEAT-XMENUSNAP-001: persist / restore a snapshot of the machine the menu is
    // holding paused. Fire-and-forget: the App methods log, guard, and dismiss on success.
    private void OnSave(object sender, RoutedEventArgs e) => _ = App.Instance.SaveSnapshotAsync();

    private void OnLoad(object sender, RoutedEventArgs e) => _ = App.Instance.LoadSnapshotAsync();

    // Mouse-friendly dismiss (operator 2026-07-14): leave the menu WITHOUT resetting or
    // resuming the emulator: pure UI (App.DismissMenu -> HideMenu), no host call at all.
    // FEAT-XMENUSLIDE-001 removed the click-outside backdrop: the menu is a docked
    // panel now, so clicks left of it land on the emulator view.
    private void OnCloseMenu(object sender, RoutedEventArgs e) => App.Instance.DismissMenu();

    // PLAN-ROMM-001 (AC-XUI-01): open the RomM game library browser.
    private void OnLibrary(object sender, RoutedEventArgs e) =>
        Push(NavigationDestination.Library, typeof(LibraryPage));

    // PLAN-ROMM-001 X3 (AC-XUI-06): open RomM list/collections management.
    private void OnLists(object sender, RoutedEventArgs e) =>
        Push(NavigationDestination.Lists, typeof(ListsPage));

    // PLAN-ROMM-001 X4 (FR-CSDB-001): open CSDb discovery + ingest.
    private void OnCsdb(object sender, RoutedEventArgs e) =>
        Push(NavigationDestination.Csdb, typeof(CsdbPage));

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
