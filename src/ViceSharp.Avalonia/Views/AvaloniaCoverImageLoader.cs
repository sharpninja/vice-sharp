using System.Net.Http;
using System.Net.Http.Headers;
using Avalonia.Media.Imaging;
using ViceSharp.Library.ViewModels;
using ViceSharp.RomM;

namespace ViceSharp.Avalonia.Views;

/// <summary>
/// FR-ROMM-COVER-001 AC-COVER-01..04: desktop cover decoding over the shared authenticated
/// source/cache. Each returned bitmap belongs to its caller; the loader owns its HTTP clients.
/// </summary>
public sealed class AvaloniaCoverImageLoader : IDisposable
{
    private readonly CoverCache _cache;
    private readonly HttpClient _authenticated;
    private readonly HttpClient _anonymous;
    private bool _disposed;

    public AvaloniaCoverImageLoader(Uri serverUrl, string? token) : this(serverUrl, token, new HttpClientHandler(), new HttpClientHandler()) { }

    // Transport-only seam for deterministic auth/ownership tests. Production always uses two
    // separate handlers, so public CDN URLs cannot inherit server credentials.
    internal AvaloniaCoverImageLoader(Uri serverUrl, string? token, HttpMessageHandler authenticatedHandler, HttpMessageHandler anonymousHandler)
    {
        ArgumentNullException.ThrowIfNull(serverUrl);
        if (!serverUrl.IsAbsoluteUri || serverUrl.Scheme is not ("http" or "https"))
            throw new ArgumentException("An absolute HTTP(S) server URL is required.", nameof(serverUrl));

        _authenticated = new HttpClient(authenticatedHandler) { BaseAddress = serverUrl };
        if (!string.IsNullOrWhiteSpace(token))
            _authenticated.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Trim());
        _anonymous = new HttpClient(anonymousHandler);
        _cache = new CoverCache(new RomMCoverImageSource(_authenticated, _anonymous), []);
    }

    /// <summary>Returns an owned bitmap, or null for absent, canceled or invalid cover art.</summary>
    public async Task<Bitmap?> LoadCoverAsync(CoverRef? cover, CancellationToken cancellationToken = default)
    {
        if (cover is null || _disposed) return null;
        try
        {
            byte[] bytes = await _cache.GetAsync(cover, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (bytes.Length == 0) return null;
            using var stream = new MemoryStream(bytes, writable: false);
            return new Bitmap(stream);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _authenticated.Dispose();
        _anonymous.Dispose();
    }
}
