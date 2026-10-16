using ViceSharp.Abstractions;

namespace ViceSharp.Architectures.Vic20;

/// <summary>
/// Required VIC-20 ROM set (BASIC, KERNAL, character generator).
/// </summary>
/// <remarks>
/// FR-PRF-005, FR-VIC20-002.
/// </remarks>
public sealed class Vic20RomSet : IRomSet
{
    public Vic20RomSet()
        : this(
            Vic20ViceRomNames.ArchitectureKey,
            Vic20ViceRomNames.Basic,
            Vic20ViceRomNames.KernalPal,
            Vic20ViceRomNames.Character)
    {
    }

    public Vic20RomSet(
        string architecture,
        string basicRomName,
        string kernalRomName,
        string characterRomName)
    {
        Architecture = string.IsNullOrWhiteSpace(architecture)
            ? Vic20ViceRomNames.ArchitectureKey
            : architecture;
        BasicRomName = string.IsNullOrWhiteSpace(basicRomName)
            ? Vic20ViceRomNames.Basic
            : basicRomName;
        KernalRomName = string.IsNullOrWhiteSpace(kernalRomName)
            ? Vic20ViceRomNames.KernalPal
            : kernalRomName;
        CharacterRomName = string.IsNullOrWhiteSpace(characterRomName)
            ? Vic20ViceRomNames.Character
            : characterRomName;
    }

    /// <inheritdoc />
    public string Architecture { get; }

    public string BasicRomName { get; }

    public string KernalRomName { get; }

    public string CharacterRomName { get; }

    /// <inheritdoc />
    public bool IsComplete(IRomProvider provider)
    {
        return provider.IsAvailable(BasicRomName, Architecture)
            && provider.IsAvailable(KernalRomName, Architecture)
            && provider.IsAvailable(CharacterRomName, Architecture);
    }
}
