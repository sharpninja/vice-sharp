namespace ViceSharp.Chips.Tape;

public sealed class TapImage
{
    private static readonly byte[] Header = "C64-TAPE-RAW"u8.ToArray();
    private readonly byte[] _pulseData;

    private TapImage(byte version, byte[] pulseData)
    {
        Version = version;
        _pulseData = pulseData;
    }

    public byte Version { get; }

    public int PulseDataLength => _pulseData.Length;

    public static bool TryAttach(ReadOnlySpan<byte> imageData, out TapImage? image)
    {
        image = null;

        if (imageData.Length < 20 || !imageData[..Header.Length].SequenceEqual(Header))
        {
            return false;
        }

        var version = imageData[12];
        if (version > 1)
        {
            return false;
        }

        var declaredLength = imageData[16]
            | (imageData[17] << 8)
            | (imageData[18] << 16)
            | (imageData[19] << 24);

        if (declaredLength < 0 || imageData.Length - 20 != declaredLength)
        {
            return false;
        }

        image = new TapImage(version, imageData[20..].ToArray());
        return true;
    }

    public TapPulseReader CreatePulseReader() => new(_pulseData, Version);
}
