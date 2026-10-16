namespace ViceSharp.Library.ViewModels;

/// <summary>
/// FR-ROMM-BROWSE-001. A single library ROM (one variant): enough to render a cover, a title, and to
/// launch or open the game without a second round-trip. Multiple tiles that share <see cref="Name"/>
/// are collapsed into a <see cref="GameGroup"/> on the grid.
/// </summary>
/// <param name="Id">The RomM ROM id.</param>
/// <param name="Name">The display name (shared across language/region variants).</param>
/// <param name="FileName">The primary file name (RomM <c>fs_name</c>), used to pick the media slot.</param>
/// <param name="PlatformSlug">The RomM platform slug (always the active machine's).</param>
/// <param name="SizeBytes">The primary file size in bytes, when known.</param>
/// <param name="Cover">The cover art reference, or <c>null</c> when the ROM has no cover.</param>
/// <param name="Launchable">Whether the primary file can be attached and booted.</param>
/// <param name="Regions">RomM region tags (e.g. <c>Europe</c>), when known.</param>
/// <param name="Languages">RomM language tags (e.g. <c>English</c>), when known.</param>
/// <param name="Revision">RomM revision tag, when known.</param>
public sealed record RomTile(
    int Id,
    string Name,
    string FileName,
    string? PlatformSlug,
    long? SizeBytes,
    CoverRef? Cover,
    bool Launchable,
    string? Regions = null,
    string? Languages = null,
    string? Revision = null)
{
    /// <summary>
    /// Human-readable variant line for the details list: language / region / revision when present,
    /// otherwise the file name.
    /// </summary>
    public string VariantLabel
    {
        get
        {
            var parts = new List<string>(3);
            if (!string.IsNullOrWhiteSpace(Languages))
            {
                parts.Add(Languages);
            }

            if (!string.IsNullOrWhiteSpace(Regions))
            {
                parts.Add(Regions);
            }

            if (!string.IsNullOrWhiteSpace(Revision))
            {
                parts.Add($"rev {Revision}");
            }

            return parts.Count > 0 ? string.Join(" · ", parts) : FileName;
        }
    }
}
