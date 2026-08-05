namespace ViceSharp.Library.ViewModels;

/// <summary>
/// FR-ROMM-CONN-001 (AC-CONN-02). Exchanges a device-pairing code for a token. The actual exchange
/// (RomM endpoint or bridge) is a head/config detail behind this seam so the coordinator stays portable
/// and testable.
/// </summary>
public interface IRomMPairingExchange
{
    /// <summary>Exchanges a pairing code for a bearer token.</summary>
    /// <param name="baseUrl">The RomM server base URL.</param>
    /// <param name="code">The pairing code shown on the device.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task<string> ExchangeAsync(string baseUrl, string code, CancellationToken cancellationToken = default);
}

/// <summary>
/// FR-ROMM-CONN-001 (AC-CONN-02). Runs the device-pairing exchange and persists the resulting connection
/// through the store so the device stays paired.
/// </summary>
public sealed class RomMPairingCoordinator
{
    private readonly IRomMPairingExchange _exchange;
    private readonly IRomMConnectionStore _store;

    /// <summary>Creates the coordinator.</summary>
    /// <param name="exchange">The pairing-code exchange.</param>
    /// <param name="store">The connection store.</param>
    public RomMPairingCoordinator(IRomMPairingExchange exchange, IRomMConnectionStore store)
    {
        _exchange = exchange ?? throw new ArgumentNullException(nameof(exchange));
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    /// <summary>
    /// AC-CONN-02. Exchanges <paramref name="code"/> for a token, persists the connection, and returns it.
    /// </summary>
    /// <param name="baseUrl">The RomM server base URL.</param>
    /// <param name="code">The pairing code.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public async Task<RomMConnection> PairAsync(string baseUrl, string code, CancellationToken cancellationToken = default)
    {
        string token = await _exchange.ExchangeAsync(baseUrl, code, cancellationToken).ConfigureAwait(false);
        var connection = new RomMConnection(baseUrl, RomMAuthMode.DevicePair, token);
        await _store.SaveAsync(connection, cancellationToken).ConfigureAwait(false);
        return connection;
    }
}
