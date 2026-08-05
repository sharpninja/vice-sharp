namespace ViceSharp.Library.ViewModels;

/// <summary>
/// FR-ROMM-CONN-001 (AC-CONN-05). Persists the RomM connection (base URL + auth mode + token) so a head
/// can reconnect without re-authenticating. Implemented per head (a file store on desktop; the secure
/// keystore on Xbox).
/// </summary>
public interface IRomMConnectionStore
{
    /// <summary>Loads the persisted connection, or <c>null</c> when none is stored.</summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task<RomMConnection?> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>Persists the connection.</summary>
    /// <param name="connection">The connection to store.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task SaveAsync(RomMConnection connection, CancellationToken cancellationToken = default);

    /// <summary>Clears any persisted connection.</summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task ClearAsync(CancellationToken cancellationToken = default);
}
