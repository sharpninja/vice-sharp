using System.Net.Http;
using FluentAssertions;
using RomM.Client.Auth;
using ViceSharp.Library.Tests.Adapter;
using ViceSharp.Library.ViewModels;
using ViceSharp.RomM;
using Xunit;

namespace ViceSharp.Library.Tests.Covers;

/// <summary>
/// FR-ROMM-COVER-001 (AC-COVER-01). Use case: public cover URLs must be fetched without auth, while
/// server-relative cover paths must carry the bearer token.
/// </summary>
[Trait("Category", "Library")]
public sealed class RomMCoverImageSourceTests
{
    /// <summary>AC-COVER-01: url_cover is fetched anonymously; path_cover carries the bearer.</summary>
    [Fact]
    [Trait("AC", "AC-COVER-01")]
    public async Task AuthRules()
    {
        var ct = TestContext.Current.CancellationToken;
        var anonHandler = new FakeRomMHandler(_ => FakeRomMHandler.Bytes(new byte[] { 1, 2, 3 }));
        var authHandler = new FakeRomMHandler(_ => FakeRomMHandler.Bytes(new byte[] { 4, 5, 6 }));

        using var anon = new HttpClient(anonHandler);
        using var auth = new HttpClient(new RomMAuthHandler(RomMAuth.ClientApiToken("tok")) { InnerHandler = authHandler })
        {
            BaseAddress = new Uri("https://romm.local/"),
        };
        var source = new RomMCoverImageSource(auth, anon);

        (await source.OpenCoverAsync(new CoverRef("https://cdn.romm.local/1.png", null), ct)).Dispose();
        (await source.OpenCoverAsync(new CoverRef(null, "/assets/roms/1/cover/small.png"), ct)).Dispose();

        anonHandler.Requests.Should().ContainSingle();
        anonHandler.Requests[0].Authorization.Should().BeNull();

        authHandler.Requests.Should().ContainSingle();
        authHandler.Requests[0].Authorization.Should().Be("Bearer tok");
    }
}
