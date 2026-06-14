namespace ViceSharp.Chips.IEC;

/// <summary>
/// GCR 4/5 encoding/decoding for 1541 disk format
/// </summary>
public static class GcrCodec
{
    private static readonly byte[] _decodeTable = new byte[32]
    {
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x08, 0x00, 0x01, 0x00, 0x0C, 0x04, 0x05,
        0x00, 0x00, 0x02, 0x03, 0x00, 0x0F, 0x06, 0x07,
        0x00, 0x09, 0x0A, 0x0B, 0x00, 0x0D, 0x0E, 0x00
    };

    private static readonly byte[] _encodeTable = new byte[16]
    {
        0x0A, 0x0B, 0x12, 0x13,
        0x0E, 0x0F, 0x16, 0x17,
        0x09, 0x19, 0x1A, 0x1B,
        0x0D, 0x1D, 0x1E, 0x15
    };

    public static byte Encode4(byte value)
    {
        return _encodeTable[value & 0x0F];
    }

    public static byte Decode5(byte value)
    {
        return _decodeTable[value & 0x1F];
    }

    public static void EncodeBlock(ReadOnlySpan<byte> source, Span<byte> destination)
    {
        if ((source.Length & 0x03) != 0)
            throw new ArgumentException("GCR source length must be a multiple of 4 bytes.", nameof(source));

        var required = (source.Length / 4) * 5;
        if (destination.Length < required)
            throw new ArgumentException("GCR destination is too small.", nameof(destination));

        for (int si = 0, di = 0; si < source.Length; si += 4, di += 5)
        {
            ulong bits = 0;
            for (var i = 0; i < 4; i++)
            {
                var value = source[si + i];
                bits = (bits << 5) | Encode4((byte)(value >> 4));
                bits = (bits << 5) | Encode4((byte)(value & 0x0F));
            }

            destination[di + 0] = (byte)(bits >> 32);
            destination[di + 1] = (byte)(bits >> 24);
            destination[di + 2] = (byte)(bits >> 16);
            destination[di + 3] = (byte)(bits >> 8);
            destination[di + 4] = (byte)bits;
        }
    }

    public static void DecodeBlock(ReadOnlySpan<byte> source, Span<byte> destination)
    {
        if (source.Length % 5 != 0)
            throw new ArgumentException("GCR source length must be a multiple of 5 bytes.", nameof(source));

        var required = (source.Length / 5) * 4;
        if (destination.Length < required)
            throw new ArgumentException("GCR destination is too small.", nameof(destination));

        for (int si = 0, di = 0; si < source.Length; si += 5, di += 4)
        {
            ulong bits =
                ((ulong)source[si + 0] << 32) |
                ((ulong)source[si + 1] << 24) |
                ((ulong)source[si + 2] << 16) |
                ((ulong)source[si + 3] << 8) |
                source[si + 4];

            for (var i = 0; i < 4; i++)
            {
                var hi = Decode5((byte)(bits >> 35));
                bits <<= 5;
                var lo = Decode5((byte)(bits >> 35));
                bits <<= 5;
                destination[di + i] = (byte)((hi << 4) | lo);
            }
        }
    }
}
