using System.Net.Http;
using System.Text.Json;
using ViceSharp.Library.ViewModels;

namespace ViceSharp.RomM;

/// <summary>
/// FR-ROMM-CONN-001 (AC-CONN-07). Fetches a RomM connection (server URL + Client API Token) from a
/// csdb-bridge on the LAN via its <c>GET /romm/v1/connection</c> endpoint, so a head can self-provision
/// without a pairing code or a typed token. The bridge only serves same-subnet callers, so a 403/404 (or
/// a malformed body) yields <c>null</c> and the head falls back to manual URL/token entry. The response is
/// parsed leniently with <see cref="JsonDocument"/> (no new source-gen DTO), matching the adapter's
/// untyped-response convention.
/// </summary>
public sealed class RomMBridgeConnectionSource
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(4);

    private readonly HttpMessageHandler? _handler;

    /// <summary>Creates the source.</summary>
    /// <param name="handler">An optional message handler (a test seam); a default handler is used when null.</param>
    public RomMBridgeConnectionSource(HttpMessageHandler? handler = null) => _handler = handler;

    /// <summary>
    /// Fetches the shared RomM connection from the bridge, or <c>null</c> when the bridge declines
    /// (not same-subnet / sharing disabled / no token configured) or is unreachable.
    /// </summary>
    /// <param name="bridgeBaseUrl">The csdb-bridge base URL (e.g. <c>http://host:8090/</c>).</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public async Task<RomMConnection?> FetchAsync(Uri bridgeBaseUrl, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bridgeBaseUrl);

        using HttpClient http = _handler is null
            ? new HttpClient()
            : new HttpClient(_handler, disposeHandler: false);

        var endpoint = new Uri(bridgeBaseUrl, "romm/v1/connection");
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(RequestTimeout);

        try
        {
            using HttpResponseMessage response = await http.GetAsync(endpoint, cts.Token).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            string json = await response.Content.ReadAsStringAsync(cts.Token).ConfigureAwait(false);
            return Parse(json);
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or IOException)
        {
            return null;
        }
    }

    /// <summary>Parses a bridge connection body into a <see cref="RomMConnection"/>, or <c>null</c> when incomplete.</summary>
    /// <param name="json">The response body.</param>
    internal static RomMConnection? Parse(string json)
    {
        try
        {
            using JsonDocument doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            string? url = GetString(doc.RootElement, "url");
            string? token = GetString(doc.RootElement, "token");
            if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            return new RomMConnection(url, RomMAuthMode.SubnetShared, token);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? GetString(JsonElement obj, string name) =>
        obj.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
