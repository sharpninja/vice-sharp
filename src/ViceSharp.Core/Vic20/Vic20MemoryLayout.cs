namespace ViceSharp.Core.Vic20;

/// <summary>
/// VIC-20 expansion sizes (kilobytes of expansion pack, not total system RAM).
/// Convenience presets over <see cref="Vic20RamBlocks"/> (xvic -memory).
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
/// VIC-20 system RAM expansion blocks (VICE RAMBlock0/1/2/3/5 / xvic -memory).
/// </summary>
/// <remarks>
/// FR-VIC20-002. Aligns with VICE <c>vic20-cmdline-options.c</c> cmdline_memory:
/// BLK0 $0400-$0FFF, BLK1 $2000-$3FFF, BLK2 $4000-$5FFF, BLK3 $6000-$7FFF,
/// BLK5 $A000-$BFFF. Base low RAM and main $1000-$1FFF are always installed.
/// </remarks>
[Flags]
public enum Vic20RamBlocks : byte
{
    None = 0,
    /// <summary>BLK0 / 3k: $0400-$0FFF.</summary>
    Blk0 = 1 << 0,
    /// <summary>BLK1 / 8k: $2000-$3FFF.</summary>
    Blk1 = 1 << 1,
    /// <summary>BLK2: $4000-$5FFF.</summary>
    Blk2 = 1 << 2,
    /// <summary>BLK3: $6000-$7FFF.</summary>
    Blk3 = 1 << 3,
    /// <summary>BLK5: $A000-$BFFF.</summary>
    Blk5 = 1 << 4,
    /// <summary>All expansion blocks (xvic -memory all).</summary>
    All = Blk0 | Blk1 | Blk2 | Blk3 | Blk5,
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
    public const ushort Blk2Start = 0x4000;
    public const ushort Blk2End = 0x5FFF;
    public const ushort Blk3Start = 0x6000;
    public const ushort Blk3End = 0x7FFF;
    public const ushort Blk5Start = 0xA000;
    public const ushort Blk5End = 0xBFFF;
    public const ushort UnexpandedScreenBase = 0x1E00;

    /// <summary>Maps legacy expansion presets to RAM block flags.</summary>
    public static Vic20RamBlocks ToBlocks(Vic20ExpansionKind expansion)
        => expansion switch
        {
            Vic20ExpansionKind.Exp3K => Vic20RamBlocks.Blk0,
            Vic20ExpansionKind.Exp8K => Vic20RamBlocks.Blk1,
            Vic20ExpansionKind.Exp16K => Vic20RamBlocks.Blk1 | Vic20RamBlocks.Blk2,
            Vic20ExpansionKind.Exp24K => Vic20RamBlocks.Blk1 | Vic20RamBlocks.Blk2 | Vic20RamBlocks.Blk3,
            Vic20ExpansionKind.Exp32K => Vic20RamBlocks.All,
            _ => Vic20RamBlocks.None,
        };

    /// <summary>
    /// Maps exact block combinations back to a legacy preset when possible;
    /// non-preset combinations return <see cref="Vic20ExpansionKind.Unexpanded"/>
    /// (callers should prefer <see cref="Vic20RamBlocks"/> for custom maps).
    /// </summary>
    public static Vic20ExpansionKind ToExpansionKind(Vic20RamBlocks blocks)
        => blocks switch
        {
            Vic20RamBlocks.None => Vic20ExpansionKind.Unexpanded,
            Vic20RamBlocks.Blk0 => Vic20ExpansionKind.Exp3K,
            Vic20RamBlocks.Blk1 => Vic20ExpansionKind.Exp8K,
            Vic20RamBlocks.Blk1 | Vic20RamBlocks.Blk2 => Vic20ExpansionKind.Exp16K,
            Vic20RamBlocks.Blk1 | Vic20RamBlocks.Blk2 | Vic20RamBlocks.Blk3 => Vic20ExpansionKind.Exp24K,
            Vic20RamBlocks.All => Vic20ExpansionKind.Exp32K,
            _ => Vic20ExpansionKind.Unexpanded,
        };

    /// <summary>
    /// Parses an xvic <c>-memory</c> style string (comma-separated tokens).
    /// Returns false when any token is unsupported (VICE logs error and fails).
    /// </summary>
    public static bool TryParseMemorySpec(string? spec, out Vic20RamBlocks blocks)
    {
        blocks = Vic20RamBlocks.None;
        if (spec is null)
            return true;

        var trimmed = spec.Trim();
        if (trimmed.Length == 0 || trimmed.Equals("none", StringComparison.OrdinalIgnoreCase))
            return true;

        // Empty token list from "" only; a lone comma is invalid.
        var parts = trimmed.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return true;

        Vic20RamBlocks acc = Vic20RamBlocks.None;
        foreach (var part in parts)
        {
            if (!TryParseMemoryToken(part, out var tokenBlocks))
            {
                blocks = Vic20RamBlocks.None;
                return false;
            }

            // "all" replaces prior bits (same as VICE assigning VIC_BLK_ALL).
            if (tokenBlocks == Vic20RamBlocks.All && part.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                acc = Vic20RamBlocks.All;
                continue;
            }

            acc |= tokenBlocks;
        }

        blocks = acc;
        return true;
    }

    /// <summary>Parses or throws <see cref="ArgumentException"/>.</summary>
    public static Vic20RamBlocks ParseMemorySpec(string? spec)
    {
        if (TryParseMemorySpec(spec, out var blocks))
            return blocks;
        throw new ArgumentException($"Unsupported memory extension option: '{spec}'.", nameof(spec));
    }

    /// <summary>
    /// Canonical xvic-ish form: known presets as none/3k/8k/16k/24k/all;
    /// otherwise comma-separated address tokens (04,20,40,60,a0) in block order.
    /// </summary>
    public static string FormatMemorySpec(Vic20RamBlocks blocks)
    {
        if (blocks == Vic20RamBlocks.None)
            return "none";
        if (blocks == Vic20RamBlocks.All)
            return "all";
        if (blocks == Vic20RamBlocks.Blk0)
            return "3k";
        if (blocks == Vic20RamBlocks.Blk1)
            return "8k";
        if (blocks == (Vic20RamBlocks.Blk1 | Vic20RamBlocks.Blk2))
            return "16k";
        if (blocks == (Vic20RamBlocks.Blk1 | Vic20RamBlocks.Blk2 | Vic20RamBlocks.Blk3))
            return "24k";

        var parts = new List<string>(5);
        if ((blocks & Vic20RamBlocks.Blk0) != 0)
            parts.Add("04");
        if ((blocks & Vic20RamBlocks.Blk1) != 0)
            parts.Add("20");
        if ((blocks & Vic20RamBlocks.Blk2) != 0)
            parts.Add("40");
        if ((blocks & Vic20RamBlocks.Blk3) != 0)
            parts.Add("60");
        if ((blocks & Vic20RamBlocks.Blk5) != 0)
            parts.Add("a0");
        return parts.Count == 0 ? "none" : string.Join(',', parts);
    }

    public static Vic20ExpansionKind ParseBoardModel(string? boardModel)
    {
        if (string.IsNullOrWhiteSpace(boardModel))
            return Vic20ExpansionKind.Unexpanded;

        var t = boardModel.Trim();
        // xvic -memory strings on BoardModel (future settings path).
        if (TryParseMemorySpec(t, out var fromSpec)
            && (t.Contains(',')
                || t.Equals("none", StringComparison.OrdinalIgnoreCase)
                || t.Equals("all", StringComparison.OrdinalIgnoreCase)
                || t.Equals("3k", StringComparison.OrdinalIgnoreCase)
                || t.Equals("8k", StringComparison.OrdinalIgnoreCase)
                || t.Equals("16k", StringComparison.OrdinalIgnoreCase)
                || t.Equals("24k", StringComparison.OrdinalIgnoreCase)))
        {
            return ToExpansionKind(fromSpec);
        }

        return t.ToUpperInvariant() switch
        {
            "EXP3K" or "3K" => Vic20ExpansionKind.Exp3K,
            "EXP8K" or "8K" => Vic20ExpansionKind.Exp8K,
            "EXP16K" or "16K" => Vic20ExpansionKind.Exp16K,
            "EXP24K" or "24K" => Vic20ExpansionKind.Exp24K,
            "EXP32K" or "32K" => Vic20ExpansionKind.Exp32K,
            "UNEXPANDED" => Vic20ExpansionKind.Unexpanded,
            _ => Vic20ExpansionKind.Unexpanded
        };
    }

    public static bool IsInstalledRam(Vic20ExpansionKind expansion, ushort address)
        => IsInstalledRam(ToBlocks(expansion), address);

    public static bool IsInstalledRam(Vic20RamBlocks blocks, ushort address)
    {
        if (address <= BaseLowRamEnd)
            return true;
        if (address >= MainRamStart && address <= MainRamEnd)
            return true;

        foreach (var (start, end) in ExpansionRegions(blocks))
        {
            if (address >= start && address <= end)
                return true;
        }

        return false;
    }

    public static IReadOnlyList<(ushort Start, ushort End)> ExpansionRegions(Vic20ExpansionKind expansion)
        => ExpansionRegions(ToBlocks(expansion));

    public static IReadOnlyList<(ushort Start, ushort End)> ExpansionRegions(Vic20RamBlocks blocks)
    {
        if (blocks == Vic20RamBlocks.None)
            return Array.Empty<(ushort, ushort)>();

        var regions = new List<(ushort, ushort)>(5);
        if ((blocks & Vic20RamBlocks.Blk0) != 0)
            regions.Add((Exp3KStart, Exp3KEnd));
        if ((blocks & Vic20RamBlocks.Blk1) != 0)
            regions.Add((Blk1Start, Blk1End));
        if ((blocks & Vic20RamBlocks.Blk2) != 0)
            regions.Add((Blk2Start, Blk2End));
        if ((blocks & Vic20RamBlocks.Blk3) != 0)
            regions.Add((Blk3Start, Blk3End));
        if ((blocks & Vic20RamBlocks.Blk5) != 0)
            regions.Add((Blk5Start, Blk5End));
        return regions;
    }

    private static bool TryParseMemoryToken(string token, out Vic20RamBlocks blocks)
    {
        blocks = Vic20RamBlocks.None;
        if (token.Length == 0)
            return true;

        switch (token.ToLowerInvariant())
        {
            case "none":
                return true;
            case "all":
                blocks = Vic20RamBlocks.All;
                return true;
            case "3k":
                blocks = Vic20RamBlocks.Blk0;
                return true;
            case "8k":
                blocks = Vic20RamBlocks.Blk1;
                return true;
            case "16k":
                blocks = Vic20RamBlocks.Blk1 | Vic20RamBlocks.Blk2;
                return true;
            case "24k":
                blocks = Vic20RamBlocks.Blk1 | Vic20RamBlocks.Blk2 | Vic20RamBlocks.Blk3;
                return true;
            case "0":
            case "04":
                blocks = Vic20RamBlocks.Blk0;
                return true;
            case "1":
            case "20":
                blocks = Vic20RamBlocks.Blk1;
                return true;
            case "2":
            case "40":
                blocks = Vic20RamBlocks.Blk2;
                return true;
            case "3":
            case "60":
                blocks = Vic20RamBlocks.Blk3;
                return true;
            case "5":
            case "a0":
                blocks = Vic20RamBlocks.Blk5;
                return true;
            default:
                return false;
        }
    }
}
