using ViceSharp.Abstractions;
using ViceSharp.Chips.Tape;

namespace ViceSharp.Core;

/// <summary>
/// C2N Datasette peripheral emulation.
///
/// Motor ramp (TR-TAPE-EDGE-001): VICE models a 32,000-cycle motor spin-up
/// delay before tape pulses are delivered (datasette/datasette.c:62
/// MOTOR_DELAY=32000). Tick() advances the ramp counter when MotorEnabled
/// is true; TryReadNextPulse blocks during the ramp period.
///
/// Sense line (TR-TAP-EDGE-001): SenseLine returns false (low) when any
/// Datasette button (PlayPressed or RecordPressed) is pressed. Machine glue
/// maps this active-low line to the owning board's input port.
///
/// Record mode (TR-TAP-EDGE-001): When RecordPressed and MotorEnabled are
/// true, TryWritePulse(cycles) stores a pulse to the internal record buffer.
/// RecordedPulseCount tracks the number of pulses written.
/// VICE datasette/datasette.c: record path stores pulses to image buffer.
/// </summary>
public sealed class Datasette : ITapeDevice
{
    // VICE MOTOR_DELAY constant (datasette/datasette.c:62): 32,000 device cycles.
    private const int MotorRampCycles = 32_000;

    private TapImage? _image;
    private TapPulseReader? _reader;

    // Motor ramp state machine.
    // _motorRampStarted tracks whether Tick() has been called with motor on.
    // Before any Tick() call, TryReadNextPulse delivers pulses immediately
    // (backward-compatible with tests that do not use the Tick() timing path).
    // Once Tick() is called with motor on, the 32,000-cycle ramp applies.
    private long _motorRampCounter = 0;
    private bool _motorRampComplete = false;
    private bool _motorRampStarted = false;

    // Record mode buffer (stored pulses for inspection or future playback).
    private readonly List<int> _recordBuffer = new();

    public DeviceId Id => new(0x0020);

    public string Name => "C2N Datasette";

    public bool HasTape => _image is not null;

    public bool IsAttached { get; private set; } = true;

    public bool MotorEnabled { get; set; }

    public bool PlayPressed { get; set; }

    /// <summary>
    /// True when the RECORD button is pressed. Enables TryWritePulse and
    /// asserts the SENSE line low (same as PlayPressed).
    /// VICE datasette.c: RecordPressed drives record mode.
    /// </summary>
    public bool RecordPressed { get; set; }

    /// <summary>
    /// SENSE line state as driven by the Datasette hardware.
    /// False (low) when any button is pressed (PlayPressed or RecordPressed);
    /// true (high) when no button is pressed. The owning machine maps this
    /// line to its input port.
    /// VICE datasette.c: sense line is active-low, asserted by PLAY or RECORD.
    /// </summary>
    public bool SenseLine => !(PlayPressed || RecordPressed);

    /// <summary>
    /// Number of pulse values stored by TryWritePulse calls (record mode).
    /// Resets to zero when Reset() is called.
    /// </summary>
    public int RecordedPulseCount => _recordBuffer.Count;

    public void Reset()
    {
        MotorEnabled = false;
        PlayPressed = false;
        RecordPressed = false;
        _reader = _image?.CreatePulseReader();
        _motorRampCounter = 0;
        _motorRampComplete = false;
        _motorRampStarted = false;
        _recordBuffer.Clear();
    }

    public void Attach() => IsAttached = true;

    public void Detach() => IsAttached = false;

    public void InsertTape(ReadOnlySpan<byte> tapeImage)
    {
        if (!TapImage.TryAttach(tapeImage, out var image))
            throw new ArgumentException("Tape image must be a supported TAP image.", nameof(tapeImage));

        Attach(image!);
    }

    public void EjectTape()
    {
        _image = null;
        _reader = null;
    }

    public bool Attach(TapImage image)
    {
        _image = image;
        _reader = image.CreatePulseReader();
        IsAttached = true;
        return true;
    }

    /// <summary>
    /// Advance the Datasette state machine by one machine cycle.
    ///
    /// Motor ramp: when MotorEnabled is true, tracks the 32,000-cycle ramp.
    /// The ramp only activates after the first Tick() call with motor on;
    /// before any Tick(), TryReadNextPulse delivers pulses immediately
    /// (backward-compatible with tests that manage timing externally).
    /// After the ramp completes, pulses are delivered. Motor off resets
    /// the ramp so a full new spin-up is required on next motor-on.
    /// VICE datasette/datasette.c:62 MOTOR_DELAY=32000,
    /// datasette.c:1196 motor_stop_clk reset on motor off.
    /// </summary>
    public void Tick()
    {
        if (MotorEnabled)
        {
            if (!_motorRampStarted)
            {
                // First Tick with motor on: begin the ramp countdown.
                _motorRampStarted = true;
                _motorRampComplete = false;
                _motorRampCounter = 0;
            }
            if (!_motorRampComplete)
            {
                _motorRampCounter++;
                if (_motorRampCounter >= MotorRampCycles)
                    _motorRampComplete = true;
            }
        }
        else
        {
            // Motor off: reset ramp so a new spin-up is required.
            _motorRampCounter = 0;
            _motorRampComplete = false;
            _motorRampStarted = false;
        }
    }

    public bool TryReadNextPulse(out int cycles)
    {
        cycles = 0;

        // Ramp only blocks when Tick() has been called with motor on.
        // If Tick() has never been called (external pulse management),
        // the ramp is not applied (backward-compatible behavior).
        // VICE datasette.c:62 MOTOR_DELAY=32000 (applies when clocked).
        bool rampBlocking = _motorRampStarted && !_motorRampComplete;
        if (!IsAttached || !MotorEnabled || !PlayPressed || _reader is null || rampBlocking)
        {
            return false;
        }

        return _reader.TryReadNextPulse(out cycles);
    }

    /// <summary>
    /// Store a pulse value (in machine cycles) to the record buffer.
    /// Returns true when RecordPressed and MotorEnabled are both true;
    /// returns false otherwise (motor off or not in record mode).
    /// VICE datasette.c: record mode appends pulses to the tape image buffer.
    /// </summary>
    public bool TryWritePulse(int cycles)
    {
        if (!MotorEnabled || !RecordPressed)
            return false;

        _recordBuffer.Add(cycles);
        return true;
    }

    /// <summary>
    /// Rewind the tape to pulse zero. Safe no-op when no tape is inserted.
    /// </summary>
    public void Rewind()
    {
        _reader?.Rewind();
    }

    /// <summary>
    /// Position the tape cursor at the given pulse index. Returns false
    /// when no tape is inserted, the index is negative, or the index
    /// exceeds the pulse count; on failure the cursor is left unchanged.
    /// </summary>
    public bool SeekTo(int pulseIndex)
    {
        if (_reader is null)
        {
            return false;
        }

        return _reader.TrySeekToPulse(pulseIndex);
    }
}
