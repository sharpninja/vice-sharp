namespace ViceSharp.Library.ViewModels;

/// <summary>
/// FR-ROMM-RECENTS-001. One entry in the local Recents list: enough to render a tile and relaunch
/// from the download cache without a second RomM fetch when the file is still present.
/// </summary>
/// <param name="Id">The RomM ROM id.</param>
/// <param name="Name">The display name.</param>
/// <param name="FileName">The primary file name (cache key with <see cref="Id"/>).</param>
/// <param name="PlatformSlug">The platform slug when known.</param>
/// <param name="SizeBytes">The file size used for cache identity, when known.</param>
/// <param name="Cover">Cover art reference, or <c>null</c>.</param>
/// <param name="Launchable">Whether the file can be attached and booted.</param>
/// <param name="LoadedAt">When the game was last loaded (MRU order).</param>
public sealed record RecentGame(
    int Id,
    string Name,
    string FileName,
    string? PlatformSlug,
    long? SizeBytes,
    CoverRef? Cover,
    bool Launchable,
    DateTimeOffset LoadedAt)
{
    /// <summary>Default capacity of the Recents list.</summary>
    public const int DefaultCapacity = 25;

    /// <summary>Maps this entry to a library tile for the grid / attach path.</summary>
    public RomTile ToTile() =>
        new(Id, Name, FileName, PlatformSlug, SizeBytes, Cover, Launchable);

    /// <summary>Builds a recents entry from a tile at the given (or current UTC) time.</summary>
    public static RecentGame FromTile(RomTile tile, DateTimeOffset? loadedAt = null) =>
        new(
            tile.Id,
            tile.Name,
            tile.FileName,
            tile.PlatformSlug,
            tile.SizeBytes,
            tile.Cover,
            tile.Launchable,
            loadedAt ?? DateTimeOffset.UtcNow);
}
