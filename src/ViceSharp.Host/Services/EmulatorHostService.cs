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
            return ValueTask.FromResult(new EmulatorCommandResponse(
                RpcStatus.NotImplemented("ResetAndAutostartDrive8 requires drive command/autostart support that is not available yet."),
                HostProtocolMapper.ToStatusDto(session)));
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
                return ValueTask.FromResult(new EmulatorCommandResponse(
                    RpcStatus.NotImplemented("ResetAndAutostartDrive8 requires drive command/autostart support that is not available yet."),
                    HostProtocolMapper.ToStatusDto(session)));
            }

            session.Machine.Reset();
            session.RunState = EmulatorRunState.Stopped;
            session.PowerState = "On";
            session.ResetPerformanceCounters();
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
}
