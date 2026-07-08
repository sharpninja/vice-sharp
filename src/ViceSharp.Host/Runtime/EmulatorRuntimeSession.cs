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

    // Per-CPU rate baselines, keyed "{index}:{label}" so each CPU in the machine's roster
    // (host + each peripheral) gets its own executed-cycle anchor for an independent rate.
    private Dictionary<string, long> _lastPerCpuExecuted = new();

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

    // Capture stats overlay (FR-MEDOVL-001): a reusable buffer the overlay is drawn into so the
    // burned-in stats land only in the recording, never on the live presented frame.
    private byte[]? _overlayFrame;
    private DateTimeOffset _captureStartUtc;
    private long _captureStartCycle;

    /// <summary>When true (default), an active video capture has a live-stats overlay burned in
    /// (rate, fps, emulated-vs-wall time, cycle) for debugging pacing/recorder behaviour.</summary>
    public bool CaptureStatsOverlayEnabled { get; set; } = true;

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
        // Anchor on the same source MachineCycle reads (machine time), so the
        // first performance sample after construction measures a clean delta.
        _lastPerformanceSampleCycle = machine.GetState().Cycle;
        _lastPerCpuExecuted = SnapshotCpuExecuted();
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

    public object InputStateSyncRoot { get; } = new();

    public Dictionary<MediaSlot, MediaAttachmentDto> MediaAttachments { get; } = new();

    public Dictionary<string, KeyStateDto> KeyStates { get; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<InputPort, JoystickStateDto> JoystickStates { get; } = new();

    public Dictionary<string, CaptureSessionDto> CaptureSessions { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The live audio-recording tap installed in the SID -> output path at
    /// machine-build time, or null when the session has no live audio path
    /// (headless / test rigs built without the platform backend). Sound capture
    /// (FR-MED-003) attaches a WAV recorder to this tap; video capture does not
    /// use it. Assigning the tap applies the current warp audio policy so a
    /// session already in warp pauses its live output immediately
    /// (TR-AUDIO-WARP-001).
    /// </summary>
    public CaptureAudioTap? AudioCaptureTap
    {
        get => _audioCaptureTap;
        set
        {
            _audioCaptureTap = value;
            // A freshly attached tap is unpaused; only suspension (warp or
            // fast-forward past the live-audio ceiling) needs enforcing here.
            _liveAudioOutputSuspendedApplied = IsLiveAudioOutputSuspended;
            if (_liveAudioOutputSuspendedApplied)
                value?.Pause();
        }
    }

    private CaptureAudioTap? _audioCaptureTap;

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

    /// <summary>
    /// Fast-forward rates above this keep live audio suspended: a 44100 Hz
    /// device cannot be fed at more than double speed without resampling, so
    /// past 200 percent the vsync regulator paces the requested rate while
    /// the live leaf discards (TR-AUDIO-WARP-001).
    /// </summary>
    public const double LiveAudioMaxRatePercent = 200;

    /// <summary>
    /// Limiter target as a percentage of real time. Setting it applies the
    /// live audio policy: above <see cref="LiveAudioMaxRatePercent"/> the
    /// live output leaf is paused (fast-forward), at or below it resumes.
    /// </summary>
    public double LimiterRatePercent
    {
        get => _limiterRatePercent;
        set
        {
            _limiterRatePercent = value;
            ApplyLiveAudioPolicy();
            PushRelativeSpeedToAudioChip();
        }
    }

    private double _limiterRatePercent = 100;

    /// <summary>
    /// True while the pace limiter is on; false is warp. Toggling applies the
    /// live audio policy (TR-AUDIO-WARP-001): warp pauses the live audio leaf
    /// so SID fragments are discarded without blocking, mirroring VICE's
    /// sound_flush (sound.c:1528-1531 discards pending samples;
    /// sound.c:1573 gates the blocking device write on !warp_mode_enabled)
    /// while an attached recorder keeps receiving samples through the tap.
    /// SID sample calculation itself continues, matching VICE's
    /// SoundEmulateOnWarp default of 1 (sound.c:733).
    /// </summary>
    public bool LimiterEnabled
    {
        get => _limiterEnabled;
        set
        {
            _limiterEnabled = value;
            ApplyLiveAudioPolicy();
            PushRelativeSpeedToAudioChip();
        }
    }

    private bool _limiterEnabled = true;

    // Keep the SID's live-audio cadence in step with the limiter (VICE
    // sound_set_relative_speed tracks relative_speed from vsync.c,
    // sound.c:913/:1799): with the limiter on, the fixed-rate device then
    // paces emulation to the requested rate. Skipped in warp - output is
    // suspended and vsync is bypassed anyway; re-enabling pushes the rate.
    private void PushRelativeSpeedToAudioChip()
    {
        if (!_limiterEnabled)
            return;

        if (Machine.Devices.GetByRole(DeviceRole.AudioChip) is IAudioChip chip)
            chip.SetRelativeSpeed(_limiterRatePercent);
    }

    public bool IsWarpMode => !LimiterEnabled;

    /// <summary>
    /// True while live audio output is suspended - in warp, or fast-forward
    /// past <see cref="LiveAudioMaxRatePercent"/>. The pacing gates use this
    /// to skip the sound regulator (which relies on device back-pressure) and
    /// pace by vsync instead, so fast-forward actually reaches its target.
    /// </summary>
    public bool IsLiveAudioOutputSuspended => !_limiterEnabled || _limiterRatePercent > LiveAudioMaxRatePercent;

    // Pause the live output leaf while suspended (drop fragments non-blocking),
    // resume it otherwise - on state TRANSITIONS only, so repeated setter
    // writes and the two-step SetLimiter assignment do not churn the device.
    // The tap sits above the leaf, so recorders attached for sound capture
    // keep receiving samples either way - exactly VICE's warp behavior with
    // an active recording device (sound.c:1528).
    private bool _liveAudioOutputSuspendedApplied;

    private void ApplyLiveAudioPolicy()
    {
        var suspended = IsLiveAudioOutputSuspended;
        if (suspended == _liveAudioOutputSuspendedApplied)
            return;

        _liveAudioOutputSuspendedApplied = suspended;
        if (_audioCaptureTap is not { } tap)
            return;

        if (suspended)
            tap.Pause();
        else
            tap.Resume();
    }

    /// <summary>
    /// Selected emulation pacing strategy id ("semaphore" | "vice"), surfaced in the
    /// limiter settings. The active gate lives on the global emulation pump; this mirrors
    /// the choice so GetSettings round-trips it.
    /// </summary>
    public string PacingStrategy { get; set; } = "vice";

    public DisplaySettingsDto DisplaySettings { get; set; } = new();

    public InputSettingsDto InputSettings { get; set; } = new();

    public AudioSettingsDto AudioSettings { get; set; } = new();

    public ResourceSettingsDto ResourceSettings { get; set; } = new();

    public long FrameCount { get; private set; }

    public double MeasuredFramesPerSecond { get; private set; }

    public double EffectiveClockHz { get; private set; }

    /// <summary>
    /// Per-CPU speed readings - one per CPU in the machine's roster (host first, then each
    /// peripheral CPU), each measuring that CPU's own executed-cycle rate over wall time and its
    /// percentage of its own clock. Lets the status surface list every CPU's rate distinctly.
    /// </summary>
    public IReadOnlyList<CpuRateReading> PerCpuRates { get; private set; } = Array.Empty<CpuRateReading>();

    public string SelectedKeyboardMapId { get; set; } = "c64:gtk3_pos";

    public KeyboardMapDto? SelectedKeyboardMap { get; set; }

    public HostKeyboardAutomation? HostKeyboardAutomation { get; private set; }

    public string? LastHostAutomationError { get; private set; }

    public void SetLimiter(double ratePercent, bool enabled)
    {
        var changed = LimiterRatePercent != ratePercent || LimiterEnabled != enabled;
        LimiterRatePercent = ratePercent;
        LimiterEnabled = enabled;

        if (changed)
            PublishWarpModeStatus();
    }

    public void PublishWarpModeStatus()
    {
        Machine.PubSub?.Publish(
            WarpModeEvent.Topic,
            new WarpModeEvent(IsWarpMode, LimiterEnabled, LimiterRatePercent, Machine.GetState().Cycle));
    }

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
        // Snapshot the sink under the lock, then write OUTSIDE it: a sink whose
        // bounded queue is saturated applies back-pressure (a blocking Enqueue), and
        // that block must not be held under _captureSync or it would stall Begin/End.
        IVideoCaptureSink? sink;
        lock (_captureSync)
        {
            sink = _videoCapture;
        }

        if (sink is null)
            return;

        // Burn the live-stats overlay into a private copy so the recording carries the numbers
        // while the presented frame stays clean. The byte writer copies on enqueue, so one
        // reused buffer is safe on this single worker thread.
        var outBytes = bgra;
        if (CaptureStatsOverlayEnabled && width > 0 && height > 0 && bgra.Length >= width * height * 4)
        {
            if (_overlayFrame is null || _overlayFrame.Length < bgra.Length)
                _overlayFrame = new byte[bgra.Length];

            bgra.CopyTo(_overlayFrame);
            var view = _overlayFrame.AsSpan(0, bgra.Length);
            StatsOverlayRenderer.Draw(view, width, height, BuildCaptureOverlayLines(sink));
            outBytes = view;
        }

        try
        {
            sink.CaptureFrame(outBytes, width, height);
        }
        catch
        {
            // A recorder fault (e.g. ffmpeg died, or a frame-size mismatch) must
            // never propagate onto the emulation worker thread. Drop the frame;
            // the recorder's own fault latch stops further writes and the user
            // finalises via StopCapture (which runs off this thread).
        }
    }

    // Live-stats overlay lines for the capture. EMU vs WALL seconds at capture time is the key
    // pair: if the emulator runs faster than real time, EMU outpaces WALL (a pacing bug); if they
    // track but a clip still plays fast, the container fps tag is wrong (a recorder bug).
    private IReadOnlyList<string> BuildCaptureOverlayLines(IVideoCaptureSink sink)
    {
        var master = Architecture.MasterClockHz;
        var pct = master > 0 ? EffectiveClockHz / master * 100.0 : 0.0;
        // Emulated seconds SINCE capture start (not since boot) so EMU vs WALL reads the pace
        // over the recording: EMU < WALL means the run is behind real time.
        var emuSeconds = master > 0 ? (_committedCycle - _captureStartCycle) / (double)master : 0.0;
        var wallSeconds = (DateTimeOffset.UtcNow - _captureStartUtc).TotalSeconds;
        var inv = System.Globalization.CultureInfo.InvariantCulture;

        return new[]
        {
            string.Format(inv, "RATE {0:0}% FPS {1:0.0}", pct, MeasuredFramesPerSecond),
            string.Format(inv, "EMU {0:0.0}S WALL {1:0.0}S", emuSeconds, wallSeconds),
            string.Format(inv, "FRM {0} DROP {1}", sink.FrameCount, sink.DroppedFrameCount),
        };
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
    /// <remarks>
    /// Throws <see cref="InvalidOperationException"/> when a video capture is
    /// already active, or when a muxed audio track is requested but the audio tap
    /// is already in use (atomic conflict guard for concurrent StartCapture calls).
    /// </remarks>
    public void BeginVideoCapture(string captureId, IVideoCaptureSink sink, IAudioRecorder? audioTrack = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(captureId);
        ArgumentNullException.ThrowIfNull(sink);
        lock (_captureSync)
        {
            if (_videoCapture is not null)
                throw new InvalidOperationException("A video capture is already active for this session.");
            if (audioTrack is not null && AudioCaptureTap is { IsRecording: true })
                throw new InvalidOperationException("Audio is already being recorded for this session.");

            _videoCapture = sink;
            _videoCaptureId = captureId;
            _captureStartUtc = DateTimeOffset.UtcNow;
            _captureStartCycle = _committedCycle;

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
    /// returns the number of frames it wrote (0 when none was active). The blocking
    /// dispose (writer join / ffmpeg wait) runs OFF <c>_captureSync</c>.
    /// </summary>
    public int EndVideoCapture()
    {
        IVideoCaptureSink? sink;
        int frames;
        lock (_captureSync)
        {
            sink = DetachVideoLocked(out frames);
        }

        sink?.Dispose();
        return frames;
    }

    // Detaches the active video sink + its audio track under _captureSync, returning
    // the sink for the caller to Dispose OFF the lock. Returns null when none active.
    private IVideoCaptureSink? DetachVideoLocked(out int frames)
    {
        var sink = _videoCapture;
        frames = sink?.FrameCount ?? 0;

        // Detach the audio track from the tap BEFORE the sink is disposed so no
        // further samples are written into a half-closed ffmpeg recorder.
        if (_videoAudioTrack is not null)
        {
            AudioCaptureTap?.DetachRecorder();
            _videoAudioTrack = null;
        }

        _videoCapture = null;
        _videoCaptureId = null;
        return sink;
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
            // Atomic conflict guard: only one recorder may own the tap at a time
            // (a WAV recording and a muxed-video audio track cannot coexist).
            if (_audioRecorder is not null || tap.IsRecording)
                throw new InvalidOperationException("Audio is already being recorded for this session.");

            _audioRecorder = recorder;
            _audioStream = stream;
            _audioCaptureId = captureId;
            tap.AttachRecorder(recorder);
        }
    }

    /// <summary>
    /// Finalises the active WAV recording: detaches it from the tap, patches the
    /// RIFF/data sizes, and closes the output stream. Returns the number of PCM
    /// data bytes written (0 when none was active). The blocking dispose (writer
    /// join) runs OFF <c>_captureSync</c>.
    /// </summary>
    public long EndAudioCapture()
    {
        WavAudioRecorder? recorder;
        Stream? stream;
        lock (_captureSync)
        {
            (recorder, stream) = DetachAudioLocked();
        }

        recorder?.Dispose();   // off-lock: Stop() flushes + joins the writer
        stream?.Dispose();
        return recorder?.DataBytesWritten ?? 0;
    }

    // Detaches the active WAV recorder + stream under _captureSync, returning them
    // for the caller to Dispose OFF the lock. Returns (null, null) when none active.
    private (WavAudioRecorder? Recorder, Stream? Stream) DetachAudioLocked()
    {
        AudioCaptureTap?.DetachRecorder();
        var recorder = _audioRecorder;
        var stream = _audioStream;
        _audioRecorder = null;
        _audioStream = null;
        _audioCaptureId = null;
        return (recorder, stream);
    }

    /// <summary>
    /// Finalises any in-progress video AND audio capture. Called when the session
    /// is closed/removed so an active ffmpeg process or open file handle is never
    /// leaked on teardown. The blocking disposes run OFF <c>_captureSync</c>.
    /// </summary>
    public void EndAllCaptures()
    {
        IVideoCaptureSink? sink;
        WavAudioRecorder? recorder;
        Stream? stream;
        lock (_captureSync)
        {
            sink = DetachVideoLocked(out _);
            (recorder, stream) = DetachAudioLocked();
        }

        sink?.Dispose();
        recorder?.Dispose();
        stream?.Dispose();
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

    // Headline speed is emulated MACHINE time (FR-CPUTICK-001: the store's
    // AC2 defines the PER-CPU percent, which lives in PerCpuRates and may
    // legitimately dip to 95-100 on the BA-stalled primary; the headline
    // stays the machine clock so a perfectly paced session reads 100%).
    private long MachineCycle => Machine.GetState().Cycle;

    public void ResetPerformanceCounters() => ResetPerformanceCounters(DateTimeOffset.UtcNow);

    internal void ResetPerformanceCounters(DateTimeOffset now)
    {
        FrameCount = 0;
        MeasuredFramesPerSecond = 0;
        EffectiveClockHz = 0;
        PerCpuRates = Array.Empty<CpuRateReading>();
        _lastPerformanceSampleTime = now;
        _lastPerformanceSampleCycle = MachineCycle;
        _lastPerformanceSampleFrameCount = FrameCount;
        _lastPerCpuExecuted = SnapshotCpuExecuted();
    }

    public void UpdatePerformanceCounters() => UpdatePerformanceCounters(DateTimeOffset.UtcNow);

    internal void UpdatePerformanceCounters(DateTimeOffset now)
    {
        var elapsed = (now - _lastPerformanceSampleTime).TotalSeconds;
        if (elapsed < 0.25)
            return;

        var executed = MachineCycle;
        var cycleDelta = Math.Max(0, executed - _lastPerformanceSampleCycle);
        var frameDelta = Math.Max(0, FrameCount - _lastPerformanceSampleFrameCount);

        EffectiveClockHz = cycleDelta / elapsed;
        MeasuredFramesPerSecond = frameDelta / elapsed;
        PerCpuRates = ComputePerCpuRates(elapsed);
        _lastPerformanceSampleTime = now;
        _lastPerformanceSampleCycle = executed;
        _lastPerformanceSampleFrameCount = FrameCount;
    }

    // Measures each CPU in the machine's roster on its OWN clock: that CPU's executed-cycle
    // delta since the last sample over the elapsed wall time, and as a percentage of its target
    // clock. Re-anchors per CPU so the host and each peripheral read independently. A CPU that
    // appears mid-run (hot-plugged drive) baselines this sample and reads a real rate next time.
    private IReadOnlyList<CpuRateReading> ComputePerCpuRates(double elapsed)
    {
        var infos = Machine.CpuInfos;
        if (infos.Count == 0)
        {
            _lastPerCpuExecuted = new Dictionary<string, long>();
            return Array.Empty<CpuRateReading>();
        }

        var readings = new List<CpuRateReading>(infos.Count);
        var next = new Dictionary<string, long>(infos.Count);
        for (var i = 0; i < infos.Count; i++)
        {
            var info = infos[i];
            var key = $"{i}:{info.Label}";
            next[key] = info.ExecutedCycles;

            var previous = _lastPerCpuExecuted.TryGetValue(key, out var p) ? p : info.ExecutedCycles;
            var delta = Math.Max(0, info.ExecutedCycles - previous);
            var hz = elapsed > 0 ? delta / elapsed : 0.0;
            var percent = info.ClockHz > 0 ? hz / info.ClockHz * 100.0 : 0.0;
            readings.Add(new CpuRateReading(info.Label, hz, percent));
        }

        _lastPerCpuExecuted = next;
        return readings;
    }

    private Dictionary<string, long> SnapshotCpuExecuted()
    {
        var infos = Machine.CpuInfos;
        var samples = new Dictionary<string, long>(infos.Count);
        for (var i = 0; i < infos.Count; i++)
            samples[$"{i}:{infos[i].Label}"] = infos[i].ExecutedCycles;
        return samples;
    }
}
