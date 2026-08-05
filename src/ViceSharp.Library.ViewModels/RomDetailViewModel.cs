namespace ViceSharp.Library.ViewModels;

/// <summary>
/// FR-ROMM-DETAIL-001 (AC-DETAIL-02). The game-details ViewModel: exposes a <see cref="RomDetail"/> and
/// can add the ROM to a collection.
/// </summary>
public sealed class RomDetailViewModel : LibraryObservableObject
{
    private readonly IRomMCollectionsGateway _collections;

    /// <summary>Creates the ViewModel.</summary>
    /// <param name="detail">The ROM detail to present.</param>
    /// <param name="collections">The collections gateway (for add-to-list).</param>
    public RomDetailViewModel(RomDetail detail, IRomMCollectionsGateway collections)
    {
        Detail = detail ?? throw new ArgumentNullException(nameof(detail));
        _collections = collections ?? throw new ArgumentNullException(nameof(collections));
    }

    /// <summary>The ROM detail.</summary>
    public RomDetail Detail { get; }

    /// <summary>AC-DETAIL-02. Adds this ROM to the given collection.</summary>
    /// <param name="collectionId">The target collection id.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public Task AddToCollectionAsync(int collectionId, CancellationToken cancellationToken = default) =>
        _collections.AddRomsAsync(collectionId, new[] { Detail.Id }, cancellationToken);
}
