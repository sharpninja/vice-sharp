using ViceSharp.Core.Snapshots;
using ViceSharp.Host.Runtime;
using ViceSharp.Protocol;

namespace ViceSharp.Host.Services;

/// <summary>
/// The host snapshot service. FIX-XSNAPWARP-001: C64 sessions capture and restore the
/// v2 MACHINE snapshot (<see cref="MachineSnapshotStager"/>: true RAM, color RAM, 6510
/// port, CPU, VIC, CIA1/2, SID) through the lockstep-proven chip injectors; the v1
/// runtime snapshot's 64KB Bus.Write replay scrambled live I/O registers on restore
/// (dead IRQ chain, the operator's "restarts in Warp mode"), so a v1 payload is
/// REFUSED on a C64 with a re-save message. Non-C64 machines (flat-RAM test
/// architectures with no I/O window) keep the v1 runtime snapshot, where the replay
/// is exact.
/// </summary>
public sealed class SnapshotServiceHost : ISnapshotService
{
    /// <summary>The v1 runtime-snapshot format id (non-C64 machines; refused on a C64).</summary>
    public const string RuntimeSnapshotFormat = "vice-sharp.runtime-snapshot.v1";

    private readonly EmulatorRuntimeRegistry _registry;
    private readonly MachineSnapshotStager _stager = new();
    private readonly RuntimeSnapshotStore _legacyStore = new();

    public SnapshotServiceHost(EmulatorRuntimeRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        _registry = registry;
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
            if (MachineSnapshotStager.CanSnapshot(session.Machine))
            {
                var payload = _stager.Capture(session.Machine);
                return ValueTask.FromResult(new CaptureSnapshotResponse(
                    RpcStatus.Ok(),
                    new SnapshotDto(MachineSnapshotStager.FormatV2, (ulong)session.Machine.GetState().Cycle, payload)));
            }

            var snapshot = _legacyStore.Capture(session.Machine);
            var legacyPayload = new byte[snapshot.GetSerializedSize()];
            snapshot.Serialize(legacyPayload);
            return ValueTask.FromResult(new CaptureSnapshotResponse(
                RpcStatus.Ok(),
                new SnapshotDto(RuntimeSnapshotFormat, snapshot.Cycle, legacyPayload)));
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

        lock (session.SyncRoot)
        {
            var isC64 = MachineSnapshotStager.CanSnapshot(session.Machine);

            if (string.Equals(request.Snapshot.Format, MachineSnapshotStager.FormatV2, StringComparison.Ordinal))
            {
                if (!isC64)
                {
                    return ValueTask.FromResult(new RestoreSnapshotResponse(
                        RpcStatus.InvalidArgument("A machine snapshot can only be restored into a C64 session."),
                        null));
                }

                _stager.Restore(session.Machine, request.Snapshot.Payload);
                return ValueTask.FromResult(new RestoreSnapshotResponse(RpcStatus.Ok(), HostProtocolMapper.ToStatusDto(session)));
            }

            if (string.Equals(request.Snapshot.Format, RuntimeSnapshotFormat, StringComparison.Ordinal))
            {
                if (isC64)
                {
                    // FIX-XSNAPWARP-001: the v1 Bus.Write replay corrupts C64 I/O state.
                    return ValueTask.FromResult(new RestoreSnapshotResponse(
                        RpcStatus.InvalidArgument(
                            "This snapshot was saved by an older build and cannot be restored faithfully; please save a new snapshot."),
                        null));
                }

                var snapshot = new RuntimeSnapshot();
                snapshot.Deserialize(request.Snapshot.Payload);
                _legacyStore.Restore(session.Machine, snapshot);
                return ValueTask.FromResult(new RestoreSnapshotResponse(RpcStatus.Ok(), HostProtocolMapper.ToStatusDto(session)));
            }

            return ValueTask.FromResult(new RestoreSnapshotResponse(
                RpcStatus.InvalidArgument($"Snapshot format '{request.Snapshot.Format}' is not supported."),
                null));
        }
    }
}
