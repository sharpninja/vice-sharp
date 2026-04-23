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

    private static readonly DeviceDescriptor[] _devices =
    {
        new("6510 CPU", new DeviceId(0x0001), DeviceRole.Cpu, 0x0000, 0),
        new("VIC-II", new DeviceId(0x0003), DeviceRole.VideoChip, 0xD000, 0x0400),
        new("SID", new DeviceId(0x0004), DeviceRole.AudioChip, 0xD400, 0x0400),
        new("CIA1", new DeviceId(0x0005), DeviceRole.Cia1, 0xDC00, 0x0100),
        new("CIA2", new DeviceId(0x0006), DeviceRole.Cia2, 0xDD00, 0x0100),
        new("PLA", new DeviceId(0x0007), DeviceRole.Pla, 0x0001, 1),
        new("System RAM", new DeviceId(0x0101), DeviceRole.SystemRam, 0x0000, 0x10000),
    };

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
