using System.Diagnostics;
using System.Globalization;

namespace ViceSharp.Core.Media;

/// <summary>
/// FR-MED-005. Records PCM through ffmpeg as FLAC or MP3 instead of RIFF/WAVE.
/// Samples are buffered as little-endian s16le and encoded when <see cref="Stop"/> runs.
/// </summary>
public sealed class FfmpegAudioRecorder : ViceSharp.Abstractions.IAudioRecorder
{
    private readonly string _targetPath;
    private readonly string _format;
    private readonly string _ffmpegPath;
    private readonly int _sampleRate;
    private readonly int _channels;
    private readonly string _audioBitrate;
    private readonly object _gate = new();
    private readonly List<short> _samples = new();
    private int _stopGuard;
    private bool _disposed;

    /// <summary>
    /// Creates a recorder that writes <paramref name="format"/> to <paramref name="targetPath"/>.
    /// </summary>
    /// <param name="targetPath">Destination file. ffmpeg overwrites it on stop.</param>
    /// <param name="format"><c>flac</c> or <c>mp3</c>.</param>
    /// <param name="sampleRate">PCM sample rate in Hz.</param>
    /// <param name="channels">1 for mono, 2 for interleaved stereo.</param>
    /// <param name="ffmpegPath">Existing ffmpeg executable.</param>
    /// <param name="audioBitrate">MP3 bitrate such as <c>32k</c> or <c>320k</c>. Empty uses 128k.</param>
    public FfmpegAudioRecorder(
        string targetPath,
        string format,
        int sampleRate,
        int channels,
        string ffmpegPath,
        string? audioBitrate = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(format);
        ArgumentException.ThrowIfNullOrWhiteSpace(ffmpegPath);
        if (sampleRate <= 0)
            throw new ArgumentOutOfRangeException(nameof(sampleRate));
        if (channels < 1 || channels > 2)
            throw new ArgumentOutOfRangeException(nameof(channels));

        _audioBitrate = NormalizeBitrate(audioBitrate);
        _targetPath = targetPath;
        _format = format;
        _sampleRate = sampleRate;
        _channels = channels;
        _ffmpegPath = ffmpegPath;
    }

    private static string NormalizeBitrate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "128k";

        var text = value.Trim();
        if (text.EndsWith('k') || text.EndsWith('K'))
            return text;
        if (int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var bits) && bits > 0)
            return bits >= 1000
                ? (bits / 1000).ToString(CultureInfo.InvariantCulture) + "k"
                : bits.ToString(CultureInfo.InvariantCulture) + "k";

        return text;
    }

    /// <inheritdoc />
    public int SampleRate => _sampleRate;

    /// <inheritdoc />
    public int Channels => _channels;

    /// <inheritdoc />
    public void WriteSamples(ReadOnlySpan<short> samples)
    {
        if (_stopGuard != 0 || samples.IsEmpty)
            return;

        lock (_gate)
        {
            if (_stopGuard != 0)
                return;

            for (var i = 0; i < samples.Length; i++)
                _samples.Add(samples[i]);
        }
    }

    /// <inheritdoc />
    public void Stop()
    {
        if (Interlocked.Exchange(ref _stopGuard, 1) != 0)
            return;

        short[] samples;
        lock (_gate)
            samples = _samples.ToArray();

        var pcmPath = Path.Combine(Path.GetTempPath(), "vicesharp-pcm-" + Guid.NewGuid().ToString("N") + ".s16le");
        try
        {
            var bytes = new byte[samples.Length * 2];
            for (var i = 0; i < samples.Length; i++)
                System.Buffers.Binary.BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(i * 2, 2), samples[i]);
            File.WriteAllBytes(pcmPath, bytes);

            var codec = _format.Equals("mp3", StringComparison.OrdinalIgnoreCase) ? "libmp3lame" : "flac";
            var start = new ProcessStartInfo(_ffmpegPath)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true
            };
            start.ArgumentList.Add("-hide_banner");
            start.ArgumentList.Add("-loglevel");
            start.ArgumentList.Add("error");
            start.ArgumentList.Add("-y");
            start.ArgumentList.Add("-f");
            start.ArgumentList.Add("s16le");
            start.ArgumentList.Add("-ar");
            start.ArgumentList.Add(_sampleRate.ToString(CultureInfo.InvariantCulture));
            start.ArgumentList.Add("-ac");
            start.ArgumentList.Add(_channels.ToString(CultureInfo.InvariantCulture));
            start.ArgumentList.Add("-i");
            start.ArgumentList.Add(pcmPath);
            start.ArgumentList.Add("-c:a");
            start.ArgumentList.Add(codec);
            if (codec == "libmp3lame")
            {
                start.ArgumentList.Add("-b:a");
                start.ArgumentList.Add(_audioBitrate);
            }

            // The requested format selects the container. A .wav target
            // must not make ffmpeg mux WAV around a flac or mp3 codec.
            start.ArgumentList.Add("-f");
            start.ArgumentList.Add(codec == "libmp3lame" ? "mp3" : "flac");
            start.ArgumentList.Add(_targetPath);
            using var process = Process.Start(start) ?? throw new InvalidOperationException("ffmpeg did not start.");
            var stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode != 0)
                throw new InvalidOperationException("ffmpeg audio encode failed. " + stderr);
        }
        finally
        {
            if (File.Exists(pcmPath))
                File.Delete(pcmPath);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        Stop();
    }
}
