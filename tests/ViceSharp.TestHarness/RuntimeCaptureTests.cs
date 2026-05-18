namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Core.Capture;
using Xunit;

public sealed class RuntimeCaptureTests
{
    /// <summary>
    /// FR: FR-MED-001, TR: TR-MEDIA-001.
    /// Use case: Host code presents a BGRA frame to the recording frame
    /// sink and then asks the sink to materialise that frame as a BMP file
    /// for screenshot capture.
    /// Acceptance: The on-disk file begins with the "BM" magic, the sink
    /// reports FrameCount == 1, and LastFrame round-trips the originally
    /// presented bytes.
    /// </summary>
    [Fact]
    public async Task RecordingFrameSink_WritesBmpArtifact_FromPresentedBgraFrame()
    {
        var sink = new RecordingFrameSink();
        byte[] frame =
        [
            0x01, 0x02, 0x03, 0xFF,
            0x04, 0x05, 0x06, 0xFF,
            0x07, 0x08, 0x09, 0xFF,
            0x0A, 0x0B, 0x0C, 0xFF
        ];
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.bmp");

        try
        {
            sink.PresentFrame(frame);
            await sink.SaveLastFrameAsync(2, 2, path, TestContext.Current.CancellationToken);

            var bytes = await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken);
            Assert.Equal((byte)'B', bytes[0]);
            Assert.Equal((byte)'M', bytes[1]);
            Assert.Equal(1, sink.FrameCount);
            Assert.Equal(frame, sink.LastFrame.ToArray());
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    /// <summary>
    /// FR: FR-MED-001, TR: TR-MEDIA-001.
    /// Use case: Host capture pipeline reads the active video chip's frame
    /// buffer directly (without an external sink) and persists it as a BMP
    /// artifact suitable for screenshot delivery.
    /// Acceptance: <see cref="FrameCapture.CaptureAsync"/> writes a file
    /// beginning with the "BM" magic and contains more than the 54-byte
    /// BMP header (i.e. pixel data was appended).
    /// </summary>
    [Fact]
    public async Task FrameCapture_WritesBmpArtifact_FromVideoChipBuffer()
    {
        var videoChip = new TestVideoChip();
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.bmp");

        try
        {
            await FrameCapture.CaptureAsync(videoChip, path, TestContext.Current.CancellationToken);

            var bytes = await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken);
            Assert.Equal((byte)'B', bytes[0]);
            Assert.Equal((byte)'M', bytes[1]);
            Assert.True(bytes.Length > 54);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    private sealed class TestVideoChip : IVideoChip
    {
        public DeviceId Id => new(0xC001);
        public string Name => "Test Video";
        public uint ClockDivisor => 1;
        public ClockPhase Phase => ClockPhase.Phi1;
        public ushort CurrentRasterLine => 0;
        public int CyclesPerLine => 1;
        public int VisibleLines => 2;
        public int TotalLines => 2;
        public bool IsVBlank => false;
        public byte[] FrameBuffer { get; } =
        [
            0x10, 0x20, 0x30, 0xFF,
            0x40, 0x50, 0x60, 0xFF,
            0x70, 0x80, 0x90, 0xFF,
            0xA0, 0xB0, 0xC0, 0xFF
        ];
        public int FrameWidth => 2;
        public int FrameHeight => 2;
        public event EventHandler? FrameCompleted;
        public void Tick() => FrameCompleted?.Invoke(this, EventArgs.Empty);
        public void Reset() { }
    }
}
