using System.Collections.ObjectModel;
using ViceSharp.Avalonia.Host;
using ViceSharp.Protocol;

namespace ViceSharp.Avalonia.ViewModels;

public sealed class AttachPanelViewModel : ObservableObject
{
    private readonly IHostProtocolClient _hostClient;
    private AttachDockSide _dockSide = AttachDockSide.Left;
    private SidebarTab _activeTab = SidebarTab.Peripherals;
    private string _statusText = "Disconnected";
    private KeyboardMapDto? _selectedKeyboardMap;
    private string _keyboardMapStatus = "No keyboard map selected.";
    private double _limiterRatePercent = 100;
    private string _monitorOutput = "Monitor ready.";
    private string _monitorCommand = "r";

    public AttachPanelViewModel(IHostProtocolClient hostClient)
    {
        ArgumentNullException.ThrowIfNull(hostClient);
        _hostClient = hostClient;

        Slots =
        [
            new AttachSlotViewModel(MediaSlot.Drive8, "Drive 8", "Disk", ["*.d64", "*.g64"]),
            new AttachSlotViewModel(MediaSlot.Drive9, "Drive 9", "Disk", ["*.d64", "*.g64"]),
            new AttachSlotViewModel(MediaSlot.Tape, "Tape", "Tape", ["*.tap"]),
            new AttachSlotViewModel(MediaSlot.Cartridge, "Cartridge", "Cart", ["*.crt", "*.bin", "*.rom"])
        ];
    }

    public ObservableCollection<AttachSlotViewModel> Slots { get; }

    public ObservableCollection<KeyboardMapDto> KeyboardMaps { get; } = new();

    public AttachDockSide DockSide
    {
        get => _dockSide;
        private set => SetProperty(ref _dockSide, value);
    }

    public SidebarTab ActiveTab
    {
        get => _activeTab;
        set => SetProperty(ref _activeTab, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public KeyboardMapDto? SelectedKeyboardMap
    {
        get => _selectedKeyboardMap;
        set
        {
            if (SetProperty(ref _selectedKeyboardMap, value) && value is not null)
                _ = SelectKeyboardMapAsync(value.Id);
        }
    }

    public string KeyboardMapStatus
    {
        get => _keyboardMapStatus;
        private set => SetProperty(ref _keyboardMapStatus, value);
    }

    public double LimiterRatePercent
    {
        get => _limiterRatePercent;
        set => SetProperty(ref _limiterRatePercent, Math.Clamp(value, 10, 400));
    }

    public string MonitorOutput
    {
        get => _monitorOutput;
        private set => SetProperty(ref _monitorOutput, value);
    }

    public string MonitorCommand
    {
        get => _monitorCommand;
        set => SetProperty(ref _monitorCommand, value);
    }

    public void DockLeft() => DockSide = AttachDockSide.Left;

    public void DockRight() => DockSide = AttachDockSide.Right;

    public void ShowPeripherals() => ActiveTab = SidebarTab.Peripherals;

    public void ShowSettings() => ActiveTab = SidebarTab.Settings;

    public void ShowMonitor() => ActiveTab = SidebarTab.Monitor;

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var response = await _hostClient.ListMediaAsync(cancellationToken);
        if (!response.Status.IsSuccess)
        {
            StatusText = response.Status.Message;
            return;
        }

        foreach (var slot in Slots)
            slot.MarkEmpty();

        foreach (var attachment in response.Attachments)
        {
            var slot = Slots.FirstOrDefault(candidate => candidate.Slot == attachment.Slot);
            slot?.ApplyAttachment(attachment);
        }

        await RefreshKeyboardMapsAsync(cancellationToken).ConfigureAwait(true);

        StatusText = "Connected";
    }

    public async Task AttachAsync(AttachSlotViewModel slot, string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(slot);

        if (string.IsNullOrWhiteSpace(filePath))
        {
            slot.MarkError("No file selected.");
            return;
        }

        var response = await _hostClient.AttachMediaAsync(slot.Slot, filePath, slot.IsReadOnly, cancellationToken)
            .ConfigureAwait(true);

        if (!response.Status.IsSuccess)
        {
            slot.MarkError(response.Status.Message);
            StatusText = response.Status.Message;
            return;
        }

        if (response.Attachment is not null)
            slot.ApplyAttachment(response.Attachment);

        StatusText = "Attached";
    }

    public async Task EjectAsync(AttachSlotViewModel slot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(slot);

        var response = await _hostClient.DetachMediaAsync(slot.Slot, cancellationToken);
        if (!response.Status.IsSuccess)
        {
            slot.MarkError(response.Status.Message);
            StatusText = response.Status.Message;
            return;
        }

        slot.MarkEmpty();
        StatusText = "Ejected";
    }

    public async Task RefreshKeyboardMapsAsync(CancellationToken cancellationToken = default)
    {
        var response = await _hostClient.ListKeyboardMapsAsync(cancellationToken).ConfigureAwait(true);
        if (!response.Status.IsSuccess)
        {
            KeyboardMapStatus = response.Status.Message;
            return;
        }

        KeyboardMaps.Clear();
        foreach (var map in response.KeyboardMaps)
            KeyboardMaps.Add(map);

        var selected = response.KeyboardMaps.FirstOrDefault(map => map.IsSelected) ?? response.KeyboardMaps.FirstOrDefault();
        if (selected is not null)
        {
            _selectedKeyboardMap = selected;
            OnPropertyChanged(nameof(SelectedKeyboardMap));
            KeyboardMapStatus = string.IsNullOrWhiteSpace(selected.Error)
                ? $"Using {selected.DisplayName}"
                : selected.Error;
        }
    }

    public async Task SelectKeyboardMapAsync(string keyboardMapId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(keyboardMapId))
            return;

        var response = await _hostClient.SetKeyboardMapAsync(keyboardMapId, cancellationToken: cancellationToken).ConfigureAwait(true);
        ApplyKeyboardMapResponse(response);
    }

    public async Task SelectCustomKeyboardMapAsync(
        string filePath,
        byte[] payload,
        CancellationToken cancellationToken = default)
    {
        var response = await _hostClient.SetKeyboardMapAsync(
            $"custom:{Path.GetFileNameWithoutExtension(filePath)}",
            payload,
            Path.GetFileName(filePath),
            filePath,
            cancellationToken).ConfigureAwait(true);
        ApplyKeyboardMapResponse(response);
    }

    public async Task ApplyLimiterAsync(CancellationToken cancellationToken = default)
    {
        var response = await _hostClient.SetLimiterRateAsync(LimiterRatePercent, cancellationToken).ConfigureAwait(true);
        StatusText = response.Status.IsSuccess ? "Limiter updated" : response.Status.Message;
    }

    public async Task ExecuteMonitorCommandAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(MonitorCommand))
            return;

        var command = MonitorCommand;
        var response = await _hostClient.ExecuteMonitorCommandAsync(command, cancellationToken).ConfigureAwait(true);
        var prompt = $"> {command}";
        var output = response.Status.IsSuccess ? response.Output : response.Status.Message;
        MonitorOutput = string.IsNullOrWhiteSpace(MonitorOutput)
            ? $"{prompt}{Environment.NewLine}{output}"
            : $"{MonitorOutput}{Environment.NewLine}{prompt}{Environment.NewLine}{output}";
    }

    private void ApplyKeyboardMapResponse(KeyboardMapResponse response)
    {
        if (!response.Status.IsSuccess)
        {
            KeyboardMapStatus = response.Status.Message;
            return;
        }

        if (response.KeyboardMap is not null)
        {
            _selectedKeyboardMap = response.KeyboardMap;
            OnPropertyChanged(nameof(SelectedKeyboardMap));
            KeyboardMapStatus = string.IsNullOrWhiteSpace(response.KeyboardMap.Error)
                ? $"Using {response.KeyboardMap.DisplayName}"
                : response.KeyboardMap.Error;
        }
    }
}

public enum SidebarTab
{
    Peripherals,
    Settings,
    Monitor
}
