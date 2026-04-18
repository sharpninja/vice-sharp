namespace ViceSharp.Chips;

/// <summary>
/// 1541 Disk Drive
/// </summary>
public sealed class IecDrive
{
    /// <summary>Drive number 8-11</summary>
    public byte DriveNumber;

    /// <summary>Drive motor status</summary>
    public bool MotorOn;

    /// <summary>Current track</summary>
    public byte CurrentTrack;

    /// <summary>Current sector</summary>
    public byte CurrentSector;

    /// <summary>Disk image loaded</summary>
    public bool DiskInserted;

    /// <summary>Read/write head position</summary>
    public int HeadPosition;

    /// <summary>
    /// Reset drive
    /// </summary>
    public void Reset()
    {
        DriveNumber = 8;
        MotorOn = false;
        CurrentTrack = 1;
        CurrentSector = 0;
        DiskInserted = false;
        HeadPosition = 0;
    }

    /// <summary>
    /// Execute single clock cycle
    /// </summary>
    public void Step()
    {
        // TODO: 1541 drive logic
    }
}