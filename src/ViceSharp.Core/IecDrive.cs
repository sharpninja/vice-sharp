using ViceSharp.Abstractions;
using ViceSharp.Chips.IEC;

namespace ViceSharp.Core;

/// <summary>
/// VICE-style IEC disk drive emulation
/// </summary>
public sealed class IecDrive : IClockedDevice, IAddressSpace, IFloppyDrive
{
    private const string IecAtn = "ATN";
    private const string IecClk = "CLK";
    private const string IecData = "DATA";

    public DeviceId Id => new DeviceId((uint)(0x000A + DriveNumber));
    public string Name => $"IEC Drive {DriveNumber}";
    public uint ClockDivisor => 1;
    public ClockPhase Phase => ClockPhase.Phi2;
    public ushort BaseAddress => (ushort)(0xC00 + DriveNumber * 0x10);
    public ushort Size => 16;
    public bool IsReadOnly => false;

    public byte DriveNumber { get; }
    public bool IsOnline { get; set; } = true;
    public bool MotorOn { get; private set; }
    public byte CurrentTrack { get; private set; } = 18;
    public bool HasDisk => _diskImage is not null;
    public bool IsAttached => IsOnline;
    
    // IEC bus signals (active low)
    private bool _atnLine;
    private bool _clockLine;
    private bool _dataLine;
    private IBusEndpoint? _iecEndpoint;
    
    // Drive buffer (exposed for test inspection via SectorBuffer property).
    private readonly byte[] _sectorBuffer = new byte[256];

    // Motor ramp state machine.
    // Commodore 1541 motor requires ~300ms spin-up at 1,000,000 Hz drive clock
    // = 300,000 cycles before reliable sector access is possible.
    // VICE drive/drive.c: motor state tracked per drive unit.
    private const long MotorRampCyclesToSpeed = 300_000;
    private long _motorRampCycles = 0;
    private long _motorRotationCycles = 0;

    // Disk image support
    private D64Image? _diskImage;

    /// <summary>
    /// The currently inserted disk image, or null when empty. Read by the
    /// KERNAL serial trap (host-side vdrive) to service LOAD when True Drive is
    /// OFF; resolved lazily at open time so runtime insert/eject is honoured.
    /// </summary>
    public D64Image? DiskImage => _diskImage;
    
    public IecDrive(byte driveNumber, D64Image? diskImage = null)
    {
        DriveNumber = driveNumber;
        _diskImage = diskImage;
    }
    
    /// <summary>
    /// Number of drive cycles elapsed since the motor reached operating speed.
    /// Zero while motor is off or during the 300,000-cycle ramp-up phase.
    /// Advances monotonically once the ramp is complete.
    /// VICE drive/drive.c: motor rotation tracking for GCR flux emulation.
    /// </summary>
    public long MotorRotationCycles => _motorRotationCycles;

    /// <summary>
    /// The current 256-byte sector buffer (read by ReadSector, written by WriteSector).
    /// Exposed for test inspection; production callers should use ReadSector/WriteSector.
    /// </summary>
    public byte[] SectorBuffer => _sectorBuffer;

    public void Reset()
    {
        MotorOn = false;
        CurrentTrack = 18;
        _motorRampCycles = 0;
        _motorRotationCycles = 0;
    }

    /// <summary>
    /// Set motor state (simulates the 1541 VIA2 MOTEN bit, port B bit 2).
    /// Turning the motor off resets the ramp counter; the drive must complete
    /// a new 300,000-cycle ramp before rotation resumes.
    /// VICE drive/drive.c: motor enable/disable via VIA2 port B write.
    /// </summary>
    public void SetMotor(bool enabled)
    {
        if (!enabled)
        {
            MotorOn = false;
            _motorRampCycles = 0;
            _motorRotationCycles = 0;
        }
        else
        {
            MotorOn = true;
        }
    }

    /// <summary>
    /// Advance the drive motor state machine by one drive cycle.
    ///
    /// Motor ramp (TR-DRV-EDGE-001): When MotorOn is true, the drive spends
    /// the first 300,000 cycles in spin-up (ramp). During ramp, MotorRotationCycles
    /// stays at 0. After ramp, MotorRotationCycles increments each cycle.
    /// When MotorOn is false, both counters remain at 0.
    /// VICE drive/drive.c: motor cycle tracking for D64 sector timing.
    /// </summary>
    public void Tick()
    {
        if (!MotorOn)
            return;

        if (_motorRampCycles < MotorRampCyclesToSpeed)
        {
            _motorRampCycles++;
        }
        else
        {
            _motorRotationCycles++;
        }
    }
    
    public void Initialize() => Reset();

    public void Attach() => IsOnline = true;

    public void Detach() => IsOnline = false;

    public void InsertDisk(ReadOnlySpan<byte> diskImage)
    {
        if (diskImage.Length != D64Image.DiskSize35Track)
            throw new ArgumentException("D64 disk image must be 174,848 bytes.", nameof(diskImage));

        _diskImage = new D64Image(diskImage.ToArray());
        CurrentTrack = 18;
    }

    public void EjectDisk()
    {
        _diskImage = null;
        MotorOn = false;
    }
    
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
    public void SetAtn(bool active)
    {
        _atnLine = active;
        _iecEndpoint?.Pull(IecAtn, active);
    }
    
    /// <summary>
    /// VICE-style: Set clock line
    /// </summary>
    public void SetClock(bool active)
    {
        _clockLine = active;
        _iecEndpoint?.Pull(IecClk, active);
    }
    
    /// <summary>
    /// VICE-style: Set data line
    /// </summary>
    public void SetData(bool active)
    {
        _dataLine = active;
        _iecEndpoint?.Pull(IecData, active);
    }

    public void ConnectIecBus(IInterSystemBus bus)
    {
        ArgumentNullException.ThrowIfNull(bus);

        if (_iecEndpoint is not null)
            return;

        _iecEndpoint = bus.AttachEndpoint(Name);
        _iecEndpoint.Pull(IecAtn, _atnLine);
        _iecEndpoint.Pull(IecClk, _clockLine);
        _iecEndpoint.Pull(IecData, _dataLine);
    }

    public bool TryReadFirstProgram(out D64ProgramFile? program, out string error)
    {
        program = null;

        if (_diskImage is null)
        {
            error = $"IEC drive {DriveNumber} has no disk.";
            return false;
        }

        PulseIecActivity();
        return _diskImage.TryReadFirstProgram(out program, out error);
    }
    
    /// <summary>
    /// VICE-style: Read sector from disk image
    /// </summary>
    public bool ReadSector(int track, int sector)
    {
        if (_diskImage == null) return false;
        PulseIecActivity();
        return _diskImage.ReadSector(track, sector, _sectorBuffer);
    }
    
    /// <summary>
    /// VICE-style: Write sector to disk image
    /// </summary>
    public bool WriteSector(int track, int sector)
    {
        if (_diskImage == null) return false;
        PulseIecActivity();
        return _diskImage.WriteSector(track, sector, _sectorBuffer);
    }

    private void PulseIecActivity()
    {
        if (_iecEndpoint is null)
            return;

        _iecEndpoint.Pull(IecAtn, true);
        _iecEndpoint.Pull(IecClk, true);
        _iecEndpoint.Pull(IecData, true);
        _iecEndpoint.Pull(IecData, _dataLine);
        _iecEndpoint.Pull(IecClk, _clockLine);
        _iecEndpoint.Pull(IecAtn, _atnLine);
    }
}
