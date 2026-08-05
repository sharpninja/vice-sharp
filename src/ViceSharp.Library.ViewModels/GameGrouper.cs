namespace ViceSharp.Library.ViewModels;

/// <summary>
/// FR-ROMM-BROWSE-001. Groups consecutive RomM tiles that share the same display name into
/// <see cref="GameGroup"/> rows. RomM returns name-sorted pages, so siblings sit next to each other;
/// paging may split a group across page edges, so <see cref="Append"/> merges the boundary.
/// </summary>
public static class GameGrouper
{
    /// <summary>
    /// Groups a contiguous, name-ordered sequence of tiles. Consecutive equal names (ordinal
    /// ignore-case) become one group.
    /// </summary>
    /// <param name="tiles">The page items in server order.</param>
    /// <returns>One group per distinct name run.</returns>
    public static List<GameGroup> GroupConsecutive(IEnumerable<RomTile> tiles)
    {
        ArgumentNullException.ThrowIfNull(tiles);

        var groups = new List<GameGroup>();
        foreach (RomTile tile in tiles)
        {
            AppendTile(groups, tile);
        }

        return groups;
    }

    /// <summary>
    /// Appends <paramref name="tiles"/> onto <paramref name="groups"/>, merging into the last group
    /// when the name matches (page-boundary sibling merge).
    /// </summary>
    /// <param name="groups">The groups already shown in the grid.</param>
    /// <param name="tiles">Newly loaded page items in server order.</param>
    public static void Append(IList<GameGroup> groups, IEnumerable<RomTile> tiles)
    {
        ArgumentNullException.ThrowIfNull(groups);
        ArgumentNullException.ThrowIfNull(tiles);

        foreach (RomTile tile in tiles)
        {
            AppendTile(groups, tile);
        }
    }

    private static void AppendTile(IList<GameGroup> groups, RomTile tile)
    {
        if (groups.Count > 0
            && string.Equals(groups[groups.Count - 1].Name, tile.Name, StringComparison.OrdinalIgnoreCase))
        {
            groups[groups.Count - 1].Variants.Add(tile);
            return;
        }

        groups.Add(new GameGroup(tile.Name, new List<RomTile> { tile }));
    }
}
