using FluentAssertions;
using ViceSharp.Library.ViewModels;
using ViceSharp.RomM;
using Xunit;

namespace ViceSharp.Library.Tests.Adapter;

/// <summary>
/// FR-ROMM-CONN-001 (AC-CONN-01, AC-CONN-06). Use case: every request carries the client API token as a
/// bearer header, and the token never leaks into a URL.
/// </summary>
[Trait("Category", "Library")]
public sealed class RomMGatewayAuthTests
{
    /// <summary>AC-CONN-01: the client API token is sent as an Authorization: Bearer header.</summary>
    [Fact]
    [Trait("AC", "AC-CONN-01")]
    public async Task ClientToken_SetsBearer()
    {
        var handler = new FakeRomMHandler(RomMFixtures.DefaultRouter);
        await using var client = RomMFixtures.Client(handler, token: "rmm_secrettoken");
        var gateway = new RomMLibraryGateway(client);

        await gateway.BrowseAsync(new LibraryQuery(null, 15, 50, 0), TestContext.Current.CancellationToken);

        handler.Requests.Should().NotBeEmpty();
        handler.Requests.Should().OnlyContain(r => r.Authorization == "Bearer rmm_secrettoken");
    }

    /// <summary>AC-CONN-06: the token never appears in any request URI.</summary>
    [Fact]
    [Trait("AC", "AC-CONN-06")]
    public async Task Token_NeverInUri()
    {
        var handler = new FakeRomMHandler(RomMFixtures.DefaultRouter);
        await using var client = RomMFixtures.Client(handler, token: "rmm_secrettoken");
        var gateway = new RomMLibraryGateway(client);

        var ct = TestContext.Current.CancellationToken;
        await gateway.ResolvePlatformIdAsync("c64", ct);
        await gateway.BrowseAsync(new LibraryQuery("boulder", 15, 50, 0), ct);

        handler.Requests.Should().OnlyContain(r => !r.Uri.AbsoluteUri.Contains("rmm_secrettoken"));
    }
}
