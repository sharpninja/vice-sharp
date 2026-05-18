using ViceSharp.Abstractions;
using ViceSharp.Chips.IEC;

namespace ViceSharp.Core.Wiring;

/// <summary>
/// Wires a 1541 drive's VIA2 chip to a mounted <see cref="D64DiskImageDevice"/>.
/// VIA2 owns:
///   PB0/PB1 (STP0/STP1)  - 4-phase head stepper Gray code (outputs)
///   PB2 (MTR)            - motor on/off (output)
///   PB3 (ACT)            - activity LED (output, ignored here)
///   PB4 (WPRT)           - write-protect input (active-low; high = ready)
///   PB7 (SYNC)           - sync byte detected input (deferred)
///   PA (D0..D7)          - read/write head data bus (sector byte stream)
///
/// Implementation:
///   - PB4 reflects disk mount state (Phase G2a).
///   - PB0/PB1 transitions drive head step in/out by Gray code (Phase G2b).
///   - PB2 transitions toggle motor on/off.
///   - When motor on, reads on PA return sequential bytes from the current
///     track + sector (cycles through sectors on the same track; track is
///     derived from accumulated head steps).
///
/// This is a "fast-path" simulation - not real GCR encoding, just a byte
/// stream sufficient for a drive ROM that uses U1-style block reads after
/// stepping the head. Phase G2c could add real GCR + byte-ready CB1 IRQ.
/// </summary>
public static class C1541Via2BusBinding
{
    private const byte WriteProtectBit = 0x10;
    private const byte MotorBit = 0x04;
    private const byte StepPhaseMask = 0x03;

    public static void Bind(Via6522 via2, D64DiskImageDevice? disk)
    {
        ArgumentNullException.ThrowIfNull(via2);

        var state = new DriveHeadState();

        var prevPbIn = via2.PortBInput;
        var prevPbOut = via2.PortBOutputChanged;
        var prevPaIn = via2.PortAInput;

        via2.PortBInput = () =>
        {
            byte composed = prevPbIn?.Invoke() ?? 0;
            if (disk is not null && !disk.IsEjected)
                composed |= WriteProtectBit;
            return composed;
        };

        via2.PortBOutputChanged = value =>
        {
            prevPbOut?.Invoke(value);
            state.UpdateMotor((value & MotorBit) != 0);
            state.UpdateHeadStep(value & StepPhaseMask);
        };

        via2.PortAInput = () =>
        {
            byte composed = prevPaIn?.Invoke() ?? 0xFF;
            if (disk is null || disk.IsEjected || !state.MotorOn)
                return composed;
            return state.ReadNextByte(disk.Image);
        };
    }

    /// <summary>
    /// State holder tracking one drive's head position, motor, and
    /// byte-stream cursor. The binding owns one instance per VIA2 bound.
    /// </summary>
    public sealed class DriveHeadState
    {
        public int Track => 18 + (_quarterTracks / 4);
        public bool MotorOn { get; private set; }

        private int _stepPhase = -1;
        private int _quarterTracks; // 4 quarter-steps = 1 full track
        private int _byteIndex;
        private int _sector;

        public void UpdateMotor(bool motorOn)
        {
            if (MotorOn == motorOn) return;
            MotorOn = motorOn;
            if (motorOn)
            {
                _byteIndex = 0;
                _sector = 0;
            }
        }

        public void UpdateHeadStep(int phase)
        {
            if (_stepPhase < 0) { _stepPhase = phase; return; }
            var delta = GrayDelta(_stepPhase, phase);
            _stepPhase = phase;
            if (delta == 0) return;

            var nextQt = _quarterTracks + delta;
            var nextTrack = 18 + (nextQt / 4);
            if (nextTrack < 1 || nextTrack > 35) return;
            var oldTrack = Track;
            _quarterTracks = nextQt;
            if (Track != oldTrack)
            {
                _byteIndex = 0;
                _sector = 0;
            }
        }

        private static int GrayDelta(int prev, int next)
        {
            var prevIdx = GrayToIndex(prev);
            var nextIdx = GrayToIndex(next);
            var diff = (nextIdx - prevIdx) & 0x03;
            return diff switch { 0 => 0, 1 => 1, 2 => 0, 3 => -1, _ => 0 };
        }

        private static int GrayToIndex(int gray) => gray switch
        {
            0 => 0,
            1 => 1,
            3 => 2,
            2 => 3,
            _ => 0,
        };

        public byte ReadNextByte(D64Image image)
        {
            var sectorsThisTrack = SectorCount(Track);
            var b = image.ReadSectorByte(Track, _sector, _byteIndex);
            _byteIndex++;
            if (_byteIndex >= 256)
            {
                _byteIndex = 0;
                _sector = (_sector + 1) % sectorsThisTrack;
            }
            return b;
        }

        public static int SectorCount(int track) => track switch
        {
            >= 1 and <= 17 => 21,
            >= 18 and <= 24 => 19,
            >= 25 and <= 30 => 18,
            >= 31 and <= 35 => 17,
            _ => 0,
        };
    }
}
