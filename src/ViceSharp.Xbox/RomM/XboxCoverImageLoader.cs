// PLAN-ROMM-001 X2 (AC-COVER-01..04): the Xbox head's cover-image loader. #if HAS_UWP-guarded
// (it builds a WinRT BitmapImage).
#if HAS_UWP
namespace ViceSharp.Xbox.RomM;

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using ViceSharp.Library.ViewModels;
using ViceSharp.RomM;

/// <summary>
/// PLAN-ROMM-001 X2 (AC-COVER-01..04). The Xbox head's cover-image loader for the library grid:
/// fetches RomM cover art with the correct auth rule (anonymous CDN <c>url_cover</c>, bearer
/// <c>path_cover_*</c> via <see cref="RomMCoverImageSource"/>), de-duplicates + caps concurrent
/// fetches and caches the bytes (<see cref="CoverCache"/>), then decodes them into a UWP
/// <see cref="BitmapImage"/>. A missing cover or a failed fetch yields <c>null</c> so the tile
/// keeps its text fallback rather than showing a broken image.
/// </summary>
public sealed class XboxCoverImageLoader
{
    private readonly CoverCache _cache;

    /// <summary>Creates the loader for a connected server.</summary>
    /// <param name="serverUrl">The RomM server base URL (for bearer <c>path_cover_*</c> fetches).</param>
    /// <param name="token">The client API / per-user bearer token, or <c>null</c> for anonymous.</param>
    public XboxCoverImageLoader(Uri serverUrl, string? token)
    {
        ArgumentNullException.ThrowIfNull(serverUrl);

        var authenticated = new HttpClient { BaseAddress = serverUrl };
        if (!string.IsNullOrWhiteSpace(token))
            authenticated.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var anonymous = new HttpClient();
        var source = new RomMCoverImageSource(authenticated, anonymous);

        // An EMPTY placeholder is the "no image" sentinel: a failed fetch returns zero bytes and
        // LoadCoverAsync maps that to null so the tile shows its title/format fallback.
        _cache = new CoverCache(source, Array.Empty<byte>());
    }

    /// <summary>
    /// Loads the cover as a <see cref="BitmapImage"/>, or <c>null</c> when there is no cover, the
    /// fetch failed, or the load was cancelled. Must be called on the UI thread (it creates the
    /// BitmapImage); the network fetch itself runs off the caller via the cache.
    /// </summary>
    /// <param name="cover">The cover reference (may be <c>null</c>).</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public async Task<ImageSource?> LoadCoverAsync(CoverRef? cover, CancellationToken cancellationToken = default)
    {
        if (cover is null)
            return null;

        byte[] bytes;
        try
        {
            bytes = await _cache.GetAsync(cover, cancellationToken).ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            return null;
        }

        if (bytes.Length == 0)
            return null;

        var image = new BitmapImage();
        using var stream = new InMemoryRandomAccessStream();
        await stream.WriteAsync(bytes.AsBuffer());
        stream.Seek(0);
        await image.SetSourceAsync(stream);
        return image;
    }
}
#endif
