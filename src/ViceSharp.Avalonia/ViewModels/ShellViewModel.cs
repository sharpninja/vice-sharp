using System.Linq;
using ViceSharp.Avalonia.Host;
using ViceSharp.Protocol;

namespace ViceSharp.Avalonia.ViewModels;

/// <summary>
/// FR-UIMENUBAR-001: testable command surface behind the shell's menu bar and
/// transport buttons. Transport commands delegate to the host protocol client;
/// menu actions (warp, swap joysticks, true-drive, attach/detach, navigation)
/// route to the host and/or the <see cref="AttachPanelViewModel"/>. Keeping this
/// off the Window makes the menu wiring unit-testable against a mock host.
/// </summary>
public sealed class ShellViewModel
{
    private readonly IHostProtocolClient _host;

    public ShellViewModel(IHostProtocolClient host, AttachPanelViewModel panel)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(panel);
        _host = host;
        Panel = panel;
    }

    /// <summary>The peripherals/settings panel view-model the shell hosts.</summary>
    public AttachPanelViewModel Panel { get; }

    // ---- Transport (return the host response so the shell can apply status) ---

    public ValueTask<EmulatorCommandResponse> PauseAsync(CancellationToken ct = default) => _host.PauseAsync(ct);

    public ValueTask<EmulatorCommandResponse> ResumeAsync(CancellationToken ct = default) => _host.ResumeAsync(ct);

    public ValueTask<EmulatorCommandResponse> StepCycleAsync(int cycles = 1, CancellationToken ct = default) => _host.StepCycleAsync(cycles, ct);

    public ValueTask<EmulatorCommandResponse> StepFrameAsync(int frames = 1, CancellationToken ct = default) => _host.StepFrameAsync(frames, ct);

    public ValueTask<EmulatorCommandResponse> RewindCycleAsync(int cycles = 1, CancellationToken ct = default) => _host.RewindCycleAsync(cycles, ct);

    public ValueTask<EmulatorCommandResponse> RewindFrameAsync(int frames = 1, CancellationToken ct = default) => _host.RewindFrameAsync(frames, ct);

    public ValueTask<EmulatorCommandResponse> ColdResetAsync(CancellationToken ct = default) => _host.ColdResetAsync(ct);

    public ValueTask<EmulatorCommandResponse> WarmResetAsync(CancellationToken ct = default) => _host.WarmResetAsync(ct);

    public ValueTask<EmulatorCommandResponse> AutostartDrive8Async(CancellationToken ct = default) => _host.ResetAndAutostartDrive8Async(ct);

    /// <summary>Capture the current frame to <paramref name="filePath"/> as a screenshot
    /// ("png" or "bmp"). Wired to Snapshot -&gt; Save screenshot... (x64sc screenshot parity).</summary>
    public ValueTask<CaptureFrameResponse> CaptureScreenshotAsync(string filePath, string format = "png", CancellationToken ct = default)
        => _host.CaptureFrameAsync(filePath, format, ct);

    private string? _soundCaptureId;
    private string? _videoCaptureId;

    /// <summary>True while a WAV sound recording is in progress.</summary>
    public bool IsRecordingSound => _soundCaptureId is not null;

    /// <summary>True while a BMP-sequence video recording is in progress.</summary>
    public bool IsRecordingVideo => _videoCaptureId is not null;

    /// <summary>Start recording the emulator's audio output to <paramref name="filePath"/>
    /// as WAV (x64sc sound-recording parity). Wired to Snapshot -&gt; Record sound...</summary>
    public async ValueTask<RpcStatus> StartSoundRecordingAsync(string filePath, CancellationToken ct = default)
    {
        var response = await _host.StartCaptureAsync(CaptureKind.Audio, filePath, "wav", null, ct).ConfigureAwait(false);
        if (response.Status.IsSuccess && response.Capture is not null)
            _soundCaptureId = response.Capture.CaptureId;
        return response.Status;
    }

    /// <summary>Stop the active sound recording (no-op when none is active).</summary>
    public async ValueTask<RpcStatus> StopSoundRecordingAsync(CancellationToken ct = default)
    {
        var id = _soundCaptureId;
        if (id is null)
            return RpcStatus.Ok();
        var response = await _host.StopCaptureAsync(id, ct).ConfigureAwait(false);
        _soundCaptureId = null;
        return response.Status;
    }

    /// <summary>Start recording video to <paramref name="target"/> in the given
    /// <paramref name="format"/>: a muxed container with sound ("mp4"/"mkv"/"avi", via
    /// ffmpeg; target is a file) or a numbered BMP sequence ("bmpseq"; target is a
    /// directory). <paramref name="options"/> carries format-specific settings such
    /// as the BMP-sequence "frames" selector ("all" | "unique"). Wired to
    /// Snapshot -&gt; Record video...</summary>
    public async ValueTask<RpcStatus> StartVideoRecordingAsync(
        string target,
        string format = "mp4",
        IReadOnlyDictionary<string, string>? options = null,
        CancellationToken ct = default,
        bool captureMicrophone = false,
        string microphoneDevice = "",
        string microphoneInputFormat = "")
    {
        var response = await _host.StartCaptureAsync(
            CaptureKind.Video, target, format, options, ct,
            captureMicrophone, microphoneDevice, microphoneInputFormat).ConfigureAwait(false);
        if (response.Status.IsSuccess && response.Capture is not null)
            _videoCaptureId = response.Capture.CaptureId;
        return response.Status;
    }

    /// <summary>Stop the active video recording (no-op when none is active).</summary>
    public async ValueTask<RpcStatus> StopVideoRecordingAsync(CancellationToken ct = default)
    {
        var id = _videoCaptureId;
        if (id is null)
            return RpcStatus.Ok();
        var response = await _host.StopCaptureAsync(id, ct).ConfigureAwait(false);
        _videoCaptureId = null;
        return response.Status;
    }

    // ---- Menu actions --------------------------------------------------------

    /// <summary>Toggle VICE-style warp (uncapped) mode and apply settings.</summary>
    public Task ToggleWarpAsync(CancellationToken ct = default)
    {
        Panel.IsWarpMode = !Panel.IsWarpMode;
        return Panel.ApplySettingsAsync(restartRequired: false, ct);
    }

    /// <summary>Swap the joystick ports and apply settings.</summary>
    public Task SwapJoysticksAsync(CancellationToken ct = default)
    {
        Panel.SwapJoystickPorts = !Panel.SwapJoystickPorts;
        return Panel.ApplySettingsAsync(restartRequired: false, ct);
    }

    /// <summary>Toggle the per-drive True Drive selection (drives only).</summary>
    public void ToggleTrueDrive(MediaSlot slot)
    {
        var target = FindSlot(slot);
        if (target is { SupportsTrueDrive: true })
            target.TrueDrive = !target.TrueDrive;
    }

    /// <summary>Pick a file and attach it to the given media slot.</summary>
    public Task AttachAsync(MediaSlot slot, CancellationToken ct = default)
    {
        var target = FindSlot(slot);
        return target is null ? Task.CompletedTask : Panel.AttachFromPickerAsync(target, ct);
    }

    /// <summary>Attach and start supported media dropped on the emulator display.</summary>
    public async Task<RpcStatus> DropAndStartFileAsync(string filePath, CancellationToken ct = default)
    {
        var target = FindDropTarget(filePath);
        if (target is null)
        {
            var status = RpcStatus.InvalidArgument($"Unsupported media file: {Path.GetFileName(filePath)}");
            Panel.ReportStatus(status.Message);
            return status;
        }

        await Panel.AttachAsync(target, filePath, ct).ConfigureAwait(true);
        if (!target.IsAttached)
        {
            var reason = string.IsNullOrWhiteSpace(target.ValidationError)
                ? Panel.StatusText
                : target.ValidationError;
            var status = RpcStatus.FailedPrecondition(reason);
            Panel.ReportStatus(status.Message);
            return status;
        }

        RpcStatus startStatus;
        if (target.Slot == MediaSlot.Drive8)
        {
            startStatus = (await _host.ResetAndAutostartDrive8Async(ct).ConfigureAwait(true)).Status;
        }
        else
        {
            startStatus = (await _host.ColdResetAsync(ct).ConfigureAwait(true)).Status;
        }

        Panel.ReportStatus(startStatus.IsSuccess
            ? $"Started {Path.GetFileName(filePath)}"
            : startStatus.Message);
        return startStatus;
    }

    /// <summary>Detach media from the given slot.</summary>
    public Task DetachAsync(MediaSlot slot, CancellationToken ct = default)
    {
        var target = FindSlot(slot);
        return target is null ? Task.CompletedTask : Panel.EjectAsync(target, ct);
    }

    public void ShowSettings()
    {
        Panel.IsPaneOpen = true;
        Panel.ShowSettings();
    }

    public void ShowPeripherals()
    {
        Panel.IsPaneOpen = true;
        Panel.ShowPeripherals();
    }

    public void ShowMonitor()
    {
        Panel.IsPaneOpen = true;
        Panel.ShowMonitor();
    }

    public void ToggleSidebar() => Panel.ToggleSidebar();

    public void ToggleDockSide() => Panel.ToggleDockSide();

    private AttachSlotViewModel? FindSlot(MediaSlot slot)
        => Panel.Slots.FirstOrDefault(candidate => candidate.Slot == slot);

    private AttachSlotViewModel? FindDropTarget(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return null;

        var extension = Path.GetExtension(filePath);
        if (string.IsNullOrWhiteSpace(extension))
            return null;

        var pattern = "*" + extension.ToLowerInvariant();
        return Panel.Slots.FirstOrDefault(slot =>
            slot.FilePatterns.Any(candidate => string.Equals(candidate, pattern, StringComparison.OrdinalIgnoreCase)));
    }
}
