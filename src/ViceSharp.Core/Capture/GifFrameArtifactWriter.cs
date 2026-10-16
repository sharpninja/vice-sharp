namespace ViceSharp.Core.Capture;

/// <summary>
/// Writes one BGRA frame as a GIF89a image. Colors are quantized to the
/// 3-3-2 table VICE-style GIF output can carry in a 256-entry global palette.
/// The LZW stream follows the GIF specification (clear code, then indices, then EOI).
/// </summary>
public static class GifFrameArtifactWriter
{
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

        await using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await using var body = new MemoryStream();
        WriteGif(body, bgraFrame.Span, width, height);
        body.Position = 0;
        await body.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static void WriteGif(Stream stream, ReadOnlySpan<byte> bgra, int width, int height)
    {
        stream.Write("GIF89a"u8);
        WriteLe16(stream, width);
        WriteLe16(stream, height);
        stream.WriteByte(0xF7);
        stream.WriteByte(0);
        stream.WriteByte(0);
        for (var i = 0; i < 256; i++)
        {
            var r = (i >> 5) & 7;
            var g = (i >> 2) & 7;
            var b = i & 3;
            stream.WriteByte((byte)(r * 255 / 7));
            stream.WriteByte((byte)(g * 255 / 7));
            stream.WriteByte((byte)(b * 255 / 3));
        }

        stream.WriteByte(0x2C);
        WriteLe16(stream, 0);
        WriteLe16(stream, 0);
        WriteLe16(stream, width);
        WriteLe16(stream, height);
        stream.WriteByte(0);

        var count = width * height;
        var indexes = new byte[count];
        for (var pixel = 0; pixel < count; pixel++)
        {
            var offset = pixel * 4;
            var blue = bgra[offset];
            var green = bgra[offset + 1];
            var red = bgra[offset + 2];
            indexes[pixel] = (byte)(((red >> 5) << 5) | ((green >> 5) << 2) | (blue >> 6));
        }

        WriteLzw(stream, indexes);
        stream.WriteByte(0x3B);
    }

    private static void WriteLzw(Stream stream, ReadOnlySpan<byte> indexes)
    {
        stream.WriteByte(8);
        var block = new List<byte>(indexes.Length);
        var accumulator = 0;
        var bitCount = 0;
        var codeSize = 9;
        var nextCode = 258;
        var dictionary = new Dictionary<ulong, int>();

        void Emit(int code)
        {
            accumulator |= code << bitCount;
            bitCount += codeSize;
            while (bitCount >= 8)
            {
                block.Add((byte)accumulator);
                accumulator >>= 8;
                bitCount -= 8;
            }
        }

        void ResetDictionary()
        {
            dictionary.Clear();
            codeSize = 9;
            nextCode = 258;
        }

        Emit(256);
        if (indexes.Length == 0)
        {
            Emit(257);
            FlushBits();
            WriteBlocks();
            return;
        }

        var prefix = (int)indexes[0];
        for (var i = 1; i < indexes.Length; i++)
        {
            var pixel = indexes[i];
            var key = ((ulong)(uint)prefix << 8) | pixel;
            if (dictionary.TryGetValue(key, out var existing))
            {
                prefix = existing;
                continue;
            }

            Emit(prefix);
            if (nextCode < 4096)
            {
                dictionary[key] = nextCode;
                nextCode++;
                if (nextCode == (1 << codeSize) && codeSize < 12)
                    codeSize++;
            }
            else
            {
                Emit(256);
                ResetDictionary();
            }

            prefix = pixel;
        }

        Emit(prefix);
        Emit(257);
        FlushBits();
        WriteBlocks();

        void FlushBits()
        {
            if (bitCount > 0)
            {
                block.Add((byte)accumulator);
                accumulator = 0;
                bitCount = 0;
            }
        }

        void WriteBlocks()
        {
            var offset = 0;
            while (offset < block.Count)
            {
                var length = Math.Min(255, block.Count - offset);
                stream.WriteByte((byte)length);
                for (var n = 0; n < length; n++)
                    stream.WriteByte(block[offset + n]);
                offset += length;
            }

            stream.WriteByte(0);
        }
    }

    private static void WriteLe16(Stream stream, int value)
    {
        stream.WriteByte((byte)value);
        stream.WriteByte((byte)(value >> 8));
    }
}
