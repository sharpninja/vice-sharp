namespace ViceSharp.TestHarness.Xbox;

using System;
using System.Linq;
using ViceSharp.Abstractions;
using ViceSharp.Chips.Cia;
using ViceSharp.Chips.VicIi;
using ViceSharp.Core;
using ViceSharp.Host.Runtime;
using ViceSharp.Host.Services;
using ViceSharp.Protocol;
using Xunit;

/// <summary>
/// FIX-XSNAPWARP-001 (operator 2026-07-14: "After clicking LOAD, the emulator loads the
/// snapshot but restarts in Warp mode"). The v1 runtime snapshot was CPU registers plus
/// a 64KB <c>Bus.Write</c> replay: writing the whole I/O window back through live
/// registers scrambled the CIA ICR masks and timer latches (and the VIC state never
/// round-tripped at all), so the restored game lost its raster/timer IRQ, its main loop
/// free-ran (the "warp" look while the emulator itself paced at ~100%), and broken
/// state restarted it. The v2 machine snapshot captures TRUE state - the RAM array
/// under any banking, color RAM, the 6510 port, the CPU register file, and the full
/// VIC/CIA1/CIA2/SID chip state - and restores it through the same chip injectors the
/// VSF lockstep rig proved cycle-exact (TR-LOCKSTEP-VSF-001).
/// </summary>
/// <remarks>
/// FR: FR-XBOXUI-008 (menu snapshot save/load), FR-SNAP-001. TR: TR-LOCKSTEP-VSF-001.
/// Use case: the player saves mid-game from the shell menu and later loads: the game
/// resumes exactly where it was, IRQ-driven pacing intact.
/// Acceptance:
///   TEST-XSNAP-001a: capture -> diverge -> restore round-trips RAM, color RAM, the
///     6510 port, CPU registers, and the VIC/CIA snapshot state field-for-field.
///   TEST-XSNAP-001b: the restored machine's IRQ chain is ALIVE: the KERNAL jiffy
///     clock keeps ticking across post-restore frames (the exact thing the scrambled
///     ICR killed).
///   TEST-XSNAP-001c: SID registers round-trip (write regs restart their envelopes;
///     that is the documented v2 limitation).
///   TEST-XSNAP-001d: a v1-format snapshot is refused with a re-save message instead
///     of scrambling the machine.
/// </remarks>
[Trait("Category", "Xbox")]
public sealed class XboxMachineSnapshotTests
{
    [Fact]
    public void RoundTrip_RestoresMemoryAndChipState()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        machine.Reset();
        Step(machine, 4_000_000);

        var stager = new MachineSnapshotStager();
        var payload = stager.Capture(machine);

        // Reference state at capture time.
        var ram = ((IMemory)machine.Devices.GetByRole(DeviceRole.SystemRam)!).Span.ToArray();
        var color = CaptureColor(machine);
        var cpu0 = machine.GetState();
        var vic = (Mos6569)machine.Devices.GetByRole(DeviceRole.VideoChip)!;
        var cia1 = (Mos6526)machine.Devices.GetByRole(DeviceRole.Cia1)!;
        var cia2 = (Mos6526)machine.Devices.GetByRole(DeviceRole.Cia2)!;
        var vic0 = vic.CaptureSnapshotState();
        var cia10 = cia1.CaptureSnapshotState();
        var cia20 = cia2.CaptureSnapshotState();
        var port0 = (machine.Bus.Peek(0x0000), machine.Bus.Peek(0x0001));

        // Diverge well away from the captured state.
        Step(machine, 700_000);

        stager.Restore(machine, payload);

        Assert.Equal(ram, ((IMemory)machine.Devices.GetByRole(DeviceRole.SystemRam)!).Span.ToArray());
        Assert.Equal(color, CaptureColor(machine));
        Assert.Equal(port0, (machine.Bus.Peek(0x0000), machine.Bus.Peek(0x0001)));

        var cpu1 = machine.GetState();
        Assert.Equal(
            (cpu0.PC, cpu0.A, cpu0.X, cpu0.Y, cpu0.S, cpu0.P),
            (cpu1.PC, cpu1.A, cpu1.X, cpu1.Y, cpu1.S, cpu1.P));

        var vic1 = vic.CaptureSnapshotState();
        Assert.Equal(vic0.Registers, vic1.Registers);
        Assert.Equal(
            (vic0.RasterLine, vic0.InLineCycle, vic0.AllowBadLines, vic0.IdleState),
            (vic1.RasterLine, vic1.InLineCycle, vic1.AllowBadLines, vic1.IdleState));
        Assert.Equal(
            (vic0.VideoCounter, vic0.VideoCounterBase, vic0.RowCounter, vic0.RefreshCounter, vic0.SpriteDmaActiveMask),
            (vic1.VideoCounter, vic1.VideoCounterBase, vic1.RowCounter, vic1.RefreshCounter, vic1.SpriteDmaActiveMask));

        Assert.Equal(cia10, cia1.CaptureSnapshotState());
        Assert.Equal(cia20, cia2.CaptureSnapshotState());
    }

    [Fact]
    public void RestoredMachine_KeepsTheIrqChainAlive()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        machine.Reset();
        Step(machine, 4_000_000);

        var stager = new MachineSnapshotStager();
        var payload = stager.Capture(machine);

        Step(machine, 700_000);
        stager.Restore(machine, payload);

        // TEST-XSNAP-001b: the KERNAL jiffy clock ($A0-$A2) ticks ONLY from the CIA1
        // timer IRQ. If the restore had scrambled the ICR mask (the v1 defect), the
        // jiffy would freeze and the game logic would free-run.
        var ram = ((IMemory)machine.Devices.GetByRole(DeviceRole.SystemRam)!).Span;
        var jiffyBefore = (ram[0xA0] << 16) | (ram[0xA1] << 8) | ram[0xA2];

        Step(machine, 200_000); // ~10 PAL frames

        var jiffyAfter = (ram[0xA0] << 16) | (ram[0xA1] << 8) | ram[0xA2];
        Assert.True(
            jiffyAfter != jiffyBefore,
            "the jiffy clock must keep ticking after a restore: the CIA IRQ chain died.");
    }

    [Fact]
    public void SidRegisters_RoundTrip()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        machine.Reset();
        Step(machine, 4_000_000);

        // A distinctive voice-1 setup (freq, PW, ADSR) through the banked bus.
        machine.Bus.Write(0xD400, 0x37);
        machine.Bus.Write(0xD401, 0x11);
        machine.Bus.Write(0xD402, 0x55);
        machine.Bus.Write(0xD403, 0x0A);
        machine.Bus.Write(0xD405, 0x28);
        machine.Bus.Write(0xD406, 0xC9);

        var stager = new MachineSnapshotStager();
        var payload = stager.Capture(machine);

        Step(machine, 300_000); // KERNAL boot chatter may touch SID (volume etc.)
        stager.Restore(machine, payload);

        var sid = machine.Devices.GetByRole(DeviceRole.AudioChip)!;
        var peek = (ViceSharp.Abstractions.IAddressSpace)sid;
        Assert.Equal(0x37, peek.Peek(0xD400));
        Assert.Equal(0x11, peek.Peek(0xD401));
        Assert.Equal(0x55, peek.Peek(0xD402));
        Assert.Equal(0x0A, peek.Peek(0xD403));
        Assert.Equal(0x28, peek.Peek(0xD405));
        Assert.Equal(0xC9, peek.Peek(0xD406));
    }

    [Fact]
    public async Task V1Snapshot_IsRefused_WithAResaveMessage()
    {
        var ct = TestContext.Current.CancellationToken;
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateSession(registry);

        var host = new SnapshotServiceHost(registry);
        var response = await host.RestoreSnapshotAsync(
            new RestoreSnapshotRequest(
                session.SessionId,
                new SnapshotDto("vice-sharp.runtime-snapshot.v1", 0, new byte[16])),
            ct);

        Assert.False(response.Status.IsSuccess);
        Assert.Contains("save a new snapshot", response.Status.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>The service captures v2 and the payload round-trips through the service itself.</summary>
    [Fact]
    public async Task SnapshotService_CapturesAndRestores_TheV2Format()
    {
        var ct = TestContext.Current.CancellationToken;
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateSession(registry);
        lock (session.SyncRoot)
            Step(session.Machine, 4_000_000);

        var host = new SnapshotServiceHost(registry);
        var captured = await host.CaptureSnapshotAsync(new SessionRequest(session.SessionId), ct);
        Assert.True(captured.Status.IsSuccess);
        Assert.Equal(MachineSnapshotStager.FormatV2, captured.Snapshot!.Format);

        lock (session.SyncRoot)
            Step(session.Machine, 300_000);

        var restored = await host.RestoreSnapshotAsync(new RestoreSnapshotRequest(session.SessionId, captured.Snapshot), ct);
        Assert.True(restored.Status.IsSuccess);
    }

    private static void Step(IMachine machine, long cycles)
    {
        var clock = machine.Clock;
        var target = clock.TotalCycles + cycles;
        while (clock.TotalCycles < target)
            machine.StepInstruction();
    }

    private static byte[] CaptureColor(IMachine machine)
    {
        // The VIC's board-wired video-memory reader resolves $D800-$DBFF to the color
        // nybbles regardless of CPU banking.
        var vic = (Mos6569)machine.Devices.GetByRole(DeviceRole.VideoChip)!;
        var color = new byte[0x0400];
        for (var i = 0; i < color.Length; i++)
            color[i] = vic.VideoMemoryReader((ushort)(0xD800 + i));
        return color;
    }

    private static EmulatorRuntimeSession CreateSession(EmulatorRuntimeRegistry registry)
    {
        var factory = new DefaultEmulatorRuntimeFactory(
            new ArchitectureBuilder(MachineTestFactory.CreateC64RomProvider()),
            [new ViceSharp.Architectures.C64.C64Descriptor()],
            "c64");

        var session = factory.Create(new CreateEmulatorSessionRequest("c64"));
        registry.Add(session);
        return session;
    }
}
