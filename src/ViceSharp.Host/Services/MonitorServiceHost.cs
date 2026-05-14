using ViceSharp.Host.Runtime;
using ViceSharp.Protocol;

namespace ViceSharp.Host.Services;

public sealed class MonitorServiceHost : IMonitorService
{
    private readonly EmulatorRuntimeRegistry _registry;

    public MonitorServiceHost(EmulatorRuntimeRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        _registry = registry;
    }

    public ValueTask<MonitorCommandResponse> ExecuteCommandAsync(
        ExecuteMonitorCommandRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_registry.TryGet(request.SessionId, out var session))
            return ValueTask.FromResult(new MonitorCommandResponse(HostProtocolMapper.MissingSessionStatus(request.SessionId), string.Empty, null));

        if (string.IsNullOrWhiteSpace(request.Command))
            return ValueTask.FromResult(new MonitorCommandResponse(RpcStatus.InvalidArgument("Command is required."), string.Empty, null));

        lock (session.SyncRoot)
        {
            var monitor = new ViceSharp.Monitor.Monitor(session.Machine);
            var output = monitor.ExecuteCommand(request.Command);
            return ValueTask.FromResult(new MonitorCommandResponse(RpcStatus.Ok(), output, HostProtocolMapper.ToStatusDto(session)));
        }
    }
}
