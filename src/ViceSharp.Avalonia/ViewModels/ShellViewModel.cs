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
}
