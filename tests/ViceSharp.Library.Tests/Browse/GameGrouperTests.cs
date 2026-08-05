using FluentAssertions;
using ViceSharp.Library.ViewModels;
using Xunit;

namespace ViceSharp.Library.Tests.Browse;

/// <summary>
/// FR-ROMM-BROWSE-001. Use case: same-name RomM ROMs (language/region/revision variants) collapse into
/// one grid group; page-boundary siblings merge into the previous group.
/// </summary>
[Trait("Category", "Library")]
public sealed class GameGrouperTests
{
    private static RomTile Tile(int id, string name, string file = "g.d64") =>
        new(id, name, file, "c64", 1000, null, true);

    /// <summary>Consecutive same names become one group with all variants.</summary>
    [Fact]
    public void GroupConsecutive_CollapsesSameName()
    {
        List<RomTile> tiles =
        [
            Tile(1, "64 Breakout", "a.t64"),
            Tile(2, "64 Breakout", "b.t64"),
            Tile(3, "64 Breakout", "c.t64"),
            Tile(4, "Other", "d.d64"),
        ];

        List<GameGroup> groups = GameGrouper.GroupConsecutive(tiles);

        groups.Should().HaveCount(2);
        groups[0].Name.Should().Be("64 Breakout");
        groups[0].VariantCount.Should().Be(3);
        groups[0].HasMultipleVariants.Should().BeTrue();
        groups[0].Subtitle.Should().Be("3 variants");
        groups[0].Variants.Select(v => v.Id).Should().Equal(1, 2, 3);
        groups[1].Name.Should().Be("Other");
        groups[1].HasMultipleVariants.Should().BeFalse();
        groups[1].Subtitle.Should().Be("d.d64");
    }

    /// <summary>Different names stay as separate groups even when adjacent.</summary>
    [Fact]
    public void GroupConsecutive_KeepsDistinctNamesSeparate()
    {
        List<RomTile> tiles = [Tile(1, "Alpha"), Tile(2, "Beta"), Tile(3, "Gamma")];

        List<GameGroup> groups = GameGrouper.GroupConsecutive(tiles);

        groups.Should().HaveCount(3);
        groups.Select(g => g.Name).Should().Equal("Alpha", "Beta", "Gamma");
    }

    /// <summary>Appending a page that continues the last name merges into that group.</summary>
    [Fact]
    public void Append_MergesPageBoundarySiblings()
    {
        var groups = new List<GameGroup>
        {
            new("64 Breakout", new List<RomTile> { Tile(1, "64 Breakout", "a.t64") }),
        };

        GameGrouper.Append(groups, new[]
        {
            Tile(2, "64 Breakout", "b.t64"),
            Tile(3, "Next Game", "c.d64"),
        });

        groups.Should().HaveCount(2);
        groups[0].VariantCount.Should().Be(2);
        groups[0].Variants.Select(v => v.Id).Should().Equal(1, 2);
        groups[1].Name.Should().Be("Next Game");
        groups[1].Variants[0].Id.Should().Be(3);
    }

    /// <summary>VariantLabel prefers language/region/revision over the raw file name.</summary>
    [Fact]
    public void RomTile_VariantLabel_UsesMetadata()
    {
        var labeled = new RomTile(1, "G", "file.d64", "c64", 1, null, true, "Europe", "English", "02");
        labeled.VariantLabel.Should().Be("English · Europe · rev 02");

        var bare = new RomTile(2, "G", "file.d64", "c64", 1, null, true);
        bare.VariantLabel.Should().Be("file.d64");
    }
}
