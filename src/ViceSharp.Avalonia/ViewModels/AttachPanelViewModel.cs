using System.Collections.ObjectModel;
using System.ComponentModel;
using ViceSharp.Avalonia.Host;
using ViceSharp.Protocol;

namespace ViceSharp.Avalonia.ViewModels;

public sealed class AttachPanelViewModel : ObservableObject
{
    public const double LimiterMinimumPercent = 1;
    public const double LimiterMaximumPercent = 1000;

    private readonly IHostProtocolClient _hostClient;
    private AttachDockSide _dockSide = AttachDockSide.Left;
    private bool _isPaneOpen = true;
    private bool _applyingTrueDrive;
    private SidebarTab _activeTab = SidebarTab.Peripherals;
    private string _statusText = "Disconnected";
    private KeyboardMapDto? _selectedKeyboardMap;
    private string _keyboardMapStatus = "No keyboard map selected.";
    private double _limiterRatePercent = 100;
    private bool _limiterEnabled = true;
    private MachineProfileOption _selectedMachineProfile;
    private string _selectedRenderer = "Host direct";
    private string _selectedDisplayScale = "2x";
    private string _selectedCropMode = "Visible area";
    private string _selectedAspectMode = "VICE pixel aspect";
    private string _selectedPalette = "VICE default";
    private string _selectedAudioMode = "Enabled";
    private string _selectedInputMode = "Keyboard + joystick";
    private string _selectedPrimaryJoystickPort = "Joystick 2";
    private bool _swapJoystickPorts;
    private string _selectedResourceMode = "Auto detect";
    private string _settingsStatusText = "Settings will load from the connected host.";
    private bool _hasPendingSettingsChanges;
    private bool _requiresRestart;
    private SettingsSnapshot _appliedSettings;
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

        MachineProfiles =
        [
            new MachineProfileOption("c64", "C64 PAL", "Placeholder until host profile enumeration is available.", true),
            new MachineProfileOption("ntsc", "C64 NTSC", "Placeholder until host profile enumeration is available.", true),
            new MachineProfileOption("c64c", "C64C PAL", "Placeholder until host profile enumeration is available.", true)
        ];
        _selectedMachineProfile = MachineProfiles[0];
        _appliedSettings = CaptureSettings();

        // FR-DRVTRUE-001: when a drive's True Drive toggle flips (from the card
        // checkbox or the menu), drive the host to (re)build the session as a
        // true-drive rig. Only one true-drive rig is supported at a time, so
        // enabling one drive disables the other.
        foreach (var slot in Slots)
        {
            if (slot.SupportsTrueDrive)
                slot.PropertyChanged += OnSlotPropertyChanged;
        }
    }

    /// <summary>
    /// Host-supplied file picker used by <see cref="AttachFromPickerAsync"/> so a
    /// reusable peripheral card can request an attach without owning the dialog.
    /// Set by the shell (the Avalonia window provides the StorageProvider).
    /// </summary>
    public Func<AttachSlotViewModel, Task<string?>>? FilePicker { get; set; }

    public ObservableCollection<AttachSlotViewModel> Slots { get; }

    public ObservableCollection<KeyboardMapDto> KeyboardMaps { get; } = new();

    public ObservableCollection<MachineProfileOption> MachineProfiles { get; }

    public ObservableCollection<SettingsResourceValidationDto> SettingsValidationResults { get; } = new();

    public IReadOnlyList<string> RendererModes { get; } = ["Host direct", "Software"];

    public IReadOnlyList<string> DisplayScales { get; } = ["1x", "2x", "3x", "Fit window"];

    public IReadOnlyList<string> CropModes { get; } = ["Full frame", "Visible area", "Borderless"];

    public IReadOnlyList<string> AspectModes { get; } = ["Square pixels", "VICE pixel aspect", "Force 4:3"];

    public IReadOnlyList<string> PaletteModes { get; } = ["VICE default", "Pepto", "Monochrome green", "Amber"];

    public IReadOnlyList<string> AudioModes { get; } = ["Enabled", "Muted", "Unavailable"];

    public IReadOnlyList<string> InputModes { get; } = ["Keyboard + joystick", "Keyboard only", "Disabled"];

    public IReadOnlyList<string> PrimaryJoystickPorts { get; } = ["Joystick 2", "Joystick 1"];

    public IReadOnlyList<string> ResourceModes { get; } = ["Auto detect", "Use configured paths", "Missing resources"];

    public AttachDockSide DockSide
    {
        get => _dockSide;
        private set
        {
            if (SetProperty(ref _dockSide, value))
                OnPropertyChanged(nameof(PanePlacement));
        }
    }

    /// <summary>
    /// Avalonia <c>SplitView.PanePlacement</c> value derived from
    /// <see cref="DockSide"/>: Left maps to the left edge, Right to the right.
    /// Lets the AXAML shell bind the flyout side without a converter.
    /// </summary>
    public global::Avalonia.Controls.SplitViewPanePlacement PanePlacement =>
        _dockSide == AttachDockSide.Left
            ? global::Avalonia.Controls.SplitViewPanePlacement.Left
            : global::Avalonia.Controls.SplitViewPanePlacement.Right;

    /// <summary>
    /// Whether the sidebar flyout pane is open. Bound to
    /// <c>SplitView.IsPaneOpen</c>; the single toggle button flips it.
    /// </summary>
    public bool IsPaneOpen
    {
        get => _isPaneOpen;
        set => SetProperty(ref _isPaneOpen, value);
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
        set => SetSettingsProperty(ref _limiterRatePercent, Math.Clamp(value, LimiterMinimumPercent, LimiterMaximumPercent));
    }

    public bool LimiterEnabled
    {
        get => _limiterEnabled;
        set => SetSettingsProperty(ref _limiterEnabled, value);
    }

    /// <summary>
    /// When true, the speed limiter is disabled and the emulator runs
    /// as fast as the host allows (equivalent to VICE warp mode).
    /// This is the recommended mode for profiling with dotTrace.
    /// </summary>
    public bool IsWarpMode
    {
        get => !LimiterEnabled;
        set
        {
            if (IsWarpMode != value)
            {
                LimiterEnabled = !value;           // will raise PropertyChanged for LimiterEnabled + HasPending...
                OnPropertyChanged();               // raise for "IsWarpMode" itself
            }
        }
    }

    public MachineProfileOption SelectedMachineProfile
    {
        get => _selectedMachineProfile;
        set => SetSettingsProperty(ref _selectedMachineProfile, value);
    }

    public string SelectedRenderer
    {
        get => _selectedRenderer;
        set => SetSettingsProperty(ref _selectedRenderer, value);
    }

    public string SelectedDisplayScale
    {
        get => _selectedDisplayScale;
        set => SetSettingsProperty(ref _selectedDisplayScale, value);
    }

    public string SelectedCropMode
    {
        get => _selectedCropMode;
        set => SetSettingsProperty(ref _selectedCropMode, value);
    }

    public string SelectedAspectMode
    {
        get => _selectedAspectMode;
        set => SetSettingsProperty(ref _selectedAspectMode, value);
    }

    public string SelectedPalette
    {
        get => _selectedPalette;
        set => SetSettingsProperty(ref _selectedPalette, value);
    }

    public string SelectedAudioMode
    {
        get => _selectedAudioMode;
        set => SetSettingsProperty(ref _selectedAudioMode, value);
    }

    public string SelectedInputMode
    {
        get => _selectedInputMode;
        set => SetSettingsProperty(ref _selectedInputMode, value);
    }

    public string SelectedPrimaryJoystickPort
    {
        get => _selectedPrimaryJoystickPort;
        set => SetSettingsProperty(ref _selectedPrimaryJoystickPort, value);
    }

    public bool SwapJoystickPorts
    {
        get => _swapJoystickPorts;
        set => SetSettingsProperty(ref _swapJoystickPorts, value);
    }

    public string SelectedResourceMode
    {
        get => _selectedResourceMode;
        set => SetSettingsProperty(ref _selectedResourceMode, value);
    }

    public string SettingsStatusText
    {
        get => _settingsStatusText;
        private set => SetProperty(ref _settingsStatusText, value);
    }

    public bool HasPendingSettingsChanges
    {
        get => _hasPendingSettingsChanges;
        private set => SetProperty(ref _hasPendingSettingsChanges, value);
    }

    public bool RequiresRestart
    {
        get => _requiresRestart;
        private set => SetProperty(ref _requiresRestart, value);
    }

    public bool HasSettingsValidationResults => SettingsValidationResults.Count > 0;

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

    /// <summary>Flip the sidebar flyout between the left and right edges.
    /// Backs the single side-toggle icon button (FR-UIFLYOUT-001).</summary>
    public void ToggleDockSide() =>
        DockSide = _dockSide == AttachDockSide.Left ? AttachDockSide.Right : AttachDockSide.Left;

    /// <summary>Open or close the sidebar flyout pane (FR-UIFLYOUT-001).</summary>
    public void ToggleSidebar() => IsPaneOpen = !IsPaneOpen;

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
        await RefreshSettingsAsync(cancellationToken).ConfigureAwait(true);

        StatusText = "Connected";
    }

    public void ApplyStatus(EmulatorStatusDto status)
    {
        ArgumentNullException.ThrowIfNull(status);

        foreach (var slot in Slots)
            slot.SetIecActivity(status.IecBusActive);
    }

    public async Task AttachAsync(AttachSlotViewModel slot, string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(slot);

        if (string.IsNullOrWhiteSpace(filePath))
        {
            slot.MarkError("No file selected.");
            return;
        }

        byte[]? payload = null;
        if (File.Exists(filePath))
            payload = await File.ReadAllBytesAsync(filePath, cancellationToken).ConfigureAwait(true);

        var response = payload is { Length: > 0 }
            ? await _hostClient.AttachMediaAsync(
                slot.Slot,
                filePath,
                slot.IsReadOnly,
                payload,
                Path.GetFileName(filePath),
                cancellationToken).ConfigureAwait(true)
            : await _hostClient.AttachMediaAsync(slot.Slot, filePath, slot.IsReadOnly, cancellationToken)
                .ConfigureAwait(true);

        if (!response.Status.IsSuccess)
        {
            slot.MarkError(response.Status.Message);
            StatusText = response.Status.Message;
            return;
        }

        if (response.Attachment is not null)
            slot.ApplyAttachment(response.Attachment, filePath);

        StatusText = "Attached";
    }

    /// <summary>
    /// Show the host file picker for <paramref name="slot"/> and attach the
    /// chosen file. Backs the reusable peripheral card's Attach button
    /// (FR-UIPERIPHERAL-001). No-op with an inline error if no picker is set.
    /// </summary>
    public async Task AttachFromPickerAsync(AttachSlotViewModel slot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(slot);

        if (FilePicker is null)
        {
            slot.MarkError("File picker is unavailable.");
            return;
        }

        var filePath = await FilePicker(slot).ConfigureAwait(true);
        if (!string.IsNullOrWhiteSpace(filePath))
            await AttachAsync(slot, filePath, cancellationToken).ConfigureAwait(true);
    }

    private async void OnSlotPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(AttachSlotViewModel.TrueDrive) || sender is not AttachSlotViewModel changed)
            return;
        if (_applyingTrueDrive)
            return;

        _applyingTrueDrive = true;
        try
        {
            // Single true-drive rig: enabling one drive disables the other.
            if (changed.TrueDrive)
            {
                foreach (var other in Slots)
                {
                    if (other.SupportsTrueDrive && !ReferenceEquals(other, changed) && other.TrueDrive)
                        other.TrueDrive = false;
                }
            }

            var active = Slots.FirstOrDefault(slot => slot.SupportsTrueDrive && slot.TrueDrive);
            var device = active?.Slot == MediaSlot.Drive9 ? 9 : 8;
            await _hostClient.SetTrueDriveAsync(active is not null, device).ConfigureAwait(true);
            StatusText = active is not null
                ? $"True Drive enabled for {active.Title} (session restarted)"
                : "True Drive disabled (session restarted)";
        }
        catch (Exception ex)
        {
            StatusText = ex.Message;
        }
        finally
        {
            _applyingTrueDrive = false;
        }
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

    public async Task ApplySettingsAsync(bool restartRequired, CancellationToken cancellationToken = default)
    {
        var response = await _hostClient.UpdateSettingsAsync(CreateUpdateSettingsRequest(restartRequired), cancellationToken).ConfigureAwait(true);
        if (!response.Status.IsSuccess)
        {
            StatusText = response.Status.Message;
            SettingsStatusText = response.Status.Message;
            return;
        }

        if (response.Settings is not null)
            ApplySettingsFromHost(response.Settings);

        _appliedSettings = CaptureSettings();
        HasPendingSettingsChanges = false;
        RequiresRestart = response.Diagnostics.Any(diagnostic => diagnostic.RestartRequired);
        StatusText = RequiresRestart ? "Settings applied; restart required" : "Settings applied";
        SettingsStatusText = CreateSettingsStatus(response.Diagnostics);
    }

    public async Task ValidateSettingsAsync(CancellationToken cancellationToken = default)
    {
        var response = await _hostClient.ValidateSettingsResourcesAsync(CreateValidateSettingsRequest(), cancellationToken)
            .ConfigureAwait(true);
        if (!response.Status.IsSuccess)
        {
            StatusText = response.Status.Message;
            SettingsStatusText = response.Status.Message;
            return;
        }

        ReplaceSettingsValidationResults(response.Resources);

        var invalidCount = response.Resources.Count(resource => !resource.IsValid);
        var restartCount = response.Resources.Count(resource => resource.RestartRequired);
        SettingsStatusText = invalidCount == 0
            ? restartCount == 0
                ? "Settings validation passed."
                : $"Settings validation passed; {restartCount} setting(s) may require restart."
            : $"Settings validation found {invalidCount} issue(s).";
        StatusText = invalidCount == 0 ? "Settings validated" : "Settings validation failed";
    }

    public void RevertSettings()
    {
        RestoreSettings(_appliedSettings);
        HasPendingSettingsChanges = false;
        RequiresRestart = false;
        ReplaceSettingsValidationResults([]);
        SettingsStatusText = "Reverted to the last applied local settings.";
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

    private bool SetSettingsProperty<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
    {
        if (!SetProperty(ref field, value, propertyName))
            return false;

        HasPendingSettingsChanges = !CaptureSettings().Equals(_appliedSettings);
        RequiresRestart = HasPendingSettingsChanges && IsRestartRelevant(propertyName);
        if (SettingsValidationResults.Count > 0)
            ReplaceSettingsValidationResults([]);
        SettingsStatusText = HasPendingSettingsChanges
            ? "Settings changed locally. Apply sends supported settings through the host protocol."
            : "Settings match the last applied local state.";
        return true;
    }

    private async Task RefreshSettingsAsync(CancellationToken cancellationToken)
    {
        var profiles = await _hostClient.ListSettingsProfilesAsync(cancellationToken).ConfigureAwait(true);
        if (profiles.Status.IsSuccess && profiles.Profiles.Count > 0)
        {
            MachineProfiles.Clear();
            foreach (var profile in profiles.Profiles)
            {
                MachineProfiles.Add(new MachineProfileOption(
                    profile.Id,
                    profile.DisplayName,
                    string.IsNullOrWhiteSpace(profile.Description) ? profile.Machine : profile.Description,
                    false));
            }

            var selectedProfile = profiles.Profiles.FirstOrDefault(profile => profile.IsCurrent) ?? profiles.Profiles[0];
            _selectedMachineProfile = MachineProfiles.First(profile => profile.Id == selectedProfile.Id);
            OnPropertyChanged(nameof(SelectedMachineProfile));
        }
        else if (!profiles.Status.IsSuccess)
        {
            SettingsStatusText = profiles.Status.Message;
            return;
        }

        var settings = await _hostClient.GetSettingsAsync(cancellationToken).ConfigureAwait(true);
        if (!settings.Status.IsSuccess || settings.Settings is null)
        {
            if (!settings.Status.IsSuccess)
                SettingsStatusText = settings.Status.Message;
            return;
        }

        ApplySettingsFromHost(settings.Settings);
        _appliedSettings = CaptureSettings();
        HasPendingSettingsChanges = false;
        RequiresRestart = false;
        ReplaceSettingsValidationResults([]);
        SettingsStatusText = "Settings loaded from host.";
    }

    private void ApplySettingsFromHost(SessionSettingsDto settings)
    {
        _limiterRatePercent = Math.Clamp(settings.Limiter.RatePercent, LimiterMinimumPercent, LimiterMaximumPercent);
        _limiterEnabled = settings.Limiter.IsEnabled;
        _selectedMachineProfile = MachineProfiles.FirstOrDefault(profile => string.Equals(profile.Id, settings.ProfileId, StringComparison.OrdinalIgnoreCase))
            ?? _selectedMachineProfile;
        _selectedRenderer = FromRendererId(settings.Display.Renderer);
        _selectedDisplayScale = FromScaleId(settings.Display.Scale);
        _selectedPalette = FromPaletteId(settings.Display.Palette);
        _selectedCropMode = FromCropModeId(settings.Display.CropMode, settings.Display.ShowBorder);
        _selectedAspectMode = FromAspectModeId(settings.Display.AspectMode, settings.Display.MaintainAspectRatio);
        _selectedAudioMode = FromAudioModeId(settings.Audio?.Mode ?? new AudioSettingsDto().Mode);
        _selectedInputMode = FromInputModeId(settings.Input.Mode);
        _selectedPrimaryJoystickPort = FromInputPort(settings.Input.PrimaryJoystickPort);
        _swapJoystickPorts = settings.Input.SwapJoystickPorts;
        _selectedResourceMode = FromResourceModeId(settings.Resources?.Mode ?? new ResourceSettingsDto().Mode);

        OnPropertyChanged(nameof(LimiterRatePercent));
        OnPropertyChanged(nameof(LimiterEnabled));
        OnPropertyChanged(nameof(IsWarpMode)); // derived from LimiterEnabled; keep the Warp checkbox in sync
        OnPropertyChanged(nameof(SelectedMachineProfile));
        OnPropertyChanged(nameof(SelectedRenderer));
        OnPropertyChanged(nameof(SelectedDisplayScale));
        OnPropertyChanged(nameof(SelectedPalette));
        OnPropertyChanged(nameof(SelectedCropMode));
        OnPropertyChanged(nameof(SelectedAspectMode));
        OnPropertyChanged(nameof(SelectedAudioMode));
        OnPropertyChanged(nameof(SelectedInputMode));
        OnPropertyChanged(nameof(SelectedPrimaryJoystickPort));
        OnPropertyChanged(nameof(SwapJoystickPorts));
        OnPropertyChanged(nameof(SelectedResourceMode));
    }

    private UpdateSettingsRequest CreateUpdateSettingsRequest(bool restartSession)
    {
        return new UpdateSettingsRequest(
            _hostClient.SessionId,
            new LimiterSettingsDto(LimiterRatePercent, LimiterEnabled),
            new DisplaySettingsDto(
                ToRendererId(SelectedRenderer),
                ToPaletteId(SelectedPalette),
                !string.Equals(SelectedCropMode, "Borderless", StringComparison.OrdinalIgnoreCase),
                !string.Equals(SelectedAspectMode, "Square pixels", StringComparison.OrdinalIgnoreCase),
                ToScaleId(SelectedDisplayScale),
                ToCropModeId(SelectedCropMode),
                ToAspectModeId(SelectedAspectMode)),
            new InputSettingsDto(
                SelectedKeyboardMap?.Id ?? "c64:gtk3_pos",
                ToInputPort(SelectedPrimaryJoystickPort),
                SwapJoystickPorts,
                ToInputModeId(SelectedInputMode)),
            SelectedMachineProfile.Id,
            restartSession,
            new AudioSettingsDto(ToAudioModeId(SelectedAudioMode)),
            new ResourceSettingsDto(ToResourceModeId(SelectedResourceMode)));
    }

    private ValidateSettingsResourcesRequest CreateValidateSettingsRequest()
    {
        return new ValidateSettingsResourcesRequest(
            _hostClient.SessionId,
            new LimiterSettingsDto(LimiterRatePercent, LimiterEnabled),
            new DisplaySettingsDto(
                ToRendererId(SelectedRenderer),
                ToPaletteId(SelectedPalette),
                !string.Equals(SelectedCropMode, "Borderless", StringComparison.OrdinalIgnoreCase),
                !string.Equals(SelectedAspectMode, "Square pixels", StringComparison.OrdinalIgnoreCase),
                ToScaleId(SelectedDisplayScale),
                ToCropModeId(SelectedCropMode),
                ToAspectModeId(SelectedAspectMode)),
            new InputSettingsDto(
                SelectedKeyboardMap?.Id ?? "c64:gtk3_pos",
                ToInputPort(SelectedPrimaryJoystickPort),
                SwapJoystickPorts,
                ToInputModeId(SelectedInputMode)),
            new AudioSettingsDto(ToAudioModeId(SelectedAudioMode)),
            new ResourceSettingsDto(ToResourceModeId(SelectedResourceMode)));
    }

    private void ReplaceSettingsValidationResults(IEnumerable<SettingsResourceValidationDto> resources)
    {
        SettingsValidationResults.Clear();
        foreach (var resource in resources)
            SettingsValidationResults.Add(resource);

        OnPropertyChanged(nameof(HasSettingsValidationResults));
    }

    private static string CreateSettingsStatus(IReadOnlyList<SettingApplyDiagnosticDto> diagnostics)
    {
        if (diagnostics.Count == 0)
            return "Settings applied.";

        return string.Join(" ", diagnostics.Select(diagnostic => diagnostic.Message));
    }

    private static string ToPaletteId(string palette)
    {
        return palette switch
        {
            "Pepto" => "pepto",
            "Monochrome green" => "monochrome-green",
            "Amber" => "amber",
            _ => "vice"
        };
    }

    private static string ToRendererId(string renderer)
    {
        return renderer switch
        {
            "Software" => "software",
            _ => "host"
        };
    }

    private static string FromRendererId(string renderer)
    {
        return renderer switch
        {
            "software" => "Software",
            _ => "Host direct"
        };
    }

    private static string ToScaleId(string scale)
    {
        return scale switch
        {
            "Fit window" => "fit-window",
            _ => scale
        };
    }

    private static string FromScaleId(string scale)
    {
        return scale switch
        {
            "fit-window" => "Fit window",
            "" => "2x",
            _ => scale
        };
    }

    private static string ToCropModeId(string cropMode)
    {
        return cropMode switch
        {
            "Full frame" => "full-frame",
            "Borderless" => "borderless",
            _ => "visible-area"
        };
    }

    private static string FromCropModeId(string cropMode, bool showBorder)
    {
        return cropMode switch
        {
            "full-frame" => "Full frame",
            "borderless" => "Borderless",
            "visible-area" => "Visible area",
            _ => showBorder ? "Visible area" : "Borderless"
        };
    }

    private static string ToAspectModeId(string aspectMode)
    {
        return aspectMode switch
        {
            "Square pixels" => "square-pixels",
            "Force 4:3" => "force-4-3",
            _ => "vice-pixel-aspect"
        };
    }

    private static string FromAspectModeId(string aspectMode, bool maintainAspectRatio)
    {
        return aspectMode switch
        {
            "square-pixels" => "Square pixels",
            "force-4-3" => "Force 4:3",
            "vice-pixel-aspect" => "VICE pixel aspect",
            _ => maintainAspectRatio ? "VICE pixel aspect" : "Square pixels"
        };
    }

    private static string ToAudioModeId(string audioMode)
    {
        return audioMode switch
        {
            "Muted" => "muted",
            "Unavailable" => "unavailable",
            _ => "enabled"
        };
    }

    private static string FromAudioModeId(string audioMode)
    {
        return audioMode switch
        {
            "muted" => "Muted",
            "unavailable" => "Unavailable",
            _ => "Enabled"
        };
    }

    private static string ToInputModeId(string inputMode)
    {
        return inputMode switch
        {
            "Keyboard only" => "keyboard-only",
            "Disabled" => "disabled",
            _ => "keyboard-joystick"
        };
    }

    private static string FromInputModeId(string inputMode)
    {
        return inputMode switch
        {
            "keyboard-only" => "Keyboard only",
            "disabled" => "Disabled",
            _ => "Keyboard + joystick"
        };
    }

    private static string ToResourceModeId(string resourceMode)
    {
        return resourceMode switch
        {
            "Use configured paths" => "configured-paths",
            "Missing resources" => "missing-resources",
            _ => "auto-detect"
        };
    }

    private static string FromResourceModeId(string resourceMode)
    {
        return resourceMode switch
        {
            "configured-paths" => "Use configured paths",
            "missing-resources" => "Missing resources",
            _ => "Auto detect"
        };
    }

    private static string FromPaletteId(string palette)
    {
        return palette switch
        {
            "pepto" => "Pepto",
            "monochrome-green" => "Monochrome green",
            "amber" => "Amber",
            _ => "VICE default"
        };
    }

    private static InputPort ToInputPort(string inputPort)
    {
        return string.Equals(inputPort, "Joystick 1", StringComparison.OrdinalIgnoreCase)
            ? InputPort.Joystick1
            : InputPort.Joystick2;
    }

    private static string FromInputPort(InputPort inputPort)
    {
        return inputPort == InputPort.Joystick1 ? "Joystick 1" : "Joystick 2";
    }

    private static bool IsRestartRelevant(string propertyName)
    {
        return propertyName is nameof(SelectedMachineProfile) or nameof(SelectedResourceMode);
    }

    private SettingsSnapshot CaptureSettings()
    {
        return new SettingsSnapshot(
            LimiterRatePercent,
            LimiterEnabled,
            SelectedMachineProfile.Id,
            SelectedRenderer,
            SelectedDisplayScale,
            SelectedCropMode,
            SelectedAspectMode,
            SelectedPalette,
            SelectedAudioMode,
            SelectedInputMode,
            SelectedPrimaryJoystickPort,
            SwapJoystickPorts,
            SelectedResourceMode);
    }

    private void RestoreSettings(SettingsSnapshot snapshot)
    {
        _limiterRatePercent = snapshot.LimiterRatePercent;
        _limiterEnabled = snapshot.LimiterEnabled;
        _selectedMachineProfile = MachineProfiles.FirstOrDefault(profile => profile.Id == snapshot.MachineProfileId) ?? MachineProfiles[0];
        _selectedRenderer = snapshot.Renderer;
        _selectedDisplayScale = snapshot.DisplayScale;
        _selectedCropMode = snapshot.CropMode;
        _selectedAspectMode = snapshot.AspectMode;
        _selectedPalette = snapshot.Palette;
        _selectedAudioMode = snapshot.AudioMode;
        _selectedInputMode = snapshot.InputMode;
        _selectedPrimaryJoystickPort = snapshot.PrimaryJoystickPort;
        _swapJoystickPorts = snapshot.SwapJoystickPorts;
        _selectedResourceMode = snapshot.ResourceMode;

        OnPropertyChanged(nameof(LimiterRatePercent));
        OnPropertyChanged(nameof(LimiterEnabled));
        OnPropertyChanged(nameof(IsWarpMode)); // derived from LimiterEnabled; keep the Warp checkbox in sync
        OnPropertyChanged(nameof(SelectedMachineProfile));
        OnPropertyChanged(nameof(SelectedRenderer));
        OnPropertyChanged(nameof(SelectedDisplayScale));
        OnPropertyChanged(nameof(SelectedCropMode));
        OnPropertyChanged(nameof(SelectedAspectMode));
        OnPropertyChanged(nameof(SelectedPalette));
        OnPropertyChanged(nameof(SelectedAudioMode));
        OnPropertyChanged(nameof(SelectedInputMode));
        OnPropertyChanged(nameof(SelectedPrimaryJoystickPort));
        OnPropertyChanged(nameof(SwapJoystickPorts));
        OnPropertyChanged(nameof(SelectedResourceMode));
    }
}

public sealed record MachineProfileOption(string Id, string DisplayName, string StatusText, bool IsPlaceholder);

internal sealed record SettingsSnapshot(
    double LimiterRatePercent,
    bool LimiterEnabled,
    string MachineProfileId,
    string Renderer,
    string DisplayScale,
    string CropMode,
    string AspectMode,
    string Palette,
    string AudioMode,
    string InputMode,
    string PrimaryJoystickPort,
    bool SwapJoystickPorts,
    string ResourceMode);

public enum SidebarTab
{
    Peripherals,
    Settings,
    Monitor
}
