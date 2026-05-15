using ViceSharp.Abstractions;
using ViceSharp.Architectures.C64;
using ViceSharp.Host.Runtime;
using ViceSharp.Protocol;

namespace ViceSharp.Host.Services;

public sealed class SettingsServiceHost : ISettingsService
{
    private static readonly HashSet<string> KnownRenderers = new(StringComparer.OrdinalIgnoreCase)
    {
        "host",
        "software"
    };

    private static readonly HashSet<string> KnownPalettes = new(StringComparer.OrdinalIgnoreCase)
    {
        "default",
        "vice",
        "pepto",
        "monochrome-green",
        "amber"
    };

    private static readonly HashSet<string> KnownDisplayScales = new(StringComparer.OrdinalIgnoreCase)
    {
        "1x",
        "2x",
        "3x",
        "fit-window"
    };

    private static readonly HashSet<string> KnownCropModes = new(StringComparer.OrdinalIgnoreCase)
    {
        "full-frame",
        "visible-area",
        "borderless"
    };

    private static readonly HashSet<string> KnownAspectModes = new(StringComparer.OrdinalIgnoreCase)
    {
        "square-pixels",
        "vice-pixel-aspect",
        "force-4-3"
    };

    private static readonly HashSet<string> KnownAudioModes = new(StringComparer.OrdinalIgnoreCase)
    {
        "enabled",
        "muted",
        "unavailable"
    };

    private static readonly HashSet<string> KnownInputModes = new(StringComparer.OrdinalIgnoreCase)
    {
        "keyboard-joystick",
        "keyboard-only",
        "disabled"
    };

    private static readonly HashSet<string> KnownResourceModes = new(StringComparer.OrdinalIgnoreCase)
    {
        "auto-detect",
        "configured-paths",
        "missing-resources"
    };

    private readonly EmulatorRuntimeRegistry _registry;
    private readonly IEmulatorRuntimeFactory _runtimeFactory;

    public SettingsServiceHost(EmulatorRuntimeRegistry registry)
        : this(registry, new DefaultEmulatorRuntimeFactory())
    {
    }

    public SettingsServiceHost(EmulatorRuntimeRegistry registry, IEmulatorRuntimeFactory runtimeFactory)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(runtimeFactory);
        _registry = registry;
        _runtimeFactory = runtimeFactory;
    }

    public ValueTask<ListSettingsProfilesResponse> ListProfilesAsync(
        SessionRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_registry.TryGet(request.SessionId, out var session))
            return ValueTask.FromResult(new ListSettingsProfilesResponse(HostProtocolMapper.MissingSessionStatus(request.SessionId), []));

        lock (session.SyncRoot)
        {
            return ValueTask.FromResult(new ListSettingsProfilesResponse(RpcStatus.Ok(), CreateProfiles(session)));
        }
    }

    public ValueTask<GetSettingsResponse> GetSettingsAsync(
        SessionRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_registry.TryGet(request.SessionId, out var session))
            return ValueTask.FromResult(new GetSettingsResponse(HostProtocolMapper.MissingSessionStatus(request.SessionId), null));

        lock (session.SyncRoot)
        {
            return ValueTask.FromResult(new GetSettingsResponse(RpcStatus.Ok(), HostProtocolMapper.ToSettingsDto(session)));
        }
    }

    public ValueTask<UpdateSettingsResponse> UpdateSettingsAsync(
        UpdateSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_registry.TryGet(request.SessionId, out var session))
            return ValueTask.FromResult(new UpdateSettingsResponse(HostProtocolMapper.MissingSessionStatus(request.SessionId), null, []));

        var validation = Validate(request.Limiter, request.Display, request.Input, request.Audio, request.Resources);
        var invalid = validation.FirstOrDefault(resource => !resource.IsValid);
        if (invalid is not null)
        {
            return ValueTask.FromResult(new UpdateSettingsResponse(
                RpcStatus.InvalidArgument(invalid.Message),
                null,
                []));
        }

        if (!string.IsNullOrWhiteSpace(request.ProfileId) &&
            !TryResolveProfileId(request.ProfileId, out _))
        {
            return ValueTask.FromResult(new UpdateSettingsResponse(
                RpcStatus.InvalidArgument($"Unknown machine profile '{request.ProfileId}'."),
                null,
                []));
        }

        lock (session.SyncRoot)
        {
            var diagnostics = new List<SettingApplyDiagnosticDto>();
            var currentProfileId = HostProtocolMapper.ToSettingsDto(session).ProfileId;
            var requestedProfileId = string.IsNullOrWhiteSpace(request.ProfileId)
                ? currentProfileId
                : ResolveProfileId(request.ProfileId);
            var profileChanged = !string.Equals(requestedProfileId, currentProfileId, StringComparison.OrdinalIgnoreCase);
            var restartRelevant = profileChanged || request.Display is not null || request.Input is not null || request.Resources is not null;

            if (request.RestartSession && restartRelevant)
            {
                var limiterRatePercent = request.Limiter is null
                    ? session.LimiterRatePercent
                    : request.Limiter.RatePercent;
                var limiterEnabled = request.Limiter?.IsEnabled ?? session.LimiterEnabled;
                var displaySettings = request.Display ?? session.DisplaySettings;
                var inputSettings = request.Input ?? session.InputSettings with { KeyboardMapId = session.SelectedKeyboardMapId };
                var audioSettings = request.Audio ?? session.AudioSettings;
                var resourceSettings = request.Resources ?? session.ResourceSettings;
                var selectedKeyboardMap = string.Equals(inputSettings.KeyboardMapId, session.SelectedKeyboardMapId, StringComparison.OrdinalIgnoreCase)
                    ? session.SelectedKeyboardMap
                    : null;

                EmulatorRuntimeSession restarted;
                try
                {
                    restarted = CreateRestartedSession(
                        session,
                        requestedProfileId,
                        limiterRatePercent,
                        limiterEnabled,
                        displaySettings,
                        inputSettings,
                        audioSettings,
                        resourceSettings,
                        selectedKeyboardMap);
                }
                catch (InvalidOperationException ex)
                {
                    return ValueTask.FromResult(new UpdateSettingsResponse(
                        RpcStatus.FailedPrecondition(ex.Message),
                        HostProtocolMapper.ToSettingsDto(session),
                        diagnostics));
                }

                AddLimiterDiagnostic(request, diagnostics);

                if (profileChanged)
                {
                    diagnostics.Add(new SettingApplyDiagnosticDto(
                        "profile",
                        SettingApplyScope.RestartRequired,
                        true,
                        false,
                        $"Profile '{requestedProfileId}' was applied by restarting the host session from active profile '{currentProfileId}'."));
                }

                if (request.Display is not null)
                {
                    diagnostics.Add(new SettingApplyDiagnosticDto(
                        "display",
                        SettingApplyScope.RestartRequired,
                        true,
                        false,
                        "Display settings were applied by restarting the host session."));
                }

                if (request.Input is not null)
                {
                    diagnostics.Add(new SettingApplyDiagnosticDto(
                        "input",
                        SettingApplyScope.RestartRequired,
                        true,
                        false,
                        "Input settings were applied by restarting the host session."));
                }

                if (request.Audio is not null)
                {
                    diagnostics.Add(new SettingApplyDiagnosticDto(
                        "audio",
                        SettingApplyScope.RestartRequired,
                        true,
                        false,
                        "Audio settings were applied by restarting the host session."));
                }

                if (request.Resources is not null)
                {
                    diagnostics.Add(new SettingApplyDiagnosticDto(
                        "resources",
                        SettingApplyScope.RestartRequired,
                        true,
                        false,
                        "Resource settings were applied by restarting the host session."));
                }

                _registry.Replace(restarted);
                return ValueTask.FromResult(new UpdateSettingsResponse(
                    RpcStatus.Ok(),
                    HostProtocolMapper.ToSettingsDto(restarted),
                    diagnostics));
            }

            if (request.Limiter is not null)
            {
                session.LimiterRatePercent = request.Limiter.RatePercent;
                session.LimiterEnabled = request.Limiter.IsEnabled;
                AddLimiterDiagnostic(request, diagnostics);
            }

            if (!string.IsNullOrWhiteSpace(request.ProfileId) && profileChanged)
            {
                diagnostics.Add(new SettingApplyDiagnosticDto(
                    "profile",
                    SettingApplyScope.RestartRequired,
                    false,
                    true,
                    $"Profile '{requestedProfileId}' is staged and requires session restart to replace active profile '{currentProfileId}'."));
            }

            if (request.Display is not null)
            {
                session.DisplaySettings = request.Display;
                diagnostics.Add(new SettingApplyDiagnosticDto(
                    "display",
                    SettingApplyScope.RestartRequired,
                    false,
                    true,
                    "Display settings were stored on the host session and require runtime display reinitialization to take effect."));
            }

            if (request.Input is not null)
            {
                var previousInput = session.InputSettings;
                session.InputSettings = request.Input;
                session.SelectedKeyboardMapId = request.Input.KeyboardMapId;
                AddInputDiagnostics(previousInput, request.Input, diagnostics);
            }

            if (request.Audio is not null)
            {
                session.AudioSettings = request.Audio;
                diagnostics.Add(new SettingApplyDiagnosticDto(
                    "audio.mode",
                    SettingApplyScope.Live,
                    true,
                    false,
                    "Audio mode was stored on the host session."));
            }

            if (request.Resources is not null)
            {
                session.ResourceSettings = request.Resources;
                diagnostics.Add(new SettingApplyDiagnosticDto(
                    "resources.mode",
                    SettingApplyScope.RestartRequired,
                    false,
                    true,
                    "Resource mode was stored on the host session and may require session restart."));
            }

            return ValueTask.FromResult(new UpdateSettingsResponse(
                RpcStatus.Ok(),
                HostProtocolMapper.ToSettingsDto(session),
                diagnostics));
        }
    }

    public ValueTask<ValidateSettingsResourcesResponse> ValidateResourcesAsync(
        ValidateSettingsResourcesRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_registry.TryGet(request.SessionId, out _))
            return ValueTask.FromResult(new ValidateSettingsResourcesResponse(HostProtocolMapper.MissingSessionStatus(request.SessionId), []));

        return ValueTask.FromResult(new ValidateSettingsResourcesResponse(
            RpcStatus.Ok(),
            Validate(request.Limiter, request.Display, request.Input, request.Audio, request.Resources)));
    }

    private static SettingsProfileDto[] CreateProfiles(EmulatorRuntimeSession session)
    {
        var currentProfileId = HostProtocolMapper.ToSettingsDto(session).ProfileId;

        var profiles = new List<SettingsProfileDto>
        {
            new(
                MinimalHostArchitectureDescriptor.ArchitectureId,
                "Minimal host",
                "minimal",
                string.Equals(currentProfileId, MinimalHostArchitectureDescriptor.ArchitectureId, StringComparison.OrdinalIgnoreCase),
                true,
                "Host protocol smoke-test profile without C64 runtime devices.")
        };

        profiles.AddRange(C64MachineProfiles.All.Select(profile => new SettingsProfileDto(
            profile.Id,
            profile.DisplayName,
            profile.Family,
            string.Equals(currentProfileId, profile.Id, StringComparison.OrdinalIgnoreCase),
            true,
            $"{profile.VideoStandard} {profile.NominalClockHz / 1_000_000.0:0.000} MHz, {profile.CyclesPerLine}x{profile.RasterLines}, {profile.VicIIModel}/{profile.SidModel}, ROM {profile.RomSet}.")));

        return profiles.ToArray();
    }

    private EmulatorRuntimeSession CreateRestartedSession(
        EmulatorRuntimeSession current,
        string profileId,
        double limiterRatePercent,
        bool limiterEnabled,
        DisplaySettingsDto display,
        InputSettingsDto input,
        AudioSettingsDto audio,
        ResourceSettingsDto resources,
        KeyboardMapDto? selectedKeyboardMap)
    {
        var created = _runtimeFactory.Create(new CreateEmulatorSessionRequest(profileId));
        return new EmulatorRuntimeSession(current.SessionId, created.Architecture, created.Machine)
        {
            PowerState = current.PowerState,
            RunState = EmulatorRunState.Stopped,
            LimiterRatePercent = limiterRatePercent,
            LimiterEnabled = limiterEnabled,
            DisplaySettings = display,
            InputSettings = input,
            AudioSettings = audio,
            ResourceSettings = resources,
            SelectedKeyboardMapId = input.KeyboardMapId,
            SelectedKeyboardMap = selectedKeyboardMap
        };
    }

    private static void AddLimiterDiagnostic(
        UpdateSettingsRequest request,
        ICollection<SettingApplyDiagnosticDto> diagnostics)
    {
        if (request.Limiter is null)
            return;

        diagnostics.Add(new SettingApplyDiagnosticDto(
            "limiter.ratePercent",
            SettingApplyScope.Live,
            true,
            false,
            "Limiter rate was applied to the running host session."));
    }

    private static void AddInputDiagnostics(
        InputSettingsDto previousInput,
        InputSettingsDto currentInput,
        ICollection<SettingApplyDiagnosticDto> diagnostics)
    {
        if (previousInput.PrimaryJoystickPort != currentInput.PrimaryJoystickPort ||
            previousInput.SwapJoystickPorts != currentInput.SwapJoystickPorts)
        {
            diagnostics.Add(new SettingApplyDiagnosticDto(
                "input.joystickRouting",
                SettingApplyScope.Live,
                true,
                false,
                "Joystick port routing was applied to the running host session."));
        }

        if (!string.Equals(previousInput.KeyboardMapId, currentInput.KeyboardMapId, StringComparison.OrdinalIgnoreCase))
        {
            diagnostics.Add(new SettingApplyDiagnosticDto(
                "input.keyboardMapId",
                SettingApplyScope.RestartRequired,
                false,
                true,
                "Keyboard map id was stored; select the map through InputService to validate and apply runtime VKM translation."));
        }
    }

    private static string ResolveProfileId(string profileId)
    {
        return TryResolveProfileId(profileId, out var resolvedProfileId)
            ? resolvedProfileId
            : profileId;
    }

    private static bool TryResolveProfileId(string profileId, out string resolvedProfileId)
    {
        if (string.Equals(profileId, MinimalHostArchitectureDescriptor.ArchitectureId, StringComparison.OrdinalIgnoreCase))
        {
            resolvedProfileId = MinimalHostArchitectureDescriptor.ArchitectureId;
            return true;
        }

        if (C64MachineProfiles.TryResolve(profileId, out var profile))
        {
            resolvedProfileId = profile.Id;
            return true;
        }

        resolvedProfileId = string.Empty;
        return false;
    }

    private static SettingsResourceValidationDto[] Validate(
        LimiterSettingsDto? limiter,
        DisplaySettingsDto? display,
        InputSettingsDto? input,
        AudioSettingsDto? audio,
        ResourceSettingsDto? resourceSettings)
    {
        var resources = new List<SettingsResourceValidationDto>();

        if (limiter is not null &&
            (!double.IsFinite(limiter.RatePercent) || limiter.RatePercent <= 0 || limiter.RatePercent > 1000))
        {
            resources.Add(new SettingsResourceValidationDto(
                "limiter.ratePercent",
                SettingsResourceKind.Display,
                false,
                false,
                "Limiter rate percent must be between 0 and 1000."));
        }

        if (display is not null)
        {
            resources.Add(ValidateKnownValue(
                "display.renderer",
                SettingsResourceKind.Display,
                display.Renderer,
                KnownRenderers,
                true));
            resources.Add(ValidateKnownValue(
                "display.palette",
                SettingsResourceKind.Display,
                display.Palette,
                KnownPalettes,
                true));
            resources.Add(ValidateKnownValue(
                "display.scale",
                SettingsResourceKind.Display,
                display.Scale,
                KnownDisplayScales,
                false));
            resources.Add(ValidateKnownValue(
                "display.cropMode",
                SettingsResourceKind.Display,
                display.CropMode,
                KnownCropModes,
                false));
            resources.Add(ValidateKnownValue(
                "display.aspectMode",
                SettingsResourceKind.Display,
                display.AspectMode,
                KnownAspectModes,
                false));
        }

        if (input is not null)
        {
            var keymapValid = !string.IsNullOrWhiteSpace(input.KeyboardMapId);
            resources.Add(new SettingsResourceValidationDto(
                "input.keyboardMapId",
                SettingsResourceKind.Input,
                keymapValid,
                true,
                keymapValid
                    ? "Keyboard map id is syntactically valid."
                    : "Keyboard map id is required."));

            var joystickValid = input.PrimaryJoystickPort is InputPort.Joystick1 or InputPort.Joystick2;
            resources.Add(new SettingsResourceValidationDto(
                "input.primaryJoystickPort",
                SettingsResourceKind.Input,
                joystickValid,
                true,
                joystickValid
                    ? "Primary joystick port is valid."
                    : "Primary joystick port must be Joystick1 or Joystick2."));

            resources.Add(ValidateKnownValue(
                "input.mode",
                SettingsResourceKind.Input,
                input.Mode,
                KnownInputModes,
                false));
        }

        if (audio is not null)
        {
            resources.Add(ValidateKnownValue(
                "audio.mode",
                SettingsResourceKind.Audio,
                audio.Mode,
                KnownAudioModes,
                false));
        }

        if (resourceSettings is not null)
        {
            resources.Add(ValidateKnownValue(
                "resources.mode",
                SettingsResourceKind.Resource,
                resourceSettings.Mode,
                KnownResourceModes,
                true));
        }

        return resources.ToArray();
    }

    private static SettingsResourceValidationDto ValidateKnownValue(
        string resourceKey,
        SettingsResourceKind kind,
        string value,
        HashSet<string> knownValues,
        bool restartRequired)
    {
        var valid = !string.IsNullOrWhiteSpace(value) && knownValues.Contains(value);
        return new SettingsResourceValidationDto(
            resourceKey,
            kind,
            valid,
            restartRequired,
            valid
                ? $"{resourceKey} is available."
                : $"{resourceKey} must be one of: {string.Join(", ", knownValues.Order(StringComparer.OrdinalIgnoreCase))}.");
    }
}
