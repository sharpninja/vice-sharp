using System.Collections.ObjectModel;

namespace ViceSharp.Library.ViewModels;

/// <summary>
/// FR-CSDB-001 (AC-CSDB-01/02/05). The CSDb discovery ViewModel: searches CSDb, and ingests a capped
/// selection into RomM, raising <see cref="LibraryRefreshRequested"/> when the library changes so the
/// browser can reload.
/// </summary>
public sealed class CsdbDiscoveryViewModel : LibraryObservableObject
{
    /// <summary>AC-CSDB-02. The maximum number of entries that may be ingested at once.</summary>
    public const int MaxIngestSelection = 20;

    private readonly ICsdbGateway _gateway;
    private string _query = string.Empty;
    private CsdbIngestResult? _lastResult;

    /// <summary>Creates the ViewModel.</summary>
    /// <param name="gateway">The CSDb gateway (co-located or bridge).</param>
    public CsdbDiscoveryViewModel(ICsdbGateway gateway) =>
        _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));

    /// <summary>The search query.</summary>
    public string Query
    {
        get => _query;
        set => SetProperty(ref _query, value);
    }

    /// <summary>The kind filters to include; empty means all kinds.</summary>
    public IList<CsdbKind> Kinds { get; } = new List<CsdbKind>();

    /// <summary>The current search results.</summary>
    public ObservableCollection<CsdbHit> Results { get; } = new();

    /// <summary>The most recent ingest result, or <c>null</c>.</summary>
    public CsdbIngestResult? LastResult
    {
        get => _lastResult;
        private set => SetProperty(ref _lastResult, value);
    }

    /// <summary>AC-CSDB-05. Raised after a successful ingest so the library can refresh.</summary>
    public event EventHandler? LibraryRefreshRequested;

    /// <summary>AC-CSDB-01. Runs the search and replaces <see cref="Results"/>.</summary>
    /// <param name="limit">The maximum number of hits.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public async Task SearchAsync(int limit = 50, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<CsdbHit> hits = await _gateway
            .SearchAsync(Query, Kinds.Count > 0 ? Kinds.ToList() : null, limit, cancellationToken)
            .ConfigureAwait(false);

        Dispatch(() =>
        {
            Results.Clear();
            foreach (CsdbHit hit in hits)
            {
                Results.Add(hit);
            }
        });
    }

    /// <summary>
    /// AC-CSDB-02/05. Ingests the selection (capped to <see cref="MaxIngestSelection"/>) and raises
    /// <see cref="LibraryRefreshRequested"/>.
    /// </summary>
    /// <param name="selections">The chosen entries.</param>
    /// <param name="force">Whether to re-ingest entries that already exist.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public async Task IngestAsync(
        IReadOnlyList<CsdbSelection> selections,
        bool force = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(selections);

        IReadOnlyList<CsdbSelection> capped = selections.Count > MaxIngestSelection
            ? selections.Take(MaxIngestSelection).ToList()
            : selections;

        if (capped.Count == 0)
        {
            return;
        }

        LastResult = await _gateway.IngestAndScanAsync(capped, force, cancellationToken).ConfigureAwait(false);
        Dispatch(() => LibraryRefreshRequested?.Invoke(this, EventArgs.Empty));
    }
}
