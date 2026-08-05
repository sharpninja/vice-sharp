namespace ViceSharp.Library.ViewModels;

/// <summary>
/// FR-ROMM-BROWSE-001. A single library grid tile: enough to render a cover, a title, and to launch
/// or open the game without a second round-trip.
/// </summary>
/// <param name="Id">The RomM ROM id.</param>
/// <param name="Name">The display name.</param>
/// <param name="FileName">The primary file name (RomM <c>fs_name</c>), used to pick the media slot.</param>
/// <param name="PlatformSlug">The RomM platform slug (always the active machine's).</param>
/// <param name="SizeBytes">The primary file size in bytes, when known.</param>
/// <param name="Cover">The cover art reference, or <c>null</c> when the ROM has no cover.</param>
/// <param name="Launchable">Whether the primary file can be attached and booted.</param>
public sealed record RomTile(
    int Id,
    string Name,
    string FileName,
    string? PlatformSlug,
    long? SizeBytes,
    CoverRef? Cover,
    bool Launchable);
