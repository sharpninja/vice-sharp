namespace ViceSharp.Core.Media;

using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;

/// <summary>
/// FR/TR: FR-MED-003 (BACKFILL-MEDIA WAV audio recording).
/// Use case: Tee PCM audio samples from the emulator (SID stream via
/// IAudioBackend or similar) into an uncompressed RIFF/WAVE 16-bit PCM
/// file. Supports mono / stereo with configurable sample rate
/// (44100/48000/96000 Hz). The recorder writes a placeholder canonical
/// 44-byte header on construction, appends LE16 samples as WriteSamples
/// is called, and rewinds the stream on Stop to patch the final RIFF
/// + data chunk sizes.
/// Acceptance: Output bytes parse as a valid WAV file (RIFF/WAVE +
/// fmt chunk + data chunk) whose data-chunk size matches submitted
/// sample bytes and whose fmt chunk reflects sample-rate / channels.
/// </summary>
public sealed class WavAudioRecorder : ViceSharp.Abstractions.IAudioRecorder
{
    /// <summary>Canonical RIFF/WAVE header size for a single-fmt + single-data layout.</summary>
    public const int HeaderSize = 44;

    private const int BitsPerSample = 16;
    private const int BytesPerSample = BitsPerSample / 8;
    private const ushort AudioFormatPcm = 1;
    private const uint FmtChunkSize = 16;

    private readonly Stream _output;
    private readonly int _sampleRate;
    private readonly int _channels;
    private readonly long _headerStartPosition;
    private uint _dataBytesWritten;
    private bool _stopped;
    private bool _disposed;

    /// <summary>
    /// Constructs a new WAV recorder writing to <paramref name="output"/>.
    /// </summary>
    /// <param name="output">Destination stream. Must be writable + seekable.</param>
    /// <param name="sampleRate">Sample rate in Hz (e.g. 44100/48000/96000).</param>
    /// <param name="channels">Channel count (1 = mono, 2 = stereo). Caller must
    /// interleave stereo samples as [L, R, L, R, ...] short pairs.</param>
    public WavAudioRecorder(Stream output, int sampleRate = 44100, int channels = 1)
    {
        ArgumentNullException.ThrowIfNull(output);
        if (!output.CanWrite) throw new ArgumentException("Output stream must be writable.", nameof(output));
        if (!output.CanSeek) throw new ArgumentException("Output stream must be seekable.", nameof(output));
        if (sampleRate <= 0) throw new ArgumentOutOfRangeException(nameof(sampleRate));
        if (channels < 1 || channels > 2) throw new ArgumentOutOfRangeException(nameof(channels), "Mono or stereo only.");

        _output = output;
        _sampleRate = sampleRate;
        _channels = channels;
        _headerStartPosition = output.Position;

        WritePlaceholderHeader();
    }

    /// <summary>Sample rate in Hz as written to the fmt chunk.</summary>
    public int SampleRate => _sampleRate;

    /// <summary>Channel count (1 = mono, 2 = stereo).</summary>
    public int Channels => _channels;

    /// <summary>Total number of sample bytes written to the data chunk so far.</summary>
    public uint DataBytesWritten => _dataBytesWritten;

    /// <summary>
    /// Appends a buffer of 16-bit PCM samples to the data chunk. For stereo,
    /// the buffer must be L/R interleaved (each pair = 1 frame).
    /// </summary>
    public void WriteSamples(ReadOnlySpan<short> samples)
    {
        if (_stopped) throw new InvalidOperationException("Cannot write samples after Stop.");
        if (samples.IsEmpty) return;

        Span<byte> buffer = stackalloc byte[2];
        // Use a small chunk of the heap for larger writes to avoid one
        // stream call per sample.
        const int ChunkBytes = 4096;
        byte[] chunk = new byte[ChunkBytes];
        int filled = 0;

        foreach (var s in samples)
        {
            BinaryPrimitives.WriteInt16LittleEndian(buffer, s);
            chunk[filled++] = buffer[0];
            chunk[filled++] = buffer[1];
            if (filled == ChunkBytes)
            {
                _output.Write(chunk, 0, filled);
                filled = 0;
            }
        }
        if (filled > 0) _output.Write(chunk, 0, filled);
        _dataBytesWritten += (uint)(samples.Length * BytesPerSample);
    }

    /// <summary>
    /// Finalizes the WAV file by rewinding to the header and patching the
    /// RIFF + data chunk sizes. Idempotent; subsequent calls are no-ops.
    /// </summary>
    public void Stop()
    {
        if (_stopped) return;
        _stopped = true;

        long endPosition = _output.Position;

        // RIFF chunk size = 36 + dataBytes (covers everything after the
        // RIFF size field).
        uint riffSize = 36u + _dataBytesWritten;

        // Patch RIFF size at offset 4.
        _output.Seek(_headerStartPosition + 4, SeekOrigin.Begin);
        WriteLeUInt32(riffSize);

        // Patch data chunk size at offset 40.
        _output.Seek(_headerStartPosition + 40, SeekOrigin.Begin);
        WriteLeUInt32(_dataBytesWritten);

        // Restore position to end so any further (out-of-band) writes append.
        _output.Seek(endPosition, SeekOrigin.Begin);
        _output.Flush();
    }

    /// <summary>Stops the recorder (patching final sizes). Stream is not closed.</summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
    }

    private void WritePlaceholderHeader()
    {
        // RIFF chunk descriptor
        WriteAscii("RIFF");
        WriteLeUInt32(0); // placeholder for RIFF chunk size

        WriteAscii("WAVE");

        // fmt sub-chunk
        WriteAscii("fmt ");
        WriteLeUInt32(FmtChunkSize);
        WriteLeUInt16(AudioFormatPcm);
        WriteLeUInt16((ushort)_channels);
        WriteLeUInt32((uint)_sampleRate);
        uint byteRate = (uint)(_sampleRate * _channels * BytesPerSample);
        WriteLeUInt32(byteRate);
        ushort blockAlign = (ushort)(_channels * BytesPerSample);
        WriteLeUInt16(blockAlign);
        WriteLeUInt16(BitsPerSample);

        // data sub-chunk header
        WriteAscii("data");
        WriteLeUInt32(0); // placeholder for data chunk size
    }

    private void WriteAscii(string s)
    {
        Span<byte> buf = stackalloc byte[s.Length];
        Encoding.ASCII.GetBytes(s, buf);
        _output.Write(buf);
    }

    private void WriteLeUInt32(uint value)
    {
        Span<byte> buf = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(buf, value);
        _output.Write(buf);
    }

    private void WriteLeUInt16(ushort value)
    {
        Span<byte> buf = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16LittleEndian(buf, value);
        _output.Write(buf);
    }
}
