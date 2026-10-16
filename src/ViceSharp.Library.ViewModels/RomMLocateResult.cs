namespace ViceSharp.Library.ViewModels;

/// <summary>
/// FR-ROMM-CONN-001. Outcome of preferring a remembered RomM server before a LAN scan.
/// </summary>
/// <param name="BaseUrl">The server to use, or <c>null</c> when none is available.</param>
/// <param name="ScannedNetwork">
/// <c>true</c> when a subnet scan ran (no saved server, or the saved server did not respond).
/// </param>
/// <param name="SavedConnection">
/// The persisted connection when the remembered server is still reachable; otherwise <c>null</c>.
/// </param>
/// <param name="StatusMessage">A short status line for the connect UI.</param>
public sealed record RomMLocateResult(
    Uri? BaseUrl,
    bool ScannedNetwork,
    RomMConnection? SavedConnection,
    string StatusMessage);
