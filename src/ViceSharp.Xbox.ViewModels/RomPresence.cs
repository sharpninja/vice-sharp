namespace ViceSharp.Xbox.ViewModels;

/// <summary>
/// PLAN-XBOXUWP S28 (IMPL-XBOXUWP-028), area XROM. FR-XROM-001, TR-XPATH-001. The per-role
/// presence classification the evaluator assigns to each core ROM after reading its file.
/// </summary>
public enum RomPresence
{
    /// <summary>The role's ROM file is absent from the C64 directory.</summary>
    Missing = 0,

    /// <summary>The role's ROM file is present with the expected size and matching pinned hash.</summary>
    Present = 1,

    /// <summary>The role's ROM file is present but the wrong size or fails its pinned hash.</summary>
    Invalid = 2,
}
