namespace ViceSharp.Core.Capture;

/// <summary>
/// Minimal pure-C# baseline JFIF encoder for BGRA frame buffers.
/// Each 8x8 block is converted to Y, Cb, and Cr, transformed, quantized,
/// and Huffman coded. A block that is not flat emits nonzero AC coefficients.
/// Color is three components that share the luminance tables.
/// </summary>
public static class JpegFrameArtifactWriter
{
    // JPEG markers
    private const byte MarkerPrefix = 0xFF;
    private const byte SoiMarker = 0xD8; // Start of Image
    private const byte App0Marker = 0xE0; // JFIF APP0
    private const byte DqtMarker = 0xDB; // Define Quantization Table
    private const byte SofMarker = 0xC0; // Start of Frame (Baseline DCT)
    private const byte DhtMarker = 0xC4; // Define Huffman Table
    private const byte SosMarker = 0xDA; // Start of Scan
    private const byte EoiMarker = 0xD9; // End of Image

    // Standard luminance quantization table (quality ~50, per JPEG spec Annex K)
    private static readonly byte[] LumaQuantTable =
    [
        16, 11, 10, 16, 24,  40,  51,  61,
        12, 12, 14, 19, 26,  58,  60,  55,
        14, 13, 16, 24, 40,  57,  69,  56,
        14, 17, 22, 29, 51,  87,  80,  62,
        18, 22, 37, 56, 68,  109, 103, 77,
        24, 35, 55, 64, 81,  104, 113, 92,
        49, 64, 78, 87, 103, 121, 120, 101,
        72, 92, 95, 98, 112, 100, 103, 99
    ];

    // Minimal DC luminance Huffman codes (from JPEG spec example)
    private static readonly byte[] DcLumaLengths = [0, 1, 5, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0];
    private static readonly byte[] DcLumaValues  = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11];

    // Minimal AC luminance Huffman codes (from JPEG spec example)
    private static readonly byte[] AcLumaLengths = [0, 2, 1, 3, 3, 2, 4, 3, 5, 5, 4, 4, 0, 0, 1, 125];
    private static readonly byte[] AcLumaValues  =
    [
        0x01, 0x02, 0x03, 0x00, 0x04, 0x11, 0x05, 0x12,
        0x21, 0x31, 0x41, 0x06, 0x13, 0x51, 0x61, 0x07,
        0x22, 0x71, 0x14, 0x32, 0x81, 0x91, 0xA1, 0x08,
        0x23, 0x42, 0xB1, 0xC1, 0x15, 0x52, 0xD1, 0xF0,
        0x24, 0x33, 0x62, 0x72, 0x82, 0x09, 0x0A, 0x16,
        0x17, 0x18, 0x19, 0x1A, 0x25, 0x26, 0x27, 0x28,
        0x29, 0x2A, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39,
        0x3A, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49,
        0x4A, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59,
        0x5A, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69,
        0x6A, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78, 0x79,
        0x7A, 0x83, 0x84, 0x85, 0x86, 0x87, 0x88, 0x89,
        0x8A, 0x92, 0x93, 0x94, 0x95, 0x96, 0x97, 0x98,
        0x99, 0x9A, 0xA2, 0xA3, 0xA4, 0xA5, 0xA6, 0xA7,
        0xA8, 0xA9, 0xAA, 0xB2, 0xB3, 0xB4, 0xB5, 0xB6,
        0xB7, 0xB8, 0xB9, 0xBA, 0xC2, 0xC3, 0xC4, 0xC5,
        0xC6, 0xC7, 0xC8, 0xC9, 0xCA, 0xD2, 0xD3, 0xD4,
        0xD5, 0xD6, 0xD7, 0xD8, 0xD9, 0xDA, 0xE1, 0xE2,
        0xE3, 0xE4, 0xE5, 0xE6, 0xE7, 0xE8, 0xE9, 0xEA,
        0xF1, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6, 0xF7, 0xF8,
        0xF9, 0xFA
    ];

    private static readonly int[] ZigZag =
    [
        0, 1, 8, 16, 9, 2, 3, 10,
        17, 24, 32, 25, 18, 11, 4, 5,
        12, 19, 26, 33, 40, 48, 41, 34,
        27, 20, 13, 6, 7, 14, 21, 28,
        35, 42, 49, 56, 57, 50, 43, 36,
        29, 22, 15, 23, 30, 37, 44, 51,
        58, 59, 52, 45, 38, 31, 39, 46,
        53, 60, 61, 54, 47, 55, 62, 63
    ];

    private static readonly Dictionary<int, (uint Code, int Length)> AcCodes = BuildAcCodes();

    public static async Task WriteBgraAsync(
        ReadOnlyMemory<byte> bgraFrame,
        int width,
        int height,
        string filePath,
        CancellationToken cancellationToken = default,
        int quality = 50)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var expectedLength = checked(width * height * 4);
        if (bgraFrame.Length != expectedLength)
            throw new ArgumentException("BGRA frame length does not match width and height.", nameof(bgraFrame));

        await using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await using var ms = new MemoryStream();

        WriteJfifJpeg(ms, bgraFrame.Span, width, height, quality);

        ms.Position = 0;
        await ms.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static void WriteJfifJpeg(Stream output, ReadOnlySpan<byte> bgra, int width, int height, int quality)
    {
        var quant = ScaleQuantTable(quality);
        using var writer = new BinaryWriter(output, System.Text.Encoding.ASCII, leaveOpen: true);

        // SOI
        writer.Write(MarkerPrefix);
        writer.Write(SoiMarker);

        // APP0 JFIF header
        WriteApp0(writer);

        // DQT: luminance quantization table
        WriteDqt(writer, quant);

        // SOF0: Start of Frame (grayscale, 1 component)
        WriteSof(writer, width, height);

        // DHT: DC + AC Huffman tables for luminance
        WriteDht(writer, 0x00, DcLumaLengths, DcLumaValues);
        WriteDht(writer, 0x10, AcLumaLengths, AcLumaValues);

        // SOS: Start of Scan header
        WriteSos(writer);

        // Scan data: simple grayscale, one 8x8 block with EOB, padded
        WriteScanData(writer, bgra, width, height, quant);

        // EOI
        writer.Write(MarkerPrefix);
        writer.Write(EoiMarker);
    }

    private static void WriteApp0(BinaryWriter writer)
    {
        // APP0 marker + length (16) + "JFIF\0" + version 1.1 + density units + Xdensity + Ydensity + thumbnail
        writer.Write(MarkerPrefix);
        writer.Write(App0Marker);
        writer.Write(new byte[] { 0x00, 0x10 }); // length = 16
        writer.Write(new byte[] { 0x4A, 0x46, 0x49, 0x46, 0x00 }); // "JFIF\0"
        writer.Write(new byte[] { 0x01, 0x01 }); // version 1.1
        writer.Write((byte)0x00); // pixel aspect ratio units: none
        writer.Write(new byte[] { 0x00, 0x01 }); // Xdensity = 1
        writer.Write(new byte[] { 0x00, 0x01 }); // Ydensity = 1
        writer.Write((byte)0x00); // Xthumbnail = 0
        writer.Write((byte)0x00); // Ythumbnail = 0
    }

    private static byte[] ScaleQuantTable(int quality)
    {
        // IJG quality scale. 50 keeps the Annex K luminance table.
        quality = Math.Clamp(quality, 1, 100);
        var scale = quality < 50 ? 5000 / quality : 200 - (quality * 2);
        var table = new byte[64];
        for (var i = 0; i < 64; i++)
        {
            var value = ((LumaQuantTable[i] * scale) + 50) / 100;
            if (value < 1)
                value = 1;
            else if (value > 255)
                value = 255;
            table[i] = (byte)value;
        }

        return table;
    }

    private static void WriteDqt(BinaryWriter writer, byte[] quant)
    {
        writer.Write(MarkerPrefix);
        writer.Write(DqtMarker);
        var length = (ushort)(2 + 1 + 64); // length field + table ID byte + 64 table values
        WriteUInt16BigEndian(writer, length);
        writer.Write((byte)0x00); // table ID 0, precision 0 (8-bit)
        writer.Write(quant);
    }

    private static void WriteSof(BinaryWriter writer, int width, int height)
    {
        // SOF0: baseline DCT, 8-bit, Y/Cb/Cr at 1x1. Chroma uses quant table 0.
        writer.Write(MarkerPrefix);
        writer.Write(SofMarker);
        WriteUInt16BigEndian(writer, 17);
        writer.Write((byte)8);
        WriteUInt16BigEndian(writer, (ushort)height);
        WriteUInt16BigEndian(writer, (ushort)width);
        writer.Write((byte)3);
        for (byte id = 1; id <= 3; id++)
        {
            writer.Write(id);
            writer.Write((byte)0x11);
            writer.Write((byte)0);
        }
    }

    private static void WriteDht(BinaryWriter writer, byte tableId, byte[] lengths, byte[] values)
    {
        writer.Write(MarkerPrefix);
        writer.Write(DhtMarker);
        var totalLength = (ushort)(2 + 1 + 16 + values.Length);
        WriteUInt16BigEndian(writer, totalLength);
        writer.Write(tableId);
        writer.Write(lengths);
        writer.Write(values);
    }

    private static void WriteSos(BinaryWriter writer)
    {
        // SOS: Y, Cb, Cr interleaved. Each component uses DC table 0 and AC table 0.
        writer.Write(MarkerPrefix);
        writer.Write(SosMarker);
        WriteUInt16BigEndian(writer, 12);
        writer.Write((byte)3);
        for (byte id = 1; id <= 3; id++)
        {
            writer.Write(id);
            writer.Write((byte)0x00);
        }

        writer.Write((byte)0);
        writer.Write((byte)63);
        writer.Write((byte)0);
    }

    private static void WriteScanData(BinaryWriter writer, ReadOnlySpan<byte> bgra, int width, int height, byte[] quant)
    {
        int mcusX = (width + 7) / 8;
        int mcusY = (height + 7) / 8;
        var bits = new BitWriter(writer);
        var samples = new double[64];
        var quantized = new int[64];
        int prevY = 0;
        int prevCb = 0;
        int prevCr = 0;
        for (int my = 0; my < mcusY; my++)
        {
            for (int mx = 0; mx < mcusX; mx++)
            {
                EncodeComponent(bits, samples, quantized, quant, bgra, width, height, mx, my, 0, ref prevY);
                EncodeComponent(bits, samples, quantized, quant, bgra, width, height, mx, my, 1, ref prevCb);
                EncodeComponent(bits, samples, quantized, quant, bgra, width, height, mx, my, 2, ref prevCr);
            }
        }

        bits.Flush();
    }

    private static void EncodeComponent(
        BitWriter bits,
        double[] samples,
        int[] quantized,
        byte[] quant,
        ReadOnlySpan<byte> bgra,
        int width,
        int height,
        int mcuX,
        int mcuY,
        int component,
        ref int prevDc)
    {
        for (int y = 0; y < 8; y++)
        {
            var py = mcuY * 8 + y;
            if (py >= height)
                py = height - 1;
            for (int x = 0; x < 8; x++)
            {
                var px = mcuX * 8 + x;
                if (px >= width)
                    px = width - 1;
                SampleYCbCr(bgra, width, py, px, out var yVal, out var cb, out var cr);
                var level = component switch
                {
                    1 => cb,
                    2 => cr,
                    _ => yVal
                };
                samples[y * 8 + x] = level - 128;
            }
        }

        ForwardDct(samples);
        for (int i = 0; i < 64; i++)
        {
            var natural = ZigZag[i];
            quantized[i] = (int)Math.Round(samples[natural] / quant[i], MidpointRounding.AwayFromZero);
        }

        var dc = quantized[0];
        EncodeDcCoefficient(bits, dc - prevDc);
        prevDc = dc;
        EncodeAcCoefficients(bits, quantized);
    }

    private static void SampleYCbCr(ReadOnlySpan<byte> bgra, int width, int y, int x, out int yVal, out int cb, out int cr)
    {
        var i = (y * width + x) * 4;
        int b = bgra[i];
        int g = bgra[i + 1];
        int r = bgra[i + 2];
        yVal = (299 * r + 587 * g + 114 * b) / 1000;
        cb = 128 + ((-169 * r - 331 * g + 500 * b) / 1000);
        cr = 128 + ((500 * r - 419 * g - 81 * b) / 1000);
        if (cb < 0) cb = 0;
        if (cb > 255) cb = 255;
        if (cr < 0) cr = 0;
        if (cr > 255) cr = 255;
    }

    private static void ForwardDct(double[] block)
    {
        var temp = new double[64];
        for (int v = 0; v < 8; v++)
        {
            var cv = v == 0 ? 1.0 / Math.Sqrt(2.0) : 1.0;
            for (int u = 0; u < 8; u++)
            {
                var cu = u == 0 ? 1.0 / Math.Sqrt(2.0) : 1.0;
                double sum = 0;
                for (int y = 0; y < 8; y++)
                {
                    for (int x = 0; x < 8; x++)
                    {
                        sum += block[y * 8 + x]
                            * Math.Cos(((2 * x) + 1) * u * Math.PI / 16.0)
                            * Math.Cos(((2 * y) + 1) * v * Math.PI / 16.0);
                    }
                }

                temp[v * 8 + u] = 0.25 * cu * cv * sum;
            }
        }

        Array.Copy(temp, block, 64);
    }

    private static void EncodeAcCoefficients(BitWriter bits, int[] quantized)
    {
        var run = 0;
        for (var i = 1; i < 64; i++)
        {
            var coef = quantized[i];
            if (coef == 0)
            {
                run++;
                continue;
            }

            while (run > 15)
            {
                WriteAcSymbol(bits, 0xF0);
                run -= 16;
            }

            var category = Category(coef);
            WriteAcSymbol(bits, (run << 4) | category);
            WriteAmplitude(bits, coef, category);
            run = 0;
        }

        if (run > 0)
            WriteAcSymbol(bits, 0x00);
    }

    private static void WriteAcSymbol(BitWriter bits, int symbol)
    {
        if (!AcCodes.TryGetValue(symbol, out var code))
            throw new InvalidOperationException("JPEG AC symbol is not in the luminance Huffman table.");

        bits.WriteBits(code.Code, code.Length);
    }

    private static void WriteAmplitude(BitWriter bits, int value, int category)
    {
        if (category <= 0)
            return;

        var amplitude = value < 0 ? value - 1 + (1 << category) : value;
        bits.WriteBits((uint)amplitude, category);
    }

    private static int Category(int value)
    {
        var abs = Math.Abs(value);
        var category = 0;
        while (abs > 0)
        {
            category++;
            abs >>= 1;
        }

        return category;
    }

    private static Dictionary<int, (uint Code, int Length)> BuildAcCodes()
    {
        var map = new Dictionary<int, (uint Code, int Length)>();
        uint code = 0;
        var k = 0;
        for (var length = 1; length <= 16; length++)
        {
            var count = AcLumaLengths[length - 1];
            for (var i = 0; i < count; i++)
            {
                map[AcLumaValues[k]] = (code, length);
                code++;
                k++;
            }

            code <<= 1;
        }

        return map;
    }

    private static void EncodeDcCoefficient(BitWriter bits, int diff)
    {
        // Determine category (number of bits needed to represent diff)
        int category = 0;
        int absVal = Math.Abs(diff);
        int temp = absVal;
        while (temp > 0) { category++; temp >>= 1; }
        if (absVal == 0) category = 0;

        // From standard DC luminance Huffman table:
        // Category 0: code 00 (2 bits)
        // Category 1: code 010 (3 bits)
        // Category 2: code 011 (3 bits)
        // Category 3: code 100 (3 bits)
        // Category 4: code 101 (3 bits)
        // Category 5: code 110 (3 bits)
        // Category 6: code 1110 (4 bits)
        // Category 7: code 11110 (5 bits)
        // Category 8: code 111110 (6 bits)
        // Category 9: code 1111110 (7 bits)
        // Category 10: code 11111110 (8 bits)
        // Category 11: code 111111110 (9 bits)
        (uint code, int len) = category switch
        {
            0 => (0b00u, 2),
            1 => (0b010u, 3),
            2 => (0b011u, 3),
            3 => (0b100u, 3),
            4 => (0b101u, 3),
            5 => (0b110u, 3),
            6 => (0b1110u, 4),
            7 => (0b11110u, 5),
            8 => (0b111110u, 6),
            9 => (0b1111110u, 7),
            10 => (0b11111110u, 8),
            _ => (0b111111110u, 9)
        };

        bits.WriteBits(code, len);

        // Append the coefficient amplitude (category bits)
        if (category > 0)
        {
            int amplitude = diff < 0 ? diff - 1 + (1 << category) : diff;
            bits.WriteBits((uint)amplitude, category);
        }
    }

    private static void WriteUInt16BigEndian(BinaryWriter writer, ushort value)
    {
        writer.Write((byte)(value >> 8));
        writer.Write((byte)(value & 0xFF));
    }

    /// <summary>Bit-level writer that emits MSB-first with 0xFF byte stuffing.</summary>
    private sealed class BitWriter(BinaryWriter writer)
    {
        private uint _buffer;
        private int _bitsInBuffer;

        public void WriteBits(uint value, int count)
        {
            _buffer = (_buffer << count) | (value & (uint)((1 << count) - 1));
            _bitsInBuffer += count;

            while (_bitsInBuffer >= 8)
            {
                _bitsInBuffer -= 8;
                var b = (byte)((_buffer >> _bitsInBuffer) & 0xFF);
                writer.Write(b);
                if (b == 0xFF)
                    writer.Write((byte)0x00); // stuff byte
            }
        }

        public void Flush()
        {
            if (_bitsInBuffer > 0)
            {
                // Pad remaining bits with 1s (JPEG spec)
                var b = (byte)((_buffer << (8 - _bitsInBuffer)) | ((1u << (8 - _bitsInBuffer)) - 1u));
                writer.Write(b);
                if (b == 0xFF)
                    writer.Write((byte)0x00);
                _bitsInBuffer = 0;
            }
        }
    }
}
