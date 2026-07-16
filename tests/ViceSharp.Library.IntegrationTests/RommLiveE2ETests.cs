using System.Net.Http;
using FluentAssertions;
using ViceSharp.Library.ViewModels;
using ViceSharp.RomM;
using Xunit;

namespace ViceSharp.Library.IntegrationTests;

/// <summary>
/// PLAN-ROMM-001 Phase E ([V] validation tier). The executable acceptance for the automatable slice of
/// the on-device E2E: it drives the REAL RomM adapter/gateways against a live server (browse, resolve,
/// detail, download, collections round-trip, CSDb bridge). This is the gateway/VM half of AC-XUI-* /
/// AC-AUI-*; the GUI-render + gamepad half is validated on the physical Xbox (the runbook). Opt-in via
/// <see cref="RommLiveFixture.Enabled"/>; skips (never falsely passes) when no server is configured.
/// </summary>
[Trait("Category", "Integration")]
public sealed class RommLiveE2ETests : IClassFixture<RommLiveFixture>
{
    private readonly RommLiveFixture _fixture;

    /// <summary>Creates the test over the live fixture.</summary>
    /// <param name="fixture">The live RomM fixture.</param>
    public RommLiveE2ETests(RommLiveFixture fixture) => _fixture = fixture;

    /// <summary>The RomM heartbeat is reachable (proven by the fixture reachability gate).</summary>
    [Fact]
    [Trait("AC", "AC-CONN-01")]
    public void Heartbeat_Reachable()
    {
        Assert.SkipUnless(RommLiveFixture.Enabled, RommLiveFixture.SkipReason);
        _fixture.Client.Should().NotBeNull();
    }

    /// <summary>The active machine's slug resolves to a numeric platform id.</summary>
    [Fact]
    [Trait("AC", "AC-BROWSE-02")]
    public async Task ResolvePlatform_C64_ReturnsId()
    {
        Assert.SkipUnless(RommLiveFixture.Enabled, RommLiveFixture.SkipReason);
        CancellationToken ct = TestContext.Current.CancellationToken;

        int id = await _fixture.Library.ResolvePlatformIdAsync("c64", ct);

        id.Should().BeGreaterThan(0);
    }

    /// <summary>Browsing the C64 platform returns a well-formed page.</summary>
    [Fact]
    [Trait("AC", "AC-BROWSE-01")]
    public async Task Browse_C64_ReturnsPage()
    {
        Assert.SkipUnless(RommLiveFixture.Enabled, RommLiveFixture.SkipReason);
        CancellationToken ct = TestContext.Current.CancellationToken;

        int platformId = await _fixture.Library.ResolvePlatformIdAsync("c64", ct);
        LibraryPage page = await _fixture.Library.BrowseAsync(new LibraryQuery(null, platformId, 25, 0), ct);

        page.Should().NotBeNull();
        page.Total.Should().BeGreaterThanOrEqualTo(0);
        page.Offset.Should().Be(0);
    }

    /// <summary>The first launchable title's detail loads and its file downloads to the cache.</summary>
    [Fact]
    [Trait("AC", "AC-LAUNCH-01")]
    public async Task Detail_And_Download_FirstLaunchable()
    {
        Assert.SkipUnless(RommLiveFixture.Enabled, RommLiveFixture.SkipReason);
        CancellationToken ct = TestContext.Current.CancellationToken;

        int platformId = await _fixture.Library.ResolvePlatformIdAsync("c64", ct);
        LibraryPage page = await _fixture.Library.BrowseAsync(new LibraryQuery(null, platformId, 50, 0), ct);
        RomTile? tile = page.Items.FirstOrDefault(t => t.Launchable);
        if (tile is null)
        {
            Assert.Skip("The live C64 library has no launchable title to exercise the download path.");
            return;
        }

        RomDetail detail = await _fixture.Library.GetRomAsync(tile.Id, ct);
        detail.Files.Should().NotBeEmpty();

        string cacheDir = Path.Combine(Path.GetTempPath(), "vicesharp-romm-e2e");
        AcquiredGame acquired = await _fixture.Library.DownloadAsync(
            tile.Id, tile.FileName, tile.SizeBytes ?? 0, cacheDir, progress: null, cancellationToken: ct);

        File.Exists(acquired.LocalPath).Should().BeTrue();
        new FileInfo(acquired.LocalPath).Length.Should().BeGreaterThan(0);
    }

    /// <summary>A user collection can be created, listed, renamed, and deleted (round-trip).</summary>
    [Fact]
    [Trait("AC", "AC-COLLECT-02")]
    public async Task Collections_RoundTrip()
    {
        Assert.SkipUnless(RommLiveFixture.Enabled, RommLiveFixture.SkipReason);
        CancellationToken ct = TestContext.Current.CancellationToken;
        const string name = "vicesharp-e2e-list";
        const string renamed = "vicesharp-e2e-list-renamed";

        // Clean any leftover from a prior interrupted run so the round-trip starts fresh.
        IReadOnlyList<LibraryCollection> existing = await _fixture.Collections.GetCollectionsAsync(false, ct);
        foreach (LibraryCollection stale in existing.Where(c => c.Name == name || c.Name == renamed))
        {
            await _fixture.Collections.DeleteCollectionAsync(stale.Id, ct);
        }

        LibraryCollection created = await _fixture.Collections.CreateCollectionAsync(name, ct);
        created.Id.Should().BeGreaterThan(0);
        try
        {
            IReadOnlyList<LibraryCollection> listed = await _fixture.Collections.GetCollectionsAsync(false, ct);
            listed.Should().Contain(c => c.Id == created.Id);

            await _fixture.Collections.RenameCollectionAsync(created.Id, renamed, ct);
            IReadOnlyList<LibraryCollection> afterRename = await _fixture.Collections.GetCollectionsAsync(false, ct);
            afterRename.Should().Contain(c => c.Id == created.Id && c.Name == renamed);
        }
        finally
        {
            await _fixture.Collections.DeleteCollectionAsync(created.Id, ct);
        }
    }

    /// <summary>The CSDb bridge search endpoint answers (opt-in: needs VICESHARP_CSDB_BRIDGE_URL).</summary>
    [Fact]
    [Trait("AC", "AC-CSDB-04")]
    public async Task Csdb_Bridge_Search()
    {
        Assert.SkipUnless(RommLiveFixture.Enabled, RommLiveFixture.SkipReason);
        Assert.SkipUnless(
            !string.IsNullOrWhiteSpace(_fixture.BridgeUrl),
            "Set VICESHARP_CSDB_BRIDGE_URL to run the CSDb bridge E2E.");
        CancellationToken ct = TestContext.Current.CancellationToken;

        using var http = new HttpClient { BaseAddress = new Uri(_fixture.BridgeUrl!) };
        var gateway = new BridgeCsdbGateway(http, _fixture.Client.Tasks);

        IReadOnlyList<CsdbHit> hits = await gateway.SearchAsync("boulder", null, 10, ct);

        hits.Should().NotBeNull();
    }
}
