namespace ViceSharp.TestHarness;

using ViceSharp.Chips.VicIi;
using ViceSharp.Chips.Cpu;
using ViceSharp.Core;
using ViceSharp.Abstractions;
using Xunit;

public sealed class VicIiCoreTimingTests
{
    /// <summary>
    /// FR: FR-VIC-006, FR: FR-CPU-002, TR: TR-CYCLE-001.
    /// Use case: During a VIC-II bad-line, the system clock must steal
    /// CPU cycles for the c-access window but keep ticking phi2 side
    /// devices (CIAs, SID, etc.) without skipping them.
    /// Acceptance: While IsDmaStealing is true, the CPU's tick count
    /// stays frozen at the entry value; the phi2 peripheral's tick
    /// count keeps advancing every cycle.
    /// </summary>
    [Fact]
    public void SystemClock_HoldsCpuDuringVicBadLineDmaButContinuesPhi2Devices()
    {
        var irq = new InterruptLine(InterruptType.Irq);
        var vic = new Mos6569(new BasicBus(), irq);
        var cpu = new CountingCpu();
        var peripheral = new CountingPhi2Device();
        var clock = new SystemClock();

        clock.Register(cpu);
        clock.Register(vic);
        clock.Register(peripheral);

        vic.Write(0xD011, 0x10);
        AdvanceClockTo(clock, vic, 0x30, 12);

        Assert.False(vic.IsDmaStealing);
        Assert.Equal(clock.TotalCycles - 1, cpu.TickCount);
        Assert.Equal(clock.TotalCycles, peripheral.TickCount);

        clock.Step(2);

        Assert.True(vic.IsDmaStealing);
        Assert.Equal(clock.TotalCycles - 3, cpu.TickCount);
        Assert.Equal(clock.TotalCycles, peripheral.TickCount);

        AdvanceClockTo(clock, vic, 0x30, 55);

        Assert.False(vic.IsCpuCycleStolen);
        Assert.Equal(clock.TotalCycles - 43, cpu.TickCount);
        Assert.Equal(clock.TotalCycles, peripheral.TickCount);

        clock.Step();

        Assert.False(vic.IsDmaStealing);
        Assert.Equal(clock.TotalCycles - 43, cpu.TickCount);
        Assert.Equal(clock.TotalCycles, peripheral.TickCount);
    }

    /// <summary>
    /// FR: FR-VIC-001, FR: FR-CIA-007, TR: TR-CYCLE-001.
    /// Use case: The VIC-II raster IRQ must assert on the configured
    /// compare line and remain latched in $D019 bit 0 until the
    /// CPU writes a 1 to that bit to clear it.
    /// Acceptance: After 57 cycles IRQ is still low; the next tick
    /// raises IRQ and $D019 reads $F1 (raster flag + IR master + fixed
    /// high bits 6-4); writing $01 to $D019 deasserts IRQ and clears
    /// the flag.
    /// </summary>
    [Fact]
    public void RasterIrq_AssertsAtCompareCycleAndClearsByWriteOneToD019()
    {
        var irq = new InterruptLine(InterruptType.Irq);
        var vic = new Mos6569(new BasicBus(), irq);

        vic.Write(0xD012, 0x00);
        vic.Write(0xD01A, 0x01);

        Advance(vic, 57);
        Assert.False(irq.IsAsserted);
        Assert.Equal(0x70, vic.Read(0xD019));

        vic.Tick();

        Assert.True(irq.IsAsserted);
        Assert.Equal(0xF1, vic.Read(0xD019));
        Assert.Equal(0xF1, vic.Read(0xD019));

        vic.Write(0xD019, 0x01);

        Assert.False(irq.IsAsserted);
        Assert.Equal(0x70, vic.Read(0xD019));
    }

    /// <summary>
    /// FR: FR-VIC-001, TR: TR-CYCLE-001.
    /// Use case: $D012 is a write-to-compare-line register; reading
    /// $D012 returns the current raster line. The raster-IRQ comparator
    /// must use the written value, not the live raster line.
    /// Acceptance: After writing $01 to $D012 (compare line 1), the IRQ
    /// asserts only after the raster reaches line 1 and the configured
    /// cycle, not when the live raster is still on line 0.
    /// </summary>
    [Fact]
    public void RasterIrq_UsesWrittenCompareLineInsteadOfCurrentRasterRegister()
    {
        var irq = new InterruptLine(InterruptType.Irq);
        var vic = new Mos6569(new BasicBus(), irq);

        vic.Write(0xD012, 0x01);
        vic.Write(0xD01A, 0x01);

        Advance(vic, 58);
        Assert.False(irq.IsAsserted);

        Advance(vic, Mos6569.PalCyclesPerLine - 58);
        Assert.Equal(0x01, vic.CurrentRasterLine);

        Advance(vic, 58);
        Assert.True(irq.IsAsserted);
        Assert.Equal(0xF1, vic.Read(0xD019));
    }

    /// <summary>
    /// FR: FR-VIC-006, TR: TR-CYCLE-001.
    /// Use case: A VIC-II bad-line requires DEN, the visible raster
    /// range, and a YScroll match. Changing YScroll mid-line cancels
    /// the bad-line on the current scan line but a matching YScroll on
    /// the following line restores it.
    /// Acceptance: With DEN/YScroll=$10 on line $30, IsBadLine is true;
    /// writing YScroll=$11 (no match) clears IsBadLine; advancing to
    /// line $31 with YScroll=$11 restores IsBadLine.
    /// </summary>
    [Fact]
    public void BadLine_RequiresDenVisibleRangeAndYScrollMatch()
    {
        var vic = new Mos6569(new BasicBus(), new InterruptLine(InterruptType.Irq));

        vic.Write(0xD011, 0x10);
        AdvanceTo(vic, 0x30, 0);
        Assert.Equal(0x30, vic.CurrentRasterLine);
        Assert.True(vic.IsBadLine);

        vic.Write(0xD011, 0x11);
        Assert.False(vic.IsBadLine);

        Advance(vic, Mos6569.PalCyclesPerLine);

        Assert.Equal(0x31, vic.CurrentRasterLine);
        Assert.True(vic.IsBadLine);
    }

    /// <summary>
    /// FR: FR-VIC-006, TR: TR-CYCLE-001.
    /// Use case: DEN must be set before the first DMA-line latch on the
    /// line in question; toggling DEN on the same line after the latch
    /// does not retroactively make it a bad-line.
    /// Acceptance: Writing $D011=$10 only after AdvanceTo line $30
    /// leaves IsBadLine false on that line; on the next line, with DEN
    /// already set in time, IsBadLine becomes true.
    /// </summary>
    [Fact]
    public void BadLine_DenMustBeSetBeforeFirstDmaLineLatch()
    {
        var vic = new Mos6569(new BasicBus(), new InterruptLine(InterruptType.Irq));

        AdvanceTo(vic, 0x30, 0);
        vic.Write(0xD011, 0x10);

        Assert.Equal(0x30, vic.CurrentRasterLine);
        Assert.False(vic.IsBadLine);

        vic.Tick();
        AdvanceTo(vic, 0x30, 0);

        Assert.Equal(0x30, vic.CurrentRasterLine);
        Assert.True(vic.IsBadLine);
    }

    /// <summary>
    /// FR: FR-VIC-006, TR: TR-CYCLE-001.
    /// Use case: On a bad-line, the DMA stealing window covers the
    /// video-matrix c-access and character g-access cycles; outside
    /// those cycles the CPU regains the bus.
    /// Acceptance: At raster cycle 14 IsDmaStealing is true and the
    /// VIC is performing a video-matrix access; at cycle 54 the c-access
    /// is over and IsDmaStealing is false (character access continues).
    /// </summary>
    [Fact]
    public void BadLine_DmaStealingWindowTracksCharacterFetchCycles()
    {
        var vic = new Mos6569(new BasicBus(), new InterruptLine(InterruptType.Irq));

        vic.Write(0xD011, 0x10);
        AdvanceTo(vic, 0x30, 0);

        Assert.True(vic.IsBadLine);
        Assert.False(vic.IsDmaStealing);

        Advance(vic, 14);
        Assert.Equal(14, vic.RasterX);
        Assert.True(vic.IsDmaStealing);
        Assert.True(vic.IsVideoMatrixAccess);

        Advance(vic, 40);
        Assert.Equal(54, vic.RasterX);
        Assert.False(vic.IsDmaStealing);
        Assert.True(vic.IsCharacterAccess);
    }

    /// <summary>
    /// FR: FR-VIC-006, TR: TR-CYCLE-001.
    /// Use case: The last DMA line of the display (line $F7 with
    /// YScroll=7) is still a bad-line; line $F8 is past the visible
    /// range and cannot be a bad-line even with the same YScroll.
    /// Acceptance: On line $F7 IsBadLine is true and IsDmaStealing
    /// fires at cycle 14; advancing to line $F8 clears IsBadLine.
    /// </summary>
    [Fact]
    public void BadLine_IncludesLastDmaLineWhenYScrollMatches()
    {
        var vic = new Mos6569(new BasicBus(), new InterruptLine(InterruptType.Irq));

        vic.Write(0xD011, 0x17);
        AdvanceTo(vic, 0xF7, 0);

        Assert.True(vic.IsBadLine);

        Advance(vic, 14);

        Assert.Equal(14, vic.RasterX);
        Assert.True(vic.IsDmaStealing);

        AdvanceTo(vic, 0xF8, 0);

        Assert.False(vic.IsBadLine);
    }

    /// <summary>
    /// FR: FR-CPU-003, FR: FR-VIC-006, TR: TR-CYCLE-001.
    /// Use case: A conditional steal request that ultimately does NOT
    /// skip the CPU must still allow the CPU to honour an IRQ pulse
    /// from a separate device on the same cycle.
    /// Acceptance: With a scripted stealer that asks but does not
    /// preempt, plus an IRQ pulse asserting on tick 2, two clock steps
    /// drive the CPU into its IRQ vector at $4000 with the correct
    /// stack state and X register unchanged from the in-flight
    /// instruction's already-executed effect.
    /// </summary>
    [Fact]
    public void SystemClock_DeliversIrqWhenConditionalStealRequestDoesNotSkipCpu()
    {
        var bus = new TestMemoryBus();
        bus.SetMemory(0x0200, 0xCA);
        bus.SetMemory(0xFFFE, 0x00);
        bus.SetMemory(0xFFFF, 0x40);
        var irq = new InterruptLine(InterruptType.Irq);
        var cpu = new Mos6502(bus)
        {
            PC = 0x0200,
            X = 0x01,
            S = 0xFF,
            P = 0x00
        };
        var stealer = new ScriptedCycleStealer(requestOnTick: 2);
        var irqPulse = new IrqPulseDevice(irq, assertOnTick: 2);
        var clock = new SystemClock(985_248, cpu, irq);

        clock.Register(cpu);
        clock.Register(stealer);
        clock.Register(irqPulse);

        clock.Step();
        clock.Step();

        Assert.Equal(0x4000, cpu.PC);
        Assert.Equal(0xFC, cpu.S);
        Assert.Equal(0x00, cpu.X);
    }

    private static void Advance(Mos6569 vic, int cycles)
    {
        for (var cycle = 0; cycle < cycles; cycle++)
            vic.Tick();
    }

    private static void AdvanceTo(Mos6569 vic, ushort rasterLine, byte rasterCycle)
    {
        var maxCycles = vic.TotalLines * vic.CyclesPerLine * 2;
        for (var cycle = 0; cycle < maxCycles; cycle++)
        {
            if (vic.CurrentRasterLine == rasterLine && vic.RasterX == rasterCycle)
                return;

            vic.Tick();
        }

        throw new InvalidOperationException($"VIC did not reach line ${rasterLine:X3}, cycle {rasterCycle}.");
    }

    private static void AdvanceClockTo(SystemClock clock, Mos6569 vic, ushort rasterLine, byte rasterCycle)
    {
        var maxCycles = vic.TotalLines * vic.CyclesPerLine * 2;
        for (var cycle = 0; cycle < maxCycles; cycle++)
        {
            if (vic.CurrentRasterLine == rasterLine && vic.RasterX == rasterCycle)
                return;

            clock.Step();
        }

        throw new InvalidOperationException($"Clock did not reach VIC line ${rasterLine:X3}, cycle {rasterCycle}.");
    }

    private sealed class CountingCpu : ICpu, ICpuCycleStealTarget
    {
        public DeviceId Id => new(0x9001);
        public string Name => "Counting CPU";
        public uint ClockDivisor => 1;
        public ClockPhase Phase => ClockPhase.Phi2;
        public ushort PC { get; set; }
        public byte Flags { get; set; }
        public long TickCount { get; private set; }
        public bool CanStealCurrentCycle => true;
        public bool CanForceStealCurrentCycle => false;

        public void Tick() => TickCount++;
        public void Reset() => TickCount = 0;
        public void Initialize() { }
        public int ExecuteInstruction() => 0;
        public void Irq() { }
        public void Nmi() { }
    }

    private sealed class CountingPhi2Device : IClockedDevice
    {
        public DeviceId Id => new(0x9002);
        public string Name => "Counting Phi2";
        public uint ClockDivisor => 1;
        public ClockPhase Phase => ClockPhase.Phi2;
        public long TickCount { get; private set; }

        public void Tick() => TickCount++;
        public void Reset() => TickCount = 0;
        public void Initialize() { }
    }

    private sealed class ScriptedCycleStealer(int requestOnTick) : IClockedDevice, ICpuCycleStealer
    {
        private int _ticks;

        public DeviceId Id => new(0x9003);
        public string Name => "Scripted cycle stealer";
        public uint ClockDivisor => 1;
        public ClockPhase Phase => ClockPhase.Phi1;
        public bool IsCpuCycleStolen => _ticks == requestOnTick;
        public bool IsCpuCycleStealMandatory => false;

        public void Tick() => _ticks++;
        public void Reset() => _ticks = 0;
        public void Initialize() { }
    }

    private sealed class IrqPulseDevice(IInterruptLine irq, int assertOnTick) : IClockedDevice, IInterruptSource
    {
        private int _ticks;

        public DeviceId Id => SourceId;
        public string Name => "IRQ pulse";
        public uint ClockDivisor => 1;
        public ClockPhase Phase => ClockPhase.Phi2;
        public DeviceId SourceId => new(0x9004);
        public IReadOnlyList<IInterruptLine> ConnectedLines { get; } = [irq];

        public void Tick()
        {
            _ticks++;
            if (_ticks == assertOnTick)
                irq.Assert(this);
        }

        public void Reset()
        {
            _ticks = 0;
            irq.Release(this);
        }

        public void Initialize() { }
    }

    private sealed class TestMemoryBus : IBus
    {
        private readonly byte[] _memory = new byte[65536];

        public void SetMemory(int address, byte value) => _memory[address & 0xFFFF] = value;
        public byte Read(ushort address) => _memory[address];
        public void Write(ushort address, byte value) => _memory[address] = value;
        public byte Peek(ushort address) => _memory[address];
        public void RegisterDevice(IAddressSpace device) { }
        public void UnregisterDevice(IAddressSpace device) { }
    }

    /// <summary>
    /// BACKFILL-VIDEO-001 (native depth) / TR-VIC-EDGE-00X (FLI/AFLI timing depth) /
    /// FR-VIC-002 / FR-VIC-003 / FR-VIC-007 / TEST-VIC-001.
    ///
    /// Use case / acceptance criteria: FLI (and AFLI on non-PAL) forces badlines on every raster line
    /// by writing YSCROLL each line to match (CurrentRasterLine &amp; 7). IsForcedBadline must be true,
    /// IsBadLine must follow for DMA/fetch windows, and IsDmaStealing / video matrix access must be
    /// asserted during the appropriate cycles even across consecutive lines (normal badline latch
    /// alone is insufficient for full FLI depth).
    ///
    /// VICE sources (from explore subagent report ID 019e6acc-29b8-77f1-a9cc-56499af366f9):
    /// native/vice/vice/src/viciisc/vicii-cycle.c (raster handler + badline force for FLI at start-of-line
    /// and per-cycle evaluation), vicii-fetch.c:275-309 (sprite ptr + data fetch side effects under forced
    /// badlines), vicii-draw-cycle.c (timing interaction of FLI forced DMA with priority/collision).
    /// Additional: vicii-chip-model.c for model-specific FLI timing on NTSC.
    ///
    /// BDP: This [Fact] + expectations constitute the tests/mocks-first gate for the narrow FLI/AFLI
    /// timing depth slice. Simulator-like assertions exercised on every run before any Mos6569 predicate
    /// change. Full relevant suite (including this + lockstep) must stay green.
    /// </summary>
    [Fact]
    public void FliTiming_ForcedBadlines_EveryLineByYScrollUpdate()
    {
        var vic = new Mos6569(new BasicBus(), new InterruptLine(InterruptType.Irq));

        // DEN set, start with YSCROLL=0
        vic.Write(0xD011, 0x10); // DEN=1, YSCROLL=0

        // Line $30 (&amp;7=0) matches YSCROLL=0 -> normal + forced
        AdvanceTo(vic, 0x30, 0);
        Assert.True(vic.IsForcedBadline, "FLI: line $30 must report forced badline when DEN+YSCROLL match");
        Assert.True(vic.IsBadLine, "FLI: IsBadLine must be true for forced case on line $30");

        // FLI pattern: advance to next line, update YSCROLL to 1 to force $31 (&amp;7 ==1)
        Advance(vic, vic.CyclesPerLine);
        Assert.Equal(0x31, vic.CurrentRasterLine);
        vic.Write(0xD011, 0x11); // YSCROLL=1 now matches line &amp;7
        Assert.True(vic.IsForcedBadline, "FLI: line $31 must be forced bad after YSCROLL update");
        Assert.True(vic.IsBadLine, "FLI depth: IsBadLine follows forced on consecutive line");

        // DMA stealing window must be active under forced badline (data-fetch side effect)
        Advance(vic, 20); // into c-access window ~14+
        Assert.True(vic.IsDmaStealing || vic.IsVideoMatrixAccess,
            "FLI: forced badline must drive DMA stealing / video matrix access for fetch timing parity");
    }
}
