using ViceSharp.Abstractions;

namespace ViceSharp.Architectures.Adhoc;

/// <summary>
/// <see cref="IArchitectureDescriptor"/> assembled from a validated
/// ad-hoc YAML document.
/// </summary>
public sealed class AdhocArchitectureDescriptor : IArchitectureDescriptor
{
    public AdhocArchitectureDescriptor(string machineName, long masterClockHz, VideoStandard videoStandard)
    {
        MachineName = machineName;
        MasterClockHz = masterClockHz;
        VideoStandard = videoStandard;
    }

    /// <inheritdoc />
    public string MachineName { get; }

    /// <inheritdoc />
    public long MasterClockHz { get; }

    /// <inheritdoc />
    public VideoStandard VideoStandard { get; }

    /// <inheritdoc />
    public IReadOnlyList<DeviceDescriptor> Devices { get; } = Array.Empty<DeviceDescriptor>();

    /// <inheritdoc />
    public IRomSet? RequiredRoms => null;
}
