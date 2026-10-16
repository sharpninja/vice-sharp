namespace ViceSharp.Protocol;

/// <summary>
/// Pure UI-state helper for VIC-20 system RAM (xvic -memory) presets and BLK toggles.
/// Shared by Avalonia and Xbox settings ViewModels. Bit layout matches Core
/// <c>Vic20RamBlocks</c>: Blk0=1, Blk1=2, Blk2=4, Blk3=8, Blk5=16.
/// </summary>
public sealed class Vic20MemoryUiState
{
    public const int Blk0Bit = 1;
    public const int Blk1Bit = 2;
    public const int Blk2Bit = 4;
    public const int Blk3Bit = 8;
    public const int Blk5Bit = 16;
    public const int AllBits = Blk0Bit | Blk1Bit | Blk2Bit | Blk3Bit | Blk5Bit;

    private int _blocks;

    public Vic20MemoryUiState(string? memorySpec = "none")
        => SetFromSpec(memorySpec);

    public int Blocks => _blocks;

    public string MemorySpec => FormatSpec(_blocks);

    public string PresetLabel => SettingsOptionCatalog.FromVic20MemoryId(MemorySpec) switch
    {
        "Custom" when _blocks == 0 => "Unexpanded",
        var label => label,
    };

    public bool Blk0
    {
        get => (_blocks & Blk0Bit) != 0;
        set => _ = TrySetBit(Blk0Bit, value);
    }

    public bool Blk1
    {
        get => (_blocks & Blk1Bit) != 0;
        set => _ = TrySetBit(Blk1Bit, value);
    }

    public bool Blk2
    {
        get => (_blocks & Blk2Bit) != 0;
        set => _ = TrySetBit(Blk2Bit, value);
    }

    public bool Blk3
    {
        get => (_blocks & Blk3Bit) != 0;
        set => _ = TrySetBit(Blk3Bit, value);
    }

    public bool Blk5
    {
        get => (_blocks & Blk5Bit) != 0;
        set => _ = TrySetBit(Blk5Bit, value);
    }

    /// <summary>Replaces the block map from an xvic-style memory spec. Returns whether the map changed.</summary>
    public bool SetFromSpec(string? memorySpec)
    {
        var next = ParseSpec(memorySpec);
        if (next == _blocks)
            return false;
        _blocks = next;
        return true;
    }

    /// <summary>
    /// Applies a catalog preset label. "Custom" is a no-op (toggles own the map).
    /// Returns whether the underlying block map changed.
    /// </summary>
    public bool SetFromPresetLabel(string? presetLabel)
    {
        var id = SettingsOptionCatalog.ToVic20MemoryId(presetLabel ?? "Unexpanded");
        if (id == "custom")
            return false;
        return SetFromSpec(id);
    }

    /// <summary>Sets one BLK flag. Returns whether the map changed.</summary>
    public bool TrySetBit(int bit, bool enabled)
    {
        var next = enabled ? (_blocks | bit) : (_blocks & ~bit);
        if (next == _blocks)
            return false;
        _blocks = next;
        return true;
    }

    public static int ParseSpec(string? spec)
    {
        if (string.IsNullOrWhiteSpace(spec))
            return 0;
        var t = spec.Trim().ToLowerInvariant();
        if (t is "none" or "")
            return 0;
        if (t == "all")
            return AllBits;
        if (t == "3k")
            return Blk0Bit;
        if (t == "8k")
            return Blk1Bit;
        if (t == "16k")
            return Blk1Bit | Blk2Bit;
        if (t == "24k")
            return Blk1Bit | Blk2Bit | Blk3Bit;

        var acc = 0;
        foreach (var part in t.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            acc |= part switch
            {
                "0" or "04" or "3k" => Blk0Bit,
                "1" or "20" or "8k" => Blk1Bit,
                "2" or "40" => Blk2Bit,
                "3" or "60" => Blk3Bit,
                "5" or "a0" => Blk5Bit,
                "all" => AllBits,
                "16k" => Blk1Bit | Blk2Bit,
                "24k" => Blk1Bit | Blk2Bit | Blk3Bit,
                _ => 0,
            };
        }

        return acc;
    }

    public static string FormatSpec(int blocks)
    {
        if (blocks == 0)
            return "none";
        if (blocks == AllBits)
            return "all";
        if (blocks == Blk0Bit)
            return "3k";
        if (blocks == Blk1Bit)
            return "8k";
        if (blocks == (Blk1Bit | Blk2Bit))
            return "16k";
        if (blocks == (Blk1Bit | Blk2Bit | Blk3Bit))
            return "24k";

        var parts = new List<string>(5);
        if ((blocks & Blk0Bit) != 0)
            parts.Add("04");
        if ((blocks & Blk1Bit) != 0)
            parts.Add("20");
        if ((blocks & Blk2Bit) != 0)
            parts.Add("40");
        if ((blocks & Blk3Bit) != 0)
            parts.Add("60");
        if ((blocks & Blk5Bit) != 0)
            parts.Add("a0");
        return parts.Count == 0 ? "none" : string.Join(',', parts);
    }
}
