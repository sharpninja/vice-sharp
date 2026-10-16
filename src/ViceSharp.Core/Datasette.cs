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

    private bool _playPressed;
    private bool _recordPressed;
    private Action<bool>? _playRecorder;
    private Action<string, int, byte[]?>? _transportRecorder;
    private Action<bool>? _recordRecorder;

    /// <summary>
    /// PLAY button. A change notifies an open recording. VICE
    /// datasette_event_record stores EVENT_DATASETTE for the command.
    /// A repeat of the same level is not another event.
    /// </summary>
    public bool PlayPressed
    {
        get => _playPressed;
        set => SetButton(ref _playPressed, value, _playRecorder);
    }

    /// <summary>
    /// True when the RECORD button is pressed. Enables TryWritePulse and
    /// asserts the SENSE line low (same as PlayPressed).
    /// VICE datasette.c: RecordPressed drives record mode and is its own
    /// EVENT_DATASETTE command.
    /// </summary>
    public bool RecordPressed
    {
        get => _recordPressed;
        set => SetButton(ref _recordPressed, value, _recordRecorder);
    }

    /// <summary>
    /// FR-SNP-003. Observes PLAY and RECORD edges. Playback applies the
    /// stored edge with observation suppressed.
    /// </summary>
    internal void AttachControlRecorder(Action<bool>? playRecorder, Action<bool>? recordRecorder)
    {
        _playRecorder = playRecorder;
        _recordRecorder = recordRecorder;
    }

    /// <summary>
    /// FR-SNP-003. Observes rewind, eject, seek, and insert. VICE
    /// datasette_control records EVENT_DATASETTE before applying the
    /// command. Eject is an image detach on that same external path.
    /// </summary>
    internal void AttachTransportRecorder(Action<string, int, byte[]?>? recorder)
    {
        _transportRecorder = recorder;
    }

    private static void SetButton(ref bool field, bool value, Action<bool>? recorder)
    {
        if (field == value)
            return;

        field = value;
        recorder?.Invoke(value);
    }

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

    /// <summary>
    /// Puts the captured PLAY and RECORD levels back. Does not record an
    /// edge. VICE restores the datasette control with the snapshot.
    /// </summary>
    internal void RestoreControls(bool playPressed, bool recordPressed)
    {
        _playPressed = playPressed;
        _recordPressed = recordPressed;
    }

    /// <summary>Pulse-data cursor. Zero when no tape is loaded.</summary>
    internal int PulseOffset => _reader?.Offset ?? 0;

    /// <summary>TAP bytes for the mounted image. Null when no tape is loaded.</summary>
    internal byte[]? CopyImage() => _image?.ToImageBytes();

    /// <summary>
    /// Puts the mounted image back without recording an insert or eject.
    /// VICE restores the tape image from the datasette snapshot module.
    /// </summary>
    internal void RestoreImage(byte[]? image)
    {
        if (image is null || image.Length == 0)
        {
            _image = null;
            _reader = null;
            return;
        }

        if (!TapImage.TryAttach(image, out var parsed) || parsed is null)
            return;

        AttachCore(parsed);
    }

    /// <summary>
    /// Puts the pulse cursor back. VICE tape_snapshot restores the image
    /// position beside the pending alarm.
    /// </summary>
    internal void RestorePulseOffset(int offset)
    {
        _reader?.RestoreOffset(offset);
    }

    /// <summary>Motor spin-up counter. VICE saves motor_stop timing with the datasette.</summary>
    internal long MotorRampCounter => _motorRampCounter;

    internal bool MotorRampComplete => _motorRampComplete;

    internal bool MotorRampStarted => _motorRampStarted;

    internal void RestoreMotorRamp(long counter, bool complete, bool started)
    {
        _motorRampCounter = counter;
        _motorRampComplete = complete;
        _motorRampStarted = started;
    }

    public void Reset()
    {
        MotorEnabled = false;
        _playPressed = false;
        _recordPressed = false;
        _reader = _image?.CreatePulseReader();
        _motorRampCounter = 0;
        _motorRampComplete = false;
        _motorRampStarted = false;
        _recordBuffer.Clear();
    }

    public void Attach()
    {
        if (IsAttached)
            return;

        IsAttached = true;
        _transportRecorder?.Invoke("attach", 0, null);
    }

    public void Detach()
    {
        if (!IsAttached)
            return;

        IsAttached = false;
        _transportRecorder?.Invoke("detach", 0, null);
    }

    /// <summary>
    /// Puts the peripheral connection back without recording an edge.
    /// </summary>
    internal void RestoreAttached(bool attached) => IsAttached = attached;

    public void InsertTape(ReadOnlySpan<byte> tapeImage)
    {
        if (!TapImage.TryAttach(tapeImage, out var image))
            throw new ArgumentException("Tape image must be a supported TAP image.", nameof(tapeImage));

        AttachCore(image!);
        _transportRecorder?.Invoke("insert", 0, tapeImage.ToArray());
    }

    public void EjectTape()
    {
        if (_image is null && _reader is null)
            return;

        _image = null;
        _reader = null;
        _transportRecorder?.Invoke("eject", 0, null);
    }

    public bool Attach(TapImage image)
    {
        AttachCore(image);
        _transportRecorder?.Invoke("insert", 0, image.ToImageBytes());
        return true;
    }

    private void AttachCore(TapImage image)
    {
        _image = image;
        _reader = image.CreatePulseReader();
        IsAttached = true;
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
        if (_reader is null)
            return;

        _reader.Rewind();
        _transportRecorder?.Invoke("rewind", 0, null);
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

        var moved = _reader.TrySeekToPulse(pulseIndex);
        if (moved)
            _transportRecorder?.Invoke("seek", pulseIndex, null);

        return moved;
    }
}
