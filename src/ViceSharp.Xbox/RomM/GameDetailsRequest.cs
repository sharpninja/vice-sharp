// PLAN-ROMM-001 X3 (IMPL-ROMM-017): the navigation handoff for the game-details page.
namespace ViceSharp.Xbox.RomM;

using System.Collections.Generic;
using ViceSharp.Library.ViewModels;

/// <summary>
/// PLAN-ROMM-001 (AC-XUI-05). The parameter LibraryPage passes to GameDetailsPage via
/// <c>Frame.Navigate</c>: enough to rebuild the RomM gateways, present the game group, and let the
/// user pick a specific language/region/revision variant to attach.
/// </summary>
/// <param name="ServerUrl">The RomM server base URL.</param>
/// <param name="Token">The client API token, or <c>null</c> when unauthenticated.</param>
/// <param name="GameName">The shared display name for the group.</param>
/// <param name="Variants">The ROM variants already known from the library grid (non-empty).</param>
public sealed record GameDetailsRequest(
    string ServerUrl,
    string? Token,
    string GameName,
    IReadOnlyList<RomTile> Variants);
