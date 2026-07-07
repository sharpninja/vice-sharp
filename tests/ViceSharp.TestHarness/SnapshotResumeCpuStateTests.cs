using ViceSharp.Abstractions;
using ViceSharp.Chips.Cpu;
using Xunit;

namespace ViceSharp.TestHarness;

/// <summary>
/// FR/TR/TEST: FR-VIC-CYCLE / TR-LOCKSTEP-VSF-001 / TEST-RUNLOG-PARITY-001.
///
/// Managed snapshot-resume CPU state: the managed Mos6502 must accept the .vsf
/// MAINCPU register file mid-run and resume with VICE's x64sc semantics. The
/// native hosted bootstrap (native/vice/vice/src/mainc64cpu.c, hosted block in
/// maincpu_mainloop) adopts the snapshot-restored register file, JUMPs to the
/// restored PC and clears the per-run micro-op bookkeeping (last_opcode_info,
/// stolen_cycles, check_ba_low, rmw flag), then re-enters the instruction fetch
/// loop - i.e. the in-flight instruction RESTARTS from its first cycle. The
/// managed CPU mirrors that contract through
/// <see cref="Mos6502.InjectSnapshotResumeState"/>.
/// </summary>
public sealed class SnapshotResumeCpuStateTests
{
    /// <summary>
    /// FR: FR-VIC-CYCLE, TR: TR-LOCKSTEP-VSF-001, TEST: TEST-RUNLOG-PARITY-001.
    /// Use case: LockstepValidator.StageManagedFromNative resumes the managed C64
    /// from a .vsf that VICE wrote inside a raster-poll loop (the READY fixture
    /// stops at "$5766: CMP $D012 / $5769: BNE $5766" with PC=$5769). The managed
    /// CPU must accept that mid-run register file and reproduce VICE's visible
    /// per-cycle register timeline: x64sc exports registers once per cycle from
    /// the hosted CLK_INC (native/vice/vice/src/c64/c64cpusc.c), so a taken
    /// branch shows its fall-through PC on its final (dummy-fetch) cycle
    /// (6510dtvcore.c BRANCH macro: INC_PC(2) then LOAD_DUMMY + CLK_INC before
    /// JUMP), and CMP abs holds the pre-instruction PC/P through all four of its
    /// cycles, with the new PC/P first exported during the next instruction's
    /// opcode-fetch cycle (CMP computes flags and INC_PC(3) after GET_ABS's
    /// final CLK_INC returns).
    /// Acceptance: after InjectSnapshotResumeState(A=$C2, P=$20, PC=$5769) over
    /// RAM staged with the READY fixture's poll loop and a constant $D012=$E1,
    /// the CPU restarts at a clean instruction boundary (DebugCycle 0, no stale
    /// opcode latch) and the next 8 ticks show exactly the native per-cycle
    /// visible timeline measured from the resumed READY fixture: one resume
    /// stagger tick at $5769, BNE fetch at $5769, branch resolve showing $576B,
    /// four CMP cycles holding PC=$5766 P=$20 (the fourth cycle must NOT yet
    /// show the CMP result), then PC=$5769 P=$A0 on the commit/fetch cycle.
    /// </summary>
    [Fact]
    public void InjectSnapshotResumeState_RasterPollLoop_ReproducesNativeVisibleTimeline()
    {
        var bus = new MockBus();
        // READY fixture poll loop: $5766: CMP $D012 ; $5769: BNE $5766
        bus.SetMemory(0x5766, 0xCD);
        bus.SetMemory(0x5767, 0x12);
        bus.SetMemory(0x5768, 0xD0);
        bus.SetMemory(0x5769, 0xD0);
        bus.SetMemory(0x576A, 0xFB);
        bus.SetMemory(0xD012, 0xE1);

        var cpu = new Mos6502(bus);
        cpu.InjectSnapshotResumeState(a: 0xC2, x: 0x02, y: 0x00, s: 0xFA, p: 0x20, pc: 0x5769);

        Assert.Equal(0xC2, cpu.A);
        Assert.Equal(0x02, cpu.X);
        Assert.Equal(0x00, cpu.Y);
        Assert.Equal(0xFA, cpu.S);
        Assert.Equal(0x20, cpu.P);
        Assert.Equal(0x5769, cpu.PC);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal(0, cpu.DebugOpcode);
        Assert.False(cpu.DebugDelayNextFetch);

        // Native visible timeline measured from the resumed READY fixture
        // (SaveNativeRunLog over ready-c64sc-truedrive.vsf, cycles 1..8) with
        // P recolored by the constant $D012=$E1 read: CMP $C2 vs $E1 clears C,
        // sets N (P $20 -> $A0), first visible on the commit/fetch cycle.
        (ushort Pc, byte P)[] expected =
        [
            (0x5769, 0x20), // resume stagger (native BNE C1)
            (0x5769, 0x20), // BNE fetch      (native BNE C2)
            (0x576B, 0x20), // branch resolve (native BNE C3: fall-through PC visible)
            (0x5766, 0x20), // CMP C1 (opcode fetch at branch target)
            (0x5766, 0x20), // CMP C2 (operand lo)
            (0x5766, 0x20), // CMP C3 (operand hi)
            (0x5766, 0x20), // CMP C4 (read $D012) - result must NOT be visible yet
            (0x5769, 0xA0), // commit + next BNE fetch cycle
        ];

        for (var step = 0; step < expected.Length; step++)
        {
            cpu.Tick();
            Assert.True(
                expected[step].Pc == cpu.PC && expected[step].P == cpu.P,
                $"step {step + 1}: expected PC=${expected[step].Pc:X4} P=${expected[step].P:X2}, " +
                $"actual PC=${cpu.PC:X4} P=${cpu.P:X2} (DebugCycle={cpu.DebugCycle}, opcode=${cpu.DebugOpcode:X2})");
        }
    }

    private sealed class MockBus : IBus
    {
        private readonly byte[] _memory = new byte[65536];

        public void SetMemory(int address, byte value) => _memory[address & 0xFFFF] = value;

        public byte Read(ushort address) => _memory[address];

        public void Write(ushort address, byte value) => _memory[address] = value;

        public byte Peek(ushort address) => _memory[address];

        public void RegisterDevice(IAddressSpace device) { }

        public void UnregisterDevice(IAddressSpace device) { }
    }
}
