namespace ViceSharp.Chips.Tape;

public sealed class Datasette
{
    private TapImage? _image;
    private TapPulseReader? _reader;

    public bool HasTape => _image is not null;

    public bool MotorEnabled { get; set; }

    public bool PlayPressed { get; set; }

    public bool Attach(TapImage image)
    {
        _image = image;
        _reader = image.CreatePulseReader();
        return true;
    }

    public void Detach()
    {
        _image = null;
        _reader = null;
    }

    public bool TryReadNextPulse(out int cycles)
    {
        cycles = 0;

        if (!MotorEnabled || !PlayPressed || _reader is null)
        {
            return false;
        }

        return _reader.TryReadNextPulse(out cycles);
    }
}
