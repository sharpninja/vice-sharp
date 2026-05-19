namespace ViceSharp.TestHarness;

using ViceSharp.Host.Runtime;
using ViceSharp.Host.Services;
using ViceSharp.Protocol;
using Xunit;

/// <summary>
/// Direct (in-process, no gRPC) unit tests for
/// <see cref="MonitorServiceHost.WriteMemoryAsync"/> against a minimal
/// in-memory architecture. Reuses the
/// <see cref="MinimalHostArchitectureDescriptor"/> fixture pattern from
/// <see cref="MonitorServiceHostTests"/> so the tests do not require C64
/// ROM assets and can run in any worktree.
///
/// Fourth MonitorService RPC slice in BACKFILL-HOSTUI-001 after
/// ReadRegistersAsync (#90), DisassembleAsync (#98), and
/// ReadMemoryAsync (#99). Closes the Read/Write RPC pair so the host UI
/// can both inspect and mutate emulator memory through the protocol
/// surface.
/// </summary>
public sealed class MonitorServiceHostWriteMemoryTests
{
    private const int AddressSpaceSize = 0x10000;

    /// <summary>
    /// FR/TR: FR-MONITOR (BACKFILL-HOSTUI-001 WriteMemoryAsync).
    /// Use case: A debugger client requests a memory write for a session
    /// the host runtime registry does not know about.
    /// Acceptance: The RPC returns the standard missing-session NotFound
    /// status, echoes the requested address, reports zero bytes written,
    /// and emits no emulator status payload.
    /// </summary>
    [Fact]
    public async Task WriteMemoryAsync_MissingSession_ReturnsMissingSessionStatus()
    {
        var registry = new EmulatorRuntimeRegistry();
        var monitorService = new MonitorServiceHost(registry);

        var response = await monitorService.WriteMemoryAsync(
            new MonitorWriteMemoryRequest("does-not-exist", 0xC000, [0x11, 0x22, 0x33, 0x44]),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.NotFound, response.Status.Code);
        Assert.Contains("does-not-exist", response.Status.Message);
        Assert.Equal(0xC000, response.Address);
        Assert.Equal(0, response.BytesWritten);
        Assert.Null(response.EmulatorStatus);
    }

    /// <summary>
    /// FR/TR: FR-MONITOR (BACKFILL-HOSTUI-001 WriteMemoryAsync).
    /// Use case: A debugger client writes a small contiguous payload at
    /// a known RAM address and then reads it back to verify the write
    /// landed on the emulator bus.
    /// Acceptance: WriteMemoryAsync returns Ok with the echoed address
    /// and the byte count actually written, an emulator status payload
    /// is attached, and a subsequent ReadMemoryAsync at the same address
    /// returns the exact bytes that were written.
    /// </summary>
    [Fact]
    public async Task WriteMemoryAsync_ValidSession_RoundTripsThroughReadMemory()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var monitorService = new MonitorServiceHost(registry);

        const ushort start = 0xC000;
        byte[] payload = [0x11, 0x22, 0x33, 0x44];

        var write = await monitorService.WriteMemoryAsync(
            new MonitorWriteMemoryRequest(session.SessionId, start, payload),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, write.Status.Code);
        Assert.Equal(start, write.Address);
        Assert.Equal(payload.Length, write.BytesWritten);
        Assert.NotNull(write.EmulatorStatus);
        Assert.Equal(session.SessionId, write.EmulatorStatus.SessionId);

        var read = await monitorService.ReadMemoryAsync(
            new MonitorReadMemoryRequest(session.SessionId, start, payload.Length),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, read.Status.Code);
        Assert.Equal(start, read.Address);
        Assert.Equal(payload, read.Data);
    }

    /// <summary>
    /// FR/TR: FR-MONITOR (BACKFILL-HOSTUI-001 WriteMemoryAsync).
    /// Use case: A debugger client mistakenly issues a memory write with
    /// an empty Data array.
    /// Acceptance: Documented behaviour is to reject. The host returns
    /// InvalidArgument citing the missing Data payload, BytesWritten is
    /// zero, and no emulator status payload is leaked. This pairs with
    /// the explicit <c>request.Data is null or Length == 0</c> guard in
    /// <see cref="MonitorServiceHost.WriteMemoryAsync"/>, distinct from
    /// the length-must-be-positive guard that ReadMemoryAsync uses.
    /// </summary>
    [Fact]
    public async Task WriteMemoryAsync_EmptyPayload_ReturnsInvalidArgument()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var monitorService = new MonitorServiceHost(registry);

        var response = await monitorService.WriteMemoryAsync(
            new MonitorWriteMemoryRequest(session.SessionId, 0xC000, Array.Empty<byte>()),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.InvalidArgument, response.Status.Code);
        Assert.Contains("Data", response.Status.Message);
        Assert.Equal(0, response.BytesWritten);
        Assert.Null(response.EmulatorStatus);
    }

    /// <summary>
    /// FR/TR: FR-MONITOR (BACKFILL-HOSTUI-001 WriteMemoryAsync).
    /// Use case: A debugger client requests a write that would advance
    /// past the top of the 16-bit address space (start=$FFFC,
    /// payload length=8 implies end address $10004).
    /// Acceptance: Documented behaviour is to reject (NOT clamp, NOT
    /// wrap). The host returns InvalidArgument citing the 16-bit
    /// address space constraint, BytesWritten is zero, no emulator
    /// status payload is leaked, AND the bytes at the high end of RAM
    /// remain untouched (verified via a follow-up ReadMemoryAsync).
    /// This matches the shared <c>ValidateMemoryRange</c> helper used
    /// by both Read and Write paths in
    /// <see cref="MonitorServiceHost"/>.
    /// </summary>
    [Fact]
    public async Task WriteMemoryAsync_AddressPlusPayloadOverruns_ReturnsInvalidArgument()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var monitorService = new MonitorServiceHost(registry);

        // Seed a sentinel so we can detect any partial write into the
        // top of the address space.
        const ushort sentinelStart = 0xFFFC;
        byte[] sentinel = [0xAA, 0xBB, 0xCC, 0xDD];
        for (var i = 0; i < sentinel.Length; i++)
            session.Machine.Bus.Write((ushort)(sentinelStart + i), sentinel[i]);

        var response = await monitorService.WriteMemoryAsync(
            new MonitorWriteMemoryRequest(session.SessionId, 0xFFFC, [1, 2, 3, 4, 5, 6, 7, 8]),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.InvalidArgument, response.Status.Code);
        Assert.Contains("16-bit", response.Status.Message);
        Assert.Equal(0, response.BytesWritten);
        Assert.Null(response.EmulatorStatus);

        // Sentinel bytes at $FFFC..$FFFF must be unchanged - the host
        // rejects before any partial write.
        var read = await monitorService.ReadMemoryAsync(
            new MonitorReadMemoryRequest(session.SessionId, sentinelStart, sentinel.Length),
            TestContext.Current.CancellationToken);

        Assert.Equal(sentinel, read.Data);

        // Boundary: the exact last 4 bytes ($FFFC..$FFFF) must still be
        // accepted because address + length == AddressSpaceSize.
        var boundary = await monitorService.WriteMemoryAsync(
            new MonitorWriteMemoryRequest(session.SessionId, sentinelStart, [0x55, 0x66, 0x77, 0x88]),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, boundary.Status.Code);
        Assert.Equal(4, boundary.BytesWritten);
    }

    /// <summary>
    /// FR/TR: FR-MONITOR (BACKFILL-HOSTUI-001 WriteMemoryAsync).
    /// Use case: A client cancels a memory-write RPC before the host
    /// has a chance to service it; the RPC must observe the
    /// cancellation rather than writing partial data.
    /// Acceptance: Invoking WriteMemoryAsync with an already-cancelled
    /// <see cref="CancellationToken"/> throws
    /// <see cref="OperationCanceledException"/>, matching the
    /// <c>ThrowIfCancellationRequested</c> contract on every monitor
    /// RPC; a follow-up read confirms target memory was not mutated.
    /// </summary>
    [Fact]
    public async Task WriteMemoryAsync_CancelledToken_Throws()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var monitorService = new MonitorServiceHost(registry);

        const ushort start = 0xC000;
        // Seed the target with a known sentinel; if cancellation is
        // honoured before the lock is taken the bytes must stay as-is.
        byte[] sentinel = [0x5A, 0x5A, 0x5A, 0x5A];
        for (var i = 0; i < sentinel.Length; i++)
            session.Machine.Bus.Write((ushort)(start + i), sentinel[i]);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await monitorService.WriteMemoryAsync(
                new MonitorWriteMemoryRequest(session.SessionId, start, [0x11, 0x22, 0x33, 0x44]),
                cts.Token));

        var read = await monitorService.ReadMemoryAsync(
            new MonitorReadMemoryRequest(session.SessionId, start, sentinel.Length),
            TestContext.Current.CancellationToken);

        Assert.Equal(sentinel, read.Data);
    }

    /// <summary>
    /// FR/TR: FR-MONITOR (BACKFILL-HOSTUI-001 WriteMemoryAsync).
    /// Use case: A debugger client writes into a high address typically
    /// covered by ROM on a real C64 (e.g. KERNAL at $E000-$FFFF). The
    /// minimal host architecture used by these unit tests deliberately
    /// has no ROM mappings (see
    /// <see cref="MinimalHostArchitectureDescriptor.RequiredRoms"/>),
    /// so the documented behaviour for the minimal fixture is that the
    /// entire 16-bit space is RAM-backed and writes round-trip.
    /// Acceptance: The write succeeds, BytesWritten matches the
    /// payload length, and a follow-up ReadMemoryAsync returns the
    /// written bytes verbatim. This pins the contract: ROM shadowing
    /// is an architecture-level concern, not a MonitorService concern.
    /// </summary>
    [Fact]
    public async Task WriteMemoryAsync_RomShadowAddress_IsRamUnderRomOnMinimalFixture()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var monitorService = new MonitorServiceHost(registry);

        const ushort kernalShadow = 0xE000;
        byte[] payload = [0xCA, 0xFE, 0xBA, 0xBE];

        var write = await monitorService.WriteMemoryAsync(
            new MonitorWriteMemoryRequest(session.SessionId, kernalShadow, payload),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, write.Status.Code);
        Assert.Equal(payload.Length, write.BytesWritten);

        var read = await monitorService.ReadMemoryAsync(
            new MonitorReadMemoryRequest(session.SessionId, kernalShadow, payload.Length),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, read.Status.Code);
        Assert.Equal(payload, read.Data);
    }

    private static EmulatorRuntimeSession CreateMinimalSession()
    {
        var factory = new DefaultEmulatorRuntimeFactory(
            new ViceSharp.Core.ArchitectureBuilder(),
            [MinimalHostArchitectureDescriptor.Instance],
            MinimalHostArchitectureDescriptor.ArchitectureId);
        return factory.Create(new CreateEmulatorSessionRequest());
    }
}
