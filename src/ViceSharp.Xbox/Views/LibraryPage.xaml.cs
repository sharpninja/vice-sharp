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
/// docs/wireframes/romm-xbox-library.svg): auto-connects on open (remembered server first; LAN scan
/// only when that server is offline; csdb-bridge for zero-touch auth), then browses the C64 library
/// as a cover-tile grid with search, an A-Z jump strip, a
/// selected-game bar (slot + download progress) and the A/Y/X/B action bar. Builds a
/// <see cref="LibraryBrowseViewModel"/> over a <see cref="RomMLibraryGateway"/>, the head's
/// <see cref="XboxGameLauncher"/>, and an <see cref="XboxCoverImageLoader"/> for the covers.
/// </summary>
public sealed partial class LibraryPage : Page
{
    private LibraryBrowseViewModel? _browse;
    private XboxCoverImageLoader? _coverLoader;
    private IRecentsStore? _recents;
    private string? _serverUrl;
    private string? _token;
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

    // PLAN-ROMM-001 (AC-CONN-05/07): prefer the last remembered RomM server; only scan the LAN when
    // nothing is saved or that server no longer answers heartbeat. Zero-touch sign-in still uses the
    // co-located csdb-bridge when a token is not already stored.
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

            // Prefer a stored token when the remembered server is still up.
            if (saved is { Token.Length: > 0 })
            {
                ConnectStatus.Text = $"Reconnecting to {baseUrl}...";
                if (await TryConnectWithAsync(baseUrl.ToString(), saved.Token, saved.AuthMode))
                {
                    return;
                }

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
            }

            ConnectStatus.Text = $"Found {baseUrl}. Enter a Client API token and Connect.";
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

    // Builds the browse VM + cover loader; on success persists the connection so the next open can
    // skip the LAN scan when this server is still reachable.
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
            var gateway = new RomMLibraryGateway(client);
            IGameLauncher launcher = App.Instance.CreateRomMGameLauncher();
            string cacheDir = System.IO.Path.Combine(ApplicationData.Current.LocalFolder.Path, "romm-cache");
            _recents = new FileRecentsStore(
                System.IO.Path.Combine(ApplicationData.Current.LocalFolder.Path, "romm-recents.json"));

            var browse = new LibraryBrowseViewModel(
                gateway, launcher, new C64MachineProvider(), cacheDir, recents: _recents);
            browse.PropertyChanged += OnBrowsePropertyChanged;
            await browse.InitializeAsync();

            _browse = browse;
            _serverUrl = uri.ToString();
            _token = token;
            _coverLoader = new XboxCoverImageLoader(uri, token);

            TilesView.ItemsSource = browse.Items;
            ConnectPanel.Visibility = Visibility.Collapsed;
            BrowseArea.Visibility = Visibility.Visible;
            UpdateBrowseChrome();
            TilesView.Focus(FocusState.Programmatic);

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

    private void OnBrowsePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(LibraryBrowseViewModel.Total):
            case nameof(LibraryBrowseViewModel.IsShowingRecents):
                UpdateBrowseChrome();
                break;
            case nameof(LibraryBrowseViewModel.StatusMessage):
                StatusText.Text = _browse?.StatusMessage ?? string.Empty;
                break;
            case nameof(LibraryBrowseViewModel.DownloadProgress):
                DownloadBar.Value = _browse?.DownloadProgress ?? 0.0;
                break;
        }
    }

    private void UpdateBrowseChrome()
    {
        if (_browse is null)
        {
            CountText.Text = string.Empty;
            return;
        }

        if (_browse.IsShowingRecents)
        {
            TitleText.Text = "Recents";
            CountText.Text = $"{_browse.Total:N0} recent";
            RecentsButton.Content = "All games";
            SearchBox.IsEnabled = false;
            AzScroll.Visibility = Visibility.Collapsed;
        }
        else
        {
            TitleText.Text = "RomM library";
            CountText.Text = $"{_browse.Total:N0} {ActiveMachineLabel()} titles";
            RecentsButton.Content = "Recents";
            SearchBox.IsEnabled = true;
            AzScroll.Visibility = Visibility.Visible;
        }
    }

    private async void OnRecentsToggle(object sender, RoutedEventArgs e)
    {
        if (_browse is null)
        {
            return;
        }

        if (_browse.IsShowingRecents)
        {
            await _browse.ReloadAsync();
        }
        else
        {
            await _browse.ShowRecentsAsync();
        }

        UpdateBrowseChrome();
        StatusText.Text = _browse.StatusMessage;
    }

    private void OnSearchChanged(object sender, TextChangedEventArgs e)
    {
        if (_browse is not null && !_browse.IsShowingRecents)
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

        if (TilesView.SelectedItem is GameGroup group)
        {
            _browse.SelectedGroup = group;
            SelectedText.Text = group.HasMultipleVariants
                ? $"Selected  {group.Name} · {group.VariantCount} variants"
                : $"Selected  {group.Name} · {group.Primary.FileName}";
            SyncSlotToSelection();
            return;
        }

        _browse.SelectedGroup = null;
        SelectedText.Text = "No game selected";
    }

    private void OnTileClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is not GameGroup group || _browse is null)
        {
            return;
        }

        TilesView.SelectedItem = group;
        _browse.SelectedGroup = group;
        // Multi-variant games open the detail picker; single-variant attaches immediately.
        if (group.HasMultipleVariants)
        {
            OpenDetails(group);
            return;
        }

        _ = AttachAsync(autostart: false);
    }

    private async void OnPageKeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        switch (e.Key)
        {
            case Windows.System.VirtualKey.GamepadA:
                e.Handled = true;
                await HandlePrimaryActionAsync(autostart: false);
                break;
            case Windows.System.VirtualKey.GamepadY:
                e.Handled = true;
                await HandlePrimaryActionAsync(autostart: true);
                break;
            case Windows.System.VirtualKey.GamepadX:
                e.Handled = true;
                OnAddToList();
                break;
            case Windows.System.VirtualKey.GamepadB:
            case Windows.System.VirtualKey.Escape:
                e.Handled = true;
                OnBack();
                break;
        }
    }

    /// <summary>
    /// A/Y and the action-bar buttons: open the variant picker when the selection has multiple
    /// ROMs; otherwise attach the single variant (optionally with autostart).
    /// </summary>
    private async Task HandlePrimaryActionAsync(bool autostart)
    {
        if (_browse?.SelectedGroup is { HasMultipleVariants: true } group)
        {
            OpenDetails(group);
            return;
        }

        await AttachAsync(autostart);
    }

    private async void OnAttachClick(object sender, RoutedEventArgs e) =>
        await HandlePrimaryActionAsync(autostart: false);

    private async void OnAttachAutostartClick(object sender, RoutedEventArgs e) =>
        await HandlePrimaryActionAsync(autostart: true);

    private void OnAddToListClick(object sender, RoutedEventArgs e) => OnAddToList();

    private void OnBackClick(object sender, RoutedEventArgs e) => OnBack();

    private void OnAddToList()
    {
        if (_browse?.SelectedGroup is { } group)
        {
            OpenDetails(group);
            return;
        }

        StatusText.Text = "Select a game first.";
    }

    private void OpenDetails(GameGroup group)
    {
        if (string.IsNullOrWhiteSpace(_serverUrl))
        {
            StatusText.Text = "Not connected.";
            return;
        }

        var request = new GameDetailsRequest(
            _serverUrl,
            _token,
            group.Name,
            group.Variants.ToList());

        if (Frame is not null)
        {
            Frame.Navigate(typeof(GameDetailsPage), request);
            return;
        }

        StatusText.Text = "Cannot open details (no frame).";
    }

    private void OnBack()
    {
        App.Instance.Navigation.GoBack();
        if (Frame?.CanGoBack == true)
            Frame.GoBack();
    }

    // Lazily loads each realized tile's cover art (virtualization-friendly), guarding against the
    // container being recycled to a different group before the fetch completes.
    private void OnTileContainerChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
    {
        if (args.InRecycleQueue || args.Item is not GameGroup group)
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

        if (args.ItemContainer.ContentTemplateRoot is not FrameworkElement root)
        {
            return;
        }

        if (root.FindName("VariantBadge") is Border badge && root.FindName("VariantBadgeText") is TextBlock badgeText)
        {
            if (group.HasMultipleVariants)
            {
                badge.Visibility = Visibility.Visible;
                badgeText.Text = group.VariantCount.ToString();
            }
            else
            {
                badge.Visibility = Visibility.Collapsed;
            }
        }

        if (root.FindName("CoverImage") is not Image image)
        {
            return;
        }

        image.Source = null;
        CoverRef? cover = group.Cover;
        if (_coverLoader is null || cover is null)
        {
            return;
        }

        _ = LoadCoverAsync(image, args.ItemContainer, group, cover);
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

    private async Task LoadCoverAsync(Image image, SelectorItem container, GameGroup group, CoverRef cover)
    {
        try
        {
            var source = await _coverLoader!.LoadCoverAsync(cover, CancellationToken.None);
            if (source is not null
                && container.Content is GameGroup current
                && string.Equals(current.Name, group.Name, StringComparison.OrdinalIgnoreCase)
                && current.Primary.Id == group.Primary.Id)
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

        LaunchOutcome outcome = await _browse.AttachAsync(autostart);
        StatusText.Text = _browse.StatusMessage;

        // Autostart hands control to the running C64: leave the library and resume the emulator.
        if (autostart && outcome.Success)
        {
            App.Instance.DismissMenu();
        }
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
                Height = 32,
                Margin = new Thickness(0, 1, 0, 1),
                Padding = new Thickness(0),
                FontSize = 15,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                // Gamepad focus scrolls the letter into view inside AzScroll.
                UseSystemFocusVisuals = true,
            };
            button.Click += async (_, _) =>
            {
                if (_browse is not null)
                {
                    await _browse.JumpToLetterAsync(target);
                    // Keep the pressed letter visible after the grid reloads.
                    button.StartBringIntoView();
                }
            };
            button.GotFocus += (_, _) => button.StartBringIntoView();
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
