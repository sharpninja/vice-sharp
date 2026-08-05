using System.Net;
using System.Net.Http;
using FluentAssertions;
using ViceSharp.Library.ViewModels;
using ViceSharp.RomM;
using Xunit;

namespace ViceSharp.Library.Tests.Adapter;

/// <summary>
/// FR-ROMM-COLLECT-001 (AC-COLLECT-01..04). Use case: the adapter lists, creates, renames, deletes, and
/// edits the membership of RomM server-side collections over the untyped endpoints.
/// </summary>
[Trait("Category", "Library")]
public sealed class RomMGatewayCollectionsTests
{
    /// <summary>AC-COLLECT-01: user collections plus smart/virtual, with smart/virtual flagged read-only.</summary>
    [Fact]
    [Trait("AC", "AC-COLLECT-01")]
    public async Task List_FlagsReadOnly()
    {
        var ct = TestContext.Current.CancellationToken;

        static HttpResponseMessage Router(HttpRequestMessage req) => req.RequestUri!.AbsolutePath switch
        {
            "/api/collections" => FakeRomMHandler.Json("""[{"id":1,"name":"Favorites","rom_count":12,"rom_ids":[10,11],"is_smart":false,"is_virtual":false}]"""),
            "/api/collections/smart" => FakeRomMHandler.Json("""[{"id":2,"name":"Recently Added","rom_count":50,"rom_ids":[],"is_smart":true,"is_virtual":false}]"""),
            "/api/collections/virtual" => FakeRomMHandler.Json("[]"),
            _ => FakeRomMHandler.NotFound(),
        };

        var handler = new FakeRomMHandler(Router);
        await using var client = RomMFixtures.Client(handler);
        var gateway = new RomMCollectionsGateway(client);

        IReadOnlyList<LibraryCollection> cols = await gateway.GetCollectionsAsync(includeSmartVirtual: true, ct);

        cols.Should().HaveCount(2);
        LibraryCollection favorites = cols.Single(c => c.Name == "Favorites");
        favorites.ReadOnly.Should().BeFalse();
        favorites.Count.Should().Be(12);
        favorites.RomIds.Should().Equal(10, 11);
        cols.Single(c => c.Name == "Recently Added").ReadOnly.Should().BeTrue();
    }

    /// <summary>
    /// RomM 5.x can return 422 for /api/collections/virtual; Lists auto-connect must still succeed.
    /// </summary>
    [Fact]
    [Trait("AC", "AC-COLLECT-01")]
    public async Task List_VirtualEndpoint422_IsIgnored()
    {
        var ct = TestContext.Current.CancellationToken;

        static HttpResponseMessage Router(HttpRequestMessage req) => req.RequestUri!.AbsolutePath switch
        {
            "/api/collections" => FakeRomMHandler.Json("""[{"id":1,"name":"Favorites","rom_count":1,"rom_ids":[10]}]"""),
            "/api/collections/smart" => FakeRomMHandler.Json("[]"),
            "/api/collections/virtual" => new HttpResponseMessage(HttpStatusCode.UnprocessableEntity)
            {
                Content = new StringContent("""{"detail":"not supported"}"""),
            },
            _ => FakeRomMHandler.NotFound(),
        };

        var handler = new FakeRomMHandler(Router);
        await using var client = RomMFixtures.Client(handler);
        var gateway = new RomMCollectionsGateway(client);

        IReadOnlyList<LibraryCollection> cols = await gateway.GetCollectionsAsync(includeSmartVirtual: true, ct);

        cols.Should().ContainSingle(c => c.Name == "Favorites");
    }

    /// <summary>AC-COLLECT-02: create POSTs to /api/collections and returns the created collection.</summary>
    [Fact]
    [Trait("AC", "AC-COLLECT-02")]
    public async Task Create_Posts()
    {
        var ct = TestContext.Current.CancellationToken;

        static HttpResponseMessage Router(HttpRequestMessage req) =>
            req.Method == HttpMethod.Post && req.RequestUri!.AbsolutePath == "/api/collections"
                ? FakeRomMHandler.Json("""{"id":3,"name":"New List","rom_count":0,"rom_ids":[]}""")
                : FakeRomMHandler.NotFound();

        var handler = new FakeRomMHandler(Router);
        await using var client = RomMFixtures.Client(handler);
        var gateway = new RomMCollectionsGateway(client);

        LibraryCollection created = await gateway.CreateCollectionAsync("New List", ct);

        created.Id.Should().Be(3);
        created.Name.Should().Be("New List");

        FakeRomMHandler.Captured post = handler.Requests.Single(r =>
            r.Method == HttpMethod.Post && r.Uri.AbsolutePath == "/api/collections");
        post.Body.Should().Contain("New List");
    }

    /// <summary>AC-COLLECT-03: add/remove send a CollectionRomsPayload on POST/DELETE .../roms.</summary>
    [Fact]
    [Trait("AC", "AC-COLLECT-03")]
    public async Task AddRemove_SendPayload()
    {
        var ct = TestContext.Current.CancellationToken;

        static HttpResponseMessage Router(HttpRequestMessage req) =>
            req.RequestUri!.AbsolutePath == "/api/collections/1/roms"
                ? FakeRomMHandler.Json("""{"id":1,"name":"Favorites","rom_count":13,"rom_ids":[10,11,12]}""")
                : FakeRomMHandler.NotFound();

        var handler = new FakeRomMHandler(Router);
        await using var client = RomMFixtures.Client(handler);
        var gateway = new RomMCollectionsGateway(client);

        await gateway.AddRomsAsync(1, new[] { 10, 11 }, ct);
        await gateway.RemoveRomsAsync(1, new[] { 10 }, ct);

        FakeRomMHandler.Captured add = handler.Requests.Single(r =>
            r.Method == HttpMethod.Post && r.Uri.AbsolutePath == "/api/collections/1/roms");
        add.Body.Should().Contain("rom_ids");
        add.Body.Should().Contain("10").And.Contain("11");

        FakeRomMHandler.Captured remove = handler.Requests.Single(r =>
            r.Method == HttpMethod.Delete && r.Uri.AbsolutePath == "/api/collections/1/roms");
        remove.Body.Should().Contain("rom_ids");
        remove.Body.Should().Contain("10");
    }

    /// <summary>AC-COLLECT-04: rename PUTs (with the current rom_ids) and delete DELETEs the collection.</summary>
    [Fact]
    [Trait("AC", "AC-COLLECT-04")]
    public async Task RenameDelete()
    {
        var ct = TestContext.Current.CancellationToken;

        static HttpResponseMessage Router(HttpRequestMessage req)
        {
            string path = req.RequestUri!.AbsolutePath;
            if (path == "/api/collections/1" && req.Method == HttpMethod.Get)
            {
                return FakeRomMHandler.Json("""{"id":1,"name":"Favorites","rom_count":2,"rom_ids":[10,11]}""");
            }

            if (path == "/api/collections/1" && req.Method == HttpMethod.Put)
            {
                return FakeRomMHandler.Json("""{"id":1,"name":"Renamed","rom_count":2,"rom_ids":[10,11]}""");
            }

            if (path == "/api/collections/1" && req.Method == HttpMethod.Delete)
            {
                return FakeRomMHandler.Json("{}");
            }

            return FakeRomMHandler.NotFound();
        }

        var handler = new FakeRomMHandler(Router);
        await using var client = RomMFixtures.Client(handler);
        var gateway = new RomMCollectionsGateway(client);

        await gateway.RenameCollectionAsync(1, "Renamed", ct);
        FakeRomMHandler.Captured put = handler.Requests.Single(r =>
            r.Method == HttpMethod.Put && r.Uri.AbsolutePath == "/api/collections/1");
        put.Body.Should().Contain("Renamed");
        put.Body.Should().Contain("rom_ids");

        await gateway.DeleteCollectionAsync(1, ct);
        handler.Requests.Should().Contain(r =>
            r.Method == HttpMethod.Delete && r.Uri.AbsolutePath == "/api/collections/1");
    }
}
