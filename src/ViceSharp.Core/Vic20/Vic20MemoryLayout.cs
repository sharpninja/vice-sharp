namespace ViceSharp.Core.Vic20;

/// <summary>
/// VIC-20 expansion sizes (kilobytes of expansion pack, not total system RAM).
/// </summary>
public enum Vic20ExpansionKind
{
    Unexpanded = 0,
    Exp3K = 3,
    Exp8K = 8,
    Exp16K = 16,
    Exp24K = 24,
    Exp32K = 32,
}

/// <summary>
/// Address-map helpers for VIC-20 base and expansion RAM.
/// </summary>
/// <remarks>FR-VIC20-002.</remarks>
public static class Vic20MemoryLayout
{
    public const ushort BaseLowRamEnd = 0x03FF;
    public const ushort Exp3KStart = 0x0400;
    public const ushort Exp3KEnd = 0x0FFF;
    public const ushort MainRamStart = 0x1000;
    public const ushort MainRamEnd = 0x1FFF;
    public const ushort Blk1Start = 0x2000;
    public const ushort Blk1End = 0x3FFF;
    public const ushort Blk2End = 0x5FFF;
    public const ushort Blk3End = 0x7FFF;
    public const ushort Blk5Start = 0xA000;
    public const ushort Blk5End = 0xBFFF;
    public const ushort UnexpandedScreenBase = 0x1E00;

    public static Vic20ExpansionKind ParseBoardModel(string? boardModel)
    {
        if (string.IsNullOrWhiteSpace(boardModel))
            return Vic20ExpansionKind.Unexpanded;

        return boardModel.Trim().ToUpperInvariant() switch
        {
            "EXP3K" or "3K" => Vic20ExpansionKind.Exp3K,
            "EXP8K" or "8K" => Vic20ExpansionKind.Exp8K,
            "EXP16K" or "16K" => Vic20ExpansionKind.Exp16K,
            "EXP24K" or "24K" => Vic20ExpansionKind.Exp24K,
            "EXP32K" or "32K" => Vic20ExpansionKind.Exp32K,
            _ => Vic20ExpansionKind.Unexpanded
        };
    }

    public static bool IsInstalledRam(Vic20ExpansionKind expansion, ushort address)
    {
        if (address <= BaseLowRamEnd)
            return true;
        if (address >= MainRamStart && address <= MainRamEnd)
            return true;

        foreach (var (start, end) in ExpansionRegions(expansion))
        {
            if (address >= start && address <= end)
                return true;
        }

        return false;
    }

    public static IReadOnlyList<(ushort Start, ushort End)> ExpansionRegions(Vic20ExpansionKind expansion)
        => expansion switch
        {
            Vic20ExpansionKind.Exp3K => [(Exp3KStart, Exp3KEnd)],
            Vic20ExpansionKind.Exp8K => [(Blk1Start, Blk1End)],
            Vic20ExpansionKind.Exp16K => [(Blk1Start, Blk2End)],
            Vic20ExpansionKind.Exp24K => [(Blk1Start, Blk3End)],
            Vic20ExpansionKind.Exp32K => [(Exp3KStart, Exp3KEnd), (Blk1Start, Blk3End), (Blk5Start, Blk5End)],
            _ => Array.Empty<(ushort, ushort)>()
        };
}
