using ViceSharp.Protocol;

namespace ViceSharp.Avalonia.Host;

public sealed class DisconnectedHostProtocolClient : IHostProtocolClient
{
    private readonly RpcStatus _disconnectedStatus;

    public DisconnectedHostProtocolClient(string message = "No emulator host is connected.")
    {
        _disconnectedStatus = RpcStatus.Unavailable(message);
    }

    public string SessionId => string.Empty;

    public bool TrueDrive => false;

    public ValueTask SetTrueDriveAsync(bool enabled, int driveDevice = 8, string? diskImagePath = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.CompletedTask;
    }

    public ValueTask<GetEmulatorStatusResponse> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(new GetEmulatorStatusResponse(_disconnectedStatus, null));
    }

    public ValueTask<EmulatorCommandResponse> StartAsync(CancellationToken cancellationToken = default) => CommandAsync(cancellationToken);

    public ValueTask<EmulatorCommandResponse> PauseAsync(CancellationToken cancellationToken = default) => CommandAsync(cancellationToken);

    public ValueTask<EmulatorCommandResponse> ResumeAsync(CancellationToken cancellationToken = default) => CommandAsync(cancellationToken);

    public ValueTask<EmulatorCommandResponse> StepCycleAsync(int cycleCount, CancellationToken cancellationToken = default) => CommandAsync(cancellationToken);

    public ValueTask<EmulatorCommandResponse> StepFrameAsync(int frameCount, CancellationToken cancellationToken = default) => CommandAsync(cancellationToken);

    public ValueTask<EmulatorCommandResponse> RewindCycleAsync(int cycleCount, CancellationToken cancellationToken = default) => CommandAsync(cancellationToken);

    public ValueTask<EmulatorCommandResponse> RewindFrameAsync(int frameCount, CancellationToken cancellationToken = default) => CommandAsync(cancellationToken);

    public ValueTask<EmulatorCommandResponse> ColdResetAsync(CancellationToken cancellationToken = default) => CommandAsync(cancellationToken);

    public ValueTask<EmulatorCommandResponse> WarmResetAsync(CancellationToken cancellationToken = default) => CommandAsync(cancellationToken);

    public ValueTask<EmulatorCommandResponse> ResetAndAutostartDrive8Async(CancellationToken cancellationToken = default) => CommandAsync(cancellationToken);

    public ValueTask<EmulatorCommandResponse> SetLimiterRateAsync(double ratePercent, CancellationToken cancellationToken = default) => CommandAsync(cancellationToken);

    public ValueTask<ListSettingsProfilesResponse> ListSettingsProfilesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(new ListSettingsProfilesResponse(_disconnectedStatus, Array.Empty<SettingsProfileDto>()));
    }

    public ValueTask<GetSettingsResponse> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(new GetSettingsResponse(_disconnectedStatus, null));
    }

    public ValueTask<UpdateSettingsResponse> UpdateSettingsAsync(
        UpdateSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(new UpdateSettingsResponse(_disconnectedStatus, null, Array.Empty<SettingApplyDiagnosticDto>()));
    }

    public ValueTask<ValidateSettingsResourcesResponse> ValidateSettingsResourcesAsync(
        ValidateSettingsResourcesRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(new ValidateSettingsResourcesResponse(_disconnectedStatus, Array.Empty<SettingsResourceValidationDto>()));
    }

    public ValueTask<ListMediaResponse> ListMediaAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(new ListMediaResponse(_disconnectedStatus, Array.Empty<MediaAttachmentDto>()));
    }

    public ValueTask<AttachMediaResponse> AttachMediaAsync(
        MediaSlot slot,
        string filePath,
        bool isReadOnly,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(new AttachMediaResponse(_disconnectedStatus, null));
    }

    public ValueTask<AttachMediaResponse> AttachMediaAsync(
        MediaSlot slot,
        string filePath,
        bool isReadOnly,
        byte[] payload,
        string displayName,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(new AttachMediaResponse(_disconnectedStatus, null));
    }

    public ValueTask<DetachMediaResponse> DetachMediaAsync(
        MediaSlot slot,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(new DetachMediaResponse(_disconnectedStatus, null));
    }

    public ValueTask<InputCommandResponse> SetKeyStateAsync(
        string key,
        bool isPressed,
        string physicalKey = "",
        string text = "",
        int modifiers = 0,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(new InputCommandResponse(_disconnectedStatus, null));
    }

    public ValueTask<ListKeyboardMapsResponse> ListKeyboardMapsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(new ListKeyboardMapsResponse(_disconnectedStatus, Array.Empty<KeyboardMapDto>()));
    }

    public ValueTask<KeyboardMapResponse> SetKeyboardMapAsync(
        string keyboardMapId,
        byte[]? payload = null,
        string displayName = "",
        string sourcePath = "",
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(new KeyboardMapResponse(_disconnectedStatus, null));
    }

    public ValueTask<MonitorCommandResponse> ExecuteMonitorCommandAsync(
        string command,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(new MonitorCommandResponse(_disconnectedStatus, string.Empty, null));
    }

    public ValueTask<MonitorRegistersResponse> ReadRegistersAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(new MonitorRegistersResponse(_disconnectedStatus, null, null));
    }

    public ValueTask<MonitorMemoryResponse> ReadMemoryAsync(int address, int length, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(new MonitorMemoryResponse(_disconnectedStatus, address, Array.Empty<byte>(), null));
    }

    public ValueTask<MonitorDisassemblyResponse> DisassembleAsync(int address, int count, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(new MonitorDisassemblyResponse(_disconnectedStatus, Array.Empty<MonitorDisassemblyLineDto>(), null));
    }

    public ValueTask<GetTickHistoryResponse> GetTickHistoryAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(new GetTickHistoryResponse(_disconnectedStatus, Array.Empty<TickHistoryEntryDto>()));
    }

    public ValueTask<MonitorMemoryResponse> ReadMemoryAtTickAsync(int tickIndex, int address, int length, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(new MonitorMemoryResponse(_disconnectedStatus, address, Array.Empty<byte>(), null));
    }

    public ValueTask<GetChipStateAtTickResponse> GetChipStateAtTickAsync(int tickIndex, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(new GetChipStateAtTickResponse(_disconnectedStatus, Array.Empty<ChipStateDto>()));
    }

    public ValueTask<GetVideoFrameResponse> GetFrameAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(new GetVideoFrameResponse(_disconnectedStatus, null));
    }

    public ValueTask<CaptureFrameResponse> CaptureFrameAsync(string filePath, string format = "png", CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(new CaptureFrameResponse(_disconnectedStatus, null));
    }

    public ValueTask<GetCaptureCapabilitiesResponse> GetCaptureCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(new GetCaptureCapabilitiesResponse(
            _disconnectedStatus, Array.Empty<string>(), Array.Empty<string>(), Array.Empty<CaptureVideoFormatDto>()));
    }

    public ValueTask<StartCaptureResponse> StartCaptureAsync(
        CaptureKind kind,
        string targetPath,
        string format = "",
        IReadOnlyDictionary<string, string>? options = null,
        CancellationToken cancellationToken = default,
        bool captureMicrophone = false,
        string microphoneDevice = "",
        string microphoneInputFormat = "")
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(new StartCaptureResponse(_disconnectedStatus, null));
    }

    public ValueTask<StopCaptureResponse> StopCaptureAsync(string captureId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(new StopCaptureResponse(_disconnectedStatus, null));
    }

    public ValueTask<ListCapturesResponse> ListCapturesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(new ListCapturesResponse(_disconnectedStatus, Array.Empty<CaptureSessionDto>()));
    }

    private ValueTask<EmulatorCommandResponse> CommandAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(new EmulatorCommandResponse(_disconnectedStatus, null));
    }
}
