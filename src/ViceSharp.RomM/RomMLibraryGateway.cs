using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using RomM.Client;
using RomM.Client.Models;
using ViceSharp.Library.ViewModels;

namespace ViceSharp.RomM;

/// <summary>
/// FR-ROMM-BROWSE/DETAIL/LAUNCH-001. The RomM adapter implementation of <see cref="IRomMLibraryGateway"/>.
/// Wraps an <see cref="IRomMClient"/>: browse/detail/download go through the typed clients; the cover
/// paths and the A-Z char index (which RomM.Client surfaces only as response extension data) are read
/// from that extension data and deserialized reflection-free via <see cref="RomMJsonContext"/>
/// (TR-ROMM-JSON-001).
/// </summary>
public sealed class RomMLibraryGateway : IRomMLibraryGateway
{
    private static readonly IReadOnlyDictionary<string, int> EmptyCharIndex = new Dictionary<string, int>();

    private static readonly JsonTypeInfo<Dictionary<string, int>> CharIndexTypeInfo =
        (JsonTypeInfo<Dictionary<string, int>>)RomMJsonContext.Default.GetTypeInfo(typeof(Dictionary<string, int>))!;

    private readonly IRomMClient _client;
    private readonly Dictionary<string, int> _platformIdCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _platformGate = new(1, 1);

    /// <summary>Creates the gateway over a constructed RomM client.</summary>
    /// <param name="client">The authenticated RomM client (built by the head from a connection).</param>
    public RomMLibraryGateway(IRomMClient client) =>
        _client = client ?? throw new ArgumentNullException(nameof(client));

    /// <inheritdoc />
    public async Task<int> ResolvePlatformIdAsync(string slug, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);

        await _platformGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_platformIdCache.TryGetValue(slug, out int cached))
            {
                return cached;
            }

            IReadOnlyList<PlatformSchema> platforms =
                await _client.Platforms.ListAsync(cancellationToken).ConfigureAwait(false);

            PlatformSchema? match = platforms.FirstOrDefault(p =>
                string.Equals(p.Slug, slug, StringComparison.OrdinalIgnoreCase)
                || string.Equals(p.FsSlug, slug, StringComparison.OrdinalIgnoreCase));

            if (match is null)
            {
                throw new InvalidOperationException($"RomM has no platform for slug '{slug}'.");
            }

            _platformIdCache[slug] = match.Id;
            return match.Id;
        }
        finally
        {
            _platformGate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<LibraryPage> BrowseAsync(LibraryQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        (string? orderBy, string? orderDir) = MapOrder(query.Order);
        var romQuery = new RomListQuery
        {
            SearchTerm = string.IsNullOrWhiteSpace(query.SearchTerm) ? null : query.SearchTerm,
            PlatformIds = new[] { query.PlatformId },
            Limit = query.Limit,
            Offset = query.Offset,
            OrderBy = orderBy,
            OrderDir = orderDir,
        };

        RomPage page = await _client.Roms.ListAsync(romQuery, cancellationToken).ConfigureAwait(false);

        var items = new List<RomTile>(page.Items.Count);
        foreach (SimpleRomSchema rom in page.Items)
        {
            items.Add(MapTile(rom));
        }

        return new LibraryPage(items, page.Total, page.Offset, ParseCharIndex(page.ExtensionData));
    }

    /// <inheritdoc />
    public async Task<RomDetail> GetRomAsync(int romId, CancellationToken cancellationToken = default)
    {
        DetailedRomSchema rom = await _client.Roms.GetAsync(romId, cancellationToken).ConfigureAwait(false);

        return new RomDetail(
            rom.Id,
            rom.Name ?? rom.FsName ?? string.Empty,
            rom.Summary,
            rom.PlatformSlug,
            ExtractCover(rom.ExtensionData),
            ExtractFiles(rom),
            Array.Empty<int>());
    }

    /// <inheritdoc />
    public async Task<AcquiredGame> DownloadAsync(
        int romId,
        string fileName,
        long expectedSizeBytes,
        string cacheDir,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(cacheDir);

        string dir = Path.Combine(cacheDir, romId.ToString(CultureInfo.InvariantCulture));
        Directory.CreateDirectory(dir);
        string dest = Path.Combine(dir, fileName);
        MediaKind kind = MediaExtensionMap.Resolve(fileName)?.Kind ?? MediaKind.Program;

        if (File.Exists(dest))
        {
            long length = new FileInfo(dest).Length;
            // Reuse when size matches the known size, or when size is unknown but a non-empty
            // cache entry already exists (Recents relaunch without a second download).
            if (length > 0 && (expectedSizeBytes <= 0 || length == expectedSizeBytes))
            {
                progress?.Report(1.0);
                return new AcquiredGame(dest, fileName, kind);
            }
        }

        await using Stream source = await _client.Roms
            .DownloadContentAsync(romId, fileName, cancellationToken).ConfigureAwait(false);

        await using (FileStream target = File.Create(dest))
        {
            byte[] buffer = new byte[81920];
            long copied = 0;
            int read;
            while ((read = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
            {
                await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                copied += read;
                if (expectedSizeBytes > 0)
                {
                    progress?.Report(Math.Min(1.0, (double)copied / expectedSizeBytes));
                }
            }
        }

        progress?.Report(1.0);
        return new AcquiredGame(dest, fileName, kind);
    }

    private static RomTile MapTile(SimpleRomSchema rom)
    {
        string fileName = rom.FsName ?? rom.Name ?? string.Empty;
        return new RomTile(
            rom.Id,
            rom.Name ?? fileName,
            fileName,
            rom.PlatformSlug,
            rom.FsSizeBytes,
            ExtractCover(rom.ExtensionData),
            MediaExtensionMap.IsLaunchable(fileName),
            GetString(rom.ExtensionData, "regions"),
            GetString(rom.ExtensionData, "languages"),
            GetString(rom.ExtensionData, "revision"));
    }

    private static (string? OrderBy, string? OrderDir) MapOrder(LibraryOrder order) => order switch
    {
        LibraryOrder.Name => ("name", "asc"),
        LibraryOrder.ReleaseDate => ("first_release_date", "asc"),
        LibraryOrder.Rating => ("average_rating", "desc"),
        LibraryOrder.Added => ("id", "desc"),
        _ => ("name", "asc"),
    };

    private static CoverRef? ExtractCover(IDictionary<string, JsonElement>? ext)
    {
        string? url = GetString(ext, "url_cover");
        string? path = GetString(ext, "path_cover_large") ?? GetString(ext, "path_cover_small");
        return url is null && path is null ? null : new CoverRef(url, path);
    }

    private static IReadOnlyList<RomFile> ExtractFiles(DetailedRomSchema rom)
    {
        if (rom.ExtensionData is not null
            && rom.ExtensionData.TryGetValue("files", out JsonElement filesElement)
            && filesElement.ValueKind == JsonValueKind.Array)
        {
            var files = new List<RomFile>();
            foreach (JsonElement file in filesElement.EnumerateArray())
            {
                string? name = GetString(file, "file_name") ?? GetString(file, "fs_name");
                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }

                long size = GetInt64(file, "file_size_bytes") ?? GetInt64(file, "fs_size_bytes") ?? 0;
                files.Add(new RomFile(
                    name,
                    size,
                    MediaExtensionMap.Resolve(name)?.Kind ?? MediaKind.Program,
                    MediaExtensionMap.IsLaunchable(name)));
            }

            if (files.Count > 0)
            {
                return files;
            }
        }

        // Fallback: synthesize a single file from the ROM's own fs_name.
        string fileName = rom.FsName ?? rom.Name ?? string.Empty;
        return fileName.Length == 0
            ? Array.Empty<RomFile>()
            : new[]
            {
                new RomFile(
                    fileName,
                    rom.FsSizeBytes ?? 0,
                    MediaExtensionMap.Resolve(fileName)?.Kind ?? MediaKind.Program,
                    MediaExtensionMap.IsLaunchable(fileName)),
            };
    }

    private static IReadOnlyDictionary<string, int> ParseCharIndex(IDictionary<string, JsonElement>? ext)
    {
        if (ext is null
            || !ext.TryGetValue("char_index", out JsonElement element)
            || element.ValueKind != JsonValueKind.Object)
        {
            return EmptyCharIndex;
        }

        try
        {
            Dictionary<string, int>? raw =
                JsonSerializer.Deserialize(element.GetRawText(), CharIndexTypeInfo);
            return NormalizeCharIndex(raw);
        }
        catch (JsonException)
        {
            return EmptyCharIndex;
        }
    }

    /// <summary>
    /// RomM 5.x emits lowercase letter keys (<c>a</c>..<c>z</c>). The A-Z strip and
    /// <see cref="LibraryBrowseViewModel.JumpToLetterAsync"/> look up uppercase keys, so normalize here.
    /// </summary>
    private static IReadOnlyDictionary<string, int> NormalizeCharIndex(IReadOnlyDictionary<string, int>? raw)
    {
        if (raw is null || raw.Count == 0)
        {
            return EmptyCharIndex;
        }

        var normalized = new Dictionary<string, int>(raw.Count, StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<string, int> pair in raw)
        {
            if (string.IsNullOrEmpty(pair.Key))
            {
                continue;
            }

            string key = pair.Key.ToUpperInvariant();
            // Prefer the earliest offset when the server emits both cases.
            if (!normalized.TryGetValue(key, out int existing) || pair.Value < existing)
            {
                normalized[key] = pair.Value;
            }
        }

        return normalized.Count == 0 ? EmptyCharIndex : normalized;
    }

    private static string? GetString(IDictionary<string, JsonElement>? ext, string key) =>
        ext is not null && ext.TryGetValue(key, out JsonElement el) && el.ValueKind == JsonValueKind.String
            ? el.GetString()
            : null;

    private static string? GetString(JsonElement obj, string key) =>
        obj.ValueKind == JsonValueKind.Object && obj.TryGetProperty(key, out JsonElement el) && el.ValueKind == JsonValueKind.String
            ? el.GetString()
            : null;

    private static long? GetInt64(JsonElement obj, string key) =>
        obj.ValueKind == JsonValueKind.Object
        && obj.TryGetProperty(key, out JsonElement el)
        && el.ValueKind == JsonValueKind.Number
        && el.TryGetInt64(out long v)
            ? v
            : null;
}
