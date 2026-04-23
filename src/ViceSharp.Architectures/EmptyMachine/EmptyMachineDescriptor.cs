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
    public IReadOnlyList<DeviceDescriptor> Devices { get; } =
    [
        new("6502 CPU", new DeviceId(0x0001), DeviceRole.Cpu, 0x0000, 0),
        new("System RAM", new DeviceId(0x0100), DeviceRole.SystemRam, 0x0000, 0x10000),
    ];
    
    /// <inheritdoc />
    public IRomSet? RequiredRoms => null;
}
