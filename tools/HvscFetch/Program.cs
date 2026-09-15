using System.Net.Http.Headers;
using System.Text.Json;
using SharpCompress.Archives;
using SharpCompress.Common;

// Download + normalize HVSC for RomM embedding (C# only; no Python/bash).
// Usage: HvscFetch [destDir]
// Env: HVSC_URL, HVSC_VERSION_API, HVSC_WORK_DIR, HVSC_SKIP_DOWNLOAD=1

var dest = args.Length > 0 ? Path.GetFullPath(args[0]) : "/romm/library/hvsc";
var work = Environment.GetEnvironmentVariable("HVSC_WORK_DIR") is { Length: > 0 } w
    ? Path.GetFullPath(w)
    : Path.Combine(Path.GetTempPath(), "hvsc-work");
var api = Environment.GetEnvironmentVariable("HVSC_VERSION_API")
    ?? "https://www.hvsc.c64.org/api/v1/version/7z";
var forcedUrl = Environment.GetEnvironmentVariable("HVSC_URL");
var skipDownload = Environment.GetEnvironmentVariable("HVSC_SKIP_DOWNLOAD") == "1";
var archivePath = Path.Combine(work, "hvsc-complete.bin");

Directory.CreateDirectory(work);
Directory.CreateDirectory(dest);

using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(30) };
http.DefaultRequestHeaders.UserAgent.ParseAdd("RomM-GB64-HVSC/1.0");

string resolvedUrl;
if (!skipDownload)
{
    resolvedUrl = await DiscoverUrlAsync(http, api, forcedUrl);
    Console.Error.WriteLine($"Downloading HVSC from: {resolvedUrl}");
    await DownloadAsync(http, resolvedUrl, archivePath);
}
else
{
    if (!File.Exists(archivePath))
    {
        Console.Error.WriteLine($"error: HVSC_SKIP_DOWNLOAD set but missing {archivePath}");
        return 1;
    }

    resolvedUrl = forcedUrl ?? "local-cache";
}

var rawDir = Path.Combine(work, "raw");
if (Directory.Exists(rawDir))
    Directory.Delete(rawDir, recursive: true);
Directory.CreateDirectory(rawDir);

Console.Error.WriteLine("Extracting archive...");
ExtractArchive(archivePath, rawDir);

var root = FindHvscRoot(rawDir);
Console.Error.WriteLine($"Detected HVSC content root: {root}");
InstallNormalized(root, dest, resolvedUrl);
Console.Error.WriteLine($"HVSC ready at {dest}");
return 0;

static async Task<string> DiscoverUrlAsync(HttpClient http, string api, string? forced)
{
    if (!string.IsNullOrWhiteSpace(forced))
        return forced!;

    string? version = null;
    string? official = null;
    try
    {
        var json = await http.GetStringAsync(api);
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("version", out var v))
            version = v.ToString();
        if (doc.RootElement.TryGetProperty("complete", out var c) &&
            c.TryGetProperty("url", out var u))
            official = u.GetString();
    }
    catch
    {
        // fall through to mirrors
    }

    version = string.IsNullOrWhiteSpace(version) ? "85" : version;
    var candidates = new List<string?>
    {
        official,
        $"https://hvsc.brona.dk/HVSC/HVSC_{version}-all-of-them.7z",
        $"https://hvsc.brona.dk/HVSC/HVSC_{version}-all-of-them.rar",
        $"https://boswme.home.xs4all.nl/HVSC/HVSC_{version}-all-of-them.7z",
    };

    foreach (var c in candidates.Where(x => !string.IsNullOrWhiteSpace(x) && x != "null"))
    {
        Console.Error.WriteLine($"Probing HVSC candidate: {c}");
        if (await UrlLooksLikeArchiveAsync(http, c!))
            return c!;
    }

    throw new InvalidOperationException(
        "Could not discover a working HVSC complete download URL. Set HVSC_URL.");
}

static async Task<bool> UrlLooksLikeArchiveAsync(HttpClient http, string url)
{
    try
    {
        using var req = new HttpRequestMessage(HttpMethod.Head, url);
        using var resp = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
        if (!resp.IsSuccessStatusCode && (int)resp.StatusCode is not (301 or 302 or 303 or 307 or 308))
            return false;
        var ctype = resp.Content.Headers.ContentType?.MediaType?.ToLowerInvariant() ?? "";
        if (ctype.Contains("html"))
            return false;
        if (ctype.Contains("7z") || ctype.Contains("zip") || ctype.Contains("rar") ||
            ctype.Contains("octet-stream"))
            return true;
        var len = resp.Content.Headers.ContentLength;
        return len is > 1_000_000;
    }
    catch
    {
        return false;
    }
}

static async Task DownloadAsync(HttpClient http, string url, string path)
{
    using var resp = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
    resp.EnsureSuccessStatusCode();
    await using (var fs = File.Create(path))
    await using (var stream = await resp.Content.ReadAsStreamAsync())
        await stream.CopyToAsync(fs);

    var info = new FileInfo(path);
    if (info.Length < 1_000_000)
        throw new InvalidOperationException($"Downloaded file too small ({info.Length} bytes).");

    await using (var fs = File.OpenRead(path))
    {
        var buf = new byte[15];
        var n = await fs.ReadAsync(buf);
        var head = System.Text.Encoding.ASCII.GetString(buf, 0, n).ToLowerInvariant();
        if (head.Contains("<!doctype") || head.Contains("<html"))
            throw new InvalidOperationException("Download returned HTML, not an archive.");
    }

    Console.Error.WriteLine($"Downloaded {info.Length} bytes");
}

static void ExtractArchive(string archivePath, string outDir)
{
    using var stream = File.OpenRead(archivePath);
    using var archive = ArchiveFactory.OpenArchive(stream);
    foreach (var entry in archive.Entries.Where(e => !e.IsDirectory))
    {
        var key = (entry.Key ?? "").Replace('\\', '/').TrimStart('/');
        if (key.Length == 0 || key.Contains("..", StringComparison.Ordinal))
            continue;
        var destPath = Path.Combine(outDir, key.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
        using var es = entry.OpenEntryStream();
        using var fs = File.Create(destPath);
        es.CopyTo(fs);
    }
}

static string FindHvscRoot(string raw)
{
    var hit = Directory.EnumerateDirectories(raw, "MUSICIANS", SearchOption.AllDirectories)
        .FirstOrDefault();
    if (hit is null)
        throw new InvalidOperationException("Extracted archive has no MUSICIANS/ directory.");
    return Path.GetDirectoryName(hit)!;
}

static void InstallNormalized(string root, string dest, string sourceUrl)
{
    if (Directory.Exists(dest))
        Directory.Delete(dest, recursive: true);
    Directory.CreateDirectory(dest);

    foreach (var d in new[] { "MUSICIANS", "GAMES", "DEMOS", "DOCUMENTS", "DOC", "STIL", "Update" })
    {
        var src = Path.Combine(root, d);
        if (!Directory.Exists(src))
            continue;
        Console.Error.WriteLine($"  + {d}/");
        CopyDirectory(src, Path.Combine(dest, d));
    }

    var marker = Path.Combine(dest, ".hvsc-origin.txt");
    File.WriteAllText(marker, string.Join('\n',
    [
        $"hvsc_root={dest}",
        $"installed_utc={DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}",
        $"source_url={sourceUrl}",
        "nfo_sid_resolve=join(hvsc_root, SID path with backslash to slash)",
        "example=MUSICIANS/W/Whittaker_David/180.sid",
        "",
    ]));

    var sidCount = Directory.EnumerateFiles(dest, "*.sid", SearchOption.AllDirectories).Count();
    Console.Error.WriteLine($"Installed SID count: {sidCount}");
    if (sidCount < 1000)
        throw new InvalidOperationException($"Unexpectedly few .sid files ({sidCount}).");
}

static void CopyDirectory(string source, string target)
{
    Directory.CreateDirectory(target);
    foreach (var file in Directory.GetFiles(source))
        File.Copy(file, Path.Combine(target, Path.GetFileName(file)), overwrite: true);
    foreach (var dir in Directory.GetDirectories(source))
        CopyDirectory(dir, Path.Combine(target, Path.GetFileName(dir)));
}
