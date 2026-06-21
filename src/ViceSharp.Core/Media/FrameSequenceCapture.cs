namespace ViceSharp.Core.Media;

using System;
using System.Buffers.Binary;
using System.IO;

/// <summary>
/// FR/TR: FR-MED (RUNTIME-CAPTURE-002 continuous multi-frame BMP capture).
/// Use case: Tee a stream of BGRA video frames from the emulator's video
/// chip into a target directory as individually numbered BMP files
/// (frame_000001.bmp, frame_000002.bmp, ...). Each call to
/// <see cref="CaptureFrame"/> advances the frame counter and emits one
/// 24-bit BMP artifact. Built on top of the same 24-bit BMP layout used
/// by RUNTIME-CAPTURE-001 (BmpFrameArtifactWriter) but synchronous, per
/// frame, and re-entrant against a long-running capture session.
/// Acceptance: An empty session produces no BMP files. A session that
/// submits N frames produces N files named frame_NNNNNN.bmp (1-indexed,
/// zero-padded to 6 digits). Disposing the session silently ignores
/// any subsequent frames.
/// </summary>
public sealed class FrameSequenceCapture : IVideoCaptureSink
{
    private readonly string _outputDir;
    private readonly FrameSequenceMode _mode;
    private byte[]? _lastWrittenFrame;
    private int _frameIndex;
    private int _framesConsidered;
    private bool _disposed;

    /// <summary>
    /// Creates a new capture session writing numbered BMP frames to
    /// <paramref name="outputDirectory"/>. The directory is created if
    /// it does not already exist.
    /// </summary>
    /// <param name="outputDirectory">Target directory for BMP artifacts.</param>
    /// <param name="mode">
    /// <see cref="FrameSequenceMode.AllFrames"/> writes every submitted frame;
    /// <see cref="FrameSequenceMode.UniqueFrames"/> skips a frame that is
    /// byte-identical to the previously written one (collapsing static screens to
    /// a compact, contiguously-numbered set of distinct frames).
    /// </param>
    public FrameSequenceCapture(string outputDirectory, FrameSequenceMode mode = FrameSequenceMode.AllFrames)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        _outputDir = outputDirectory;
        _mode = mode;
        Directory.CreateDirectory(outputDirectory);
    }

    /// <summary>Target directory for captured BMP frames.</summary>
    public string OutputDirectory => _outputDir;

    /// <summary>Whether every frame is written or only distinct (deduplicated) frames.</summary>
    public FrameSequenceMode Mode => _mode;

    /// <summary>Total number of BMP files written so far.</summary>
    public int FrameCount => _frameIndex;

    /// <summary>Total number of frames submitted (including ones skipped as duplicates).</summary>
    public int FramesConsidered => _framesConsidered;

    /// <summary>
    /// Persists a single BGRA frame as the next numbered BMP file in
    /// the output directory. If the session has been disposed, the
    /// call is silently ignored so late frames from the emulation
    /// pipeline cannot corrupt a closed session.
    /// </summary>
    /// <param name="bgra">Frame pixel data in BGRA8888 order, row-major,
    /// top-down. Length must equal <c>width * height * 4</c>.</param>
    /// <param name="width">Frame width in pixels (positive).</param>
    /// <param name="height">Frame height in pixels (positive).</param>
    public void CaptureFrame(ReadOnlySpan<byte> bgra, int width, int height)
    {
        if (_disposed) return;
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        var expectedLength = checked(width * height * 4);
        if (bgra.Length != expectedLength)
            throw new ArgumentException("BGRA frame length does not match width and height.", nameof(bgra));

        _framesConsidered++;

        // Unique mode collapses runs of identical frames (e.g. a static screen) by
        // skipping any frame byte-identical to the previously written one.
        if (_mode == FrameSequenceMode.UniqueFrames)
        {
            if (_lastWrittenFrame is not null && bgra.SequenceEqual(_lastWrittenFrame))
                return;

            if (_lastWrittenFrame is null || _lastWrittenFrame.Length != bgra.Length)
                _lastWrittenFrame = new byte[bgra.Length];
            bgra.CopyTo(_lastWrittenFrame);
        }

        _frameIndex++;
        var path = Path.Combine(_outputDir, $"frame_{_frameIndex:D6}.bmp");
        WriteBmp(path, bgra, width, height);
    }

    /// <summary>Marks the session as closed. Subsequent frames are ignored.</summary>
    public void Dispose()
    {
        _disposed = true;
    }

    private static void WriteBmp(string path, ReadOnlySpan<byte> bgra, int width, int height)
    {
        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        WriteHeader(stream, width, height);

        var rowStride = (width * 3 + 3) & ~3;
        var padding = rowStride - width * 3;
        Span<byte> paddingBytes = stackalloc byte[3];
        // 24-bit BMP scan order is bottom-up.
        for (var y = height - 1; y >= 0; y--)
        {
            var rowOffset = y * width * 4;
            for (var x = 0; x < width; x++)
            {
                var offset = rowOffset + x * 4;
                // BGRA -> 24-bit BGR (drop alpha).
                stream.WriteByte(bgra[offset]);
                stream.WriteByte(bgra[offset + 1]);
                stream.WriteByte(bgra[offset + 2]);
            }

            if (padding > 0)
                stream.Write(paddingBytes[..padding]);
        }
    }

    private static void WriteHeader(Stream stream, int width, int height)
    {
        var rowStride = (width * 3 + 3) & ~3;
        var pixelDataSize = rowStride * height;
        var fileSize = 54 + pixelDataSize;

        Span<byte> buf = stackalloc byte[54];
        buf[0] = (byte)'B';
        buf[1] = (byte)'M';
        BinaryPrimitives.WriteInt32LittleEndian(buf[2..], fileSize);
        BinaryPrimitives.WriteInt32LittleEndian(buf[6..], 0);
        BinaryPrimitives.WriteInt32LittleEndian(buf[10..], 54);

        // DIB header (BITMAPINFOHEADER, 40 bytes).
        BinaryPrimitives.WriteInt32LittleEndian(buf[14..], 40);
        BinaryPrimitives.WriteInt32LittleEndian(buf[18..], width);
        BinaryPrimitives.WriteInt32LittleEndian(buf[22..], height);
        BinaryPrimitives.WriteInt16LittleEndian(buf[26..], 1);
        BinaryPrimitives.WriteInt16LittleEndian(buf[28..], 24);
        BinaryPrimitives.WriteInt32LittleEndian(buf[30..], 0);
        BinaryPrimitives.WriteInt32LittleEndian(buf[34..], pixelDataSize);
        BinaryPrimitives.WriteInt32LittleEndian(buf[38..], 0);
        BinaryPrimitives.WriteInt32LittleEndian(buf[42..], 0);
        BinaryPrimitives.WriteInt32LittleEndian(buf[46..], 0);
        BinaryPrimitives.WriteInt32LittleEndian(buf[50..], 0);

        stream.Write(buf);
    }
}
