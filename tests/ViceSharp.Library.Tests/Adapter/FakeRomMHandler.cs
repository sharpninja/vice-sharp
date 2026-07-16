using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RomM.Client;
using RomM.Client.Auth;

namespace ViceSharp.Library.Tests.Adapter;

/// <summary>
/// A canned <see cref="HttpMessageHandler"/> for driving <see cref="RomMClient"/> in tests. It records
/// each request (method, absolute URI, Authorization header) as it passes through, and returns a
/// response chosen by an absolute-path router supplied by the test.
/// </summary>
internal sealed class FakeRomMHandler : HttpMessageHandler
{
    public sealed record Captured(HttpMethod Method, Uri Uri, string? Authorization, string? Body);

    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

    public FakeRomMHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) => _responder = responder;

    public List<Captured> Requests { get; } = new();

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string? body = request.Content is null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        Requests.Add(new Captured(request.Method, request.RequestUri!, request.Headers.Authorization?.ToString(), body));
        return _responder(request);
    }

    /// <summary>Number of recorded requests whose absolute path starts with <paramref name="prefix"/>.</summary>
    public int CountPathPrefix(string prefix) =>
        Requests.Count(r => r.Uri.AbsolutePath.StartsWith(prefix, StringComparison.Ordinal));

    public static HttpResponseMessage Json(string json) =>
        new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    public static HttpResponseMessage Bytes(byte[] payload) =>
        new(HttpStatusCode.OK) { Content = new ByteArrayContent(payload) };

    public static HttpResponseMessage NotFound() => new(HttpStatusCode.NotFound);
}

/// <summary>Canned RomM 5.0.0-shaped responses plus a default router used across the adapter tests.</summary>
internal static class RomMFixtures
{
    public const string PlatformsJson =
        """[{"id":15,"slug":"c64","fs_slug":"c64","name":"Commodore 64","display_name":"Commodore 64","rom_count":3412}]""";

    public const string RomsPageJson =
        """
        {
          "items": [
            {"id":101,"name":"Boulder Dash","fs_name":"boulderdash.d64","platform_id":15,"platform_slug":"c64","fs_size_bytes":174848,"url_cover":"https://cdn.romm.local/101.png","path_cover_small":"/assets/roms/101/cover/small.png"},
            {"id":102,"name":"Hello","fs_name":"hello.prg","platform_id":15,"platform_slug":"c64","fs_size_bytes":200}
          ],
          "total": 2,
          "limit": 50,
          "offset": 0,
          "char_index": {"B":0,"H":1}
        }
        """;

    public const string DetailJson =
        """
        {"id":101,"name":"Boulder Dash","fs_name":"boulderdash.d64","platform_slug":"c64","fs_size_bytes":174848,"summary":"Dig diamonds, dodge boulders.","slug":"boulder-dash","url_cover":"https://cdn.romm.local/101.png","path_cover_large":"/assets/roms/101/cover/large.png","files":[{"file_name":"boulderdash.d64","file_size_bytes":174848}]}
        """;

    /// <summary>Routes platforms/roms/detail by absolute path; content paths return 404 (override per test).</summary>
    public static HttpResponseMessage DefaultRouter(HttpRequestMessage request)
    {
        string path = request.RequestUri!.AbsolutePath;
        return path switch
        {
            "/api/platforms" => FakeRomMHandler.Json(PlatformsJson),
            "/api/roms" => FakeRomMHandler.Json(RomsPageJson),
            "/api/roms/101" => FakeRomMHandler.Json(DetailJson),
            _ => FakeRomMHandler.NotFound(),
        };
    }

    public static IRomMClient Client(FakeRomMHandler handler, string token = "rmm_secrettoken") =>
        RomMClient.Create(
            new RomMClientOptions
            {
                BaseAddress = new Uri("https://romm.local/"),
                Auth = RomMAuth.ClientApiToken(token),
            },
            handler);
}
