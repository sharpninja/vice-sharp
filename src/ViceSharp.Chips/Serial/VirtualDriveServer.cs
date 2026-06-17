using ViceSharp.Chips.IEC;

namespace ViceSharp.Chips.Serial;

/// <summary>
/// Host-side virtual disk drive (the "vdrive") that services IEC bus commands
/// when True Drive is OFF. It is a faithful port of VICE's serial channel state
/// machine (native/vice/vice/src/serial/fsdrive.c) plus the vdrive read path
/// (vdrive-iec.c): it accumulates a filename while a channel is AWAITING_NAME,
/// opens the named file (or the "$" directory) on UNLISTEN/secondary, and then
/// streams its bytes back on ACPTR with the EOI status set on the final byte.
///
/// The disk image for a device is resolved lazily through the supplied delegate
/// so runtime disk insertion/ejection on the simulated <c>IecDrive</c> is picked
/// up at open time. Only disk devices 8-11 are served; writing (SAVE) is not yet
/// implemented and is silently ignored.
/// </summary>
public sealed class VirtualDriveServer
{
    public const byte SerialOk = 0x00;
    public const byte SerialEof = 0x40;
    public const byte SerialError = 0x02;
    public const byte SerialDeviceNotPresent = 0x80;

    private const int SerialNameLength = 16;
    private const int ChannelCount = 16;

    private enum ChannelState
    {
        Closed = 0,
        AwaitingName,
        Open
    }

    private sealed class DeviceState
    {
        public readonly ChannelState[] IsOpen = new ChannelState[ChannelCount];
        public readonly byte[]?[] Stream = new byte[ChannelCount][];
        public readonly int[] Position = new int[ChannelCount];
    }

    private readonly Func<int, D64Image?> _diskResolver;
    private readonly byte[] _nameBuffer = new byte[SerialNameLength + 1];
    private int _namePtr;
    private readonly Dictionary<int, DeviceState> _devices = new();

    public VirtualDriveServer(Func<int, D64Image?> diskResolver)
    {
        ArgumentNullException.ThrowIfNull(diskResolver);
        _diskResolver = diskResolver;
    }

    /// <summary>True when the device currently has a virtual disk inserted.</summary>
    public bool HasDisk(int device) => _diskResolver(device & 0x0F) is not null;

    /// <summary>Reset all channel/name state (called on machine reset).</summary>
    public void Reset()
    {
        _devices.Clear();
        _namePtr = 0;
    }

    // serial_iec_bus_open -> fsdrive_open: arm the channel to receive a name.
    public void Open(int device, byte secondary)
    {
        Device(device).IsOpen[secondary & 0x0F] = ChannelState.AwaitingName;
        _namePtr = 0;
    }

    // serial_iec_bus_close -> fsdrive_close -> serialcommand.
    public byte Close(int device, byte secondary) => SerialCommand(device, secondary);

    // serial_iec_bus_listen / serial_iec_bus_talk -> fsdrive_listentalk -> serialcommand.
    public byte ListenTalk(int device, byte secondary) => SerialCommand(device, secondary);

    // serial_iec_bus_unlisten -> fsdrive_unlisten: only OPEN/command channels act.
    public byte Unlisten(int device, byte secondary)
    {
        if ((secondary & 0xF0) == 0xF0 || (secondary & 0x0F) == 0x0F)
            return SerialCommand(device, secondary);
        return SerialOk;
    }

    // serial_iec_bus_untalk -> fsdrive_untalk: no-op for read.
    public void Untalk(int device, byte secondary)
    {
    }

    // serial_iec_bus_write -> fsdrive_write: accumulate the name, else (SAVE) ignore.
    public byte Write(int device, byte secondary, byte data)
    {
        var d = Device(device);
        var channel = secondary & 0x0F;
        if (d.IsOpen[channel] == ChannelState.AwaitingName)
        {
            if (_namePtr < SerialNameLength)
                _nameBuffer[_namePtr++] = data;
        }

        return SerialOk;
    }

    // serial_iec_bus_read -> fsdrive_read -> getf: stream the opened file/dir.
    public (byte Data, byte Status) Read(int device, byte secondary)
    {
        var d = Device(device);
        var channel = secondary & 0x0F;
        var stream = d.Stream[channel];

        if (stream is null || stream.Length == 0)
            return (0, SerialEof);

        var position = d.Position[channel];
        if (position >= stream.Length)
            return (0, SerialEof);

        var data = stream[position];
        position++;
        d.Position[channel] = position;

        // EOI is reported together with the final valid byte (VICE
        // iec_read_sequential), so the KERNAL ACPTR loop terminates correctly.
        return (data, position >= stream.Length ? SerialEof : SerialOk);
    }

    // fsdrive.c serialcommand(): dispatch on the secondary-address command nibble.
    private byte SerialCommand(int device, byte secondary)
    {
        var d = Device(device);
        var channel = secondary & 0x0F;
        byte status = SerialOk;

        switch (secondary & 0xF0)
        {
            case 0x60: // OPEN CHANNEL (reopen for talk/listen)
                if (d.IsOpen[channel] == ChannelState.AwaitingName)
                {
                    d.IsOpen[channel] = ChannelState.Open;
                    status = OpenFile(device, channel);
                }

                break;

            case 0xE0: // CLOSE FILE
                d.IsOpen[channel] = ChannelState.Closed;
                d.Stream[channel] = null;
                d.Position[channel] = 0;
                break;

            case 0xF0: // OPEN FILE
                if (d.IsOpen[channel] != ChannelState.Closed)
                {
                    if (_namePtr != 0 || channel == 0x0F)
                    {
                        d.IsOpen[channel] = ChannelState.Open;
                        status = OpenFile(device, channel);
                    }

                    // OPEN always clears the error bit (fsdrive: st &= ~2).
                    status &= unchecked((byte)~SerialError);
                }

                break;

                // 0x20/0x30 (LISTEN) and 0x40/0x50 (TALK) require no driver action.
        }

        return status;
    }

    // vdrive_iec_open (read path): resolve the name to a byte stream on the channel.
    private byte OpenFile(int device, int channel)
    {
        var d = Device(device);
        d.Position[channel] = 0;

        var image = _diskResolver(device & 0x0F);
        if (image is null)
        {
            d.Stream[channel] = Array.Empty<byte>();
            _namePtr = 0;
            return SerialDeviceNotPresent;
        }

        var name = _nameBuffer.AsSpan(0, _namePtr);
        var fileSystem = new D64FileSystem(image);
        byte status = SerialOk;

        if (_namePtr > 0 && name[0] == (byte)'$')
        {
            d.Stream[channel] = fileSystem.BuildDirectoryListing();
        }
        else if (_namePtr == 0)
        {
            // Reopen of an already-resolved channel with no fresh name.
            d.Stream[channel] ??= Array.Empty<byte>();
        }
        else if (fileSystem.TryFindFile(name, out var entry))
        {
            d.Stream[channel] = fileSystem.ReadFileStream(entry.StartTrack, entry.StartSector);
        }
        else
        {
            d.Stream[channel] = Array.Empty<byte>();
            status = SerialError; // file not found
        }

        _namePtr = 0;
        return status;
    }

    private DeviceState Device(int device)
    {
        var key = device & 0x0F;
        if (!_devices.TryGetValue(key, out var state))
        {
            state = new DeviceState();
            _devices[key] = state;
        }

        return state;
    }
}
