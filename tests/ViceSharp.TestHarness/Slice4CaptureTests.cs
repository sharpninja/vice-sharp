namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Core.Capture;
using Xunit;

/// <summary>
/// Slice 4C: PNG and JPEG capture format tests.
/// FR-MED-001, RUNTIME-CAPTURE-002, TR-MEDIA-001.
/// </summary>
public sealed class Slice4CaptureTests
{
    /// <summary>
    /// FR-MED-001, RUNTIME-CAPTURE-002, TR-MEDIA-001.
    /// Use case: Host code requests a PNG screenshot from the video chip's
    /// frame buffer. The file must begin with the canonical PNG magic bytes.
    /// Acceptance: CaptureAsync with format="PNG" produces a file whose
    /// first two bytes are 0x89 and 0x50 (PNG signature start) and the
    /// total length is greater than 8 bytes.
    /// </summary>
    [Fact]
    public async Task FrameCapture_WritesPngArtifact_FromVideoChipBuffer()
    {
        var videoChip = new TestVideoChip();
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.png");

        try
        {
            await FrameCapture.CaptureAsync(videoChip, path, "PNG", TestContext.Current.CancellationToken);

            var bytes = await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken);
            Assert.Equal(0x89, bytes[0]);
            Assert.Equal(0x50, bytes[1]); // 'P' - second byte of PNG signature
            Assert.True(bytes.Length > 8);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    /// <summary>
    /// FR-MED-001, RUNTIME-CAPTURE-002, TR-MEDIA-001.
    /// Use case: Host code requests a JPEG screenshot from the video chip's
    /// frame buffer. The file must begin with the JPEG SOI marker.
    /// Acceptance: CaptureAsync with format="JPEG" produces a file whose
    /// first two bytes are 0xFF and 0xD8 (JPEG SOI marker).
    /// </summary>
    [Fact]
    public async Task FrameCapture_WritesJpegArtifact_FromVideoChipBuffer()
    {
        var videoChip = new TestVideoChip();
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.jpg");

        try
        {
            await FrameCapture.CaptureAsync(videoChip, path, "JPEG", TestContext.Current.CancellationToken);

            var bytes = await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken);
            Assert.Equal(0xFF, bytes[0]);
            Assert.Equal(0xD8, bytes[1]); // JPEG SOI marker second byte
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    private sealed class TestVideoChip : IVideoChip
    {
        public DeviceId Id => new(0xC002);
        public string Name => "Test Video Slice4";
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
