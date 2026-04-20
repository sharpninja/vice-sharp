using ViceSharp.Abstractions;

namespace ViceSharp.Architectures.EmptyMachine;

/// <summary>
/// Empty test machine descriptor for validation and testing.
/// Minimal implementation with no hardware devices.
/// </summary>
public sealed class EmptyMachineDescriptor : IArchitectureDescriptor
{
    /// <inheritdoc />
    public string MachineName => "Empty Test Machine";

    /// <inheritdoc />
    public long MasterClockHz => 1000000;

    /// <inheritdoc />
    public VideoStandard VideoStandard => VideoStandard.Ntsc;
    
    /// <inheritdoc />
    public IReadOnlyList<DeviceDescriptor> Devices { get; } = Array.Empty<DeviceDescriptor>();
    
    /// <inheritdoc />
    public IRomSet? RequiredRoms => null;
}
