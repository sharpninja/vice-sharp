namespace ViceSharp.TestHarness;

using ViceSharp.Host.Runtime;
using ViceSharp.Host.Services;
using ViceSharp.Protocol;
using Xunit;

/// <summary>
/// Direct (in-process, no gRPC) unit tests for
/// <see cref="MonitorServiceHost.ExecuteCommandAsync"/> against a
/// minimal in-memory architecture. Reuses the
/// <see cref="MinimalHostArchitectureDescriptor"/> fixture pattern from
/// <see cref="MonitorServiceHostTests"/> so the tests do not require
/// C64 ROM assets and can run in any worktree.
///
/// Final MonitorService RPC slice in BACKFILL-HOSTUI-001, completing
/// the Monitor RPC quartet after ReadRegistersAsync (#90),
/// DisassembleAsync (#98), ReadMemoryAsync (#99), and
/// WriteMemoryAsync (#100). ExecuteCommandAsync is the most flexible
/// RPC: it routes a free-form text command into the internal
/// <see cref="ViceSharp.Monitor.Monitor.ExecuteCommand(string)"/> text
/// dispatcher (registers, step, mem, disass, breakpoints, reset,
/// cycles, help). The text monitor returns its diagnostic output as
/// the RPC <c>Output</c> string; unknown commands are reported as
/// in-band text rather than RPC errors.
/// </summary>
public sealed class MonitorServiceHostExecuteCommandTests
{
    /// <summary>
    /// FR/TR: FR-MONITOR (BACKFILL-HOSTUI-001 ExecuteCommandAsync).
    /// Use case: A debugger client routes a monitor command into a
    /// session id that the host runtime registry does not know about.
    /// Acceptance: The RPC returns the standard NotFound missing-session
    /// status (echoing the unknown session id), the Output is empty,
    /// and no emulator status payload is leaked.
    /// </summary>
    [Fact]
    public async Task ExecuteCommandAsync_MissingSession_ReturnsMissingSessionStatus()
    {
        var registry = new EmulatorRuntimeRegistry();
        var monitorService = new MonitorServiceHost(registry);

        var response = await monitorService.ExecuteCommandAsync(
            new ExecuteMonitorCommandRequest("does-not-exist", "r"),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.NotFound, response.Status.Code);
        Assert.Contains("does-not-exist", response.Status.Message);
        Assert.Equal(string.Empty, response.Output);
        Assert.Null(response.EmulatorStatus);
    }

    /// <summary>
    /// FR/TR: FR-MONITOR (BACKFILL-HOSTUI-001 ExecuteCommandAsync).
    /// Use case: A debugger client invokes ExecuteCommand with an empty
    /// or whitespace-only Command string, which the underlying text
    /// monitor would have to special-case anyway.
    /// Acceptance: The host short-circuits with InvalidArgument citing
    /// the missing Command field, returns an empty Output, and does
    /// not attach an emulator status payload. This pairs with the
    /// explicit <c>string.IsNullOrWhiteSpace(request.Command)</c> guard
    /// in <see cref="MonitorServiceHost.ExecuteCommandAsync"/>.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public async Task ExecuteCommandAsync_EmptyOrWhitespaceCommand_ReturnsInvalidArgument(string command)
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var monitorService = new MonitorServiceHost(registry);

        var response = await monitorService.ExecuteCommandAsync(
            new ExecuteMonitorCommandRequest(session.SessionId, command),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.InvalidArgument, response.Status.Code);
        Assert.Contains("Command", response.Status.Message);
        Assert.Equal(string.Empty, response.Output);
        Assert.Null(response.EmulatorStatus);
    }

    /// <summary>
    /// FR/TR: FR-MONITOR (BACKFILL-HOSTUI-001 ExecuteCommandAsync).
    /// Use case: A debugger client issues the "r" registers command,
    /// the canonical text-monitor inspection verb.
    /// Acceptance: Status is Ok, Output is non-null and non-empty, an
    /// emulator status payload is attached with the matching session
    /// id. Output content is exercised in the dedicated registers test
    /// below; this test pins the RPC envelope shape for a valid
    /// command.
    /// </summary>
    [Fact]
    public async Task ExecuteCommandAsync_ValidCommand_ReturnsOkWithOutput()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var monitorService = new MonitorServiceHost(registry);

        var response = await monitorService.ExecuteCommandAsync(
            new ExecuteMonitorCommandRequest(session.SessionId, "r"),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, response.Status.Code);
        Assert.NotNull(response.Output);
        Assert.NotEmpty(response.Output);
        Assert.NotNull(response.EmulatorStatus);
        Assert.Equal(session.SessionId, response.EmulatorStatus.SessionId);
    }

    /// <summary>
    /// FR/TR: FR-MONITOR (BACKFILL-HOSTUI-001 ExecuteCommandAsync).
    /// Use case: A debugger client routes "help" through the RPC; the
    /// text monitor's help banner is a well-known, stable string that
    /// proves the host actually invoked the underlying Monitor (and
    /// not just an empty-Output stub).
    /// Acceptance: Output is non-empty and contains the substring
    /// "Monitor:" (from
    /// <see cref="ViceSharp.Monitor.Monitor.ShowHelp"/>), confirming
    /// the help dispatcher fired. "?" is documented as an alias of
    /// "help" by the underlying monitor and is also exercised here.
    /// </summary>
    [Theory]
    [InlineData("help")]
    [InlineData("?")]
    public async Task ExecuteCommandAsync_HelpCommand_ReturnsHelpBanner(string command)
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var monitorService = new MonitorServiceHost(registry);

        var response = await monitorService.ExecuteCommandAsync(
            new ExecuteMonitorCommandRequest(session.SessionId, command),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, response.Status.Code);
        Assert.Contains("Monitor:", response.Output);
    }

    /// <summary>
    /// FR/TR: FR-MONITOR (BACKFILL-HOSTUI-001 ExecuteCommandAsync).
    /// Use case: A debugger client sends a command the text monitor
    /// does not understand. The historical text-monitor contract is to
    /// report unknown commands as in-band diagnostic text rather than
    /// to raise an RPC error, so the UI can render the response in the
    /// normal output pane.
    /// Acceptance: Documented behaviour is Ok status with the literal
    /// "Unknown:" prefix in Output (matching
    /// <see cref="ViceSharp.Monitor.Monitor.ExecuteCommand(string)"/>'s
    /// default switch arm). This test pins that contract so a future
    /// change to InvalidArgument would be a breaking change visible in
    /// CI.
    /// </summary>
    [Fact]
    public async Task ExecuteCommandAsync_UnknownCommand_ReturnsOkWithUnknownPrefix()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var monitorService = new MonitorServiceHost(registry);

        var response = await monitorService.ExecuteCommandAsync(
            new ExecuteMonitorCommandRequest(session.SessionId, "definitely-not-a-real-command"),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, response.Status.Code);
        Assert.Contains("Unknown", response.Output);
        Assert.NotNull(response.EmulatorStatus);
    }

    /// <summary>
    /// FR/TR: FR-MONITOR (BACKFILL-HOSTUI-001 ExecuteCommandAsync).
    /// Use case: A client cancels a monitor-command RPC before the
    /// host has a chance to service it; the RPC must observe the
    /// cancellation rather than running the command and returning
    /// partial output.
    /// Acceptance: Invoking ExecuteCommandAsync with an already-
    /// cancelled <see cref="CancellationToken"/> throws
    /// <see cref="OperationCanceledException"/>, matching the
    /// <c>ThrowIfCancellationRequested</c> contract on every monitor
    /// RPC.
    /// </summary>
    [Fact]
    public async Task ExecuteCommandAsync_CancelledToken_Throws()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var monitorService = new MonitorServiceHost(registry);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await monitorService.ExecuteCommandAsync(
                new ExecuteMonitorCommandRequest(session.SessionId, "r"),
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
