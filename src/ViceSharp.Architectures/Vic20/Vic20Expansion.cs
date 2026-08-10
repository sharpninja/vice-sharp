using CoreLayout = ViceSharp.Core.Vic20.Vic20MemoryLayout;
using CoreExpansion = ViceSharp.Core.Vic20.Vic20ExpansionKind;
using CoreBlocks = ViceSharp.Core.Vic20.Vic20RamBlocks;

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
/// Architecture-layer facade over <see cref="CoreLayout"/> (xvic RAM blocks).
/// </summary>
public static class Vic20MemoryLayout
{
    public const ushort UnexpandedScreenBase = CoreLayout.UnexpandedScreenBase;

    public static CoreBlocks ToBlocks(Vic20Expansion expansion)
        => CoreLayout.ToBlocks((CoreExpansion)(int)expansion);

    public static bool IsInstalledRam(Vic20Expansion expansion, ushort address)
        => CoreLayout.IsInstalledRam(ToBlocks(expansion), address);

    public static bool IsInstalledRam(CoreBlocks blocks, ushort address)
        => CoreLayout.IsInstalledRam(blocks, address);

    public static IReadOnlyList<(ushort Start, ushort End)> ExpansionRegions(Vic20Expansion expansion)
        => CoreLayout.ExpansionRegions(ToBlocks(expansion));

    public static IReadOnlyList<(ushort Start, ushort End)> ExpansionRegions(CoreBlocks blocks)
        => CoreLayout.ExpansionRegions(blocks);
}
