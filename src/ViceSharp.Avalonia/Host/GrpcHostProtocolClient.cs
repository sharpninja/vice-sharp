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
    private readonly GrpcContracts.CaptureService.CaptureServiceClient _captureClient;
    private readonly SemaphoreSlim _sessionGate = new(1, 1);
    private string _sessionId;
    private bool _trueDrive;
    private int _trueDriveDevice = 8;
    private string _trueDriveDiskImagePath = string.Empty;

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
        _captureClient = new GrpcContracts.CaptureService.CaptureServiceClient(_channel);
        _sessionId = sessionId;
    }

    public string SessionId => _sessionId;

    public event EventHandler<string>? SessionIdChanged;

    public bool TrueDrive => _trueDrive;

    public ValueTask SetTrueDriveAsync(bool enabled, int driveDevice = 8, string? diskImagePath = null, CancellationToken cancellationToken = default)
    {
        var disk = diskImagePath ?? string.Empty;
        if (_trueDrive == enabled && _trueDriveDevice == driveDevice && _trueDriveDiskImagePath == disk)
            return ValueTask.CompletedTask;

        _trueDrive = enabled;
        _trueDriveDevice = driveDevice;
        _trueDriveDiskImagePath = disk;

        // True-drive is a machine-config choice: drop the current session so the
        // next EnsureSessionAsync rebuilds the rig with the new selection.
        if (!string.IsNullOrWhiteSpace(_sessionId))
        {
            try
            {
                _hostClient.Shutdown(new GrpcContracts.SessionRequest { SessionId = _sessionId });
            }
            catch
            {
                // Best-effort; the new session is created fresh regardless.
            }

            SetSessionId(string.Empty);
        }

        return ValueTask.CompletedTask;
    }

    public async ValueTask<GetEmulatorStatusResponse> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var sessionId = await EnsureSessionAsync(cancellationToken).ConfigureAwait(false);
        var response = await _hostClient.GetStatusAsync(
            new GrpcContracts.SessionRequest { SessionId = sessionId },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        var status = MapStatus(response.Status);
        HealLostSession(status);
        return new GetEmulatorStatusResponse(status, MapStatusDto(response.EmulatorStatus));
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

    public async ValueTask<MonitorRegistersResponse> ReadRegistersAsync(CancellationToken cancellationToken = default)
    {
        var sessionId = await EnsureSessionAsync(cancellationToken).ConfigureAwait(false);
        var response = await _monitorClient.ReadRegistersAsync(
            new GrpcContracts.SessionRequest { SessionId = sessionId },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return new MonitorRegistersResponse(
            MapStatus(response.Status),
            MapMachineState(response.Registers),
            MapStatusDto(response.EmulatorStatus));
    }

    public async ValueTask<MonitorMemoryResponse> ReadMemoryAsync(int address, int length, CancellationToken cancellationToken = default)
    {
        var sessionId = await EnsureSessionAsync(cancellationToken).ConfigureAwait(false);
        var response = await _monitorClient.ReadMemoryAsync(
            new GrpcContracts.MonitorReadMemoryRequest { SessionId = sessionId, Address = (uint)address, Length = (uint)length },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return new MonitorMemoryResponse(
            MapStatus(response.Status),
            (int)response.Address,
            response.Data.ToByteArray(),
            MapStatusDto(response.EmulatorStatus));
    }

    public async ValueTask<MonitorDisassemblyResponse> DisassembleAsync(int address, int count, CancellationToken cancellationToken = default)
    {
        var sessionId = await EnsureSessionAsync(cancellationToken).ConfigureAwait(false);
        var response = await _monitorClient.DisassembleAsync(
            new GrpcContracts.MonitorDisassemblyRequest { SessionId = sessionId, Address = (uint)address, Count = (uint)count },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return new MonitorDisassemblyResponse(
            MapStatus(response.Status),
            response.Lines.Select(MapDisassemblyLine).ToArray(),
            MapStatusDto(response.EmulatorStatus));
    }

    public async ValueTask<GetTickHistoryResponse> GetTickHistoryAsync(CancellationToken cancellationToken = default)
    {
        var sessionId = await EnsureSessionAsync(cancellationToken).ConfigureAwait(false);
        var response = await _monitorClient.GetTickHistoryAsync(
            new GrpcContracts.SessionRequest { SessionId = sessionId },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return new GetTickHistoryResponse(MapStatus(response.Status), response.Ticks.Select(MapTickEntry).ToArray());
    }

    public async ValueTask<MonitorMemoryResponse> ReadMemoryAtTickAsync(int tickIndex, int address, int length, CancellationToken cancellationToken = default)
    {
        var sessionId = await EnsureSessionAsync(cancellationToken).ConfigureAwait(false);
        var response = await _monitorClient.ReadMemoryAtTickAsync(
            new GrpcContracts.ReadMemoryAtTickRequest { SessionId = sessionId, TickIndex = tickIndex, Address = address, Length = length },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return new MonitorMemoryResponse(
            MapStatus(response.Status),
            (int)response.Address,
            response.Data.ToByteArray(),
            MapStatusDto(response.EmulatorStatus));
    }

    public async ValueTask<GetChipStateAtTickResponse> GetChipStateAtTickAsync(int tickIndex, CancellationToken cancellationToken = default)
    {
        var sessionId = await EnsureSessionAsync(cancellationToken).ConfigureAwait(false);
        var response = await _monitorClient.GetChipStateAtTickAsync(
            new GrpcContracts.GetChipStateAtTickRequest { SessionId = sessionId, TickIndex = tickIndex },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return new GetChipStateAtTickResponse(MapStatus(response.Status), response.Chips.Select(MapChipState).ToArray());
    }

    public async ValueTask<GetVideoFrameResponse> GetFrameAsync(CancellationToken cancellationToken = default)
    {
        var sessionId = await EnsureSessionAsync(cancellationToken).ConfigureAwait(false);
        var response = await _videoClient.GetFrameAsync(
            new GrpcContracts.SessionRequest { SessionId = sessionId },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return new GetVideoFrameResponse(MapStatus(response.Status), response.Frame is null ? null : MapFrame(response.Frame));
    }

    public async ValueTask<CaptureFrameResponse> CaptureFrameAsync(
        string filePath,
        string format = "png",
        CancellationToken cancellationToken = default)
    {
        var sessionId = await EnsureSessionAsync(cancellationToken).ConfigureAwait(false);
        var response = await _captureClient.CaptureFrameAsync(
            new GrpcContracts.CaptureFrameRequest { SessionId = sessionId, FilePath = filePath, Format = format },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return new CaptureFrameResponse(
            MapStatus(response.Status),
            response.Artifact is null
                ? null
                : new CaptureArtifactDto(response.Artifact.FilePath, response.Artifact.Format, response.Artifact.Cycle));
    }

    public async ValueTask<GetCaptureCapabilitiesResponse> GetCaptureCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        var sessionId = await EnsureSessionAsync(cancellationToken).ConfigureAwait(false);
        var response = await _captureClient.GetCaptureCapabilitiesAsync(
            new GrpcContracts.SessionRequest { SessionId = sessionId },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        var video = new CaptureVideoFormatDto[response.VideoFormats.Count];
        for (var i = 0; i < video.Length; i++)
        {
            var v = response.VideoFormats[i];
            video[i] = new CaptureVideoFormatDto(
                v.Id, v.Container, v.VideoCodecs.ToArray(), v.AudioCodecs.ToArray(),
                v.RequiresFfmpeg,
                v.SupportsMicrophone);
        }

        return new GetCaptureCapabilitiesResponse(
            MapStatus(response.Status),
            response.ScreenshotFormats.ToArray(),
            response.AudioFormats.ToArray(),
            video);
    }

    public async ValueTask<StartCaptureResponse> StartCaptureAsync(
        CaptureKind kind,
        string targetPath,
        string format = "",
        IReadOnlyDictionary<string, string>? options = null,
        CancellationToken cancellationToken = default,
        bool captureMicrophone = false,
        string microphoneDevice = "",
        string microphoneInputFormat = "")
    {
        var sessionId = await EnsureSessionAsync(cancellationToken).ConfigureAwait(false);
        var request = new GrpcContracts.StartCaptureRequest
        {
            SessionId = sessionId,
            Kind = (GrpcContracts.CaptureKind)(int)kind,
            TargetPath = targetPath,
            Format = format,
            CaptureMicrophone = captureMicrophone,
            MicrophoneDevice = microphoneDevice,
            MicrophoneInputFormat = microphoneInputFormat
        };
        if (options is not null)
            foreach (var pair in options)
                request.Options[pair.Key] = pair.Value;

        var response = await _captureClient.StartCaptureAsync(request, cancellationToken: cancellationToken).ConfigureAwait(false);
        return new StartCaptureResponse(MapStatus(response.Status), MapCapture(response.Capture));
    }

    public async ValueTask<StopCaptureResponse> StopCaptureAsync(string captureId, CancellationToken cancellationToken = default)
    {
        var sessionId = await EnsureSessionAsync(cancellationToken).ConfigureAwait(false);
        var response = await _captureClient.StopCaptureAsync(
            new GrpcContracts.StopCaptureRequest { SessionId = sessionId, CaptureId = captureId },
            cancellationToken: cancellationToken).ConfigureAwait(false);
        return new StopCaptureResponse(MapStatus(response.Status), MapCapture(response.Capture));
    }

    public async ValueTask<ListCapturesResponse> ListCapturesAsync(CancellationToken cancellationToken = default)
    {
        var sessionId = await EnsureSessionAsync(cancellationToken).ConfigureAwait(false);
        var response = await _captureClient.ListCapturesAsync(
            new GrpcContracts.SessionRequest { SessionId = sessionId },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        var captures = new CaptureSessionDto[response.Captures.Count];
        for (var i = 0; i < captures.Length; i++)
            captures[i] = MapCapture(response.Captures[i])!;

        return new ListCapturesResponse(MapStatus(response.Status), captures);
    }

    private static CaptureSessionDto? MapCapture(GrpcContracts.CaptureSessionDto? capture)
        => capture is null
            ? null
            : new CaptureSessionDto(capture.CaptureId, (CaptureKind)(int)capture.Kind, capture.TargetPath, capture.IsActive);

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
        _sessionGate.Dispose();
    }

    private async ValueTask<string> EnsureSessionAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_sessionId))
            return _sessionId;

        await _sessionGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!string.IsNullOrWhiteSpace(_sessionId))
                return _sessionId;

            var response = await _hostClient.CreateSessionAsync(
                new GrpcContracts.CreateEmulatorSessionRequest
                {
                    ArchitectureId = Environment.GetEnvironmentVariable("VICESHARP_ARCHITECTURE") ?? string.Empty,
                    TrueDrive = _trueDrive,
                    TrueDriveDevice = _trueDriveDevice,
                    TrueDriveDiskImagePath = _trueDriveDiskImagePath
                },
                cancellationToken: cancellationToken).ConfigureAwait(false);

            var status = MapStatus(response.Status);
            if (!status.IsSuccess)
                throw new InvalidOperationException(status.Message);

            SetSessionId(response.SessionId);
            var started = await _hostClient.StartAsync(
                new GrpcContracts.SessionRequest { SessionId = response.SessionId },
                cancellationToken: cancellationToken).ConfigureAwait(false);
            var startedStatus = MapStatus(started.Status);
            if (!startedStatus.IsSuccess)
                throw new InvalidOperationException(startedStatus.Message);

            return response.SessionId;
        }
        finally
        {
            _sessionGate.Release();
        }
    }

    private void SetSessionId(string sessionId)
    {
        if (StringComparer.Ordinal.Equals(_sessionId, sessionId))
            return;

        _sessionId = sessionId;
        SessionIdChanged?.Invoke(this, sessionId);
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
        var mapped = MapCommand(response);
        HealLostSession(mapped.Status);
        return mapped;
    }

    /// <summary>
    /// TR-HOST-SESSIONHEAL-001: true when a host response means the client's
    /// CURRENT session no longer exists on the host (externally shut down or
    /// the host recreated its registry) - a NotFound status naming this
    /// client's session id. Pure classification, unit-tested directly.
    /// </summary>
    public static bool IndicatesLostSession(RpcStatus? status, string sessionId)
    {
        return status is { Code: RpcStatusCode.NotFound }
            && !string.IsNullOrWhiteSpace(sessionId)
            && status.Message.Contains($"Emulator session '{sessionId}'", StringComparison.Ordinal);
    }

    // Drop the cached session id when the host says it is gone, so the next
    // command's EnsureSessionAsync creates a fresh session instead of every
    // later call (and the status bar) wedging on the dead id forever.
    private void HealLostSession(RpcStatus status)
    {
        if (IndicatesLostSession(status, _sessionId))
            SetSessionId(string.Empty);
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

        var perCpuRates = value.PerCpuRates.Count == 0
            ? (IReadOnlyList<PerCpuRateDto>)Array.Empty<PerCpuRateDto>()
            : value.PerCpuRates
                .Select(r => new PerCpuRateDto(r.Label, r.EffectiveClockHz, r.EffectiveClockPercent))
                .ToArray();

        var iecBusLines = value.IecBusLines.Count == 0
            ? (IReadOnlyList<IecBusLineDto>)Array.Empty<IecBusLineDto>()
            : value.IecBusLines
                .Select(l => new IecBusLineDto(l.Signal, l.IsHigh, l.Pullers))
                .ToArray();

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
            value.LastHostAutomationError,
            value.IecBusActive,
            value.IecBusTransitionCount,
            value.IecBusActivityState)
        {
            PerCpuRates = perCpuRates,
            IecBusLines = iecBusLines,
        };
    }

    private static MachineStateDto? MapMachineState(GrpcContracts.MachineStateDto? value)
        => value is null
            ? null
            : new MachineStateDto(
                (byte)value.A, (byte)value.X, (byte)value.Y, (byte)value.S, (byte)value.P, (ushort)value.Pc, value.Cycle);

    private static MonitorDisassemblyLineDto MapDisassemblyLine(GrpcContracts.MonitorDisassemblyLineDto value)
        => new((int)value.Address, value.InstructionBytes.ToByteArray(), value.Text, (int)value.Length, (int)value.NextAddress);

    private static ChipStateDto MapChipState(GrpcContracts.ChipStateDto value)
        => new(value.ChipName, value.Fields.Select(f => new ChipStateFieldDto(f.Name, f.Value, f.Width)).ToArray());

    private static TickHistoryEntryDto MapTickEntry(GrpcContracts.TickHistoryEntryDto value)
        => new(
            value.Index,
            (int)value.InstructionAddress,
            (int)value.Opcode,
            (int)value.A, (int)value.X, (int)value.Y, (int)value.S, (int)value.P,
            (int)value.Pc,
            value.WriteCount);

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
        return new LimiterSettingsDto(
            limiter.RatePercent,
            limiter.IsEnabled,
            DefaultIfBlank(limiter.PacingStrategy, new LimiterSettingsDto().PacingStrategy));
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
        return new GrpcContracts.LimiterSettingsDto { RatePercent = limiter.RatePercent, IsEnabled = limiter.IsEnabled, PacingStrategy = limiter.PacingStrategy };
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
