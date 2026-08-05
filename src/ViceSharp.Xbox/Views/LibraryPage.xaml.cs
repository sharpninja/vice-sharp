// PLAN-ROMM-001 X3 (IMPL-ROMM-012): RomM library page code-behind. #if HAS_UWP-guarded in full.
#if HAS_UWP
namespace ViceSharp.Xbox.Views;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Navigation;
using global::RomM.Client;
using global::RomM.Client.Auth;
using ViceSharp.Library.ViewModels;
using ViceSharp.Protocol;
using ViceSharp.RomM;
using ViceSharp.Xbox.RomM;

/// <summary>
/// PLAN-ROMM-001 (AC-XUI-02). The RomM library page (Game-Pass style, per
/// docs/wireframes/romm-xbox-library.svg): auto-connects on open (scan + csdb-bridge as the Xbox
/// user), then browses the C64 library as a cover-tile grid with search, an A-Z jump strip, a
/// selected-game bar (slot + download progress) and the A/Y/X/B action bar. Builds a
/// <see cref="LibraryBrowseViewModel"/> over a <see cref="RomMLibraryGateway"/>, the head's
/// <see cref="XboxGameLauncher"/>, and an <see cref="XboxCoverImageLoader"/> for the covers.
/// </summary>
public sealed partial class LibraryPage : Page
{
    private LibraryBrowseViewModel? _browse;
    private XboxCoverImageLoader? _coverLoader;
    private bool _autoConnectTried;
    private bool _loadingMore;

    /// <summary>Creates the page and builds the A-Z strip + slot picker.</summary>
    public LibraryPage()
    {
        InitializeComponent();
        BuildAzStrip();
        BuildSlotPicker();

        // Y/X are not part of the shell's menu input map, so the page owns them (Y = attach +
        // autostart, X = add to list). handledEventsToo so the focused tile does not swallow them.
        AddHandler(
            UIElement.KeyDownEvent,
            new Windows.UI.Xaml.Input.KeyEventHandler(OnPageKeyDown),
            handledEventsToo: true);
    }

    /// <summary>Auto-connects the first time the page is shown.</summary>
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (_autoConnectTried)
        {
            return;
        }

        _autoConnectTried = true;
        MachineText.Text = $"Machine · {ActiveMachineLabel()}";
        _ = AutoConnectAsync();
    }

    // PLAN-ROMM-001 (AC-CONN-07): on open, scan the LAN for RomM and sign in ZERO-TOUCH via the
    // co-located csdb-bridge as the current Xbox user. Leaves the connection panel up for manual
    // entry when nothing is reachable.
    private async Task AutoConnectAsync()
    {
        try
        {
            ConnectStatus.Text = "Scanning the local network for RomM...";
            IReadOnlyList<DiscoveredRomM> servers = await new RomMSubnetDiscovery().ScanAsync();
            if (servers.Count == 0)
            {
                ConnectStatus.Text = "No RomM servers found. Enter a URL and token, then Connect.";
                return;
            }

            DiscoveredRomM server = servers[0];
            UrlBox.Text = server.BaseUrl.ToString();

            var bridgeUrl = new UriBuilder(server.BaseUrl) { Port = 8090, Path = "/" }.Uri;
            string userId = await GetXboxUserIdAsync();
            RomMConnection? connection = await new RomMBridgeConnectionSource().FetchAsync(bridgeUrl, userId);
            if (connection is not null)
            {
                ConnectStatus.Text = "Signing in as this Xbox user via the bridge...";
                await ConnectWithAsync(server.BaseUrl.ToString(), connection.Token);
                return;
            }

            ConnectStatus.Text = $"Found {server.BaseUrl}. Enter a Client API token and Connect.";
        }
        catch (Exception ex)
        {
            ConnectStatus.Text = $"Auto-connect failed: {ex.Message}. Enter a URL + token and Connect.";
        }
    }

    private async void OnScan(object sender, RoutedEventArgs e) => await AutoConnectAsync();

    private async void OnConnect(object sender, RoutedEventArgs e)
    {
        string? token = string.IsNullOrWhiteSpace(TokenBox.Password) ? null : TokenBox.Password.Trim();
        await ConnectWithAsync(UrlBox.Text, token);
    }

    // Builds the browse VM + cover loader against the given server + optional bearer token, wires
    // the reactive fields, and swaps the connection panel for the browse grid on success.
    private async Task ConnectWithAsync(string serverUrl, string? token)
    {
        try
        {
            if (!Uri.TryCreate(serverUrl, UriKind.Absolute, out Uri? uri))
            {
                ConnectStatus.Text = "Invalid server URL.";
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

            var browse = new LibraryBrowseViewModel(gateway, launcher, new C64MachineProvider(), cacheDir);
            browse.PropertyChanged += OnBrowsePropertyChanged;
            await browse.InitializeAsync();

            _browse = browse;
            _coverLoader = new XboxCoverImageLoader(uri, token);

            TilesView.ItemsSource = browse.Items;
            ConnectPanel.Visibility = Visibility.Collapsed;
            BrowseArea.Visibility = Visibility.Visible;
            UpdateCount();
            TilesView.Focus(FocusState.Programmatic);
        }
        catch (Exception ex)
        {
            ConnectStatus.Text = $"Connect failed: {ex.Message}";
        }
    }

    private void OnBrowsePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(LibraryBrowseViewModel.Total):
                UpdateCount();
                break;
            case nameof(LibraryBrowseViewModel.StatusMessage):
                StatusText.Text = _browse?.StatusMessage ?? string.Empty;
                break;
            case nameof(LibraryBrowseViewModel.DownloadProgress):
                DownloadBar.Value = _browse?.DownloadProgress ?? 0.0;
                break;
        }
    }

    private void UpdateCount() => CountText.Text = _browse is null ? string.Empty : $"{_browse.Total:N0} {ActiveMachineLabel()} titles";

    private void OnSearchChanged(object sender, TextChangedEventArgs e)
    {
        if (_browse is not null)
        {
            _browse.SearchText = SearchBox.Text;
        }
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_browse is null)
        {
            return;
        }

        _browse.SelectedTile = TilesView.SelectedItem as RomTile;
        RomTile? tile = _browse.SelectedTile;
        SelectedText.Text = tile is null ? "No game selected" : $"Selected  {tile.Name} · {tile.FileName}";
        SyncSlotToSelection();
    }

    private async void OnTileClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is RomTile tile && _browse is not null)
        {
            TilesView.SelectedItem = tile;
            _browse.SelectedTile = tile;
            await AttachAsync(autostart: false);
        }
    }

    private async void OnPageKeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        switch (e.Key)
        {
            case Windows.System.VirtualKey.GamepadA:
                // A = attach selected tile (same as click-to-attach without autostart).
                e.Handled = true;
                await AttachAsync(autostart: false);
                break;
            case Windows.System.VirtualKey.GamepadY:
                e.Handled = true;
                await AttachAsync(autostart: true);
                break;
            case Windows.System.VirtualKey.GamepadX:
                e.Handled = true;
                StatusText.Text = "Add to list: open Lists from the shell menu.";
                break;
            case Windows.System.VirtualKey.GamepadB:
            case Windows.System.VirtualKey.Escape:
                e.Handled = true;
                OnBack();
                break;
        }
    }

    private void OnBack()
    {
        App.Instance.Navigation.GoBack();
        if (Frame?.CanGoBack == true)
            Frame.GoBack();
    }

    // Lazily loads each realized tile's cover art (virtualization-friendly), guarding against the
    // container being recycled to a different tile before the fetch completes.
    private void OnTileContainerChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
    {
        if (args.InRecycleQueue || args.Item is not RomTile tile)
        {
            return;
        }

        // Incremental paging: pull the next page as the tail of the current one realizes, so the
        // grid streams through thousands of titles without a "Load more" button.
        if (_browse is { HasMore: true } browse && !_loadingMore && args.ItemIndex >= browse.Items.Count - 12)
        {
            _loadingMore = true;
            _ = LoadMoreAsync();
        }

        if (args.ItemContainer.ContentTemplateRoot is not FrameworkElement root || root.FindName("CoverImage") is not Image image)
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

    private async Task LoadMoreAsync()
    {
        try
        {
            if (_browse is not null)
            {
                await _browse.LoadMoreAsync();
            }
        }
        finally
        {
            _loadingMore = false;
        }
    }

    private async Task LoadCoverAsync(Image image, SelectorItem container, RomTile tile)
    {
        try
        {
            var source = await _coverLoader!.LoadCoverAsync(tile.Cover, CancellationToken.None);
            if (source is not null && container.Content is RomTile current && current.Id == tile.Id)
            {
                image.Source = source;
            }
        }
        catch
        {
            // A cover is decorative: never let a load fault surface on the grid.
        }
    }

    private void OnSlotChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_browse is not null && SlotBox.SelectedItem is ComboBoxItem { Tag: MediaSlot slot })
        {
            _browse.SelectedSlot = slot;
        }
    }

    private async Task AttachAsync(bool autostart)
    {
        if (_browse is null)
        {
            return;
        }

        if (!_browse.CanAttach)
        {
            StatusText.Text = "Select a launchable title first.";
            return;
        }

        await _browse.AttachAsync(autostart);
        StatusText.Text = _browse.StatusMessage;
    }

    private void BuildAzStrip()
    {
        foreach (char letter in Enumerable.Range('A', 26).Select(c => (char)c).Append('#'))
        {
            char target = letter;
            var button = new Button
            {
                Content = target.ToString(),
                Width = 40,
                Height = 34,
                Margin = new Thickness(0, 1, 0, 1),
                Padding = new Thickness(0),
                FontSize = 16,
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };
            button.Click += async (_, _) =>
            {
                if (_browse is not null)
                {
                    await _browse.JumpToLetterAsync(target);
                }
            };
            AzStrip.Children.Add(button);
        }
    }

    private void BuildSlotPicker()
    {
        (MediaSlot Slot, string Label)[] slots =
        {
            (MediaSlot.Drive8, "Drive 8"),
            (MediaSlot.Drive9, "Drive 9"),
            (MediaSlot.Tape, "Tape"),
            (MediaSlot.Cartridge, "Cartridge"),
        };

        foreach ((MediaSlot slot, string label) in slots)
        {
            SlotBox.Items.Add(new ComboBoxItem { Content = label, Tag = slot });
        }

        SlotBox.SelectedIndex = 0;
    }

    private void SyncSlotToSelection()
    {
        if (_browse?.SelectedSlot is not { } slot)
        {
            return;
        }

        for (int i = 0; i < SlotBox.Items.Count; i++)
        {
            if (SlotBox.Items[i] is ComboBoxItem { Tag: MediaSlot itemSlot } && itemSlot == slot)
            {
                SlotBox.SelectedIndex = i;
                return;
            }
        }
    }

    private static string ActiveMachineLabel()
    {
        try
        {
            return new C64MachineProvider().GetActivePlatformSlug().ToUpperInvariant();
        }
        catch
        {
            return "C64";
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
}
#endif
