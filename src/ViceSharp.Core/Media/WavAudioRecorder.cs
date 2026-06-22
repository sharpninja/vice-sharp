namespace ViceSharp.Core.Media;

using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

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
    // The actual file writes run on a background thread (off the emulation worker);
    // the worker only converts to LE16 in a reused scratch and enqueues a copy. The
    // single writer thread is the sole writer of the data region, and Stop joins it
    // before patching the header, so there is no concurrent _output access.
    private readonly BackgroundByteWriter _writer;
    private byte[] _scratch = [];
    private uint _dataBytesWritten;     // written only by the background writer thread
    private volatile bool _stopped;
    private int _stopGuard;             // Interlocked latch for idempotent Stop
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

        // Header is written synchronously above; the data region is appended by the
        // background writer, which also tracks the byte count.
        _writer = new BackgroundByteWriter(
            (b, n) => { _output.Write(b, 0, n); _dataBytesWritten += (uint)n; },
            capacity: 64,
            "vice-wav-writer");
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
        // Writes after Stop are silently ignored (per IAudioRecorder), so a late
        // worker batch racing finalisation can never corrupt the file.
        if (samples.IsEmpty || _stopped) return;

        // Convert to LE16 in the reused scratch (single producer = the worker), then
        // enqueue a pooled copy for the background writer - no per-call allocation.
        int byteLen = samples.Length * BytesPerSample;
        if (_scratch.Length < byteLen)
            _scratch = new byte[byteLen];

        var dst = _scratch.AsSpan(0, byteLen);
        if (BitConverter.IsLittleEndian)
        {
            MemoryMarshal.AsBytes(samples).CopyTo(dst);
        }
        else
        {
            for (var i = 0; i < samples.Length; i++)
                BinaryPrimitives.WriteInt16LittleEndian(dst.Slice(i * 2, 2), samples[i]);
        }

        _writer.Enqueue(dst);
    }

    /// <summary>
    /// Finalizes the WAV file by rewinding to the header and patching the
    /// RIFF + data chunk sizes. Idempotent; subsequent calls are no-ops.
    /// </summary>
    public void Stop()
    {
        // Idempotent: only the first caller finalises.
        if (Interlocked.Exchange(ref _stopGuard, 1) != 0) return;
        _stopped = true;

        // Flush every queued batch and join the writer, so the data region is
        // complete and this thread is now the sole writer of _output.
        _writer.CompleteAndJoin(TimeSpan.FromSeconds(10));

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

        _writer.Dispose();
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
