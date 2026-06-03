using YamlDotNet.Serialization;

namespace ViceSharp.Architectures.Adhoc;

/// <summary>
/// Serialization entity for a machine-definition YAML document (schema v1 plus
/// the variant extensions: machine.id, systemCore, chip model/raster, rom).
/// Populated from a profile and written with YamlDotNet's source-generated
/// (AOT/trim-safe) serializer - see <see cref="MachineDefinitionYamlContext"/>.
/// Property names map to YAML keys via the camelCase naming convention;
/// nullable members are omitted when null so optional fields don't emit keys.
/// </summary>
[YamlSerializable]
public sealed class MachineDefinitionDocument
{
    public int SchemaVersion { get; set; } = 1;
    public MachineInfo Machine { get; set; } = new();
    public SystemCoreInfo? SystemCore { get; set; }
    public List<InterruptLineInfo> InterruptLines { get; set; } = [];
    public MemoryInfo Memory { get; set; } = new();
    public List<ChipInfo> Chips { get; set; } = [];
}

[YamlSerializable]
public sealed class MachineInfo
{
    public string? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string VideoStandard { get; set; } = string.Empty;
    public long MasterClockHz { get; set; }
    public string ResetVector { get; set; } = "0xFFFC";
}

[YamlSerializable]
public sealed class SystemCoreInfo
{
    public string Board { get; set; } = string.Empty;
    public string BusPolicy { get; set; } = string.Empty;
    public string AddressDecoderPolicy { get; set; } = string.Empty;
    public bool KeyboardConnected { get; set; }
    public bool TapePortConnected { get; set; }
    public bool IecBusConnected { get; set; }
    public bool Cia2Connected { get; set; }
    public bool CartridgeBootExpected { get; set; }
}

[YamlSerializable]
public sealed class InterruptLineInfo
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}

[YamlSerializable]
public sealed class MemoryInfo
{
    public List<MemoryRegionInfo> Regions { get; set; } = [];
}

[YamlSerializable]
public sealed class MemoryRegionInfo
{
    public string Id { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string Start { get; set; } = string.Empty;
    public string End { get; set; } = string.Empty;
    public RomSelectionInfo? Rom { get; set; }
}

[YamlSerializable]
public sealed class RomSelectionInfo
{
    public string System { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;

    /// <summary>A single pinned ROM dump (variant definitions). Mutually exclusive with Candidates.</summary>
    public string? File { get; set; }

    /// <summary>Ordered candidate dumps, most-preferred first (generic/fallback definitions).</summary>
    public List<string>? Candidates { get; set; }
}

[YamlSerializable]
public sealed class ChipInfo
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Model { get; set; }
    public string? Role { get; set; }
    public string? BaseAddress { get; set; }
    public string? IrqLine { get; set; }
    public string? NmiLine { get; set; }
    public int? CyclesPerLine { get; set; }
    public int? RasterLines { get; set; }
}
