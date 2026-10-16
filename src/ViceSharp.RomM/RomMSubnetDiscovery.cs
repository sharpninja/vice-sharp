using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.Json;
using ViceSharp.Library.ViewModels;

namespace ViceSharp.RomM;

/// <summary>
/// FR-ROMM-CONN-001 (AC-CONN-07). The adapter implementation of <see cref="IRomMDiscovery"/>: it enumerates
/// the local /24 subnet(s) the machine sits on and probes each host's unauthenticated <c>/api/heartbeat</c>
/// concurrently, keeping the ones that answer with a RomM heartbeat (a <c>SYSTEM.VERSION</c> string). The
/// probe is a plain HTTP GET (no auth, no RomM.Client), parsed leniently with <see cref="JsonDocument"/> the
/// same way the CSDb bridge parses untyped responses, so no new source-gen DTO is needed.
/// </summary>
public sealed class RomMSubnetDiscovery : IRomMDiscovery
{
    private const int MaxConcurrentProbes = 32;

    private readonly HttpMessageHandler? _handler;
    private readonly IReadOnlyList<string>? _hostOverride;

    /// <summary>Creates the discovery service.</summary>
    /// <param name="handler">
    /// An optional message handler for the probes (a test seam); when <c>null</c> a default handler is used.
    /// </param>
    /// <param name="hosts">
    /// An optional explicit host list to probe (a test seam); when <c>null</c> the local subnet is enumerated.
    /// </param>
    public RomMSubnetDiscovery(HttpMessageHandler? handler = null, IReadOnlyList<string>? hosts = null)
    {
        _handler = handler;
        _hostOverride = hosts;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DiscoveredRomM>> ScanAsync(
        int port = 8080,
        TimeSpan? perHostTimeout = null,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        TimeSpan timeout = perHostTimeout ?? TimeSpan.FromSeconds(1);
        IReadOnlyList<string> hosts = _hostOverride ?? EnumerateSubnetHosts();

        using HttpClient http = _handler is null
            ? new HttpClient()
            : new HttpClient(_handler, disposeHandler: false);

        // The per-host timeout is enforced by a linked CTS per probe, so disable the client-wide timeout.
        http.Timeout = Timeout.InfiniteTimeSpan;

        var found = new List<DiscoveredRomM>();
        var gate = new SemaphoreSlim(MaxConcurrentProbes);
        var sync = new object();
        int scanned = 0;

        async Task ProbeAsync(string host)
        {
            await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                DiscoveredRomM? hit = await ProbeHostAsync(http, host, port, timeout, cancellationToken).ConfigureAwait(false);
                lock (sync)
                {
                    if (hit is not null)
                    {
                        found.Add(hit);
                    }

                    scanned++;
                    progress?.Report(scanned);
                }
            }
            finally
            {
                gate.Release();
            }
        }

        await Task.WhenAll(hosts.Select(ProbeAsync)).ConfigureAwait(false);
        return found.OrderBy(d => d.BaseUrl.Host, StringComparer.Ordinal).ToList();
    }

    private static async Task<DiscoveredRomM?> ProbeHostAsync(
        HttpClient http,
        string host,
        int port,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var baseUrl = new UriBuilder(Uri.UriSchemeHttp, host, port).Uri;
        var probe = new Uri(baseUrl, "api/heartbeat");

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        try
        {
            using HttpResponseMessage response = await http.GetAsync(probe, cts.Token).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            string json = await response.Content.ReadAsStringAsync(cts.Token).ConfigureAwait(false);
            return TryParseHeartbeat(json, baseUrl);
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or IOException)
        {
            // Host unreachable, not listening, or slower than the per-host budget: not a discovered server.
            return null;
        }
    }

    /// <summary>Parses a heartbeat body; returns a hit only when it is a RomM heartbeat (SYSTEM.VERSION).</summary>
    /// <param name="json">The response body.</param>
    /// <param name="baseUrl">The probed base URL.</param>
    /// <returns>The discovered server, or <c>null</c> when the body is not a RomM heartbeat.</returns>
    internal static DiscoveredRomM? TryParseHeartbeat(string json, Uri baseUrl)
    {
        try
        {
            using JsonDocument doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!doc.RootElement.TryGetProperty("SYSTEM", out JsonElement system) || system.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!system.TryGetProperty("VERSION", out JsonElement version) || version.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            string? name = null;
            if (doc.RootElement.TryGetProperty("FRONTEND", out JsonElement frontend)
                && frontend.ValueKind == JsonValueKind.Object
                && frontend.TryGetProperty("NAME", out JsonElement frontendName)
                && frontendName.ValueKind == JsonValueKind.String)
            {
                name = frontendName.GetString();
            }

            return new DiscoveredRomM(baseUrl, name, version.GetString());
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Enumerates the host addresses of every up, non-loopback IPv4 interface whose mask is /24 or wider on
    /// the last octet (255.255.255.x), skipping the machine's own address. Bounded to 254 hosts per subnet.
    /// </summary>
    private static IReadOnlyList<string> EnumerateSubnetHosts()
    {
        var hosts = new List<string>();

        foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (nic.OperationalStatus != OperationalStatus.Up || nic.NetworkInterfaceType == NetworkInterfaceType.Loopback)
            {
                continue;
            }

            foreach (UnicastIPAddressInformation address in nic.GetIPProperties().UnicastAddresses)
            {
                if (address.Address.AddressFamily != AddressFamily.InterNetwork)
                {
                    continue;
                }

                byte[] ip = address.Address.GetAddressBytes();
                byte[] mask = address.IPv4Mask.GetAddressBytes();

                // Only /24 (or a subnet sharing the first three octets); avoids scanning a whole /16.
                if (mask[0] != 255 || mask[1] != 255 || mask[2] != 255)
                {
                    continue;
                }

                for (int hostByte = 1; hostByte <= 254; hostByte++)
                {
                    if (hostByte == ip[3])
                    {
                        continue;
                    }

                    hosts.Add($"{ip[0]}.{ip[1]}.{ip[2]}.{hostByte}");
                }
            }
        }

        return hosts.Distinct(StringComparer.Ordinal).ToList();
    }
}
