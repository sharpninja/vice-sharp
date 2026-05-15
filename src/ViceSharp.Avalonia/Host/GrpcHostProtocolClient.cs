using Google.Protobuf;
using Grpc.Net.Client;
using ViceSharp.Protocol;
using GrpcContracts = ViceSharp.Protocol.Grpc;

namespace ViceSharp.Avalonia.Host;

public sealed class GrpcHostProtocolClient : IHostProtocolClient, IDisposable
{
    private readonly GrpcChannel _channel;
    private readonly GrpcContracts.EmulatorHost.EmulatorHostClient _hostClient;
    private readonly GrpcContracts.MediaService.MediaServiceClient _mediaClient;
    private readonly GrpcContracts.VideoService.VideoServiceClient _videoClient;
    private readonly GrpcContracts.InputService.InputServiceClient _inputClient;
    private readonly GrpcContracts.SettingsService.SettingsServiceClient _settingsClient;
    private readonly GrpcContracts.MonitorService.MonitorServiceClient _monitorClient;
    private string _sessionId;

    public GrpcHostProtocolClient(Uri endpoint, string sessionId = "")
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        _channel = GrpcChannel.ForAddress(endpoint);
        _hostClient = new GrpcContracts.EmulatorHost.EmulatorHostClient(_channel);
        _mediaClient = new GrpcContracts.MediaService.MediaServiceClient(_channel);
        _videoClient = new GrpcContracts.VideoService.VideoServiceClient(_channel);
        _inputClient = new GrpcContracts.InputService.InputServiceClient(_channel);
        _settingsClient = new GrpcContracts.SettingsService.SettingsServiceClient(_channel);
        _monitorClient = new GrpcContracts.MonitorService.MonitorServiceClient(_channel);
        _sessionId = sessionId;
    }

    public string SessionId => _sessionId;

    public async ValueTask<GetEmulatorStatusResponse> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var sessionId = await EnsureSessionAsync(cancellationToken).ConfigureAwait(false);
        var response = await _hostClient.GetStatusAsync(
            new GrpcContracts.SessionRequest { SessionId = sessionId },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return new GetEmulatorStatusResponse(MapStatus(response.Status), MapStatusDto(response.EmulatorStatus));
    }

    public async ValueTask<EmulatorCommandResponse> StartAsync(CancellationToken cancellationToken = default)
        => await SendHostCommandAsync(client => client.StartAsync(CreateSessionRequest(), cancellationToken: cancellationToken), cancellationToken).ConfigureAwait(false);

    public async ValueTask<EmulatorCommandResponse> PauseAsync(CancellationToken cancellationToken = default)
        => await SendHostCommandAsync(client => client.PauseAsync(CreateSessionRequest(), cancellationToken: cancellationToken), cancellationToken).ConfigureAwait(false);

    public async ValueTask<EmulatorCommandResponse> ResumeAsync(CancellationToken cancellationToken = default)
        => await SendHostCommandAsync(client => client.ResumeAsync(CreateSessionRequest(), cancellationToken: cancellationToken), cancellationToken).ConfigureAwait(false);

    public async ValueTask<EmulatorCommandResponse> StepCycleAsync(int cycleCount, CancellationToken cancellationToken = default)
    {
        var sessionId = await EnsureSessionAsync(cancellationToken).ConfigureAwait(false);
        var response = await _hostClient.StepCycleAsync(
            new GrpcContracts.StepCycleRequest { SessionId = sessionId, CycleCount = cycleCount },
            cancellationToken: cancellationToken).ConfigureAwait(false);
        return MapCommand(response);
    }

    public async ValueTask<EmulatorCommandResponse> StepFrameAsync(int frameCount, CancellationToken cancellationToken = default)
    {
        var sessionId = await EnsureSessionAsync(cancellationToken).ConfigureAwait(false);
        var response = await _hostClient.StepFrameAsync(
            new GrpcContracts.StepFrameRequest { SessionId = sessionId, FrameCount = frameCount },
            cancellationToken: cancellationToken).ConfigureAwait(false);
        return MapCommand(response);
    }

    public async ValueTask<EmulatorCommandResponse> RewindCycleAsync(int cycleCount, CancellationToken cancellationToken = default)
    {
        var sessionId = await EnsureSessionAsync(cancellationToken).ConfigureAwait(false);
        var response = await _hostClient.RewindCycleAsync(
            new GrpcContracts.RewindCycleRequest { SessionId = sessionId, CycleCount = cycleCount },
            cancellationToken: cancellationToken).ConfigureAwait(false);
        return MapCommand(response);
    }

    public async ValueTask<EmulatorCommandResponse> RewindFrameAsync(int frameCount, CancellationToken cancellationToken = default)
    {
        var sessionId = await EnsureSessionAsync(cancellationToken).ConfigureAwait(false);
        var response = await _hostClient.RewindFrameAsync(
            new GrpcContracts.RewindFrameRequest { SessionId = sessionId, FrameCount = frameCount },
            cancellationToken: cancellationToken).ConfigureAwait(false);
        return MapCommand(response);
    }

    public async ValueTask<EmulatorCommandResponse> ColdResetAsync(CancellationToken cancellationToken = default)
        => await SendHostCommandAsync(client => client.ColdResetAsync(CreateSessionRequest(), cancellationToken: cancellationToken), cancellationToken).ConfigureAwait(false);

    public async ValueTask<EmulatorCommandResponse> WarmResetAsync(CancellationToken cancellationToken = default)
        => await SendHostCommandAsync(client => client.WarmResetAsync(CreateSessionRequest(), cancellationToken: cancellationToken), cancellationToken).ConfigureAwait(false);

    public async ValueTask<EmulatorCommandResponse> ResetAndAutostartDrive8Async(CancellationToken cancellationToken = default)
    {
        var sessionId = await EnsureSessionAsync(cancellationToken).ConfigureAwait(false);
        return await SendHostCommandAsync(
            client => client.ResetAndAutostartDrive8Async(
                new GrpcContracts.ResetAndAutostartDrive8Request { SessionId = sessionId },
                cancellationToken: cancellationToken),
            cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<EmulatorCommandResponse> SetLimiterRateAsync(double ratePercent, CancellationToken cancellationToken = default)
    {
        var sessionId = await EnsureSessionAsync(cancellationToken).ConfigureAwait(false);
        var response = await _hostClient.SetLimiterRateAsync(
            new GrpcContracts.SetLimiterRateRequest { SessionId = sessionId, LimiterRatePercent = ratePercent },
            cancellationToken: cancellationToken).ConfigureAwait(false);
        return MapCommand(response);
    }

    public async ValueTask<ListSettingsProfilesResponse> ListSettingsProfilesAsync(CancellationToken cancellationToken = default)
    {
        var sessionId = await EnsureSessionAsync(cancellationToken).ConfigureAwait(false);
        var response = await _settingsClient.ListProfilesAsync(
            new GrpcContracts.SessionRequest { SessionId = sessionId },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return new ListSettingsProfilesResponse(
            MapStatus(response.Status),
            response.Profiles.Select(MapSettingsProfile).ToArray());
    }

    public async ValueTask<GetSettingsResponse> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        var sessionId = await EnsureSessionAsync(cancellationToken).ConfigureAwait(false);
        var response = await _settingsClient.GetSettingsAsync(
            new GrpcContracts.SessionRequest { SessionId = sessionId },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return new GetSettingsResponse(MapStatus(response.Status), response.Settings is null ? null : MapSessionSettings(response.Settings));
    }

    public async ValueTask<UpdateSettingsResponse> UpdateSettingsAsync(
        UpdateSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        var sessionId = await EnsureSessionAsync(cancellationToken).ConfigureAwait(false);
        var response = await _settingsClient.UpdateSettingsAsync(
            new GrpcContracts.UpdateSettingsRequest
            {
                SessionId = sessionId,
                Limiter = request.Limiter is null ? null : MapLimiter(request.Limiter),
                Display = request.Display is null ? null : MapDisplay(request.Display),
                Input = request.Input is null ? null : MapInput(request.Input),
                Audio = request.Audio is null ? null : MapAudio(request.Audio),
                Resources = request.Resources is null ? null : MapResources(request.Resources),
                ProfileId = request.ProfileId,
                RestartSession = request.RestartSession
            },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return new UpdateSettingsResponse(
            MapStatus(response.Status),
            response.Settings is null ? null : MapSessionSettings(response.Settings),
            response.Diagnostics.Select(MapSettingDiagnostic).ToArray());
    }

    public async ValueTask<ValidateSettingsResourcesResponse> ValidateSettingsResourcesAsync(
        ValidateSettingsResourcesRequest request,
        CancellationToken cancellationToken = default)
    {
        var sessionId = await EnsureSessionAsync(cancellationToken).ConfigureAwait(false);
        var response = await _settingsClient.ValidateResourcesAsync(
            new GrpcContracts.ValidateSettingsResourcesRequest
            {
                SessionId = sessionId,
                Limiter = request.Limiter is null ? null : MapLimiter(request.Limiter),
                Display = request.Display is null ? null : MapDisplay(request.Display),
                Input = request.Input is null ? null : MapInput(request.Input),
                Audio = request.Audio is null ? null : MapAudio(request.Audio),
                Resources = request.Resources is null ? null : MapResources(request.Resources)
            },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return new ValidateSettingsResourcesResponse(
            MapStatus(response.Status),
            response.Resources.Select(MapResourceValidation).ToArray());
    }

    public async ValueTask<ListMediaResponse> ListMediaAsync(CancellationToken cancellationToken = default)
    {
        var sessionId = await EnsureSessionAsync(cancellationToken).ConfigureAwait(false);
        var response = await _mediaClient.ListMediaAsync(
            new GrpcContracts.SessionRequest { SessionId = sessionId },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return new ListMediaResponse(
            MapStatus(response.Status),
            response.Attachments.Select(MapAttachment).ToArray());
    }

    public async ValueTask<AttachMediaResponse> AttachMediaAsync(
        MediaSlot slot,
        string filePath,
        bool isReadOnly,
        CancellationToken cancellationToken = default)
        => await AttachMediaCoreAsync(slot, filePath, isReadOnly, null, string.Empty, cancellationToken)
            .ConfigureAwait(false);

    public async ValueTask<AttachMediaResponse> AttachMediaAsync(
        MediaSlot slot,
        string filePath,
        bool isReadOnly,
        byte[] payload,
        string displayName,
        CancellationToken cancellationToken = default)
        => await AttachMediaCoreAsync(slot, filePath, isReadOnly, payload, displayName, cancellationToken)
            .ConfigureAwait(false);

    private async ValueTask<AttachMediaResponse> AttachMediaCoreAsync(
        MediaSlot slot,
        string filePath,
        bool isReadOnly,
        byte[]? payload,
        string displayName,
        CancellationToken cancellationToken)
    {
        var sessionId = await EnsureSessionAsync(cancellationToken).ConfigureAwait(false);
        var request = new GrpcContracts.AttachMediaRequest
        {
            SessionId = sessionId,
            Slot = MapSlot(slot),
            FilePath = payload is { Length: > 0 } ? string.Empty : filePath,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? Path.GetFileName(filePath) : displayName,
            IsReadOnly = isReadOnly
        };
        if (payload is { Length: > 0 })
            request.Payload = ByteString.CopyFrom(payload);

        var response = await _mediaClient.AttachMediaAsync(request, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return new AttachMediaResponse(MapStatus(response.Status), response.Attachment is null ? null : MapAttachment(response.Attachment));
    }

    public async ValueTask<DetachMediaResponse> DetachMediaAsync(
        MediaSlot slot,
        CancellationToken cancellationToken = default)
    {
        var sessionId = await EnsureSessionAsync(cancellationToken).ConfigureAwait(false);
        var response = await _mediaClient.EjectMediaAsync(
            new GrpcContracts.DetachMediaRequest { SessionId = sessionId, Slot = MapSlot(slot) },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return new DetachMediaResponse(MapStatus(response.Status), response.Attachment is null ? null : MapAttachment(response.Attachment));
    }

    public async ValueTask<InputCommandResponse> SetKeyStateAsync(
        string key,
        bool isPressed,
        string physicalKey = "",
        string text = "",
        int modifiers = 0,
        CancellationToken cancellationToken = default)
    {
        var sessionId = await EnsureSessionAsync(cancellationToken).ConfigureAwait(false);
        var response = await _inputClient.SetKeyStateAsync(
            new GrpcContracts.SetKeyStateRequest
            {
                SessionId = sessionId,
                Key = key,
                IsPressed = isPressed,
                PhysicalKey = physicalKey,
                Text = text,
                Modifiers = modifiers
            },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return new InputCommandResponse(MapStatus(response.Status), MapInputState(response.InputState));
    }

    public async ValueTask<ListKeyboardMapsResponse> ListKeyboardMapsAsync(CancellationToken cancellationToken = default)
    {
        var sessionId = await EnsureSessionAsync(cancellationToken).ConfigureAwait(false);
        var response = await _inputClient.ListKeyboardMapsAsync(
            new GrpcContracts.SessionRequest { SessionId = sessionId },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return new ListKeyboardMapsResponse(
            MapStatus(response.Status),
            response.KeyboardMaps.Select(MapKeyboardMap).ToArray());
    }

    public async ValueTask<KeyboardMapResponse> SetKeyboardMapAsync(
        string keyboardMapId,
        byte[]? payload = null,
        string displayName = "",
        string sourcePath = "",
        CancellationToken cancellationToken = default)
    {
        var sessionId = await EnsureSessionAsync(cancellationToken).ConfigureAwait(false);
        var request = new GrpcContracts.SetKeyboardMapRequest
        {
            SessionId = sessionId,
            KeyboardMapId = keyboardMapId,
            DisplayName = displayName,
            SourcePath = sourcePath
        };
        if (payload is { Length: > 0 })
            request.Payload = ByteString.CopyFrom(payload);

        var response = await _inputClient.SetKeyboardMapAsync(request, cancellationToken: cancellationToken).ConfigureAwait(false);
        return new KeyboardMapResponse(
            MapStatus(response.Status),
            response.KeyboardMap is null ? null : MapKeyboardMap(response.KeyboardMap),
            MapInputState(response.InputState));
    }

    public async ValueTask<MonitorCommandResponse> ExecuteMonitorCommandAsync(
        string command,
        CancellationToken cancellationToken = default)
    {
        var sessionId = await EnsureSessionAsync(cancellationToken).ConfigureAwait(false);
        var response = await _monitorClient.ExecuteCommandAsync(
            new GrpcContracts.ExecuteMonitorCommandRequest { SessionId = sessionId, Command = command },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return new MonitorCommandResponse(
            MapStatus(response.Status),
            response.Output,
            MapStatusDto(response.EmulatorStatus));
    }

    public async ValueTask<GetVideoFrameResponse> GetFrameAsync(CancellationToken cancellationToken = default)
    {
        var sessionId = await EnsureSessionAsync(cancellationToken).ConfigureAwait(false);
        var response = await _videoClient.GetFrameAsync(
            new GrpcContracts.SessionRequest { SessionId = sessionId },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return new GetVideoFrameResponse(MapStatus(response.Status), response.Frame is null ? null : MapFrame(response.Frame));
    }

    public void Dispose()
    {
        if (!string.IsNullOrWhiteSpace(_sessionId))
        {
            try
            {
                _hostClient.Shutdown(new GrpcContracts.SessionRequest { SessionId = _sessionId });
            }
            catch
            {
                // Shutdown is best-effort; channel disposal and local host disposal clean up the process resources.
            }
        }

        _channel.Dispose();
    }

    private async ValueTask<string> EnsureSessionAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_sessionId))
            return _sessionId;

        var response = await _hostClient.CreateSessionAsync(
            new GrpcContracts.CreateEmulatorSessionRequest
            {
                ArchitectureId = Environment.GetEnvironmentVariable("VICESHARP_ARCHITECTURE") ?? string.Empty
            },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        var status = MapStatus(response.Status);
        if (!status.IsSuccess)
            throw new InvalidOperationException(status.Message);

        _sessionId = response.SessionId;
        var started = await _hostClient.StartAsync(
            new GrpcContracts.SessionRequest { SessionId = _sessionId },
            cancellationToken: cancellationToken).ConfigureAwait(false);
        var startedStatus = MapStatus(started.Status);
        if (!startedStatus.IsSuccess)
            throw new InvalidOperationException(startedStatus.Message);

        return _sessionId;
    }

    private GrpcContracts.SessionRequest CreateSessionRequest()
    {
        return new GrpcContracts.SessionRequest { SessionId = _sessionId };
    }

    private async ValueTask<EmulatorCommandResponse> SendHostCommandAsync(
        Func<GrpcContracts.EmulatorHost.EmulatorHostClient, Grpc.Core.AsyncUnaryCall<GrpcContracts.EmulatorCommandResponse>> command,
        CancellationToken cancellationToken = default)
    {
        await EnsureSessionAsync(cancellationToken).ConfigureAwait(false);
        var response = await command(_hostClient).ConfigureAwait(false);
        return MapCommand(response);
    }

    private static GrpcContracts.MediaSlot MapSlot(MediaSlot slot) => slot switch
    {
        MediaSlot.Drive8 => GrpcContracts.MediaSlot.Drive8,
        MediaSlot.Drive9 => GrpcContracts.MediaSlot.Drive9,
        MediaSlot.Tape => GrpcContracts.MediaSlot.Tape,
        MediaSlot.Cartridge => GrpcContracts.MediaSlot.Cartridge,
        _ => GrpcContracts.MediaSlot.Drive8
    };

    private static MediaSlot MapSlot(GrpcContracts.MediaSlot slot) => slot switch
    {
        GrpcContracts.MediaSlot.Drive8 => MediaSlot.Drive8,
        GrpcContracts.MediaSlot.Drive9 => MediaSlot.Drive9,
        GrpcContracts.MediaSlot.Tape => MediaSlot.Tape,
        GrpcContracts.MediaSlot.Cartridge => MediaSlot.Cartridge,
        _ => MediaSlot.Drive8
    };

    private static RpcStatus MapStatus(GrpcContracts.RpcStatus? status)
    {
        if (status is null)
            return RpcStatus.Unavailable("The host returned no status.");

        return new RpcStatus((RpcStatusCode)(int)status.Code, status.Message);
    }

    private static EmulatorCommandResponse MapCommand(GrpcContracts.EmulatorCommandResponse response)
    {
        return new EmulatorCommandResponse(MapStatus(response.Status), MapStatusDto(response.EmulatorStatus));
    }

    private static MediaAttachmentDto MapAttachment(GrpcContracts.MediaAttachmentDto attachment)
    {
        return new MediaAttachmentDto(
            MapSlot(attachment.Slot),
            attachment.FilePath,
            attachment.DisplayName,
            attachment.IsAttached,
            attachment.IsReadOnly,
            attachment.AppliedToRuntime,
            attachment.Error);
    }

    private static VideoFrameDto MapFrame(GrpcContracts.VideoFrameDto frame)
    {
        return new VideoFrameDto(frame.Width, frame.Height, frame.Cycle, frame.Bgra.ToByteArray());
    }

    private static EmulatorStatusDto? MapStatusDto(GrpcContracts.EmulatorStatusDto? value)
    {
        if (value is null)
            return null;

        var machine = value.MachineState is null
            ? new MachineStateDto(0, 0, 0, 0, 0, 0, value.Cycle)
            : new MachineStateDto(
                (byte)value.MachineState.A,
                (byte)value.MachineState.X,
                (byte)value.MachineState.Y,
                (byte)value.MachineState.S,
                (byte)value.MachineState.P,
                (ushort)value.MachineState.Pc,
                value.MachineState.Cycle);

        return new EmulatorStatusDto(
            value.SessionId,
            value.Architecture,
            (EmulatorRunState)(int)value.RunState,
            value.Cycle,
            machine,
            value.PowerState,
            value.LimiterRatePercent,
            value.MeasuredFps,
            value.FrameCount,
            value.NominalClockHz,
            value.EffectiveClockHz,
            value.EffectiveClockPercent,
            (ushort)value.Pc,
            value.ModelId,
            value.HostAutomationDescription,
            value.HostAutomationActive,
            value.LastHostAutomationError);
    }

    private static InputStateDto? MapInputState(GrpcContracts.InputStateDto? inputState)
    {
        if (inputState is null)
            return null;

        var keys = inputState.Keys
            .Select(key => new KeyStateDto(
                key.Key,
                key.IsPressed,
                key.AppliedToRuntime,
                key.PhysicalKey,
                key.Text,
                key.Modifiers))
            .ToArray();
        var joysticks = inputState.Joysticks
            .Select(joystick => new JoystickPortStateDto(
                (InputPort)(int)joystick.Port,
                new JoystickStateDto((byte)joystick.State.DirectionMask, joystick.State.FireButton, joystick.State.AppliedToRuntime)))
            .ToArray();

        return new InputStateDto(keys, joysticks, inputState.SelectedKeyboardMap is null ? null : MapKeyboardMap(inputState.SelectedKeyboardMap));
    }

    private static KeyboardMapDto MapKeyboardMap(GrpcContracts.KeyboardMapDto map)
    {
        return new KeyboardMapDto(
            map.Id,
            map.DisplayName,
            map.Machine,
            map.Kind,
            map.SourcePath,
            map.IsSelected,
            map.IsBuiltin,
            map.Error);
    }

    private static SettingsProfileDto MapSettingsProfile(GrpcContracts.SettingsProfileDto profile)
    {
        return new SettingsProfileDto(
            profile.Id,
            profile.DisplayName,
            profile.Machine,
            profile.IsCurrent,
            profile.IsAvailable,
            profile.Description);
    }

    private static SessionSettingsDto MapSessionSettings(GrpcContracts.SessionSettingsDto settings)
    {
        return new SessionSettingsDto(
            settings.ProfileId,
            settings.Limiter is null ? new LimiterSettingsDto() : MapLimiter(settings.Limiter),
            settings.Display is null ? new DisplaySettingsDto() : MapDisplay(settings.Display),
            settings.Input is null ? new InputSettingsDto() : MapInput(settings.Input),
            settings.Audio is null ? new AudioSettingsDto() : MapAudio(settings.Audio),
            settings.Resources is null ? new ResourceSettingsDto() : MapResources(settings.Resources));
    }

    private static LimiterSettingsDto MapLimiter(GrpcContracts.LimiterSettingsDto limiter)
    {
        return new LimiterSettingsDto(limiter.RatePercent, limiter.IsEnabled);
    }

    private static DisplaySettingsDto MapDisplay(GrpcContracts.DisplaySettingsDto display)
    {
        var defaults = new DisplaySettingsDto();
        return new DisplaySettingsDto(
            DefaultIfBlank(display.Renderer, defaults.Renderer),
            DefaultIfBlank(display.Palette, defaults.Palette),
            display.ShowBorder,
            display.MaintainAspectRatio,
            DefaultIfBlank(display.Scale, defaults.Scale),
            DefaultIfBlank(display.CropMode, defaults.CropMode),
            DefaultIfBlank(display.AspectMode, defaults.AspectMode));
    }

    private static InputSettingsDto MapInput(GrpcContracts.InputSettingsDto input)
    {
        var defaults = new InputSettingsDto();
        return new InputSettingsDto(
            DefaultIfBlank(input.KeyboardMapId, defaults.KeyboardMapId),
            (InputPort)(int)input.PrimaryJoystickPort,
            input.SwapJoystickPorts,
            DefaultIfBlank(input.Mode, defaults.Mode));
    }

    private static AudioSettingsDto MapAudio(GrpcContracts.AudioSettingsDto audio)
    {
        return new AudioSettingsDto(DefaultIfBlank(audio.Mode, new AudioSettingsDto().Mode));
    }

    private static ResourceSettingsDto MapResources(GrpcContracts.ResourceSettingsDto resources)
    {
        return new ResourceSettingsDto(DefaultIfBlank(resources.Mode, new ResourceSettingsDto().Mode));
    }

    private static SettingApplyDiagnosticDto MapSettingDiagnostic(GrpcContracts.SettingApplyDiagnosticDto diagnostic)
    {
        return new SettingApplyDiagnosticDto(
            diagnostic.Setting,
            (SettingApplyScope)(int)diagnostic.Scope,
            diagnostic.AppliedLive,
            diagnostic.RestartRequired,
            diagnostic.Message);
    }

    private static SettingsResourceValidationDto MapResourceValidation(GrpcContracts.SettingsResourceValidationDto validation)
    {
        return new SettingsResourceValidationDto(
            validation.ResourceKey,
            (SettingsResourceKind)(int)validation.Kind,
            validation.IsValid,
            validation.RestartRequired,
            validation.Message);
    }

    private static GrpcContracts.LimiterSettingsDto MapLimiter(LimiterSettingsDto limiter)
    {
        return new GrpcContracts.LimiterSettingsDto { RatePercent = limiter.RatePercent, IsEnabled = limiter.IsEnabled };
    }

    private static GrpcContracts.DisplaySettingsDto MapDisplay(DisplaySettingsDto display)
    {
        return new GrpcContracts.DisplaySettingsDto
        {
            Renderer = display.Renderer,
            Palette = display.Palette,
            ShowBorder = display.ShowBorder,
            MaintainAspectRatio = display.MaintainAspectRatio,
            Scale = display.Scale,
            CropMode = display.CropMode,
            AspectMode = display.AspectMode
        };
    }

    private static GrpcContracts.InputSettingsDto MapInput(InputSettingsDto input)
    {
        return new GrpcContracts.InputSettingsDto
        {
            KeyboardMapId = input.KeyboardMapId,
            PrimaryJoystickPort = (GrpcContracts.InputPort)(int)input.PrimaryJoystickPort,
            SwapJoystickPorts = input.SwapJoystickPorts,
            Mode = input.Mode
        };
    }

    private static GrpcContracts.AudioSettingsDto MapAudio(AudioSettingsDto audio)
    {
        return new GrpcContracts.AudioSettingsDto { Mode = audio.Mode };
    }

    private static GrpcContracts.ResourceSettingsDto MapResources(ResourceSettingsDto resources)
    {
        return new GrpcContracts.ResourceSettingsDto { Mode = resources.Mode };
    }

    private static string DefaultIfBlank(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }
}
