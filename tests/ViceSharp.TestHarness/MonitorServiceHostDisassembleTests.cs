namespace ViceSharp.TestHarness;

using ViceSharp.Host.Runtime;
using ViceSharp.Host.Services;
using ViceSharp.Protocol;
using Xunit;

/// <summary>
/// Direct (in-process, no gRPC) unit tests for
/// <see cref="MonitorServiceHost.DisassembleAsync"/> against a minimal
/// in-memory architecture. Reuses the
/// <see cref="MinimalHostArchitectureDescriptor"/> fixture pattern from
/// <see cref="MonitorServiceHostTests"/> so the tests do not require C64
/// ROM assets and can run in any worktree.
///
/// Companion to the ReadRegistersAsync coverage introduced in #90; this
/// file targets the BACKFILL-HOSTUI-001 slice for DisassembleAsync.
/// </summary>
public sealed class MonitorServiceHostDisassembleTests
{
    private const int MaxDisassemblyCount = 256;

    /// <summary>
    /// FR/TR: FR-MONITOR (BACKFILL-HOSTUI-001 DisassembleAsync).
    /// Use case: A debugger client requests a disassembly for a session
    /// the host runtime registry does not know about.
    /// Acceptance: The RPC returns the standard missing-session NotFound
    /// status, an empty line collection, and no emulator status payload.
    /// </summary>
    [Fact]
    public async Task DisassembleAsync_MissingSession_ReturnsMissingSessionStatus()
    {
        var registry = new EmulatorRuntimeRegistry();
        var monitorService = new MonitorServiceHost(registry);

        var response = await monitorService.DisassembleAsync(
            new MonitorDisassemblyRequest("does-not-exist", 0xC000, 4),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.NotFound, response.Status.Code);
        Assert.Contains("does-not-exist", response.Status.Message);
        Assert.Empty(response.Lines);
        Assert.Null(response.EmulatorStatus);
    }

    /// <summary>
    /// FR/TR: FR-MONITOR (BACKFILL-HOSTUI-001 DisassembleAsync).
    /// Use case: A debugger client requests a disassembly of N
    /// instructions starting at a known address after seeding memory
    /// with deterministic opcodes (LDA #$42 / NOP / RTS).
    /// Acceptance: Status is Ok, the returned line count equals the
    /// requested count, the first line's address matches the requested
    /// address, and subsequent lines advance by the per-opcode length
    /// (2 + 1 + 1 = NOPs after the trailing RTS).
    /// </summary>
    [Fact]
    public async Task DisassembleAsync_ValidSession_ReturnsRequestedInstructions()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var monitorService = new MonitorServiceHost(registry);

        // Seed deterministic opcodes at $C000:
        //   $C000: A9 42        LDA #$42  (2 bytes)
        //   $C002: EA           NOP       (1 byte)
        //   $C003: 60           RTS       (1 byte)
        const ushort start = 0xC000;
        session.Machine.Bus.Write(start, 0xA9);
        session.Machine.Bus.Write((ushort)(start + 1), 0x42);
        session.Machine.Bus.Write((ushort)(start + 2), 0xEA);
        session.Machine.Bus.Write((ushort)(start + 3), 0x60);

        var response = await monitorService.DisassembleAsync(
            new MonitorDisassemblyRequest(session.SessionId, start, 3),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, response.Status.Code);
        Assert.NotNull(response.EmulatorStatus);
        Assert.Equal(3, response.Lines.Count);

        // First instruction: LDA #$42 at $C000, length 2, next $C002.
        Assert.Equal(start, response.Lines[0].Address);
        Assert.Equal(2, response.Lines[0].Length);
        Assert.Equal(start + 2, response.Lines[0].NextAddress);
        Assert.Contains("LDA", response.Lines[0].Text);

        // Second instruction: NOP at $C002, length 1, next $C003.
        Assert.Equal(start + 2, response.Lines[1].Address);
        Assert.Equal(1, response.Lines[1].Length);
        Assert.Equal(start + 3, response.Lines[1].NextAddress);
        Assert.Contains("NOP", response.Lines[1].Text);

        // Third instruction: RTS at $C003, length 1, next $C004.
        Assert.Equal(start + 3, response.Lines[2].Address);
        Assert.Equal(1, response.Lines[2].Length);
        Assert.Equal(start + 4, response.Lines[2].NextAddress);
        Assert.Contains("RTS", response.Lines[2].Text);

        Assert.Equal(session.SessionId, response.EmulatorStatus.SessionId);
    }

    /// <summary>
    /// FR/TR: FR-MONITOR (BACKFILL-HOSTUI-001 DisassembleAsync).
    /// Use case: A debugger client mistakenly issues a disassembly
    /// request with a non-positive count (zero or negative).
    /// Acceptance: Status is InvalidArgument with a message that calls
    /// out the count constraint, the line collection is empty, and no
    /// emulator status payload is leaked.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task DisassembleAsync_NonPositiveCount_ReturnsInvalidArgument(int count)
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var monitorService = new MonitorServiceHost(registry);

        var response = await monitorService.DisassembleAsync(
            new MonitorDisassemblyRequest(session.SessionId, 0xC000, count),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.InvalidArgument, response.Status.Code);
        Assert.Contains("Count", response.Status.Message);
        Assert.Empty(response.Lines);
        Assert.Null(response.EmulatorStatus);
    }

    /// <summary>
    /// FR/TR: FR-MONITOR (BACKFILL-HOSTUI-001 DisassembleAsync).
    /// Use case: A debugger client requests more than
    /// <c>MaxDisassemblyCount</c> (256) instructions in one call.
    /// Acceptance: The host rejects the request with InvalidArgument
    /// rather than silently clamping. This documents the chosen
    /// behaviour for the BACKFILL-HOSTUI-001 slice (reject, not clamp),
    /// matching <c>ValidateDisassemblyRequest</c> in
    /// <see cref="MonitorServiceHost"/>. The boundary value 256 is
    /// accepted; 257 is rejected.
    /// </summary>
    [Fact]
    public async Task DisassembleAsync_CountExceedsMax_ReturnsInvalidArgument()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var monitorService = new MonitorServiceHost(registry);

        var response = await monitorService.DisassembleAsync(
            new MonitorDisassemblyRequest(session.SessionId, 0xC000, MaxDisassemblyCount + 1),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.InvalidArgument, response.Status.Code);
        Assert.Contains(MaxDisassemblyCount.ToString(), response.Status.Message);
        Assert.Empty(response.Lines);
        Assert.Null(response.EmulatorStatus);

        // Boundary: exactly MaxDisassemblyCount must still be accepted.
        var boundary = await monitorService.DisassembleAsync(
            new MonitorDisassemblyRequest(session.SessionId, 0xC000, MaxDisassemblyCount),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, boundary.Status.Code);
        Assert.Equal(MaxDisassemblyCount, boundary.Lines.Count);
    }

    /// <summary>
    /// FR/TR: FR-MONITOR (BACKFILL-HOSTUI-001 DisassembleAsync).
    /// Use case: A client cancels a disassembly RPC before the host has
    /// a chance to service it; the RPC must observe the cancellation
    /// rather than returning partial output.
    /// Acceptance: Invoking DisassembleAsync with an already-cancelled
    /// <see cref="CancellationToken"/> throws
    /// <see cref="OperationCanceledException"/>, matching the
    /// <c>ThrowIfCancellationRequested</c> contract on every monitor
    /// RPC.
    /// </summary>
    [Fact]
    public async Task DisassembleAsync_CancelledToken_Throws()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var monitorService = new MonitorServiceHost(registry);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await monitorService.DisassembleAsync(
                new MonitorDisassemblyRequest(session.SessionId, 0xC000, 4),
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
