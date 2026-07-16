// PLAN-ROMM-001 X3 (IMPL-ROMM-012): RomM library page code-behind. #if HAS_UWP-guarded in full.
#if HAS_UWP
namespace ViceSharp.Xbox.Views;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using global::RomM.Client;
using global::RomM.Client.Auth;
using ViceSharp.Library.ViewModels;
using ViceSharp.RomM;
using ViceSharp.Xbox.RomM;

/// <summary>
/// PLAN-ROMM-001 (AC-XUI-02). The RomM library page: connect, browse the C64 library, and Attach /
/// Attach+play the selection. Builds a <see cref="LibraryBrowseViewModel"/> over a
/// <see cref="RomMLibraryGateway"/> and the head's <see cref="XboxGameLauncher"/> on Connect.
/// </summary>
public sealed partial class LibraryPage : Page
{
    private LibraryBrowseViewModel? _browse;

    /// <summary>Creates the page.</summary>
    public LibraryPage() => InitializeComponent();

    // PLAN-ROMM-001 (AC-CONN-07): scan the local network and fill in the first RomM server found.
    private async void OnScan(object sender, RoutedEventArgs e)
    {
        try
        {
            StatusText.Text = "Scanning the local network for RomM servers...";
            IReadOnlyList<DiscoveredRomM> servers = await new RomMSubnetDiscovery().ScanAsync();
            if (servers.Count > 0)
            {
                UrlBox.Text = servers[0].BaseUrl.ToString();
                StatusText.Text = $"Found {servers.Count} server(s). Selected {servers[0].BaseUrl}. Connect to browse.";
            }
            else
            {
                StatusText.Text = "No RomM servers found on the local network. Enter a URL manually.";
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Scan failed: {ex.Message}";
        }
    }

    private async void OnConnect(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!Uri.TryCreate(UrlBox.Text, UriKind.Absolute, out Uri? uri))
            {
                StatusText.Text = "Invalid server URL.";
                return;
            }

            var options = new RomMClientOptions { BaseAddress = uri };
            if (!string.IsNullOrWhiteSpace(TokenBox.Password))
            {
                options.Auth = RomMAuth.ClientApiToken(TokenBox.Password.Trim());
            }

            IRomMClient client = RomMClient.Create(options);
            var gateway = new RomMLibraryGateway(client);
            IGameLauncher launcher = App.Instance.CreateRomMGameLauncher();
            string cacheDir = System.IO.Path.Combine(ApplicationData.Current.LocalFolder.Path, "romm-cache");

            _browse = new LibraryBrowseViewModel(gateway, launcher, new C64MachineProvider(), cacheDir);
            await _browse.InitializeAsync();

            ResultsList.ItemsSource = _browse.Items;
            StatusText.Text = $"Connected: {_browse.Total} C64 titles.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Connect failed: {ex.Message}";
        }
    }

    private async void OnAttachPlay(object sender, RoutedEventArgs e) => await AttachAsync(autostart: true);

    private async void OnAttach(object sender, RoutedEventArgs e) => await AttachAsync(autostart: false);

    private async void OnLoadMore(object sender, RoutedEventArgs e)
    {
        if (_browse is not null)
        {
            await _browse.LoadMoreAsync();
        }
    }

    // PLAN-ROMM-001 (AC-XUI-05): open the selected title's details page. Hands the details page the
    // server URL + token + rom id so it can rebuild the gateways and fetch the detail itself.
    private void OnDetails(object sender, RoutedEventArgs e)
    {
        if (ResultsList.SelectedItem is not RomTile tile)
        {
            StatusText.Text = "Select a title first.";
            return;
        }

        var request = new GameDetailsRequest(UrlBox.Text, TokenBox.Password, tile.Id);
        App.Instance.Navigation.Push(ViceSharp.Xbox.ViewModels.NavigationDestination.GameDetails);
        Frame?.Navigate(typeof(GameDetailsPage), request);
    }

    private void OnBack(object sender, RoutedEventArgs e)
    {
        App.Instance.Navigation.GoBack();
        if (Frame?.CanGoBack == true)
        {
            Frame.GoBack();
        }
    }

    private async Task AttachAsync(bool autostart)
    {
        if (_browse is null)
        {
            StatusText.Text = "Connect first.";
            return;
        }

        _browse.SelectedTile = ResultsList.SelectedItem as RomTile;
        if (!_browse.CanAttach)
        {
            StatusText.Text = "Select a launchable title.";
            return;
        }

        await _browse.AttachAsync(autostart);
        StatusText.Text = _browse.StatusMessage;
    }
}
#endif
