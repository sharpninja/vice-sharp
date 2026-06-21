namespace ViceSharp.Core.Media;

using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// FR-MED-004 (x64sc ffmpeg video parity): describes one muxed container the
/// external ffmpeg recorder can produce. Mirrors the format/codec choices of
/// VICE's ffmpegexedrv (avi/mp4/matroska with h264/mpeg4 video + aac/mp3 audio).
/// </summary>
/// <param name="Id">Stable id surfaced over the capture API and used in file extensions ("mp4", "mkv", "avi").</param>
/// <param name="Container">ffmpeg <c>-f</c> output muxer name ("mp4", "matroska", "avi").</param>
/// <param name="VideoCodec">ffmpeg output video encoder ("libx264", "mpeg4").</param>
/// <param name="AudioCodec">ffmpeg output audio encoder ("aac", "libmp3lame").</param>
/// <param name="OutputPixelFormat">Output pixel format for codec compatibility ("yuv420p"), or null to leave default.</param>
public sealed record FfmpegVideoFormat(
    string Id,
    string Container,
    string VideoCodec,
    string AudioCodec,
    string? OutputPixelFormat);

/// <summary>
/// The muxed video formats this host can drive through an external ffmpeg
/// process. Kept small and broadly compatible; extend as needed for parity.
/// </summary>
public static class FfmpegVideoFormats
{
    /// <summary>MPEG-4 / H.264 + AAC in an MP4 container (default, universally playable).</summary>
    public static readonly FfmpegVideoFormat Mp4 = new("mp4", "mp4", "libx264", "aac", "yuv420p");

    /// <summary>H.264 + AAC in a Matroska container.</summary>
    public static readonly FfmpegVideoFormat Mkv = new("mkv", "matroska", "libx264", "aac", "yuv420p");

    /// <summary>MPEG-4 Part 2 + MP3 in an AVI container (matches VICE's classic default).</summary>
    public static readonly FfmpegVideoFormat Avi = new("avi", "avi", "mpeg4", "libmp3lame", null);

    /// <summary>All supported muxed formats, in preference order.</summary>
    public static readonly IReadOnlyList<FfmpegVideoFormat> All = [Mp4, Mkv, Avi];

    /// <summary>Resolve a format by its id (case-insensitive). Returns false for unknown ids.</summary>
    public static bool TryGet(string? id, out FfmpegVideoFormat format)
    {
        if (!string.IsNullOrWhiteSpace(id))
        {
            foreach (var candidate in All)
            {
                if (string.Equals(candidate.Id, id, StringComparison.OrdinalIgnoreCase))
                {
                    format = candidate;
                    return true;
                }
            }
        }

        format = Mp4;
        return false;
    }
}

/// <summary>
/// Builds the ffmpeg argument vector that reads raw BGRA video and (optionally)
/// s16le PCM audio from two local TCP sockets and muxes them into the requested
/// container. The recorder is the TCP server (ffmpeg connects as client to
/// <c>tcp://127.0.0.1:port</c>), inverting VICE's default so there is no
/// connect-retry race: we bind the ports first, then launch ffmpeg pointing at
/// them. Pure and allocation-light so it can be unit-tested without spawning a
/// process.
/// </summary>
public static class FfmpegArgumentBuilder
{
    /// <summary>Default video bitrate (bits/s) when the caller does not override.</summary>
    public const int DefaultVideoBitrate = 2_000_000;

    /// <summary>Default audio bitrate (bits/s) when the caller does not override.</summary>
    public const int DefaultAudioBitrate = 192_000;

    /// <summary>
    /// Build the ffmpeg argv. The recorder feeds raw BGRA frames into the video
    /// socket and (when <paramref name="includeAudio"/>) s16le samples into the
    /// audio socket. <paramref name="frameRate"/> is the emulated refresh rate
    /// (e.g. 50.12 PAL); it is emitted with an invariant '.' decimal so ffmpeg
    /// parses it regardless of host locale.
    /// </summary>
    public static IReadOnlyList<string> Build(
        FfmpegVideoFormat format,
        int width,
        int height,
        double frameRate,
        int videoPort,
        bool includeAudio,
        int audioPort,
        int sampleRate,
        int channels,
        string outputPath,
        int videoBitrate = DefaultVideoBitrate,
        int audioBitrate = DefaultAudioBitrate)
    {
        ArgumentNullException.ThrowIfNull(format);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        if (frameRate <= 0) throw new ArgumentOutOfRangeException(nameof(frameRate));

        var fps = frameRate.ToString("0.######", CultureInfo.InvariantCulture);
        var args = new List<string>(32)
        {
            "-nostdin",
            "-hide_banner",
            "-loglevel", "error",

            // Input 0: raw BGRA video over TCP (we listen, ffmpeg connects).
            // -analyzeduration 0 / -probesize 32: the rawvideo format is fully
            // specified, so skip stream analysis - otherwise ffmpeg blocks reading
            // a frame from this input before it opens the audio input, deadlocking
            // the connect handshake.
            "-f", "rawvideo",
            "-pixel_format", "bgra",
            "-framerate", fps,
            "-r", fps,
            "-s", $"{width}x{height}",
            "-analyzeduration", "0",
            "-probesize", "32",
            "-thread_queue_size", "512",
            "-i", $"tcp://127.0.0.1:{videoPort}",
        };

        if (includeAudio)
        {
            // Input 1: raw signed-16 little-endian PCM over TCP.
            args.AddRange(new[]
            {
                "-f", "s16le",
                "-acodec", "pcm_s16le",
                "-ac", channels.ToString(CultureInfo.InvariantCulture),
                "-ar", sampleRate.ToString(CultureInfo.InvariantCulture),
                "-analyzeduration", "0",
                "-probesize", "32",
                "-thread_queue_size", "512",
                "-i", $"tcp://127.0.0.1:{audioPort}",
            });
        }

        // Output: overwrite, chosen container, finish with the shortest stream so
        // a clean Stop (both sockets closed) terminates muxing deterministically.
        args.AddRange(new[]
        {
            "-y",
            "-f", format.Container,
            "-shortest",
            "-r", fps,
            "-vcodec", format.VideoCodec,
            "-b:v", videoBitrate.ToString(CultureInfo.InvariantCulture),
        });

        if (format.OutputPixelFormat is not null)
        {
            args.Add("-pix_fmt");
            args.Add(format.OutputPixelFormat);
        }

        if (includeAudio)
        {
            args.AddRange(new[]
            {
                "-acodec", format.AudioCodec,
                "-b:a", audioBitrate.ToString(CultureInfo.InvariantCulture),
            });
        }

        args.Add(outputPath);
        return args;
    }
}
