namespace ViceSharp.Library.ViewModels;

/// <summary>
/// FR-ROMM-COLLECT-001. The seam for RomM server-side collections (user lists), implemented in the
/// <c>ViceSharp.RomM</c> adapter. Kept separate from <see cref="IRomMLibraryGateway"/> so browse and
/// list management can evolve independently.
/// </summary>
public interface IRomMCollectionsGateway
{
    /// <summary>
    /// AC-COLLECT-01. Lists the user's collections; when <paramref name="includeSmartVirtual"/> is
    /// <c>true</c>, also includes server-managed smart/virtual collections (flagged read-only).
    /// </summary>
    /// <param name="includeSmartVirtual">Whether to include smart/virtual collections.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task<IReadOnlyList<LibraryCollection>> GetCollectionsAsync(
        bool includeSmartVirtual,
        CancellationToken cancellationToken = default);

    /// <summary>AC-COLLECT-02. Creates a new collection and returns it.</summary>
    /// <param name="name">The new collection name.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task<LibraryCollection> CreateCollectionAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>AC-COLLECT-04. Renames a collection.</summary>
    /// <param name="id">The collection id.</param>
    /// <param name="newName">The new name.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task RenameCollectionAsync(int id, string newName, CancellationToken cancellationToken = default);

    /// <summary>AC-COLLECT-04. Deletes a collection.</summary>
    /// <param name="id">The collection id.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task DeleteCollectionAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>AC-COLLECT-03. Adds ROMs to a collection.</summary>
    /// <param name="id">The collection id.</param>
    /// <param name="romIds">The ROM ids to add.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task AddRomsAsync(int id, IReadOnlyList<int> romIds, CancellationToken cancellationToken = default);

    /// <summary>AC-COLLECT-03. Removes ROMs from a collection.</summary>
    /// <param name="id">The collection id.</param>
    /// <param name="romIds">The ROM ids to remove.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task RemoveRomsAsync(int id, IReadOnlyList<int> romIds, CancellationToken cancellationToken = default);
}
