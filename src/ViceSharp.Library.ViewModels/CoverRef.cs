namespace ViceSharp.Library.ViewModels;

/// <summary>
/// FR-ROMM-COVER-001. A reference to a ROM's cover art. <see cref="Url"/> is an absolute URL served
/// without authentication; <see cref="Path"/> is a server-relative static-asset path that is fetched
/// with the bearer token. At least one is non-null when a cover exists.
/// </summary>
/// <param name="Url">Absolute, unauthenticated cover URL (RomM <c>url_cover</c>), or <c>null</c>.</param>
/// <param name="Path">Server-relative authenticated cover path (RomM <c>path_cover_*</c>), or <c>null</c>.</param>
public sealed record CoverRef(string? Url, string? Path);
