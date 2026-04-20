using ViceSharp.Abstractions;

namespace ViceSharp.Architectures.C64;

/// <summary>
/// C64 PAL architecture descriptor with 1.022727MHz clock.
/// </summary>
public sealed class C64Descriptor : IArchitectureDescriptor
{
    /// <inheritdoc />
    public string MachineName => "Commodore 64 PAL";

    /// <inheritdoc />
    public long MasterClockHz => 1022727;

    /// <inheritdoc />
    public VideoStandard VideoStandard => VideoStandard.Pal;

    /// <inheritdoc />
    public IReadOnlyList<DeviceDescriptor> Devices => _devices;

    private static readonly DeviceDescriptor[] _devices = Array.Empty<DeviceDescriptor>();

    /// <inheritdoc />
    public IRomSet? RequiredRoms => new C64RomSet();
}

/// <summary>
/// C64 required ROM set.
/// </summary>
public sealed class C64RomSet : IRomSet
{
    /// <inheritdoc />
    public string Architecture => "C64";

    /// <inheritdoc />
    public bool IsComplete(IRomProvider provider)
    {
        return provider.IsAvailable("basic", Architecture)
            && provider.IsAvailable("kernal", Architecture)
            && provider.IsAvailable("characters", Architecture);
    }
}
