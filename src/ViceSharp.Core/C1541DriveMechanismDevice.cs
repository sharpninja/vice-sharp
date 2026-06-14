using ViceSharp.Abstractions;
using ViceSharp.Chips.Cpu;
using ViceSharp.Chips.IEC;

namespace ViceSharp.Core;

/// <summary>
/// 1541 drive mechanism electronics connected to VIA2.
/// </summary>
public sealed class C1541DriveMechanismDevice : IClockedDevice
{
    private const int ByteReadyIntervalCycles = 32;
    private const byte ProcessorOverflowFlag = 0x40;
    private const byte SpeedZoneMask = 0x60;

    private readonly DriveHeadState _head = new();
    private readonly Mos6502? _driveCpu;
    private D64DiskImageDevice? _disk;
    private Via6522? _via2;
    private byte _lastPortBOutput = 0xFF;
    private int _byteReadyCountdown = ByteReadyIntervalCycles;
    private int _speedZone;
    private bool _byteReadyLevel;
    private bool _byteReadyEdge;

    private static ReadOnlySpan<int> ByteReadyIntervalsBySpeedZone => [32, 30, 28, 26];

    public C1541DriveMechanismDevice(D64DiskImageDevice? disk = null, Mos6502? driveCpu = null)
    {
        _disk = disk;
        _driveCpu = driveCpu;
    }

    public DeviceId Id => new(0x1401);
    public string Name => "1541 Drive Mechanism";
    public uint ClockDivisor => 1;
    public ClockPhase Phase => ClockPhase.Phi2;

    public void Mount(D64DiskImageDevice? disk) => _disk = disk;

    public void ConnectVia2(Via6522 via2)
    {
        ArgumentNullException.ThrowIfNull(via2);

        _via2 = via2;
        var prevPbIn = via2.PortBInput;
        var prevPbOut = via2.PortBOutputChanged;
        var prevPaIn = via2.PortAInput;
        var prevPaOut = via2.PortAOutputChanged;

        via2.PortBInput = () =>
        {
            byte composed = (byte)((prevPbIn?.Invoke() ?? 0) | 0x6F);
            if (_disk is not null && !_disk.IsEjected)
            {
                composed |= DriveHeadState.WriteProtectBit;
                if (_head.IsSync(_disk.Image))
                    composed &= 0x7F;
                else
                    composed |= 0x80;
            }
            else
            {
                composed |= 0x80;
            }
            ClearByteReadyLevel();
            return composed;
        };

        via2.PortBOutputChanged = value =>
        {
            var previous = _lastPortBOutput;
            prevPbOut?.Invoke(value);
            ClearByteReadyLevel();
            _head.UpdatePortBOutput(previous, value);
            UpdateSpeedZone(previous, value);
            _lastPortBOutput = value;
        };

        via2.PortAInput = () =>
        {
            byte composed = prevPaIn?.Invoke() ?? 0xFF;
            if (_disk is null || _disk.IsEjected || !_head.MotorOn)
                return composed;
            var value = _driveCpu is null
                ? _head.ReadNextByte(_disk.Image)
                : _head.ReadCurrentByte(_disk.Image);
            ClearByteReadyLevel();
            return value;
        };

        via2.PortAOutputChanged = value =>
        {
            prevPaOut?.Invoke(value);
            ClearByteReadyLevel();
        };
    }

    public void Tick()
    {
        if (!_head.MotorOn || !IsByteReadyEnabled())
        {
            FlushPendingByteReadyEdge();
            _byteReadyCountdown = CurrentByteReadyIntervalCycles();
            return;
        }

        if (--_byteReadyCountdown > 0)
            return;

        _byteReadyCountdown = CurrentByteReadyIntervalCycles();
        var byteReady = false;
        if (_disk is not null && !_disk.IsEjected)
        {
            _head.AdvanceByte(_disk.Image);
            byteReady = !_head.IsSync(_disk.Image);
        }

        if (byteReady)
            RaiseByteReady();
    }

    private bool IsByteReadyEnabled()
    {
        return _via2 is not null && (_via2.Peek((ushort)(_via2.BaseAddress + 0x0C)) & 0x02) != 0;
    }

    public void Reset()
    {
        _head.Reset();
        _lastPortBOutput = 0xFF;
        _byteReadyCountdown = ByteReadyIntervalCycles;
        _speedZone = 0;
        _byteReadyLevel = false;
        _byteReadyEdge = false;
    }

    private void UpdateSpeedZone(byte previous, byte value)
    {
        if (((previous ^ value) & SpeedZoneMask) == 0)
            return;

        _speedZone = (value >> 5) & 0x03;
        var interval = CurrentByteReadyIntervalCycles();
        if (_byteReadyCountdown > interval)
            _byteReadyCountdown = interval;
    }

    private int CurrentByteReadyIntervalCycles() => ByteReadyIntervalsBySpeedZone[_speedZone];

    private void RaiseByteReady()
    {
        if (!_byteReadyLevel)
            _byteReadyLevel = true;
        _byteReadyEdge = true;
        if (_driveCpu is not null)
        {
            _driveCpu.P |= ProcessorOverflowFlag;
            _byteReadyEdge = false;
        }
    }

    private void FlushPendingByteReadyEdge()
    {
        if (!_byteReadyEdge || _driveCpu is null)
            return;

        _driveCpu.P |= ProcessorOverflowFlag;
        _byteReadyEdge = false;
    }

    private void ClearByteReadyLevel()
    {
        if (!_byteReadyLevel)
            return;

        _byteReadyLevel = false;
    }

    /// <summary>
    /// Tracks one 1541 head position, motor state, and sector byte cursor.
    /// </summary>
    public sealed class DriveHeadState
    {
        public const byte WriteProtectBit = 0x10;
        public const byte MotorBit = 0x04;
        public const byte StepPhaseMask = 0x03;

        private const int MaxGcrTrackSize = 7928;
        private const int HeaderGapSize = 9;
        private const int SyncSize = 5;
        private const int SectorGcrSizeWithHeader = 335;

        public int Track => _currentHalfTrack / 2;
        public int HalfTrack => _currentHalfTrack;
        public bool MotorOn { get; private set; }

        private const int MinHalfTrack = 2;
        private const int MaxHalfTrack = 35 * 2;
        private const int InitialHalfTrack = 18 * 2;

        private int _currentHalfTrack = InitialHalfTrack;
        private int _gcrByteIndex;
        private int _pendingGcrByteIndex = -1;
        private int _currentByteIndex = -1;
        private int _gcrHalfTrackNumber;
        private int _gcrTrackLength;
        private byte _currentByte = 0xFF;
        private D64Image? _gcrImage;
        private readonly byte[] _gcrTrack = new byte[MaxGcrTrackSize];
        private readonly byte[] _gcrScratch = new byte[MaxGcrTrackSize];

        public void Reset()
        {
            MotorOn = false;
            _currentHalfTrack = InitialHalfTrack;
            _gcrByteIndex = 0;
            _pendingGcrByteIndex = -1;
            _currentByteIndex = -1;
            _gcrHalfTrackNumber = 0;
            _gcrTrackLength = 0;
            _currentByte = 0xFF;
            _gcrImage = null;
        }

        public void UpdateMotor(bool motorOn)
        {
            if (MotorOn == motorOn) return;
            MotorOn = motorOn;
            if (motorOn && _gcrTrackLength == 0)
                _currentByte = 0xFF;
        }

        public void UpdatePortBOutput(byte previous, byte value)
        {
            var trackNumber = _currentHalfTrack - 2;
            var oldStepperPosition = trackNumber & StepPhaseMask;
            var newStepperPosition = value & StepPhaseMask;
            var stepCount = (newStepperPosition - oldStepperPosition) & StepPhaseMask;
            if (stepCount == 3)
                stepCount = -1;

            if ((value & MotorBit) != 0 && IsSingleStep(stepCount))
                MoveHead(stepCount);

            var motorChanged = ((previous ^ value) & MotorBit) != 0;
            UpdateMotor((value & MotorBit) != 0);
            if (motorChanged && MotorOn && newStepperPosition != oldStepperPosition)
                MoveHead(stepCount);
        }

        public void UpdateHeadStep(int phase)
        {
            var value = (byte)((MotorOn ? MotorBit : 0) | (phase & StepPhaseMask));
            UpdatePortBOutput(value, value);
        }

        public byte ReadNextByte(D64Image image)
        {
            AdvanceByte(image);
            return _currentByte;
        }

        public byte ReadCurrentByte(D64Image image)
        {
            EnsureGcrTrack(image);
            return _currentByte;
        }

        public bool IsSync(D64Image image)
        {
            if (!MotorOn)
                return false;

            EnsureGcrTrack(image);
            return IsCurrentByteInSyncRun();
        }

        private void EnsureGcrTrack(D64Image image)
        {
            var halfTrack = _currentHalfTrack;
            var track = Track;
            if (ReferenceEquals(_gcrImage, image) && _gcrHalfTrackNumber == halfTrack && _gcrTrackLength != 0)
                return;

            _gcrImage = image;
            _gcrHalfTrackNumber = halfTrack;
            _gcrTrackLength = RawTrackSize(track);
            if (_pendingGcrByteIndex >= 0)
            {
                _gcrByteIndex = _pendingGcrByteIndex % _gcrTrackLength;
                _pendingGcrByteIndex = -1;
            }
            else
            {
                _gcrByteIndex = 0;
            }

            if ((halfTrack & 1) != 0)
            {
                Array.Clear(_gcrTrack, 0, _gcrTrackLength);
                return;
            }

            Array.Fill(_gcrScratch, (byte)0x55, 0, _gcrTrackLength);

            var id1 = image.ReadSectorByte(18, 0, 0xA2);
            var id2 = image.ReadSectorByte(18, 0, 0xA3);
            var gap = GapSize(track);
            var sectorCount = SectorCount(track);
            var offset = 0;
            Span<byte> sectorBuffer = stackalloc byte[256];

            for (var sector = 0; sector < sectorCount; sector++)
            {
                image.GetSector(track, sector).CopyTo(sectorBuffer);
                ConvertSectorToGcr(sectorBuffer, _gcrScratch.AsSpan(offset), track, sector, id1, id2);
                offset += SectorGcrSizeWithHeader + HeaderGapSize + gap + SyncSize * 2;
                if (offset >= _gcrTrackLength)
                    break;
            }

            CopyTrackWithViceSkew(track);
        }

        private static void ConvertSectorToGcr(
            ReadOnlySpan<byte> sectorData,
            Span<byte> destination,
            int track,
            int sector,
            byte id1,
            byte id2)
        {
            destination[..SyncSize].Fill(0xFF);
            var offset = SyncSize;

            Span<byte> block = stackalloc byte[4];
            block[0] = 0x08;
            block[1] = (byte)(sector ^ track ^ id2 ^ id1);
            block[2] = (byte)sector;
            block[3] = (byte)track;
            GcrCodec.EncodeBlock(block, destination.Slice(offset, 5));
            offset += 5;

            block[0] = id2;
            block[1] = id1;
            block[2] = 0x0F;
            block[3] = 0x0F;
            GcrCodec.EncodeBlock(block, destination.Slice(offset, 5));
            offset += 5 + HeaderGapSize;

            destination.Slice(offset, SyncSize).Fill(0xFF);
            offset += SyncSize;

            byte checksum = 0;
            block[0] = 0x07;
            block[1] = sectorData[0];
            block[2] = sectorData[1];
            block[3] = sectorData[2];
            checksum = (byte)(checksum ^ sectorData[0] ^ sectorData[1] ^ sectorData[2]);
            GcrCodec.EncodeBlock(block, destination.Slice(offset, 5));
            offset += 5;

            var dataOffset = 3;
            for (var i = 0; i < 63; i++)
            {
                block[0] = sectorData[dataOffset + 0];
                block[1] = sectorData[dataOffset + 1];
                block[2] = sectorData[dataOffset + 2];
                block[3] = sectorData[dataOffset + 3];
                checksum = (byte)(checksum ^ block[0] ^ block[1] ^ block[2] ^ block[3]);
                GcrCodec.EncodeBlock(block, destination.Slice(offset, 5));
                dataOffset += 4;
                offset += 5;
            }

            block[0] = sectorData[dataOffset];
            block[1] = (byte)(checksum ^ sectorData[dataOffset]);
            block[2] = 0;
            block[3] = 0;
            GcrCodec.EncodeBlock(block, destination.Slice(offset, 5));
        }

        private void CopyTrackWithViceSkew(int track)
        {
            var skew = TrackOffset(track);
            if (skew == 0)
            {
                _gcrScratch.AsSpan(0, _gcrTrackLength).CopyTo(_gcrTrack);
                return;
            }

            var tail = _gcrTrackLength - skew;
            _gcrScratch.AsSpan(0, tail).CopyTo(_gcrTrack.AsSpan(skew, tail));
            _gcrScratch.AsSpan(tail, skew).CopyTo(_gcrTrack.AsSpan(0, skew));
        }

        private static int TrackOffset(int track)
        {
            var trackOffset = 0;
            for (var current = 1; current <= track; current++)
            {
                var trackSize = RawTrackSize(current);
                var gap = GapSize(current);
                var sectorCount = SectorCount(current);
                var bytesWritten = sectorCount * (SectorGcrSizeWithHeader + HeaderGapSize + gap + SyncSize * 2);
                trackOffset += bytesWritten - gap;
                trackOffset += (trackSize * 100) / 270;
                trackOffset %= trackSize;
            }

            return trackOffset;
        }

        private void MoveHead(int stepCount)
        {
            var next = _currentHalfTrack + stepCount;
            if (next < MinHalfTrack)
                next = MinHalfTrack;
            else if (next > MaxHalfTrack)
                next = MaxHalfTrack;

            if (next == _currentHalfTrack)
                return;

            var previousTrackLength = _gcrTrackLength;
            var previousByteIndex = _pendingGcrByteIndex >= 0 ? _pendingGcrByteIndex : _gcrByteIndex;
            _currentHalfTrack = next;
            var nextTrackLength = RawTrackSize(Track);
            _pendingGcrByteIndex = previousTrackLength != 0
                ? (previousByteIndex * nextTrackLength) / previousTrackLength
                : 0;
            _gcrHalfTrackNumber = 0;
            _gcrTrackLength = nextTrackLength;
        }

        private static bool IsSingleStep(int stepCount)
        {
            return stepCount is 1 or -1;
        }

        public void AdvanceByte(D64Image image)
        {
            EnsureGcrTrack(image);

            _currentByte = _gcrTrack[_gcrByteIndex++];
            _currentByteIndex = _gcrByteIndex - 1;
            if (_gcrByteIndex >= _gcrTrackLength)
                _gcrByteIndex = 0;
        }

        private bool IsCurrentByteInSyncRun()
        {
            if (_currentByte != 0xFF)
                return false;
            if (_currentByteIndex < 0 || _gcrTrackLength == 0)
                return true;

            var previousIndex = _currentByteIndex == 0 ? _gcrTrackLength - 1 : _currentByteIndex - 1;
            var nextIndex = _currentByteIndex + 1;
            if (nextIndex >= _gcrTrackLength)
                nextIndex = 0;

            return _gcrTrack[previousIndex] == 0xFF || _gcrTrack[nextIndex] == 0xFF;
        }

        public static int SectorCount(int track) => track switch
        {
            >= 1 and <= 17 => 21,
            >= 18 and <= 24 => 19,
            >= 25 and <= 30 => 18,
            >= 31 and <= 35 => 17,
            _ => 0,
        };

        private static int RawTrackSize(int track) => track switch
        {
            >= 1 and <= 17 => 7692,
            >= 18 and <= 24 => 7142,
            >= 25 and <= 30 => 6666,
            >= 31 and <= 35 => 6250,
            _ => MaxGcrTrackSize,
        };

        private static int GapSize(int track) => track switch
        {
            >= 1 and <= 17 => 8,
            >= 18 and <= 24 => 17,
            >= 25 and <= 30 => 12,
            >= 31 and <= 35 => 9,
            _ => 8,
        };
    }
}
