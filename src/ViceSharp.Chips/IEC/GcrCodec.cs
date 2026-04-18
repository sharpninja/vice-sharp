namespace ViceSharp.Chips.IEC;

/// <summary>
/// GCR 4/5 encoding/decoding for 1541 disk format
/// </summary>
public static class GcrCodec
{
    private static readonly byte[] _decodeTable = new byte[32]
    {
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x08, 0x00, 0x0C, 0x00, 0x04, 0x08, 0x00,
        0x00, 0x02, 0x00, 0x0A, 0x00, 0x0F, 0x06, 0x00,
        0x00, 0x00, 0x03, 0x0B, 0x0D, 0x05, 0x09, 0x0E
    };

    private static readonly byte[] _encodeTable = new byte[16]
    {
        0x0A, 0x0B, 0x12, 0x13,
        0x0E, 0x1F, 0x16, 0x17,
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
        for (int i = 0; i < source.Length; i += 4)
        {
            uint w = (uint)(source[i+0] | (source[i+1] << 8) | (source[i+2] << 16) | (source[i+3] << 24));

            destination[i+0] = Encode4((byte)(w >> 0));
            destination[i+1] = Encode4((byte)(w >> 4));
            destination[i+2] = Encode4((byte)(w >> 8));
            destination[i+3] = Encode4((byte)(w >> 12));
            destination[i+4] = Encode4((byte)(w >> 16));
        }
    }

    public static void DecodeBlock(ReadOnlySpan<byte> source, Span<byte> destination)
    {
        for (int i = 0; i < destination.Length; i += 4)
        {
            uint w = 0;
            w |= (uint)Decode5(source[i+0]) << 0;
            w |= (uint)Decode5(source[i+1]) << 4;
            w |= (uint)Decode5(source[i+2]) << 8;
            w |= (uint)Decode5(source[i+3]) << 12;
            w |= (uint)Decode5(source[i+4]) << 16;

            destination[i+0] = (byte)(w >> 0);
            destination[i+1] = (byte)(w >> 8);
            destination[i+2] = (byte)(w >> 16);
            destination[i+3] = (byte)(w >> 24);
        }
    }
}