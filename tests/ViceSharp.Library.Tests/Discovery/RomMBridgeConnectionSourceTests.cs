using System.Net;
using System.Net.Http;
using FluentAssertions;
using ViceSharp.Library.Tests.Adapter;
using ViceSharp.Library.ViewModels;
using ViceSharp.RomM;
using Xunit;

namespace ViceSharp.Library.Tests.Discovery;

/// <summary>
/// FR-ROMM-CONN-001 (AC-CONN-07). Use case: a head discovers the csdb-bridge on the LAN and fetches the
/// RomM connection (URL + Client API Token) from its <c>/romm/v1/connection</c> endpoint, so it can
/// connect without a pairing code. A non-same-subnet caller gets 403 (-> null); a malformed body -> null.
/// </summary>
[Trait("Category", "Library")]
public sealed class RomMBridgeConnectionSourceTests
{
    [Fact]
    [Trait("AC", "AC-CONN-07")]
    public async Task Fetch_ReturnsConnection_FromBridge()
    {
        var ct = TestContext.Current.CancellationToken;

        static HttpResponseMessage Router(HttpRequestMessage req) =>
            req.RequestUri!.AbsolutePath == "/romm/v1/connection"
                ? FakeRomMHandler.Json("""{"url":"http://192.168.1.77:8080","token":"per-user-jwt"}""")
                : FakeRomMHandler.NotFound();

        var handler = new FakeRomMHandler(Router);
        var source = new RomMBridgeConnectionSource(handler);

        RomMConnection? conn = await source.FetchAsync(new Uri("http://192.168.1.77:8090/"), "xbox-user-1", ct);

        conn.Should().NotBeNull();
        conn!.BaseUrl.Should().Be("http://192.168.1.77:8080");
        conn.Token.Should().Be("per-user-jwt");
        conn.Username.Should().BeNull();
        conn.AuthMode.Should().Be(RomMAuthMode.SubnetShared);

        // The Xbox user id is passed to the bridge so it can provision + scope the per-user token.
        handler.Requests.Should().ContainSingle(r => r.Uri.Query.Contains("user_id=xbox-user-1"));
    }

    [Fact]
    [Trait("AC", "AC-CONN-07")]
    public async Task Fetch_Returns_Null_When_Forbidden()
    {
        var ct = TestContext.Current.CancellationToken;

        static HttpResponseMessage Router(HttpRequestMessage req) =>
            new(HttpStatusCode.Forbidden);

        var handler = new FakeRomMHandler(Router);
        var source = new RomMBridgeConnectionSource(handler);

        RomMConnection? conn = await source.FetchAsync(new Uri("http://192.168.1.77:8090/"), "xbox-user-1", ct);

        conn.Should().BeNull();
    }

    [Fact]
    [Trait("AC", "AC-CONN-07")]
    public async Task Fetch_Returns_Null_When_Body_Incomplete()
    {
        var ct = TestContext.Current.CancellationToken;

        static HttpResponseMessage Router(HttpRequestMessage req) =>
            FakeRomMHandler.Json("""{"url":"http://192.168.1.77:8080"}""");

        var handler = new FakeRomMHandler(Router);
        var source = new RomMBridgeConnectionSource(handler);

        RomMConnection? conn = await source.FetchAsync(new Uri("http://192.168.1.77:8090/"), "xbox-user-1", ct);

        conn.Should().BeNull();
    }
}
