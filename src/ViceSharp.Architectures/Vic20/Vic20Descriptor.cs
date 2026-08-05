using ViceSharp.Abstractions;
using ViceSharp.Core.Vic20;

namespace ViceSharp.Architectures.Vic20;

/// <summary>
/// VIC-20 (xvic) architecture descriptor: 6502, dual VIA, VIC-I, base RAM, ROMs.
/// </summary>
/// <remarks>
/// FR-PRF-005, FR-VIC20-001..006.
/// VIA and video board wiring live in Core builder; shared chips stay machine-agnostic.
/// </remarks>
public sealed class Vic20Descriptor : IProfiledArchitectureDescriptor, IVic20CartridgeHost
{
    public Vic20Descriptor()
        : this(Vic20MachineProfiles.Default)
    {
    }

    public Vic20Descriptor(string modelSelector)
        : this(Vic20MachineProfiles.Resolve(modelSelector))
    {
    }

    public Vic20Descriptor(Vic20MachineProfile profile, Vic20Cartridge? cartridge = null)
    {
        Profile = profile ?? throw new ArgumentNullException(nameof(profile));
        Cartridge = cartridge;
    }

    public Vic20MachineProfile Profile { get; }

    /// <summary>Optional MVP cart image mapped by the architecture builder.</summary>
    public Vic20Cartridge? Cartridge { get; }

    /// <inheritdoc />
    public IMachineProfile MachineProfile => Profile;

    /// <inheritdoc />
    public string MachineName => Profile.DisplayName;

    /// <inheritdoc />
    public long MasterClockHz => Profile.NominalClockHz;

    /// <inheritdoc />
    public VideoStandard VideoStandard => Profile.VideoStandard;

    /// <inheritdoc />
    public IReadOnlyList<DeviceDescriptor> Devices { get; } =
    [
        new("6502 CPU", new DeviceId(0x0001), DeviceRole.Cpu, 0x0000, 0),
        new("VIC-I", new DeviceId(0x0003), DeviceRole.VideoChip, 0x9000, 0x0010),
        new("VIA1", new DeviceId(0x0005), DeviceRole.Via1, 0x9110, 0x0010),
        new("VIA2", new DeviceId(0x0006), DeviceRole.Via2, 0x9120, 0x0010),
        new("System RAM", new DeviceId(0x0101), DeviceRole.SystemRam, 0x0000, 0x10000),
        new("Cartridge port", new DeviceId(0x0008), DeviceRole.CartridgePort, 0xA000, 0x2000),
    ];

    /// <inheritdoc />
    public IRomSet? RequiredRoms => new Vic20RomSet(
        Profile.RomSet,
        Profile.BasicRomName,
        Profile.KernalRomName,
        Profile.CharacterRomName);

    /// <summary>Clone with a different expansion pack.</summary>
    public Vic20Descriptor WithExpansion(Vic20Expansion expansion)
        => new(Profile with { Expansion = expansion }, Cartridge);

    /// <summary>Clone with an attached MVP cartridge.</summary>
    public Vic20Descriptor WithCartridge(Vic20Cartridge cartridge)
        => new(Profile, cartridge);
}
