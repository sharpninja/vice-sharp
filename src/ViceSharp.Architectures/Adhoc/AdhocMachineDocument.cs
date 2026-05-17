namespace ViceSharp.Architectures.Adhoc;

/// <summary>
/// Raw YAML/JSON shape for an ad-hoc machine architecture document.
/// This type maps 1:1 to the schema described in
/// <c>docs/schemas/machine-architecture.schema.md</c>; semantic validation
/// is performed by <see cref="AdhocMachineYamlLoader"/>.
/// </summary>
public sealed class AdhocMachineDocument
{
    public int? SchemaVersion { get; set; }

    public AdhocMachineSection? Machine { get; set; }

    public AdhocMemorySection? Memory { get; set; }

    public List<AdhocChipDefinition>? Chips { get; set; }

    public List<AdhocInterruptLineDefinition>? InterruptLines { get; set; }
}

public sealed class AdhocMachineSection
{
    public string? Name { get; set; }
    public string? VideoStandard { get; set; }
    public long? MasterClockHz { get; set; }
    public string? ResetVector { get; set; }
}

public sealed class AdhocMemorySection
{
    public List<AdhocMemoryRegion>? Regions { get; set; }
}

public sealed class AdhocMemoryRegion
{
    public string? Id { get; set; }
    public string? Kind { get; set; }
    public string? Start { get; set; }
    public string? End { get; set; }
    public int? Size { get; set; }
}

public sealed class AdhocChipDefinition
{
    public string? Id { get; set; }
    public string? Type { get; set; }
    public string? Role { get; set; }
    public string? BaseAddress { get; set; }
    public string? IrqLine { get; set; }
    public string? NmiLine { get; set; }
}

public sealed class AdhocInterruptLineDefinition
{
    public string? Id { get; set; }
    public string? Type { get; set; }
}
