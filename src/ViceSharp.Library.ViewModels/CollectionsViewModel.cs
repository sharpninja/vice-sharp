using System.Collections.ObjectModel;

namespace ViceSharp.Library.ViewModels;

/// <summary>
/// FR-ROMM-COLLECT-001 (AC-COLLECT-05). The list-management ViewModel: lists collections and creates,
/// renames, deletes, and edits their membership, refreshing from the server after each mutation so both
/// heads see a consistent view. Background collection mutations dispatch to the captured UI context.
/// </summary>
public sealed class CollectionsViewModel : LibraryObservableObject
{
    private readonly IRomMCollectionsGateway _gateway;
    private LibraryCollection? _selected;

    /// <summary>Creates the ViewModel.</summary>
    /// <param name="gateway">The collections gateway.</param>
    public CollectionsViewModel(IRomMCollectionsGateway gateway) =>
        _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));

    /// <summary>The user's collections (refreshed from the server).</summary>
    public ObservableCollection<LibraryCollection> Collections { get; } = new();

    /// <summary>The currently selected collection.</summary>
    public LibraryCollection? SelectedCollection
    {
        get => _selected;
        set => SetProperty(ref _selected, value);
    }

    /// <summary>Reloads the collections from the server.</summary>
    /// <param name="includeSmartVirtual">Whether to include smart/virtual collections.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public async Task RefreshAsync(bool includeSmartVirtual = true, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<LibraryCollection> items =
            await _gateway.GetCollectionsAsync(includeSmartVirtual, cancellationToken).ConfigureAwait(false);

        Dispatch(() =>
        {
            Collections.Clear();
            foreach (LibraryCollection collection in items)
            {
                Collections.Add(collection);
            }
        });
    }

    /// <summary>Creates a collection, then refreshes.</summary>
    /// <param name="name">The new collection name.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public async Task CreateAsync(string name, CancellationToken cancellationToken = default)
    {
        await _gateway.CreateCollectionAsync(name, cancellationToken).ConfigureAwait(false);
        await RefreshAsync(true, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Renames a collection, then refreshes.</summary>
    /// <param name="id">The collection id.</param>
    /// <param name="newName">The new name.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public async Task RenameAsync(int id, string newName, CancellationToken cancellationToken = default)
    {
        await _gateway.RenameCollectionAsync(id, newName, cancellationToken).ConfigureAwait(false);
        await RefreshAsync(true, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Deletes a collection, then refreshes.</summary>
    /// <param name="id">The collection id.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await _gateway.DeleteCollectionAsync(id, cancellationToken).ConfigureAwait(false);
        await RefreshAsync(true, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>AC-COLLECT-05. Adds ROMs to a collection, then refreshes.</summary>
    /// <param name="id">The collection id.</param>
    /// <param name="romIds">The ROM ids to add.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public async Task AddRomsAsync(int id, IReadOnlyList<int> romIds, CancellationToken cancellationToken = default)
    {
        await _gateway.AddRomsAsync(id, romIds, cancellationToken).ConfigureAwait(false);
        await RefreshAsync(true, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>AC-COLLECT-05. Removes ROMs from a collection, then refreshes.</summary>
    /// <param name="id">The collection id.</param>
    /// <param name="romIds">The ROM ids to remove.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public async Task RemoveRomsAsync(int id, IReadOnlyList<int> romIds, CancellationToken cancellationToken = default)
    {
        await _gateway.RemoveRomsAsync(id, romIds, cancellationToken).ConfigureAwait(false);
        await RefreshAsync(true, cancellationToken).ConfigureAwait(false);
    }
}
