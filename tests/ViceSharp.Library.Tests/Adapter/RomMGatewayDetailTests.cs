using FluentAssertions;
using ViceSharp.Library.ViewModels;
using ViceSharp.RomM;
using Xunit;

namespace ViceSharp.Library.Tests.Adapter;

/// <summary>
/// FR-ROMM-DETAIL-001 (AC-DETAIL-01). Use case: the adapter maps a RomM detailed rom to a
/// <see cref="RomDetail"/> with files, cover, summary and launchability.
/// </summary>
[Trait("Category", "Library")]
public sealed class RomMGatewayDetailTests
{
    /// <summary>AC-DETAIL-01: the detailed rom maps its files/cover/summary/launchable fields.</summary>
    [Fact]
    [Trait("AC", "AC-DETAIL-01")]
    public async Task Detail_Maps()
    {
        var handler = new FakeRomMHandler(RomMFixtures.DefaultRouter);
        await using var client = RomMFixtures.Client(handler);
        var gateway = new RomMLibraryGateway(client);

        RomDetail detail = await gateway.GetRomAsync(101, TestContext.Current.CancellationToken);

        detail.Id.Should().Be(101);
        detail.Name.Should().Be("Boulder Dash");
        detail.Summary.Should().Be("Dig diamonds, dodge boulders.");
        detail.PlatformSlug.Should().Be("c64");
        detail.Cover!.Url.Should().Be("https://cdn.romm.local/101.png");
        detail.Cover!.Path.Should().Be("/assets/roms/101/cover/large.png");

        detail.Files.Should().ContainSingle();
        detail.Files[0].FileName.Should().Be("boulderdash.d64");
        detail.Files[0].SizeBytes.Should().Be(174848);
        detail.Files[0].Kind.Should().Be(MediaKind.Disk);
        detail.Files[0].Launchable.Should().BeTrue();
    }
}
