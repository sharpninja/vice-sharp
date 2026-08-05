using System.Collections.ObjectModel;
using System.Threading;
using ViceSharp.Protocol;

namespace ViceSharp.Library.ViewModels;

/// <summary>
/// FR-ROMM-BROWSE/LAUNCH-001. The source-agnostic library browser. It pages the RomM library scoped to
/// the active machine, supports A-Z jump and debounced search, re-scopes when the machine changes, and
/// downloads + launches the selected game with a two-phase status. All background PropertyChanged /
/// collection mutations dispatch to the captured UI context via <see cref="LibraryObservableObject"/>
/// (TR-ROMM-THREAD-001).
/// </summary>
public sealed class LibraryBrowseViewModel : LibraryObservableObject, IDisposable
{
    private static readonly TimeSpan DefaultDebounce = TimeSpan.FromMilliseconds(300);

    private readonly IRomMLibraryGateway _gateway;
    private readonly IGameLauncher _launcher;
    private readonly ICurrentMachineProvider _machine;
    private readonly string _cacheDir;
    private readonly int _pageSize;
    private readonly TimeSpan _debounce;

    private int _platformId;
    private int _offset;
    private int _total;
    private IReadOnlyDictionary<string, int> _charIndex = new Dictionary<string, int>();

    private string _searchText = string.Empty;
    private string _statusMessage = string.Empty;
    private double _downloadProgress;
    private RomTile? _selectedTile;
    private MediaSlot? _selectedSlot;
    private LibraryOrder _order = LibraryOrder.Name;

    private CancellationTokenSource? _searchCts;
    private Task? _pendingSearch;
    private Task? _pendingRescope;

    /// <summary>Creates the browser.</summary>
    /// <param name="gateway">The RomM library gateway.</param>
    /// <param name="launcher">The head's game launcher.</param>
    /// <param name="machine">The active-machine provider (drives the platform scope).</param>
    /// <param name="cacheDir">The local download cache root.</param>
    /// <param name="pageSize">The page size for paging (default 60).</param>
    /// <param name="debounce">The search debounce interval (default 300 ms).</param>
    public LibraryBrowseViewModel(
        IRomMLibraryGateway gateway,
        IGameLauncher launcher,
        ICurrentMachineProvider machine,
        string cacheDir,
        int pageSize = 60,
        TimeSpan? debounce = null)
    {
        _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
        _launcher = launcher ?? throw new ArgumentNullException(nameof(launcher));
        _machine = machine ?? throw new ArgumentNullException(nameof(machine));
        _cacheDir = cacheDir ?? throw new ArgumentNullException(nameof(cacheDir));
        _pageSize = pageSize > 0 ? pageSize : 60;
        _debounce = debounce ?? DefaultDebounce;

        _machine.PlatformChanged += OnPlatformChanged;
    }

    /// <summary>The tiles loaded so far (grows as pages load).</summary>
    public ObservableCollection<RomTile> Items { get; } = new();

    /// <summary>The total number of matching ROMs across all pages.</summary>
    public int Total
    {
        get => _total;
        private set
        {
            if (SetProperty(ref _total, value))
            {
                OnPropertyChanged(nameof(HasMore));
            }
        }
    }

    /// <summary>AC-BROWSE-04: whether more pages remain to load.</summary>
    public bool HasMore => Items.Count < _total;

    /// <summary>AC-BROWSE-06: the search text; setting it triggers a debounced reload.</summary>
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                _pendingSearch = DebouncedSearchAsync();
            }
        }
    }

    /// <summary>A short human-readable status for the current operation.</summary>
    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>The fractional download progress in [0, 1] for the current attach.</summary>
    public double DownloadProgress
    {
        get => _downloadProgress;
        private set => SetProperty(ref _downloadProgress, value);
    }

    /// <summary>The currently selected tile. Selecting one resets the default slot.</summary>
    public RomTile? SelectedTile
    {
        get => _selectedTile;
        set
        {
            if (SetProperty(ref _selectedTile, value))
            {
                OnPropertyChanged(nameof(CanAttach));
                SelectedSlot = value is null ? null : MediaExtensionMap.Resolve(value.FileName)?.Slot;
            }
        }
    }

    /// <summary>The media slot the selection will attach to (defaults from the file extension).</summary>
    public MediaSlot? SelectedSlot
    {
        get => _selectedSlot;
        set => SetProperty(ref _selectedSlot, value);
    }

    /// <summary>AC-LAUNCH-03: whether the current selection can be attached and booted.</summary>
    public bool CanAttach => _selectedTile?.Launchable == true;

    /// <summary>The debounced-search task in flight (test seam).</summary>
    internal Task PendingSearch => _pendingSearch ?? Task.CompletedTask;

    /// <summary>The machine-change rescope task in flight (test seam).</summary>
    internal Task PendingRescope => _pendingRescope ?? Task.CompletedTask;

    /// <summary>Resolves the active machine's platform and loads the first page.</summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _platformId = await _gateway
            .ResolvePlatformIdAsync(_machine.GetActivePlatformSlug(), cancellationToken)
            .ConfigureAwait(false);
        await ReloadAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Resets to the first page and reloads with the current search/order.</summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    public async Task ReloadAsync(CancellationToken cancellationToken = default)
    {
        _offset = 0;
        LibraryPage page = await _gateway.BrowseAsync(BuildQuery(0), cancellationToken).ConfigureAwait(false);
        ApplyPage(page, replace: true);
    }

    /// <summary>AC-BROWSE-04: loads and appends the next page, if any remain.</summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    public async Task LoadMoreAsync(CancellationToken cancellationToken = default)
    {
        if (!HasMore)
        {
            return;
        }

        _offset = Items.Count;
        LibraryPage page = await _gateway.BrowseAsync(BuildQuery(_offset), cancellationToken).ConfigureAwait(false);
        ApplyPage(page, replace: false);
    }

    /// <summary>AC-BROWSE-05: jumps to the page where entries for <paramref name="letter"/> begin.</summary>
    /// <param name="letter">The A-Z (or #) jump target.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public async Task JumpToLetterAsync(char letter, CancellationToken cancellationToken = default)
    {
        string key = char.ToUpperInvariant(letter).ToString();
        if (!_charIndex.TryGetValue(key, out int offset))
        {
            return;
        }

        _offset = offset;
        LibraryPage page = await _gateway.BrowseAsync(BuildQuery(offset), cancellationToken).ConfigureAwait(false);
        ApplyPage(page, replace: true);
    }

    /// <summary>
    /// AC-BROWSE-07: re-resolves the active machine's platform and reloads from the first page. Called
    /// when the machine changes.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    public async Task RescopeAsync(CancellationToken cancellationToken = default)
    {
        _platformId = await _gateway
            .ResolvePlatformIdAsync(_machine.GetActivePlatformSlug(), cancellationToken)
            .ConfigureAwait(false);
        await ReloadAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// AC-LAUNCH-04/07: downloads the selected game (two-phase status "Downloading N%" then "Starting")
    /// and hands it to the launcher for the resolved slot. A no-op when nothing launchable is selected.
    /// </summary>
    /// <param name="autostart">Whether to boot after attaching.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public async Task AttachAsync(bool autostart, CancellationToken cancellationToken = default)
    {
        RomTile? tile = SelectedTile;
        if (tile is null || !tile.Launchable)
        {
            StatusMessage = "Select a launchable game first.";
            return;
        }

        MediaSlot slot = SelectedSlot
            ?? MediaExtensionMap.Resolve(tile.FileName)?.Slot
            ?? MediaSlot.Drive8;

        var progress = new ActionProgress(fraction =>
        {
            DownloadProgress = fraction;
            StatusMessage = $"Downloading {fraction:P0}";
        });

        AcquiredGame game = await _gateway
            .DownloadAsync(tile.Id, tile.FileName, tile.SizeBytes ?? 0, _cacheDir, progress, cancellationToken)
            .ConfigureAwait(false);

        StatusMessage = "Starting";
        LaunchOutcome outcome = await _launcher
            .LaunchAsync(game, slot, autostart, cancellationToken)
            .ConfigureAwait(false);

        StatusMessage = outcome.Message;
    }

    /// <inheritdoc />
    public void Dispose() => _machine.PlatformChanged -= OnPlatformChanged;

    private void OnPlatformChanged(object? sender, EventArgs e) =>
        _pendingRescope = RescopeAsync(CancellationToken.None);

    private async Task DebouncedSearchAsync()
    {
        _searchCts?.Cancel();
        var cts = new CancellationTokenSource();
        _searchCts = cts;
        try
        {
            await Task.Delay(_debounce, cts.Token).ConfigureAwait(false);
            await ReloadAsync(cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Superseded by a newer keystroke; drop this query.
        }
    }

    private LibraryQuery BuildQuery(int offset) => new(
        string.IsNullOrWhiteSpace(_searchText) ? null : _searchText,
        _platformId,
        _pageSize,
        offset,
        _order);

    private void ApplyPage(LibraryPage page, bool replace)
    {
        Dispatch(() =>
        {
            if (replace)
            {
                Items.Clear();
            }

            foreach (RomTile tile in page.Items)
            {
                Items.Add(tile);
            }
        });

        _charIndex = page.CharIndex;
        Total = page.Total;
        OnPropertyChanged(nameof(HasMore));
    }

    private sealed class ActionProgress : IProgress<double>
    {
        private readonly Action<double> _onReport;

        public ActionProgress(Action<double> onReport) => _onReport = onReport;

        public void Report(double value) => _onReport(value);
    }
}
