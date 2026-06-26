using ViceSharp.Protocol;

namespace ViceSharp.Avalonia.Host;

public interface IHostProtocolClient
{
    string SessionId { get; }

    /// <summary>Whether the current/next session runs the cycle-accurate
    /// true-drive 1541 (FR-DRVTRUE-001).</summary>
    bool TrueDrive { get; }

    /// <summary>
    /// Select simulated vs emulated (true-drive) for the given IEC drive. Because
    /// true-drive is a machine-config choice, changing it recreates the session
    /// (the next call rebuilds the rig); attached media must be re-attached.
    /// Mirrors VICE's per-unit DriveTrueEmulation requiring a drive reset.
    /// </summary>
    ValueTask SetTrueDriveAsync(bool enabled, int driveDevice = 8, string? diskImagePath = null, CancellationToken cancellationToken = default);

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

    ValueTask<MonitorRegistersResponse> ReadRegistersAsync(CancellationToken cancellationToken = default);

    ValueTask<MonitorMemoryResponse> ReadMemoryAsync(
        int address,
        int length,
        CancellationToken cancellationToken = default);

    ValueTask<MonitorDisassemblyResponse> DisassembleAsync(
        int address,
        int count,
        CancellationToken cancellationToken = default);

    /// <summary>Time-travel debugger: the last captured CPU instructions (ticks).</summary>
    ValueTask<GetTickHistoryResponse> GetTickHistoryAsync(CancellationToken cancellationToken = default);

    /// <summary>Time-travel debugger: a memory window reconstructed as it was at a past tick.</summary>
    ValueTask<MonitorMemoryResponse> ReadMemoryAtTickAsync(
        int tickIndex,
        int address,
        int length,
        CancellationToken cancellationToken = default);

    /// <summary>Time-travel debugger: each chip's decoded full state at a past tick.</summary>
    ValueTask<GetChipStateAtTickResponse> GetChipStateAtTickAsync(
        int tickIndex,
        CancellationToken cancellationToken = default);

    ValueTask<GetVideoFrameResponse> GetFrameAsync(CancellationToken cancellationToken = default);

    /// <summary>Discover the screenshot / sound / video formats the host supports.</summary>
    ValueTask<GetCaptureCapabilitiesResponse> GetCaptureCapabilitiesAsync(CancellationToken cancellationToken = default);

    /// <summary>Capture the current frame to <paramref name="filePath"/> as a screenshot
    /// ("png" default, or "bmp"). Mirrors x64sc's screenshot capability.</summary>
    ValueTask<CaptureFrameResponse> CaptureFrameAsync(
        string filePath,
        string format = "png",
        CancellationToken cancellationToken = default);

    /// <summary>Begin a continuous sound or video recording to <paramref name="targetPath"/>.</summary>
    ValueTask<StartCaptureResponse> StartCaptureAsync(
        CaptureKind kind,
        string targetPath,
        string format = "",
        IReadOnlyDictionary<string, string>? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Stop an active recording started with <see cref="StartCaptureAsync"/>.</summary>
    ValueTask<StopCaptureResponse> StopCaptureAsync(string captureId, CancellationToken cancellationToken = default);

    /// <summary>List the captures active (or recently completed) for the session.</summary>
    ValueTask<ListCapturesResponse> ListCapturesAsync(CancellationToken cancellationToken = default);
}
