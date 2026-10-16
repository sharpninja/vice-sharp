namespace ViceSharp.Library.ViewModels;

/// <summary>
/// FR-ROMM-CONN-001. Locates a RomM server by preferring the last remembered connection when it still
/// responds, and only scanning the LAN when there is no saved server or the saved server is down.
/// </summary>
public sealed class RomMServerLocator
{
    private readonly IRomMConnectionStore _store;
    private readonly IRomMServerProbe _probe;
    private readonly IRomMDiscovery _discovery;

    /// <summary>Creates the locator.</summary>
    /// <param name="store">Persisted last connection (URL + token).</param>
    /// <param name="probe">Reachability probe for a single base URL.</param>
    /// <param name="discovery">LAN subnet scan fallback.</param>
    public RomMServerLocator(IRomMConnectionStore store, IRomMServerProbe probe, IRomMDiscovery discovery)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _probe = probe ?? throw new ArgumentNullException(nameof(probe));
        _discovery = discovery ?? throw new ArgumentNullException(nameof(discovery));
    }

    /// <summary>
    /// Returns the remembered server when it is reachable; otherwise scans the network and returns the
    /// first hit (if any). Does not mutate the store (the head saves after a successful connect).
    /// </summary>
    /// <param name="port">Scan port when a LAN scan is required (RomM default 8080).</param>
    /// <param name="probeTimeout">Timeout for the saved-server probe.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public async Task<RomMLocateResult> LocateAsync(
        int port = 8080,
        TimeSpan? probeTimeout = null,
        CancellationToken cancellationToken = default)
    {
        RomMConnection? saved = await _store.LoadAsync(cancellationToken).ConfigureAwait(false);
        if (saved is not null
            && Uri.TryCreate(saved.BaseUrl, UriKind.Absolute, out Uri? remembered)
            && remembered is not null)
        {
            bool up = await _probe
                .IsReachableAsync(remembered, probeTimeout, cancellationToken)
                .ConfigureAwait(false);
            if (up)
            {
                return new RomMLocateResult(
                    remembered,
                    ScannedNetwork: false,
                    saved,
                    $"Reconnecting to {remembered}...");
            }
        }

        IReadOnlyList<DiscoveredRomM> servers = await _discovery
            .ScanAsync(port, perHostTimeout: probeTimeout, progress: null, cancellationToken)
            .ConfigureAwait(false);

        if (servers.Count == 0)
        {
            string hint = saved is null
                ? "No RomM servers found. Enter a URL and token, then Connect."
                : "Remembered server is offline and no other RomM servers were found. Enter a URL and token, then Connect.";
            return new RomMLocateResult(null, ScannedNetwork: true, SavedConnection: null, hint);
        }

        DiscoveredRomM first = servers[0];
        return new RomMLocateResult(
            first.BaseUrl,
            ScannedNetwork: true,
            SavedConnection: null,
            $"Found {first.BaseUrl}. Signing in...");
    }
}
