namespace ViceSharp.TestHarness.C1541;

using FluentAssertions;
using ViceSharp.Chips.IEC;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: FR-1541 (RUNTIME-1541-002 D64 stream load).
/// Use case: Closes the read side of the streaming persistence loop by
/// pairing D64Image.LoadFromStream + D64DiskImageDevice.LoadFromStream
/// with the existing CommitToStream emitters. Together they let callers
/// move a D64 image through any Stream (FileStream, MemoryStream, network
/// stream) without round-tripping through the filesystem.
/// </summary>
public sealed class D64LoadFromStreamTests
{
    private const int Track = 1;
    private const int Sector = 0;
    private const int DiskSize = 174_848;

    /// <summary>
    /// FR/TR: FR-1541 (RUNTIME-1541-002 D64 stream load).
    /// Use case: LoadFromStream consumes the canonical 174,848 bytes from a
    /// MemoryStream containing a known pattern and exposes them via
    /// RawData.
    /// Acceptance: Image.RawData has Length = DiskSize and matches the
    /// source pattern byte-for-byte at probe offsets 0, mid, and end.
    /// </summary>
    [Fact]
    public void LoadFromStream_ReadsCanonical174848Bytes()
    {
        var buffer = new byte[DiskSize];
        for (int i = 0; i < buffer.Length; i++)
            buffer[i] = (byte)(i & 0xFF);
        using var ms = new MemoryStream(buffer);

        var image = D64Image.LoadFromStream(ms);

        image.RawData.Length.Should().Be(DiskSize);
        image.RawData[0].Should().Be((byte)0x00);
        image.RawData[1].Should().Be((byte)0x01);
        image.RawData[DiskSize / 2].Should().Be((byte)((DiskSize / 2) & 0xFF));
        image.RawData[DiskSize - 1].Should().Be((byte)((DiskSize - 1) & 0xFF));
    }

    /// <summary>
    /// FR/TR: FR-1541 (RUNTIME-1541-002 D64 stream load).
    /// Use case: A short stream (under 174,848 bytes) is not a valid D64
    /// image; LoadFromStream must reject it loudly so callers do not
    /// silently mount a truncated disk.
    /// Acceptance: ArgumentException is thrown (parameter name "source").
    /// Pick: ArgumentException over EndOfStreamException to align with the
    /// existing D64Image(byte[]) ctor which also throws ArgumentException
    /// on size mismatch.
    /// </summary>
    [Fact]
    public void LoadFromStream_RejectsTooShortStream()
    {
        using var ms = new MemoryStream(new byte[174_000]);

        var act = () => D64Image.LoadFromStream(ms);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("source");
    }

    /// <summary>
    /// FR/TR: FR-1541 (RUNTIME-1541-002 D64 stream load).
    /// Use case: CommitToStream + LoadFromStream form a lossless round-trip.
    /// Write a sector, commit, rewind, load into a new device, and verify
    /// the resulting image is byte-identical to the original.
    /// Acceptance: Image.RawData of the round-tripped device equals the
    /// source device byte-for-byte; the written sector survives intact.
    /// </summary>
    [Fact]
    public void LoadFromStream_RoundTripsCommitToStream_ByteExact()
    {
        var source = new D64DiskImageDevice(new D64Image());
        var pattern = new byte[256];
        for (int i = 0; i < 256; i++) pattern[i] = (byte)(i ^ 0xA5);
        source.WriteSector(Track, Sector, pattern);

        using var ms = new MemoryStream();
        source.CommitToStream(ms);
        ms.Position = 0;

        var restored = D64DiskImageDevice.LoadFromStream(ms);

        restored.Image.RawData.ToArray()
            .Should().Equal(source.Image.RawData.ToArray());

        var actualSector = new byte[256];
        restored.Image.GetSector(Track, Sector).CopyTo(actualSector);
        actualSector.Should().Equal(pattern);
    }

    /// <summary>
    /// FR/TR: FR-1541 (RUNTIME-1541-002 D64 stream load).
    /// Use case: The device-level LoadFromStream wrapper forwards the
    /// optional sourcePath label for diagnostics + emits a device whose
    /// Image holds the loaded bytes.
    /// Acceptance: SourcePath round-trips; Image.RawData first byte equals
    /// the source pattern.
    /// </summary>
    [Fact]
    public void DeviceLoadFromStream_PreservesSourcePathAndContent()
    {
        var buffer = new byte[DiskSize];
        buffer[0] = 0xC1;
        buffer[1] = 0x54;
        using var ms = new MemoryStream(buffer);

        var device = D64DiskImageDevice.LoadFromStream(ms, "test.d64");

        device.SourcePath.Should().Be("test.d64");
        device.Image.RawData.Length.Should().Be(DiskSize);
        device.Image.RawData[0].Should().Be((byte)0xC1);
        device.Image.RawData[1].Should().Be((byte)0x54);
    }

    /// <summary>
    /// FR/TR: FR-1541 (RUNTIME-1541-002 D64 stream load).
    /// Use case: An empty stream is an invalid D64 image; the loader must
    /// reject it rather than returning a zero-filled image.
    /// Acceptance: ArgumentException thrown for a 0-byte MemoryStream.
    /// </summary>
    [Fact]
    public void LoadFromStream_EmptyStreamRejected()
    {
        using var ms = new MemoryStream();

        var act = () => D64Image.LoadFromStream(ms);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("source");
    }
}
