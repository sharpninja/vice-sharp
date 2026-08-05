namespace ViceSharp.Library.ViewModels;

/// <summary>
/// FR-ROMM-BROWSE-001. One grid tile for a game that may have multiple RomM ROM variants
/// (language / region / revision). The grid shows the group once; the details page lists variants.
/// </summary>
public sealed class GameGroup
{
    /// <summary>Creates a group with at least one variant.</summary>
    /// <param name="name">The shared display name (RomM <c>name</c>).</param>
    /// <param name="variants">The ROM variants in this group (non-empty).</param>
    public GameGroup(string name, IList<RomTile> variants)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(variants);
        if (variants.Count == 0)
        {
            throw new ArgumentException("A game group needs at least one variant.", nameof(variants));
        }

        Name = name;
        Variants = variants;
    }

    /// <summary>The shared display name.</summary>
    public string Name { get; }

    /// <summary>ROM variants that share <see cref="Name"/> (mutable so paging can merge across page edges).</summary>
    public IList<RomTile> Variants { get; }

    /// <summary>Number of variants in the group.</summary>
    public int VariantCount => Variants.Count;

    /// <summary>Whether more than one ROM maps to this game name.</summary>
    public bool HasMultipleVariants => Variants.Count > 1;

    /// <summary>
    /// Subtitle under the cover: variant count when grouped, otherwise the single file name.
    /// </summary>
    public string Subtitle =>
        HasMultipleVariants
            ? $"{Variants.Count} variants"
            : Variants[0].FileName;

    /// <summary>Cover from the first variant that has one, else the first variant's cover.</summary>
    public CoverRef? Cover
    {
        get
        {
            foreach (RomTile tile in Variants)
            {
                if (tile.Cover is not null)
                {
                    return tile.Cover;
                }
            }

            return Variants[0].Cover;
        }
    }

    /// <summary>Preferred launch target: first launchable variant, else the first variant.</summary>
    public RomTile Primary
    {
        get
        {
            foreach (RomTile tile in Variants)
            {
                if (tile.Launchable)
                {
                    return tile;
                }
            }

            return Variants[0];
        }
    }

    /// <summary>Whether any variant can be attached and booted.</summary>
    public bool Launchable
    {
        get
        {
            foreach (RomTile tile in Variants)
            {
                if (tile.Launchable)
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>Platform slug from the primary variant.</summary>
    public string? PlatformSlug => Primary.PlatformSlug;
}
