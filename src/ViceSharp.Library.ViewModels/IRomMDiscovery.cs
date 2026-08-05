namespace ViceSharp.Library.ViewModels;

/// <summary>
/// FR-ROMM-CONN-001 (AC-CONN-07, operator request 2026-07-16). Discovers RomM servers on the local
/// network so the head does not have to assume a fixed URL. The implementation lives in the adapter
/// (it holds the HTTP); this seam keeps the VM library transport-free (TR-ROMM-BOUNDARY-001).
/// </summary>
public interface IRomMDiscovery
{
    /// <summary>Scans the local subnet and returns every host that answers with a RomM heartbeat.</summary>
    /// <param name="port">The port to probe (RomM default 8080).</param>
    /// <param name="perHostTimeout">The per-host probe timeout; defaults to one second when <c>null</c>.</param>
    /// <param name="progress">An optional callback reporting the number of hosts scanned so far.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The discovered servers, ordered by host.</returns>
    Task<IReadOnlyList<DiscoveredRomM>> ScanAsync(
        int port = 8080,
        TimeSpan? perHostTimeout = null,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default);
}
