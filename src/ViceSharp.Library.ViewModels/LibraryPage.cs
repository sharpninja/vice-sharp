namespace ViceSharp.Library.ViewModels;

/// <summary>
/// FR-ROMM-BROWSE-001. One page of library results plus the paging cursor and the A-Z jump index.
/// </summary>
/// <param name="Items">The tiles on this page.</param>
/// <param name="Total">The total number of matching ROMs across all pages.</param>
/// <param name="Offset">The zero-based offset of the first item on this page.</param>
/// <param name="CharIndex">
/// Maps a leading character (e.g. <c>"A"</c>, <c>"#"</c>) to the item offset where that character's
/// entries begin, so the A-Z strip can jump directly. Empty when the server does not supply one.
/// </param>
public sealed record LibraryPage(
    IReadOnlyList<RomTile> Items,
    int Total,
    int Offset,
    IReadOnlyDictionary<string, int> CharIndex);
