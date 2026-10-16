namespace ViceSharp.Library.ViewModels;

/// <summary>
/// FR-ROMM-CONN-001 (AC-CONN-07). A RomM server found on the local network by <see cref="IRomMDiscovery"/>.
/// </summary>
/// <param name="BaseUrl">The server base URL (scheme + host + port), ready to hand to a connection.</param>
/// <param name="Name">The server's friendly name when the heartbeat exposes one, otherwise <c>null</c>.</param>
/// <param name="Version">The RomM version string from the heartbeat, when present.</param>
public sealed record DiscoveredRomM(Uri BaseUrl, string? Name, string? Version);
