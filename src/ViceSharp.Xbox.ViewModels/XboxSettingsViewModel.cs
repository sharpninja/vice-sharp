namespace ViceSharp.Xbox.ViewModels;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ViceSharp.Protocol;

/// <summary>
/// PLAN-XBOXUWP S26 (IMPL-XBOXUWP-026), area XSET / XMVVM. FR-XSET-001..005,
/// TR-XMVVM-001. The portable 10-foot (couch) settings ViewModel: it reproduces the
/// desktop settings-panel behaviors against the host-owned
/// <see cref="IXboxSettingsGateway"/> seam, so a TV UI can read, edit, validate, apply,
/// and revert emulator settings with a controller.
/// </summary>
/// <remarks>
/// <para>
/// Pure MVVM (TR-MVVM-001): it references only the portable contracts
/// (<c>ViceSharp.Abstractions</c>, <c>ViceSharp.Protocol</c>,
/// <c>ViceSharp.Xbox.Input</c>) and holds no engine, host, or XAML reference. It is a
/// fresh class that MIRRORS the SEMANTICS of the desktop settings panel (it does not
/// reference it):
/// </para>
/// <list type="bullet">
///   <item><description>Each picker binds a display LABEL; the shared
///   <see cref="SettingsOptionCatalog"/> converts label &lt;-&gt; stored id on refresh
///   (<c>From*Id</c>) and on apply/validate (<c>To*Id</c>), so the Xbox and desktop
///   surfaces stay byte-for-byte identical.</description></item>
///   <item><description>Dirty tracking: any bindable change measured against the
///   last-applied baseline sets <see cref="IsDirty"/>.</description></item>
///   <item><description>Restart gating: changing the profile or the resource mode sets
///   <see cref="RequiresRestart"/> (other changes apply live).</description></item>
///   <item><description>Host-canonical adoption: <see cref="ApplySettingsAsync"/> re-binds
///   every field from what the host RETURNED, which may differ from what was sent.</description></item>
/// </list>
/// <para>
/// FR-XSET-005: master volume, mute, the TV-safe-area inset, and the per-stick deadzones
/// are Xbox-only preferences. They participate in dirty tracking and revert but are NOT
/// emulator settings, so they never appear in an <see cref="UpdateSettingsRequest"/>;
/// persistence and live audio wiring are owned by other slices.
/// </para>
/// </remarks>
public sealed class XboxSettingsViewModel : INotifyPropertyChanged
{
    private const double LimiterMinimumPercent = 1;
    private const double LimiterMaximumPercent = 100_000;

    // FEAT-XROMPICK-001: optional ROM readiness evaluator for the model picker.
    private RomProvisionEvaluator? _romEvaluator;
    private string _c64RomDirectory = string.Empty;
    private string _selectedModelRomStatus = "ROM readiness: not configured.";

    // The one implemented computer family: its C64 machine profiles are the selectable
    // models. The "Minimal host" pseudo-profile (Machine=="minimal") is excluded by this
    // same ordinal filter.
    private const string C64FamilyId = "x64sc";
    private const string Vic20FamilyId = "xvic";

    private readonly IXboxSettingsGateway _gateway;
    private readonly string _sessionId;
    private readonly List<SettingsResourceValidationDto> _validationResults = new();

    // UI-thread dispatch (FIX-XSETBLANK-001): the refresh continuations resume on the
    // thread pool (ConfigureAwait(false)), and UWP bindings ignore PropertyChanged raised
    // off the UI thread; the on-device receipt showed a fully populated VM behind blank
    // pickers. Captured at construction (the page builds the VM on the UI thread) and
    // posted to from any other thread, same as XboxRomProvisioningViewModel.
    private readonly SynchronizationContext? _sync;

    // Non-bindable host state preserved across a round-trip (seeded on refresh/adopt,
    // echoed back on apply so we never fabricate ids the picker does not surface).
    private string _keyboardMapId = "c64:gtk3_pos";
    private bool _limiterEnabled = true;
    private bool _showBorder = true;
    private bool _maintainAspectRatio = true;

    // Suppresses dirty/restart recomputation while the ViewModel itself rewrites the
    // bound fields (refresh, host-canonical adoption, revert).
    private bool _suppressTracking;

    private SettingsBaseline _baseline;

    // Bindable backing fields.
    private string _selectedRenderer = SettingsOptionCatalog.RendererModes[0];
    private string _selectedDisplayScale = "2x";
    private string _selectedCropMode = SettingsOptionCatalog.CropModes[1];
    private string _selectedAspectMode = SettingsOptionCatalog.AspectModes[1];
    private string _selectedPalette = SettingsOptionCatalog.PaletteModes[0];
    private string _selectedAudioMode = SettingsOptionCatalog.AudioModes[0];
    private string _selectedInputMode = SettingsOptionCatalog.InputModes[0];
    private string _selectedPrimaryJoystickPort = SettingsOptionCatalog.PrimaryJoystickPorts[0];
    private bool _swapJoystickPorts;
    private string _selectedResourceMode = SettingsOptionCatalog.ResourceModes[0];
    private string _selectedPacingStrategy = SettingsOptionCatalog.PacingStrategies[0];
    private double _limiterRatePercent = 100;
    private string _selectedProfileId = string.Empty;

    // Xbox-only preferences (FR-XSET-005): not emulator settings.
    private double _masterVolumePercent = 100;
    private bool _muted;
    private double _tvSafeAreaInsetPercent = 5;
    private double _leftStickDeadzonePercent = 30;
    private double _rightStickDeadzonePercent = 30;

    private IReadOnlyList<SettingsProfileDto> _profiles = Array.Empty<SettingsProfileDto>();
    private IReadOnlyList<SettingsProfileDto> _models = Array.Empty<SettingsProfileDto>();
    private bool _isDirty;
    private bool _requiresRestart;
    private string _statusText = string.Empty;

    /// <summary>
    /// Creates the settings ViewModel over a host settings gateway and the session id the
    /// apply/validate requests are addressed to.
    /// </summary>
    /// <param name="gateway">The host-owned settings gateway seam.</param>
    /// <param name="sessionId">The emulator session id carried on update/validate requests.</param>
    /// <exception cref="ArgumentNullException"><paramref name="gateway"/> is <c>null</c>.</exception>
    public XboxSettingsViewModel(IXboxSettingsGateway gateway, string sessionId = "")
    {
        _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
        _sessionId = sessionId ?? string.Empty;
        _sync = SynchronizationContext.Current;
        _baseline = CaptureBaseline();

        // Iteration 2: VIC-20 is selectable (ROMs via VICESHARP_ROM_PATH). C128/PET/Plus4 remain roadmap placeholders.
        Computers = CreateDefaultComputerOptions();
    }

    /// <summary>
    /// Shared computer-family catalog for Xbox settings and heads tests.
    /// </summary>
    public static ComputerOption[] CreateDefaultComputerOptions() =>
    [
        new("Commodore 64", C64FamilyId, true),
        new("VIC-20", "xvic", true),
        new("Commodore 128", "x128", false),
        new("Commodore PET", "xpet", false),
        new("Plus/4", "xplus4", false),
    ];

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    // ---- Picker option lists (shared catalog) --------------------------------

    /// <summary>Renderer picker options.</summary>
    public IReadOnlyList<string> Renderers => SettingsOptionCatalog.RendererModes;

    /// <summary>Display-scale picker options.</summary>
    public IReadOnlyList<string> DisplayScales => SettingsOptionCatalog.DisplayScales;

    /// <summary>Crop-mode picker options.</summary>
    public IReadOnlyList<string> CropModes => SettingsOptionCatalog.CropModes;

    /// <summary>Aspect-mode picker options.</summary>
    public IReadOnlyList<string> AspectModes => SettingsOptionCatalog.AspectModes;

    /// <summary>Palette picker options.</summary>
    public IReadOnlyList<string> Palettes => SettingsOptionCatalog.PaletteModes;

    /// <summary>Audio-mode picker options.</summary>
    public IReadOnlyList<string> AudioModes => SettingsOptionCatalog.AudioModes;

    /// <summary>Input-mode picker options.</summary>
    public IReadOnlyList<string> InputModes => SettingsOptionCatalog.InputModes;

    /// <summary>Primary-joystick-port picker options.</summary>
    public IReadOnlyList<string> PrimaryJoystickPorts => SettingsOptionCatalog.PrimaryJoystickPorts;

    /// <summary>Resource-mode picker options.</summary>
    public IReadOnlyList<string> ResourceModes => SettingsOptionCatalog.ResourceModes;

    /// <summary>Frame-pacing-strategy picker options.</summary>
    public IReadOnlyList<string> PacingStrategies => SettingsOptionCatalog.PacingStrategies;

    /// <summary>The available settings profiles (loaded by <see cref="RefreshAsync"/>).</summary>
    public IReadOnlyList<SettingsProfileDto> Profiles
    {
        get => _profiles;
        private set
        {
            if (!SetProperty(ref _profiles, value))
                return;

            // Model picker: implemented families (C64 + VIC-20); "Minimal host" excluded.
            RebuildModelsForSelectedFamily();
            OnPropertyChanged(nameof(SelectedModel));
            OnPropertyChanged(nameof(SelectedComputer));
            RefreshSelectedModelRomStatus();
        }
    }

    /// <summary>
    /// The selectable computer families (FR-XSET-002). C64 and VIC-20 are implemented;
    /// C128 / PET / Plus4 remain disabled placeholders.
    /// </summary>
    public IReadOnlyList<ComputerOption> Computers { get; }

    /// <summary>
    /// Models for the selected computer family (C64 <c>x64sc</c> or VIC-20 <c>xvic</c>)
    /// from <see cref="Profiles"/>, excluding the "Minimal host" pseudo-profile.
    /// </summary>
    public IReadOnlyList<SettingsProfileDto> Models => _models;

    /// <summary>
    /// The model matching the current <see cref="SelectedProfileId"/>, or <c>null</c> when the
    /// selection is not one of the selectable <see cref="Models"/>. Setting it routes through
    /// the existing restart-gated <see cref="SelectedProfileId"/>; a <c>null</c> value or a DTO
    /// whose id is not in <see cref="Models"/> is ignored (no change, no dirty flip).
    /// </summary>
    public SettingsProfileDto? SelectedModel
    {
        get => Models.FirstOrDefault(m => string.Equals(m.Id, SelectedProfileId, StringComparison.Ordinal));
        set
        {
            if (value is not null && Models.Any(m => string.Equals(m.Id, value.Id, StringComparison.Ordinal)))
                SelectedProfileId = value.Id;
        }
    }

    /// <summary>
    /// Computer family for the current model. Setting to an available family selects that
    /// family's default model (first matching profile) so the Model combo refreshes.
    /// </summary>
    public ComputerOption? SelectedComputer
    {
        get
        {
            var machine = SelectedModel?.Machine
                ?? Profiles.FirstOrDefault(p => string.Equals(p.Id, SelectedProfileId, StringComparison.Ordinal))?.Machine;
            return machine is null
                ? null
                : Computers.FirstOrDefault(c => string.Equals(c.FamilyId, machine, StringComparison.Ordinal));
        }
        set
        {
            if (value is null || !value.IsAvailable)
                return;

            if (SelectedComputer is not null
                && string.Equals(SelectedComputer.FamilyId, value.FamilyId, StringComparison.Ordinal))
            {
                return;
            }

            var defaultId = value.FamilyId switch
            {
                Vic20FamilyId => "vic20",
                C64FamilyId => "c64",
                _ => null
            };

            var match = Profiles.FirstOrDefault(p =>
                string.Equals(p.Machine, value.FamilyId, StringComparison.Ordinal)
                && (defaultId is null || string.Equals(p.Id, defaultId, StringComparison.Ordinal)))
                ?? Profiles.FirstOrDefault(p => string.Equals(p.Machine, value.FamilyId, StringComparison.Ordinal));

            if (match is null)
                return;

            SelectedProfileId = match.Id;
            RebuildModelsForSelectedFamily();
            OnPropertyChanged(nameof(SelectedComputer));
            OnPropertyChanged(nameof(SelectedModel));
        }
    }

    private void RebuildModelsForSelectedFamily()
    {
        var family = Profiles.FirstOrDefault(p => string.Equals(p.Id, SelectedProfileId, StringComparison.Ordinal))?.Machine
            ?? C64FamilyId;
        _models = BuildModels(Profiles, family);
        OnPropertyChanged(nameof(Models));
    }

    private static IReadOnlyList<SettingsProfileDto> BuildModels(
        IReadOnlyList<SettingsProfileDto> profiles,
        string? familyFilter = null)
    {
        var models = new List<SettingsProfileDto>(profiles.Count);
        foreach (var profile in profiles)
        {
            if (!IsImplementedMachineFamily(profile.Machine))
                continue;
            if (familyFilter is not null
                && !string.Equals(profile.Machine, familyFilter, StringComparison.Ordinal))
            {
                continue;
            }

            models.Add(profile);
        }

        return models;
    }

    private static bool IsImplementedMachineFamily(string machine)
        => string.Equals(machine, C64FamilyId, StringComparison.Ordinal)
            || string.Equals(machine, Vic20FamilyId, StringComparison.Ordinal);

    // ---- Bindable emulator settings ------------------------------------------

    /// <summary>Selected renderer label.</summary>
    public string SelectedRenderer
    {
        get => _selectedRenderer;
        set => SetSettingsProperty(ref _selectedRenderer, value);
    }

    /// <summary>Selected display-scale label.</summary>
    public string SelectedDisplayScale
    {
        get => _selectedDisplayScale;
        set => SetSettingsProperty(ref _selectedDisplayScale, value);
    }

    /// <summary>Selected crop-mode label.</summary>
    public string SelectedCropMode
    {
        get => _selectedCropMode;
        set => SetSettingsProperty(ref _selectedCropMode, value);
    }

    /// <summary>Selected aspect-mode label.</summary>
    public string SelectedAspectMode
    {
        get => _selectedAspectMode;
        set => SetSettingsProperty(ref _selectedAspectMode, value);
    }

    /// <summary>Selected palette label.</summary>
    public string SelectedPalette
    {
        get => _selectedPalette;
        set => SetSettingsProperty(ref _selectedPalette, value);
    }

    /// <summary>Selected audio-mode label.</summary>
    public string SelectedAudioMode
    {
        get => _selectedAudioMode;
        set => SetSettingsProperty(ref _selectedAudioMode, value);
    }

    /// <summary>Selected input-mode label.</summary>
    public string SelectedInputMode
    {
        get => _selectedInputMode;
        set => SetSettingsProperty(ref _selectedInputMode, value);
    }

    /// <summary>Selected primary-joystick-port label.</summary>
    public string SelectedPrimaryJoystickPort
    {
        get => _selectedPrimaryJoystickPort;
        set => SetSettingsProperty(ref _selectedPrimaryJoystickPort, value);
    }

    /// <summary>Whether the two joystick ports are swapped.</summary>
    public bool SwapJoystickPorts
    {
        get => _swapJoystickPorts;
        set => SetSettingsProperty(ref _swapJoystickPorts, value);
    }

    /// <summary>Selected resource-mode label (restart-relevant).</summary>
    public string SelectedResourceMode
    {
        get => _selectedResourceMode;
        set => SetSettingsProperty(ref _selectedResourceMode, value);
    }

    /// <summary>Selected frame-pacing-strategy label.</summary>
    public string SelectedPacingStrategy
    {
        get => _selectedPacingStrategy;
        set => SetSettingsProperty(ref _selectedPacingStrategy, value);
    }

    /// <summary>Speed-limiter rate as a percentage.</summary>
    public double LimiterRatePercent
    {
        get => _limiterRatePercent;
        set => SetSettingsProperty(ref _limiterRatePercent, Math.Clamp(value, LimiterMinimumPercent, LimiterMaximumPercent));
    }

    /// <summary>Selected machine/settings profile id (restart-relevant).</summary>
    public string SelectedProfileId
    {
        get => _selectedProfileId;
        set
        {
            if (!SetSettingsProperty(ref _selectedProfileId, value ?? string.Empty))
                return;

            // Model list is per computer family; rebuild when the profile family may change.
            RebuildModelsForSelectedFamily();
            OnPropertyChanged(nameof(SelectedModel));
            OnPropertyChanged(nameof(SelectedComputer));
            RefreshSelectedModelRomStatus();
        }
    }

    /// <summary>
    /// FEAT-XROMPICK-001: human-readable ROM readiness for the selected model
    /// (Complete / Partial / Not provisioned / Ultimax kernal-optional). Hard boot
    /// block remains the ROM provisioning page; this is guidance only.
    /// </summary>
    public string SelectedModelRomStatus
    {
        get => _selectedModelRomStatus;
        private set => SetProperty(ref _selectedModelRomStatus, value);
    }

    /// <summary>
    /// FEAT-XROMPICK-001: attach a <see cref="RomProvisionEvaluator"/> and the writable
    /// C64 ROM directory so the model picker can surface readiness. Call after construction
    /// (App composition) before the player opens Settings.
    /// </summary>
    public void ConfigureRomReadiness(RomProvisionEvaluator evaluator, string c64RomDirectory)
    {
        _romEvaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
        _c64RomDirectory = c64RomDirectory ?? string.Empty;
        RefreshSelectedModelRomStatus();
    }

    /// <summary>
    /// Maps a settings profile id to a <see cref="RomProfile"/> for readiness evaluation.
    /// Ultimax-named profiles use the kernal-optional rule; everything else is Standard.
    /// </summary>
    public static RomProfile ResolveRomProfile(string? profileId)
    {
        if (string.IsNullOrEmpty(profileId))
            return RomProfile.Standard;
        return profileId.Contains("ultimax", StringComparison.OrdinalIgnoreCase)
            ? RomProfile.Ultimax
            : RomProfile.Standard;
    }

    private void RefreshSelectedModelRomStatus()
    {
        if (_romEvaluator is null || string.IsNullOrWhiteSpace(_c64RomDirectory))
        {
            SelectedModelRomStatus = "ROM readiness: open the ROM page to provision core ROMs.";
            return;
        }

        var assessment = _romEvaluator.Evaluate(_c64RomDirectory, ResolveRomProfile(SelectedProfileId));
        SelectedModelRomStatus = assessment.State switch
        {
            RomProvisionState.Complete => "ROMs: ready for this model.",
            RomProvisionState.Partial => "ROMs: partial - provision missing roles before boot.",
            RomProvisionState.NotProvisioned => "ROMs: not provisioned - open the ROM page.",
            RomProvisionState.Invalid => "ROMs: invalid files detected - re-import or download.",
            _ => $"ROMs: {assessment.State}.",
        };
    }

    // ---- Xbox-only preferences (FR-XSET-005) ---------------------------------

    /// <summary>Master output volume 0-100 (Xbox-only pref, never sent to the host DTOs).</summary>
    public double MasterVolumePercent
    {
        get => _masterVolumePercent;
        set => SetSettingsProperty(ref _masterVolumePercent, Math.Clamp(value, 0, 100));
    }

    /// <summary>Whether audio output is muted (Xbox-only pref, never sent to the host DTOs).</summary>
    public bool Muted
    {
        get => _muted;
        set => SetSettingsProperty(ref _muted, value);
    }

    /// <summary>TV-safe-area inset per edge as a percentage (Xbox-only pref).</summary>
    public double TvSafeAreaInsetPercent
    {
        get => _tvSafeAreaInsetPercent;
        set => SetSettingsProperty(ref _tvSafeAreaInsetPercent, Math.Clamp(value, 0, 25));
    }

    /// <summary>Left-stick radial deadzone as a percentage (Xbox-only pref).</summary>
    public double LeftStickDeadzonePercent
    {
        get => _leftStickDeadzonePercent;
        set => SetSettingsProperty(ref _leftStickDeadzonePercent, Math.Clamp(value, 0, 90));
    }

    /// <summary>Right-stick radial deadzone as a percentage (Xbox-only pref).</summary>
    public double RightStickDeadzonePercent
    {
        get => _rightStickDeadzonePercent;
        set => SetSettingsProperty(ref _rightStickDeadzonePercent, Math.Clamp(value, 0, 90));
    }

    // ---- Status flags + validation surface -----------------------------------

    /// <summary>True when any bindable value differs from the last-applied baseline.</summary>
    public bool IsDirty
    {
        get => _isDirty;
        private set => SetProperty(ref _isDirty, value);
    }

    /// <summary>
    /// True when a restart-relevant change (profile or resource mode) is pending. Other
    /// settings apply live.
    /// </summary>
    public bool RequiresRestart
    {
        get => _requiresRestart;
        private set => SetProperty(ref _requiresRestart, value);
    }

    /// <summary>The per-resource results from the most recent validation, or empty.</summary>
    public IReadOnlyList<SettingsResourceValidationDto> ValidationResults => _validationResults;

    /// <summary>True when <see cref="ValidationResults"/> is non-empty.</summary>
    public bool HasValidationResults => _validationResults.Count > 0;

    /// <summary>A short human-readable status message describing the last operation.</summary>
    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    // ---- Operations ----------------------------------------------------------

    /// <summary>
    /// Loads the host-canonical settings and the available profiles, binding every field
    /// from the returned DTOs, then snapshots the result as the last-applied baseline
    /// (<see cref="IsDirty"/> and <see cref="RequiresRestart"/> both cleared).
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var profiles = await _gateway.ListSettingsProfilesAsync(cancellationToken).ConfigureAwait(false);
        if (profiles.Status.IsSuccess)
            Profiles = profiles.Profiles;

        var settings = await _gateway.GetSettingsAsync(cancellationToken).ConfigureAwait(false);
        if (!settings.Status.IsSuccess || settings.Settings is null)
        {
            StatusText = settings.Status.IsSuccess ? "No settings returned by host." : settings.Status.Message;
            return;
        }

        _suppressTracking = true;
        try
        {
            AdoptSettings(settings.Settings);
            ClearValidationResults();
        }
        finally
        {
            _suppressTracking = false;
        }

        _baseline = CaptureBaseline();
        IsDirty = false;
        RequiresRestart = false;
        StatusText = "Settings loaded from host.";
    }

    /// <summary>
    /// Applies the current settings through the host pipeline, sending canonical stored ids,
    /// then adopts the host-canonical settings from the response (which may differ from what
    /// was sent), updates the last-applied baseline, and clears <see cref="IsDirty"/>.
    /// </summary>
    /// <param name="restartSession">
    /// Whether to request a session rebuild (forwarded verbatim as
    /// <see cref="UpdateSettingsRequest.RestartSession"/>).
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public async Task ApplySettingsAsync(bool restartSession, CancellationToken cancellationToken = default)
    {
        var response = await _gateway
            .UpdateSettingsAsync(BuildUpdateRequest(restartSession), cancellationToken)
            .ConfigureAwait(false);

        if (!response.Status.IsSuccess)
        {
            StatusText = response.Status.Message;
            return;
        }

        _suppressTracking = true;
        try
        {
            if (response.Settings is not null)
                AdoptSettings(response.Settings);
        }
        finally
        {
            _suppressTracking = false;
        }

        _baseline = CaptureBaseline();
        IsDirty = false;
        RequiresRestart = false;
        StatusText = "Settings applied.";
    }

    /// <summary>
    /// Validates the resources referenced by the current (possibly unapplied) settings without
    /// applying them: it populates <see cref="ValidationResults"/> and does NOT send an
    /// <see cref="UpdateSettingsRequest"/> or clear <see cref="IsDirty"/>.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public async Task ValidateSettingsAsync(CancellationToken cancellationToken = default)
    {
        var response = await _gateway
            .ValidateSettingsResourcesAsync(BuildValidateRequest(), cancellationToken)
            .ConfigureAwait(false);

        if (!response.Status.IsSuccess)
        {
            StatusText = response.Status.Message;
            return;
        }

        ReplaceValidationResults(response.Resources);

        var invalid = 0;
        foreach (var resource in response.Resources)
        {
            if (!resource.IsValid)
                invalid++;
        }

        StatusText = invalid == 0
            ? "Settings validation passed."
            : $"Settings validation found {invalid} issue(s).";
    }

    /// <summary>
    /// Restores every bindable field to the last-applied baseline, clears the validation
    /// results, and clears <see cref="IsDirty"/> / <see cref="RequiresRestart"/>.
    /// </summary>
    public void RevertSettings()
    {
        _suppressTracking = true;
        try
        {
            RestoreBaseline(_baseline);
            ClearValidationResults();
        }
        finally
        {
            _suppressTracking = false;
        }

        IsDirty = false;
        RequiresRestart = false;
        StatusText = "Reverted to the last applied settings.";
    }

    // ---- Internal binding helpers --------------------------------------------

    private void AdoptSettings(SessionSettingsDto settings)
    {
        var audio = settings.Audio ?? new AudioSettingsDto();
        var resources = settings.Resources ?? new ResourceSettingsDto();

        _keyboardMapId = settings.Input.KeyboardMapId;
        _limiterEnabled = settings.Limiter.IsEnabled;
        _showBorder = settings.Display.ShowBorder;
        _maintainAspectRatio = settings.Display.MaintainAspectRatio;

        SelectedRenderer = SettingsOptionCatalog.FromRendererId(settings.Display.Renderer);
        SelectedDisplayScale = SettingsOptionCatalog.FromScaleId(settings.Display.Scale);
        SelectedPalette = SettingsOptionCatalog.FromPaletteId(settings.Display.Palette);
        SelectedCropMode = SettingsOptionCatalog.FromCropModeId(settings.Display.CropMode, settings.Display.ShowBorder);
        SelectedAspectMode = SettingsOptionCatalog.FromAspectModeId(settings.Display.AspectMode, settings.Display.MaintainAspectRatio);
        SelectedAudioMode = SettingsOptionCatalog.FromAudioModeId(audio.Mode);
        SelectedInputMode = SettingsOptionCatalog.FromInputModeId(settings.Input.Mode);
        SelectedPrimaryJoystickPort = SettingsOptionCatalog.FromInputPort(settings.Input.PrimaryJoystickPort);
        SwapJoystickPorts = settings.Input.SwapJoystickPorts;
        SelectedResourceMode = SettingsOptionCatalog.FromResourceModeId(resources.Mode);
        SelectedPacingStrategy = SettingsOptionCatalog.FromPacingStrategyId(settings.Limiter.PacingStrategy);
        LimiterRatePercent = settings.Limiter.RatePercent;
        SelectedProfileId = settings.ProfileId;
    }

    private UpdateSettingsRequest BuildUpdateRequest(bool restartSession) => new(
        _sessionId,
        BuildLimiter(),
        BuildDisplay(),
        BuildInput(),
        SelectedProfileId,
        restartSession,
        BuildAudio(),
        BuildResources());

    private ValidateSettingsResourcesRequest BuildValidateRequest() => new(
        _sessionId,
        BuildLimiter(),
        BuildDisplay(),
        BuildInput(),
        BuildAudio(),
        BuildResources());

    private LimiterSettingsDto BuildLimiter() => new(
        LimiterRatePercent,
        _limiterEnabled,
        SettingsOptionCatalog.ToPacingStrategyId(SelectedPacingStrategy));

    private DisplaySettingsDto BuildDisplay() => new(
        SettingsOptionCatalog.ToRendererId(SelectedRenderer),
        SettingsOptionCatalog.ToPaletteId(SelectedPalette),
        !string.Equals(SelectedCropMode, "Borderless", StringComparison.OrdinalIgnoreCase),
        !string.Equals(SelectedAspectMode, "Square pixels", StringComparison.OrdinalIgnoreCase),
        SettingsOptionCatalog.ToScaleId(SelectedDisplayScale),
        SettingsOptionCatalog.ToCropModeId(SelectedCropMode),
        SettingsOptionCatalog.ToAspectModeId(SelectedAspectMode));

    private InputSettingsDto BuildInput() => new(
        _keyboardMapId,
        SettingsOptionCatalog.ToInputPort(SelectedPrimaryJoystickPort),
        SwapJoystickPorts,
        SettingsOptionCatalog.ToInputModeId(SelectedInputMode));

    private AudioSettingsDto BuildAudio() => new(SettingsOptionCatalog.ToAudioModeId(SelectedAudioMode));

    private ResourceSettingsDto BuildResources() => new(SettingsOptionCatalog.ToResourceModeId(SelectedResourceMode));

    private SettingsBaseline CaptureBaseline() => new(
        SelectedRenderer,
        SelectedDisplayScale,
        SelectedCropMode,
        SelectedAspectMode,
        SelectedPalette,
        SelectedAudioMode,
        SelectedInputMode,
        SelectedPrimaryJoystickPort,
        SwapJoystickPorts,
        SelectedResourceMode,
        SelectedPacingStrategy,
        LimiterRatePercent,
        SelectedProfileId,
        MasterVolumePercent,
        Muted,
        TvSafeAreaInsetPercent,
        LeftStickDeadzonePercent,
        RightStickDeadzonePercent);

    private void RestoreBaseline(SettingsBaseline baseline)
    {
        SelectedRenderer = baseline.Renderer;
        SelectedDisplayScale = baseline.DisplayScale;
        SelectedCropMode = baseline.CropMode;
        SelectedAspectMode = baseline.AspectMode;
        SelectedPalette = baseline.Palette;
        SelectedAudioMode = baseline.AudioMode;
        SelectedInputMode = baseline.InputMode;
        SelectedPrimaryJoystickPort = baseline.PrimaryJoystickPort;
        SwapJoystickPorts = baseline.SwapJoystickPorts;
        SelectedResourceMode = baseline.ResourceMode;
        SelectedPacingStrategy = baseline.PacingStrategy;
        LimiterRatePercent = baseline.LimiterRatePercent;
        SelectedProfileId = baseline.ProfileId;
        MasterVolumePercent = baseline.MasterVolumePercent;
        Muted = baseline.Muted;
        TvSafeAreaInsetPercent = baseline.TvSafeAreaInsetPercent;
        LeftStickDeadzonePercent = baseline.LeftStickDeadzonePercent;
        RightStickDeadzonePercent = baseline.RightStickDeadzonePercent;
    }

    private void RecomputeDirtyState()
    {
        var current = CaptureBaseline();
        IsDirty = !current.Equals(_baseline);
        RequiresRestart =
            !string.Equals(current.ProfileId, _baseline.ProfileId, StringComparison.Ordinal)
            || !string.Equals(current.ResourceMode, _baseline.ResourceMode, StringComparison.Ordinal);
    }

    private void ReplaceValidationResults(IReadOnlyList<SettingsResourceValidationDto> resources)
    {
        _validationResults.Clear();
        _validationResults.AddRange(resources);
        OnPropertyChanged(nameof(ValidationResults));
        OnPropertyChanged(nameof(HasValidationResults));
    }

    private void ClearValidationResults()
    {
        if (_validationResults.Count == 0)
            return;

        _validationResults.Clear();
        OnPropertyChanged(nameof(ValidationResults));
        OnPropertyChanged(nameof(HasValidationResults));
    }

    private bool SetSettingsProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (!SetProperty(ref field, value, propertyName))
            return false;

        if (_suppressTracking)
            return true;

        RecomputeDirtyState();
        ClearValidationResults();
        return true;
    }

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        var handler = PropertyChanged;
        if (handler is null)
            return;

        // Raise inline on the captured (UI) context; dispatch from anywhere else so the
        // XAML bindings always hear the change (FIX-XSETBLANK-001).
        if (_sync is null || SynchronizationContext.Current == _sync)
            handler(this, new PropertyChangedEventArgs(propertyName));
        else
            _sync.Post(_ => handler(this, new PropertyChangedEventArgs(propertyName)), null);
    }

    /// <summary>
    /// The immutable snapshot of the last-applied bindable state used for dirty comparison
    /// and revert. Structural record equality drives <see cref="IsDirty"/>.
    /// </summary>
    private sealed record SettingsBaseline(
        string Renderer,
        string DisplayScale,
        string CropMode,
        string AspectMode,
        string Palette,
        string AudioMode,
        string InputMode,
        string PrimaryJoystickPort,
        bool SwapJoystickPorts,
        string ResourceMode,
        string PacingStrategy,
        double LimiterRatePercent,
        string ProfileId,
        double MasterVolumePercent,
        bool Muted,
        double TvSafeAreaInsetPercent,
        double LeftStickDeadzonePercent,
        double RightStickDeadzonePercent);
}
