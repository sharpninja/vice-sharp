namespace ViceSharp.TestHarness.Media;

using System;
using System.Diagnostics;
using System.IO;
using ViceSharp.Core.Media;
using Xunit;

/// <summary>
/// FR-MED-004 / TR-MEDIA-VIDEO-FFMPEG-001: end-to-end proof that the external
/// ffmpeg recorder produces a real, muxed container holding BOTH a video and an
/// audio stream (x64sc ffmpegexedrv parity). Gated on ffmpeg being installed;
/// skipped otherwise so the suite stays green on machines without it.
/// </summary>
public sealed class FfmpegVideoRecorderTests
{
    [Fact]
    public void Record_Mp4_ProducesFileWithVideoAndAudioStreams()
    {
        var ffmpeg = FfmpegLocator.Locate();
        if (ffmpeg is null)
        {
            Assert.Skip("ffmpeg not installed - skipping muxed-recording integration test.");
            return;
        }

        const int width = 64;
        const int height = 64;
        const double fps = 50.0;
        const int sampleRate = 44100;
        const int frames = 20;
        var samplesPerFrame = (int)(sampleRate / fps); // 882

        var outPath = Path.Combine(Path.GetTempPath(), $"vice-ffmpeg-{Guid.NewGuid():N}.mp4");
        var recorder = new FfmpegVideoRecorder(
            ffmpeg, FfmpegVideoFormats.Mp4, width, height, fps, outPath, includeAudio: true, sampleRate, channels: 1);

        try
        {
            recorder.Start();

            var frame = new byte[width * height * 4];
            var audio = new short[samplesPerFrame];
            for (var f = 0; f < frames; f++)
            {
                // Vary pixels + a simple tone so the encoders have real content.
                for (var i = 0; i < frame.Length; i++)
                    frame[i] = (byte)((i + f * 7) & 0xFF);
                recorder.CaptureFrame(frame, width, height);

                for (var s = 0; s < audio.Length; s++)
                    audio[s] = (short)(8000 * Math.Sin((s + f * audio.Length) * 0.05));
                recorder.WriteSamples(audio);
            }

            recorder.Stop();

            Assert.True(File.Exists(outPath), $"ffmpeg produced no output. stderr: {recorder.StandardError}");
            var size = new FileInfo(outPath).Length;
            Assert.True(size > 1024, $"Output file is suspiciously small ({size} bytes). stderr: {recorder.StandardError}");
            Assert.Equal(frames, recorder.FrameCount);

            // When ffprobe is available, assert the container really holds both streams.
            var streams = ProbeStreamTypes(ffmpeg, outPath);
            if (streams is not null)
            {
                Assert.Contains("video", streams);
                Assert.Contains("audio", streams);
            }
        }
        finally
        {
            recorder.Dispose();
            if (File.Exists(outPath)) File.Delete(outPath);
        }
    }

    [Fact]
    public void Record_VideoOnly_ProducesFileWithoutAudio()
    {
        var ffmpeg = FfmpegLocator.Locate();
        if (ffmpeg is null)
        {
            Assert.Skip("ffmpeg not installed - skipping video-only integration test.");
            return;
        }

        const int width = 48, height = 48;
        var outPath = Path.Combine(Path.GetTempPath(), $"vice-ffmpeg-vo-{Guid.NewGuid():N}.mkv");
        var recorder = new FfmpegVideoRecorder(
            ffmpeg, FfmpegVideoFormats.Mkv, width, height, frameRate: 50.0, outPath, includeAudio: false);

        try
        {
            recorder.Start();
            var frame = new byte[width * height * 4];
            for (var f = 0; f < 10; f++)
            {
                Array.Fill(frame, (byte)(f * 20));
                recorder.CaptureFrame(frame, width, height);
            }
            recorder.Stop();

            Assert.True(File.Exists(outPath), $"no output. stderr: {recorder.StandardError}");
            Assert.True(new FileInfo(outPath).Length > 512);

            var streams = ProbeStreamTypes(ffmpeg, outPath);
            if (streams is not null)
            {
                Assert.Contains("video", streams);
                Assert.DoesNotContain("audio", streams);
            }
        }
        finally
        {
            recorder.Dispose();
            if (File.Exists(outPath)) File.Delete(outPath);
        }
    }

    // Returns the newline-joined codec_type list (e.g. "video\naudio") via ffprobe,
    // or null when ffprobe is not alongside ffmpeg.
    private static string? ProbeStreamTypes(string ffmpegPath, string mediaPath)
    {
        var dir = Path.GetDirectoryName(ffmpegPath)!;
        var probe = Path.Combine(dir, OperatingSystem.IsWindows() ? "ffprobe.exe" : "ffprobe");
        if (!File.Exists(probe))
            return null;

        var psi = new ProcessStartInfo
        {
            FileName = probe,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        foreach (var a in new[]
                 {
                     "-v", "error",
                     "-show_entries", "stream=codec_type",
                     "-of", "csv=p=0",
                     mediaPath,
                 })
        {
            psi.ArgumentList.Add(a);
        }

        using var p = Process.Start(psi)!;
        var output = p.StandardOutput.ReadToEnd();
        p.WaitForExit(10_000);
        return output;
    }
}
