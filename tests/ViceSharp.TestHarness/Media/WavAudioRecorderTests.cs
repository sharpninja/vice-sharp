namespace ViceSharp.TestHarness.Media;

using System.IO;
using System.Text;
using FluentAssertions;
using ViceSharp.Core.Media;
using Xunit;

/// <summary>
/// FR/TR: FR-MED-003 (BACKFILL-MEDIA WAV audio recording).
/// Use case: The emulator records audio output to a WAV file. The recorder
/// writes uncompressed 16-bit PCM (RIFF/WAVE) with a configurable sample
/// rate (44100/48000/96000 Hz) and mono or stereo channel count. The host
/// can tee samples from the SID through the recorder via the IAudioBackend
/// pipeline.
/// Acceptance: Output bytes are a parseable RIFF/WAVE 16-bit PCM file with
/// correct header fields (LE encoding, fmt chunk size 16, sample rate, etc.)
/// and a data chunk whose byte length matches the submitted sample count.
/// </summary>
public sealed class WavAudioRecorderTests
{
    /// <summary>
    /// FR/TR: FR-MED-003
    /// Use case: A recorder started + immediately stopped with no samples
    /// still produces a valid (empty-data-chunk) WAV file.
    /// Acceptance: Output stream is exactly 44 bytes (canonical RIFF/WAVE
    /// header). RIFF size = 36, fmt size = 16, data size = 0.
    /// </summary>
    [Fact]
    public void EmptyRecorder_WritesValidEmptyWav()
    {
        using var ms = new MemoryStream();
        using (var recorder = new WavAudioRecorder(ms, sampleRate: 44100, channels: 1))
        {
            // No samples submitted.
        }

        var bytes = ms.ToArray();
        bytes.Length.Should().Be(44, "canonical WAV header is 44 bytes for empty payload");

        ReadAscii(bytes, 0, 4).Should().Be("RIFF");
        ReadLeUInt32(bytes, 4).Should().Be(36u, "RIFF chunk size = 36 + dataSize(0)");
        ReadAscii(bytes, 8, 4).Should().Be("WAVE");

        ReadAscii(bytes, 12, 4).Should().Be("fmt ");
        ReadLeUInt32(bytes, 16).Should().Be(16u, "PCM fmt chunk size is 16");

        ReadAscii(bytes, 36, 4).Should().Be("data");
        ReadLeUInt32(bytes, 40).Should().Be(0u, "no samples written");
    }

    /// <summary>
    /// FR/TR: FR-MED-003
    /// Use case: A typical recording pass writes a sample buffer and stops.
    /// Acceptance: For 100 mono samples at 16-bit, payload = 200 bytes;
    /// data chunk size = 200; RIFF size = 200 + 36 = 236.
    /// </summary>
    [Fact]
    public void SingleSampleBuffer_WritesCorrectWav()
    {
        using var ms = new MemoryStream();
        var samples = new short[100];
        for (int i = 0; i < samples.Length; i++) samples[i] = (short)(i * 100);

        using (var recorder = new WavAudioRecorder(ms, sampleRate: 44100, channels: 1))
        {
            recorder.WriteSamples(samples);
        }

        var bytes = ms.ToArray();
        bytes.Length.Should().Be(44 + 200);
        ReadLeUInt32(bytes, 4).Should().Be(236u);
        ReadLeUInt32(bytes, 40).Should().Be(200u, "100 samples * 2 bytes");

        // Spot check: first sample LE encoding for value 0
        ReadLeInt16(bytes, 44).Should().Be(0);
        // Second sample = 100
        ReadLeInt16(bytes, 46).Should().Be(100);
    }

    /// <summary>
    /// FR/TR: FR-MED-003
    /// Use case: Sample rate is configurable (44100/48000/96000) and must
    /// be reflected in the fmt chunk.
    /// Acceptance: fmt sample-rate field = 48000 when constructed at 48 kHz.
    /// byte-rate = sample_rate * channels * 2.
    /// </summary>
    [Fact]
    public void SampleRate_RespectedInFmtChunk()
    {
        using var ms = new MemoryStream();
        using (var recorder = new WavAudioRecorder(ms, sampleRate: 48000, channels: 1))
        {
            recorder.WriteSamples(new short[] { 1, 2, 3, 4 });
        }

        var bytes = ms.ToArray();
        ReadLeUInt16(bytes, 22).Should().Be((ushort)1, "mono");
        ReadLeUInt32(bytes, 24).Should().Be(48000u, "sample rate");
        ReadLeUInt32(bytes, 28).Should().Be(48000u * 1u * 2u, "byte rate = SR * channels * bytes/sample");
        ReadLeUInt16(bytes, 32).Should().Be((ushort)2, "block align = channels * bytes/sample");
        ReadLeUInt16(bytes, 34).Should().Be((ushort)16, "bits per sample");
    }

    /// <summary>
    /// FR/TR: FR-MED-003
    /// Use case: Stereo recording: caller submits interleaved L/R short pairs.
    /// Acceptance: fmt channels = 2; data payload = sample_count * channels * 2.
    /// For 50 stereo frames (100 short values) payload = 200 bytes.
    /// </summary>
    [Fact]
    public void StereoRecording_TwoChannelsInFmtAndInterleavedData()
    {
        using var ms = new MemoryStream();
        var interleaved = new short[100]; // 50 frames * 2 channels
        for (int i = 0; i < interleaved.Length; i++) interleaved[i] = (short)(i + 1);

        using (var recorder = new WavAudioRecorder(ms, sampleRate: 44100, channels: 2))
        {
            recorder.WriteSamples(interleaved);
        }

        var bytes = ms.ToArray();
        ReadLeUInt16(bytes, 22).Should().Be((ushort)2, "stereo");
        ReadLeUInt32(bytes, 28).Should().Be(44100u * 2u * 2u, "stereo byte rate");
        ReadLeUInt16(bytes, 32).Should().Be((ushort)4, "stereo block align = 2 * 2");
        ReadLeUInt32(bytes, 40).Should().Be(200u, "100 shorts * 2 bytes");

        // Confirm interleave preserved: bytes 44..45 should be sample value 1
        ReadLeInt16(bytes, 44).Should().Be(1);
        ReadLeInt16(bytes, 46).Should().Be(2);
    }

    /// <summary>
    /// FR/TR: FR-MED-003
    /// Use case: WAV format mandates little-endian multi-byte fields. Confirm
    /// header chunk sizes are LE so the file is parseable by any compliant
    /// reader regardless of host endianness.
    /// Acceptance: For a 4-sample mono recording, RIFF size = 44, fmt size =
    /// 16, data size = 8, each laid out byte-by-byte in LE order.
    /// </summary>
    [Fact]
    public void HeaderByteOrder_IsLittleEndian()
    {
        using var ms = new MemoryStream();
        using (var recorder = new WavAudioRecorder(ms, sampleRate: 96000, channels: 1))
        {
            recorder.WriteSamples(new short[] { 0, 0, 0, 0 });
        }

        var bytes = ms.ToArray();

        // RIFF chunk size at offset 4-7: 36 + dataSize(8) = 44 -> 0x2C 0x00 0x00 0x00
        bytes[4].Should().Be(0x2C);
        bytes[5].Should().Be(0x00);
        bytes[6].Should().Be(0x00);
        bytes[7].Should().Be(0x00);

        // fmt chunk size at offset 16-19: 16 -> 0x10 0x00 0x00 0x00
        bytes[16].Should().Be(0x10);
        bytes[17].Should().Be(0x00);
        bytes[18].Should().Be(0x00);
        bytes[19].Should().Be(0x00);

        // data chunk size at offset 40-43: 8 -> 0x08 0x00 0x00 0x00
        bytes[40].Should().Be(0x08);
        bytes[41].Should().Be(0x00);
        bytes[42].Should().Be(0x00);
        bytes[43].Should().Be(0x00);

        // 96000 = 0x00017700 -> LE: 0x00, 0x77, 0x01, 0x00 at offset 24
        bytes[24].Should().Be(0x00);
        bytes[25].Should().Be(0x77);
        bytes[26].Should().Be(0x01);
        bytes[27].Should().Be(0x00);
    }

    private static string ReadAscii(byte[] data, int offset, int length) =>
        Encoding.ASCII.GetString(data, offset, length);

    private static uint ReadLeUInt32(byte[] data, int offset) =>
        (uint)(data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) | (data[offset + 3] << 24));

    private static ushort ReadLeUInt16(byte[] data, int offset) =>
        (ushort)(data[offset] | (data[offset + 1] << 8));

    private static short ReadLeInt16(byte[] data, int offset) =>
        (short)(data[offset] | (data[offset + 1] << 8));
}
