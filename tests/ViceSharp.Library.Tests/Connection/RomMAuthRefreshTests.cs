using System.Net.Http;
using FluentAssertions;
using RomM.Client.Auth;
using ViceSharp.Library.Tests.Adapter;
using Xunit;

namespace ViceSharp.Library.Tests.Connection;

/// <summary>
/// FR-ROMM-CONN-001 (AC-CONN-03). Use case: an OAuth password session auto-refreshes near expiry so the
/// heads never hit an avoidable 401. Exercises the RomM.Client auth handler vice-sharp relies on.
/// </summary>
[Trait("Category", "Library")]
public sealed class RomMAuthRefreshTests
{
    private sealed class MutableTimeProvider : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = DateTimeOffset.UnixEpoch;

        public override DateTimeOffset GetUtcNow() => Now;
    }

    /// <summary>AC-CONN-03: a near-expiry token triggers a refresh (a second token request).</summary>
    [Fact]
    [Trait("AC", "AC-CONN-03")]
    public async Task NearExpiry_Refreshes()
    {
        var ct = TestContext.Current.CancellationToken;

        static HttpResponseMessage Router(HttpRequestMessage req) =>
            req.RequestUri!.AbsolutePath == "/api/token"
                ? FakeRomMHandler.Json("""{"access_token":"tok","token_type":"bearer","expires":60,"refresh_token":"ref"}""")
                : FakeRomMHandler.Json("{}");

        var inner = new FakeRomMHandler(Router);
        var time = new MutableTimeProvider();
        var authHandler = new RomMAuthHandler(RomMAuth.OAuthPassword("u", "p"), tokenStore: null, timeProvider: time)
        {
            InnerHandler = inner,
        };
        using var http = new HttpClient(authHandler) { BaseAddress = new Uri("https://romm.local/") };

        // First call acquires the token.
        (await http.GetAsync("api/heartbeat", ct)).Dispose();
        inner.Requests.Count(r => r.Uri.AbsolutePath == "/api/token").Should().Be(1);

        // Advance to within the 30s refresh skew of the 60s token.
        time.Now = time.Now.AddSeconds(40);

        // Second call refreshes.
        (await http.GetAsync("api/heartbeat", ct)).Dispose();
        inner.Requests.Count(r => r.Uri.AbsolutePath == "/api/token").Should().Be(2);
    }
}
