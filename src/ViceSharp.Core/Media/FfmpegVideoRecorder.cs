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
    // Background writers move the actual socket writes OFF the emulation worker
    // thread (the worker only copies + enqueues). Created once each stream connects.
    private BackgroundByteWriter? _videoWriter;
    private BackgroundByteWriter? _audioWriter;
    private readonly StringBuilder _stderr = new();
    private byte[] _audioScratch = [];
    // Audio captured before ffmpeg connects the audio socket is buffered here
    // (bounded) and flushed when the writer comes up, so the start of the audio
    // track is never lost.
    private byte[] _pendingAudio = [];
    private int _pendingAudioLen;
    private int _frameCount;
    private bool _started;
    private bool _stopped;
    private bool _faulted;

    // Bounded queue depth. The writers are DROP-ON-FULL (see Start/EnsureAudioStream):
    // the emulation worker must never block on a stalled ffmpeg socket, so a full
    // queue drops the overflow rather than freezing the emulator. The video depth is
    // sized to absorb ffmpeg's startup window (output open + header) without dropping
    // a normal recording's first frames. ~32 BGRA frames (~13 MB at 384x272) / 64
    // audio batches.
    private const int VideoQueueCapacity = 32;
    private const int AudioQueueCapacity = 64;

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

    /// <summary>Frames dropped by the background video writer under back-pressure (compresses
    /// the clip timeline, since each survivor is tagged at the nominal frame rate).</summary>
    public long DroppedFrameCount => _videoWriter?.DroppedCount ?? 0;

    /// <summary>The container format being written.</summary>
    public FfmpegVideoFormat Format => _format;

    /// <summary>Captured ffmpeg stderr, populated after a fault or stop (diagnostics).</summary>
    public string StandardError
    {
        get
        {
            string err;
            lock (_stderr) { err = _stderr.ToString(); }
            if (_videoWriter?.FaultException is { } vf) err += $"\n[video-writer fault] {vf}";
            if (_audioWriter?.FaultException is { } af) err += $"\n[audio-writer fault] {af}";
            // A non-zero drop count means ffmpeg could not drain the socket fast enough
            // and frames/samples were dropped to keep the emulator live (never frozen).
            if (_videoWriter is { DroppedCount: > 0 } vw) err += $"\n[video-writer dropped] {vw.DroppedCount} frame(s) under back-pressure";
            if (_audioWriter is { DroppedCount: > 0 } aw) err += $"\n[audio-writer dropped] {aw.DroppedCount} batch(es) under back-pressure";
            return err;
        }
    }

    /// <summary>
    /// Launch ffmpeg and wait for it to connect both sockets. Throws
    /// <see cref="InvalidOperationException"/> (with captured stderr) when ffmpeg
    /// cannot be started or does not connect within the timeout.
    /// </summary>
    public void Start()
    {
        // Claim the started flag under the lock, then release it: the process
        // launch and the blocking socket accept run WITHOUT holding _sync, so the
        // operational lock (used by the emulation tee paths) is never held during
        // long I/O.
        lock (_sync)
        {
            if (_started) throw new InvalidOperationException("Recorder already started.");
            _started = true;
        }

        TcpListener? videoListener = null;
        TcpListener? audioListener = null;
        Process? ffmpeg = null;
        TcpClient? videoClient = null;
        NetworkStream? videoStream = null;
        Task<TcpClient>? audioAccept = null;

        try
        {
            var dir = Path.GetDirectoryName(_outputPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            videoListener = new TcpListener(IPAddress.Loopback, 0);
            videoListener.Start();
            var videoPort = ((IPEndPoint)videoListener.LocalEndpoint).Port;

            var audioPort = 0;
            if (_includeAudio)
            {
                audioListener = new TcpListener(IPAddress.Loopback, 0);
                audioListener.Start();
                audioPort = ((IPEndPoint)audioListener.LocalEndpoint).Port;
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
                ffmpeg = Process.Start(psi)
                    ?? throw new InvalidOperationException($"Failed to start ffmpeg at '{_ffmpegPath}'.");
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                throw new InvalidOperationException($"Failed to start ffmpeg at '{_ffmpegPath}': {ex.Message}", ex);
            }

            // Drain stdout/stderr so a chatty ffmpeg never blocks on a full pipe.
            DrainAsync(ffmpeg.StandardError, capture: true);
            DrainAsync(ffmpeg.StandardOutput, capture: false);

            // The video connection is mandatory and quick (ffmpeg opens input 0
            // during init); block briefly for it so frame writes have a stream.
            videoClient = AcceptOrThrow(videoListener, "video");
            videoStream = videoClient.GetStream();
            videoListener.Stop(); // only one client connects; stop listening now

            // Audio connects asynchronously: ffmpeg may only open the audio input
            // after it has begun reading video, so blocking here could deadlock
            // Start(). WriteSamples promotes the accepted client to a live stream
            // once it connects; any samples before then are dropped (a few ms).
            if (_includeAudio && audioListener is not null)
                audioAccept = audioListener.AcceptTcpClientAsync();

            // Publish the connected resources under a short critical section, and
            // start the background video writer (the worker enqueues; this thread
            // performs the socket writes).
            var connectedVideoStream = videoStream;
            lock (_sync)
            {
                _videoListener = videoListener;
                _audioListener = audioListener;
                _ffmpeg = ffmpeg;
                _videoClient = videoClient;
                _videoStream = videoStream;
                _audioAccept = audioAccept;
                _videoWriter = new BackgroundByteWriter(
                    (b, n) => connectedVideoStream.Write(b, 0, n), VideoQueueCapacity, "vice-ffmpeg-video",
                    dropWhenFull: true);
            }
        }
        catch
        {
            // Any failure after we claimed _started must release every resource we
            // acquired (ffmpeg process + listeners + clients) so nothing leaks;
            // callers are not required to dispose a recorder whose Start threw.
            DisposeStartResources(videoStream, videoClient, videoListener, audioListener, audioAccept, ffmpeg);
            lock (_sync) { _stopped = true; }
            throw;
        }
    }

    private static void DisposeStartResources(
        NetworkStream? videoStream, TcpClient? videoClient,
        TcpListener? videoListener, TcpListener? audioListener,
        Task<TcpClient>? audioAccept, Process? ffmpeg)
    {
        CloseQuietly(videoStream);
        try { videoClient?.Close(); } catch { /* already closed */ }
        try { videoListener?.Stop(); } catch { /* already stopped */ }
        try { audioListener?.Stop(); } catch { /* already stopped */ }
        ObserveAudioAccept(audioAccept);
        if (ffmpeg is not null)
        {
            try { if (!ffmpeg.HasExited) ffmpeg.Kill(entireProcessTree: true); } catch { /* already gone */ }
            try { ffmpeg.Dispose(); } catch { /* already disposed */ }
        }
    }

    // Observe a pending audio-accept task so a never-connected audio input does not
    // surface as an unobserved task exception when its listener is stopped.
    private static void ObserveAudioAccept(Task<TcpClient>? pendingAudioAccept)
    {
        if (pendingAudioAccept is null) return;

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

    private TcpClient AcceptOrThrow(TcpListener listener, string label)
    {
        var task = listener.AcceptTcpClientAsync();
        if (!task.Wait(ConnectTimeout))
            throw new InvalidOperationException(
                $"ffmpeg did not connect the {label} stream within {ConnectTimeout.TotalSeconds:F0}s. " +
                $"ffmpeg stderr: {StandardError}");
        return task.Result;
    }

    // Promote the async audio accept to a live stream once ffmpeg has connected,
    // and start its background writer. Must be called under _sync.
    private void EnsureAudioStreamLocked()
    {
        if (_audioStream is not null || _audioAccept is null)
            return;
        if (_audioAccept.IsCompletedSuccessfully)
        {
            _audioClient = _audioAccept.Result;
            _audioStream = _audioClient.GetStream();
            _audioListener?.Stop(); // only one client connects; stop listening now
            var connectedAudioStream = _audioStream;
            _audioWriter = new BackgroundByteWriter(
                (b, n) => connectedAudioStream.Write(b, 0, n), AudioQueueCapacity, "vice-ffmpeg-audio",
                dropWhenFull: true);

            // Flush audio captured before the connection completed.
            if (_pendingAudioLen > 0)
            {
                _audioWriter.Enqueue(new ReadOnlySpan<byte>(_pendingAudio, 0, _pendingAudioLen));
                _pendingAudioLen = 0;
            }
        }
    }

    // Buffer pre-connection audio bytes (bounded to ~0.5s) so the start of the
    // track survives the brief window before ffmpeg connects the audio socket.
    // Must be called under _sync.
    private void BufferPendingAudioLocked(ReadOnlySpan<byte> bytes)
    {
        const int MaxPendingBytes = 44100 * 2; // ~0.5s of mono s16
        if (_pendingAudioLen + bytes.Length > MaxPendingBytes)
            return; // audio never connected; stop growing the buffer

        var needed = _pendingAudioLen + bytes.Length;
        if (_pendingAudio.Length < needed)
            Array.Resize(ref _pendingAudio, Math.Max(needed, 8192));

        bytes.CopyTo(_pendingAudio.AsSpan(_pendingAudioLen));
        _pendingAudioLen += bytes.Length;
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
        BackgroundByteWriter writer;
        lock (_sync)
        {
            if (_stopped || _faulted || _videoWriter is null) return;
            if (_videoWriter.Faulted) { _faulted = true; return; }

            // A frame whose size does not match the configured geometry is dropped
            // (and faults the recorder) rather than thrown on the emulation worker's
            // hot path.
            if (bgra.Length != _expectedFrameBytes)
            {
                _faulted = true;
                return;
            }

            writer = _videoWriter;
            _frameCount++;
        }

        // Copy + enqueue happen OUTSIDE _sync: the (rare) back-pressure block when
        // the writer is saturated never holds the operational lock.
        writer.Enqueue(bgra);
    }

    /// <inheritdoc/>
    public void WriteSamples(ReadOnlySpan<short> samples)
    {
        if (samples.IsEmpty) return;

        BackgroundByteWriter writer;
        ReadOnlySpan<byte> bytes;
        lock (_sync)
        {
            if (_stopped || _faulted) return;
            EnsureAudioStreamLocked();
            bytes = ToLittleEndianBytes(samples); // fills the reused scratch (worker thread only)

            if (_audioWriter is null)
            {
                // ffmpeg has not connected audio yet: buffer so the track start is
                // not lost (flushed by EnsureAudioStreamLocked on connect).
                BufferPendingAudioLocked(bytes);
                return;
            }
            if (_audioWriter.Faulted) { _faulted = true; return; }
            writer = _audioWriter;
        }

        writer.Enqueue(bytes);
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
        // Latch stopped; if audio was expected but ffmpeg has not connected the
        // audio socket yet (a very short recording can Stop within ffmpeg's startup
        // window), capture the pending accept so we can wait for it below.
        Task<TcpClient>? acceptToAwait;
        lock (_sync)
        {
            if (_stopped) return;
            _stopped = true;
            acceptToAwait = (_includeAudio && _audioWriter is null) ? _audioAccept : null;
        }

        // Wait briefly for a late audio connection, then promote it and flush the
        // buffered audio - otherwise -shortest would truncate the file to zero.
        if (acceptToAwait is not null)
        {
            try { acceptToAwait.Wait(ConnectTimeout); } catch { /* never connected */ }
            lock (_sync) { EnsureAudioStreamLocked(); }
        }

        Process? ffmpeg;
        Task<TcpClient>? pendingAudioAccept;
        BackgroundByteWriter? videoWriter;
        BackgroundByteWriter? audioWriter;
        NetworkStream? videoStream;
        NetworkStream? audioStream;
        TcpClient? videoClient;
        TcpClient? audioClient;
        TcpListener? videoListener;
        TcpListener? audioListener;
        lock (_sync)
        {
            videoWriter = _videoWriter;
            audioWriter = _audioWriter;
            videoStream = _videoStream;
            audioStream = _audioStream;
            videoClient = _videoClient;
            audioClient = _audioClient;
            videoListener = _videoListener;
            audioListener = _audioListener;
            ffmpeg = _ffmpeg;

            // Only observe the accept task when audio NEVER connected; once it has,
            // the accepted client is _audioClient and is closed below (avoids a
            // double-close race between the observe continuation and EnsureAudioStream).
            pendingAudioAccept = _audioStream is null ? _audioAccept : null;
        }

        // 1) Flush every queued frame/sample to the sockets (the background writers
        //    perform the actual writes), then close the sockets so ffmpeg sees EOF
        //    on both inputs and -shortest finalises the muxer. If a join times out
        //    (ffmpeg stopped reading), closing the stream deliberately unblocks the
        //    stuck writer - its pending Write throws and the writer thread exits -
        //    rather than racing it; NetworkStream tolerates a write/dispose overlap.
        videoWriter?.CompleteAndJoin(ShutdownTimeout);
        audioWriter?.CompleteAndJoin(ShutdownTimeout);

        CloseQuietly(videoStream);
        CloseQuietly(audioStream);
        try { videoClient?.Close(); } catch { /* already closed */ }
        try { audioClient?.Close(); } catch { /* already closed */ }
        try { videoListener?.Stop(); } catch { /* already stopped */ }
        try { audioListener?.Stop(); } catch { /* already stopped */ }

        ObserveAudioAccept(pendingAudioAccept);

        // 2) Wait for ffmpeg to finalise the container.
        if (ffmpeg is not null)
        {
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

        // 3) Release the writer queues (returns any pooled buffers).
        videoWriter?.Dispose();
        audioWriter?.Dispose();
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
