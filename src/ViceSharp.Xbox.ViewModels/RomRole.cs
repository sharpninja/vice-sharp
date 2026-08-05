namespace ViceSharp.Xbox.ViewModels;

/// <summary>
/// PLAN-XBOXUWP S28 (IMPL-XBOXUWP-028), area XROM. FR-XROM-001/002, TR-XPATH-001. The
/// logical role of a core C64 ROM the first-run wizard provisions. The three roles map to
/// the three files VICE loads for a stock C64 (BASIC, KERNAL, character generator).
/// </summary>
public enum RomRole
{
    /// <summary>The BASIC interpreter ROM (VICE <c>basic-901226-01.bin</c>, 8&#160;KiB at $A000).</summary>
    Basic = 0,

    /// <summary>The KERNAL ROM (VICE <c>kernal-901227-03.bin</c>, 8&#160;KiB at $E000). Optional under Ultimax.</summary>
    Kernal = 1,

    /// <summary>The character-generator ROM (VICE <c>chargen-901225-01.bin</c>, 4&#160;KiB at $D000).</summary>
    Chargen = 2,
}
