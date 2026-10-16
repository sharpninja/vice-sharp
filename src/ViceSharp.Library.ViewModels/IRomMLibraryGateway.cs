namespace ViceSharp.Library.ViewModels;

/// <summary>
/// FR-ROMM-BROWSE/DETAIL/LAUNCH-001. The seam the browser ViewModels use to reach the RomM server,
/// implemented in the <c>ViceSharp.RomM</c> adapter. All HTTP and REST-client detail lives behind this
/// interface so the portable VM library stays transport-free (TR-ROMM-BOUNDARY-001). Collections live
/// on a separate seam introduced with the collections slice.
/// </summary>
public interface IRomMLibraryGateway
{
    /// <summary>
    /// AC-BROWSE-02. Resolves a RomM platform slug (e.g. <c>c64</c>) to its numeric platform id,
    /// caching the lookup.
    /// </summary>
    /// <param name="slug">The RomM platform slug of the active machine.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <exception cref="System.InvalidOperationException">The server has no platform for the slug.</exception>
    Task<int> ResolvePlatformIdAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>
    /// AC-BROWSE-01/02/03. Fetches one page of the library scoped to <see cref="LibraryQuery.PlatformId"/>,
    /// optionally filtered by <see cref="LibraryQuery.SearchTerm"/>.
    /// </summary>
    /// <param name="query">The page request.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task<LibraryPage> BrowseAsync(LibraryQuery query, CancellationToken cancellationToken = default);

    /// <summary>AC-DETAIL-01. Fetches the full detail for a single ROM.</summary>
    /// <param name="romId">The RomM ROM id.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task<RomDetail> GetRomAsync(int romId, CancellationToken cancellationToken = default);

    /// <summary>
    /// AC-LAUNCH-01. Downloads a ROM file into <paramref name="cacheDir"/> under
    /// <c>{romId}/{fileName}</c>, reusing an existing file whose length matches
    /// <paramref name="expectedSizeBytes"/> (no re-download), and reporting fractional progress.
    /// </summary>
    /// <param name="romId">The RomM ROM id.</param>
    /// <param name="fileName">The file to download (RomM <c>fs_name</c>).</param>
    /// <param name="expectedSizeBytes">The expected file size, used for reuse detection and progress.</param>
    /// <param name="cacheDir">The local cache root directory.</param>
    /// <param name="progress">An optional progress sink reporting a fraction in [0, 1].</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task<AcquiredGame> DownloadAsync(
        int romId,
        string fileName,
        long expectedSizeBytes,
        string cacheDir,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);
}
