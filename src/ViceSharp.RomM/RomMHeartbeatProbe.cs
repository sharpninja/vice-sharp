using ViceSharp.Library.ViewModels;

namespace ViceSharp.RomM;

/// <summary>
/// FR-ROMM-CONN-001. Unauthenticated <c>/api/heartbeat</c> probe for a single RomM base URL.
/// </summary>
public sealed class RomMHeartbeatProbe : IRomMServerProbe
{
    private readonly HttpMessageHandler? _handler;

    /// <summary>Creates the probe.</summary>
    /// <param name="handler">Optional HTTP handler (test seam).</param>
    public RomMHeartbeatProbe(HttpMessageHandler? handler = null) => _handler = handler;

    /// <inheritdoc />
    public async Task<bool> IsReachableAsync(
        Uri baseUrl,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(baseUrl);

        TimeSpan budget = timeout ?? TimeSpan.FromSeconds(2);
        using HttpClient http = _handler is null
            ? new HttpClient()
            : new HttpClient(_handler, disposeHandler: false);
        http.Timeout = Timeout.InfiniteTimeSpan;

        var probe = new Uri(baseUrl, "api/heartbeat");
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(budget);

        try
        {
            using HttpResponseMessage response = await http.GetAsync(probe, cts.Token).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            string json = await response.Content.ReadAsStringAsync(cts.Token).ConfigureAwait(false);
            return RomMSubnetDiscovery.TryParseHeartbeat(json, baseUrl) is not null;
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or IOException)
        {
            return false;
        }
    }
}
