// PLAN-ROMM-001 X3 (IMPL-ROMM-017): the navigation handoff for the game-details page.
namespace ViceSharp.Xbox.RomM;

/// <summary>
/// PLAN-ROMM-001 (AC-XUI-05). The parameter LibraryPage passes to GameDetailsPage via
/// <c>Frame.Navigate</c>: enough to rebuild the RomM gateways and fetch the selected ROM's detail on the
/// details page (in-process, single session, so a plain reference handoff is sufficient).
/// </summary>
/// <param name="ServerUrl">The RomM server base URL.</param>
/// <param name="Token">The client API token, or <c>null</c> when unauthenticated.</param>
/// <param name="RomId">The selected ROM id to fetch and present.</param>
public sealed record GameDetailsRequest(string ServerUrl, string? Token, int RomId);
