namespace ViceSharp.Library.ViewModels;

/// <summary>
/// FR-CSDB-001. The seam for CSDb discovery and ingest, implemented two ways in the <c>ViceSharp.RomM</c>
/// adapter: a co-located gateway (where the RomM roms root is locally writable) and a bridge gateway
/// (the csdb-bridge sidecar, for sandboxed/remote heads such as Xbox).
/// </summary>
public interface ICsdbGateway
{
    /// <summary>
    /// AC-CSDB-01. Searches CSDb for scene releases, optionally filtered by <paramref name="kinds"/>.
    /// </summary>
    /// <param name="query">The free-text query.</param>
    /// <param name="kinds">The kinds to include, or <c>null</c> for all.</param>
    /// <param name="limit">The maximum number of hits.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task<IReadOnlyList<CsdbHit>> SearchAsync(
        string query,
        IReadOnlyList<CsdbKind>? kinds,
        int limit,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// AC-CSDB-03/04. Ingests the selected entries into the RomM library and triggers a scan so they
    /// appear as normal ROMs.
    /// </summary>
    /// <param name="selections">The chosen entries (already capped by the caller).</param>
    /// <param name="force">Whether to re-ingest entries that already exist.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task<CsdbIngestResult> IngestAndScanAsync(
        IReadOnlyList<CsdbSelection> selections,
        bool force,
        CancellationToken cancellationToken = default);
}
