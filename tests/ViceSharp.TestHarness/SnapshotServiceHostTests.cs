namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Chips.Cpu;
using ViceSharp.Core;
using ViceSharp.Host.Runtime;
using ViceSharp.Host.Services;
using ViceSharp.Protocol;
using Xunit;

/// <summary>
/// Direct (in-process, no gRPC) unit tests for the
/// <see cref="SnapshotServiceHost"/> RPC surface
/// (<see cref="ISnapshotService.CaptureSnapshotAsync"/> /
/// <see cref="ISnapshotService.RestoreSnapshotAsync"/>) against a
/// minimal in-memory architecture. Complements
/// <c>SnapshotRoundTripTests</c> (#85), which exercises the chip-layer
/// <c>RuntimeSnapshotStore</c>, by covering the host-RPC layer that
/// wraps it: session resolution, status mapping, format validation, and
/// cancellation contracts. Uses
/// <see cref="MinimalHostArchitectureDescriptor"/> so the tests do not
/// require C64 ROM assets on disk and can run in any worktree.
/// </summary>
public sealed class SnapshotServiceHostTests
{
    /// <summary>
    /// FR/TR: FR-SNAPSHOT (BACKFILL-HOSTUI-001 Snapshot RPC).
    /// Use case: A client calls CaptureSnapshot with a session id that
    /// the host runtime registry does not know about.
    /// Acceptance: The RPC returns the standard missing-session NotFound
    /// status (carrying the unknown session id) and no snapshot payload.
    /// </summary>
    [Fact]
    public async Task CaptureSnapshotAsync_MissingSession_ReturnsMissingSessionStatus()
    {
        var registry = new EmulatorRuntimeRegistry();
        var snapshotService = new SnapshotServiceHost(registry);

        var response = await snapshotService.CaptureSnapshotAsync(
            new SessionRequest("does-not-exist"),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.NotFound, response.Status.Code);
        Assert.Contains("does-not-exist", response.Status.Message);
        Assert.Null(response.Snapshot);
    }

    /// <summary>
    /// FR/TR: FR-SNAPSHOT (BACKFILL-HOSTUI-001 Snapshot RPC).
    /// Use case: A client calls RestoreSnapshot with a session id that
    /// the host runtime registry does not know about.
    /// Acceptance: The RPC returns the standard missing-session NotFound
    /// status and no emulator status payload, without touching any
    /// machine.
    /// </summary>
    [Fact]
    public async Task RestoreSnapshotAsync_MissingSession_ReturnsMissingSessionStatus()
    {
        var registry = new EmulatorRuntimeRegistry();
        var snapshotService = new SnapshotServiceHost(registry);
        var payload = new byte[16 + 65536];
        var snapshotDto = new SnapshotDto(
            SnapshotServiceHost.RuntimeSnapshotFormat,
            0,
            payload);

        var response = await snapshotService.RestoreSnapshotAsync(
            new RestoreSnapshotRequest("does-not-exist", snapshotDto),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.NotFound, response.Status.Code);
        Assert.Contains("does-not-exist", response.Status.Message);
        Assert.Null(response.EmulatorStatus);
    }

    /// <summary>
    /// FR/TR: FR-SNAPSHOT (BACKFILL-HOSTUI-001 Snapshot RPC).
    /// Use case: A client calls CaptureSnapshot for a freshly registered
    /// session and expects a serialised payload it can later persist or
    /// hand back to RestoreSnapshot.
    /// Acceptance: Status is Ok, the returned <see cref="SnapshotDto"/>
    /// uses the documented runtime-snapshot format string, carries the
    /// machine's current cycle, and exposes a non-empty payload sized
    /// for the 16-byte header plus a full 64K memory image.
    /// </summary>
    [Fact]
    public async Task CaptureSnapshotAsync_ValidSession_ReturnsOkWithNonEmptyPayload()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var snapshotService = new SnapshotServiceHost(registry);

        var response = await snapshotService.CaptureSnapshotAsync(
            new SessionRequest(session.SessionId),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, response.Status.Code);
        Assert.NotNull(response.Snapshot);
        Assert.Equal(SnapshotServiceHost.RuntimeSnapshotFormat, response.Snapshot.Format);
        Assert.NotNull(response.Snapshot.Payload);
        Assert.Equal(16 + 65536, response.Snapshot.Payload.Length);
        Assert.Equal((ulong)session.Machine.GetState().Cycle, response.Snapshot.Cycle);
    }

    /// <summary>
    /// FR/TR: FR-SNAPSHOT (BACKFILL-HOSTUI-001 Snapshot RPC).
    /// Use case: A client captures a snapshot via the RPC, mutates the
    /// machine's CPU registers and RAM, then calls RestoreSnapshot with
    /// the captured payload. The host must restore the captured CPU
    /// state and memory image so subsequent reads observe the snapshot
    /// values rather than the post-capture mutations.
    /// Acceptance: After RestoreSnapshot, ReadRegisters reports the
    /// captured A/X/Y/PC, the bus byte at the mutated address matches
    /// the captured value, RestoreSnapshot returns Ok, and the returned
    /// <see cref="EmulatorStatusDto"/> reflects the restored session.
    /// </summary>
    [Fact]
    public async Task CaptureThenRestore_RoundTripsCpuRegistersAndRam()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var snapshotService = new SnapshotServiceHost(registry);
        var monitorService = new MonitorServiceHost(registry);

        var cpu = (Mos6502)session.Machine.Devices.GetByRole(DeviceRole.Cpu)!;
        cpu.A = 0xAB;
        cpu.X = 0xCD;
        cpu.Y = 0xEF;
        cpu.PC = 0xC0DE;
        session.Machine.Bus.Write(0x4000, 0x99);

        var capture = await snapshotService.CaptureSnapshotAsync(
            new SessionRequest(session.SessionId),
            TestContext.Current.CancellationToken);
        Assert.Equal(RpcStatusCode.Ok, capture.Status.Code);
        Assert.NotNull(capture.Snapshot);

        cpu.A = 0x00;
        cpu.X = 0x00;
        cpu.Y = 0x00;
        cpu.PC = 0x0000;
        session.Machine.Bus.Write(0x4000, 0x00);

        var restore = await snapshotService.RestoreSnapshotAsync(
            new RestoreSnapshotRequest(session.SessionId, capture.Snapshot),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, restore.Status.Code);
        Assert.NotNull(restore.EmulatorStatus);
        Assert.Equal(session.SessionId, restore.EmulatorStatus.SessionId);

        var registers = await monitorService.ReadRegistersAsync(
            new SessionRequest(session.SessionId),
            TestContext.Current.CancellationToken);
        Assert.Equal(RpcStatusCode.Ok, registers.Status.Code);
        Assert.NotNull(registers.Registers);
        Assert.Equal((byte)0xAB, registers.Registers.A);
        Assert.Equal((byte)0xCD, registers.Registers.X);
        Assert.Equal((byte)0xEF, registers.Registers.Y);
        Assert.Equal((ushort)0xC0DE, registers.Registers.Pc);
        Assert.Equal((byte)0x99, session.Machine.Bus.Peek(0x4000));
    }

    /// <summary>
    /// FR/TR: FR-SNAPSHOT (BACKFILL-HOSTUI-001 Snapshot RPC).
    /// Use case: A client cancels a CaptureSnapshot RPC before the host
    /// has a chance to service it; the host must observe the
    /// cancellation rather than producing a partial snapshot.
    /// Acceptance: Invoking CaptureSnapshotAsync with an already-
    /// cancelled <see cref="CancellationToken"/> throws
    /// <see cref="OperationCanceledException"/> (matching the
    /// <c>ThrowIfCancellationRequested</c> contract).
    /// </summary>
    [Fact]
    public async Task CaptureSnapshotAsync_CancelledToken_Throws()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var snapshotService = new SnapshotServiceHost(registry);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await snapshotService.CaptureSnapshotAsync(
                new SessionRequest(session.SessionId),
                cts.Token));
    }

    /// <summary>
    /// FR/TR: FR-SNAPSHOT (BACKFILL-HOSTUI-001 Snapshot RPC).
    /// Use case: A client cancels a RestoreSnapshot RPC before the host
    /// has a chance to service it; the host must observe the
    /// cancellation rather than mutating the live machine from a
    /// partially validated payload.
    /// Acceptance: Invoking RestoreSnapshotAsync with an already-
    /// cancelled <see cref="CancellationToken"/> throws
    /// <see cref="OperationCanceledException"/> before any session
    /// lookup or snapshot deserialisation occurs.
    /// </summary>
    [Fact]
    public async Task RestoreSnapshotAsync_CancelledToken_Throws()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var snapshotService = new SnapshotServiceHost(registry);
        var payload = new byte[16 + 65536];
        var snapshotDto = new SnapshotDto(
            SnapshotServiceHost.RuntimeSnapshotFormat,
            0,
            payload);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await snapshotService.RestoreSnapshotAsync(
                new RestoreSnapshotRequest(session.SessionId, snapshotDto),
                cts.Token));
    }

    /// <summary>
    /// FR/TR: FR-SNAPSHOT (BACKFILL-HOSTUI-001 Snapshot RPC).
    /// Use case: A client calls RestoreSnapshot with a payload whose
    /// format string does not match the documented runtime-snapshot
    /// format (for example, an older or third-party format the host has
    /// no decoder for). The host must reject the request without
    /// touching the machine.
    /// Acceptance: RestoreSnapshotAsync returns InvalidArgument, the
    /// message identifies the offending format string, and no emulator
    /// status payload is returned. The session's machine state must be
    /// unchanged after the failed call.
    /// </summary>
    [Fact]
    public async Task RestoreSnapshotAsync_UnknownFormat_ReturnsInvalidArgument()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var snapshotService = new SnapshotServiceHost(registry);

        var cpu = (Mos6502)session.Machine.Devices.GetByRole(DeviceRole.Cpu)!;
        cpu.A = 0x55;
        var stateBefore = session.Machine.GetState();

        var snapshotDto = new SnapshotDto(
            "third-party-snapshot.v0",
            0,
            new byte[16 + 65536]);

        var response = await snapshotService.RestoreSnapshotAsync(
            new RestoreSnapshotRequest(session.SessionId, snapshotDto),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.InvalidArgument, response.Status.Code);
        Assert.Contains("third-party-snapshot.v0", response.Status.Message);
        Assert.Null(response.EmulatorStatus);

        var stateAfter = session.Machine.GetState();
        Assert.Equal(stateBefore.A, stateAfter.A);
        Assert.Equal(stateBefore.X, stateAfter.X);
        Assert.Equal(stateBefore.Y, stateAfter.Y);
        Assert.Equal(stateBefore.PC, stateAfter.PC);
    }

    private static EmulatorRuntimeSession CreateMinimalSession()
    {
        var factory = new DefaultEmulatorRuntimeFactory(
            new ArchitectureBuilder(),
            [MinimalHostArchitectureDescriptor.Instance],
            MinimalHostArchitectureDescriptor.ArchitectureId);
        return factory.Create(new CreateEmulatorSessionRequest());
    }
}
