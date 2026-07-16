namespace ViceSharp.Library.ViewModels;

/// <summary>FR-ROMM-CONN-001. How a RomM authentication credential was obtained.</summary>
public enum RomMAuthMode
{
    /// <summary>A Client API Token entered by the user.</summary>
    ClientToken = 0,

    /// <summary>An OAuth password-grant session (auto-refreshed).</summary>
    OAuthPassword = 1,

    /// <summary>A token obtained through the device-pairing flow.</summary>
    DevicePair = 2,

    /// <summary>
    /// AC-CONN-07. A Client API Token auto-provisioned from a csdb-bridge on the same subnet
    /// (GET /romm/v1/connection), so the client did not have to pair or type a token.
    /// </summary>
    SubnetShared = 3,
}

/// <summary>
/// FR-ROMM-CONN-001. A persisted RomM connection: the server base URL, how its token was obtained, and
/// the token itself.
/// </summary>
/// <param name="BaseUrl">The RomM server base URL.</param>
/// <param name="AuthMode">How the token was obtained.</param>
/// <param name="Token">The bearer token.</param>
public sealed record RomMConnection(string BaseUrl, RomMAuthMode AuthMode, string Token);
