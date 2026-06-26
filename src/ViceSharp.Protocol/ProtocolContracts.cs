namespace ViceSharp.Protocol;

public static class ViceSharpProtocol
{
    public const string Package = "vice_sharp.v1";
    public const string ProtoFile = "Protos/emulator_host.proto";
}

public interface IRpcResponse
{
    RpcStatus Status { get; }
}

public sealed record RpcStatus(RpcStatusCode Code, string Message)
{
    public bool IsSuccess => Code == RpcStatusCode.Ok;

    public static RpcStatus Ok() => new(RpcStatusCode.Ok, string.Empty);

    public static RpcStatus InvalidArgument(string message) => new(RpcStatusCode.InvalidArgument, message);

    public static RpcStatus NotFound(string message) => new(RpcStatusCode.NotFound, message);

    public static RpcStatus FailedPrecondition(string message) => new(RpcStatusCode.FailedPrecondition, message);

    public static RpcStatus Unavailable(string message) => new(RpcStatusCode.Unavailable, message);

    public static RpcStatus NotImplemented(string message) => new(RpcStatusCode.NotImplemented, message);
}

public enum RpcStatusCode
{
    Ok = 0,
    InvalidArgument = 1,
    NotFound = 2,
    FailedPrecondition = 3,
    Unavailable = 4,
    Internal = 5,
    NotImplemented = 6
}

public enum EmulatorRunState
{
    Stopped = 0,
    Running = 1,
    Paused = 2
}

public enum MediaSlot
{
    Drive8 = 0,
    Drive9 = 1,
    Tape = 2,
    Cartridge = 3
}

public enum InputPort
{
    Joystick1 = 0,
    Joystick2 = 1,
    Keyboard = 2,
    PrimaryJoystick = 3
}

public enum CaptureKind
{
    Screenshot = 0,
    Video = 1,
    Audio = 2
}

public enum ResetKind
{
    Warm = 0,
    Cold = 1,
    ResetAndAutostartDrive8 = 2
}

public enum SettingApplyScope
{
    Live = 0,
    RestartRequired = 1
}

public enum SettingsResourceKind
{
    Display = 0,
    Input = 1,
    File = 2,
    Audio = 3,
    Resource = 4
}

public sealed record MachineStateDto(byte A, byte X, byte Y, byte S, byte P, ushort Pc, long Cycle);

/// <summary>One CPU's speed entry on the status surface: its label, its effective rate (its own
/// executed-cycle delta over wall time), and that as a percent of its own target clock.</summary>
public sealed record PerCpuRateDto(string Label, double EffectiveClockHz, double EffectiveClockPercent);

/// <summary>One IEC line's live state for the bus monitor: the signal name, its resolved level
/// (high = released), and a display string of the endpoints pulling it low (empty when released).</summary>
public sealed record IecBusLineDto(string Signal, bool IsHigh, string Pullers);

public sealed record EmulatorStatusDto(
    string SessionId,
    string Architecture,
    EmulatorRunState RunState,
    long Cycle,
    MachineStateDto MachineState,
    string PowerState = "On",
    double LimiterRatePercent = 100,
    double MeasuredFps = 0,
    long FrameCount = 0,
    long NominalClockHz = 0,
    double EffectiveClockHz = 0,
    double EffectiveClockPercent = 0,
    ushort Pc = 0,
    string ModelId = "",
    string HostAutomationDescription = "",
    bool HostAutomationActive = false,
    string LastHostAutomationError = "",
    bool IecBusActive = false,
    long IecBusTransitionCount = 0,
    string IecBusActivityState = "Idle")
{
    public double MeasuredFramesPerSecond => MeasuredFps;

    /// <summary>
    /// Per-CPU speed entries - one per CPU in the rig (host first, then each peripheral CPU) so
    /// the status surface lists each distinctly. Init-only with an empty default so existing
    /// construction sites are unaffected; set via a <c>with</c> expression on the mapping path.
    /// </summary>
    public IReadOnlyList<PerCpuRateDto> PerCpuRates { get; init; } = Array.Empty<PerCpuRateDto>();

    /// <summary>
    /// Live IEC bus line states for the monitor panel - one entry per signal (ATN/CLK/DATA/SRQ)
    /// with its resolved level and who is pulling it. Init-only with an empty default; populated
    /// only for sessions that have a true-drive IEC bus.
    /// </summary>
    public IReadOnlyList<IecBusLineDto> IecBusLines { get; init; } = Array.Empty<IecBusLineDto>();
}

public static class EmulatorHost
{
    public const string ServiceName = "vice_sharp.v1.EmulatorHost";
    public const string CreateSession = "CreateSession";
    public const string GetStatus = "GetStatus";
    public const string Start = "Start";
    public const string Pause = "Pause";
    public const string Resume = "Resume";
    public const string Reset = "Reset";
    public const string ColdReset = "ColdReset";
    public const string WarmReset = "WarmReset";
    public const string ResetAndAutostartDrive8 = "ResetAndAutostartDrive8";
    public const string StepCycle = "StepCycle";
    public const string StepFrame = "StepFrame";
    public const string RewindCycle = "RewindCycle";
    public const string RewindFrame = "RewindFrame";
    public const string SetLimiterRate = "SetLimiterRate";
    public const string CloseSession = "CloseSession";
}

public interface IEmulatorHost
{
    ValueTask<CreateEmulatorSessionResponse> CreateSessionAsync(
        CreateEmulatorSessionRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<GetEmulatorStatusResponse> GetStatusAsync(
        SessionRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<EmulatorCommandResponse> StartAsync(
        SessionRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<EmulatorCommandResponse> PauseAsync(
        SessionRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<EmulatorCommandResponse> ResumeAsync(
        SessionRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<EmulatorCommandResponse> ResetAsync(
        SessionRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<EmulatorCommandResponse> ResetAsync(
        ResetRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<EmulatorCommandResponse> ColdResetAsync(
        SessionRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<EmulatorCommandResponse> WarmResetAsync(
        SessionRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<EmulatorCommandResponse> ResetAndAutostartDrive8Async(
        ResetAndAutostartDrive8Request request,
        CancellationToken cancellationToken = default);

    ValueTask<EmulatorCommandResponse> StepCycleAsync(
        StepCycleRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<EmulatorCommandResponse> StepFrameAsync(
        StepFrameRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<EmulatorCommandResponse> RewindCycleAsync(
        RewindCycleRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<EmulatorCommandResponse> RewindFrameAsync(
        RewindFrameRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<EmulatorCommandResponse> SetLimiterRateAsync(
        SetLimiterRateRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<EmulatorCommandResponse> CloseSessionAsync(
        SessionRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record CreateEmulatorSessionRequest(
    string ArchitectureId = "minimal",
    string DisplayName = "",
    bool TrueDrive = false,
    int TrueDriveDevice = 8,
    string TrueDriveDiskImagePath = "");

public sealed record SessionRequest(string SessionId);

public sealed record ResetRequest(string SessionId, ResetKind Kind = ResetKind.Warm);

public sealed record ResetAndAutostartDrive8Request(string SessionId);

public sealed record StepCycleRequest(string SessionId, long CycleCount = 1);

public sealed record StepFrameRequest(string SessionId, int FrameCount = 1);

public sealed record RewindCycleRequest(string SessionId, long CycleCount = 1);

public sealed record RewindFrameRequest(string SessionId, int FrameCount = 1);

public sealed record SetLimiterRateRequest(string SessionId, double LimiterRatePercent);

public sealed record CreateEmulatorSessionResponse(
    RpcStatus Status,
    string SessionId,
    EmulatorStatusDto? EmulatorStatus) : IRpcResponse;

public sealed record GetEmulatorStatusResponse(
    RpcStatus Status,
    EmulatorStatusDto? EmulatorStatus) : IRpcResponse;

public sealed record EmulatorCommandResponse(
    RpcStatus Status,
    EmulatorStatusDto? EmulatorStatus) : IRpcResponse;

public static class DiagnosticsService
{
    public const string ServiceName = "vice_sharp.v1.DiagnosticsService";
    public const string GetHostInfo = "GetHostInfo";
    public const string ListSessions = "ListSessions";
    public const string GetCurrentSession = "GetCurrentSession";
    public const string GetPerformanceSnapshot = "GetPerformanceSnapshot";
    public const string WatchPerformance = "WatchPerformance";
}

public interface IDiagnosticsService
{
    ValueTask<GetHostInfoResponse> GetHostInfoAsync(CancellationToken cancellationToken = default);

    ValueTask<ListSessionsResponse> ListSessionsAsync(CancellationToken cancellationToken = default);

    ValueTask<GetCurrentSessionResponse> GetCurrentSessionAsync(CancellationToken cancellationToken = default);

    ValueTask<PerformanceSnapshotResponse> GetPerformanceSnapshotAsync(
        PerformanceSnapshotRequest request,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<PerformanceSnapshotResponse> WatchPerformanceAsync(
        WatchPerformanceRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record HostInfoDto(
    int ProcessId,
    string Endpoint,
    string ProtocolPackage,
    string ProtocolVersion,
    string AppVersion,
    string BuildSha,
    DateTimeOffset StartedAtUtc);

public sealed record SessionSummaryDto(
    string SessionId,
    string DisplayName,
    string Architecture,
    EmulatorRunState RunState,
    long Cycle,
    long FrameCount,
    string CurrentMedia);

public sealed record ProcessDiagnosticsDto(
    long TotalProcessorTimeMs,
    long WorkingSetBytes,
    long PrivateMemoryBytes,
    long ManagedMemoryBytes,
    int Gen0Collections,
    int Gen1Collections,
    int Gen2Collections,
    int ThreadCount);

public sealed record PumpDiagnosticsDto(
    bool WorkerAlive,
    int ActiveSessionCount,
    DateTimeOffset LastTickAtUtc);

public sealed record UiDiagnosticsDto(
    string CurrentSessionId,
    DateTimeOffset LastStatusUpdateUtc,
    DateTimeOffset LastFrameUpdateUtc);

public sealed record PerformanceSnapshotDto(
    HostInfoDto HostInfo,
    EmulatorStatusDto? EmulatorStatus,
    ProcessDiagnosticsDto Process,
    PumpDiagnosticsDto Pump,
    UiDiagnosticsDto Ui);

public sealed record PerformanceSnapshotRequest(string SessionId = "", int IntervalMs = 1000);

public sealed record WatchPerformanceRequest(string SessionId = "", int IntervalMs = 1000);

public sealed record GetHostInfoResponse(
    RpcStatus Status,
    HostInfoDto? HostInfo) : IRpcResponse;

public sealed record ListSessionsResponse(
    RpcStatus Status,
    IReadOnlyList<SessionSummaryDto> Sessions) : IRpcResponse;

public sealed record GetCurrentSessionResponse(
    RpcStatus Status,
    SessionSummaryDto? Session) : IRpcResponse;

public sealed record PerformanceSnapshotResponse(
    RpcStatus Status,
    PerformanceSnapshotDto? Snapshot) : IRpcResponse;

public static class SettingsService
{
    public const string ServiceName = "vice_sharp.v1.SettingsService";
    public const string ListProfiles = "ListProfiles";
    public const string GetSettings = "GetSettings";
    public const string UpdateSettings = "UpdateSettings";
    public const string ValidateResources = "ValidateResources";
}

public interface ISettingsService
{
    ValueTask<ListSettingsProfilesResponse> ListProfilesAsync(
        SessionRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<GetSettingsResponse> GetSettingsAsync(
        SessionRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<UpdateSettingsResponse> UpdateSettingsAsync(
        UpdateSettingsRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<ValidateSettingsResourcesResponse> ValidateResourcesAsync(
        ValidateSettingsResourcesRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record SettingsProfileDto(
    string Id,
    string DisplayName,
    string Machine,
    bool IsCurrent,
    bool IsAvailable,
    string Description = "");

public sealed record LimiterSettingsDto(double RatePercent = 100, bool IsEnabled = true, string PacingStrategy = "vice");

public sealed record DisplaySettingsDto(
    string Renderer = "host",
    string Palette = "default",
    bool ShowBorder = true,
    bool MaintainAspectRatio = true,
    string Scale = "2x",
    string CropMode = "visible-area",
    string AspectMode = "vice-pixel-aspect");

public sealed record InputSettingsDto(
    string KeyboardMapId = "c64:gtk3_pos",
    InputPort PrimaryJoystickPort = InputPort.Joystick2,
    bool SwapJoystickPorts = false,
    string Mode = "keyboard-joystick");

public sealed record AudioSettingsDto(string Mode = "enabled");

public sealed record ResourceSettingsDto(string Mode = "auto-detect");

public sealed record SessionSettingsDto(
    string ProfileId,
    LimiterSettingsDto Limiter,
    DisplaySettingsDto Display,
    InputSettingsDto Input,
    AudioSettingsDto? Audio = null,
    ResourceSettingsDto? Resources = null);

public sealed record SettingApplyDiagnosticDto(
    string Setting,
    SettingApplyScope Scope,
    bool AppliedLive,
    bool RestartRequired,
    string Message);

public sealed record SettingsResourceValidationDto(
    string ResourceKey,
    SettingsResourceKind Kind,
    bool IsValid,
    bool RestartRequired,
    string Message);

public sealed record UpdateSettingsRequest(
    string SessionId,
    LimiterSettingsDto? Limiter = null,
    DisplaySettingsDto? Display = null,
    InputSettingsDto? Input = null,
    string ProfileId = "",
    bool RestartSession = false,
    AudioSettingsDto? Audio = null,
    ResourceSettingsDto? Resources = null);

public sealed record ValidateSettingsResourcesRequest(
    string SessionId,
    LimiterSettingsDto? Limiter = null,
    DisplaySettingsDto? Display = null,
    InputSettingsDto? Input = null,
    AudioSettingsDto? Audio = null,
    ResourceSettingsDto? Resources = null);

public sealed record ListSettingsProfilesResponse(
    RpcStatus Status,
    IReadOnlyList<SettingsProfileDto> Profiles) : IRpcResponse;

public sealed record GetSettingsResponse(
    RpcStatus Status,
    SessionSettingsDto? Settings) : IRpcResponse;

public sealed record UpdateSettingsResponse(
    RpcStatus Status,
    SessionSettingsDto? Settings,
    IReadOnlyList<SettingApplyDiagnosticDto> Diagnostics) : IRpcResponse;

public sealed record ValidateSettingsResourcesResponse(
    RpcStatus Status,
    IReadOnlyList<SettingsResourceValidationDto> Resources) : IRpcResponse;

public static class MediaService
{
    public const string ServiceName = "vice_sharp.v1.MediaService";
    public const string AttachMedia = "AttachMedia";
    public const string DetachMedia = "DetachMedia";
    public const string ListMedia = "ListMedia";
}

public interface IMediaService
{
    ValueTask<AttachMediaResponse> AttachMediaAsync(
        AttachMediaRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<DetachMediaResponse> DetachMediaAsync(
        DetachMediaRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<ListMediaResponse> ListMediaAsync(
        SessionRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record MediaAttachmentDto(
    MediaSlot Slot,
    string FilePath,
    string DisplayName,
    bool IsAttached,
    bool IsReadOnly,
    bool AppliedToRuntime,
    string Error = "");

public sealed record AttachMediaRequest(
    string SessionId,
    MediaSlot Slot,
    string FilePath,
    string DisplayName = "",
    bool IsReadOnly = false,
    byte[]? Payload = null);

public sealed record DetachMediaRequest(string SessionId, MediaSlot Slot);

public sealed record AttachMediaResponse(
    RpcStatus Status,
    MediaAttachmentDto? Attachment) : IRpcResponse;

public sealed record DetachMediaResponse(
    RpcStatus Status,
    MediaAttachmentDto? Attachment) : IRpcResponse;

public sealed record ListMediaResponse(
    RpcStatus Status,
    IReadOnlyList<MediaAttachmentDto> Attachments) : IRpcResponse;

public static class VideoService
{
    public const string ServiceName = "vice_sharp.v1.VideoService";
    public const string GetVideoStatus = "GetVideoStatus";
    public const string GetFrame = "GetFrame";
}

public interface IVideoService
{
    ValueTask<GetVideoStatusResponse> GetVideoStatusAsync(
        SessionRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<GetVideoFrameResponse> GetFrameAsync(
        SessionRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record VideoStatusDto(bool IsAvailable, int Width, int Height, long Cycle);

public sealed record VideoFrameDto(int Width, int Height, long Cycle, byte[] Bgra);

public sealed record GetVideoStatusResponse(
    RpcStatus Status,
    VideoStatusDto? VideoStatus) : IRpcResponse;

public sealed record GetVideoFrameResponse(
    RpcStatus Status,
    VideoFrameDto? Frame) : IRpcResponse;

public static class InputService
{
    public const string ServiceName = "vice_sharp.v1.InputService";
    public const string SetKeyState = "SetKeyState";
    public const string SetJoystickState = "SetJoystickState";
    public const string GetInputState = "GetInputState";
    public const string ListKeyboardMaps = "ListKeyboardMaps";
    public const string SetKeyboardMap = "SetKeyboardMap";
}

public interface IInputService
{
    ValueTask<InputCommandResponse> SetKeyStateAsync(
        SetKeyStateRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<InputCommandResponse> SetJoystickStateAsync(
        SetJoystickStateRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<GetInputStateResponse> GetInputStateAsync(
        SessionRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<ListKeyboardMapsResponse> ListKeyboardMapsAsync(
        SessionRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<KeyboardMapResponse> SetKeyboardMapAsync(
        SetKeyboardMapRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record KeyStateDto(
    string Key,
    bool IsPressed,
    bool AppliedToRuntime,
    string PhysicalKey = "",
    string Text = "",
    int Modifiers = 0);

public sealed record JoystickStateDto(byte DirectionMask, bool FireButton, bool AppliedToRuntime);

public sealed record JoystickPortStateDto(InputPort Port, JoystickStateDto State);

public sealed record KeyboardMapDto(
    string Id,
    string DisplayName,
    string Machine,
    string Kind,
    string SourcePath,
    bool IsSelected,
    bool IsBuiltin,
    string Error = "");

public sealed record InputStateDto(
    IReadOnlyList<KeyStateDto> Keys,
    IReadOnlyList<JoystickPortStateDto> Joysticks,
    KeyboardMapDto? SelectedKeyboardMap = null);

public sealed record SetKeyStateRequest(
    string SessionId,
    string Key,
    bool IsPressed,
    string PhysicalKey = "",
    string Text = "",
    int Modifiers = 0);

public sealed record SetJoystickStateRequest(
    string SessionId,
    InputPort Port,
    byte DirectionMask,
    bool FireButton);

public sealed record SetKeyboardMapRequest(
    string SessionId,
    string KeyboardMapId,
    byte[]? Payload = null,
    string DisplayName = "",
    string SourcePath = "");

public sealed record InputCommandResponse(
    RpcStatus Status,
    InputStateDto? InputState) : IRpcResponse;

public sealed record GetInputStateResponse(
    RpcStatus Status,
    InputStateDto? InputState) : IRpcResponse;

public sealed record ListKeyboardMapsResponse(
    RpcStatus Status,
    IReadOnlyList<KeyboardMapDto> KeyboardMaps) : IRpcResponse;

public sealed record KeyboardMapResponse(
    RpcStatus Status,
    KeyboardMapDto? KeyboardMap,
    InputStateDto? InputState = null) : IRpcResponse;

public static class MonitorService
{
    public const string ServiceName = "vice_sharp.v1.MonitorService";
    public const string ExecuteCommand = "ExecuteCommand";
    public const string ReadRegisters = "ReadRegisters";
    public const string Disassemble = "Disassemble";
    public const string ListBreakpoints = "ListBreakpoints";
    public const string AddBreakpoint = "AddBreakpoint";
    public const string RemoveBreakpoint = "RemoveBreakpoint";
    public const string ReadMemory = "ReadMemory";
    public const string WriteMemory = "WriteMemory";
    public const string GetTickHistory = "GetTickHistory";
    public const string ReadMemoryAtTick = "ReadMemoryAtTick";
    public const string GetChipStateAtTick = "GetChipStateAtTick";
}

public interface IMonitorService
{
    ValueTask<MonitorCommandResponse> ExecuteCommandAsync(
        ExecuteMonitorCommandRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<MonitorRegistersResponse> ReadRegistersAsync(
        SessionRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<MonitorDisassemblyResponse> DisassembleAsync(
        MonitorDisassemblyRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<MonitorBreakpointsResponse> ListBreakpointsAsync(
        SessionRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<MonitorBreakpointsResponse> AddBreakpointAsync(
        MonitorBreakpointRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<MonitorBreakpointsResponse> RemoveBreakpointAsync(
        MonitorBreakpointRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<MonitorMemoryResponse> ReadMemoryAsync(
        MonitorReadMemoryRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<MonitorMemoryWriteResponse> WriteMemoryAsync(
        MonitorWriteMemoryRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Time-travel debugger: the last captured CPU instructions (ticks).</summary>
    ValueTask<GetTickHistoryResponse> GetTickHistoryAsync(
        SessionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Time-travel debugger: a memory window reconstructed as it was at a past tick
    /// (current paused memory with later ticks' write-deltas reverse-applied).</summary>
    ValueTask<MonitorMemoryResponse> ReadMemoryAtTickAsync(
        ReadMemoryAtTickRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Time-travel debugger: each chip's decoded full state as captured at a past tick.</summary>
    ValueTask<GetChipStateAtTickResponse> GetChipStateAtTickAsync(
        GetChipStateAtTickRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record ExecuteMonitorCommandRequest(string SessionId, string Command);

public sealed record MonitorReadMemoryRequest(string SessionId, int Address, int Length);

public sealed record MonitorWriteMemoryRequest(string SessionId, int Address, byte[] Data);

public sealed record MonitorDisassemblyRequest(string SessionId, int Address, int Count);

public sealed record MonitorBreakpointRequest(string SessionId, int Address);

public sealed record MonitorCommandResponse(
    RpcStatus Status,
    string Output,
    EmulatorStatusDto? EmulatorStatus) : IRpcResponse;

public sealed record MonitorRegistersResponse(
    RpcStatus Status,
    MachineStateDto? Registers,
    EmulatorStatusDto? EmulatorStatus) : IRpcResponse;

public sealed record MonitorDisassemblyLineDto(
    int Address,
    byte[] Bytes,
    string Text,
    int Length,
    int NextAddress);

public sealed record MonitorDisassemblyResponse(
    RpcStatus Status,
    IReadOnlyList<MonitorDisassemblyLineDto> Lines,
    EmulatorStatusDto? EmulatorStatus) : IRpcResponse;

public sealed record MonitorBreakpointDto(int Address, bool IsEnabled);

public sealed record MonitorBreakpointsResponse(
    RpcStatus Status,
    IReadOnlyList<MonitorBreakpointDto> Breakpoints,
    EmulatorStatusDto? EmulatorStatus) : IRpcResponse;

public sealed record MonitorMemoryResponse(
    RpcStatus Status,
    int Address,
    byte[] Data,
    EmulatorStatusDto? EmulatorStatus) : IRpcResponse;

public sealed record MonitorMemoryWriteResponse(
    RpcStatus Status,
    int Address,
    int BytesWritten,
    EmulatorStatusDto? EmulatorStatus) : IRpcResponse;

public sealed record TickHistoryEntryDto(
    int Index,
    int InstructionAddress,
    int Opcode,
    int A,
    int X,
    int Y,
    int S,
    int P,
    int Pc,
    int WriteCount);

public sealed record GetTickHistoryResponse(
    RpcStatus Status,
    IReadOnlyList<TickHistoryEntryDto> Ticks) : IRpcResponse;

public sealed record ReadMemoryAtTickRequest(string SessionId, int TickIndex, int Address, int Length);

public sealed record ChipStateFieldDto(string Name, int Value, int Width);

public sealed record ChipStateDto(string ChipName, IReadOnlyList<ChipStateFieldDto> Fields);

public sealed record GetChipStateAtTickRequest(string SessionId, int TickIndex);

public sealed record GetChipStateAtTickResponse(
    RpcStatus Status,
    IReadOnlyList<ChipStateDto> Chips) : IRpcResponse;

public static class SnapshotService
{
    public const string ServiceName = "vice_sharp.v1.SnapshotService";
    public const string CaptureSnapshot = "CaptureSnapshot";
    public const string RestoreSnapshot = "RestoreSnapshot";
}

public interface ISnapshotService
{
    ValueTask<CaptureSnapshotResponse> CaptureSnapshotAsync(
        SessionRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<RestoreSnapshotResponse> RestoreSnapshotAsync(
        RestoreSnapshotRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record SnapshotDto(string Format, ulong Cycle, byte[] Payload);

public sealed record RestoreSnapshotRequest(string SessionId, SnapshotDto Snapshot);

public sealed record CaptureSnapshotResponse(
    RpcStatus Status,
    SnapshotDto? Snapshot) : IRpcResponse;

public sealed record RestoreSnapshotResponse(
    RpcStatus Status,
    EmulatorStatusDto? EmulatorStatus) : IRpcResponse;

public static class CaptureService
{
    public const string ServiceName = "vice_sharp.v1.CaptureService";
    public const string GetCaptureCapabilities = "GetCaptureCapabilities";
    public const string StartCapture = "StartCapture";
    public const string StopCapture = "StopCapture";
    public const string CaptureFrame = "CaptureFrame";
    public const string ListCaptures = "ListCaptures";
}

public interface ICaptureService
{
    ValueTask<GetCaptureCapabilitiesResponse> GetCaptureCapabilitiesAsync(
        SessionRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<StartCaptureResponse> StartCaptureAsync(
        StartCaptureRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<StopCaptureResponse> StopCaptureAsync(
        StopCaptureRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<CaptureFrameResponse> CaptureFrameAsync(
        CaptureFrameRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<ListCapturesResponse> ListCapturesAsync(
        SessionRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record CaptureSessionDto(
    string CaptureId,
    CaptureKind Kind,
    string TargetPath,
    bool IsActive);

public sealed record CaptureArtifactDto(string FilePath, string Format, long Cycle);

public sealed record StartCaptureRequest(
    string SessionId,
    CaptureKind Kind,
    string TargetPath,
    string Format = "",
    IReadOnlyDictionary<string, string>? Options = null);

/// <summary>One selectable video-recording driver and the codecs it offers (x64sc parity).</summary>
public sealed record CaptureVideoFormatDto(
    string Id,
    string Container,
    IReadOnlyList<string> VideoCodecs,
    IReadOnlyList<string> AudioCodecs,
    bool RequiresFfmpeg);

public sealed record GetCaptureCapabilitiesResponse(
    RpcStatus Status,
    IReadOnlyList<string> ScreenshotFormats,
    IReadOnlyList<string> AudioFormats,
    IReadOnlyList<CaptureVideoFormatDto> VideoFormats) : IRpcResponse;

public sealed record ListCapturesResponse(
    RpcStatus Status,
    IReadOnlyList<CaptureSessionDto> Captures) : IRpcResponse;

public sealed record StopCaptureRequest(string SessionId, string CaptureId);

public sealed record CaptureFrameRequest(string SessionId, string FilePath, string Format = "png");

public sealed record StartCaptureResponse(
    RpcStatus Status,
    CaptureSessionDto? Capture) : IRpcResponse;

public sealed record StopCaptureResponse(
    RpcStatus Status,
    CaptureSessionDto? Capture) : IRpcResponse;

public sealed record CaptureFrameResponse(
    RpcStatus Status,
    CaptureArtifactDto? Artifact) : IRpcResponse;
