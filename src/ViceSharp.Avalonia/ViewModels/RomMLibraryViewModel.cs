using System.ComponentModel;
using System.Runtime.CompilerServices;
using RomM.Client;
using RomM.Client.Auth;
using ViceSharp.Library.ViewModels;
using ViceSharp.RomM;

namespace ViceSharp.Avalonia.ViewModels;

/// <summary>
/// PLAN-ROMM-001 (FR-ROMM-AVUI-001). The desktop head's RomM library host: it collects the connection
/// details, builds the <see cref="RomMLibraryGateway"/> over a <see cref="RomMClient"/> on Connect, and
/// exposes a wired <see cref="LibraryBrowseViewModel"/> (over an <see cref="AvaloniaGameLauncher"/>) that
/// the <c>LibraryView</c> binds to. On-device browse/attach/launch is the [V] E2E step.
/// </summary>
public sealed class RomMLibraryViewModel : INotifyPropertyChanged
{
    private readonly IGameLaunchTarget _shell;
    private readonly string _cacheDir;

    private string _baseUrl = "http://localhost:8080/";
    private string _bridgeUrl = "http://localhost:8090/";
    private string _token = string.Empty;
    private string _status = "Enter your RomM server URL and token, then Connect.";
    private bool _isBusy;
    private LibraryBrowseViewModel? _browse;
    private CollectionsViewModel? _collections;
    private CsdbDiscoveryViewModel? _csdb;
    private IRomMLibraryGateway? _gateway;
    private IRomMCollectionsGateway? _collectionsGateway;
    private RomDetailViewModel? _selectedDetail;

    /// <summary>Creates the host.</summary>
    /// <param name="shell">The shell launch surface (for the launcher).</param>
    /// <param name="cacheDir">The local download cache root.</param>
    public RomMLibraryViewModel(IGameLaunchTarget shell, string cacheDir)
    {
        _shell = shell ?? throw new ArgumentNullException(nameof(shell));
        _cacheDir = cacheDir ?? throw new ArgumentNullException(nameof(cacheDir));
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>The RomM server base URL.</summary>
    public string BaseUrl
    {
        get => _baseUrl;
        set => SetProperty(ref _baseUrl, value);
    }

    /// <summary>The csdb-bridge base URL (for the CSDb discovery tab).</summary>
    public string BridgeUrl
    {
        get => _bridgeUrl;
        set => SetProperty(ref _bridgeUrl, value);
    }

    /// <summary>The client API token.</summary>
    public string Token
    {
        get => _token;
        set => SetProperty(ref _token, value);
    }

    /// <summary>The collections (lists) view-model, or <c>null</c> before Connect.</summary>
    public CollectionsViewModel? Collections
    {
        get => _collections;
        private set => SetProperty(ref _collections, value);
    }

    /// <summary>The CSDb discovery view-model, or <c>null</c> before Connect.</summary>
    public CsdbDiscoveryViewModel? Csdb
    {
        get => _csdb;
        private set => SetProperty(ref _csdb, value);
    }

    /// <summary>
    /// AC-AUI-03. The selected title's detail view-model (cover/metadata/files/add-to-list), or
    /// <c>null</c> until a tile is opened.
    /// </summary>
    public RomDetailViewModel? SelectedDetail
    {
        get => _selectedDetail;
        private set => SetProperty(ref _selectedDetail, value);
    }

    /// <summary>A human-readable status line.</summary>
    public string Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
    }

    /// <summary>Whether a connect/search is in flight.</summary>
    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    /// <summary>The wired browser, or <c>null</c> before Connect.</summary>
    public LibraryBrowseViewModel? Browse
    {
        get => _browse;
        private set
        {
            if (SetProperty(ref _browse, value))
            {
                OnPropertyChanged(nameof(IsConnected));
            }
        }
    }

    /// <summary>Whether the library is connected.</summary>
    public bool IsConnected => _browse is not null;

    /// <summary>Builds the gateway/browser and loads the first page, scoped to C64.</summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out Uri? uri))
            {
                Status = "Invalid server URL.";
                return;
            }

            var options = new RomMClientOptions { BaseAddress = uri };
            if (!string.IsNullOrWhiteSpace(Token))
            {
                options.Auth = RomMAuth.ClientApiToken(Token.Trim());
            }

            IRomMClient client = RomMClient.Create(options);
            var gateway = new RomMLibraryGateway(client);
            _gateway = gateway;
            var collectionsGateway = new RomMCollectionsGateway(client);
            _collectionsGateway = collectionsGateway;
            var machine = new FixedMachineProvider(LibraryMachine.C64);
            var launcher = new AvaloniaGameLauncher(_shell);
            var browse = new LibraryBrowseViewModel(gateway, launcher, machine, _cacheDir);

            await browse.InitializeAsync(cancellationToken).ConfigureAwait(true);

            Browse = browse;
            Status = $"Connected: {browse.Total} C64 titles.";

            // Lists (collections). A failure here must not tear down the working library connection.
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

            // CSDb discovery via the bridge sidecar (optional; absent bridge just leaves the tab idle).
            if (Uri.TryCreate(BridgeUrl, UriKind.Absolute, out Uri? bridgeUri))
            {
                var bridgeHttp = new System.Net.Http.HttpClient { BaseAddress = bridgeUri };
                Csdb = new CsdbDiscoveryViewModel(new BridgeCsdbGateway(bridgeHttp, client.Tasks));
            }
        }
        catch (Exception ex)
        {
            Status = $"Connect failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// AC-AUI-03. Fetches the given ROM's detail and publishes it as <see cref="SelectedDetail"/> for the
    /// details pane. A failure surfaces on the status line and leaves the previous detail in place.
    /// </summary>
    /// <param name="romId">The ROM id to open.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
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

    /// <summary>Sets the (debounced) search term.</summary>
    /// <param name="term">The search text.</param>
    public void Search(string term)
    {
        if (Browse is not null)
        {
            Browse.SearchText = term;
        }
    }

    /// <summary>Loads the next page.</summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    public Task LoadMoreAsync(CancellationToken cancellationToken = default) =>
        Browse?.LoadMoreAsync(cancellationToken) ?? Task.CompletedTask;

    /// <summary>Attaches the selection (optionally booting it).</summary>
    /// <param name="autostart">Whether to boot after attaching.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public Task AttachAsync(bool autostart, CancellationToken cancellationToken = default) =>
        Browse?.AttachAsync(autostart, cancellationToken) ?? Task.CompletedTask;

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

/// <summary>
/// PLAN-ROMM-001 (AC-BROWSE-02). A fixed <see cref="ICurrentMachineProvider"/> for the desktop head's
/// current machine (the library is scoped to it; the desktop machine does not change mid-session here).
/// </summary>
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
