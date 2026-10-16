using System.Collections.Concurrent;
using ViceSharp.Library.ViewModels;

namespace ViceSharp.RomM;

/// <summary>
/// FR-ROMM-COVER-001 (AC-COVER-02/03/04). A two-tier cover cache over an <see cref="ILibraryImageLoader"/>:
/// an in-memory byte cache backed by the loader, a concurrency gate to cap simultaneous fetches, and a
/// placeholder fallback so a failed fetch never throws. Cancellation propagates.
/// </summary>
public sealed class CoverCache
{
    private readonly ILibraryImageLoader _loader;
    private readonly byte[] _placeholder;
    private readonly SemaphoreSlim _gate;
    private readonly ConcurrentDictionary<string, byte[]> _cache = new(StringComparer.Ordinal);

    /// <summary>Creates the cache.</summary>
    /// <param name="loader">The underlying image loader.</param>
    /// <param name="placeholder">Bytes returned when a fetch fails.</param>
    /// <param name="maxConcurrency">The maximum simultaneous fetches (default 4).</param>
    public CoverCache(ILibraryImageLoader loader, byte[] placeholder, int maxConcurrency = 4)
    {
        _loader = loader ?? throw new ArgumentNullException(nameof(loader));
        _placeholder = placeholder ?? throw new ArgumentNullException(nameof(placeholder));
        MaxConcurrency = maxConcurrency > 0 ? maxConcurrency : 4;
        _gate = new SemaphoreSlim(MaxConcurrency, MaxConcurrency);
    }

    /// <summary>AC-COVER-03. The maximum number of simultaneous fetches.</summary>
    public int MaxConcurrency { get; }

    /// <summary>
    /// AC-COVER-02/04. Returns the cover bytes, fetching (gated) on a miss and caching the result; a
    /// fetch failure returns the placeholder (never throws). Cancellation propagates.
    /// </summary>
    /// <param name="cover">The cover reference.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public async Task<byte[]> GetAsync(CoverRef cover, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cover);

        string key = cover.Url ?? cover.Path ?? string.Empty;
        if (_cache.TryGetValue(key, out byte[]? cached))
        {
            return cached;
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_cache.TryGetValue(key, out cached))
            {
                return cached;
            }

            try
            {
                await using Stream stream = await _loader.OpenCoverAsync(cover, cancellationToken).ConfigureAwait(false);
                using var buffer = new MemoryStream();
                await stream.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
                byte[] bytes = buffer.ToArray();
                _cache[key] = bytes;
                return bytes;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // AC-COVER-04: a failed fetch yields the placeholder, never throws.
                return _placeholder;
            }
        }
        finally
        {
            _gate.Release();
        }
    }
}
