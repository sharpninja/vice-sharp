using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using RomM.Client;
using ViceSharp.Library.ViewModels;

namespace ViceSharp.RomM;

/// <summary>
/// FR-CSDB-001 (AC-CSDB-04). The bridge CSDb gateway: talks to the csdb-bridge sidecar (search + ingest)
/// for sandboxed/remote heads (Xbox) that cannot write the RomM roms root directly. The bridge writes the
/// files but does NOT scan, so this gateway triggers the RomM library scan itself via
/// <see cref="IRomMTasksClient"/>.
/// </summary>
public sealed class BridgeCsdbGateway : ICsdbGateway
{
    private static readonly JsonTypeInfo<RomMBridgeIngestRequest> RequestInfo =
        (JsonTypeInfo<RomMBridgeIngestRequest>)RomMJsonContext.Default.GetTypeInfo(typeof(RomMBridgeIngestRequest))!;

    private static readonly JsonTypeInfo<RomMBridgeIngestResponse> ResponseInfo =
        (JsonTypeInfo<RomMBridgeIngestResponse>)RomMJsonContext.Default.GetTypeInfo(typeof(RomMBridgeIngestResponse))!;

    private readonly HttpClient _bridge;
    private readonly IRomMTasksClient _tasks;

    /// <summary>Creates the gateway.</summary>
    /// <param name="bridgeHttpClient">An HttpClient whose base address is the csdb-bridge (e.g. :8090).</param>
    /// <param name="tasks">The RomM tasks client used to trigger the post-ingest scan.</param>
    public BridgeCsdbGateway(HttpClient bridgeHttpClient, IRomMTasksClient tasks)
    {
        _bridge = bridgeHttpClient ?? throw new ArgumentNullException(nameof(bridgeHttpClient));
        _tasks = tasks ?? throw new ArgumentNullException(nameof(tasks));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CsdbHit>> SearchAsync(
        string query,
        IReadOnlyList<CsdbKind>? kinds,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var url = new StringBuilder($"csdb/v1/search?q={Uri.EscapeDataString(query)}&limit={limit}&source=live");
        if (kinds is { Count: > 0 })
        {
            foreach (CsdbKind kind in kinds)
            {
                url.Append("&kinds=").Append(ToBridgeKind(kind));
            }
        }

        using HttpResponseMessage response = await _bridge.GetAsync(url.ToString(), cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        string json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return ParseSearch(json);
    }

    /// <inheritdoc />
    public async Task<CsdbIngestResult> IngestAndScanAsync(
        IReadOnlyList<CsdbSelection> selections,
        bool force,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(selections);

        var request = new RomMBridgeIngestRequest
        {
            Force = force,
            Items = selections.Select(s => new RomMBridgeIngestItem { Kind = ToBridgeKind(s.Kind), CsdbId = s.CsdbId }).ToList(),
        };

        string requestJson = JsonSerializer.Serialize(request, RequestInfo);
        using var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

        using HttpResponseMessage response = await _bridge
            .PostAsync("csdb/v1/ingest", content, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        string responseJson = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        RomMBridgeIngestResponse? parsed = JsonSerializer.Deserialize(responseJson, ResponseInfo);

        // The bridge writes the files but does not scan; trigger the RomM scan here.
        await _tasks.ScanLibraryAsync(cancellationToken).ConfigureAwait(false);

        return MapResult(parsed, selections.Count);
    }

    internal static CsdbIngestResult MapResult(RomMBridgeIngestResponse? response, int requested)
    {
        if (response?.Items is not { Count: > 0 } items)
        {
            return new CsdbIngestResult(requested, 0, 0, Scanned: true);
        }

        int ingested = 0;
        int skipped = 0;
        int failed = 0;
        foreach (RomMBridgeIngestResponseItem item in items)
        {
            switch (item.Status)
            {
                case "ok":
                    ingested++;
                    break;
                case "skipped":
                    skipped++;
                    break;
                default:
                    failed++;
                    break;
            }
        }

        return new CsdbIngestResult(ingested, skipped, failed, Scanned: true);
    }

    private static List<CsdbHit> ParseSearch(string json)
    {
        var hits = new List<CsdbHit>();
        using JsonDocument doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("results", out JsonElement results) || results.ValueKind != JsonValueKind.Array)
        {
            return hits;
        }

        foreach (JsonElement item in results.EnumerateArray())
        {
            int id = GetInt(item, "csdb_id") ?? GetInt(item, "csdbId") ?? 0;
            if (id <= 0)
            {
                continue;
            }

            string title = GetString(item, "title") ?? string.Empty;
            string? type = GetString(item, "csdb_type") ?? GetString(item, "csdbType");
            string? source = GetString(item, "source");
            string? pageUrl = GetString(item, "page_url") ?? GetString(item, "url");
            hits.Add(new CsdbHit(id, title, ParseKind(item), type, source, pageUrl));
        }

        return hits;
    }

    private static CsdbKind ParseKind(JsonElement item)
    {
        if (!item.TryGetProperty("kind", out JsonElement kind))
        {
            return CsdbKind.Other;
        }

        string text = kind.ValueKind switch
        {
            JsonValueKind.String => kind.GetString() ?? string.Empty,
            JsonValueKind.Number => kind.GetInt32().ToString(),
            _ => string.Empty,
        };

        return text.ToLowerInvariant() switch
        {
            "demo" or "0" => CsdbKind.Demo,
            "crack" or "1" => CsdbKind.Crack,
            "sid" or "2" => CsdbKind.Sid,
            _ => CsdbKind.Other,
        };
    }

    private static string ToBridgeKind(CsdbKind kind) => kind switch
    {
        CsdbKind.Demo => "demo",
        CsdbKind.Crack => "crack",
        CsdbKind.Sid => "sid",
        _ => "other",
    };

    private static string? GetString(JsonElement obj, string key) =>
        obj.TryGetProperty(key, out JsonElement el) && el.ValueKind == JsonValueKind.String ? el.GetString() : null;

    private static int? GetInt(JsonElement obj, string key) =>
        obj.TryGetProperty(key, out JsonElement el) && el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out int v)
            ? v
            : null;
}
