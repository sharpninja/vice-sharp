// PLAN-ROMM-001 X3 (IMPL-ROMM-014): RomM list-management page code-behind. #if HAS_UWP-guarded in full.
#if HAS_UWP
namespace ViceSharp.Xbox.Views;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using global::RomM.Client;
using global::RomM.Client.Auth;
using ViceSharp.Library.ViewModels;
using ViceSharp.Protocol;
using ViceSharp.RomM;
using ViceSharp.Xbox.RomM;

/// <summary>
/// PLAN-ROMM-001 (AC-XUI-06). List management: Library-like auto-connect, Recents + server collections,
/// List/Grid member views, and Attach / Attach+autostart on selected titles (cache-aware download).
/// </summary>
public sealed partial class ListsPage : Page
{
    private const int RecentsListId = -1;

    private CollectionsViewModel? _collections;
    private IRomMLibraryGateway? _library;
    private IGameLauncher? _launcher;
    private IRecentsStore? _recents;
    private XboxCoverImageLoader? _coverLoader;
    private IReadOnlyList<RecentGame> _recentGames = Array.Empty<RecentGame>();
    private IReadOnlyList<RomTile> _memberTiles = Array.Empty<RomTile>();
    private RomTile? _selectedTile;
    private string? _token;
    private string _cacheDir = string.Empty;
    private bool _autoConnectTried;
    private bool _gridMode;

    /// <summary>Creates the page.</summary>
    public ListsPage()
    {
        InitializeComponent();
        MembersList.ContainerContentChanging += OnMemberContainerChanging;
        AddHandler(
            UIElement.KeyDownEvent,
            new Windows.UI.Xaml.Input.KeyEventHandler(OnPageKeyDown),
            handledEventsToo: true);
        ApplyViewMode();
    }

    /// <inheritdoc />
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (_autoConnectTried)
        {
            return;
        }

        _autoConnectTried = true;
        _ = AutoConnectAsync();
    }

    private async Task AutoConnectAsync(bool forceScan = false)
    {
        try
        {
            IRomMConnectionStore store = CreateConnectionStore();
            Uri? baseUrl = null;
            RomMConnection? saved = null;

            if (!forceScan)
            {
                ConnectStatus.Text = "Looking for a remembered RomM server...";
                var locator = new RomMServerLocator(store, new RomMHeartbeatProbe(), new RomMSubnetDiscovery());
                RomMLocateResult located = await locator.LocateAsync();
                ConnectStatus.Text = located.StatusMessage;
                baseUrl = located.BaseUrl;
                saved = located.SavedConnection;
            }
            else
            {
                ConnectStatus.Text = "Scanning the local network for RomM...";
                IReadOnlyList<DiscoveredRomM> servers = await new RomMSubnetDiscovery().ScanAsync();
                if (servers.Count == 0)
                {
                    ConnectStatus.Text = "No RomM servers found. Enter a URL and token, then Connect.";
                    return;
                }

                baseUrl = servers[0].BaseUrl;
                ConnectStatus.Text = $"Found {baseUrl}.";
            }

            if (baseUrl is null)
            {
                return;
            }

            UrlBox.Text = baseUrl.ToString();
            string? lastError = null;

            if (saved is { Token.Length: > 0 })
            {
                ConnectStatus.Text = $"Reconnecting to {baseUrl}...";
                if (await TryConnectWithAsync(baseUrl.ToString(), saved.Token, saved.AuthMode))
                {
                    return;
                }

                lastError = ConnectStatus.Text;
                ConnectStatus.Text = "Saved token failed; trying the bridge...";
            }

            var bridgeUrl = new UriBuilder(baseUrl) { Port = 8090, Path = "/" }.Uri;
            string userId = await GetXboxUserIdAsync();
            RomMConnection? bridge = await new RomMBridgeConnectionSource().FetchAsync(bridgeUrl, userId);
            if (bridge is not null)
            {
                ConnectStatus.Text = "Signing in as this Xbox user via the bridge...";
                if (await TryConnectWithAsync(baseUrl.ToString(), bridge.Token, RomMAuthMode.SubnetShared))
                {
                    return;
                }

                lastError = ConnectStatus.Text;
            }

            ConnectStatus.Text = string.IsNullOrWhiteSpace(lastError)
                ? $"Found {baseUrl}. Enter a Client API token and Connect."
                : $"{lastError} Enter a Client API token and Connect.";
        }
        catch (Exception ex)
        {
            ConnectStatus.Text = $"Auto-connect failed: {ex.Message}. Enter a URL + token and Connect.";
        }
    }

    private async void OnScan(object sender, RoutedEventArgs e) => await AutoConnectAsync(forceScan: true);

    private async void OnConnect(object sender, RoutedEventArgs e)
    {
        string? token = string.IsNullOrWhiteSpace(TokenBox.Password) ? null : TokenBox.Password.Trim();
        await TryConnectWithAsync(UrlBox.Text, token, RomMAuthMode.ClientToken);
    }

    private static IRomMConnectionStore CreateConnectionStore()
    {
        string path = System.IO.Path.Combine(ApplicationData.Current.LocalFolder.Path, "romm-connection.json");
        return new FileRomMConnectionStore(path);
    }

    private async Task<bool> TryConnectWithAsync(string serverUrl, string? token, RomMAuthMode authMode)
    {
        try
        {
            if (!Uri.TryCreate(serverUrl, UriKind.Absolute, out Uri? uri))
            {
                ConnectStatus.Text = "Invalid server URL.";
                return false;
            }

            var options = new RomMClientOptions { BaseAddress = uri };
            if (!string.IsNullOrWhiteSpace(token))
            {
                options.Auth = RomMAuth.ClientApiToken(token);
            }

            IRomMClient client = RomMClient.Create(options);
            _library = new RomMLibraryGateway(client);
            _collections = new CollectionsViewModel(new RomMCollectionsGateway(client));
            _launcher = App.Instance.CreateRomMGameLauncher();
            _token = token;
            _cacheDir = System.IO.Path.Combine(ApplicationData.Current.LocalFolder.Path, "romm-cache");
            _coverLoader = new XboxCoverImageLoader(uri, token);
            _recents = new FileRecentsStore(
                System.IO.Path.Combine(ApplicationData.Current.LocalFolder.Path, "romm-recents.json"));

            await _collections.RefreshAsync();
            _recentGames = await _recents.LoadAsync();
            await BindCollectionsAsync();

            ConnectPanel.Visibility = Visibility.Collapsed;
            BrowseArea.Visibility = Visibility.Visible;
            ActionBar.Visibility = Visibility.Visible;
            SelectionBar.Visibility = Visibility.Visible;
            ApplyViewMode();
            StatusText.Text = $"{_collections.Collections.Count} server list(s)"
                + (_recentGames.Count > 0 ? $"; {_recentGames.Count} recent." : ".");
            CountText.Text = StatusText.Text;
            CollectionsList.Focus(FocusState.Programmatic);

            await CreateConnectionStore().SaveAsync(
                new RomMConnection(uri.ToString().TrimEnd('/') + "/", authMode, token ?? string.Empty));

            return true;
        }
        catch (Exception ex)
        {
            ConnectStatus.Text = $"Connect failed: {ex.Message}";
            return false;
        }
    }

    private async Task BindCollectionsAsync()
    {
        if (_collections is null)
        {
            return;
        }

        if (_recents is not null)
        {
            _recentGames = await _recents.LoadAsync();
        }

        var rows = new List<LibraryCollection>();
        if (_recentGames.Count > 0)
        {
            rows.Add(new LibraryCollection(
                RecentsListId,
                "Recents",
                _recentGames.Count,
                ReadOnly: true,
                _recentGames.Select(g => g.Id).ToList()));
        }

        foreach (LibraryCollection collection in _collections.Collections)
        {
            rows.Add(collection);
        }

        CollectionsList.ItemsSource = rows;
    }

    private async void OnCollectionSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedTile = null;
        SelectedText.Text = "No game selected";
        MembersList.ItemsSource = null;
        MembersGrid.ItemsSource = null;

        if (CollectionsList.SelectedItem is not LibraryCollection selected)
        {
            return;
        }

        StatusText.Text = "Loading titles...";
        try
        {
            if (selected.Id == RecentsListId)
            {
                if (_recents is not null)
                {
                    _recentGames = await _recents.LoadAsync();
                }

                _memberTiles = _recentGames.Select(g => g.ToTile()).ToList();
                NameBox.Text = string.Empty;
                StatusText.Text = $"Recents: {_memberTiles.Count} game(s). Select one to Attach.";
            }
            else
            {
                if (_collections is not null)
                {
                    _collections.SelectedCollection = selected;
                }

                NameBox.Text = selected.Name;
                _memberTiles = await LoadCollectionTilesAsync(selected).ConfigureAwait(true);
                StatusText.Text = selected.ReadOnly
                    ? $"{selected.Name} is server-managed (read-only). {_memberTiles.Count} title(s)."
                    : $"{selected.Name}: {_memberTiles.Count} title(s). Select one to Attach.";
            }

            MembersList.ItemsSource = _memberTiles;
            MembersGrid.ItemsSource = _memberTiles;
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Load failed: {ex.Message}";
            _memberTiles = Array.Empty<RomTile>();
        }
    }

    private async Task<IReadOnlyList<RomTile>> LoadCollectionTilesAsync(LibraryCollection collection)
    {
        if (_library is null || collection.RomIds.Count == 0)
        {
            return Array.Empty<RomTile>();
        }

        var tiles = new List<RomTile>(collection.RomIds.Count);
        foreach (int romId in collection.RomIds)
        {
            try
            {
                RomDetail detail = await _library.GetRomAsync(romId).ConfigureAwait(false);
                RomFile? file = detail.Files.FirstOrDefault(f => f.Launchable)
                    ?? detail.Files.FirstOrDefault();
                string fileName = file?.FileName ?? detail.Name;
                long? size = file is { SizeBytes: > 0 } ? file.SizeBytes : null;
                bool launchable = file?.Launchable ?? MediaExtensionMap.IsLaunchable(fileName);
                tiles.Add(new RomTile(
                    detail.Id,
                    detail.Name,
                    fileName,
                    detail.PlatformSlug,
                    size,
                    detail.Cover,
                    launchable));
            }
            catch
            {
                tiles.Add(new RomTile(romId, $"Rom #{romId}", string.Empty, null, null, null, false));
            }
        }

        return tiles;
    }

    private void OnShowListView(object sender, RoutedEventArgs e)
    {
        _gridMode = false;
        ApplyViewMode();
    }

    private void OnShowGridView(object sender, RoutedEventArgs e)
    {
        _gridMode = true;
        ApplyViewMode();
    }

    private void ApplyViewMode()
    {
        MembersList.Visibility = _gridMode ? Visibility.Collapsed : Visibility.Visible;
        MembersGrid.Visibility = _gridMode ? Visibility.Visible : Visibility.Collapsed;
        ListViewButton.Opacity = _gridMode ? 0.55 : 1.0;
        GridViewButton.Opacity = _gridMode ? 1.0 : 0.55;
    }

    private void OnMemberSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        RomTile? tile = null;
        if (sender is ListView list)
        {
            tile = list.SelectedItem as RomTile;
        }
        else if (sender is GridView grid)
        {
            tile = grid.SelectedItem as RomTile;
        }

        _selectedTile = tile;
        if (tile is null)
        {
            SelectedText.Text = "No game selected";
            return;
        }

        SelectedText.Text = tile.Launchable
            ? $"Selected  {tile.Name} · {tile.FileName}"
            : $"Selected  {tile.Name} (not launchable)";
    }

    private async void OnMemberClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is not RomTile tile)
        {
            return;
        }

        _selectedTile = tile;
        if (_gridMode)
        {
            MembersGrid.SelectedItem = tile;
        }
        else
        {
            MembersList.SelectedItem = tile;
        }

        SelectedText.Text = $"Selected  {tile.Name} · {tile.FileName}";
        if (tile.Launchable)
        {
            await AttachAsync(autostart: false);
        }
    }

    private async void OnAttach(object sender, RoutedEventArgs e) => await AttachAsync(autostart: false);

    private async void OnAttachAutostart(object sender, RoutedEventArgs e) => await AttachAsync(autostart: true);

    private async Task AttachAsync(bool autostart)
    {
        if (_selectedTile is null || _library is null || _launcher is null)
        {
            StatusText.Text = "Select a launchable title first.";
            return;
        }

        RomTile tile = _selectedTile;
        if (!tile.Launchable || string.IsNullOrWhiteSpace(tile.FileName))
        {
            StatusText.Text = "That title is not launchable.";
            return;
        }

        try
        {
            StatusText.Text = "Downloading...";
            var progress = new Progress<double>(f => StatusText.Text = $"Downloading {f:P0}");
            AcquiredGame game = await _library.DownloadAsync(
                tile.Id,
                tile.FileName,
                tile.SizeBytes ?? 0,
                _cacheDir,
                progress,
                CancellationToken.None);

            StatusText.Text = "Starting...";
            MediaSlot slot = MediaExtensionMap.Resolve(tile.FileName)?.Slot ?? MediaSlot.Drive8;
            LaunchOutcome outcome = await _launcher.LaunchAsync(game, slot, autostart, CancellationToken.None);
            StatusText.Text = outcome.Message;

            if (outcome.Success)
            {
                if (_recents is not null)
                {
                    try
                    {
                        await _recents.RecordAsync(RecentGame.FromTile(tile));
                    }
                    catch
                    {
                        // best-effort
                    }
                }

                if (autostart)
                {
                    App.Instance.DismissMenu();
                }
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Attach failed: {ex.Message}";
        }
    }

    private void OnMemberContainerChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
    {
        if (args.InRecycleQueue || args.Item is not RomTile tile)
        {
            return;
        }

        if (args.ItemContainer.ContentTemplateRoot is not FrameworkElement root)
        {
            return;
        }

        Image? image = root.FindName("CoverImage") as Image
            ?? root.FindName("ListCoverImage") as Image;
        if (image is null)
        {
            return;
        }

        image.Source = null;
        if (_coverLoader is null || tile.Cover is null)
        {
            return;
        }

        _ = LoadCoverAsync(image, args.ItemContainer, tile);
    }

    private async Task LoadCoverAsync(Image image, SelectorItem container, RomTile tile)
    {
        try
        {
            ImageSource? source = await _coverLoader!.LoadCoverAsync(tile.Cover, CancellationToken.None);
            if (source is not null && container.Content is RomTile current && current.Id == tile.Id)
            {
                image.Source = source;
            }
        }
        catch
        {
            // decorative
        }
    }

    private async void OnNewList(object sender, RoutedEventArgs e)
    {
        if (_collections is null)
        {
            StatusText.Text = "Connect first.";
            return;
        }

        string name = (NameBox.Text ?? string.Empty).Trim();
        if (name.Length == 0)
        {
            StatusText.Text = "Enter a list name.";
            return;
        }

        try
        {
            await _collections.CreateAsync(name);
            await BindCollectionsAsync();
            StatusText.Text = $"Created '{name}'.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Create failed: {ex.Message}";
        }
    }

    private async void OnRename(object sender, RoutedEventArgs e)
    {
        if (CollectionsList.SelectedItem is LibraryCollection { Id: RecentsListId })
        {
            StatusText.Text = "Recents is managed automatically and cannot be renamed.";
            return;
        }

        if (_collections?.SelectedCollection is not { } selected)
        {
            StatusText.Text = "Select a list.";
            return;
        }

        if (selected.ReadOnly)
        {
            StatusText.Text = "That list is server-managed and cannot be renamed.";
            return;
        }

        string name = (NameBox.Text ?? string.Empty).Trim();
        if (name.Length == 0)
        {
            StatusText.Text = "Enter a new name.";
            return;
        }

        try
        {
            await _collections.RenameAsync(selected.Id, name);
            await BindCollectionsAsync();
            StatusText.Text = $"Renamed to '{name}'.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Rename failed: {ex.Message}";
        }
    }

    private async void OnDelete(object sender, RoutedEventArgs e)
    {
        if (CollectionsList.SelectedItem is LibraryCollection { Id: RecentsListId })
        {
            StatusText.Text = "Recents is managed automatically and cannot be deleted.";
            return;
        }

        if (_collections?.SelectedCollection is not { } selected)
        {
            StatusText.Text = "Select a list.";
            return;
        }

        if (selected.ReadOnly)
        {
            StatusText.Text = "That list is server-managed and cannot be deleted.";
            return;
        }

        try
        {
            await _collections.DeleteAsync(selected.Id);
            MembersList.ItemsSource = null;
            MembersGrid.ItemsSource = null;
            await BindCollectionsAsync();
            StatusText.Text = $"Deleted '{selected.Name}'.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Delete failed: {ex.Message}";
        }
    }

    private async void OnPageKeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        switch (e.Key)
        {
            case Windows.System.VirtualKey.GamepadA:
                e.Handled = true;
                await AttachAsync(autostart: false);
                break;
            case Windows.System.VirtualKey.GamepadY:
                e.Handled = true;
                await AttachAsync(autostart: true);
                break;
            case Windows.System.VirtualKey.GamepadB:
            case Windows.System.VirtualKey.Escape:
                e.Handled = true;
                OnBack(sender, e);
                break;
        }
    }

    private void OnBack(object sender, RoutedEventArgs e)
    {
        App.Instance.Navigation.GoBack();
        if (Frame?.CanGoBack == true)
        {
            Frame.GoBack();
        }
    }

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
}
#endif
