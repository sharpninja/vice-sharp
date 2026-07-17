// PLAN-ROMM-001 X3 (IMPL-ROMM-012): RomM library page code-behind. #if HAS_UWP-guarded in full.
#if HAS_UWP
namespace ViceSharp.Xbox.Views;

using System;
using System.Collections.Generic;
using System.Linq;
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
    private string? _serverUrl;
    private string? _token;

    /// <summary>Creates the page.</summary>
    public LibraryPage() => InitializeComponent();

    // PLAN-ROMM-001 (AC-CONN-07): scan the LAN for RomM and, if a co-located csdb-bridge answers, sign in
    // ZERO-TOUCH as the current Xbox user via GET /romm/v1/connection (the bridge provisions the user and
    // returns a per-user token). Falls back to filling the URL box for manual token entry when no bridge
    // is reachable.
    private async void OnScan(object sender, RoutedEventArgs e)
    {
        try
        {
            StatusText.Text = "Scanning the local network for RomM...";
            IReadOnlyList<DiscoveredRomM> servers = await new RomMSubnetDiscovery().ScanAsync();
            if (servers.Count == 0)
            {
                StatusText.Text = "No RomM servers found on the local network. Enter a URL manually.";
                return;
            }

            DiscoveredRomM server = servers[0];
            UrlBox.Text = server.BaseUrl.ToString();

            // The csdb-bridge is co-located with RomM, on :8090.
            var bridgeUrl = new UriBuilder(server.BaseUrl) { Port = 8090, Path = "/" }.Uri;
            string userId = await GetXboxUserIdAsync();
            RomMConnection? connection = await new RomMBridgeConnectionSource().FetchAsync(bridgeUrl, userId);
            if (connection is not null)
            {
                StatusText.Text = "Signing in as this Xbox user via the bridge...";
                await ConnectWithAsync(connection.BaseUrl, connection.Token);
                return;
            }

            StatusText.Text = $"Found {server.BaseUrl}. No bridge auto-login - enter a token and Connect.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Scan failed: {ex.Message}";
        }
    }

    private async void OnConnect(object sender, RoutedEventArgs e)
    {
        string? token = string.IsNullOrWhiteSpace(TokenBox.Password) ? null : TokenBox.Password.Trim();
        await ConnectWithAsync(UrlBox.Text, token);
    }

    // Builds the browse VM against the given server + optional bearer token: a user-entered Client API
    // Token, or a per-user token minted by the bridge (AC-CONN-07). Shared by manual Connect and Scan.
    private async Task ConnectWithAsync(string serverUrl, string? token)
    {
        try
        {
            if (!Uri.TryCreate(serverUrl, UriKind.Absolute, out Uri? uri))
            {
                StatusText.Text = "Invalid server URL.";
                return;
            }

            var options = new RomMClientOptions { BaseAddress = uri };
            if (!string.IsNullOrWhiteSpace(token))
            {
                options.Auth = RomMAuth.ClientApiToken(token);
            }

            IRomMClient client = RomMClient.Create(options);
            var gateway = new RomMLibraryGateway(client);
            IGameLauncher launcher = App.Instance.CreateRomMGameLauncher();
            string cacheDir = System.IO.Path.Combine(ApplicationData.Current.LocalFolder.Path, "romm-cache");

            _browse = new LibraryBrowseViewModel(gateway, launcher, new C64MachineProvider(), cacheDir);
            await _browse.InitializeAsync();

            _serverUrl = uri.ToString();
            _token = token;
            ResultsList.ItemsSource = _browse.Items;
            StatusText.Text = $"Connected: {_browse.Total} C64 titles.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Connect failed: {ex.Message}";
        }
    }

    // The current Xbox user's stable per-device id, used to provision + scope a per-user RomM account.
    private static async Task<string> GetXboxUserIdAsync()
    {
        try
        {
            IReadOnlyList<Windows.System.User> users = await Windows.System.User.FindAllAsync(
                Windows.System.UserType.LocalUser,
                Windows.System.UserAuthenticationStatus.LocallyAuthenticated);
            Windows.System.User? user = users.FirstOrDefault() ?? (await Windows.System.User.FindAllAsync()).FirstOrDefault();
            return string.IsNullOrWhiteSpace(user?.NonRoamableId) ? "xbox-default" : user!.NonRoamableId;
        }
        catch
        {
            return "xbox-default";
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

        var request = new GameDetailsRequest(_serverUrl ?? UrlBox.Text, _token ?? TokenBox.Password, tile.Id);
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
