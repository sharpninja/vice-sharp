using CoreLayout = ViceSharp.Core.Vic20.Vic20MemoryLayout;
using CoreExpansion = ViceSharp.Core.Vic20.Vic20ExpansionKind;

namespace ViceSharp.Architectures.Vic20;

/// <summary>
/// VIC-20 RAM expansion configuration (aliases Core expansion kind).
/// </summary>
/// <remarks>FR-VIC20-002.</remarks>
public enum Vic20Expansion
{
    Unexpanded = 0,
    Exp3K = 3,
    Exp8K = 8,
    Exp16K = 16,
    Exp24K = 24,
    Exp32K = 32,
}

/// <summary>
/// Architecture-layer facade over <see cref="CoreLayout"/>.
/// </summary>
public static class Vic20MemoryLayout
{
    public const ushort UnexpandedScreenBase = CoreLayout.UnexpandedScreenBase;

    public static bool IsInstalledRam(Vic20Expansion expansion, ushort address)
        => CoreLayout.IsInstalledRam((CoreExpansion)(int)expansion, address);

    public static IReadOnlyList<(ushort Start, ushort End)> ExpansionRegions(Vic20Expansion expansion)
        => CoreLayout.ExpansionRegions((CoreExpansion)(int)expansion);
}
