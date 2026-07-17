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
    /// AC-CONN-07. Auto-provisioned from a csdb-bridge on the same subnet (GET /romm/v1/connection): the
    /// bridge ensured a RomM user for the Xbox user id and returned its credentials, so the client did not
    /// have to pair or type anything. Authenticate via the OAuth password grant using
    /// <see cref="RomMConnection.Username"/> and <see cref="RomMConnection.Token"/> (the shared password).
    /// </summary>
    SubnetShared = 3,
}

/// <summary>
/// FR-ROMM-CONN-001. A persisted RomM connection: the server base URL, how its credential was obtained,
/// the credential itself, and (for OAuth-password / bridge-provisioned connections) the username to log
/// in with.
/// </summary>
/// <param name="BaseUrl">The RomM server base URL.</param>
/// <param name="AuthMode">How the credential was obtained.</param>
/// <param name="Token">The bearer token, or the password for OAuth-password / bridge-provisioned modes.</param>
/// <param name="Username">The login username for OAuth-password / bridge-provisioned modes; else <c>null</c>.</param>
public sealed record RomMConnection(string BaseUrl, RomMAuthMode AuthMode, string Token, string? Username = null);
