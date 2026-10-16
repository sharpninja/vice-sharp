namespace ViceSharp.TestHarness;

using ViceSharp.Core;
using ViceSharp.Host.Runtime;
using ViceSharp.Host.Services;
using ViceSharp.Protocol;
using Xunit;

/// <summary>
/// FR: FR-UIDROP-002, TR: TR-HOST-PRG-001, TEST-UIDROP-002.
/// Use case: the emulator host must load a dropped PRG into the current
/// session and queue BASIC RUN only when the program starts at TXTTAB.
/// </summary>
public sealed class EmulatorHostLoadProgramTests
{
    /// <summary>
    /// FR: FR-UIDROP-002, TR: TR-HOST-PRG-001, TEST-UIDROP-002.
    /// Use case: a BASIC-start PRG dropped on a live session is injected
    /// into RAM and RUN is armed.
    /// Acceptance: LoadProgramAsync returns Ok with Ran true, payload bytes
    /// are at $0801, and HostKeyboardAutomation is a BASIC RUN sequence.
    /// </summary>
    [Fact]
    public async Task LoadProgramAsync_BasicStartPayload_WritesRamAndQueuesRun()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var host = new EmulatorHostService(registry, new ThrowingRuntimeFactory());
        session.Machine.Bus.Write(0x002B, 0x01);
        session.Machine.Bus.Write(0x002C, 0x08);
        byte[] prg = [0x01, 0x08, 0x0B, 0x08, 0x0A, 0x00, 0x99, 0x22, 0x48, 0x49, 0x22, 0x00, 0x00, 0x00];

        var response = await host.LoadProgramAsync(
            new LoadProgramRequest(session.SessionId, FilePath: string.Empty, prg, "hi.prg"),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, response.Status.Code);
        Assert.Equal(0x0801, response.LoadAddress);
        Assert.Equal(12, response.ByteCount);
        Assert.True(response.Ran);
        Assert.Equal(0x0B, session.Machine.Bus.Peek(0x0801));
        Assert.NotNull(session.HostKeyboardAutomation);
        Assert.Equal("BASIC RUN", session.HostKeyboardAutomation.Description);
        Assert.True(session.HostKeyboardAutomation.IsActive);
    }

    /// <summary>
    /// FR: FR-UIDROP-002, TR: TR-HOST-PRG-001, TEST-UIDROP-002.
    /// Use case: a PRG that does not start at BASIC is loaded only.
    /// Acceptance: Ran is false and no keyboard automation is started.
    /// </summary>
    [Fact]
    public async Task LoadProgramAsync_NonBasicPayload_WritesRamWithoutRun()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var host = new EmulatorHostService(registry, new ThrowingRuntimeFactory());
        session.Machine.Bus.Write(0x002B, 0x01);
        session.Machine.Bus.Write(0x002C, 0x08);
        byte[] prg = [0x00, 0xC0, 0xA9, 0x01, 0x60];

        var response = await host.LoadProgramAsync(
            new LoadProgramRequest(session.SessionId, string.Empty, prg, "ml.prg"),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, response.Status.Code);
        Assert.Equal(0xC000, response.LoadAddress);
        Assert.False(response.Ran);
        Assert.Equal(0xA9, session.Machine.Bus.Peek(0xC000));
        Assert.Null(session.HostKeyboardAutomation);
    }

    /// <summary>
    /// FR: FR-UIDROP-002, TR: TR-HOST-PRG-001, TEST-UIDROP-002.
    /// Use case: a truncated PRG must fail closed.
    /// Acceptance: InvalidArgument, Ran false, and $C000 stays at the sentinel.
    /// </summary>
    [Fact]
    public async Task LoadProgramAsync_TruncatedPayload_ReturnsInvalidArgument()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var host = new EmulatorHostService(registry, new ThrowingRuntimeFactory());
        session.Machine.Bus.Write(0xC000, 0xAA);

        var response = await host.LoadProgramAsync(
            new LoadProgramRequest(session.SessionId, string.Empty, [0x00, 0xC0], "bad.prg"),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.InvalidArgument, response.Status.Code);
        Assert.False(response.Ran);
        Assert.Equal(0xAA, session.Machine.Bus.Peek(0xC000));
        Assert.Null(session.HostKeyboardAutomation);
    }

    /// <summary>
    /// FR: FR-UIDROP-002, TR: TR-HOST-PRG-001, TEST-UIDROP-002.
    /// Use case: LoadProgram for an unknown session must not invent state.
    /// Acceptance: NotFound missing-session status and Ran false.
    /// </summary>
    [Fact]
    public async Task LoadProgramAsync_MissingSession_ReturnsNotFound()
    {
        var host = new EmulatorHostService(new EmulatorRuntimeRegistry(), new ThrowingRuntimeFactory());

        var response = await host.LoadProgramAsync(
            new LoadProgramRequest("missing", string.Empty, [0x01, 0x08, 0x00], "x.prg"),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.NotFound, response.Status.Code);
        Assert.Contains("missing", response.Status.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(response.Ran);
        Assert.Null(response.EmulatorStatus);
    }

    private static EmulatorRuntimeSession CreateMinimalSession()
    {
        var factory = new DefaultEmulatorRuntimeFactory(
            new ArchitectureBuilder(),
            [MinimalHostArchitectureDescriptor.Instance],
            MinimalHostArchitectureDescriptor.ArchitectureId);
        return factory.Create(new CreateEmulatorSessionRequest());
    }

    private sealed class ThrowingRuntimeFactory : IEmulatorRuntimeFactory
    {
        public EmulatorRuntimeSession Create(CreateEmulatorSessionRequest request)
            => throw new InvalidOperationException("Unexpected session create.");
    }
}
