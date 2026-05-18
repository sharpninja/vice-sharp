using ViceSharp.Abstractions;

namespace ViceSharp.Architectures.C1541;

/// <summary>
/// Architecture descriptor for a standalone 1541-family drive machine. The
/// 1541 is its own 6502-based computer with its own clock (1 MHz), 2KB of
/// RAM at $0000-$07FF, two VIA 6522 chips at $1800 and $1C00, and 16KB of
/// DOS ROM at $C000-$FFFF.
///
/// Used by ARCH-TRUEDRIVE-1541-001 (Phase B). The drive machine is attached
/// to an ISystemCoordinator alongside a C64 host; the IEC bus bridges signal
/// state between the two.
/// </summary>
public sealed class C1541Descriptor : IDriveArchitectureDescriptor
{
    public C1541Descriptor() : this(C1541ViceRomNames.Dos1541, deviceNumber: 8, diskImagePath: null) { }

    public C1541Descriptor(string dosRomName)
        : this(dosRomName, deviceNumber: 8, diskImagePath: null) { }

    public C1541Descriptor(int deviceNumber)
        : this(C1541ViceRomNames.Dos1541, deviceNumber, diskImagePath: null) { }

    public C1541Descriptor(string dosRomName, int deviceNumber)
        : this(dosRomName, deviceNumber, diskImagePath: null) { }

    public C1541Descriptor(int deviceNumber, string? diskImagePath)
        : this(C1541ViceRomNames.Dos1541, deviceNumber, diskImagePath) { }

    public C1541Descriptor(string dosRomName, int deviceNumber, string? diskImagePath)
    {
        if (deviceNumber < 8 || deviceNumber > 11)
            throw new ArgumentOutOfRangeException(nameof(deviceNumber), "Device number must be 8..11.");
        DosRomName = string.IsNullOrWhiteSpace(dosRomName)
            ? C1541ViceRomNames.Dos1541
            : dosRomName;
        DeviceNumber = deviceNumber;
        DiskImagePath = string.IsNullOrWhiteSpace(diskImagePath) ? null : diskImagePath;
    }

    /// <summary>DOS ROM filename to load (allows 1541 / 1541-II / 1540 variants).</summary>
    public string DosRomName { get; }

    /// <inheritdoc />
    public int DeviceNumber { get; }

    /// <inheritdoc />
    public string? DiskImagePath { get; }

    /// <inheritdoc />
    public string MachineName => "Commodore 1541";

    /// <inheritdoc />
    public long MasterClockHz => 1_000_000;

    /// <inheritdoc />
    public VideoStandard VideoStandard => VideoStandard.Pal;

    /// <inheritdoc />
    public IReadOnlyList<DeviceDescriptor> Devices { get; } =
    [
        new("6502 Drive CPU", new DeviceId(0x1001), DeviceRole.DriveCpu, 0x0000, 0),
        new("Drive RAM (2KB)", new DeviceId(0x1100), DeviceRole.DriveRam, 0x0000, 0x0800),
        new("VIA 1 (IEC/LED)", new DeviceId(0x1201), DeviceRole.DriveVia, 0x1800, 0x0400),
        new("VIA 2 (Head/Motor)", new DeviceId(0x1202), DeviceRole.DriveVia, 0x1C00, 0x0400),
        new("Drive ROM (16KB)", new DeviceId(0x1300), DeviceRole.DriveRom, 0xC000, 0x4000),
    ];

    /// <inheritdoc />
    public IRomSet? RequiredRoms => new C1541RomSet(
        C1541ViceRomNames.ArchitectureKey,
        DosRomName);
}
