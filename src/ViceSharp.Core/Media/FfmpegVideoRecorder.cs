namespace ViceSharp.Core.Media;

using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ViceSharp.Abstractions;

/// <summary>
/// FR-MED-004 (x64sc ffmpeg video parity): record the emulator's video AND audio
/// into a single muxed container (mp4/mkv/avi) by streaming raw frames + PCM to
/// an external <c>ffmpeg</c> process, mirroring VICE's ffmpegexedrv. The recorder
/// opens two loopback TCP servers (video + optional audio), launches ffmpeg as a
/// client of both, then forwards each committed BGRA frame
/// (<see cref="IVideoCaptureSink.CaptureFrame"/>) and each int16 PCM batch
/// (<see cref="IAudioRecorder.WriteSamples"/>, fed by <see cref="CaptureAudioTap"/>)
/// down the respective socket. <see cref="Stop"/> closes both sockets so ffmpeg
/// finalises the file (-shortest) and the process is awaited.
///
/// Both feeds arrive on the single emulation worker thread; <see cref="Stop"/>
/// runs on an RPC thread, so socket access is guarded by <c>_sync</c> and writes
/// after stop are dropped.
/// </summary>
public sealed class FfmpegVideoRecorder : IVideoCaptureSink, IAudioRecorder
{
    private readonly string _ffmpegPath;
    private readonly FfmpegVideoFormat _format;
    private readonly int _width;
    private readonly int _height;
    private readonly double _frameRate;
    private readonly bool _includeAudio;
    private readonly int _sampleRate;
    private readonly int _channels;
    private readonly string _outputPath;
    private readonly int _expectedFrameBytes;
    private readonly object _sync = new();

    private TcpListener? _videoListener;
    private TcpListener? _audioListener;
    private TcpClient? _videoClient;
    private TcpClient? _audioClient;
    private NetworkStream? _videoStream;
    private NetworkStream? _audioStream;
    private Task<TcpClient>? _audioAccept;
    private Process? _ffmpeg;
    private readonly StringBuilder _stderr = new();
    private byte[] _audioScratch = [];
    private int _frameCount;
    private bool _started;
    private bool _stopped;
    private bool _faulted;

    /// <summary>How long to wait for ffmpeg to connect back to each socket.</summary>
    private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(15);

    /// <summary>How long to wait for ffmpeg to flush + exit after the sockets close.</summary>
    private static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(30);

    public FfmpegVideoRecorder(
        string ffmpegPath,
        FfmpegVideoFormat format,
        int width,
        int height,
        double frameRate,
        string outputPath,
        bool includeAudio,
        int sampleRate = 44100,
        int channels = 1)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ffmpegPath);
        ArgumentNullException.ThrowIfNull(format);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        if (frameRate <= 0) throw new ArgumentOutOfRangeException(nameof(frameRate));

        _ffmpegPath = ffmpegPath;
        _format = format;
        _width = width;
        _height = height;
        _frameRate = frameRate;
        _outputPath = outputPath;
        _includeAudio = includeAudio;
        _sampleRate = sampleRate;
        _channels = channels;
        _expectedFrameBytes = checked(width * height * 4);
    }

    /// <inheritdoc/>
    public int SampleRate => _sampleRate;

    /// <inheritdoc/>
    public int Channels => _channels;

    /// <inheritdoc/>
    public int FrameCount
    {
        get { lock (_sync) { return _frameCount; } }
    }

    /// <summary>The container format being written.</summary>
    public FfmpegVideoFormat Format => _format;

    /// <summary>Captured ffmpeg stderr, populated after a fault or stop (diagnostics).</summary>
    public string StandardError
    {
        get { lock (_stderr) { return _stderr.ToString(); } }
    }

    /// <summary>
    /// Launch ffmpeg and wait for it to connect both sockets. Throws
    /// <see cref="InvalidOperationException"/> (with captured stderr) when ffmpeg
    /// cannot be started or does not connect within the timeout.
    /// </summary>
    public void Start()
    {
        lock (_sync)
        {
            if (_started) throw new InvalidOperationException("Recorder already started.");
            _started = true;

            var dir = Path.GetDirectoryName(_outputPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            _videoListener = new TcpListener(IPAddress.Loopback, 0);
            _videoListener.Start();
            var videoPort = ((IPEndPoint)_videoListener.LocalEndpoint).Port;

            var audioPort = 0;
            if (_includeAudio)
            {
                _audioListener = new TcpListener(IPAddress.Loopback, 0);
                _audioListener.Start();
                audioPort = ((IPEndPoint)_audioListener.LocalEndpoint).Port;
            }

            var argv = FfmpegArgumentBuilder.Build(
                _format, _width, _height, _frameRate,
                videoPort, _includeAudio, audioPort,
                _sampleRate, _channels, _outputPath);

            var psi = new ProcessStartInfo
            {
                FileName = _ffmpegPath,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            };
            foreach (var arg in argv)
                psi.ArgumentList.Add(arg);

            try
            {
                _ffmpeg = Process.Start(psi)
                    ?? throw new InvalidOperationException($"Failed to start ffmpeg at '{_ffmpegPath}'.");
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                throw new InvalidOperationException($"Failed to start ffmpeg at '{_ffmpegPath}': {ex.Message}", ex);
            }

            // Drain stdout/stderr so a chatty ffmpeg never blocks on a full pipe.
            DrainAsync(_ffmpeg.StandardError, capture: true);
            DrainAsync(_ffmpeg.StandardOutput, capture: false);

            // The video connection is mandatory and quick (ffmpeg opens input 0
            // during init); block briefly for it so frame writes have a stream.
            _videoClient = AcceptOrThrow(_videoListener, "video");
            _videoStream = _videoClient.GetStream();

            // Audio connects asynchronously: ffmpeg may only open the audio input
            // after it has begun reading video, so blocking here could deadlock
            // Start(). WriteSamples promotes the accepted client to a live stream
            // once it connects; any samples before then are dropped (a few ms).
            if (_includeAudio && _audioListener is not null)
                _audioAccept = _audioListener.AcceptTcpClientAsync();
        }
    }

    private TcpClient AcceptOrThrow(TcpListener listener, string label)
    {
        var task = listener.AcceptTcpClientAsync();
        if (!task.Wait(ConnectTimeout))
            throw new InvalidOperationException(
                $"ffmpeg did not connect the {label} stream within {ConnectTimeout.TotalSeconds:F0}s. " +
                $"ffmpeg stderr: {StandardError}");
        return task.Result;
    }

    // Promote the async audio accept to a live stream once ffmpeg has connected.
    // Must be called under _sync.
    private void EnsureAudioStreamLocked()
    {
        if (_audioStream is not null || _audioAccept is null)
            return;
        if (_audioAccept.IsCompletedSuccessfully)
        {
            _audioClient = _audioAccept.Result;
            _audioStream = _audioClient.GetStream();
        }
    }

    private void DrainAsync(StreamReader reader, bool capture)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                string? line;
                while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) is not null)
                {
                    if (capture)
                        lock (_stderr) { _stderr.AppendLine(line); }
                }
            }
            catch
            {
                // Process exited / pipe closed - nothing to drain.
            }
        });
    }

    /// <inheritdoc/>
    public void CaptureFrame(ReadOnlySpan<byte> bgra, int width, int height)
    {
        if (bgra.Length != _expectedFrameBytes)
            throw new ArgumentException(
                $"Frame is {bgra.Length} bytes but the recorder was configured for {_width}x{_height} ({_expectedFrameBytes} bytes).",
                nameof(bgra));

        lock (_sync)
        {
            if (_stopped || _faulted || _videoStream is null) return;
            try
            {
                _videoStream.Write(bgra);
                _frameCount++;
            }
            catch (Exception ex) when (ex is IOException or ObjectDisposedException or SocketException)
            {
                _faulted = true;
            }
        }
    }

    /// <inheritdoc/>
    public void WriteSamples(ReadOnlySpan<short> samples)
    {
        if (samples.IsEmpty) return;

        lock (_sync)
        {
            if (_stopped || _faulted) return;
            EnsureAudioStreamLocked();
            if (_audioStream is null) return; // ffmpeg has not connected audio yet
            try
            {
                _audioStream.Write(ToLittleEndianBytes(samples));
            }
            catch (Exception ex) when (ex is IOException or ObjectDisposedException or SocketException)
            {
                _faulted = true;
            }
        }
    }

    private ReadOnlySpan<byte> ToLittleEndianBytes(ReadOnlySpan<short> samples)
    {
        var byteLen = samples.Length * sizeof(short);
        if (_audioScratch.Length < byteLen)
            _audioScratch = new byte[byteLen];

        if (BitConverter.IsLittleEndian)
        {
            MemoryMarshal.AsBytes(samples).CopyTo(_audioScratch);
        }
        else
        {
            for (var i = 0; i < samples.Length; i++)
            {
                _audioScratch[i * 2] = (byte)(samples[i] & 0xFF);
                _audioScratch[i * 2 + 1] = (byte)((samples[i] >> 8) & 0xFF);
            }
        }

        return new ReadOnlySpan<byte>(_audioScratch, 0, byteLen);
    }

    /// <summary>
    /// Close both sockets (signalling end-of-stream to ffmpeg) and wait for ffmpeg
    /// to finalise the container. Idempotent.
    /// </summary>
    public void Stop()
    {
        Process? ffmpeg;
        Task<TcpClient>? pendingAudioAccept;
        lock (_sync)
        {
            if (_stopped) return;
            _stopped = true;

            // Grab the audio stream if it connected (so we close it cleanly and
            // ffmpeg sees EOF on both inputs).
            EnsureAudioStreamLocked();

            // Closing the streams sends FIN; ffmpeg's rawvideo/s16le demuxers see
            // EOF and -shortest finalises the muxer.
            CloseQuietly(_videoStream);
            CloseQuietly(_audioStream);
            _videoClient?.Close();
            _audioClient?.Close();
            _videoListener?.Stop();
            _audioListener?.Stop();
            pendingAudioAccept = _audioAccept;
            ffmpeg = _ffmpeg;
        }

        // Observe the audio-accept task so a never-connected audio input does not
        // surface as an unobserved task exception when the listener is stopped.
        if (pendingAudioAccept is not null)
        {
            _ = pendingAudioAccept.ContinueWith(
                t => { _ = t.Exception; t.Result.Close(); },
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnRanToCompletion,
                TaskScheduler.Default);
            _ = pendingAudioAccept.ContinueWith(
                t => { _ = t.Exception; },
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted,
                TaskScheduler.Default);
        }

        if (ffmpeg is null) return;
        try
        {
            if (!ffmpeg.WaitForExit((int)ShutdownTimeout.TotalMilliseconds))
            {
                try { ffmpeg.Kill(entireProcessTree: true); } catch { /* already gone */ }
            }
        }
        catch
        {
            // Process already exited / inaccessible.
        }
    }

    private static void CloseQuietly(NetworkStream? stream)
    {
        if (stream is null) return;
        try { stream.Flush(); } catch { }
        try { stream.Dispose(); } catch { }
    }

    /// <inheritdoc/>
    public void Dispose() => Stop();
}
