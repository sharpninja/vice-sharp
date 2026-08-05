namespace ViceSharp.Library.ViewModels;

/// <summary>
/// FR-ROMM-COLLECT-001. A user list, backed by a RomM server-side collection. Smart/virtual collections
/// are surfaced as <see cref="ReadOnly"/> (they cannot be edited by hand).
/// </summary>
/// <param name="Id">The RomM collection id.</param>
/// <param name="Name">The collection name.</param>
/// <param name="Count">The number of ROMs in the collection.</param>
/// <param name="ReadOnly">Whether the collection is server-managed (smart/virtual) and cannot be edited.</param>
/// <param name="RomIds">The ids of the ROMs in the collection.</param>
public sealed record LibraryCollection(
    int Id,
    string Name,
    int Count,
    bool ReadOnly,
    IReadOnlyList<int> RomIds);
