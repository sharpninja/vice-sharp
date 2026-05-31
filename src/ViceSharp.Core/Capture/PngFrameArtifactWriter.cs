using System.IO.Compression;
using System.Runtime.CompilerServices;

namespace ViceSharp.Core.Capture;

/// <summary>
/// Minimal pure-C# PNG encoder for BGRA frame buffers.
/// Produces a valid PNG (signature + IHDR + IDAT + IEND) without any
/// external image library dependencies. Only the magic bytes and a
/// structurally valid container are required by
/// FR-MED-001 / TR-MEDIA-001 / RUNTIME-CAPTURE-002.
/// </summary>
public static class PngFrameArtifactWriter
{
    // PNG signature: 8 bytes
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    public static async Task WriteBgraAsync(
        ReadOnlyMemory<byte> bgraFrame,
        int width,
        int height,
        string filePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var expectedLength = checked(width * height * 4);
        if (bgraFrame.Length != expectedLength)
            throw new ArgumentException("BGRA frame length does not match width and height.", nameof(bgraFrame));

        // Build the raw image data (filter byte + RGBA rows, top-down).
        // PNG stores RGBA not BGRA, so swap B and R channels.
        var rawData = BuildRawImageData(bgraFrame.Span, width, height);

        // Compress raw image data with zlib (Deflate with zlib header = PNG IDAT).
        var compressedData = CompressWithZlib(rawData);

        await using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await using var writer = new BinaryWriter(stream, System.Text.Encoding.ASCII, leaveOpen: true);

        // PNG signature
        writer.Write(PngSignature);

        // IHDR chunk: width, height, bit depth=8, color type=2 (RGB truecolor)
        WriteChunk(writer, "IHDR", BuildIhdr(width, height));

        // IDAT chunk: compressed image data
        WriteChunk(writer, "IDAT", compressedData);

        // IEND chunk: empty, marks end of file
        WriteChunk(writer, "IEND", []);

        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static byte[] BuildRawImageData(ReadOnlySpan<byte> bgra, int width, int height)
    {
        // Each row: 1 filter byte (0 = None) + width * 3 RGB bytes
        var rowStride = width * 3;
        var raw = new byte[height * (1 + rowStride)];
        var rawSpan = raw.AsSpan();

        for (var y = 0; y < height; y++)
        {
            var rawRow = rawSpan[(y * (1 + rowStride))..];
            rawRow[0] = 0; // filter byte: None
            var bgraRow = bgra[(y * width * 4)..];
            for (var x = 0; x < width; x++)
            {
                // BGRA -> RGB
                rawRow[1 + x * 3 + 0] = bgraRow[x * 4 + 2]; // R
                rawRow[1 + x * 3 + 1] = bgraRow[x * 4 + 1]; // G
                rawRow[1 + x * 3 + 2] = bgraRow[x * 4 + 0]; // B
            }
        }

        return raw;
    }

    private static byte[] CompressWithZlib(byte[] data)
    {
        using var compressed = new MemoryStream();
        using (var zlib = new ZLibStream(compressed, CompressionMode.Compress, leaveOpen: true))
        {
            zlib.Write(data, 0, data.Length);
        }
        return compressed.ToArray();
    }

    private static byte[] BuildIhdr(int width, int height)
    {
        var ihdr = new byte[13];
        WriteInt32BigEndian(ihdr, 0, width);
        WriteInt32BigEndian(ihdr, 4, height);
        ihdr[8] = 8;  // bit depth
        ihdr[9] = 2;  // color type: RGB truecolor
        ihdr[10] = 0; // compression method: deflate
        ihdr[11] = 0; // filter method: adaptive
        ihdr[12] = 0; // interlace method: none
        return ihdr;
    }

    private static void WriteChunk(BinaryWriter writer, string type, byte[] data)
    {
        // Chunk: length (4 bytes BE), type (4 ASCII bytes), data, CRC (4 bytes BE)
        var typeBytes = System.Text.Encoding.ASCII.GetBytes(type);

        // Length: big-endian uint32
        var lenBytes = new byte[4];
        WriteInt32BigEndian(lenBytes, 0, data.Length);
        writer.Write(lenBytes);

        // Type
        writer.Write(typeBytes);

        // Data
        if (data.Length > 0)
            writer.Write(data);

        // CRC over type + data
        var crcData = new byte[4 + data.Length];
        typeBytes.CopyTo(crcData, 0);
        data.CopyTo(crcData, 4);
        var crc = ComputeCrc32(crcData);
        var crcBytes = new byte[4];
        WriteInt32BigEndian(crcBytes, 0, (int)crc);
        writer.Write(crcBytes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteInt32BigEndian(byte[] buf, int offset, int value)
    {
        buf[offset + 0] = (byte)(value >> 24);
        buf[offset + 1] = (byte)(value >> 16);
        buf[offset + 2] = (byte)(value >> 8);
        buf[offset + 3] = (byte)(value);
    }

    // CRC-32 for PNG chunk validation
    private static readonly uint[] Crc32Table = BuildCrc32Table();

    private static uint[] BuildCrc32Table()
    {
        var table = new uint[256];
        for (uint n = 0; n < 256; n++)
        {
            var c = n;
            for (var k = 0; k < 8; k++)
                c = (c & 1) != 0 ? (0xEDB88320u ^ (c >> 1)) : (c >> 1);
            table[n] = c;
        }
        return table;
    }

    private static uint ComputeCrc32(byte[] data)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var b in data)
            crc = Crc32Table[(crc ^ b) & 0xFF] ^ (crc >> 8);
        return crc ^ 0xFFFFFFFFu;
    }
}
