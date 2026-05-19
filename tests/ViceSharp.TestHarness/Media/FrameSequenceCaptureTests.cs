namespace ViceSharp.TestHarness.Media;

using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using ViceSharp.Chips.Media;
using Xunit;

/// <summary>
/// FR/TR: FR-MED (RUNTIME-CAPTURE-002 continuous multi-frame BMP capture).
/// Use case: The host capture pipeline accepts a stream of BGRA video frames
/// from the emulator and persists each as a separately numbered BMP file
/// (frame_000001.bmp, frame_000002.bmp, ...) under a target directory so
/// post-run tooling can stitch them into video. This builds on
/// RUNTIME-CAPTURE-001 (single-frame BGRA -> BMP) but adds a long-running
/// session with monotonically increasing frame index, disposal semantics,
/// and per-frame artifact emission.
/// Acceptance: An empty session writes no BMP files. A three-frame session
/// emits frame_000001.bmp / frame_000002.bmp / frame_000003.bmp whose
/// headers report the supplied width/height and whose pixel content
/// reflects the per-frame BGRA bytes. Frames submitted after disposal are
/// silently ignored.
/// </summary>
public sealed class FrameSequenceCaptureTests : IDisposable
{
    private readonly string _tempDir;

    /// <summary>
    /// Creates a fresh per-test temporary directory for capture output.
    /// </summary>
    public FrameSequenceCaptureTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "vicesharp-fcap-test-" + Guid.NewGuid().ToString("N"));
    }

    /// <summary>Removes the per-test temporary directory and any captured artifacts.</summary>
    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    /// <summary>
    /// FR/TR: FR-MED (RUNTIME-CAPTURE-002).
    /// Use case: A capture session that is constructed and disposed without
    /// any frames being submitted must not litter the output directory.
    /// Acceptance: After Dispose(), no BMP files exist in the output dir.
    /// </summary>
    [Fact]
    public void NoFramesWritten_DirectoryHasNoBmpFiles()
    {
        using (var capture = new FrameSequenceCapture(_tempDir))
        {
            // No frames submitted.
        }

        var bmps = Directory.Exists(_tempDir)
            ? Directory.GetFiles(_tempDir, "*.bmp")
            : Array.Empty<string>();
        bmps.Should().BeEmpty("no frames were submitted");
    }

    /// <summary>
    /// FR/TR: FR-MED (RUNTIME-CAPTURE-002).
    /// Use case: A session submits three BGRA frames; each must produce
    /// a separately named BMP file under the target directory.
    /// Acceptance: Three files exist with the canonical numbered name
    /// frame_000001.bmp, frame_000002.bmp, frame_000003.bmp.
    /// </summary>
    [Fact]
    public void ThreeFramesWritten_ThreeNumberedFilesEmitted()
    {
        const int width = 320;
        const int height = 200;
        var frame = new byte[width * height * 4];
        // Fill with a known non-zero BGRA pattern to make sure pixel
        // data was actually serialized.
        for (int i = 0; i < frame.Length; i += 4)
        {
            frame[i] = 0x10;
            frame[i + 1] = 0x20;
            frame[i + 2] = 0x30;
            frame[i + 3] = 0xFF;
        }

        using (var capture = new FrameSequenceCapture(_tempDir))
        {
            capture.CaptureFrame(frame, width, height);
            capture.CaptureFrame(frame, width, height);
            capture.CaptureFrame(frame, width, height);
        }

        var bmps = Directory.GetFiles(_tempDir, "*.bmp").OrderBy(p => p).ToArray();
        bmps.Should().HaveCount(3);
        Path.GetFileName(bmps[0]).Should().Be("frame_000001.bmp");
        Path.GetFileName(bmps[1]).Should().Be("frame_000002.bmp");
        Path.GetFileName(bmps[2]).Should().Be("frame_000003.bmp");
    }

    /// <summary>
    /// FR/TR: FR-MED (RUNTIME-CAPTURE-002).
    /// Use case: Downstream tools (video stitching, screenshot review)
    /// must be able to read the BMP DIB header to recover the resolution
    /// of the captured frame.
    /// Acceptance: For a captured 320x200 BGRA frame, the DIB header at
    /// offsets 18..21 (width LE32) and 22..25 (height LE32) reports
    /// 320 and 200 respectively.
    /// </summary>
    [Fact]
    public void CapturedBmp_DibHeaderReportsCorrectDimensions()
    {
        const int width = 320;
        const int height = 200;
        var frame = new byte[width * height * 4];

        using (var capture = new FrameSequenceCapture(_tempDir))
        {
            capture.CaptureFrame(frame, width, height);
        }

        var path = Path.Combine(_tempDir, "frame_000001.bmp");
        File.Exists(path).Should().BeTrue();
        var bytes = File.ReadAllBytes(path);
        bytes.Length.Should().BeGreaterThan(54, "BMP header + pixel data");
        // "BM" magic
        bytes[0].Should().Be((byte)'B');
        bytes[1].Should().Be((byte)'M');
        // Width @ offset 18 LE32
        ReadLeInt32(bytes, 18).Should().Be(width);
        // Height @ offset 22 LE32
        ReadLeInt32(bytes, 22).Should().Be(height);
    }

    /// <summary>
    /// FR/TR: FR-MED (RUNTIME-CAPTURE-002).
    /// Use case: When the emulator emits visually different frames
    /// (e.g. animation, color change), the captured BMP sequence must
    /// preserve that variation, not collapse all frames to one image.
    /// Acceptance: Three frames with distinct solid colors (red, green,
    /// blue) produce three BMPs whose first pixel BGR bytes match the
    /// per-frame input (BMP stores 24-bit BGR bottom-up).
    /// </summary>
    [Fact]
    public void DistinctFrameContent_ProducesDistinctBmpPixels()
    {
        const int width = 2;
        const int height = 2;
        // BGRA solid-color frames.
        var red = MakeSolidFrame(width, height, b: 0x00, g: 0x00, r: 0xFF);
        var green = MakeSolidFrame(width, height, b: 0x00, g: 0xFF, r: 0x00);
        var blue = MakeSolidFrame(width, height, b: 0xFF, g: 0x00, r: 0x00);

        using (var capture = new FrameSequenceCapture(_tempDir))
        {
            capture.CaptureFrame(red, width, height);
            capture.CaptureFrame(green, width, height);
            capture.CaptureFrame(blue, width, height);
        }

        var f1 = File.ReadAllBytes(Path.Combine(_tempDir, "frame_000001.bmp"));
        var f2 = File.ReadAllBytes(Path.Combine(_tempDir, "frame_000002.bmp"));
        var f3 = File.ReadAllBytes(Path.Combine(_tempDir, "frame_000003.bmp"));

        // BMP pixel data starts at offset 54. 24-bit BGR rows are stored
        // bottom-up. For a 2x2 image with row stride = (2*3+3) & ~3 = 8,
        // the first BGR triplet at offset 54 corresponds to the
        // bottom-left pixel; for a uniformly solid frame the first
        // triplet should still match the solid color.
        // Red frame: B=0x00 G=0x00 R=0xFF
        f1[54].Should().Be(0x00, "blue channel of red frame");
        f1[55].Should().Be(0x00, "green channel of red frame");
        f1[56].Should().Be(0xFF, "red channel of red frame");

        // Green frame: B=0x00 G=0xFF R=0x00
        f2[54].Should().Be(0x00);
        f2[55].Should().Be(0xFF);
        f2[56].Should().Be(0x00);

        // Blue frame: B=0xFF G=0x00 R=0x00
        f3[54].Should().Be(0xFF);
        f3[55].Should().Be(0x00);
        f3[56].Should().Be(0x00);

        // And of course, the byte arrays as a whole differ.
        f1.Should().NotEqual(f2);
        f2.Should().NotEqual(f3);
        f1.Should().NotEqual(f3);
    }

    /// <summary>
    /// FR/TR: FR-MED (RUNTIME-CAPTURE-002).
    /// Use case: Once the host stops the capture session (Dispose), late
    /// frames coming from the emulator must NOT be written. The capture
    /// must silently ignore them.
    /// Acceptance: Submitting a frame after Dispose produces no new file
    /// (only the frames submitted before disposal are persisted).
    /// </summary>
    [Fact]
    public void DisposalStopsCapture_LateFramesIgnored()
    {
        const int width = 2;
        const int height = 2;
        var frame = new byte[width * height * 4];

        var capture = new FrameSequenceCapture(_tempDir);
        capture.CaptureFrame(frame, width, height);
        capture.Dispose();

        // Late frame: must be silently dropped.
        capture.CaptureFrame(frame, width, height);

        var bmps = Directory.GetFiles(_tempDir, "*.bmp");
        bmps.Should().HaveCount(1, "only the frame submitted before Dispose() should persist");
        Path.GetFileName(bmps[0]).Should().Be("frame_000001.bmp");
    }

    private static byte[] MakeSolidFrame(int width, int height, byte b, byte g, byte r)
    {
        var buf = new byte[width * height * 4];
        for (int i = 0; i < buf.Length; i += 4)
        {
            buf[i] = b;
            buf[i + 1] = g;
            buf[i + 2] = r;
            buf[i + 3] = 0xFF;
        }
        return buf;
    }

    private static int ReadLeInt32(byte[] data, int offset) =>
        data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) | (data[offset + 3] << 24);
}
