namespace ViceSharp.Abstractions;

/// <summary>
/// A Commodore floppy-drive model offered to the device-setup UI. The backing integer
/// value is VICE's canonical drive-type number (VICE <c>drivetypes.h</c>
/// <c>DRIVE_TYPE_*</c>), so a selected model round-trips through the host true-drive
/// rebuild as its raw drive type with no translation table. In particular the 1541-II
/// is drive type <c>1542</c>, NOT <c>1541</c>.
/// </summary>
public enum DriveModel
{
    /// <summary>No specific drive model (unset; the host chooses its default).</summary>
    None = 0,

    /// <summary>Commodore 1540 (the VIC-20-era 1541 variant). VICE drive type 1540.</summary>
    C1540 = 1540,

    /// <summary>Commodore 1541 (the canonical single-density 5.25" drive). VICE drive type 1541.</summary>
    C1541 = 1541,

    /// <summary>Commodore 1541-II (the later cost-reduced 1541). VICE drive type 1542.</summary>
    C1541II = 1542,
}
