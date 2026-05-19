using ViceSharp.Abstractions;

namespace ViceSharp.Chips.Tape;

public sealed class Datasette : ITapeDevice
{
    private TapImage? _image;
    private TapPulseReader? _reader;

    public DeviceId Id => new(0x0020);

    public string Name => "C2N Datasette";

    public bool HasTape => _image is not null;

    public bool IsAttached { get; private set; } = true;

    public bool MotorEnabled { get; set; }

    public bool PlayPressed { get; set; }

    public void Reset()
    {
        MotorEnabled = false;
        PlayPressed = false;
        _reader = _image?.CreatePulseReader();
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

    public bool TryReadNextPulse(out int cycles)
    {
        cycles = 0;

        if (!IsAttached || !MotorEnabled || !PlayPressed || _reader is null)
        {
            return false;
        }

        return _reader.TryReadNextPulse(out cycles);
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
