namespace ViceSharp.TestHarness.Media;

using System;
using System.IO;
using System.Linq;
using ViceSharp.Core.Media;
using Xunit;

/// <summary>
/// FR-MED-004 / TR-MEDIA-VIDEO-FFMPEG-001: muxed-video format table + ffmpeg
/// argument construction. Pure unit coverage that does not spawn a process, so
/// the recipe (raw BGRA video + s16le audio over two TCP inputs -> chosen
/// container) is pinned independently of whether ffmpeg is installed.
/// </summary>
public sealed class FfmpegEncodingTests
{
    [Fact]
    public void Formats_TryGet_ResolvesKnownAndRejectsUnknown()
    {
        Assert.True(FfmpegVideoFormats.TryGet("mp4", out var mp4));
        Assert.Equal("mp4", mp4.Container);
        Assert.Equal("libx264", mp4.VideoCodec);
        Assert.Equal("aac", mp4.AudioCodec);

        Assert.True(FfmpegVideoFormats.TryGet("MKV", out var mkv)); // case-insensitive
        Assert.Equal("matroska", mkv.Container);

        Assert.True(FfmpegVideoFormats.TryGet("avi", out var avi));
        Assert.Equal("mpeg4", avi.VideoCodec);
        Assert.Equal("libmp3lame", avi.AudioCodec);

        Assert.False(FfmpegVideoFormats.TryGet("gif", out _));
        Assert.False(FfmpegVideoFormats.TryGet("", out _));
        Assert.False(FfmpegVideoFormats.TryGet(null, out _));
    }

    [Fact]
    public void Build_Mp4_WithAudio_EmitsRawBgraVideoAndS16leAudioTcpInputsAndCodecs()
    {
        var args = FfmpegArgumentBuilder.Build(
            FfmpegVideoFormats.Mp4,
            width: 384, height: 272, frameRate: 50.125,
            videoPort: 5001, includeAudio: true, audioPort: 5002,
            sampleRate: 44100, channels: 1,
            outputPath: @"C:\tmp\out.mp4");

        var joined = string.Join(' ', args);

        // Video input: raw BGRA at the exact size + invariant-decimal framerate.
        Assert.Contains("-f", args);
        Assert.Contains("rawvideo", args);
        Assert.Contains("bgra", args);
        Assert.Contains("384x272", args);
        Assert.Contains("50.125", args);
        Assert.Contains("tcp://127.0.0.1:5001", args);

        // Wall-clock input timestamps so a dropped frame leaves a real time gap (the CFR output
        // duplicate-fills it) instead of compressing the timeline and playing fast. Must be an
        // input option (before -i).
        Assert.Contains("-use_wallclock_as_timestamps", args);
        Assert.True(joined.IndexOf("-use_wallclock_as_timestamps", StringComparison.Ordinal)
                  < joined.IndexOf("tcp://127.0.0.1:5001", StringComparison.Ordinal),
            "wall-clock stamping must precede the video input");

        // Audio input: s16le mono 44100 over the second socket.
        Assert.Contains("s16le", args);
        Assert.Contains("44100", args);
        Assert.Contains("tcp://127.0.0.1:5002", args);

        // Output: mp4 container, h264 + aac, yuv420p, overwrite + shortest, then path last.
        Assert.Contains("mp4", args);
        Assert.Contains("libx264", args);
        Assert.Contains("aac", args);
        Assert.Contains("yuv420p", args);
        Assert.Contains("-y", args);
        Assert.Contains("-shortest", args);
        Assert.Equal(@"C:\tmp\out.mp4", args[^1]);

        // The video input must be declared before the output path.
        Assert.True(joined.IndexOf("tcp://127.0.0.1:5001", StringComparison.Ordinal)
                  < joined.LastIndexOf("out.mp4", StringComparison.Ordinal));
    }

    [Fact]
    public void Build_Avi_UsesMpeg4AndMp3()
    {
        var args = FfmpegArgumentBuilder.Build(
            FfmpegVideoFormats.Avi,
            width: 320, height: 200, frameRate: 50,
            videoPort: 6001, includeAudio: true, audioPort: 6002,
            sampleRate: 44100, channels: 1,
            outputPath: "out.avi");

        Assert.Contains("avi", args);
        Assert.Contains("mpeg4", args);
        Assert.Contains("libmp3lame", args);
        // AVI/mpeg4 path declares no forced output pixel format.
        Assert.DoesNotContain("yuv420p", args);
    }

    [Fact]
    public void Build_NoAudio_OmitsAudioInputAndAudioCodec()
    {
        var args = FfmpegArgumentBuilder.Build(
            FfmpegVideoFormats.Mp4,
            width: 384, height: 272, frameRate: 50,
            videoPort: 7001, includeAudio: false, audioPort: 0,
            sampleRate: 44100, channels: 1,
            outputPath: "out.mp4");

        Assert.DoesNotContain("s16le", args);
        Assert.DoesNotContain("pcm_s16le", args);
        Assert.DoesNotContain("-b:a", args);
        // Still a valid video-only command.
        Assert.Contains("rawvideo", args);
        Assert.Contains("libx264", args);
        Assert.Equal("out.mp4", args[^1]);
    }

    [Fact]
    public void Locate_HonoursOverrideEnvironmentVariable()
    {
        var fake = Path.Combine(Path.GetTempPath(), $"vice-fake-ffmpeg-{Guid.NewGuid():N}.exe");
        File.WriteAllText(fake, "stub");
        var previous = Environment.GetEnvironmentVariable(FfmpegLocator.OverrideEnvironmentVariable);
        try
        {
            Environment.SetEnvironmentVariable(FfmpegLocator.OverrideEnvironmentVariable, fake);
            Assert.Equal(fake, FfmpegLocator.Locate());
            Assert.True(FfmpegLocator.IsAvailable);
        }
        finally
        {
            Environment.SetEnvironmentVariable(FfmpegLocator.OverrideEnvironmentVariable, previous);
            File.Delete(fake);
        }
    }

    [Fact]
    public void Locate_NonexistentOverride_FallsThroughToPathSearch()
    {
        var missing = Path.Combine(Path.GetTempPath(), $"vice-missing-{Guid.NewGuid():N}.exe");
        var previous = Environment.GetEnvironmentVariable(FfmpegLocator.OverrideEnvironmentVariable);
        try
        {
            Environment.SetEnvironmentVariable(FfmpegLocator.OverrideEnvironmentVariable, missing);
            // Must not return the missing override path; result depends on PATH only.
            Assert.NotEqual(missing, FfmpegLocator.Locate());
        }
        finally
        {
            Environment.SetEnvironmentVariable(FfmpegLocator.OverrideEnvironmentVariable, previous);
        }
    }
}
