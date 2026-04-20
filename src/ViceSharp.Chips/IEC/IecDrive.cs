using ViceSharp.Abstractions;

namespace ViceSharp.Chips.IEC;

/// <summary>
/// VICE-style IEC disk drive emulation
/// </summary>
public sealed class IecDrive : IClockedDevice, IAddressSpace
{
    public DeviceId Id => new DeviceId(0x000A);
    public string Name => $"IEC Drive {DriveNumber}";
    public uint ClockDivisor => 1;
    public ClockPhase Phase => ClockPhase.Phi2;
    public ushort BaseAddress => (ushort)(0xC00 + DriveNumber * 0x10);
    public ushort Size => 16;
    public bool IsReadOnly => false;

    public byte DriveNumber { get; init; } = 8;
    public bool IsOnline { get; set; } = true;
    
    // IEC bus signals (active low)
    private bool _atnLine;
    private bool _clockLine;
    private bool _dataLine;
    
    // Drive buffer
    private readonly byte[] _sectorBuffer = new byte[256];
    
    // Disk image support
    private readonly D64Image? _diskImage;
    
    public IecDrive(byte driveNumber, D64Image? diskImage = null)
    {
        DriveNumber = driveNumber;
        _diskImage = diskImage;
    }
    
    public void Reset()
    {
    }
    
    public void Tick()
    {
        // IEC timing: drives respond to bus commands
    }
    
    public void Initialize() => Reset();
    
    public byte Peek(ushort offset) => Read(offset);
    
    public byte Read(ushort offset) => _sectorBuffer[offset & 0xFF];
    
    public void Write(ushort offset, byte value)
    {
        if (offset < 0x10)
        {
            // Status register at offset 0
        }
        _sectorBuffer[offset & 0xFF] = value;
    }
    
    public bool HandlesAddress(ushort address) => 
        address >= BaseAddress && address < BaseAddress + Size;
    
    /// <summary>
    /// VICE-style: Set ATN line (attention)
    /// </summary>
    public void SetAtn(bool active) => _atnLine = active;
    
    /// <summary>
    /// VICE-style: Set clock line
    /// </summary>
    public void SetClock(bool active) => _clockLine = active;
    
    /// <summary>
    /// VICE-style: Set data line
    /// </summary>
    public void SetData(bool active) => _dataLine = active;
    
    /// <summary>
    /// VICE-style: Read sector from disk image
    /// </summary>
    public bool ReadSector(int track, int sector)
    {
        if (_diskImage == null) return false;
        return _diskImage.ReadSector(track, sector, _sectorBuffer);
    }
    
    /// <summary>
    /// VICE-style: Write sector to disk image
    /// </summary>
    public bool WriteSector(int track, int sector)
    {
        if (_diskImage == null) return false;
        return _diskImage.WriteSector(track, sector, _sectorBuffer);
    }
}
