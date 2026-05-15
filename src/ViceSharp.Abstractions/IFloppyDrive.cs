namespace ViceSharp.Abstractions;

/// <summary>
/// Floppy disk drive interface.
/// </summary>
public interface IFloppyDrive : IPeripheral
{
    /// <summary>True when drive motor is running</summary>
    bool MotorOn { get; }

    /// <summary>IEC device number for this drive.</summary>
    byte DriveNumber { get; }
    
    /// <summary>Current head track position</summary>
    byte CurrentTrack { get; }

    /// <summary>True when a disk image is inserted.</summary>
    bool HasDisk { get; }

    /// <summary>Insert a disk image into the drive</summary>
    void InsertDisk(ReadOnlySpan<byte> diskImage);
    
    /// <summary>Eject the currently inserted disk</summary>
    void EjectDisk();
}
