namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Chips.Cpu;
using ViceSharp.Core;
using ViceSharp.Host.Runtime;
using ViceSharp.Host.Services;
using ViceSharp.Protocol;
using Xunit;

/// <summary>
/// Direct (in-process, no gRPC) unit tests for
/// <see cref="MonitorServiceHost.ReadRegistersAsync"/> against a
/// minimal in-memory architecture. Uses
/// <see cref="MinimalHostArchitectureDescriptor"/> so the tests do not
/// require C64 ROM assets on disk and can run in any worktree.
/// </summary>
public sealed class MonitorServiceHostTests
{
    /// <summary>
    /// FR/TR: FR-MONITOR (BACKFILL-HOSTUI-001 ReadRegistersAsync).
    /// Use case: A debugger client calls ReadRegisters with a session id
    /// that the host runtime registry does not know about.
    /// Acceptance: The RPC returns a NotFound status (the standard
    /// missing-session status) and carries no register or status payload.
    /// </summary>
    [Fact]
    public async Task ReadRegistersAsync_MissingSession_ReturnsMissingSessionStatus()
    {
        var registry = new EmulatorRuntimeRegistry();
        var monitorService = new MonitorServiceHost(registry);

        var response = await monitorService.ReadRegistersAsync(
            new SessionRequest("does-not-exist"),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.NotFound, response.Status.Code);
        Assert.Contains("does-not-exist", response.Status.Message);
        Assert.Null(response.Registers);
        Assert.Null(response.EmulatorStatus);
    }

    /// <summary>
    /// FR/TR: FR-MONITOR (BACKFILL-HOSTUI-001 ReadRegistersAsync).
    /// Use case: A debugger client reads CPU registers for a freshly
    /// registered session backed by the minimal in-memory machine.
    /// Acceptance: Status is Ok, the returned <c>MachineStateDto</c>
    /// matches the machine's <c>GetState()</c> snapshot (A/X/Y/S/P/PC/
    /// Cycle), and the emulator status DTO is populated.
    /// </summary>
    [Fact]
    public async Task ReadRegistersAsync_ValidSession_ReturnsRegisterSnapshot()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var monitorService = new MonitorServiceHost(registry);

        var expected = session.Machine.GetState();
        var response = await monitorService.ReadRegistersAsync(
            new SessionRequest(session.SessionId),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, response.Status.Code);
        Assert.NotNull(response.Registers);
        Assert.NotNull(response.EmulatorStatus);
        Assert.Equal(expected.A, response.Registers.A);
        Assert.Equal(expected.X, response.Registers.X);
        Assert.Equal(expected.Y, response.Registers.Y);
        Assert.Equal(expected.S, response.Registers.S);
        Assert.Equal(expected.P, response.Registers.P);
        Assert.Equal(expected.PC, response.Registers.Pc);
        Assert.Equal(expected.Cycle, response.Registers.Cycle);
        Assert.Equal(session.SessionId, response.EmulatorStatus.SessionId);
    }

    /// <summary>
    /// FR/TR: FR-MONITOR (BACKFILL-HOSTUI-001 ReadRegistersAsync).
    /// Use case: After the runtime mutates CPU registers (for example,
    /// because a single-step or debugger command modified them), a
    /// subsequent ReadRegisters call must reflect the new values rather
    /// than a stale snapshot.
    /// Acceptance: Mutating the live Mos6502 chip's A/X/Y/PC fields and
    /// then invoking ReadRegistersAsync yields a <c>MachineStateDto</c>
    /// whose A/X/Y/Pc match the mutated values.
    /// </summary>
    [Fact]
    public async Task ReadRegistersAsync_ReflectsCpuMutation()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var monitorService = new MonitorServiceHost(registry);

        var cpu = (Mos6502)session.Machine.Devices.GetByRole(DeviceRole.Cpu)!;
        cpu.A = 0x42;
        cpu.X = 0x11;
        cpu.Y = 0x22;
        cpu.PC = 0xC000;

        var response = await monitorService.ReadRegistersAsync(
            new SessionRequest(session.SessionId),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, response.Status.Code);
        Assert.NotNull(response.Registers);
        Assert.Equal((byte)0x42, response.Registers.A);
        Assert.Equal((byte)0x11, response.Registers.X);
        Assert.Equal((byte)0x22, response.Registers.Y);
        Assert.Equal((ushort)0xC000, response.Registers.Pc);
    }

    /// <summary>
    /// FR/TR: FR-MONITOR (BACKFILL-HOSTUI-001 ReadRegistersAsync).
    /// Use case: A client cancels a debugger RPC before the host has a
    /// chance to service it; the RPC must observe the cancellation
    /// instead of returning a partial or stale snapshot.
    /// Acceptance: Invoking ReadRegistersAsync with an already-cancelled
    /// <see cref="CancellationToken"/> throws
    /// <see cref="OperationCanceledException"/> (matching the
    /// <c>ThrowIfCancellationRequested</c> contract on every monitor RPC).
    /// </summary>
    [Fact]
    public async Task ReadRegistersAsync_CancelledToken_Throws()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var monitorService = new MonitorServiceHost(registry);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await monitorService.ReadRegistersAsync(
                new SessionRequest(session.SessionId),
                cts.Token));
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
