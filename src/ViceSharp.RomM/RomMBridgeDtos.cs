using System.Text.Json.Serialization;

namespace ViceSharp.RomM;

/// <summary>
/// TR-ROMM-JSON-001. One entry in a csdb-bridge ingest request. The bridge (ASP.NET) binds camelCase,
/// so the property names are pinned with <see cref="JsonPropertyNameAttribute"/> to override the
/// context's SnakeCaseLower policy.
/// </summary>
internal sealed class RomMBridgeIngestItem
{
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = string.Empty;

    [JsonPropertyName("csdbId")]
    public int CsdbId { get; set; }
}

/// <summary>TR-ROMM-JSON-001. The csdb-bridge ingest request body (POST /csdb/v1/ingest).</summary>
internal sealed class RomMBridgeIngestRequest
{
    [JsonPropertyName("items")]
    public List<RomMBridgeIngestItem> Items { get; set; } = new();

    [JsonPropertyName("force")]
    public bool Force { get; set; }
}

/// <summary>TR-ROMM-JSON-001. One item in a csdb-bridge ingest response.</summary>
internal sealed class RomMBridgeIngestResponseItem
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }
}

/// <summary>TR-ROMM-JSON-001. The csdb-bridge ingest response body.</summary>
internal sealed class RomMBridgeIngestResponse
{
    [JsonPropertyName("requested")]
    public int Requested { get; set; }

    [JsonPropertyName("items")]
    public List<RomMBridgeIngestResponseItem>? Items { get; set; }
}
