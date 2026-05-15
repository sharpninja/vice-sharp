using ViceSharp.Abstractions;
using ViceSharp.Protocol;

namespace ViceSharp.Host.Runtime;

public sealed class EmulatorRuntimeSession
{
    private DateTimeOffset _lastPerformanceSampleTime;
    private long _lastPerformanceSampleCycle;
    private long _lastPerformanceSampleFrameCount;

    public EmulatorRuntimeSession(
        string sessionId,
        IArchitectureDescriptor architecture,
        IMachine machine)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentNullException.ThrowIfNull(architecture);
        ArgumentNullException.ThrowIfNull(machine);

        SessionId = sessionId;
        Architecture = architecture;
        Machine = machine;
        _lastPerformanceSampleTime = DateTimeOffset.UtcNow;
        _lastPerformanceSampleCycle = machine.GetState().Cycle;
    }

    public string SessionId { get; }

    public IArchitectureDescriptor Architecture { get; }

    public IMachine Machine { get; }

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
