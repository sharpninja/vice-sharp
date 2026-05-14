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

    public ValueTask<GetVideoFrameResponse> GetFrameAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(new GetVideoFrameResponse(_disconnectedStatus, null));
    }

    private ValueTask<EmulatorCommandResponse> CommandAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(new EmulatorCommandResponse(_disconnectedStatus, null));
    }
}
