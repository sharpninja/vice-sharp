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
    Keyboard = 2
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

public sealed record MachineStateDto(byte A, byte X, byte Y, byte S, byte P, ushort Pc, long Cycle);

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
    string ModelId = "")
{
    public double MeasuredFramesPerSecond => MeasuredFps;
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

public sealed record CreateEmulatorSessionRequest(string ArchitectureId = "minimal", string DisplayName = "");

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

public sealed record KeyStateDto(string Key, bool IsPressed, bool AppliedToRuntime);

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
}

public interface IMonitorService
{
    ValueTask<MonitorCommandResponse> ExecuteCommandAsync(
        ExecuteMonitorCommandRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record ExecuteMonitorCommandRequest(string SessionId, string Command);

public sealed record MonitorCommandResponse(
    RpcStatus Status,
    string Output,
    EmulatorStatusDto? EmulatorStatus) : IRpcResponse;

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
    public const string StartCapture = "StartCapture";
    public const string StopCapture = "StopCapture";
    public const string CaptureFrame = "CaptureFrame";
}

public interface ICaptureService
{
    ValueTask<StartCaptureResponse> StartCaptureAsync(
        StartCaptureRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<StopCaptureResponse> StopCaptureAsync(
        StopCaptureRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<CaptureFrameResponse> CaptureFrameAsync(
        CaptureFrameRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record CaptureSessionDto(
    string CaptureId,
    CaptureKind Kind,
    string TargetPath,
    bool IsActive);

public sealed record CaptureArtifactDto(string FilePath, string Format, long Cycle);

public sealed record StartCaptureRequest(string SessionId, CaptureKind Kind, string TargetPath);

public sealed record StopCaptureRequest(string SessionId, string CaptureId);

public sealed record CaptureFrameRequest(string SessionId, string FilePath);

public sealed record StartCaptureResponse(
    RpcStatus Status,
    CaptureSessionDto? Capture) : IRpcResponse;

public sealed record StopCaptureResponse(
    RpcStatus Status,
    CaptureSessionDto? Capture) : IRpcResponse;

public sealed record CaptureFrameResponse(
    RpcStatus Status,
    CaptureArtifactDto? Artifact) : IRpcResponse;
