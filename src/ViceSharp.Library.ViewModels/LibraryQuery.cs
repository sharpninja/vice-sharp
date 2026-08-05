namespace ViceSharp.Library.ViewModels;

/// <summary>
/// FR-ROMM-BROWSE-001. A single page request against the library. The platform is always the active
/// machine's (there is no user platform picker): <see cref="PlatformId"/> is the numeric RomM platform
/// id resolved from the active machine's slug.
/// </summary>
/// <param name="SearchTerm">Optional free-text search; <c>null</c> or empty matches everything.</param>
/// <param name="PlatformId">The active machine's numeric RomM platform id.</param>
/// <param name="Limit">The page size.</param>
/// <param name="Offset">The zero-based item offset.</param>
/// <param name="Order">The sort order.</param>
/// <param name="JumpLetter">Optional A-Z jump target (resolved to an offset via the page char index).</param>
public sealed record LibraryQuery(
    string? SearchTerm,
    int PlatformId,
    int Limit,
    int Offset,
    LibraryOrder Order = LibraryOrder.Name,
    char? JumpLetter = null);
