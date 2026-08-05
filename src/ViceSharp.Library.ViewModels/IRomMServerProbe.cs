namespace ViceSharp.Library.ViewModels;

/// <summary>
/// FR-ROMM-CONN-001. Probes whether a RomM base URL is reachable (typically via unauthenticated
/// <c>/api/heartbeat</c>). Used so heads can reconnect to a remembered server without a full LAN scan.
/// </summary>
public interface IRomMServerProbe
{
    /// <summary>
    /// Returns <c>true</c> when <paramref name="baseUrl"/> answers as a live RomM instance.
    /// </summary>
    /// <param name="baseUrl">The candidate RomM base URL.</param>
    /// <param name="timeout">Per-probe timeout; implementation default when <c>null</c>.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task<bool> IsReachableAsync(
        Uri baseUrl,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default);
}
