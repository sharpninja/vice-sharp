// PLAN-XBOXUWP S40 (IMPL-XBOXUWP-040), area XROM. First-run ROM-provisioning gate. #if HAS_UWP in full.
#if HAS_UWP
namespace ViceSharp.Xbox.Views;

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using ViceSharp.Xbox.ViewModels;

/// <summary>
/// The first-run ROM-provisioning gate: a pre-boot Frame destination (NOT a nav back-stack
/// entry) shown when the C64 ROMs are missing. The confirm-gated Download runs the verified
/// core-set fetch; on success the ViewModel's state flips to Complete, which
/// <see cref="App"/> observes to rebuild the host and boot the C64.
/// </summary>
public sealed partial class RomProvisioningPage : Page
{
    /// <summary>Creates the page and binds the shared provisioning ViewModel.</summary>
    public RomProvisioningPage()
    {
        ViewModel = App.Instance.ProvisioningVm;
        DataContext = ViewModel;
        InitializeComponent();
    }

    /// <summary>The compiled-binding ({x:Bind}) source: the shared provisioning ViewModel.</summary>
    public XboxRomProvisioningViewModel ViewModel { get; }

    /// <inheritdoc />
    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        // 10-foot UI: land gamepad focus on the primary action.
        DownloadButton.Focus(FocusState.Programmatic);
        await ViewModel.RefreshAsync();
    }

    private async void OnDownload(object sender, RoutedEventArgs e)
    {
        // Honor the confirm gate immediately before the verified download.
        ViewModel.ConfirmDownload();
        await ViewModel.DownloadAsync();
    }

    private async void OnImport(object sender, RoutedEventArgs e)
    {
        // Import each core role in turn (each pick is size/MD5-validated + written by the VM).
        await ViewModel.ImportAsync(RomRole.Basic);
        await ViewModel.ImportAsync(RomRole.Kernal);
        await ViewModel.ImportAsync(RomRole.Chargen);
    }
}
#endif
