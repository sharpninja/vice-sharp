namespace ViceSharp.Xbox.ViewModels;

/// <summary>
/// PLAN-XBOXUWP S28 (IMPL-XBOXUWP-028), area XROM. FR-XROM-001, TR-XPATH-001. The ROM
/// requirement profile the evaluator applies. It governs which roles are REQUIRED for a
/// bootable machine: a stock machine needs all three, an Ultimax cartridge overrides the
/// KERNAL and so makes it optional (mirrors the <c>kernal-none.bin</c> handling in
/// <c>C64RomLoader.cs:192-193</c>).
/// </summary>
public enum RomProfile
{
    /// <summary>Stock C64: BASIC, KERNAL and CHARGEN are all required.</summary>
    Standard = 0,

    /// <summary>Ultimax (max-mode cartridge): the KERNAL is supplied by the cartridge, so only BASIC and CHARGEN are required.</summary>
    Ultimax = 1,
}
