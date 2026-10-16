namespace ViceSharp.Xbox.ViewModels;

/// <summary>
/// PLAN-XBOXUWP S28 (IMPL-XBOXUWP-028), area XROM. FR-XROM-001, TR-XPATH-001. The evaluated
/// status of one core ROM role: its presence classification, the file name it was looked up
/// under, and whether the role is required under the active profile.
/// </summary>
/// <param name="Role">The core ROM role.</param>
/// <param name="FileName">The file name the role was looked up under in the C64 directory.</param>
/// <param name="Presence">The presence classification (missing / present / invalid).</param>
/// <param name="IsRequired">Whether this role is required for a bootable machine under the active profile.</param>
public sealed record RomRoleStatus(
    RomRole Role,
    string FileName,
    RomPresence Presence,
    bool IsRequired);
