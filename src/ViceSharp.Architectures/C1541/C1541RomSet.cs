using ViceSharp.Abstractions;

namespace ViceSharp.Architectures.C1541;

/// <summary>
/// Required ROM set for a 1541-family drive machine. Resolves under the
/// "DRIVES" architecture key (matches VICE's native data layout).
/// </summary>
public sealed class C1541RomSet : IRomSet
{
    public C1541RomSet()
        : this(C1541ViceRomNames.ArchitectureKey, C1541ViceRomNames.Dos1541)
    {
    }

    public C1541RomSet(string architecture, string dosRomName)
    {
        Architecture = string.IsNullOrWhiteSpace(architecture)
            ? C1541ViceRomNames.ArchitectureKey
            : architecture;
        DosRomName = string.IsNullOrWhiteSpace(dosRomName)
            ? C1541ViceRomNames.Dos1541
            : dosRomName;
    }

    /// <inheritdoc />
    public string Architecture { get; }

    /// <summary>Filename of the drive DOS ROM image (16KB).</summary>
    public string DosRomName { get; }

    /// <inheritdoc />
    public bool IsComplete(IRomProvider provider)
        => provider.IsAvailable(DosRomName, Architecture);
}
