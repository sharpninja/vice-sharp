namespace ViceSharp.Library.ViewModels;

/// <summary>FR-ROMM-DETAIL-001. The full detail of a single ROM, backing the game-details page.</summary>
/// <param name="Id">The RomM ROM id.</param>
/// <param name="Name">The display name.</param>
/// <param name="Summary">A human-readable description, or <c>null</c>.</param>
/// <param name="PlatformSlug">The RomM platform slug.</param>
/// <param name="Cover">The cover art reference, or <c>null</c>.</param>
/// <param name="Files">The downloadable files that belong to this ROM.</param>
/// <param name="CollectionIds">The ids of the user collections this ROM belongs to.</param>
public sealed record RomDetail(
    int Id,
    string Name,
    string? Summary,
    string? PlatformSlug,
    CoverRef? Cover,
    IReadOnlyList<RomFile> Files,
    IReadOnlyList<int> CollectionIds);
