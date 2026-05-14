using Google.Protobuf;
using Grpc.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using ViceSharp.Host.Runtime;
using ViceSharp.Protocol;
using HostMap = ViceSharp.Host.Services.GrpcHostMapping;
using GrpcContracts = ViceSharp.Protocol.Grpc;

namespace ViceSharp.Host.Services;

public static class GrpcHostServiceAdapters
{
    public static IServiceCollection AddViceSharpGrpcHost(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddGrpc();
        services.AddSingleton<EmulatorRuntimeRegistry>();
        services.AddSingleton<IEmulatorRuntimeFactory, DefaultEmulatorRuntimeFactory>();
        services.AddSingleton<EmulatorHostService>();
        services.AddSingleton<MediaServiceHost>();
        services.AddSingleton<VideoServiceHost>();
        services.AddSingleton<InputServiceHost>();
        services.AddSingleton<MonitorServiceHost>();
        services.AddSingleton<SnapshotServiceHost>();
        services.AddSingleton<CaptureServiceHost>();
        services.AddSingleton<ILocalVideoFrameSource, LocalVideoFrameSource>();
        services.AddSingleton<IEmulatorHost>(provider => provider.GetRequiredService<EmulatorHostService>());
        services.AddSingleton<IMediaService>(provider => provider.GetRequiredService<MediaServiceHost>());
        services.AddSingleton<IVideoService>(provider => provider.GetRequiredService<VideoServiceHost>());
        services.AddSingleton<IInputService>(provider => provider.GetRequiredService<InputServiceHost>());
        services.AddSingleton<IMonitorService>(provider => provider.GetRequiredService<MonitorServiceHost>());
        services.AddSingleton<ISnapshotService>(provider => provider.GetRequiredService<SnapshotServiceHost>());
        services.AddSingleton<ICaptureService>(provider => provider.GetRequiredService<CaptureServiceHost>());
        return services;
    }

    public static IEndpointRouteBuilder MapViceSharpGrpcHost(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGrpcService<GrpcEmulatorHostService>();
        endpoints.MapGrpcService<GrpcMediaServiceHost>();
        endpoints.MapGrpcService<GrpcVideoServiceHost>();
        endpoints.MapGrpcService<GrpcInputServiceHost>();
        endpoints.MapGrpcService<GrpcMonitorServiceHost>();
        endpoints.MapGrpcService<GrpcSnapshotServiceHost>();
        endpoints.MapGrpcService<GrpcCaptureServiceHost>();
        return endpoints;
    }
}

public sealed class GrpcEmulatorHostService : GrpcContracts.EmulatorHost.EmulatorHostBase
{
    private readonly IEmulatorHost _inner;

    public GrpcEmulatorHostService(IEmulatorHost inner)
    {
        _inner = inner;
    }

    public override async Task<GrpcContracts.CreateEmulatorSessionResponse> CreateSession(
        GrpcContracts.CreateEmulatorSessionRequest request,
        ServerCallContext context)
    {
        var response = await _inner.CreateSessionAsync(
            new CreateEmulatorSessionRequest(request.ArchitectureId, request.DisplayName),
            context.CancellationToken).ConfigureAwait(false);

        return new GrpcContracts.CreateEmulatorSessionResponse
        {
            Status = HostMap.Map(response.Status),
            SessionId = response.SessionId,
            EmulatorStatus = HostMap.Map(response.EmulatorStatus)
        };
    }

    public override async Task<GrpcContracts.GetEmulatorStatusResponse> GetStatus(
        GrpcContracts.SessionRequest request,
        ServerCallContext context)
    {
        var response = await _inner.GetStatusAsync(Map(request), context.CancellationToken).ConfigureAwait(false);
        return new GrpcContracts.GetEmulatorStatusResponse
        {
            Status = HostMap.Map(response.Status),
            EmulatorStatus = HostMap.Map(response.EmulatorStatus)
        };
    }

    public override Task<GrpcContracts.EmulatorCommandResponse> Start(
        GrpcContracts.SessionRequest request,
        ServerCallContext context)
        => MapCommandAsync(_inner.StartAsync(Map(request), context.CancellationToken));

    public override Task<GrpcContracts.EmulatorCommandResponse> Pause(
        GrpcContracts.SessionRequest request,
        ServerCallContext context)
        => MapCommandAsync(_inner.PauseAsync(Map(request), context.CancellationToken));

    public override Task<GrpcContracts.EmulatorCommandResponse> Resume(
        GrpcContracts.SessionRequest request,
        ServerCallContext context)
        => MapCommandAsync(_inner.ResumeAsync(Map(request), context.CancellationToken));

    public override Task<GrpcContracts.EmulatorCommandResponse> Reset(
        GrpcContracts.SessionRequest request,
        ServerCallContext context)
        => MapCommandAsync(_inner.ResetAsync(Map(request), context.CancellationToken));

    public override Task<GrpcContracts.EmulatorCommandResponse> ColdReset(
        GrpcContracts.SessionRequest request,
        ServerCallContext context)
        => MapCommandAsync(_inner.ColdResetAsync(Map(request), context.CancellationToken));

    public override Task<GrpcContracts.EmulatorCommandResponse> WarmReset(
        GrpcContracts.SessionRequest request,
        ServerCallContext context)
        => MapCommandAsync(_inner.WarmResetAsync(Map(request), context.CancellationToken));

    public override Task<GrpcContracts.EmulatorCommandResponse> ResetAndAutostartDrive8(
        GrpcContracts.ResetAndAutostartDrive8Request request,
        ServerCallContext context)
        => MapCommandAsync(_inner.ResetAndAutostartDrive8Async(new ResetAndAutostartDrive8Request(request.SessionId), context.CancellationToken));

    public override Task<GrpcContracts.EmulatorCommandResponse> StepCycle(
        GrpcContracts.StepCycleRequest request,
        ServerCallContext context)
        => MapCommandAsync(_inner.StepCycleAsync(new StepCycleRequest(request.SessionId, request.CycleCount), context.CancellationToken));

    public override Task<GrpcContracts.EmulatorCommandResponse> StepFrame(
        GrpcContracts.StepFrameRequest request,
        ServerCallContext context)
        => MapCommandAsync(_inner.StepFrameAsync(new StepFrameRequest(request.SessionId, request.FrameCount), context.CancellationToken));

    public override Task<GrpcContracts.EmulatorCommandResponse> RewindCycle(
        GrpcContracts.RewindCycleRequest request,
        ServerCallContext context)
        => MapCommandAsync(_inner.RewindCycleAsync(new RewindCycleRequest(request.SessionId, request.CycleCount), context.CancellationToken));

    public override Task<GrpcContracts.EmulatorCommandResponse> RewindFrame(
        GrpcContracts.RewindFrameRequest request,
        ServerCallContext context)
        => MapCommandAsync(_inner.RewindFrameAsync(new RewindFrameRequest(request.SessionId, request.FrameCount), context.CancellationToken));

    public override Task<GrpcContracts.EmulatorCommandResponse> SetLimiterRate(
        GrpcContracts.SetLimiterRateRequest request,
        ServerCallContext context)
        => MapCommandAsync(_inner.SetLimiterRateAsync(new SetLimiterRateRequest(request.SessionId, request.LimiterRatePercent), context.CancellationToken));

    public override Task<GrpcContracts.EmulatorCommandResponse> Shutdown(
        GrpcContracts.SessionRequest request,
        ServerCallContext context)
        => MapCommandAsync(_inner.CloseSessionAsync(Map(request), context.CancellationToken));

    private static async Task<GrpcContracts.EmulatorCommandResponse> MapCommandAsync(ValueTask<EmulatorCommandResponse> task)
    {
        var response = await task.ConfigureAwait(false);
        return new GrpcContracts.EmulatorCommandResponse
        {
            Status = HostMap.Map(response.Status),
            EmulatorStatus = HostMap.Map(response.EmulatorStatus)
        };
    }

    private static SessionRequest Map(GrpcContracts.SessionRequest request) => new(request.SessionId);
}

public sealed class GrpcMediaServiceHost : GrpcContracts.MediaService.MediaServiceBase
{
    private readonly IMediaService _inner;

    public GrpcMediaServiceHost(IMediaService inner)
    {
        _inner = inner;
    }

    public override async Task<GrpcContracts.ListMediaResponse> ListMedia(
        GrpcContracts.SessionRequest request,
        ServerCallContext context)
    {
        var response = await _inner.ListMediaAsync(new SessionRequest(request.SessionId), context.CancellationToken).ConfigureAwait(false);
        var result = new GrpcContracts.ListMediaResponse { Status = HostMap.Map(response.Status) };
        result.Attachments.AddRange(response.Attachments.Select(HostMap.Map));
        return result;
    }

    public override async Task<GrpcContracts.AttachMediaResponse> AttachMedia(
        GrpcContracts.AttachMediaRequest request,
        ServerCallContext context)
    {
        var response = await _inner.AttachMediaAsync(
            new AttachMediaRequest(
                request.SessionId,
                HostMap.Map(request.Slot),
                request.FilePath,
                request.DisplayName,
                request.IsReadOnly,
                request.Payload.ToByteArray()),
            context.CancellationToken).ConfigureAwait(false);

        return new GrpcContracts.AttachMediaResponse
        {
            Status = HostMap.Map(response.Status),
            Attachment = HostMap.Map(response.Attachment)
        };
    }

    public override async Task<GrpcContracts.DetachMediaResponse> EjectMedia(
        GrpcContracts.DetachMediaRequest request,
        ServerCallContext context)
    {
        var response = await _inner.DetachMediaAsync(
            new DetachMediaRequest(request.SessionId, HostMap.Map(request.Slot)),
            context.CancellationToken).ConfigureAwait(false);

        return new GrpcContracts.DetachMediaResponse
        {
            Status = HostMap.Map(response.Status),
            Attachment = HostMap.Map(response.Attachment)
        };
    }

    public override async Task WatchMediaStatus(
        GrpcContracts.SessionRequest request,
        IServerStreamWriter<GrpcContracts.MediaStatusEvent> responseStream,
        ServerCallContext context)
    {
        var response = await _inner.ListMediaAsync(new SessionRequest(request.SessionId), context.CancellationToken).ConfigureAwait(false);
        foreach (var attachment in response.Attachments)
        {
            await responseStream.WriteAsync(new GrpcContracts.MediaStatusEvent { Attachment = HostMap.Map(attachment) }).ConfigureAwait(false);
        }
    }
}

public sealed class GrpcVideoServiceHost : GrpcContracts.VideoService.VideoServiceBase
{
    private readonly IVideoService _inner;

    public GrpcVideoServiceHost(IVideoService inner)
    {
        _inner = inner;
    }

    public override async Task<GrpcContracts.GetVideoStatusResponse> GetVideoStatus(
        GrpcContracts.SessionRequest request,
        ServerCallContext context)
    {
        var response = await _inner.GetVideoStatusAsync(new SessionRequest(request.SessionId), context.CancellationToken).ConfigureAwait(false);
        return new GrpcContracts.GetVideoStatusResponse
        {
            Status = HostMap.Map(response.Status),
            VideoStatus = response.VideoStatus is null ? null : new GrpcContracts.VideoStatusDto
            {
                IsAvailable = response.VideoStatus.IsAvailable,
                Width = response.VideoStatus.Width,
                Height = response.VideoStatus.Height,
                Cycle = response.VideoStatus.Cycle
            }
        };
    }

    public override async Task<GrpcContracts.GetVideoFrameResponse> GetFrame(
        GrpcContracts.SessionRequest request,
        ServerCallContext context)
    {
        var response = await _inner.GetFrameAsync(new SessionRequest(request.SessionId), context.CancellationToken).ConfigureAwait(false);
        return Map(response);
    }

    public override async Task WatchFrames(
        GrpcContracts.SessionRequest request,
        IServerStreamWriter<GrpcContracts.GetVideoFrameResponse> responseStream,
        ServerCallContext context)
    {
        var response = await _inner.GetFrameAsync(new SessionRequest(request.SessionId), context.CancellationToken).ConfigureAwait(false);
        await responseStream.WriteAsync(Map(response)).ConfigureAwait(false);
    }

    private static GrpcContracts.GetVideoFrameResponse Map(GetVideoFrameResponse response)
    {
        return new GrpcContracts.GetVideoFrameResponse
        {
            Status = HostMap.Map(response.Status),
            Frame = response.Frame is null ? null : new GrpcContracts.VideoFrameDto
            {
                Width = response.Frame.Width,
                Height = response.Frame.Height,
                Cycle = response.Frame.Cycle,
                Bgra = ByteString.CopyFrom(response.Frame.Bgra)
            }
        };
    }
}

public sealed class GrpcInputServiceHost : GrpcContracts.InputService.InputServiceBase
{
    private readonly IInputService _inner;

    public GrpcInputServiceHost(IInputService inner)
    {
        _inner = inner;
    }

    public override async Task<GrpcContracts.InputCommandResponse> SetKeyState(
        GrpcContracts.SetKeyStateRequest request,
        ServerCallContext context)
    {
        var response = await _inner.SetKeyStateAsync(
            new SetKeyStateRequest(request.SessionId, request.Key, request.IsPressed, request.PhysicalKey, request.Text, request.Modifiers),
            context.CancellationToken).ConfigureAwait(false);

        return new GrpcContracts.InputCommandResponse { Status = HostMap.Map(response.Status), InputState = HostMap.Map(response.InputState) };
    }

    public override async Task<GrpcContracts.InputCommandResponse> SetJoystickState(
        GrpcContracts.SetJoystickStateRequest request,
        ServerCallContext context)
    {
        var response = await _inner.SetJoystickStateAsync(
            new SetJoystickStateRequest(request.SessionId, (InputPort)(int)request.Port, (byte)request.DirectionMask, request.FireButton),
            context.CancellationToken).ConfigureAwait(false);

        return new GrpcContracts.InputCommandResponse { Status = HostMap.Map(response.Status), InputState = HostMap.Map(response.InputState) };
    }

    public override async Task<GrpcContracts.GetInputStateResponse> GetInputState(
        GrpcContracts.SessionRequest request,
        ServerCallContext context)
    {
        var response = await _inner.GetInputStateAsync(new SessionRequest(request.SessionId), context.CancellationToken).ConfigureAwait(false);
        return new GrpcContracts.GetInputStateResponse { Status = HostMap.Map(response.Status), InputState = HostMap.Map(response.InputState) };
    }

    public override async Task<GrpcContracts.ListKeyboardMapsResponse> ListKeyboardMaps(
        GrpcContracts.SessionRequest request,
        ServerCallContext context)
    {
        var response = await _inner.ListKeyboardMapsAsync(new SessionRequest(request.SessionId), context.CancellationToken).ConfigureAwait(false);
        var result = new GrpcContracts.ListKeyboardMapsResponse { Status = HostMap.Map(response.Status) };
        result.KeyboardMaps.AddRange(response.KeyboardMaps.Select(HostMap.Map));
        return result;
    }

    public override async Task<GrpcContracts.KeyboardMapResponse> SetKeyboardMap(
        GrpcContracts.SetKeyboardMapRequest request,
        ServerCallContext context)
    {
        var response = await _inner.SetKeyboardMapAsync(
            new SetKeyboardMapRequest(
                request.SessionId,
                request.KeyboardMapId,
                request.Payload.Length == 0 ? null : request.Payload.ToByteArray(),
                request.DisplayName,
                request.SourcePath),
            context.CancellationToken).ConfigureAwait(false);

        return new GrpcContracts.KeyboardMapResponse
        {
            Status = HostMap.Map(response.Status),
            KeyboardMap = HostMap.Map(response.KeyboardMap),
            InputState = HostMap.Map(response.InputState)
        };
    }
}

public sealed class GrpcMonitorServiceHost : GrpcContracts.MonitorService.MonitorServiceBase
{
    private readonly IMonitorService _inner;

    public GrpcMonitorServiceHost(IMonitorService inner)
    {
        _inner = inner;
    }

    public override async Task<GrpcContracts.MonitorCommandResponse> ExecuteCommand(
        GrpcContracts.ExecuteMonitorCommandRequest request,
        ServerCallContext context)
    {
        var response = await _inner.ExecuteCommandAsync(
            new ExecuteMonitorCommandRequest(request.SessionId, request.Command),
            context.CancellationToken).ConfigureAwait(false);

        return new GrpcContracts.MonitorCommandResponse
        {
            Status = HostMap.Map(response.Status),
            Output = response.Output,
            EmulatorStatus = HostMap.Map(response.EmulatorStatus)
        };
    }
}

public sealed class GrpcSnapshotServiceHost : GrpcContracts.SnapshotService.SnapshotServiceBase
{
    private readonly ISnapshotService _inner;

    public GrpcSnapshotServiceHost(ISnapshotService inner)
    {
        _inner = inner;
    }

    public override async Task<GrpcContracts.CaptureSnapshotResponse> CaptureSnapshot(
        GrpcContracts.SessionRequest request,
        ServerCallContext context)
    {
        var response = await _inner.CaptureSnapshotAsync(new SessionRequest(request.SessionId), context.CancellationToken).ConfigureAwait(false);
        return new GrpcContracts.CaptureSnapshotResponse
        {
            Status = HostMap.Map(response.Status),
            Snapshot = response.Snapshot is null ? null : new GrpcContracts.SnapshotDto
            {
                Format = response.Snapshot.Format,
                Cycle = response.Snapshot.Cycle,
                Payload = ByteString.CopyFrom(response.Snapshot.Payload)
            }
        };
    }

    public override async Task<GrpcContracts.RestoreSnapshotResponse> RestoreSnapshot(
        GrpcContracts.RestoreSnapshotRequest request,
        ServerCallContext context)
    {
        var snapshot = new SnapshotDto(request.Snapshot.Format, request.Snapshot.Cycle, request.Snapshot.Payload.ToByteArray());
        var response = await _inner.RestoreSnapshotAsync(
            new RestoreSnapshotRequest(request.SessionId, snapshot),
            context.CancellationToken).ConfigureAwait(false);
        return new GrpcContracts.RestoreSnapshotResponse
        {
            Status = HostMap.Map(response.Status),
            EmulatorStatus = HostMap.Map(response.EmulatorStatus)
        };
    }
}

public sealed class GrpcCaptureServiceHost : GrpcContracts.CaptureService.CaptureServiceBase
{
    private readonly ICaptureService _inner;

    public GrpcCaptureServiceHost(ICaptureService inner)
    {
        _inner = inner;
    }

    public override async Task<GrpcContracts.StartCaptureResponse> StartCapture(
        GrpcContracts.StartCaptureRequest request,
        ServerCallContext context)
    {
        var response = await _inner.StartCaptureAsync(
            new StartCaptureRequest(request.SessionId, (CaptureKind)(int)request.Kind, request.TargetPath),
            context.CancellationToken).ConfigureAwait(false);
        return new GrpcContracts.StartCaptureResponse { Status = HostMap.Map(response.Status), Capture = Map(response.Capture) };
    }

    public override async Task<GrpcContracts.StopCaptureResponse> StopCapture(
        GrpcContracts.StopCaptureRequest request,
        ServerCallContext context)
    {
        var response = await _inner.StopCaptureAsync(
            new StopCaptureRequest(request.SessionId, request.CaptureId),
            context.CancellationToken).ConfigureAwait(false);
        return new GrpcContracts.StopCaptureResponse { Status = HostMap.Map(response.Status), Capture = Map(response.Capture) };
    }

    public override async Task<GrpcContracts.CaptureFrameResponse> CaptureFrame(
        GrpcContracts.CaptureFrameRequest request,
        ServerCallContext context)
    {
        var response = await _inner.CaptureFrameAsync(
            new CaptureFrameRequest(request.SessionId, request.FilePath),
            context.CancellationToken).ConfigureAwait(false);
        return new GrpcContracts.CaptureFrameResponse
        {
            Status = HostMap.Map(response.Status),
            Artifact = response.Artifact is null ? null : new GrpcContracts.CaptureArtifactDto
            {
                FilePath = response.Artifact.FilePath,
                Format = response.Artifact.Format,
                Cycle = response.Artifact.Cycle
            }
        };
    }

    private static GrpcContracts.CaptureSessionDto? Map(CaptureSessionDto? value)
    {
        if (value is null)
            return null;

        return new GrpcContracts.CaptureSessionDto
        {
            CaptureId = value.CaptureId,
            Kind = (GrpcContracts.CaptureKind)(int)value.Kind,
            TargetPath = value.TargetPath,
            IsActive = value.IsActive
        };
    }
}

internal static class GrpcHostMapping
{
    public static GrpcContracts.RpcStatus Map(RpcStatus status)
        => new() { Code = (GrpcContracts.RpcStatusCode)(int)status.Code, Message = status.Message };

    public static GrpcContracts.EmulatorStatusDto? Map(EmulatorStatusDto? value)
    {
        if (value is null)
            return null;

        return new GrpcContracts.EmulatorStatusDto
        {
            SessionId = value.SessionId,
            Architecture = value.Architecture,
            RunState = (GrpcContracts.EmulatorRunState)(int)value.RunState,
            Cycle = value.Cycle,
            MachineState = new GrpcContracts.MachineStateDto
            {
                A = value.MachineState.A,
                X = value.MachineState.X,
                Y = value.MachineState.Y,
                S = value.MachineState.S,
                P = value.MachineState.P,
                Pc = value.MachineState.Pc,
                Cycle = value.MachineState.Cycle
            },
            PowerState = value.PowerState,
            LimiterRatePercent = value.LimiterRatePercent,
            MeasuredFps = value.MeasuredFps,
            FrameCount = value.FrameCount,
            NominalClockHz = value.NominalClockHz,
            EffectiveClockHz = value.EffectiveClockHz,
            EffectiveClockPercent = value.EffectiveClockPercent,
            Pc = value.Pc,
            ModelId = value.ModelId
        };
    }

    public static GrpcContracts.MediaAttachmentDto? Map(MediaAttachmentDto? value)
    {
        if (value is null)
            return null;

        return new GrpcContracts.MediaAttachmentDto
        {
            Slot = Map(value.Slot),
            FilePath = value.FilePath,
            DisplayName = value.DisplayName,
            IsAttached = value.IsAttached,
            IsReadOnly = value.IsReadOnly,
            AppliedToRuntime = value.AppliedToRuntime,
            Error = value.Error
        };
    }

    public static GrpcContracts.InputStateDto? Map(InputStateDto? value)
    {
        if (value is null)
            return null;

        var result = new GrpcContracts.InputStateDto();
        result.Keys.AddRange(value.Keys.Select(key => new GrpcContracts.KeyStateDto
        {
            Key = key.Key,
            IsPressed = key.IsPressed,
            AppliedToRuntime = key.AppliedToRuntime
        }));
        result.Joysticks.AddRange(value.Joysticks.Select(joystick => new GrpcContracts.JoystickPortStateDto
        {
            Port = (GrpcContracts.InputPort)(int)joystick.Port,
            State = new GrpcContracts.JoystickStateDto
            {
                DirectionMask = joystick.State.DirectionMask,
                FireButton = joystick.State.FireButton,
                AppliedToRuntime = joystick.State.AppliedToRuntime
            }
        }));
        result.SelectedKeyboardMap = Map(value.SelectedKeyboardMap);
        return result;
    }

    public static GrpcContracts.KeyboardMapDto? Map(KeyboardMapDto? value)
    {
        if (value is null)
            return null;

        return new GrpcContracts.KeyboardMapDto
        {
            Id = value.Id,
            DisplayName = value.DisplayName,
            Machine = value.Machine,
            Kind = value.Kind,
            SourcePath = value.SourcePath,
            IsSelected = value.IsSelected,
            IsBuiltin = value.IsBuiltin,
            Error = value.Error
        };
    }

    public static MediaSlot Map(GrpcContracts.MediaSlot slot) => slot switch
    {
        GrpcContracts.MediaSlot.Drive8 => MediaSlot.Drive8,
        GrpcContracts.MediaSlot.Drive9 => MediaSlot.Drive9,
        GrpcContracts.MediaSlot.Tape => MediaSlot.Tape,
        GrpcContracts.MediaSlot.Cartridge => MediaSlot.Cartridge,
        _ => MediaSlot.Drive8
    };

    private static GrpcContracts.MediaSlot Map(MediaSlot slot) => slot switch
    {
        MediaSlot.Drive8 => GrpcContracts.MediaSlot.Drive8,
        MediaSlot.Drive9 => GrpcContracts.MediaSlot.Drive9,
        MediaSlot.Tape => GrpcContracts.MediaSlot.Tape,
        MediaSlot.Cartridge => GrpcContracts.MediaSlot.Cartridge,
        _ => GrpcContracts.MediaSlot.Drive8
    };
}
