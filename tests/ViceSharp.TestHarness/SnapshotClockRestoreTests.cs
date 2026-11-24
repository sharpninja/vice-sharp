namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Chips.Cpu;
using ViceSharp.Core;
using ViceSharp.Host.Runtime;
using ViceSharp.Host.Services;
using ViceSharp.Protocol;
using Xunit;

/// <summary>
/// FR-SNP-001/002, TR-STATE-001, TR-DET-001, TEST-SNP-001:
/// restoring a host checkpoint restores its clock and dispatch position, without
/// claiming that the legacy chip register snapshot captures all chip microstate.
/// </summary>
public sealed class SnapshotClockRestoreTests
{
    /// <summary>
    /// FR-SNP-001 / FR-SNP-002.
    /// Use case: Capture a running machine, advance, then restore.
    /// Acceptance: Status and the master clock rewind, and the next tick follows the
    /// restored position.
    /// </summary>
    [Fact]
    public async Task CaptureAdvanceRestore_RewindsMasterClockAndStatus()
    {
        var (session, service) = CreateSession();
        session.Machine.Clock.Step(37);
        var captured = await service.CaptureSnapshotAsync(new(session.SessionId), TestContext.Current.CancellationToken);
        Assert.True(captured.Status.IsSuccess);
        session.Machine.Clock.Step(29);

        var restored = await service.RestoreSnapshotAsync(new(session.SessionId, captured.Snapshot!), TestContext.Current.CancellationToken);

        Assert.True(restored.Status.IsSuccess);
        Assert.Equal(37, session.Machine.Clock.TotalCycles);
        Assert.Equal(37, session.Machine.GetState().Cycle);
        Assert.Equal(37, restored.EmulatorStatus!.Cycle);
        session.Machine.Clock.Step();
        Assert.Equal(38, session.Machine.Clock.TotalCycles);
    }

    /// <summary>
    /// FR-SNP-002 / FR-SNP-003.
    /// Use case: Save a clock with slower devices registered after cycle zero.
    /// Acceptance: Restoring replays both devices' divider phases.
    /// </summary>
    [Fact]
    public async Task CaptureAdvanceRestore_ReplaysBothSlowDevicePhases()
    {
        var (session, service) = CreateSession();
        var clock = session.Machine.Clock;
        clock.Step(5);
        var events = new List<(uint Id, long Cycle)>();
        clock.Register(new TickRecorder(clock, events, 700, 3, ClockPhase.Phi1));
        clock.Register(new TickRecorder(clock, events, 701, 4, ClockPhase.Phi2));
        clock.Step(2);
        var captured = await service.CaptureSnapshotAsync(new(session.SessionId), TestContext.Current.CancellationToken);
        events.Clear();
        clock.Step(12);
        var expected = events.ToArray();
        clock.Step(5);

        var restored = await service.RestoreSnapshotAsync(new(session.SessionId, captured.Snapshot!), TestContext.Current.CancellationToken);
        Assert.True(restored.Status.IsSuccess);
        events.Clear();
        clock.Step(12);

        Assert.Equal(expected, events);
    }

    /// <summary>
    /// FR-SNP-002.
    /// Use case: Restore requests a DTO cycle beyond the signed clock range.
    /// Acceptance: The request is rejected before RAM or clock mutation.
    /// </summary>
    [Fact]
    public async Task Restore_UnrepresentableCycle_LeavesMachineUnchanged()
    {
        var (session, service) = CreateSession();
        session.Machine.Clock.Step(7);
        var captured = await service.CaptureSnapshotAsync(new(session.SessionId), TestContext.Current.CancellationToken);
        session.Machine.Bus.Write(0x4000, 0x99);
        session.Machine.Clock.Step(11);

        var restored = await service.RestoreSnapshotAsync(new(session.SessionId,
            captured.Snapshot! with { Cycle = ulong.MaxValue }), TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.InvalidArgument, restored.Status.Code);
        Assert.Equal(18, session.Machine.Clock.TotalCycles);
        Assert.Equal(0x99, session.Machine.Bus.Peek(0x4000));
    }

    /// <summary>
    /// FR-SNP-002.
    /// Use case: Load a version-2 snapshot without full transient clock data.
    /// Acceptance: The cycle is rebased and the result warns that timing restoration is
    /// best effort.
    /// </summary>
    [Fact]
    public async Task Restore_LegacyV2_RebasesCycleWithBestEffortWarning()
    {
        var (session, service) = CreateSession();
        session.Machine.Clock.Step(37);
        var snapshot = new SnapshotDto(MachineSnapshotStager.FormatV2, 37,
            new MachineSnapshotStager().Capture(session.Machine));
        session.Machine.Clock.Step(29);
        var restored = await service.RestoreSnapshotAsync(new(session.SessionId, snapshot), TestContext.Current.CancellationToken);

        Assert.True(restored.Status.IsSuccess);
        Assert.Contains("Best-effort", restored.Status.Message);
        Assert.Equal(37, session.Machine.Clock.TotalCycles);
        session.Machine.Clock.Step();
        Assert.Equal(38, session.Machine.Clock.TotalCycles);
    }

    /// <summary>
    /// FR-SNP-002.
    /// Use case: Restore input has malformed clock state or mismatched DTO metadata.
    /// Acceptance: RAM, CPU, and clock state are unchanged.
    /// </summary>
    [Theory]
    [InlineData("truncated")]
    [InlineData("trailing")]
    [InlineData("version")]
    [InlineData("cycle")]
    [InlineData("frequency")]
    [InlineData("schedule")]
    public async Task Restore_InvalidClock_LeavesRamCpuAndClockUnchanged(string corruption)
    {
        var (session, service) = CreateSession();
        session.Machine.Clock.Step(37);
        var captured = (await service.CaptureSnapshotAsync(new(session.SessionId), TestContext.Current.CancellationToken)).Snapshot!;
        Assert.Equal(MachineSnapshotStager.FormatV3, captured.Format);
        var payload = captured.Payload.ToArray();
        var cycle = captured.Cycle;
        switch (corruption)
        {
            case "truncated": payload = payload[..^1]; break;
            case "trailing": payload = [.. payload, 0]; break;
            case "version": payload[0] = 0xFF; break;
            case "cycle": cycle++; break;
            case "frequency": payload[2 + new MachineSnapshotStager().Capture(session.Machine).Length + 1] ^= 1; break;
            case "schedule": session.Machine.Clock.Register(new TickRecorder(session.Machine.Clock, [], 700, 3, ClockPhase.Phi1)); break;
        }
        session.Machine.Clock.Step(11);
        session.Machine.Bus.Write(0x4000, 0x99);
        var before = session.Machine.GetState();
        var clockBefore = ((SystemClock)session.Machine.Clock).CaptureSnapshot();
        var restored = await service.RestoreSnapshotAsync(new(session.SessionId, captured with { Payload = payload, Cycle = cycle }),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.InvalidArgument, restored.Status.Code);
        Assert.Equal(before, session.Machine.GetState());
        Assert.Equal(clockBefore, ((SystemClock)session.Machine.Clock).CaptureSnapshot());
        Assert.Equal(0x99, session.Machine.Bus.Peek(0x4000));
    }

    /// <summary>
    /// FR-SNP-001 / FR-SNP-002.
    /// Use case: Capture and restore a machine with a separately clocked peripheral.
    /// Acceptance: Both coordinated clocks rewind to their captured positions.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Coordinator_CaptureRestore_RewindsBothClockPositions(bool legacy)
    {
        var host = MachineTestFactory.CreateC64Machine();
        var peripheral = MachineTestFactory.CreateC64Machine("ntsc");
        host.Reset();
        peripheral.Reset();
        var coordinator = new SystemCoordinator();
        coordinator.AttachSystem(host);
        coordinator.AttachSystem(peripheral);
        var machine = new CoordinatorMachine(host, coordinator, 19656);
        var registry = new EmulatorRuntimeRegistry();
        var session = new EmulatorRuntimeSession("coordinated", host.Architecture, machine);
        registry.Add(session);
        var service = new SnapshotServiceHost(registry);
        coordinator.Step(37);
        var captured = await service.CaptureSnapshotAsync(new(session.SessionId), TestContext.Current.CancellationToken);
        var peripheralCycle = peripheral.Clock.TotalCycles;
        var snapshot = legacy
            ? new SnapshotDto(MachineSnapshotStager.FormatV2, 37, new MachineSnapshotStager().Capture(machine))
            : captured.Snapshot!;
        if (!legacy) Assert.Equal(MachineSnapshotStager.FormatV3, snapshot.Format);
        coordinator.Step(29);
        var restored = await service.RestoreSnapshotAsync(new(session.SessionId, snapshot), TestContext.Current.CancellationToken);

        Assert.True(restored.Status.IsSuccess);
        if (legacy) Assert.Contains("Best-effort", restored.Status.Message);
        Assert.Equal(37, host.Clock.TotalCycles);
        Assert.Equal(peripheralCycle, peripheral.Clock.TotalCycles);
        Assert.Equal(37, coordinator.TotalHostCycles);
        coordinator.Step();
        Assert.Equal(38, host.Clock.TotalCycles);
        Assert.Equal(38 * peripheral.Clock.FrequencyHz / host.Clock.FrequencyHz, peripheral.Clock.TotalCycles);
    }

    /// <summary>
    /// FR-SNP-001 / FR-SNP-002.
    /// Use case: Capture a factory-wired C64 with a real 1541 on unit 8 or 9.
    /// Acceptance: The master, drive, and coordinator clock bytes round-trip.
    /// </summary>
    [Theory]
    [InlineData(8)]
    [InlineData(9)]
    public async Task TrueDrive_CaptureRestore_RoundTripsAllClockState(int unit)
    {
        var factory = C64MediaTestHost.CreateRuntimeFactory();
        var session = factory.Create(new CreateEmulatorSessionRequest("c64", TrueDrive: true, TrueDriveDevice: unit));
        var rig = Assert.IsType<CoordinatorMachine>(session.Machine);
        var registry = new EmulatorRuntimeRegistry();
        registry.Add(session);
        var service = new SnapshotServiceHost(registry);
        rig.Coordinator.Step(37);
        var clockState = rig.Coordinator.CaptureClockSnapshot();
        var captured = await service.CaptureSnapshotAsync(new(session.SessionId), TestContext.Current.CancellationToken);
        Assert.Equal(MachineSnapshotStager.FormatV3, captured.Snapshot!.Format);
        rig.Coordinator.Step(29);

        var restored = await service.RestoreSnapshotAsync(new(session.SessionId, captured.Snapshot), TestContext.Current.CancellationToken);

        Assert.True(restored.Status.IsSuccess, restored.Status.Message);
        Assert.Equal(37, restored.EmulatorStatus!.Cycle);
        Assert.Equal(clockState, rig.Coordinator.CaptureClockSnapshot());
        rig.Coordinator.Step();
        Assert.Equal(38, rig.Coordinator.TotalHostCycles);
        Assert.All(rig.Coordinator.Systems, system => Assert.True(system.Clock.TotalCycles > 37));
    }

    /// <summary>
    /// FR-SNP-002.
    /// Use case: Restore a drive snapshot on a different unit with the same clock
    /// frequency.
    /// Acceptance: The restore is rejected before machine or drive state mutates.
    /// </summary>
    [Fact]
    public async Task TrueDrive_WrongUnit_RejectsBeforeMutation()
    {
        var factory = C64MediaTestHost.CreateRuntimeFactory();
        var source = factory.Create(new CreateEmulatorSessionRequest("c64", TrueDrive: true, TrueDriveDevice: 8));
        var target = factory.Create(new CreateEmulatorSessionRequest("c64", TrueDrive: true, TrueDriveDevice: 9));
        var registry = new EmulatorRuntimeRegistry();
        registry.Add(source);
        registry.Add(target);
        var service = new SnapshotServiceHost(registry);
        var captured = await service.CaptureSnapshotAsync(new(source.SessionId), TestContext.Current.CancellationToken);
        var rig = Assert.IsType<CoordinatorMachine>(target.Machine);
        rig.Coordinator.Step(29);
        target.Machine.Bus.Write(0x4000, 0x99);
        var clockState = rig.Coordinator.CaptureClockSnapshot();
        var before = target.Machine.GetState();

        var restored = await service.RestoreSnapshotAsync(new(target.SessionId, captured.Snapshot!), TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.InvalidArgument, restored.Status.Code);
        Assert.Equal(before, target.Machine.GetState());
        Assert.Equal(clockState, rig.Coordinator.CaptureClockSnapshot());
        Assert.Equal(0x99, target.Machine.Bus.Peek(0x4000));
    }

    /// <summary>
    /// FR-SNP-002.
    /// Use case: Restore a drive snapshot containing malformed binary identity lengths.
    /// Acceptance: The result is InvalidArgument and RAM and all clocks retain their old
    /// state.
    /// </summary>
    [Fact]
    public async Task TrueDrive_MalformedIdentityLength_ReturnsInvalidArgumentWithoutMutation()
    {
        var session = C64MediaTestHost.CreateRuntimeFactory().Create(new CreateEmulatorSessionRequest("c64", TrueDrive: true));
        var rig = Assert.IsType<CoordinatorMachine>(session.Machine);
        var registry = new EmulatorRuntimeRegistry();
        registry.Add(session);
        var service = new SnapshotServiceHost(registry);
        var captured = (await service.CaptureSnapshotAsync(new(session.SessionId), TestContext.Current.CancellationToken)).Snapshot!;
        var payload = captured.Payload.ToArray();
        // v3 envelope + v2 chip section + clock kind, then coordinator version,
        // TotalHostCycles and system count precede the first length-prefixed identity.
        var identityOffset = 2 + new MachineSnapshotStager().Capture(session.Machine).Length + 1 + 8 + 4;
        payload.AsSpan(identityOffset, 5).Fill(0xFF);
        rig.Coordinator.Step(29);
        session.Machine.Bus.Write(0x4000, 0x99);
        var before = rig.Coordinator.CaptureClockSnapshot();

        var response = await service.RestoreSnapshotAsync(new(session.SessionId, captured with { Payload = payload }),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.InvalidArgument, response.Status.Code);
        Assert.Equal(before, rig.Coordinator.CaptureClockSnapshot());
        Assert.Equal(0x99, session.Machine.Bus.Peek(0x4000));
    }

    /// <summary>
    /// FR-SNP-002 / FR-SNP-003.
    /// Use case: Transfer pending interrupts and line state to an independent clock.
    /// Acceptance: NMI, IRQ delay, and unconsumed line edges replay identically.
    /// </summary>
    [Fact]
    public void ClockSnapshot_RestoresInterruptTimingAndPhysicalLines()
    {
        var source = CreateInterruptRig();
        source.Irq.Assert(source.Stealer);
        source.Nmi.Assert(source.Stealer);
        source.Clock.Step(2); // CPU is stalled: NMI remains pending, IRQ delay advances.
        source.Irq.Release(source.Stealer);
        source.Irq.Assert(source.Stealer); // unconsumed combined-line edge
        var snapshot = source.Clock.CaptureSnapshot();
        Assert.True(source.Clock.DebugIrqDelayCycles > 0);
        source.Stealer.Steal = false;
        source.Nmi.Release(source.Stealer); // captured pending NMI must still dispatch
        var expected = Trace(source.Clock, source.Cpu, 32);

        var restored = CreateInterruptRig();
        restored.Clock.Step(19);
        restored.Clock.RestoreSnapshot(snapshot);
        Assert.Equal(snapshot, restored.Clock.CaptureSnapshot());
        Assert.True(restored.Irq.IsAsserted);
        Assert.True(restored.Nmi.IsAsserted);
        restored.Stealer.Steal = false;
        restored.Nmi.Release(restored.Stealer);
        Assert.Equal(expected, Trace(restored.Clock, restored.Cpu, 32));
    }

    /// <summary>
    /// FR-SNP-002.
    /// Use case: Load a legacy cycle position while the clock has pending transient
    /// state.
    /// Acceptance: Stale edges clear, divisor phases restart at the restored origin, and
    /// devices retain their state.
    /// </summary>
    [Fact]
    public void LegacyClockPosition_ReinitializesTransientTimingWithoutResettingDevices()
    {
        var rig = CreateInterruptRig();
        var events = new List<(uint Id, long Cycle)>();
        rig.Clock.Register(new TickRecorder(rig.Clock, events, 700, 4, ClockPhase.Phi1));
        rig.Irq.Assert(rig.Stealer);
        rig.Nmi.Assert(rig.Stealer);
        rig.Clock.Step(7);
        rig.Clock.RestoreLegacyPosition(10);
        Assert.Equal(10, rig.Clock.DebugIrqAssertCycle);
        Assert.Equal(0, rig.Clock.DebugIrqDelayCycles);
        Assert.False(rig.Irq.ConsumeRisingEdge());
        events.Clear();
        rig.Clock.Step(2);
        Assert.Equal(new[] { (700U, 12L) }, events);
    }

    private static (SystemClock Clock, Mos6502 Cpu, InterruptLine Irq, InterruptLine Nmi, Stealer Stealer) CreateInterruptRig()
    {
        var bus = new MockBus();
        for (var address = 0; address < 0x10000; address++) bus.SetMemory((ushort)address, 0xEA);
        bus.SetMemory(0xFFFC, 0x00);
        bus.SetMemory(0xFFFD, 0x04);
        bus.SetMemory(0xFFFA, 0x00);
        bus.SetMemory(0xFFFB, 0x08);
        var cpu = new Mos6502(bus);
        cpu.Reset();
        var irq = new InterruptLine(InterruptType.Irq);
        var nmi = new InterruptLine(InterruptType.Nmi);
        var clock = new SystemClock(985248, cpu, irq, nmi);
        var stealer = new Stealer();
        clock.Register(stealer);
        clock.Register(cpu);
        return (clock, cpu, irq, nmi, stealer);
    }

    private static (long Cycle, ushort Pc, byte Stack, long IrqCycle, int IrqDelay)[] Trace(SystemClock clock, Mos6502 cpu, int count)
    {
        var trace = new (long, ushort, byte, long, int)[count];
        for (var i = 0; i < count; i++)
        {
            clock.Step();
            trace[i] = (clock.TotalCycles, cpu.PC, cpu.S, clock.DebugIrqAssertCycle, clock.DebugIrqDelayCycles);
        }
        return trace;
    }

    private sealed class Stealer : IClockedDevice, ICpuCycleStealer, IInterruptSource
    {
        public bool Steal { get; set; } = true;
        public bool IsCpuCycleStolen => Steal;
        public bool IsCpuCycleStealMandatory => Steal;
        public DeviceId Id => new(777);
        public DeviceId SourceId => Id;
        public string Name => "Clock snapshot test interrupt source";
        public uint ClockDivisor => 1;
        public ClockPhase Phase => ClockPhase.Phi1;
        public IReadOnlyList<IInterruptLine> ConnectedLines => [];
        public void Tick() { }
        public void Reset() { }
    }

    private sealed class MockBus : IBus
    {
        private readonly byte[] _memory = new byte[65536];
        public void SetMemory(ushort address, byte value) => _memory[address] = value;
        public byte Read(ushort address) => _memory[address];
        public byte Peek(ushort address) => _memory[address];
        public void Write(ushort address, byte value) => _memory[address] = value;
        public void RegisterDevice(IAddressSpace device) { }
        public void UnregisterDevice(IAddressSpace device) { }
    }

    private static (EmulatorRuntimeSession Session, SnapshotServiceHost Service) CreateSession()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        machine.Reset();
        var session = new EmulatorRuntimeSession("clock-snapshot", machine.Architecture, machine);
        var registry = new EmulatorRuntimeRegistry();
        registry.Add(session);
        return (session, new SnapshotServiceHost(registry));
    }

    private sealed class TickRecorder(IClock clock, List<(uint Id, long Cycle)> events,
        uint id, uint divisor, ClockPhase phase) : IClockedDevice
    {
        public DeviceId Id => new(id);
        public string Name => "Snapshot clock recorder";
        public uint ClockDivisor => divisor;
        public ClockPhase Phase => phase;
        public void Tick() => events.Add((id, clock.TotalCycles));
        public void Reset() => events.Clear();
    }
}
