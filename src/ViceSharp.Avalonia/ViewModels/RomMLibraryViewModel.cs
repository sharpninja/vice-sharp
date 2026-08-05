using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using RomM.Client;
using RomM.Client.Auth;
using ViceSharp.Library.ViewModels;
using ViceSharp.Protocol;
using ViceSharp.RomM;

namespace ViceSharp.Avalonia.ViewModels;

/// <summary>
/// PLAN-ROMM-001 (FR-ROMM-AVUI-001). Desktop RomM host: auto-connect (remembered server first),
/// shared <see cref="LibraryBrowseViewModel"/> with Recents, collections, and CSDb.
/// </summary>
public sealed class RomMLibraryViewModel : INotifyPropertyChanged
{
    private readonly IGameLaunchTarget _shell;
    private readonly string _cacheDir;
    private readonly IRomMConnectionStore _connectionStore;
    private readonly IRecentsStore _recentsStore;

    private string _baseUrl = "http://localhost:8080/";
    private string _bridgeUrl = "http://localhost:8090/";
    private string _token = string.Empty;
    private string _status = "Looking for a remembered RomM server...";
    private bool _isBusy;
    private bool _autoConnectTried;
    private LibraryBrowseViewModel? _browse;
    private CollectionsViewModel? _collections;
    private CsdbDiscoveryViewModel? _csdb;
    private IRomMLibraryGateway? _gateway;
    private IRomMCollectionsGateway? _collectionsGateway;
    private RomDetailViewModel? _selectedDetail;
    private IReadOnlyList<RecentGame> _recentGames = Array.Empty<RecentGame>();
    private IReadOnlyList<RomTile> _listMemberTiles = Array.Empty<RomTile>();
    private RomTile? _selectedListTile;

    /// <summary>Creates the host.</summary>
    public RomMLibraryViewModel(IGameLaunchTarget shell, string cacheDir)
    {
        _shell = shell ?? throw new ArgumentNullException(nameof(shell));
        _cacheDir = cacheDir ?? throw new ArgumentNullException(nameof(cacheDir));
        Directory.CreateDirectory(_cacheDir);
        string stateDir = Path.GetDirectoryName(_cacheDir) ?? _cacheDir;
        _connectionStore = new FileRomMConnectionStore(Path.Combine(stateDir, "romm-connection.json"));
        _recentsStore = new FileRecentsStore(Path.Combine(stateDir, "romm-recents.json"));
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    public string BaseUrl
    {
        get => _baseUrl;
        set => SetProperty(ref _baseUrl, value);
    }

    public string BridgeUrl
    {
        get => _bridgeUrl;
        set => SetProperty(ref _bridgeUrl, value);
    }

    public string Token
    {
        get => _token;
        set => SetProperty(ref _token, value);
    }

    public CollectionsViewModel? Collections
    {
        get => _collections;
        private set => SetProperty(ref _collections, value);
    }

    public CsdbDiscoveryViewModel? Csdb
    {
        get => _csdb;
        private set => SetProperty(ref _csdb, value);
    }

    public RomDetailViewModel? SelectedDetail
    {
        get => _selectedDetail;
        private set => SetProperty(ref _selectedDetail, value);
    }

    public string Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    public LibraryBrowseViewModel? Browse
    {
        get => _browse;
        private set
        {
            if (SetProperty(ref _browse, value))
            {
                OnPropertyChanged(nameof(IsConnected));
                OnPropertyChanged(nameof(IsShowingRecents));
            }
        }
    }

    public bool IsConnected => _browse is not null;

    public bool IsShowingRecents => _browse?.IsShowingRecents == true;

    public ObservableCollection<DiscoveredRomM> DiscoveredServers { get; } = new();

    /// <summary>Local Recents (newest first), refreshed on connect and after attach.</summary>
    public IReadOnlyList<RecentGame> RecentGames
    {
        get => _recentGames;
        private set => SetProperty(ref _recentGames, value);
    }

    /// <summary>Titles currently shown for the selected list on the Lists tab.</summary>
    public IReadOnlyList<RomTile> ListMemberTiles
    {
        get => _listMemberTiles;
        private set => SetProperty(ref _listMemberTiles, value);
    }

    /// <summary>Selected title on the Lists tab for attach.</summary>
    public RomTile? SelectedListTile
    {
        get => _selectedListTile;
        set => SetProperty(ref _selectedListTile, value);
    }

    /// <summary>
    /// Prefer remembered server; only scan when offline. Safe to call multiple times (once-only gate).
    /// </summary>
    public async Task TryAutoConnectAsync(CancellationToken cancellationToken = default)
    {
        if (_autoConnectTried || IsConnected || IsBusy)
        {
            return;
        }

        _autoConnectTried = true;
        await AutoConnectCoreAsync(forceScan: false, cancellationToken).ConfigureAwait(true);
    }

    /// <summary>Force a LAN scan (Scan LAN button).</summary>
    public async Task ScanAsync(CancellationToken cancellationToken = default)
    {
        if (IsBusy)
        {
            return;
        }

        await AutoConnectCoreAsync(forceScan: true, cancellationToken).ConfigureAwait(true);
    }

    private async Task AutoConnectCoreAsync(bool forceScan, CancellationToken cancellationToken)
    {
        IsBusy = true;
        DiscoveredServers.Clear();
        try
        {
            Uri? baseUrl = null;
            RomMConnection? saved = null;

            if (!forceScan)
            {
                Status = "Looking for a remembered RomM server...";
                var locator = new RomMServerLocator(_connectionStore, new RomMHeartbeatProbe(), new RomMSubnetDiscovery());
                RomMLocateResult located = await locator.LocateAsync(cancellationToken: cancellationToken).ConfigureAwait(true);
                Status = located.StatusMessage;
                baseUrl = located.BaseUrl;
                saved = located.SavedConnection;
                if (located.ScannedNetwork && baseUrl is not null)
                {
                    DiscoveredServers.Add(new DiscoveredRomM(baseUrl, null, null));
                }
            }
            else
            {
                Status = "Scanning the local network for RomM servers...";
                IReadOnlyList<DiscoveredRomM> servers = await new RomMSubnetDiscovery()
                    .ScanAsync(cancellationToken: cancellationToken)
                    .ConfigureAwait(true);
                foreach (DiscoveredRomM server in servers)
                {
                    DiscoveredServers.Add(server);
                }

                if (servers.Count == 0)
                {
                    Status = "No RomM servers found on the local network. Enter a URL manually.";
                    return;
                }

                baseUrl = servers[0].BaseUrl;
                Status = $"Found {baseUrl}.";
            }

            if (baseUrl is null)
            {
                return;
            }

            BaseUrl = baseUrl.ToString();

            if (saved is { Token.Length: > 0 })
            {
                Token = saved.Token;
                Status = $"Reconnecting to {baseUrl}...";
                if (await TryConnectInternalAsync(baseUrl.ToString(), saved.Token, saved.AuthMode, cancellationToken)
                        .ConfigureAwait(true))
                {
                    return;
                }

                Status = "Saved token failed; trying the bridge...";
            }

            var bridgeUrl = new UriBuilder(baseUrl) { Port = 8090, Path = "/" }.Uri;
            RomMConnection? bridge = await new RomMBridgeConnectionSource()
                .FetchAsync(bridgeUrl, Environment.UserName, cancellationToken)
                .ConfigureAwait(true);
            if (bridge is not null)
            {
                Token = bridge.Token;
                Status = $"Signing in via the bridge as {Environment.UserName}...";
                if (await TryConnectInternalAsync(baseUrl.ToString(), bridge.Token, RomMAuthMode.SubnetShared, cancellationToken)
                        .ConfigureAwait(true))
                {
                    return;
                }
            }

            Status = $"Found {baseUrl}. Enter a Client API token and Connect.";
        }
        catch (Exception ex)
        {
            Status = $"Auto-connect failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void SelectDiscovered(DiscoveredRomM server)
    {
        ArgumentNullException.ThrowIfNull(server);
        BaseUrl = server.BaseUrl.ToString();
        Status = $"Selected {server.BaseUrl}. Click Connect.";
    }

    /// <summary>Manual Connect from the UI.</summary>
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            await TryConnectInternalAsync(BaseUrl, Token, RomMAuthMode.ClientToken, cancellationToken)
                .ConfigureAwait(true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task<bool> TryConnectInternalAsync(
        string serverUrl,
        string? token,
        RomMAuthMode authMode,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!Uri.TryCreate(serverUrl, UriKind.Absolute, out Uri? uri))
            {
                Status = "Invalid server URL.";
                return false;
            }

            var options = new RomMClientOptions { BaseAddress = uri };
            if (!string.IsNullOrWhiteSpace(token))
            {
                options.Auth = RomMAuth.ClientApiToken(token.Trim());
            }

            IRomMClient client = RomMClient.Create(options);
            var gateway = new RomMLibraryGateway(client);
            _gateway = gateway;
            var collectionsGateway = new RomMCollectionsGateway(client);
            _collectionsGateway = collectionsGateway;
            var machine = new FixedMachineProvider(LibraryMachine.C64);
            var launcher = new AvaloniaGameLauncher(_shell);
            var browse = new LibraryBrowseViewModel(
                gateway, launcher, machine, _cacheDir, recents: _recentsStore);

            await browse.InitializeAsync(cancellationToken).ConfigureAwait(true);

            Browse = browse;
            Token = token ?? string.Empty;
            BaseUrl = uri.ToString();
            Status = $"Connected: {browse.Total} C64 titles.";
            OnPropertyChanged(nameof(IsShowingRecents));

            try
            {
                await _connectionStore
                    .SaveAsync(
                        new RomMConnection(
                            uri.ToString().TrimEnd('/') + "/",
                            authMode,
                            token ?? string.Empty),
                        cancellationToken)
                    .ConfigureAwait(true);
            }
            catch
            {
                // best-effort
            }

            RecentGames = await _recentsStore.LoadAsync(cancellationToken).ConfigureAwait(true);

            try
            {
                var collections = new CollectionsViewModel(collectionsGateway);
                await collections.RefreshAsync(includeSmartVirtual: true, cancellationToken).ConfigureAwait(true);
                Collections = collections;
            }
            catch (Exception ex)
            {
                Status = $"Connected; collections unavailable ({ex.Message}).";
            }

            if (Uri.TryCreate(BridgeUrl, UriKind.Absolute, out Uri? bridgeUri))
            {
                var bridgeHttp = new System.Net.Http.HttpClient { BaseAddress = bridgeUri };
                Csdb = new CsdbDiscoveryViewModel(new BridgeCsdbGateway(bridgeHttp, client.Tasks));
            }

            return true;
        }
        catch (Exception ex)
        {
            Status = $"Connect failed: {ex.Message}";
            return false;
        }
    }

    public async Task ShowDetailAsync(int romId, CancellationToken cancellationToken = default)
    {
        if (_gateway is null || _collectionsGateway is null)
        {
            return;
        }

        try
        {
            RomDetail detail = await _gateway.GetRomAsync(romId, cancellationToken).ConfigureAwait(true);
            SelectedDetail = new RomDetailViewModel(detail, _collectionsGateway);
        }
        catch (Exception ex)
        {
            Status = $"Details unavailable: {ex.Message}";
        }
    }

    public void Search(string term)
    {
        if (Browse is not null && !Browse.IsShowingRecents)
        {
            Browse.SearchText = term;
        }
    }

    public Task LoadMoreAsync(CancellationToken cancellationToken = default) =>
        Browse?.LoadMoreAsync(cancellationToken) ?? Task.CompletedTask;

    public Task JumpToLetterAsync(char letter, CancellationToken cancellationToken = default) =>
        Browse?.JumpToLetterAsync(letter, cancellationToken) ?? Task.CompletedTask;

    public async Task ToggleRecentsAsync(CancellationToken cancellationToken = default)
    {
        if (Browse is null)
        {
            return;
        }

        if (Browse.IsShowingRecents)
        {
            await Browse.ReloadAsync(cancellationToken).ConfigureAwait(true);
            Status = $"Library: {Browse.Total} titles.";
        }
        else
        {
            await Browse.ShowRecentsAsync(cancellationToken).ConfigureAwait(true);
            Status = Browse.StatusMessage;
            RecentGames = await _recentsStore.LoadAsync(cancellationToken).ConfigureAwait(true);
        }

        OnPropertyChanged(nameof(IsShowingRecents));
    }

    public async Task<LaunchOutcome> AttachAsync(bool autostart, CancellationToken cancellationToken = default)
    {
        if (Browse is null)
        {
            return new LaunchOutcome(false, "Not connected.");
        }

        LaunchOutcome outcome = await Browse.AttachAsync(autostart, cancellationToken).ConfigureAwait(true);
        Status = Browse.StatusMessage;
        if (outcome.Success)
        {
            RecentGames = await _recentsStore.LoadAsync(cancellationToken).ConfigureAwait(true);
        }

        return outcome;
    }

    /// <summary>Loads members for a collection (or Recents) into <see cref="ListMemberTiles"/>.</summary>
    public async Task LoadListMembersAsync(LibraryCollection? collection, CancellationToken cancellationToken = default)
    {
        SelectedListTile = null;
        if (collection is null)
        {
            ListMemberTiles = Array.Empty<RomTile>();
            return;
        }

        if (collection.Id == -1)
        {
            RecentGames = await _recentsStore.LoadAsync(cancellationToken).ConfigureAwait(true);
            ListMemberTiles = RecentGames.Select(g => g.ToTile()).ToList();
            Status = $"Recents: {ListMemberTiles.Count} game(s).";
            return;
        }

        if (_gateway is null)
        {
            ListMemberTiles = Array.Empty<RomTile>();
            return;
        }

        Status = "Loading list titles...";
        var tiles = new List<RomTile>();
        foreach (int romId in collection.RomIds)
        {
            try
            {
                RomDetail detail = await _gateway.GetRomAsync(romId, cancellationToken).ConfigureAwait(true);
                RomFile? file = detail.Files.FirstOrDefault(f => f.Launchable) ?? detail.Files.FirstOrDefault();
                string fileName = file?.FileName ?? detail.Name;
                tiles.Add(new RomTile(
                    detail.Id,
                    detail.Name,
                    fileName,
                    detail.PlatformSlug,
                    file is { SizeBytes: > 0 } ? file.SizeBytes : null,
                    detail.Cover,
                    file?.Launchable ?? MediaExtensionMap.IsLaunchable(fileName)));
            }
            catch
            {
                tiles.Add(new RomTile(romId, $"Rom #{romId}", string.Empty, null, null, null, false));
            }
        }

        ListMemberTiles = tiles;
        Status = $"{collection.Name}: {tiles.Count} title(s).";
    }

    /// <summary>Attach from the Lists tab selection (uses download cache when present).</summary>
    public async Task<LaunchOutcome> AttachListSelectionAsync(bool autostart, CancellationToken cancellationToken = default)
    {
        if (_gateway is null || SelectedListTile is not { } tile || !tile.Launchable)
        {
            return new LaunchOutcome(false, "Select a launchable title first.");
        }

        try
        {
            Status = "Downloading...";
            AcquiredGame game = await _gateway
                .DownloadAsync(tile.Id, tile.FileName, tile.SizeBytes ?? 0, _cacheDir, null, cancellationToken)
                .ConfigureAwait(true);
            Status = "Starting...";
            var launcher = new AvaloniaGameLauncher(_shell);
            MediaSlot slot = MediaExtensionMap.Resolve(tile.FileName)?.Slot ?? MediaSlot.Drive8;
            LaunchOutcome outcome = await launcher.LaunchAsync(game, slot, autostart, cancellationToken).ConfigureAwait(true);
            Status = outcome.Message;
            if (outcome.Success)
            {
                await _recentsStore.RecordAsync(RecentGame.FromTile(tile), cancellationToken: cancellationToken)
                    .ConfigureAwait(true);
                RecentGames = await _recentsStore.LoadAsync(cancellationToken).ConfigureAwait(true);
            }

            return outcome;
        }
        catch (Exception ex)
        {
            Status = $"Attach failed: {ex.Message}";
            return new LaunchOutcome(false, Status);
        }
    }

    /// <summary>Collections rail including a synthetic Recents row when non-empty.</summary>
    public async Task<IReadOnlyList<LibraryCollection>> GetListsRailAsync(CancellationToken cancellationToken = default)
    {
        // Load first, then assign only when the sequence of ids changed. Assigning a fresh list
        // instance every call raised RecentGames and used to re-enter ListsView.RefreshRailAsync.
        IReadOnlyList<RecentGame> loaded = await _recentsStore.LoadAsync(cancellationToken).ConfigureAwait(true);
        if (!SameRecentIds(_recentGames, loaded))
        {
            RecentGames = loaded;
        }

        var rows = new List<LibraryCollection>();
        if (_recentGames.Count > 0)
        {
            rows.Add(new LibraryCollection(
                -1,
                "Recents",
                _recentGames.Count,
                ReadOnly: true,
                _recentGames.Select(g => g.Id).ToList()));
        }

        if (Collections is not null)
        {
            rows.AddRange(Collections.Collections);
        }

        return rows;
    }

    private static bool SameRecentIds(IReadOnlyList<RecentGame> a, IReadOnlyList<RecentGame> b)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }

        if (a.Count != b.Count)
        {
            return false;
        }

        for (var i = 0; i < a.Count; i++)
        {
            if (a[i].Id != b[i].Id)
            {
                return false;
            }
        }

        return true;
    }

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

/// <summary>Fixed machine provider for the desktop library (C64).</summary>
internal sealed class FixedMachineProvider : ICurrentMachineProvider
{
    private readonly LibraryMachine _machine;

    public FixedMachineProvider(LibraryMachine machine) => _machine = machine;

    public string GetActivePlatformSlug() => MachinePlatformSlug.ToSlug(_machine);

    public event EventHandler? PlatformChanged
    {
        add { }
        remove { }
    }
}
