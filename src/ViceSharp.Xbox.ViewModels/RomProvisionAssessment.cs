namespace ViceSharp.Xbox.ViewModels;

using System.Collections.Generic;

/// <summary>
/// PLAN-XBOXUWP S28 (IMPL-XBOXUWP-028), area XROM. FR-XROM-001, TR-XPATH-001. The result of
/// evaluating first-run ROM provisioning: the overall state, the per-role breakdown, and
/// whether normal boot is blocked.
/// </summary>
/// <param name="State">The overall provisioning state.</param>
/// <param name="Roles">The per-role status for every core ROM role.</param>
/// <param name="IsBootBlocked">
/// Whether normal boot must be blocked. It is <c>false</c> only when <see cref="State"/> is
/// <see cref="RomProvisionState.Complete"/> (which already accounts for the Ultimax
/// kernal-optional rule); otherwise <c>true</c>.
/// </param>
public sealed record RomProvisionAssessment(
    RomProvisionState State,
    IReadOnlyList<RomRoleStatus> Roles,
    bool IsBootBlocked);
