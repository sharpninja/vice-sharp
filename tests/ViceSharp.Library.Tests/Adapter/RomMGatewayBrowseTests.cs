using FluentAssertions;
using ViceSharp.Library.ViewModels;
using ViceSharp.RomM;
using Xunit;

namespace ViceSharp.Library.Tests.Adapter;

/// <summary>
/// FR-ROMM-BROWSE-001 (AC-BROWSE-01/02/03). Use case: the adapter turns a RomM roms page into a
/// <see cref="LibraryPage"/>, scopes it to the active machine's platform, and passes the search term.
/// </summary>
[Trait("Category", "Library")]
public sealed class RomMGatewayBrowseTests
{
    /// <summary>AC-BROWSE-01: a RomM page maps to items, total, offset and the char index.</summary>
    [Fact]
    [Trait("AC", "AC-BROWSE-01")]
    public async Task Page_Maps()
    {
        var ct = TestContext.Current.CancellationToken;
        var handler = new FakeRomMHandler(RomMFixtures.DefaultRouter);
        await using var client = RomMFixtures.Client(handler);
        var gateway = new RomMLibraryGateway(client);

        LibraryPage page = await gateway.BrowseAsync(new LibraryQuery(null, 15, 50, 0), ct);

        page.Total.Should().Be(2);
        page.Offset.Should().Be(0);
        page.Items.Should().HaveCount(2);

        page.Items[0].Name.Should().Be("Boulder Dash");
        page.Items[0].FileName.Should().Be("boulderdash.d64");
        page.Items[0].Launchable.Should().BeTrue();
        page.Items[0].SizeBytes.Should().Be(174848);
        page.Items[0].Cover!.Url.Should().Be("https://cdn.romm.local/101.png");
        page.Items[0].Cover!.Path.Should().Be("/assets/roms/101/cover/small.png");

        page.Items[1].FileName.Should().Be("hello.prg");
        page.Items[1].Launchable.Should().BeFalse();

        // RomM emits lowercase keys; the gateway normalizes to uppercase for the A-Z strip.
        page.CharIndex.Should().Contain(new KeyValuePair<string, int>("B", 0));
        page.CharIndex.Should().Contain(new KeyValuePair<string, int>("H", 1));
        page.CharIndex.Keys.Should().OnlyContain(k => k == k.ToUpperInvariant());
    }

    /// <summary>AC-BROWSE-02: the platform slug resolves (and caches) to a numeric id that scopes the query.</summary>
    [Fact]
    [Trait("AC", "AC-BROWSE-02")]
    public async Task PlatformSlug_ResolvesAndFilters()
    {
        var ct = TestContext.Current.CancellationToken;
        var handler = new FakeRomMHandler(RomMFixtures.DefaultRouter);
        await using var client = RomMFixtures.Client(handler);
        var gateway = new RomMLibraryGateway(client);

        int id = await gateway.ResolvePlatformIdAsync("c64", ct);
        id.Should().Be(15);

        // Cached: a second resolve does not hit /api/platforms again.
        await gateway.ResolvePlatformIdAsync("c64", ct);
        handler.Requests.Count(r => r.Uri.AbsolutePath == "/api/platforms").Should().Be(1);

        await gateway.BrowseAsync(new LibraryQuery(null, id, 50, 0), ct);
        FakeRomMHandler.Captured romsRequest = handler.Requests.First(r => r.Uri.AbsolutePath == "/api/roms");
        romsRequest.Uri.Query.Should().Contain("platform_ids=15");
    }

    /// <summary>AC-BROWSE-03: the search term is passed through to the RomM search_term parameter.</summary>
    [Fact]
    [Trait("AC", "AC-BROWSE-03")]
    public async Task SearchTerm_Passed()
    {
        var ct = TestContext.Current.CancellationToken;
        var handler = new FakeRomMHandler(RomMFixtures.DefaultRouter);
        await using var client = RomMFixtures.Client(handler);
        var gateway = new RomMLibraryGateway(client);

        await gateway.BrowseAsync(new LibraryQuery("boulder", 15, 50, 0), ct);

        FakeRomMHandler.Captured romsRequest = handler.Requests.First(r => r.Uri.AbsolutePath == "/api/roms");
        romsRequest.Uri.Query.Should().Contain("search_term=boulder");
    }
}
