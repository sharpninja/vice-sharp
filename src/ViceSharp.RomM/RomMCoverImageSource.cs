using ViceSharp.Library.ViewModels;

namespace ViceSharp.RomM;

/// <summary>
/// FR-ROMM-COVER-001 (AC-COVER-01). Opens RomM cover images with the correct auth rule: an absolute
/// <see cref="CoverRef.Url"/> (<c>url_cover</c>) is fetched WITHOUT authentication (it is a public CDN
/// asset), while a server-relative <see cref="CoverRef.Path"/> (<c>path_cover_*</c>) is fetched WITH the
/// bearer token via the authenticated client.
/// </summary>
public sealed class RomMCoverImageSource : ILibraryImageLoader
{
    private readonly HttpClient _authenticated;
    private readonly HttpClient _anonymous;

    /// <summary>Creates the cover source.</summary>
    /// <param name="authenticatedClient">A RomM-based, bearer-authenticated client (base = server URL).</param>
    /// <param name="anonymousClient">A plain client for public cover URLs (no Authorization header).</param>
    public RomMCoverImageSource(HttpClient authenticatedClient, HttpClient anonymousClient)
    {
        _authenticated = authenticatedClient ?? throw new ArgumentNullException(nameof(authenticatedClient));
        _anonymous = anonymousClient ?? throw new ArgumentNullException(nameof(anonymousClient));
    }

    /// <inheritdoc />
    public async Task<Stream> OpenCoverAsync(CoverRef cover, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cover);

        if (!string.IsNullOrEmpty(cover.Url))
        {
            return await _anonymous.GetStreamAsync(cover.Url, cancellationToken).ConfigureAwait(false);
        }

        if (!string.IsNullOrEmpty(cover.Path))
        {
            return await _authenticated.GetStreamAsync(cover.Path.TrimStart('/'), cancellationToken).ConfigureAwait(false);
        }

        throw new ArgumentException("Cover has neither a URL nor a path.", nameof(cover));
    }
}
