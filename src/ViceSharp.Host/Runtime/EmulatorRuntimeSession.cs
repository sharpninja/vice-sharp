using ViceSharp.Abstractions;
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

    public string PowerState { get; set; } = "On";

    public EmulatorRunState RunState { get; set; } = EmulatorRunState.Stopped;

    public object SyncRoot { get; } = new();

    public Dictionary<MediaSlot, MediaAttachmentDto> MediaAttachments { get; } = new();

    public Dictionary<string, KeyStateDto> KeyStates { get; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<InputPort, JoystickStateDto> JoystickStates { get; } = new();

    public Dictionary<string, CaptureSessionDto> CaptureSessions { get; } = new(StringComparer.OrdinalIgnoreCase);

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
