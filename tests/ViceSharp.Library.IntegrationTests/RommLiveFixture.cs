using RomM.Client;
using RomM.Client.Auth;
using ViceSharp.Library.ViewModels;
using ViceSharp.RomM;
using Xunit;

namespace ViceSharp.Library.IntegrationTests;

/// <summary>
/// PLAN-ROMM-001 Phase E ([V] validation tier). A live fixture over a REAL RomM server, opt-in via the
/// <c>VICESHARP_ROMM_INTEGRATION=1</c> environment variable. It reads the server URL / token / bridge
/// URL from the environment and, when enabled, verifies the heartbeat is reachable (a DOWN or
/// misconfigured server fails the whole class loudly - never a false pass) and builds the real gateways.
/// When not enabled every test skips with a clear reason, so the suite never silently reports coverage.
/// </summary>
/// <remarks>
/// Run it: <c>docker compose up</c> a RomM (and, for CSDb, the bridge), mint a Client API Token, then
/// <c>$env:VICESHARP_ROMM_INTEGRATION=1; $env:VICESHARP_ROMM_URL='http://host:8080/';
/// $env:VICESHARP_ROMM_TOKEN='rmm_...'; dotnet test tests\ViceSharp.Library.IntegrationTests</c>.
/// See docs/plans/PLAN-ROMM-phase-e-validation.md.
/// </remarks>
public sealed class RommLiveFixture : IAsyncLifetime
{
    /// <summary>The skip reason shown when the live suite is not opted in.</summary>
    public const string SkipReason =
        "Live RomM E2E is opt-in: set VICESHARP_ROMM_INTEGRATION=1 (and VICESHARP_ROMM_URL / VICESHARP_ROMM_TOKEN).";

    /// <summary>Whether the live suite is opted in.</summary>
    public static bool Enabled =>
        string.Equals(Environment.GetEnvironmentVariable("VICESHARP_ROMM_INTEGRATION"), "1", StringComparison.Ordinal);

    /// <summary>The RomM server base URL (defaults to the local compose port).</summary>
    public string BaseUrl { get; } =
        Environment.GetEnvironmentVariable("VICESHARP_ROMM_URL") ?? "http://localhost:8080/";

    /// <summary>The client API token, when supplied.</summary>
    public string? Token { get; } = Environment.GetEnvironmentVariable("VICESHARP_ROMM_TOKEN");

    /// <summary>The csdb-bridge base URL, when supplied (enables the CSDb E2E).</summary>
    public string? BridgeUrl { get; } = Environment.GetEnvironmentVariable("VICESHARP_CSDB_BRIDGE_URL");

    /// <summary>The live RomM client (only built when the suite is enabled).</summary>
    public IRomMClient Client { get; private set; } = null!;

    /// <summary>The live library gateway (only built when the suite is enabled).</summary>
    public RomMLibraryGateway Library { get; private set; } = null!;

    /// <summary>The live collections gateway (only built when the suite is enabled).</summary>
    public RomMCollectionsGateway Collections { get; private set; } = null!;

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        if (!Enabled)
        {
            return;
        }

        string baseUrl = BaseUrl;
        RomMAuth? auth = null;

        if (!string.IsNullOrWhiteSpace(Token))
        {
            // Explicit Client API Token.
            auth = RomMAuth.ClientApiToken(Token);
        }
        else if (!string.IsNullOrWhiteSpace(BridgeUrl))
        {
            // No token supplied: self-provision from the LAN bridge (GET /romm/v1/connection). The bridge
            // ensures a RomM user for the id and returns creds; authenticate via the OAuth password grant.
            string userId = Environment.GetEnvironmentVariable("VICESHARP_ROMM_USER_ID") ?? "vicesharp-e2e";
            RomMConnection? connection = await new RomMBridgeConnectionSource()
                .FetchAsync(new Uri(BridgeUrl), userId, CancellationToken.None)
                .ConfigureAwait(false);
            if (connection is not null)
            {
                // The bridge returns a per-user access token; use it as a bearer.
                baseUrl = connection.BaseUrl;
                auth = RomMAuth.ClientApiToken(connection.Token);
            }
        }

        var options = new RomMClientOptions { BaseAddress = new Uri(baseUrl) };
        if (auth is not null)
        {
            options.Auth = auth;
        }

        Client = RomMClient.Create(options);

        // Reachability gate: a down or misconfigured server fails the class loudly (never a false pass).
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        _ = await Client.System.GetHeartbeatAsync(cts.Token);

        Library = new RomMLibraryGateway(Client);
        Collections = new RomMCollectionsGateway(Client);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
