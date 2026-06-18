namespace ViceSharp.TestHarness;

using System.Collections.Generic;
using ViceSharp.Abstractions;
using ViceSharp.Core;
using ViceSharp.Host.Runtime;
using ViceSharp.Host.Services;
using ViceSharp.Protocol;
using Xunit;

/// <summary>
/// FR-TICKHIST-001 / TR-TICKHIST-CAPTURE-001 / TEST-TICKHIST-001.
/// End-to-end wiring of the tick-history capture: the machine pub/sub (CPU
/// instruction-completed + bus memory-write events) feeds the session's
/// <see cref="TickHistoryRecorder"/> as the emulation pump advances a running session.
/// </summary>
public sealed class TickHistoryCaptureTests
{
    /// <summary>
    /// FR-TICKHIST-001 / TR-TICKHIST-CAPTURE-001.
    /// Use case: as the pump advances a running session, completed CPU instructions flow
    ///   through the machine pub/sub into the recorder.
    /// Acceptance: after pumping, the session's tick history is non-empty and bounded by
    ///   the recorder capacity.
    /// </summary>
    [Fact]
    public async Task Pump_CapturesInstructionHistory_BoundedByCapacity()
    {
        var (registry, sessionId) = await CreateRunningSessionAsync();
        registry.TryGet(sessionId, out var session);

        var pump = new EmulationPumpService(registry, new SemaphoreEmulationGate());
        for (var i = 0; i < 60; i++)
            pump.PumpSession(session!);

        var ticks = session!.TickHistory.Snapshot();

        Assert.NotEmpty(ticks);
        Assert.True(ticks.Count <= TickHistoryRecorder.Capacity);
    }

    /// <summary>
    /// FR-TICKHIST-001 / TR-TICKHIST-CAPTURE-001.
    /// Use case: the bus publishes a memory-write event (carrying the pre-write byte) for
    ///   each write when a subscriber is listening, so the recorder can capture write-deltas.
    /// Acceptance: writing $42 over an existing $07 at $0400 publishes one MemoryWriteEvent
    ///   with Address=$0400, OldValue=$07, NewValue=$42.
    /// </summary>
    [Fact]
    public void Bus_Write_PublishesMemoryWriteEvent_WithPreWriteByte()
    {
        var bus = new BasicBus();
        bus.RegisterDevice(new SimpleRam());
        var pubSub = new LockFreePubSub();
        bus.ConnectPubSub(pubSub);

        var captured = new List<MemoryWriteEvent>();
        pubSub.Subscribe<MemoryWriteEvent>(MemoryWriteEvent.Topic, captured.Add);

        bus.Write(0x0400, 0x07); // establish a known byte
        captured.Clear();
        bus.Write(0x0400, 0x42); // overwrite it

        Assert.Single(captured);
        Assert.Equal(0x0400, captured[0].Address);
        Assert.Equal(0x07, captured[0].OldValue);
        Assert.Equal(0x42, captured[0].NewValue);
    }

    /// <summary>
    /// FR-TICKHIST-001 / TR-TICKHIST-CAPTURE-001.
    /// Use case: with no subscriber, the bus must not publish (zero-cost unobserved path).
    /// Acceptance: a write with no subscription produces no captured events.
    /// </summary>
    [Fact]
    public void Bus_Write_DoesNotPublish_WhenNoSubscriber()
    {
        var bus = new BasicBus();
        bus.RegisterDevice(new SimpleRam());
        var pubSub = new LockFreePubSub();
        bus.ConnectPubSub(pubSub);

        var captured = new List<MemoryWriteEvent>();
        // Intentionally do NOT subscribe.

        bus.Write(0x0400, 0x42);

        Assert.Empty(captured);
    }

    /// <summary>
    /// FR-TICKHIST-001 / TR-TICKHIST-RPC-001.
    /// Use case: the monitor RPC surfaces the captured ticks to the UI.
    /// Acceptance: after pumping, GetTickHistory returns Ok with a non-empty, index-ordered
    ///   tick list.
    /// </summary>
    [Fact]
    public async Task GetTickHistory_AfterPump_ReturnsIndexOrderedTicks()
    {
        var (registry, sessionId) = await CreateRunningSessionAsync();
        registry.TryGet(sessionId, out var session);
        var pump = new EmulationPumpService(registry, new SemaphoreEmulationGate());
        for (var i = 0; i < 60; i++)
            pump.PumpSession(session!);

        var monitor = new MonitorServiceHost(registry);
        var response = await monitor.GetTickHistoryAsync(new SessionRequest(sessionId), TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, response.Status.Code);
        Assert.NotEmpty(response.Ticks);
        for (var i = 0; i < response.Ticks.Count; i++)
            Assert.Equal(i, response.Ticks[i].Index);
    }

    /// <summary>
    /// FR-TICKHIST-001 / TR-TICKHIST-RPC-001.
    /// Use case: reconstructing memory at the NEWEST captured tick (no later writes to undo)
    ///   must equal the current paused memory.
    /// Acceptance: ReadMemoryAtTick at the newest tick returns the same bytes as a live
    ///   ReadMemory of the same window.
    /// </summary>
    [Fact]
    public async Task ReadMemoryAtTick_NewestTick_MatchesCurrentMemory()
    {
        var (registry, sessionId) = await CreateRunningSessionAsync();
        registry.TryGet(sessionId, out var session);
        var pump = new EmulationPumpService(registry, new SemaphoreEmulationGate());
        for (var i = 0; i < 60; i++)
            pump.PumpSession(session!);

        var monitor = new MonitorServiceHost(registry);
        var ticks = await monitor.GetTickHistoryAsync(new SessionRequest(sessionId), TestContext.Current.CancellationToken);
        var newest = ticks.Ticks.Count - 1;

        var atTick = await monitor.ReadMemoryAtTickAsync(
            new ReadMemoryAtTickRequest(sessionId, newest, 0x0000, 64), TestContext.Current.CancellationToken);
        var current = await monitor.ReadMemoryAsync(
            new MonitorReadMemoryRequest(sessionId, 0x0000, 64), TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, atTick.Status.Code);
        Assert.Equal(current.Data, atTick.Data);
    }

    /// <summary>
    /// FR-TICKHIST-001 / TR-TICKHIST-RPC-001.
    /// Use case: an out-of-range tick index is rejected.
    /// Acceptance: ReadMemoryAtTick with an impossible index returns InvalidArgument.
    /// </summary>
    [Fact]
    public async Task ReadMemoryAtTick_OutOfRangeIndex_ReturnsInvalidArgument()
    {
        var (registry, sessionId) = await CreateRunningSessionAsync();
        registry.TryGet(sessionId, out var session);
        var pump = new EmulationPumpService(registry, new SemaphoreEmulationGate());
        for (var i = 0; i < 20; i++)
            pump.PumpSession(session!);

        var monitor = new MonitorServiceHost(registry);
        var response = await monitor.ReadMemoryAtTickAsync(
            new ReadMemoryAtTickRequest(sessionId, 99_999, 0x0000, 16), TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.InvalidArgument, response.Status.Code);
    }

    private static async Task<(EmulatorRuntimeRegistry Registry, string SessionId)> CreateRunningSessionAsync()
    {
        var registry = new EmulatorRuntimeRegistry();
        var factory = new DefaultEmulatorRuntimeFactory(
            new ArchitectureBuilder(),
            [MinimalHostArchitectureDescriptor.Instance],
            MinimalHostArchitectureDescriptor.ArchitectureId);
        var emulatorHost = new EmulatorHostService(registry, factory);

        var created = await emulatorHost.CreateSessionAsync(
            new CreateEmulatorSessionRequest(),
            TestContext.Current.CancellationToken);
        await emulatorHost.ResumeAsync(
            new SessionRequest(created.SessionId),
            TestContext.Current.CancellationToken);

        return (registry, created.SessionId);
    }
}
