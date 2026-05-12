namespace ViceSharp.Core.Capture;

public static class BmpFrameArtifactWriter
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
        using var writer = new BinaryWriter(stream);
        WriteHeader(writer, width, height);

        var frame = bgraFrame.Span;
        var padding = (4 - (width * 3) % 4) % 4;
        Span<byte> paddingBytes = stackalloc byte[3];

        for (var y = height - 1; y >= 0; y--)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var rowOffset = y * width * 4;
            for (var x = 0; x < width; x++)
            {
                var offset = rowOffset + x * 4;
                writer.Write(frame[offset]);
                writer.Write(frame[offset + 1]);
                writer.Write(frame[offset + 2]);
            }

            writer.Write(paddingBytes[..padding]);
        }
    }

    private static void WriteHeader(BinaryWriter writer, int width, int height)
    {
        var rowSize = (width * 3 + 3) & ~3;
        var pixelDataSize = rowSize * height;
        var fileSize = 54 + pixelDataSize;

        writer.Write((byte)'B');
        writer.Write((byte)'M');
        writer.Write(fileSize);
        writer.Write(0);
        writer.Write(54);
        writer.Write(40);
        writer.Write(width);
        writer.Write(height);
        writer.Write((short)1);
        writer.Write((short)24);
        writer.Write(0);
        writer.Write(pixelDataSize);
        writer.Write(0);
        writer.Write(0);
        writer.Write(0);
        writer.Write(0);
    }
}
