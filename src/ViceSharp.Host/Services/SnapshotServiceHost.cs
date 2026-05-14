using ViceSharp.Core.Snapshots;
using ViceSharp.Host.Runtime;
using ViceSharp.Protocol;

namespace ViceSharp.Host.Services;

public sealed class SnapshotServiceHost : ISnapshotService
{
    public const string RuntimeSnapshotFormat = "vice-sharp.runtime-snapshot.v1";

    private readonly EmulatorRuntimeRegistry _registry;
    private readonly RuntimeSnapshotStore _snapshotStore;

    public SnapshotServiceHost(EmulatorRuntimeRegistry registry)
        : this(registry, new RuntimeSnapshotStore())
    {
    }

    public SnapshotServiceHost(EmulatorRuntimeRegistry registry, RuntimeSnapshotStore snapshotStore)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(snapshotStore);

        _registry = registry;
        _snapshotStore = snapshotStore;
    }

    public ValueTask<CaptureSnapshotResponse> CaptureSnapshotAsync(
        SessionRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_registry.TryGet(request.SessionId, out var session))
            return ValueTask.FromResult(new CaptureSnapshotResponse(HostProtocolMapper.MissingSessionStatus(request.SessionId), null));

        lock (session.SyncRoot)
        {
            var snapshot = _snapshotStore.Capture(session.Machine);
            var payload = new byte[snapshot.GetSerializedSize()];
            snapshot.Serialize(payload);

            return ValueTask.FromResult(new CaptureSnapshotResponse(
                RpcStatus.Ok(),
                new SnapshotDto(RuntimeSnapshotFormat, snapshot.Cycle, payload)));
        }
    }

    public ValueTask<RestoreSnapshotResponse> RestoreSnapshotAsync(
        RestoreSnapshotRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(request.Snapshot);

        if (!_registry.TryGet(request.SessionId, out var session))
            return ValueTask.FromResult(new RestoreSnapshotResponse(HostProtocolMapper.MissingSessionStatus(request.SessionId), null));

        if (!string.Equals(request.Snapshot.Format, RuntimeSnapshotFormat, StringComparison.Ordinal))
        {
            return ValueTask.FromResult(new RestoreSnapshotResponse(
                RpcStatus.InvalidArgument($"Snapshot format '{request.Snapshot.Format}' is not supported."),
                null));
        }

        lock (session.SyncRoot)
        {
            var snapshot = new RuntimeSnapshot();
            snapshot.Deserialize(request.Snapshot.Payload);
            _snapshotStore.Restore(session.Machine, snapshot);
            return ValueTask.FromResult(new RestoreSnapshotResponse(RpcStatus.Ok(), HostProtocolMapper.ToStatusDto(session)));
        }
    }
}
