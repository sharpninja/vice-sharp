using ViceSharp.Abstractions;

namespace ViceSharp.Host.Runtime;

public sealed class MinimalHostArchitectureDescriptor : IArchitectureDescriptor
{
    public const string ArchitectureId = "minimal";

    public static MinimalHostArchitectureDescriptor Instance { get; } = new();

    private MinimalHostArchitectureDescriptor()
    {
    }

    public string MachineName => "Minimal Host Machine";

    public long MasterClockHz => 1_022_727;

    public VideoStandard VideoStandard => VideoStandard.Pal;

    public IReadOnlyList<DeviceDescriptor> Devices => Array.Empty<DeviceDescriptor>();

    public IRomSet? RequiredRoms => null;
}
