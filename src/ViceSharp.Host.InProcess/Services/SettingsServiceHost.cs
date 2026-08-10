using ViceSharp.Abstractions;
using ViceSharp.Architectures.C64;
using ViceSharp.Architectures.Vic20;
using ViceSharp.Host.Runtime;
using ViceSharp.Host.Startup;
using ViceSharp.Protocol;
using ViceSharp.RomFetch;

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
    private readonly EmulationPumpService? _pump;

    public SettingsServiceHost(EmulatorRuntimeRegistry registry)
        : this(registry, new DefaultEmulatorRuntimeFactory(), null)
    {
    }

    public SettingsServiceHost(EmulatorRuntimeRegistry registry, IEmulatorRuntimeFactory runtimeFactory)
        : this(registry, runtimeFactory, null)
    {
    }

    // DI resolves this constructor (all parameters are registered singletons), wiring the
    // emulation pump so a pacing-strategy change applies live to the global gate.
    public SettingsServiceHost(EmulatorRuntimeRegistry registry, IEmulatorRuntimeFactory runtimeFactory, EmulationPumpService? pump)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(runtimeFactory);
        _registry = registry;
        _runtimeFactory = runtimeFactory;
        _pump = pump;
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

            // The pacing strategy is global (the pump owns the one gate) and applies live,
            // independent of any session restart, so handle it up front - it then carries
            // through both the restart and the live branch below.
            if (request.Limiter is not null)
                ApplyPacingStrategy(session, request.Limiter.PacingStrategy, diagnostics);

            var currentProfileId = HostProtocolMapper.ToSettingsDto(session).ProfileId;
            var requestedProfileId = string.IsNullOrWhiteSpace(request.ProfileId)
                ? currentProfileId
                : ResolveProfileId(request.ProfileId);
            var profileChanged = !string.Equals(requestedProfileId, currentProfileId, StringComparison.OrdinalIgnoreCase);

            // VIC-20 system RAM (xvic -memory): validate and detect change.
            string? requestedMemorySpec = null;
            var memoryChanged = false;
            if (request.Vic20MemorySpec is not null)
            {
                if (!ViceSharp.Core.Vic20.Vic20MemoryLayout.TryParseMemorySpec(request.Vic20MemorySpec, out var parsedBlocks))
                {
                    return ValueTask.FromResult(new UpdateSettingsResponse(
                        RpcStatus.InvalidArgument($"Unsupported VIC-20 memory spec '{request.Vic20MemorySpec}'."),
                        null,
                        []));
                }

                requestedMemorySpec = ViceSharp.Core.Vic20.Vic20MemoryLayout.FormatMemorySpec(parsedBlocks);
                memoryChanged = !string.Equals(
                    requestedMemorySpec,
                    string.IsNullOrWhiteSpace(session.Vic20MemorySpec) ? "none" : session.Vic20MemorySpec,
                    StringComparison.OrdinalIgnoreCase);
            }

            var restartRelevant = profileChanged || memoryChanged
                || request.Display is not null || request.Input is not null || request.Resources is not null;

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
                var memorySpecForRestart = requestedMemorySpec
                    ?? (string.IsNullOrWhiteSpace(session.Vic20MemorySpec) ? "none" : session.Vic20MemorySpec);

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
                        selectedKeyboardMap,
                        memorySpecForRestart);
                }
                catch (InvalidOperationException ex)
                {
                    return ValueTask.FromResult(new UpdateSettingsResponse(
                        RpcStatus.FailedPrecondition(ex.Message),
                        HostProtocolMapper.ToSettingsDto(session),
                        diagnostics));
                }

                // PAL/NTSC (and other restarts) must not wipe expansion cart / uIEC selection.
                // Prefer request values when the client sent them; else keep session values.
                var expansionKind = request.Vic20ExpansionCartKind ?? restarted.Vic20ExpansionCartKind;
                var expansionWriteBack = request.Vic20ExpansionWriteBack ?? restarted.Vic20ExpansionWriteBack;
                var expansionPreset = request.Vic20ExpansionConfigPreset ?? restarted.Vic20ExpansionConfigPreset;
                ApplyVic20ExpansionSettings(
                    restarted,
                    new UpdateSettingsRequest(
                        request.SessionId,
                        Vic20ExpansionCartKind: expansionKind,
                        Vic20ExpansionWriteBack: expansionWriteBack,
                        Vic20ExpansionConfigPreset: expansionPreset),
                    diagnostics);

                if (request.FileSystemIecRootPath is not null || request.FileSystemIecUnit is not null)
                {
                    ApplyFileSystemIecSettings(
                        restarted,
                        request.FileSystemIecRootPath ?? restarted.FileSystemIecRootPath,
                        request.FileSystemIecUnit ?? restarted.FileSystemIecUnit,
                        diagnostics);
                }
                else if (!string.IsNullOrWhiteSpace(restarted.FileSystemIecRootPath)
                         || restarted.FileSystemIecUnit is >= 8 and <= 11)
                {
                    ApplyFileSystemIecSettings(
                        restarted,
                        restarted.FileSystemIecRootPath,
                        restarted.FileSystemIecUnit,
                        diagnostics);
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

                if (memoryChanged)
                {
                    diagnostics.Add(new SettingApplyDiagnosticDto(
                        "vic20.memory",
                        SettingApplyScope.RestartRequired,
                        true,
                        false,
                        $"VIC-20 memory '{memorySpecForRestart}' was applied by restarting the host session."));
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
                restarted.PublishWarpModeStatus();
                return ValueTask.FromResult(new UpdateSettingsResponse(
                    RpcStatus.Ok(),
                    HostProtocolMapper.ToSettingsDto(restarted),
                    diagnostics));
            }

            if (request.Limiter is not null)
            {
                session.SetLimiter(request.Limiter.RatePercent, request.Limiter.IsEnabled);
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

            if (memoryChanged && requestedMemorySpec is not null)
            {
                // Stage for next restart; do not mutate machine until RestartSession.
                session.Vic20MemorySpec = requestedMemorySpec;
                diagnostics.Add(new SettingApplyDiagnosticDto(
                    "vic20.memory",
                    SettingApplyScope.RestartRequired,
                    false,
                    true,
                    $"VIC-20 memory '{requestedMemorySpec}' is staged and requires session restart."));
            }

            if (request.FileSystemIecRootPath is not null || request.FileSystemIecUnit is not null)
            {
                ApplyFileSystemIecSettings(session, request.FileSystemIecRootPath, request.FileSystemIecUnit, diagnostics);
            }

            if (request.Vic20ExpansionWriteBack is not null
                || request.Vic20ExpansionConfigPreset is not null
                || request.Vic20ExpansionCartKind is not null)
            {
                ApplyVic20ExpansionSettings(session, request, diagnostics);
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

        // Iteration 2: VIC-20 PAL/NTSC for Avalonia + UWP pickers.
        var vic20RomsReady = IsVic20RomSetComplete();
        profiles.AddRange(Vic20MachineProfiles.All.Select(profile => new SettingsProfileDto(
            profile.Id,
            profile.DisplayName,
            profile.Family,
            string.Equals(currentProfileId, profile.Id, StringComparison.OrdinalIgnoreCase),
            vic20RomsReady,
            $"{profile.VideoStandard} {profile.NominalClockHz / 1_000_000.0:0.000} MHz, {profile.CyclesPerLine}x{profile.RasterLines}, {profile.VicIIModel}, drive8={profile.DefaultDriveModel}, ROM {profile.RomSet}.")));

        return profiles.ToArray();
    }

    private static bool IsVic20RomSetComplete()
    {
        foreach (var root in ViceDataPathResolver.FindDataRoots())
        {
            if (Vic20RomBootstrap.IsComplete(root))
                return true;
        }

        return false;
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
        KeyboardMapDto? selectedKeyboardMap,
        string vic20MemorySpec = "none")
    {
        // Before building a VIC-20 session, ensure ROMs under dataRoot/VIC20 (download if needed).
        if (Vic20MachineProfiles.TryResolve(profileId, out _))
        {
            foreach (var root in ViceDataPathResolver.FindDataRoots())
            {
                Vic20RomBootstrap.Ensure(root);
                if (Vic20RomBootstrap.IsComplete(root))
                    break;
            }
        }

        var created = _runtimeFactory.Create(new CreateEmulatorSessionRequest(
            profileId,
            Vic20MemorySpec: Vic20MachineProfiles.TryResolve(profileId, out _) ? vic20MemorySpec : string.Empty));
        return new EmulatorRuntimeSession(current.SessionId, created.Architecture, created.Machine)
        {
            PowerState = current.PowerState,
            // Preserve the prior run state across the restart. The video frame
            // source only advances the machine (RunFrame) while Running, so
            // forcing Stopped here would blank the emulator display after an
            // "Apply + Restart" of a running machine until the user manually
            // resumed. A paused/stopped session likewise stays as it was.
            RunState = current.RunState,
            LimiterRatePercent = limiterRatePercent,
            LimiterEnabled = limiterEnabled,
            PacingStrategy = current.PacingStrategy,
            DisplaySettings = display,
            InputSettings = input,
            AudioSettings = audio,
            ResourceSettings = resources,
            SelectedKeyboardMapId = input.KeyboardMapId,
            SelectedKeyboardMap = selectedKeyboardMap,
            Vic20MemorySpec = created.Vic20MemorySpec,
            // Timing-only profile switches (PAL/NTSC) must not reset cart/uIEC UI state.
            FileSystemIecRootPath = current.FileSystemIecRootPath,
            FileSystemIecUnit = current.FileSystemIecUnit,
            Vic20ExpansionCartKind = current.Vic20ExpansionCartKind,
            Vic20ExpansionWriteBack = current.Vic20ExpansionWriteBack,
            Vic20ExpansionConfigPreset = current.Vic20ExpansionConfigPreset,
        };
    }

    // Apply a requested pacing strategy to the global emulation pump (live) and mirror it
    // on the session so GetSettings round-trips it. No-op when unchanged.
    private void ApplyPacingStrategy(EmulatorRuntimeSession session, string? requestedStrategy, List<SettingApplyDiagnosticDto> diagnostics)
    {
        var strategyId = EmulationGateStrategies.Normalize(requestedStrategy);
        if (string.Equals(session.PacingStrategy, strategyId, StringComparison.Ordinal))
            return;

        session.PacingStrategy = strategyId;
        _pump?.SetStrategy(strategyId);
        session.PublishWarpModeStatus();
        diagnostics.Add(new SettingApplyDiagnosticDto(
            "limiter.pacingStrategy",
            SettingApplyScope.Live,
            true,
            false,
            $"Pacing strategy switched to '{EmulationGateStrategies.DisplayName(strategyId)}'."));
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

    private static void ApplyFileSystemIecSettings(
        EmulatorRuntimeSession session,
        string? rootPath,
        int? unit,
        List<SettingApplyDiagnosticDto> diagnostics)
    {
        var fs = session.Machine.Devices.GetAll<ViceSharp.Core.Iec.FileSystemIecDevice>().FirstOrDefault();
        if (fs is null)
        {
            diagnostics.Add(new SettingApplyDiagnosticDto(
                "iec.filesystem",
                SettingApplyScope.Live,
                false,
                false,
                "No filesystem IEC device on this machine."));
            return;
        }

        // True-drive claims unit 8 when the session is a true-drive rig.
        int? reservedTrueDrive = FindTrueDriveReservedUnit(session);

        try
        {
            if (unit is not null)
            {
                fs.SetUnitNumber(unit.Value, reservedTrueDrive);
                session.FileSystemIecUnit = unit.Value;
            }
            else if (session.FileSystemIecUnit != fs.UnitNumber)
            {
                fs.SetUnitNumber(session.FileSystemIecUnit, reservedTrueDrive);
            }

            if (rootPath is not null)
                session.FileSystemIecRootPath = rootPath;

            if (!string.IsNullOrWhiteSpace(session.FileSystemIecRootPath))
                fs.AttachDirectory(session.FileSystemIecRootPath);
            else
                fs.Detach();

            diagnostics.Add(new SettingApplyDiagnosticDto(
                "iec.filesystem",
                SettingApplyScope.Live,
                true,
                false,
                string.IsNullOrWhiteSpace(session.FileSystemIecRootPath)
                    ? $"Filesystem IEC unit {fs.UnitNumber} detached."
                    : $"Filesystem IEC unit {fs.UnitNumber} root '{session.FileSystemIecRootPath}'."));
        }
        catch (Exception ex)
        {
            diagnostics.Add(new SettingApplyDiagnosticDto(
                "iec.filesystem",
                SettingApplyScope.Live,
                false,
                false,
                ex.Message));
        }
    }

    /// <summary>Unit reserved by true-drive (if any); used to prevent fsdevice double-claim.</summary>
    private static int? FindTrueDriveReservedUnit(EmulatorRuntimeSession session)
    {
        // True-drive C64 rig mounts D64 on emulated 1541 as unit 8 by default.
        if (session.Machine is ViceSharp.Core.CoordinatorMachine)
            return 8;
        return null;
    }

    private static void ApplyVic20ExpansionSettings(
        EmulatorRuntimeSession session,
        UpdateSettingsRequest request,
        List<SettingApplyDiagnosticDto> diagnostics)
    {
        if (request.Vic20ExpansionWriteBack is not null)
            session.Vic20ExpansionWriteBack = request.Vic20ExpansionWriteBack.Value;
        if (request.Vic20ExpansionConfigPreset is not null)
            session.Vic20ExpansionConfigPreset = request.Vic20ExpansionConfigPreset;
        if (request.Vic20ExpansionCartKind is not null)
            session.Vic20ExpansionCartKind = request.Vic20ExpansionCartKind;

        var port = session.Machine.Devices.GetAll<ViceSharp.Core.Vic20.IVic20ExpansionCartPort>().FirstOrDefault();
        if (port is null)
            return;

        port.WriteBack = session.Vic20ExpansionWriteBack;
        try
        {
            if (!string.IsNullOrWhiteSpace(session.Vic20ExpansionConfigPreset)
                && port.AttachedKind != ViceSharp.Core.Vic20.Vic20ExpansionCartKind.None)
            {
                port.ApplyConfigPreset(session.Vic20ExpansionConfigPreset);
            }

            diagnostics.Add(new SettingApplyDiagnosticDto(
                "vic20.expansion",
                SettingApplyScope.Live,
                true,
                false,
                $"VIC-20 expansion '{session.Vic20ExpansionCartKind}' preset '{session.Vic20ExpansionConfigPreset}' writeBack={session.Vic20ExpansionWriteBack}."));
        }
        catch (Exception ex)
        {
            diagnostics.Add(new SettingApplyDiagnosticDto(
                "vic20.expansion",
                SettingApplyScope.Live,
                false,
                false,
                ex.Message));
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

        if (Vic20MachineProfiles.TryResolve(profileId, out var vic20Profile))
        {
            resolvedProfileId = vic20Profile.Id;
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
            (!double.IsFinite(limiter.RatePercent) || limiter.RatePercent < 0 || limiter.RatePercent > 1000))
        {
            resources.Add(new SettingsResourceValidationDto(
                "limiter.ratePercent",
                SettingsResourceKind.Display,
                false,
                false,
                "Limiter rate percent must be between 0 and 1000; 0 enters warp mode."));
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
