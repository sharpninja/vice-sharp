using System.IO;
using ViceSharp.Abstractions;
using ViceSharp.Core.Media;
using ViceSharp.Protocol;

namespace ViceSharp.Host.Runtime;

public sealed class EmulatorRuntimeSession
{
    private DateTimeOffset _lastPerformanceSampleTime;
    private long _lastPerformanceSampleCycle;
    private long _lastPerformanceSampleFrameCount;

    // Lock-free double-buffered presented frame. The emulation worker writes a
    // completed frame to the back buffer then atomically publishes it; the UI
    // reads the published buffer with no lock and copies it into its own render
    // surface. The two big pixel buffers are reused (no per-frame allocation).
    private byte[] _frameBufferA = Array.Empty<byte>();
    private byte[] _frameBufferB = Array.Empty<byte>();
    private volatile byte[]? _publishedFrame;
    private int _committedWidth;
    private int _committedHeight;
    private long _committedCycle;
    private bool _writeToA = true;

    // Live media-recording state (FR-MED-002 video / FR-MED-003 audio). Guarded
    // by _captureSync, which the emulation worker takes per frame to tee video and
    // which the RPC threads take to begin/finalise a recording. Distinct from
    // SyncRoot so the lock-free UI frame read path is unaffected.
    private readonly object _captureSync = new();
    private IVideoCaptureSink? _videoCapture;
    private IAudioRecorder? _videoAudioTrack;
    private string? _videoCaptureId;
    private WavAudioRecorder? _audioRecorder;
    private Stream? _audioStream;
    private string? _audioCaptureId;

    public EmulatorRuntimeSession(
        string sessionId,
        IArchitectureDescriptor architecture,
        IMachine machine,
        IecBusActivityMonitor? iecBusActivity = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentNullException.ThrowIfNull(architecture);
        ArgumentNullException.ThrowIfNull(machine);

        SessionId = sessionId;
        Architecture = architecture;
        Machine = machine;
        IecBusActivity = iecBusActivity;
        _lastPerformanceSampleTime = DateTimeOffset.UtcNow;
        _lastPerformanceSampleCycle = machine.GetState().Cycle;
    }

    public string SessionId { get; }

    public IArchitectureDescriptor Architecture { get; }

    public IMachine Machine { get; }

    public IecBusActivityMonitor? IecBusActivity { get; }

    /// <summary>
    /// Rolling last-100-instruction trace with per-instruction memory write-deltas, for the
    /// time-travel debugger. Populated by the emulation pump from the machine's pub/sub.
    /// </summary>
    public TickHistoryRecorder TickHistory { get; } = new();

    /// <summary>
    /// Gates the time-travel tick-history recorder (BUG-TICKHIST-PERF-001). Default false:
    /// the recorder stays UNSUBSCRIBED so the per-instruction chip-state capture and the
    /// per-write delta recording impose zero overhead and emulation runs at full speed.
    /// The host arms it the first time the History panel reads the trace
    /// (<c>MonitorServiceHost.GetTickHistoryAsync</c>), making the debugger pay-for-use.
    /// Read on the emulation worker thread, written from RPC threads, so it is volatile.
    /// </summary>
    public volatile bool HistoryRecordingEnabled;

    public string PowerState { get; set; } = "On";

    public EmulatorRunState RunState { get; set; } = EmulatorRunState.Stopped;

    public object SyncRoot { get; } = new();

    public Dictionary<MediaSlot, MediaAttachmentDto> MediaAttachments { get; } = new();

    public Dictionary<string, KeyStateDto> KeyStates { get; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<InputPort, JoystickStateDto> JoystickStates { get; } = new();

    public Dictionary<string, CaptureSessionDto> CaptureSessions { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The live audio-recording tap installed in the SID -> output path at
    /// machine-build time, or null when the session has no live audio path
    /// (headless / test rigs built without the platform backend). Sound capture
    /// (FR-MED-003) attaches a WAV recorder to this tap; video capture does not
    /// use it.
    /// </summary>
    public CaptureAudioTap? AudioCaptureTap { get; set; }

    /// <summary>True while a numbered-BMP video capture is in progress.</summary>
    public bool IsVideoCaptureActive
    {
        get { lock (_captureSync) { return _videoCapture is not null; } }
    }

    /// <summary>True while a WAV sound recording is in progress.</summary>
    public bool IsAudioCaptureActive
    {
        get { lock (_captureSync) { return _audioRecorder is not null; } }
    }

    /// <summary>Number of frames written by the active (or just-finished) video capture.</summary>
    public int VideoCaptureFrameCount
    {
        get { lock (_captureSync) { return _videoCapture?.FrameCount ?? 0; } }
    }

    public SortedSet<ushort> Breakpoints { get; } = new();

    public double LimiterRatePercent { get; set; } = 100;

    public bool LimiterEnabled { get; set; } = true;

    /// <summary>
    /// Selected emulation pacing strategy id ("semaphore" | "vice"), surfaced in the
    /// limiter settings. The active gate lives on the global emulation pump; this mirrors
    /// the choice so GetSettings round-trips it.
    /// </summary>
    public string PacingStrategy { get; set; } = "semaphore";

    public DisplaySettingsDto DisplaySettings { get; set; } = new();

    public InputSettingsDto InputSettings { get; set; } = new();

    public AudioSettingsDto AudioSettings { get; set; } = new();

    public ResourceSettingsDto ResourceSettings { get; set; } = new();

    public long FrameCount { get; private set; }

    public double MeasuredFramesPerSecond { get; private set; }

    public double EffectiveClockHz { get; private set; }

    public string SelectedKeyboardMapId { get; set; } = "c64:gtk3_pos";

    public KeyboardMapDto? SelectedKeyboardMap { get; set; }

    public HostKeyboardAutomation? HostKeyboardAutomation { get; private set; }

    public string? LastHostAutomationError { get; private set; }

    public void StartHostKeyboardAutomation(HostKeyboardAutomation automation)
    {
        ArgumentNullException.ThrowIfNull(automation);

        HostKeyboardAutomation = automation;
        LastHostAutomationError = null;
    }

    public void ClearHostKeyboardAutomation()
    {
        HostKeyboardAutomation = null;
        LastHostAutomationError = null;
    }

    public void AdvanceHostAutomationFrame()
    {
        var automation = HostKeyboardAutomation;
        if (automation is null)
            return;

        automation.AdvanceFrame(Machine);
        if (!automation.IsActive)
        {
            LastHostAutomationError = automation.LastError;
            HostKeyboardAutomation = null;
        }
    }

    public void RecordFrame()
    {
        FrameCount++;
        UpdatePerformanceCounters();
    }

    /// <summary>Master-cycle stamp at which the last published frame completed.</summary>
    public long CommittedFrameCycle => _committedCycle;

    /// <summary>True once at least one complete frame has been published.</summary>
    public bool HasCommittedFrame => _publishedFrame is not null;

    /// <summary>
    /// Publish a complete video frame for tear-free, lock-free presentation. Called
    /// by the emulation worker at the instant the video chip raises FrameCompleted -
    /// while the framebuffer holds a whole frame and before the next frame's lines
    /// overwrite it. Writes to the back buffer then atomically publishes it; the two
    /// pixel buffers are reused so there is no per-frame allocation. The UI reads the
    /// published buffer with NO lock (it never touches <see cref="SyncRoot"/>), so the
    /// render pull cannot stall the emulation thread.
    /// </summary>
    public void CommitFrame(IVideoChip videoChip, long cycle)
    {
        ArgumentNullException.ThrowIfNull(videoChip);

        var source = videoChip.FrameBuffer;
        // Choose the buffer that is NOT currently published so a concurrent reader
        // of the published buffer is never overwritten mid-copy.
        var back = _writeToA ? _frameBufferA : _frameBufferB;
        if (back.Length != source.Length)
        {
            back = new byte[source.Length];
            if (_writeToA) _frameBufferA = back; else _frameBufferB = back;
        }

        source.CopyTo(back, 0);
        _committedWidth = videoChip.FrameWidth;
        _committedHeight = videoChip.FrameHeight;
        _committedCycle = cycle;
        _publishedFrame = back;       // volatile publish (release barrier)
        _writeToA = !_writeToA;

        // Tee the just-published frame to an active video capture (FR-MED-002).
        // The copy `back` is owned by this session, so passing it to the BMP
        // writer cannot race the next frame's render.
        RecordVideoFrameIfActive(((ReadOnlySpan<byte>)back)[..source.Length], _committedWidth, _committedHeight);
    }

    /// <summary>
    /// Writes the supplied BGRA frame to the active video capture (numbered BMP
    /// sequence) when one is running; a no-op otherwise. Called by the emulation
    /// worker from <see cref="CommitFrame"/>, and directly by tests to drive a
    /// capture without a full video chip.
    /// </summary>
    public void RecordVideoFrameIfActive(ReadOnlySpan<byte> bgra, int width, int height)
    {
        lock (_captureSync)
        {
            if (_videoCapture is null)
                return;
            try
            {
                _videoCapture.CaptureFrame(bgra, width, height);
            }
            catch
            {
                // A recorder fault (e.g. ffmpeg died, or a frame-size mismatch) must
                // never propagate onto the emulation worker thread. Drop the frame;
                // the recorder's own fault latch stops further writes and the user
                // finalises via StopCapture (which runs off this thread).
            }
        }
    }

    /// <summary>
    /// Begins a video capture driven by <paramref name="sink"/> (a numbered-BMP
    /// sequence or a muxed ffmpeg recorder). Any previously-active video capture
    /// is finalised first. When <paramref name="audioTrack"/> is supplied and the
    /// session has a live audio path, it is attached to the
    /// <see cref="AudioCaptureTap"/> so the recorder also receives the SID audio
    /// stream (FR-MED-004 muxed video+audio). The caller has already started the
    /// sink (e.g. launched ffmpeg) so it is ready to receive frames/samples.
    /// </summary>
    public void BeginVideoCapture(string captureId, IVideoCaptureSink sink, IAudioRecorder? audioTrack = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(captureId);
        ArgumentNullException.ThrowIfNull(sink);
        lock (_captureSync)
        {
            EndVideoCaptureCore();
            _videoCapture = sink;
            _videoCaptureId = captureId;

            if (audioTrack is not null && AudioCaptureTap is not null)
            {
                _videoAudioTrack = audioTrack;
                AudioCaptureTap.AttachRecorder(audioTrack);
            }
        }
    }

    /// <summary>
    /// Finalises the active video capture (detaching any audio track from the tap
    /// and disposing the sink, which flushes/closes an ffmpeg recorder) and
    /// returns the number of frames it wrote (0 when none was active).
    /// </summary>
    public int EndVideoCapture()
    {
        lock (_captureSync)
        {
            return EndVideoCaptureCore();
        }
    }

    // Must be called under _captureSync.
    private int EndVideoCaptureCore()
    {
        var frames = _videoCapture?.FrameCount ?? 0;

        // Detach the audio track from the tap BEFORE disposing the sink so no
        // further samples are written into a half-closed ffmpeg recorder.
        if (_videoAudioTrack is not null)
        {
            AudioCaptureTap?.DetachRecorder();
            _videoAudioTrack = null;
        }

        _videoCapture?.Dispose();
        _videoCapture = null;
        _videoCaptureId = null;
        return frames;
    }

    /// <summary>
    /// Begins a WAV sound recording (FR-MED-003) by attaching
    /// <paramref name="recorder"/> to the live audio tap. Requires
    /// <see cref="AudioCaptureTap"/> to be set (a live audio path). The session
    /// takes ownership of <paramref name="recorder"/> and <paramref name="stream"/>
    /// for the duration of the capture and finalises both in
    /// <see cref="EndAudioCapture"/>.
    /// </summary>
    public void BeginAudioCapture(string captureId, WavAudioRecorder recorder, Stream stream)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(captureId);
        ArgumentNullException.ThrowIfNull(recorder);
        ArgumentNullException.ThrowIfNull(stream);

        var tap = AudioCaptureTap
            ?? throw new InvalidOperationException("This session has no live audio path to record from.");

        lock (_captureSync)
        {
            _audioRecorder = recorder;
            _audioStream = stream;
            _audioCaptureId = captureId;
            tap.AttachRecorder(recorder);
        }
    }

    /// <summary>
    /// Finalises the active WAV recording: detaches it from the tap, patches the
    /// RIFF/data sizes, and closes the output stream. Returns the number of PCM
    /// data bytes written (0 when none was active).
    /// </summary>
    public long EndAudioCapture()
    {
        lock (_captureSync)
        {
            AudioCaptureTap?.DetachRecorder();
            long bytes = _audioRecorder?.DataBytesWritten ?? 0;
            _audioRecorder?.Dispose();   // Stop() patches header sizes
            _audioStream?.Dispose();
            _audioRecorder = null;
            _audioStream = null;
            _audioCaptureId = null;
            return bytes;
        }
    }

    /// <summary>
    /// Copy the latest published frame into the caller's destination span (e.g. a
    /// WriteableBitmap's locked buffer), with NO allocation and NO lock. Returns
    /// false when no complete frame has been published yet, or the destination is
    /// too small. This is the UI's read path: a <see cref="ReadOnlySpan{T}"/> over
    /// the emulation thread's published buffer, copied into the UI's own buffer.
    /// </summary>
    public bool TryCopyLatestFrameInto(Span<byte> destination, out int width, out int height, out long cycle)
    {
        var published = _publishedFrame; // volatile read (acquire barrier)
        if (published is null)
        {
            width = 0;
            height = 0;
            cycle = 0;
            return false;
        }

        width = _committedWidth;
        height = _committedHeight;
        cycle = _committedCycle;

        if (destination.Length < published.Length)
            return false;

        ((ReadOnlySpan<byte>)published).CopyTo(destination);
        return true;
    }

    public void ResetPerformanceCounters()
    {
        FrameCount = 0;
        MeasuredFramesPerSecond = 0;
        EffectiveClockHz = 0;
        _lastPerformanceSampleTime = DateTimeOffset.UtcNow;
        _lastPerformanceSampleCycle = Machine.GetState().Cycle;
        _lastPerformanceSampleFrameCount = FrameCount;
    }

    public void UpdatePerformanceCounters()
    {
        var now = DateTimeOffset.UtcNow;
        var elapsed = (now - _lastPerformanceSampleTime).TotalSeconds;
        if (elapsed < 0.25)
            return;

        var state = Machine.GetState();
        var cycleDelta = Math.Max(0, state.Cycle - _lastPerformanceSampleCycle);
        var frameDelta = Math.Max(0, FrameCount - _lastPerformanceSampleFrameCount);

        EffectiveClockHz = cycleDelta / elapsed;
        MeasuredFramesPerSecond = frameDelta / elapsed;
        _lastPerformanceSampleTime = now;
        _lastPerformanceSampleCycle = state.Cycle;
        _lastPerformanceSampleFrameCount = FrameCount;
    }
}
