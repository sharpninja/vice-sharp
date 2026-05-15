using ViceSharp.Protocol;

namespace ViceSharp.Avalonia.Host;

public interface IHostProtocolClient
{
    string SessionId { get; }

    ValueTask<GetEmulatorStatusResponse> GetStatusAsync(CancellationToken cancellationToken = default);

    ValueTask<EmulatorCommandResponse> StartAsync(CancellationToken cancellationToken = default);

    ValueTask<EmulatorCommandResponse> PauseAsync(CancellationToken cancellationToken = default);

    ValueTask<EmulatorCommandResponse> ResumeAsync(CancellationToken cancellationToken = default);

    ValueTask<EmulatorCommandResponse> StepCycleAsync(int cycleCount, CancellationToken cancellationToken = default);

    ValueTask<EmulatorCommandResponse> StepFrameAsync(int frameCount, CancellationToken cancellationToken = default);

    ValueTask<EmulatorCommandResponse> RewindCycleAsync(int cycleCount, CancellationToken cancellationToken = default);

    ValueTask<EmulatorCommandResponse> RewindFrameAsync(int frameCount, CancellationToken cancellationToken = default);

    ValueTask<EmulatorCommandResponse> ColdResetAsync(CancellationToken cancellationToken = default);

    ValueTask<EmulatorCommandResponse> WarmResetAsync(CancellationToken cancellationToken = default);

    ValueTask<EmulatorCommandResponse> ResetAndAutostartDrive8Async(CancellationToken cancellationToken = default);

    ValueTask<EmulatorCommandResponse> SetLimiterRateAsync(double ratePercent, CancellationToken cancellationToken = default);

    ValueTask<ListSettingsProfilesResponse> ListSettingsProfilesAsync(CancellationToken cancellationToken = default);

    ValueTask<GetSettingsResponse> GetSettingsAsync(CancellationToken cancellationToken = default);

    ValueTask<UpdateSettingsResponse> UpdateSettingsAsync(
        UpdateSettingsRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<ValidateSettingsResourcesResponse> ValidateSettingsResourcesAsync(
        ValidateSettingsResourcesRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<ListMediaResponse> ListMediaAsync(CancellationToken cancellationToken = default);

    ValueTask<AttachMediaResponse> AttachMediaAsync(
        MediaSlot slot,
        string filePath,
        bool isReadOnly,
        CancellationToken cancellationToken = default);

    ValueTask<AttachMediaResponse> AttachMediaAsync(
        MediaSlot slot,
        string filePath,
        bool isReadOnly,
        byte[] payload,
        string displayName,
        CancellationToken cancellationToken = default);

    ValueTask<DetachMediaResponse> DetachMediaAsync(
        MediaSlot slot,
        CancellationToken cancellationToken = default);

    ValueTask<InputCommandResponse> SetKeyStateAsync(
        string key,
        bool isPressed,
        string physicalKey = "",
        string text = "",
        int modifiers = 0,
        CancellationToken cancellationToken = default);

    ValueTask<ListKeyboardMapsResponse> ListKeyboardMapsAsync(CancellationToken cancellationToken = default);

    ValueTask<KeyboardMapResponse> SetKeyboardMapAsync(
        string keyboardMapId,
        byte[]? payload = null,
        string displayName = "",
        string sourcePath = "",
        CancellationToken cancellationToken = default);

    ValueTask<MonitorCommandResponse> ExecuteMonitorCommandAsync(
        string command,
        CancellationToken cancellationToken = default);

    ValueTask<GetVideoFrameResponse> GetFrameAsync(CancellationToken cancellationToken = default);
}
