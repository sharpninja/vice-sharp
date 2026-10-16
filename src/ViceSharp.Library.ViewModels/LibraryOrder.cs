namespace ViceSharp.Library.ViewModels;

/// <summary>FR-ROMM-BROWSE-001. The sort order for a library browse query.</summary>
public enum LibraryOrder
{
    /// <summary>Alphabetical by name (ascending).</summary>
    Name = 0,

    /// <summary>By first release date (ascending).</summary>
    ReleaseDate = 1,

    /// <summary>By average rating (descending).</summary>
    Rating = 2,

    /// <summary>By when the ROM was added to the library (most recent first).</summary>
    Added = 3,
}
