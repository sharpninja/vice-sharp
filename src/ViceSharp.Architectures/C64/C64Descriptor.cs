using ViceSharp.Abstractions;

namespace ViceSharp.Architectures.C64;

/// <summary>
/// C64 x64sc architecture descriptor.
/// </summary>
public sealed class C64Descriptor : IProfiledArchitectureDescriptor
{
    public C64Descriptor()
        : this(C64MachineProfiles.Default)
    {
    }

    public C64Descriptor(string modelSelector)
        : this(C64MachineProfiles.Resolve(modelSelector))
    {
    }

    public C64Descriptor(C64MachineProfile profile)
    {
        Profile = profile ?? throw new ArgumentNullException(nameof(profile));
    }

    public C64MachineProfile Profile { get; }

    /// <inheritdoc />
    public IMachineProfile MachineProfile => Profile;

    /// <inheritdoc />
    public string MachineName => Profile.DisplayName;

    /// <inheritdoc />
    public long MasterClockHz => Profile.NominalClockHz;

    /// <inheritdoc />
    public VideoStandard VideoStandard => Profile.VideoStandard;

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
        new("Cartridge port", new DeviceId(0x0008), DeviceRole.CartridgePort, 0x8000, 0x8000),
        new("System RAM", new DeviceId(0x0101), DeviceRole.SystemRam, 0x0000, 0x10000),
    };

    /// <inheritdoc />
    public IRomSet? RequiredRoms => new C64RomSet(
        Profile.RomSet,
        Profile.BasicRomName,
        Profile.KernalRomName,
        Profile.CharacterRomName);
}

/// <summary>
/// C64 required ROM set.
/// </summary>
public sealed class C64RomSet : IRomSet
{
    public C64RomSet()
        : this("C64", C64ViceRomNames.Basic, C64ViceRomNames.KernalRev3, C64ViceRomNames.Character)
    {
    }

    public C64RomSet(
        string architecture,
        string basicRomName = C64ViceRomNames.Basic,
        string kernalRomName = C64ViceRomNames.KernalRev3,
        string characterRomName = C64ViceRomNames.Character)
    {
        Architecture = string.IsNullOrWhiteSpace(architecture) ? "C64" : architecture;
        BasicRomName = string.IsNullOrWhiteSpace(basicRomName) ? C64ViceRomNames.Basic : basicRomName;
        KernalRomName = string.IsNullOrWhiteSpace(kernalRomName) ? C64ViceRomNames.KernalRev3 : kernalRomName;
        CharacterRomName = string.IsNullOrWhiteSpace(characterRomName) ? C64ViceRomNames.Character : characterRomName;
    }

    /// <inheritdoc />
    public string Architecture { get; }

    public string BasicRomName { get; }

    public string KernalRomName { get; }

    public string CharacterRomName { get; }

    /// <inheritdoc />
    public bool IsComplete(IRomProvider provider)
    {
        return provider.IsAvailable(BasicRomName, Architecture)
            && (IsKernalRequired(KernalRomName) ? provider.IsAvailable(KernalRomName, Architecture) : true)
            && provider.IsAvailable(CharacterRomName, Architecture);
    }

    public static bool IsKernalRequired(string kernalRomName)
        => !string.Equals(kernalRomName, C64ViceRomNames.KernalNone, StringComparison.OrdinalIgnoreCase);
}
