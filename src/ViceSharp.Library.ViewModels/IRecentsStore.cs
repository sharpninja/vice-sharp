namespace ViceSharp.Library.ViewModels;

/// <summary>
/// FR-ROMM-RECENTS-001. Local MRU store of games loaded from RomM. Head-owned (file under the
/// app cache); independent of RomM server collections so order and offline cache stay local.
/// </summary>
public interface IRecentsStore
{
    /// <summary>Loads recents newest-first (empty when none).</summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task<IReadOnlyList<RecentGame>> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a successful load: moves an existing id to the front, otherwise inserts, then
    /// trims to <paramref name="capacity"/>.
    /// </summary>
    /// <param name="game">The game that was loaded.</param>
    /// <param name="capacity">Maximum entries to keep (default <see cref="RecentGame.DefaultCapacity"/>).</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task RecordAsync(
        RecentGame game,
        int capacity = RecentGame.DefaultCapacity,
        CancellationToken cancellationToken = default);

    /// <summary>Clears all recents.</summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task ClearAsync(CancellationToken cancellationToken = default);
}
