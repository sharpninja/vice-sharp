namespace ViceSharp.Architectures.Multisystem;

/// <summary>
/// YAML DTO for a multi-system topology document. Parsed by
/// MultiSystemYamlLoader; validated into MultiSystemBlueprint.
///
/// Top-level shape:
///   schemaVersion: 1
///   coordinator:
///     host:
///       id: c64-host
///       yamlPath: docs/samples/c64.machine.yaml
///       yamlInline: |
///         schemaVersion: 1
///         machine: ...
///       busAttachments:
///         - busId: iec
///           endpointName: c64
///     peripherals:
///       - id: drive-8
///         role: Independent
///         yamlPath: ...
///         busAttachments: ...
///     cartExtensions:
///       - id: ext-1
///         yamlPath: ...
///     buses:
///       - id: iec
///         signals: [ATN, CLK, DATA]
/// </summary>
internal sealed class MultiSystemDocument
{
    public int? SchemaVersion { get; set; }
    public MultiSystemSection? Coordinator { get; set; }
}

internal sealed class MultiSystemSection
{
    public MultiSystemMachineSpec? Host { get; set; }
    public List<MultiSystemPeripheralSpec>? Peripherals { get; set; }
    public List<MultiSystemCartExtensionSpec>? CartExtensions { get; set; }
    public List<MultiSystemBusSpec>? Buses { get; set; }
}

internal sealed class MultiSystemMachineSpec
{
    public string? Id { get; set; }
    public string? Kind { get; set; }
    public string? YamlPath { get; set; }
    public string? YamlInline { get; set; }
    public List<MultiSystemBusAttachment>? BusAttachments { get; set; }
}

internal sealed class MultiSystemPeripheralSpec
{
    public string? Id { get; set; }
    public string? Role { get; set; }
    public string? Fidelity { get; set; }
    public string? Kind { get; set; }
    public int? DeviceNumber { get; set; }
    public string? DiskImagePath { get; set; }
    public string? YamlPath { get; set; }
    public string? YamlInline { get; set; }
    public List<MultiSystemBusAttachment>? BusAttachments { get; set; }
}

internal sealed class MultiSystemCartExtensionSpec
{
    public string? Id { get; set; }
    public string? Fidelity { get; set; }
    public string? YamlPath { get; set; }
    public string? YamlInline { get; set; }
    public List<MultiSystemBusAttachment>? BusAttachments { get; set; }
}

internal sealed class MultiSystemBusSpec
{
    public string? Id { get; set; }
    public List<string>? Signals { get; set; }
}

internal sealed class MultiSystemBusAttachment
{
    public string? BusId { get; set; }
    public string? EndpointName { get; set; }
}
