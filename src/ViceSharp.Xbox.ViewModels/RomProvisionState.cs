namespace ViceSharp.Xbox.ViewModels;

/// <summary>
/// PLAN-XBOXUWP S28 (IMPL-XBOXUWP-028), area XROM. FR-XROM-001, TR-XPATH-001. The overall
/// first-run provisioning state derived from the per-role presence of the core C64 ROMs.
/// </summary>
public enum RomProvisionState
{
    /// <summary>No core ROMs are present at all: a completely un-provisioned device.</summary>
    NotProvisioned = 0,

    /// <summary>Some (but not all required) core ROMs are present and none are invalid: a resumable partial import.</summary>
    Partial = 1,

    /// <summary>At least one present ROM is the wrong size or fails its pinned hash: a corrupt/wrong dump.</summary>
    Invalid = 2,

    /// <summary>Every required role is present and hash-valid: the machine may boot.</summary>
    Complete = 3,
}
