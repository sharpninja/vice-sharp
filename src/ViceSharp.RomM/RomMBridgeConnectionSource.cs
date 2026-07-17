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
    /// Fetches the RomM connection for the given Xbox user from the bridge: the bridge ensures a RomM user
    /// exists for <paramref name="xboxUserId"/> and returns the URL + username + password (the client then
    /// authenticates via the OAuth password grant). Returns <c>null</c> when the bridge declines (not
    /// same-subnet / sharing disabled / no token configured / missing user id) or is unreachable.
    /// </summary>
    /// <param name="bridgeBaseUrl">The csdb-bridge base URL (e.g. <c>http://host:8090/</c>).</param>
    /// <param name="xboxUserId">The Xbox user id to provision + log in as.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public async Task<RomMConnection?> FetchAsync(Uri bridgeBaseUrl, string xboxUserId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bridgeBaseUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(xboxUserId);

        using HttpClient http = _handler is null
            ? new HttpClient()
            : new HttpClient(_handler, disposeHandler: false);

        var endpoint = new Uri(bridgeBaseUrl, $"romm/v1/connection?user_id={Uri.EscapeDataString(xboxUserId)}");
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

    /// <summary>
    /// Parses a bridge connection body (<c>{ url, username, password }</c>) into a bridge-provisioned,
    /// OAuth-password <see cref="RomMConnection"/>, or <c>null</c> when incomplete.
    /// </summary>
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
            string? username = GetString(doc.RootElement, "username");
            // The password doubles as the shared token; accept either key.
            string? password = GetString(doc.RootElement, "password") ?? GetString(doc.RootElement, "token");
            if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return null;
            }

            return new RomMConnection(url, RomMAuthMode.SubnetShared, password, username);
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
