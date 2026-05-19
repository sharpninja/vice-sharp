namespace ViceSharp.TestHarness;

using ViceSharp.Host.Runtime;
using ViceSharp.Host.Services;
using ViceSharp.Protocol;
using Xunit;

/// <summary>
/// Direct (in-process, no gRPC) unit tests for
/// <see cref="MonitorServiceHost.ReadMemoryAsync"/> against a minimal
/// in-memory architecture. Reuses the
/// <see cref="MinimalHostArchitectureDescriptor"/> fixture pattern from
/// <see cref="MonitorServiceHostTests"/> so the tests do not require C64
/// ROM assets and can run in any worktree.
///
/// Third MonitorService RPC slice in BACKFILL-HOSTUI-001 after
/// ReadRegistersAsync (#90) and DisassembleAsync (#98).
/// </summary>
public sealed class MonitorServiceHostReadMemoryTests
{
    private const int AddressSpaceSize = 0x10000;

    /// <summary>
    /// FR/TR: FR-MONITOR (BACKFILL-HOSTUI-001 ReadMemoryAsync).
    /// Use case: A debugger client requests a memory read for a session
    /// the host runtime registry does not know about.
    /// Acceptance: The RPC returns the standard missing-session NotFound
    /// status, echoes the requested address, returns an empty byte
    /// payload, and emits no emulator status payload.
    /// </summary>
    [Fact]
    public async Task ReadMemoryAsync_MissingSession_ReturnsMissingSessionStatus()
    {
        var registry = new EmulatorRuntimeRegistry();
        var monitorService = new MonitorServiceHost(registry);

        var response = await monitorService.ReadMemoryAsync(
            new MonitorReadMemoryRequest("does-not-exist", 0xC000, 4),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.NotFound, response.Status.Code);
        Assert.Contains("does-not-exist", response.Status.Message);
        Assert.Equal(0xC000, response.Address);
        Assert.Empty(response.Data);
        Assert.Null(response.EmulatorStatus);
    }

    /// <summary>
    /// FR/TR: FR-MONITOR (BACKFILL-HOSTUI-001 ReadMemoryAsync).
    /// Use case: A debugger client requests a small contiguous read at
    /// a known address after the test seeds deterministic bytes through
    /// the machine bus.
    /// Acceptance: Status is Ok, the response echoes the request
    /// address, the returned byte array matches the seeded values
    /// in order, and an emulator status payload accompanies the
    /// response.
    /// </summary>
    [Fact]
    public async Task ReadMemoryAsync_ValidSession_ReturnsSeededBytes()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var monitorService = new MonitorServiceHost(registry);

        // Seed deterministic bytes at $C000.
        const ushort start = 0xC000;
        byte[] expected = [0xDE, 0xAD, 0xBE, 0xEF];
        for (var i = 0; i < expected.Length; i++)
            session.Machine.Bus.Write((ushort)(start + i), expected[i]);

        var response = await monitorService.ReadMemoryAsync(
            new MonitorReadMemoryRequest(session.SessionId, start, expected.Length),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, response.Status.Code);
        Assert.Equal(start, response.Address);
        Assert.Equal(expected, response.Data);
        Assert.NotNull(response.EmulatorStatus);
        Assert.Equal(session.SessionId, response.EmulatorStatus.SessionId);
    }

    /// <summary>
    /// FR/TR: FR-MONITOR (BACKFILL-HOSTUI-001 ReadMemoryAsync).
    /// Use case: A debugger client mistakenly issues a memory read with
    /// a non-positive length (zero or negative).
    /// Acceptance: Status is InvalidArgument with a message that calls
    /// out the length constraint, the data array is empty, and no
    /// emulator status payload is leaked.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ReadMemoryAsync_NonPositiveLength_ReturnsInvalidArgument(int length)
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var monitorService = new MonitorServiceHost(registry);

        var response = await monitorService.ReadMemoryAsync(
            new MonitorReadMemoryRequest(session.SessionId, 0xC000, length),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.InvalidArgument, response.Status.Code);
        Assert.Contains("Length", response.Status.Message);
        Assert.Empty(response.Data);
        Assert.Null(response.EmulatorStatus);
    }

    /// <summary>
    /// FR/TR: FR-MONITOR (BACKFILL-HOSTUI-001 ReadMemoryAsync).
    /// Use case: A debugger client requests a read whose length alone
    /// exceeds the 16-bit address space (0x10000 bytes).
    /// Acceptance: The host rejects the request with InvalidArgument
    /// citing the 16-bit address space constraint, the data array is
    /// empty, and no emulator status payload is leaked. Matches
    /// <c>ValidateMemoryRange</c> in <see cref="MonitorServiceHost"/>.
    /// </summary>
    [Fact]
    public async Task ReadMemoryAsync_LengthExceedsAddressSpace_ReturnsInvalidArgument()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var monitorService = new MonitorServiceHost(registry);

        var response = await monitorService.ReadMemoryAsync(
            new MonitorReadMemoryRequest(session.SessionId, 0x0000, AddressSpaceSize + 1),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.InvalidArgument, response.Status.Code);
        Assert.Contains("16-bit", response.Status.Message);
        Assert.Empty(response.Data);
        Assert.Null(response.EmulatorStatus);
    }

    /// <summary>
    /// FR/TR: FR-MONITOR (BACKFILL-HOSTUI-001 ReadMemoryAsync).
    /// Use case: A debugger client requests a read that would advance
    /// past the top of the 16-bit address space (start=$FFFC,
    /// length=8 implies end address $10004).
    /// Acceptance: Documented behaviour is to reject (NOT clamp). The
    /// host returns InvalidArgument citing the 16-bit address space
    /// constraint, the data array is empty, and no emulator status
    /// payload is leaked. This matches the
    /// <c>address + length &gt; AddressSpaceSize</c> guard in
    /// <c>ValidateMemoryRange</c>.
    /// </summary>
    [Fact]
    public async Task ReadMemoryAsync_AddressWrapPastEnd_ReturnsInvalidArgument()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var monitorService = new MonitorServiceHost(registry);

        var response = await monitorService.ReadMemoryAsync(
            new MonitorReadMemoryRequest(session.SessionId, 0xFFFC, 8),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.InvalidArgument, response.Status.Code);
        Assert.Contains("16-bit", response.Status.Message);
        Assert.Empty(response.Data);
        Assert.Null(response.EmulatorStatus);

        // Boundary: the exact last 4 bytes ($FFFC..$FFFF) must still be
        // accepted because address + length == AddressSpaceSize.
        var boundary = await monitorService.ReadMemoryAsync(
            new MonitorReadMemoryRequest(session.SessionId, 0xFFFC, 4),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, boundary.Status.Code);
        Assert.Equal(4, boundary.Data.Length);
    }

    /// <summary>
    /// FR/TR: FR-MONITOR (BACKFILL-HOSTUI-001 ReadMemoryAsync).
    /// Use case: A client cancels a memory-read RPC before the host has
    /// a chance to service it; the RPC must observe the cancellation
    /// rather than returning partial output.
    /// Acceptance: Invoking ReadMemoryAsync with an already-cancelled
    /// <see cref="CancellationToken"/> throws
    /// <see cref="OperationCanceledException"/>, matching the
    /// <c>ThrowIfCancellationRequested</c> contract on every monitor
    /// RPC.
    /// </summary>
    [Fact]
    public async Task ReadMemoryAsync_CancelledToken_Throws()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var monitorService = new MonitorServiceHost(registry);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await monitorService.ReadMemoryAsync(
                new MonitorReadMemoryRequest(session.SessionId, 0xC000, 4),
                cts.Token));
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
