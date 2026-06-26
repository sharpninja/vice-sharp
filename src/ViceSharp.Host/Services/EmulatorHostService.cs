using ViceSharp.Abstractions;
using ViceSharp.Chips.IEC;
using ViceSharp.Core;
using ViceSharp.Host.Runtime;
using ViceSharp.Protocol;

namespace ViceSharp.Host.Services;

public sealed class EmulatorHostService : IEmulatorHost
{
    private readonly EmulatorRuntimeRegistry _registry;
    private readonly IEmulatorRuntimeFactory _runtimeFactory;

    public EmulatorHostService()
        : this(new EmulatorRuntimeRegistry(), new DefaultEmulatorRuntimeFactory())
    {
    }

    public EmulatorHostService(EmulatorRuntimeRegistry registry, IEmulatorRuntimeFactory runtimeFactory)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(runtimeFactory);

        _registry = registry;
        _runtimeFactory = runtimeFactory;
    }

    public ValueTask<CreateEmulatorSessionResponse> CreateSessionAsync(
        CreateEmulatorSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var session = _runtimeFactory.Create(request);
            _registry.Add(session);

            lock (session.SyncRoot)
            {
                return ValueTask.FromResult(new CreateEmulatorSessionResponse(
                    RpcStatus.Ok(),
                    session.SessionId,
                    HostProtocolMapper.ToStatusDto(session)));
            }
        }
        catch (InvalidOperationException ex)
        {
            return ValueTask.FromResult(new CreateEmulatorSessionResponse(
                RpcStatus.InvalidArgument(ex.Message),
                string.Empty,
                null));
        }
    }

    public ValueTask<GetEmulatorStatusResponse> GetStatusAsync(
        SessionRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_registry.TryGet(request.SessionId, out var session))
            return ValueTask.FromResult(new GetEmulatorStatusResponse(HostProtocolMapper.MissingSessionStatus(request.SessionId), null));

        lock (session.SyncRoot)
        {
            return ValueTask.FromResult(new GetEmulatorStatusResponse(RpcStatus.Ok(), HostProtocolMapper.ToStatusDto(session)));
        }
    }

    public ValueTask<EmulatorCommandResponse> StartAsync(
        SessionRequest request,
        CancellationToken cancellationToken = default)
    {
        return SetRunStateAsync(request, EmulatorRunState.Running, cancellationToken);
    }

    public ValueTask<EmulatorCommandResponse> PauseAsync(
        SessionRequest request,
        CancellationToken cancellationToken = default)
    {
        return SetRunStateAsync(request, EmulatorRunState.Paused, cancellationToken);
    }

    public ValueTask<EmulatorCommandResponse> ResumeAsync(
        SessionRequest request,
        CancellationToken cancellationToken = default)
    {
        return SetRunStateAsync(request, EmulatorRunState.Running, cancellationToken);
    }

    public ValueTask<EmulatorCommandResponse> ResetAsync(
        SessionRequest request,
        CancellationToken cancellationToken = default)
        => ResetAsync(new ResetRequest(request.SessionId, ResetKind.Warm), cancellationToken);

    public ValueTask<EmulatorCommandResponse> ColdResetAsync(
        SessionRequest request,
        CancellationToken cancellationToken = default)
        => ResetAsync(new ResetRequest(request.SessionId, ResetKind.Cold), cancellationToken);

    public ValueTask<EmulatorCommandResponse> WarmResetAsync(
        SessionRequest request,
        CancellationToken cancellationToken = default)
        => ResetAsync(new ResetRequest(request.SessionId, ResetKind.Warm), cancellationToken);

    public ValueTask<EmulatorCommandResponse> ResetAndAutostartDrive8Async(
        ResetAndAutostartDrive8Request request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_registry.TryGet(request.SessionId, out var session))
            return ValueTask.FromResult(new EmulatorCommandResponse(HostProtocolMapper.MissingSessionStatus(request.SessionId), null));

        lock (session.SyncRoot)
        {
            return ValueTask.FromResult(ExecuteResetAndAutostartDrive8(session));
        }
    }

    public ValueTask<EmulatorCommandResponse> ResetAsync(
        ResetRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_registry.TryGet(request.SessionId, out var session))
            return ValueTask.FromResult(new EmulatorCommandResponse(HostProtocolMapper.MissingSessionStatus(request.SessionId), null));

        lock (session.SyncRoot)
        {
            if (request.Kind == ResetKind.ResetAndAutostartDrive8)
            {
                return ValueTask.FromResult(ExecuteResetAndAutostartDrive8(session));
            }

            session.Machine.Reset();
            // A reset reboots the machine and keeps it running, exactly like a real
            // C64 reset boots straight into a running BASIC. Forcing Stopped here
            // wedged the emulator at the reset vector (the pump only advances while
            // Running), so neither Cold nor Warm reset appeared to do anything until
            // the user manually pressed Resume.
            session.RunState = EmulatorRunState.Running;
            session.PowerState = "On";
            session.ResetPerformanceCounters();
            session.ClearHostKeyboardAutomation();
            return ValueTask.FromResult(new EmulatorCommandResponse(RpcStatus.Ok(), HostProtocolMapper.ToStatusDto(session)));
        }
    }

    public ValueTask<EmulatorCommandResponse> StepCycleAsync(
        StepCycleRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (request.CycleCount <= 0)
        {
            return ValueTask.FromResult(new EmulatorCommandResponse(
                RpcStatus.InvalidArgument("CycleCount must be greater than zero."),
                null));
        }

        if (!_registry.TryGet(request.SessionId, out var session))
            return ValueTask.FromResult(new EmulatorCommandResponse(HostProtocolMapper.MissingSessionStatus(request.SessionId), null));

        lock (session.SyncRoot)
        {
            for (var i = 0; i < request.CycleCount; i++)
                session.Machine.Clock.Step();

            return ValueTask.FromResult(new EmulatorCommandResponse(RpcStatus.Ok(), HostProtocolMapper.ToStatusDto(session)));
        }
    }

    public ValueTask<EmulatorCommandResponse> StepFrameAsync(
        StepFrameRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (request.FrameCount <= 0)
        {
            return ValueTask.FromResult(new EmulatorCommandResponse(
                RpcStatus.InvalidArgument("FrameCount must be greater than zero."),
                null));
        }

        if (!_registry.TryGet(request.SessionId, out var session))
            return ValueTask.FromResult(new EmulatorCommandResponse(HostProtocolMapper.MissingSessionStatus(request.SessionId), null));

        lock (session.SyncRoot)
        {
            for (var frame = 0; frame < request.FrameCount; frame++)
            {
                session.Machine.RunFrame();
                session.RecordFrame();
                session.AdvanceHostAutomationFrame();
            }

            return ValueTask.FromResult(new EmulatorCommandResponse(RpcStatus.Ok(), HostProtocolMapper.ToStatusDto(session)));
        }
    }

    public ValueTask<EmulatorCommandResponse> RewindCycleAsync(
        RewindCycleRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (request.CycleCount <= 0)
        {
            return ValueTask.FromResult(new EmulatorCommandResponse(
                RpcStatus.InvalidArgument("CycleCount must be greater than zero."),
                null));
        }

        if (!_registry.TryGet(request.SessionId, out var session))
            return ValueTask.FromResult(new EmulatorCommandResponse(HostProtocolMapper.MissingSessionStatus(request.SessionId), null));

        lock (session.SyncRoot)
        {
            return ValueTask.FromResult(new EmulatorCommandResponse(
                RpcStatus.NotImplemented("Reverse cycle stepping requires bounded execution history and is not available yet."),
                HostProtocolMapper.ToStatusDto(session)));
        }
    }

    public ValueTask<EmulatorCommandResponse> RewindFrameAsync(
        RewindFrameRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (request.FrameCount <= 0)
        {
            return ValueTask.FromResult(new EmulatorCommandResponse(
                RpcStatus.InvalidArgument("FrameCount must be greater than zero."),
                null));
        }

        if (!_registry.TryGet(request.SessionId, out var session))
            return ValueTask.FromResult(new EmulatorCommandResponse(HostProtocolMapper.MissingSessionStatus(request.SessionId), null));

        lock (session.SyncRoot)
        {
            return ValueTask.FromResult(new EmulatorCommandResponse(
                RpcStatus.NotImplemented("Reverse frame stepping requires bounded execution history and is not available yet."),
                HostProtocolMapper.ToStatusDto(session)));
        }
    }

    public ValueTask<EmulatorCommandResponse> SetLimiterRateAsync(
        SetLimiterRateRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!double.IsFinite(request.LimiterRatePercent) ||
            request.LimiterRatePercent <= 0 ||
            request.LimiterRatePercent > 1000)
        {
            return ValueTask.FromResult(new EmulatorCommandResponse(
                RpcStatus.InvalidArgument("LimiterRatePercent must be between 0 and 1000."),
                null));
        }

        if (!_registry.TryGet(request.SessionId, out var session))
            return ValueTask.FromResult(new EmulatorCommandResponse(HostProtocolMapper.MissingSessionStatus(request.SessionId), null));

        lock (session.SyncRoot)
        {
            session.LimiterRatePercent = request.LimiterRatePercent;
            return ValueTask.FromResult(new EmulatorCommandResponse(RpcStatus.Ok(), HostProtocolMapper.ToStatusDto(session)));
        }
    }

    public ValueTask<EmulatorCommandResponse> CloseSessionAsync(
        SessionRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_registry.TryGet(request.SessionId, out var session))
            return ValueTask.FromResult(new EmulatorCommandResponse(HostProtocolMapper.MissingSessionStatus(request.SessionId), null));

        lock (session.SyncRoot)
        {
            session.RunState = EmulatorRunState.Stopped;
            session.PowerState = "Off";
        }

        // Finalise any in-progress recordings before the session is dropped, so an
        // active ffmpeg process / open file handle is not leaked on session close.
        session.EndAllCaptures();

        _registry.Remove(request.SessionId);
        return ValueTask.FromResult(new EmulatorCommandResponse(RpcStatus.Ok(), null));
    }

    private ValueTask<EmulatorCommandResponse> SetRunStateAsync(
        SessionRequest request,
        EmulatorRunState runState,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_registry.TryGet(request.SessionId, out var session))
            return ValueTask.FromResult(new EmulatorCommandResponse(HostProtocolMapper.MissingSessionStatus(request.SessionId), null));

        lock (session.SyncRoot)
        {
            session.PowerState = "On";
            session.RunState = runState;
            return ValueTask.FromResult(new EmulatorCommandResponse(RpcStatus.Ok(), HostProtocolMapper.ToStatusDto(session)));
        }
    }

    private static EmulatorCommandResponse ExecuteResetAndAutostartDrive8(EmulatorRuntimeSession session)
    {
        var drive8 = session.Machine.Devices.All
            .OfType<IFloppyDrive>()
            .FirstOrDefault(drive => drive.DriveNumber == 8);

        if (drive8 is null)
        {
            return new EmulatorCommandResponse(
                RpcStatus.FailedPrecondition("ResetAndAutostartDrive8 requires a runtime drive 8 device."),
                HostProtocolMapper.ToStatusDto(session));
        }

        var hasDrive8Attachment = session.MediaAttachments.TryGetValue(MediaSlot.Drive8, out var attachment);
        if (hasDrive8Attachment &&
            attachment!.IsAttached &&
            !attachment.AppliedToRuntime)
        {
            var reason = string.IsNullOrWhiteSpace(attachment.Error)
                ? "Drive 8 media is not applied to the runtime."
                : attachment.Error;
            return new EmulatorCommandResponse(
                RpcStatus.FailedPrecondition(reason),
                HostProtocolMapper.ToStatusDto(session));
        }

        if (!drive8.HasDisk)
        {
            var hasAppliedAttachment = attachment is { IsAttached: true, AppliedToRuntime: true };
            if (!hasAppliedAttachment)
            {
                return new EmulatorCommandResponse(
                    RpcStatus.FailedPrecondition("ResetAndAutostartDrive8 requires an attached disk in drive 8."),
                    HostProtocolMapper.ToStatusDto(session));
            }
        }

        if (!session.Machine.Devices.All.OfType<IMachineKeyboardInput>().Any())
        {
            return new EmulatorCommandResponse(
                RpcStatus.FailedPrecondition("ResetAndAutostartDrive8 requires runtime keyboard input to submit BASIC autostart commands."),
                HostProtocolMapper.ToStatusDto(session));
        }

        session.Machine.Reset();
        session.RunState = EmulatorRunState.Running;
        session.PowerState = "On";
        session.ResetPerformanceCounters();
        session.StartHostKeyboardAutomation(HostKeyboardAutomation.CreateC64Drive8Autostart());

        return new EmulatorCommandResponse(RpcStatus.Ok(), HostProtocolMapper.ToStatusDto(session));
    }
}
