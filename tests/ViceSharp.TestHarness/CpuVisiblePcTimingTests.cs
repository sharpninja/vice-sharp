namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Chips.Cpu;
using ViceSharp.Core;
using Xunit;

public sealed class CpuVisiblePcTimingTests
{
    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001.
    /// Use case: <c>JSR abs</c> spans six bus cycles; the externally
    /// visible PC must reflect the jump target after the high operand byte
    /// is fetched on cycle 7 (post-reset cycle count).
    /// Acceptance: After 7 ticks following reset, PC equals the JSR target
    /// $9000 (high-byte fetch latched the destination address).
    /// </summary>
    [Fact]
    public void Jsr_FinalCycle_ExposesTargetAfterHighOperandFetch()
    {
        var cpu = CreateJsrCpu();

        for (var i = 0; i < 7; i++)
            cpu.Tick();

        Assert.Equal((ushort)0x9000, cpu.PC);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001.
    /// Use case: Lockstep diagnostics need to subscribe to CPU subroutine
    /// calls so a KERNAL IEC close call can mark the stable post-load window.
    /// Acceptance: Completing <c>JSR $9000</c> publishes a typed control
    /// transfer event with the source PC, target PC, opcode, and return PC.
    /// </summary>
    [Fact]
    public void Jsr_FinalCycle_PublishesControlTransferEvent()
    {
        var cpu = CreateJsrCpu();
        var pubSub = new LockFreePubSub();
        CpuControlTransferEvent? observed = null;
        var subscription = pubSub.Subscribe<CpuControlTransferEvent>(
            CpuControlTransferEvent.Topic,
            transfer => observed = transfer);

        try
        {
            cpu.ConnectPubSub(pubSub);

            for (var i = 0; i < 7; i++)
                cpu.Tick();
        }
        finally
        {
            pubSub.Unsubscribe(subscription);
        }

        Assert.True(observed.HasValue);
        Assert.Equal((ushort)0x8000, observed.Value.Source);
        Assert.Equal((ushort)0x9000, observed.Value.Target);
        Assert.Equal((ushort)0x8003, observed.Value.ReturnPc);
        Assert.Equal((byte)0x20, observed.Value.Opcode);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001.
    /// Use case: The final operand read of <c>JSR abs</c> must remain a
    /// stealable bus cycle so VIC-II DMA can suspend the CPU at exactly
    /// that point without corrupting the instruction.
    /// Acceptance: After 6 ticks, the CPU reports DebugCycle == 1 and
    /// <see cref="Mos6502.CanStealCurrentCycle"/> is true.
    /// </summary>
    [Fact]
    public void Jsr_FinalOperandRead_CanBeHeldByExternalBusOwner()
    {
        var cpu = CreateJsrCpu();

        for (var i = 0; i < 6; i++)
            cpu.Tick();

        Assert.Equal(1, cpu.DebugCycle);
        Assert.True(cpu.CanStealCurrentCycle);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001.
    /// Use case: Hosted VICE exposes the first <c>JSR()</c> return-address
    /// push at the third post-reset checkpoint for the reset ROM
    /// <c>JSR $FCE2</c> sequence.
    /// Acceptance: The managed CPU decrements the stack pointer on that same
    /// checkpoint, then performs the low-byte push on the next checkpoint and
    /// reaches the target on the final high-byte operand load.
    /// </summary>
    [Fact]
    public void Jsr_ReturnAddressPushTimingMatchesHostedVice()
    {
        var cpu = CreateJsrCpu();
        cpu.S = 0xF9;

        for (var i = 0; i < 4; i++)
            cpu.Tick();

        Assert.Equal((byte)0xF8, cpu.S);
        Assert.Equal(3, cpu.DebugCycle);

        cpu.Tick();

        Assert.Equal((byte)0xF7, cpu.S);
        Assert.Equal(2, cpu.DebugCycle);

        cpu.Tick();

        Assert.Equal((byte)0xF7, cpu.S);
        Assert.Equal(1, cpu.DebugCycle);

        cpu.Tick();

        Assert.Equal((ushort)0x9000, cpu.PC);
        Assert.Equal(0, cpu.DebugCycle);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001.
    /// Use case: Wolf64 sample 2018254. VICE <c>6510dtvcore.c JSR()</c> after
    ///   the 2-byte FETCH is <c>STACK_PEEK()</c> (check_ba dummy stack read,
    ///   no SP change) then <c>CLK_INC</c>, then <c>INC_PC(2)</c> and PUSH.
    ///   A bad-line stall at DebugCycle 4 is that peek. First unstall CLK must
    ///   still export opcode PC and pre-push S, not the overlapped PUSH.
    /// Acceptance: After OnStolenCycle at overlapped JSR DebugCycle 4, Tick
    ///   keeps PC at the opcode, S unchanged, and DebugCycle at 4.
    /// </summary>
    [Fact]
    public void StolenOverlappedJsr_StackPeekCycle_KeepsOpcodePcAndStack()
    {
        var cpu = CreateJsrCpu();
        cpu.S = 0xF9;
        AdvanceToOpcode(cpu, 0x20);
        for (var i = 0; i < 8 && !(cpu.DebugOpcode == 0x20 && cpu.DebugCycle == 4); i++)
            cpu.Tick();

        Assert.Equal((byte)0x20, cpu.DebugOpcode);
        Assert.Equal(4, cpu.DebugCycle);
        Assert.True(
            cpu.CanForceStealCurrentCycle,
            "STACK_PEEK is check_ba; mandatory BA lag must steal this CLK");
        var sBefore = cpu.S;
        var pcBefore = cpu.PC;

        for (var steal = 0; steal < 15; steal++)
            cpu.OnStolenCycle();
        cpu.Tick();

        Assert.Equal((byte)0x20, cpu.DebugOpcode);
        Assert.Equal(4, cpu.DebugCycle);
        Assert.Equal(sBefore, cpu.S);
        Assert.Equal(pcBefore, cpu.PC);
        Assert.Equal((ushort)0x8000, cpu.PC);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001.
    /// Use case: VICE's x64sc <c>RTS()</c> performs its stack operations across
    /// consecutive <c>CLK_INC()</c> boundaries.
    /// Acceptance: At the point the managed CPU exposes <c>DebugCycle == 2</c>,
    /// native VICE has completed the low-byte pull and advanced the stack
    /// pointer once.
    /// </summary>
    [Fact]
    public void Rts_DebugCycleTwo_HasCompletedLowBytePull()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x8000] = 0x60; // RTS
        memory[0x01F8] = 0x34;
        memory[0x01F9] = 0x12;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));

        var cpu = new Mos6502(bus);
        cpu.Reset();
        cpu.S = 0xF7;

        for (var i = 0; i < 5; i++)
            cpu.Tick();

        Assert.Equal((byte)0xF8, cpu.S);
        Assert.Equal(2, cpu.DebugCycle);

        cpu.Tick();

        Assert.Equal((byte)0xF9, cpu.S);
        Assert.Equal(1, cpu.DebugCycle);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001.
    /// Use case: VICE PLA and PLP execute an unconditional stack peek before
    ///   their pull when the x64sc/xvic hosts define <c>SKIP_CYCLE</c> as zero.
    /// Acceptance: DebugCycle 1 keeps S unchanged for both opcodes; DebugCycle 0
    ///   performs exactly one pull.
    /// </summary>
    [Theory]
    [InlineData(0x68)]
    [InlineData(0x28)]
    public void StackPullOpcode_DebugCycleOne_StillPeeksStack(byte opcode)
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x8000] = opcode;
        memory[0x01F8] = 0x35;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        cpu.S = 0xF7;
        AdvanceToOpcode(cpu, opcode);
        var sAtFetch = cpu.S;

        while (cpu.DebugOpcode == opcode && cpu.DebugCycle != 1)
            cpu.Tick();

        Assert.Equal(opcode, cpu.DebugOpcode);
        Assert.Equal(1, cpu.DebugCycle);
        Assert.Equal(sAtFetch, cpu.S);

        cpu.Tick();

        Assert.Equal(opcode, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((byte)(sAtFetch + 1), cpu.S);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001.
    /// Use case: VICE RTI executes STACK_PEEK before pulling P, PCL, and PCH.
    /// Acceptance: DebugCycle 3 leaves S unchanged; the status pull occurs at
    ///   DebugCycle 2.
    /// </summary>
    [Fact]
    public void Rti_DebugCycleThree_StillPeeksStack()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x8000] = 0x40;
        memory[0x01F8] = 0x25;
        memory[0x01F9] = 0x34;
        memory[0x01FA] = 0x12;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        cpu.S = 0xF7;
        AdvanceToOpcode(cpu, 0x40);
        var sAtFetch = cpu.S;

        while (cpu.DebugOpcode == 0x40 && cpu.DebugCycle != 3)
            cpu.Tick();

        Assert.Equal((byte)0x40, cpu.DebugOpcode);
        Assert.Equal(3, cpu.DebugCycle);
        Assert.Equal(sAtFetch, cpu.S);

        cpu.Tick();

        Assert.Equal((byte)0x40, cpu.DebugOpcode);
        Assert.Equal(2, cpu.DebugCycle);
        Assert.Equal((byte)(sAtFetch + 1), cpu.S);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001.
    /// Use case: VICE <c>6510dtvcore.c</c> always executes
    ///   <c>STACK_PEEK(); CLK_INC();</c> before the first RTS pull when
    ///   <c>SKIP_CYCLE</c> is zero, including after an implied flag opcode.
    /// Acceptance: SEC / STA zp / BCS taken / CLC / RTS keeps S unchanged
    ///   through RTS DebugCycle 3; the first pull is visible at DebugCycle 2.
    /// </summary>
    [Fact]
    public void RtsAfterFusedClc_Cycle3_StillPeeksStack()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x01F8] = 0x34;
        memory[0x01F9] = 0x12;
        memory[0x8000] = 0x38; // SEC
        memory[0x8001] = 0x85; // STA $02
        memory[0x8002] = 0x02;
        memory[0x8003] = 0xB0; // BCS $8005
        memory[0x8004] = 0x00;
        memory[0x8005] = 0x48; // PHA (last CLK next-PC)
        memory[0x8006] = 0x18; // CLC (fused after PHA)
        memory[0x8007] = 0x60; // RTS

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        cpu.S = 0xF7;
        AdvanceToOpcode(cpu, 0x60);
        var sAtFetch = cpu.S;
        while (cpu.DebugOpcode == 0x60 && cpu.DebugCycle != 3)
            cpu.Tick();

        Assert.Equal((byte)0x60, cpu.DebugOpcode);
        Assert.Equal(3, cpu.DebugCycle);
        Assert.Equal(sAtFetch, cpu.S);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 sample 2421915. After taken BCS, INY extra CLK is
    ///   VICE FETCH check_ba. Native froze (nY=$01 nPC=$EACC) on a badline
    ///   while managed CompleteDeferred wrote Y (mY=$02 mPC=$EACD) because
    ///   CanStealCurrentCycle was false during pending deferred implied.
    ///   After-branch INY without BA still commits on the complete tick
    ///   (2061489 nY=$01). Delaying every implied complete regressed 2003248.
    /// Acceptance: LDY #$01 / LDA #$01 / BNE taken / INY, while the
    ///   after-branch defer is pending CanStealCurrentCycle is true; a stolen
    ///   Step leaves Y=$01 and opcode $C8.
    /// </summary>
    [Fact]
    public void InyAfterTakenBne_DeferredCompleteTick_IsStealable()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x8000] = 0xA0; // LDY #$01
        memory[0x8001] = 0x01;
        memory[0x8002] = 0xA9; // LDA #$01 (Z clear)
        memory[0x8003] = 0x01;
        memory[0x8004] = 0xD0; // BNE $8006 (taken, short lag)
        memory[0x8005] = 0x00;
        memory[0x8006] = 0xC8; // INY
        memory[0x8007] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        var stealer = new ClockBaStealer();
        var clock = new SystemClock();
        clock.Register(cpu);
        clock.Register(stealer);
        cpu.Reset();
        for (var i = 0; i < 64 && cpu.DebugOpcode != 0xC8; i++)
            clock.Step();
        Assert.Equal((byte)0xC8, cpu.DebugOpcode);
        while (cpu.DebugOpcode == 0xC8 && !cpu.DebugPendingDeferredImplied)
            clock.Step();

        Assert.True(cpu.DebugPendingDeferredImplied);
        Assert.True(cpu.CanStealCurrentCycle);
        stealer.IsCpuCycleStolen = true;
        clock.Step();

        Assert.Equal((byte)0xC8, cpu.DebugOpcode);
        Assert.Equal((byte)0x01, cpu.Y);
        Assert.True(cpu.DebugPendingDeferredImplied);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 sample 2423374. After AND # whose last CLK already
    ///   exported next-PC, VICE BIT GET_ABS+INC_PC is visible on the last
    ///   CLK (nPC=$EAF5 nP=$27). Managed soft-deferred opcode PC and pre-op
    ///   P (mPC=$EAF2 mP=$25). After LDA# trail=1 BIT already fuses; AND#
    ///   is the same GET_IMM then BIT shape.
    /// Acceptance: LDA #$4C / SEC / STA zp / BCS taken / AND #$00 /
    ///   BIT $9000 of $00, last CLK of BIT exports opcode+3 and Z set.
    /// </summary>
    [Fact]
    public void BitAbsAfterTakenBcsAndImm_LastClk_ExportsNextPcAndZ()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x9000] = 0x00;
        memory[0x8000] = 0xA9; // LDA #$4C
        memory[0x8001] = 0x4C;
        memory[0x8002] = 0x38; // SEC
        memory[0x8003] = 0x85; // STA $02
        memory[0x8004] = 0x02;
        memory[0x8005] = 0xB0; // BCS $8007
        memory[0x8006] = 0x00;
        memory[0x8007] = 0x29; // AND #$00
        memory[0x8008] = 0x00;
        memory[0x8009] = 0x2C; // BIT $9000
        memory[0x800A] = 0x00;
        memory[0x800B] = 0x90;
        memory[0x800C] = 0x30; // BMI $800E (not taken, N clear)
        memory[0x800D] = 0x00;
        memory[0x800E] = 0x70; // BVS $8010 (not taken, V clear)
        memory[0x800F] = 0x00;
        memory[0x8010] = 0xC9; // CMP #$00
        memory[0x8011] = 0x00;
        memory[0x8012] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        AdvanceToOpcode(cpu, 0x2C);
        while (cpu.DebugOpcode == 0x2C && cpu.DebugCycle != 0)
            cpu.Tick();

        Assert.Equal((byte)0x2C, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((ushort)0x800C, cpu.PC);
        Assert.Equal(0x02, cpu.P & 0x02);

        AdvanceToOpcode(cpu, 0x30);
        while (cpu.DebugOpcode == 0x30 && cpu.DebugCycle != 0)
            cpu.Tick();

        Assert.Equal((byte)0x30, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((ushort)0x800E, cpu.PC);

        AdvanceToOpcode(cpu, 0xC9);
        while (cpu.DebugOpcode == 0xC9 && cpu.DebugCycle != 0)
            cpu.Tick();

        Assert.Equal((byte)0xC9, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((ushort)0x8012, cpu.PC);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 sample 4131976. BIT zp after ASL zp whose last CLK
    ///   already exported next-PC. VICE BIT(GET_ZERO)+INC_PC has no extra
    ///   CLK (nPC=$E646). Managed soft-deferred opcode PC (mPC=$E644).
    /// Acceptance: LDA #$0C / STA $10 / ASL $10 / BIT $10: last CLK of BIT
    ///   exports opcode+2.
    /// </summary>
    [Fact]
    public void BitZpAfterAslZp_LastClk_ExportsFallthroughPc()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x8000] = 0xA9; // LDA #$0C
        memory[0x8001] = 0x0C;
        memory[0x8002] = 0x85; // STA $10
        memory[0x8003] = 0x10;
        memory[0x8004] = 0x06; // ASL $10
        memory[0x8005] = 0x10;
        memory[0x8006] = 0x24; // BIT $10
        memory[0x8007] = 0x10;
        memory[0x8008] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        AdvanceToOpcode(cpu, 0x24);
        while (cpu.DebugOpcode == 0x24 && cpu.DebugCycle != 0)
            cpu.Tick();

        Assert.Equal((byte)0x24, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((ushort)0x8008, cpu.PC);
    }

    private static Mos6502 CreateJsrCpu()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x8000] = 0x20; // JSR $9000
        memory[0x8001] = 0x00;
        memory[0x8002] = 0x90;
        memory[0x9000] = 0xEA; // NOP

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));

        var cpu = new Mos6502(bus);
        cpu.Reset();
        return cpu;
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001.
    /// Use case: Unstalled KERNAL INY/BNE (and DEY/BPL) taken branch: VICE
    /// BRANCH dummy CLK after INC_PC(2) exports fall-through PC.
    /// Acceptance: After DEY then taken BPL, the final host cycle of BPL
    /// exposes fall-through $8005, not the opcode $8003.
    /// </summary>
    [Fact]
    public void UnstalledTakenBplAfterDey_FinalCycle_ExportsFallThroughPc()
    {
        var cpu = CreateDeyBplCpu();
        AdvanceToOpcode(cpu, 0x10);

        while (cpu.DebugOpcode == 0x10 && cpu.DebugCycle != 0)
            cpu.Tick();

        Assert.Equal((byte)0x10, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((ushort)0x8005, cpu.PC);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001.
    /// Use case: VICE check_ba/steal_cycles run before FETCH CLK_INC, so a
    /// BA stall mid-BPL must not skip the opcode-PC FETCH clock.
    /// Acceptance: After OnStolenCycle at BPL DebugCycle 1, the next Tick
    /// still exports opcode PC $8003.
    /// </summary>
    [Fact]
    public void StolenTakenBplAfterDey_NextTick_ExportsOpcodePc()
    {
        var cpu = CreateDeyBplCpu();
        AdvanceToOpcode(cpu, 0x10);
        while (cpu.DebugOpcode == 0x10 && cpu.DebugCycle != 1)
            cpu.Tick();

        Assert.Equal(1, cpu.DebugCycle);
        cpu.OnStolenCycle();
        cpu.Tick();

        Assert.Equal((byte)0x10, cpu.DebugOpcode);
        Assert.Equal((ushort)0x8003, cpu.PC);
        Assert.True(cpu.CanStealCurrentCycle);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001.
    /// Use case: Unstalled INY/BNE (KERNAL RAMTAS $FD5D) must keep the
    /// existing fall-through export; do not hold opcode PC after implied ops.
    /// Acceptance: Taken BNE after INY ends on fall-through $8005.
    /// </summary>
    [Fact]
    public void UnstalledTakenBneAfterIny_FinalCycle_ExportsFallThroughPc()
    {
        var cpu = CreateInyBneCpu();
        AdvanceToOpcode(cpu, 0xD0);

        while (cpu.DebugOpcode == 0xD0 && cpu.DebugCycle != 0)
            cpu.Tick();

        Assert.Equal((byte)0xD0, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((ushort)0x8005, cpu.PC);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001.
    /// Use case: KERNAL <c>LDA $D012</c> after BA steal. Wolf64 sample 2044662:
    ///   native still exports pre-load A on the LDA abs data-read CLK (nA=$06
    ///   mA=$07) while opcode PC holds. Loaded A becomes visible on the last CLK.
    /// Acceptance: After taken BNE then LDA abs, OnStolenCycle at DebugCycle 2
    ///   keeps opcode PC and pre-load A; the data-read CLK still has pre-load A;
    ///   the last CLK has loaded A and opcode PC.
    /// </summary>
    [Fact]
    public void StolenLdaAbsAfterBne_DataReadCycle_KeepsPreloadA()
    {
        var cpu = CreateBneLdaAbsCpu(preloadA: 0x43, loaded: 0x44);
        AdvanceToOpcode(cpu, 0xAD);
        Assert.Equal((byte)0x43, cpu.A);

        while (cpu.DebugOpcode == 0xAD && cpu.DebugCycle > 2)
            cpu.Tick();

        Assert.Equal(2, cpu.DebugCycle);
        cpu.OnStolenCycle();
        cpu.Tick();

        Assert.Equal((byte)0xAD, cpu.DebugOpcode);
        Assert.Equal(2, cpu.DebugCycle);
        Assert.Equal((byte)0x43, cpu.A);
        Assert.Equal((ushort)0x8004, cpu.PC);

        cpu.Tick();
        Assert.Equal(1, cpu.DebugCycle);
        Assert.Equal((byte)0x43, cpu.A);
        Assert.Equal((ushort)0x8004, cpu.PC);

        cpu.Tick();
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((byte)0xAD, cpu.DebugOpcode);
        Assert.Equal((byte)0x44, cpu.A);
        Assert.Equal((ushort)0x8004, cpu.PC);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001.
    /// Use case: Wolf64 sample 2126188. KERNAL SBC then taken BCC then LDA abs
    ///   at $F6BC. VICE FETCH_OPCODE for $AD is 3 CLK (fetch_tab=1) then
    ///   GET_ABS LOAD+CLK_INC. The DebugCycle 1 sample is still FETCH of p2,
    ///   so A is the pre-load $B0 (nA=$B0 mA=$FF). GET_ABS commits A on the
    ///   last CLK.
    /// Acceptance: After LDA #$00 / SBC #$4F / BCC taken / LDA abs $FF,
    ///   DebugCycle 1 still has A=$B0. Last CLK has A=$FF and opcode PC.
    /// </summary>
    [Fact]
    public void TakenBccThenLdaAbs_DataReadCycle_KeepsPreloadA()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        // Reset C=0. LDA #$00; SBC #$4F => A=$B0, C=0 (borrow). BCC taken.
        // Fall-through $8006, target $800C (offset +6, same page as Wolf64).
        memory[0x8000] = 0xA9;
        memory[0x8001] = 0x00;
        memory[0x8002] = 0xE9;
        memory[0x8003] = 0x4F;
        memory[0x8004] = 0x90;
        memory[0x8005] = 0x06;
        memory[0x800C] = 0xAD;
        memory[0x800D] = 0x00;
        memory[0x800E] = 0x90;
        memory[0x800F] = 0xEA;
        memory[0x9000] = 0xFF;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        AdvanceToOpcode(cpu, 0xAD);
        Assert.Equal((byte)0xB0, cpu.A);

        while (cpu.DebugOpcode == 0xAD && cpu.DebugCycle != 1)
            cpu.Tick();

        Assert.Equal((byte)0xAD, cpu.DebugOpcode);
        Assert.Equal(1, cpu.DebugCycle);
        Assert.True(
            cpu.A == 0xB0,
            $"Wolf64 2126188 GET_ABS data-read must keep preload A=$B0; A=${cpu.A:X2} PC=${cpu.PC:X4} "
            + $"shortLag={cpu.DebugAfterShortTakenBranchLag} full={cpu.DebugAfterFullLengthTakenBranch} "
            + $"skipHold={cpu.DebugSkipAbsLoadLastClkHold} loadEarly={cpu.DebugLoadAEarlyAfterStagedBranch} "
            + $"tgtPend={cpu.DebugBranchTargetFetchPending} trail={cpu.DebugPriorTrailingAtNextPc} "
            + $"nonOvlF={cpu.DebugNonOverlappedFetchPhase} supp={cpu.DebugSuppressBootstrapBoundary}");
        Assert.Equal((ushort)0x800C, cpu.PC);

        cpu.Tick();
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((byte)0xFF, cpu.A);
        Assert.Equal((ushort)0x800C, cpu.PC);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001.
    /// Use case: Wolf64 sample 2060353. Not-taken BNE falls through to LDA abs.
    ///   VICE GET_ABS writes A then CLK_INC, so the data-read CLK already has
    ///   the loaded value. Treating "previous opcode is a branch" as taken-branch
    ///   lag left A at the pre-load value (nA=$71 mA=$00).
    /// Acceptance: After LDA #$00 / not-taken BNE / LDA abs, DebugCycle 1 of
    ///   LDA abs has A equal to the absolute operand.
    /// </summary>
    [Fact]
    public void NotTakenBneThenLdaAbs_DataReadCycle_HasLoadedA()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x8000] = 0xA9; // LDA #$00 (Z set)
        memory[0x8001] = 0x00;
        memory[0x8002] = 0xD0; // BNE not taken
        memory[0x8003] = 0x00;
        memory[0x8004] = 0xAD; // LDA $9000
        memory[0x8005] = 0x00;
        memory[0x8006] = 0x90;
        memory[0x9000] = 0x71;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        AdvanceToOpcode(cpu, 0xAD);
        while (cpu.DebugOpcode == 0xAD && cpu.DebugCycle != 1)
            cpu.Tick();

        Assert.Equal((byte)0xAD, cpu.DebugOpcode);
        Assert.Equal(1, cpu.DebugCycle);
        Assert.Equal((byte)0x71, cpu.A);
        Assert.Equal((ushort)0x8004, cpu.PC);

        cpu.Tick();
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((byte)0x71, cpu.A);
        Assert.Equal((ushort)0x8007, cpu.PC);

    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001, TEST: TEST-X64SC-LOCKSTEP-001.
    /// A JSR target begins with a distinct FETCH_OPCODE phase. For STA absolute,
    /// DebugCycle 2 is the third fetch checkpoint in this staged path; ST's
    /// INC_PC and STORE clock follow on the next host tick.
    /// </summary>
    [Fact]
    public void StaAbsAtJsrTarget_ThirdFetchHoldsOpcodePcBeforeStore()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x8000] = 0x20; // JSR $8100
        memory[0x8001] = 0x00;
        memory[0x8002] = 0x81;
        memory[0x8003] = 0x20; // JSR $9000 after RTS
        memory[0x8004] = 0x00;
        memory[0x8005] = 0x90;
        memory[0x8006] = 0xEA;
        memory[0x8100] = 0x60; // RTS
        memory[0x9000] = 0x8D; // STA $0400
        memory[0x9001] = 0x00;
        memory[0x9002] = 0x04;
        memory[0x9003] = 0x60;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        cpu.A = 0x2A;

        AdvanceToOpcode(cpu, 0x8D);

        Assert.Equal((byte)0x8D, cpu.DebugOpcode);
        Assert.Equal(4, cpu.DebugCycle);
        Assert.Equal((ushort)0x9000, cpu.PC);
        Assert.Equal((byte)0x00, memory[0x0400]);

        cpu.Tick();
        cpu.Tick();

        Assert.Equal(2, cpu.DebugCycle);
        Assert.Equal((ushort)0x9000, cpu.PC);
        Assert.Equal((byte)0x00, memory[0x0400]);

        cpu.Tick();

        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((ushort)0x9003, cpu.PC);
        Assert.Equal((byte)0x2A, memory[0x0400]);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001.
    /// Use case: Wolf64 sample 2060359. VICE <c>ST()</c> does INC_PC then
    ///   SET_ABS STORE+CLK_INC, so the STA abs write CLK already exports
    ///   opcode+3. After not-taken BNE / LDA abs / AND #, managed held the
    ///   opcode PC (nPC=$FF6B mPC=$FF68).
    /// Acceptance: STA abs DebugCycle 1 after that sequence has PC at opcode+3.
    /// </summary>
    [Fact]
    public void NotTakenBneLdaAndStaAbs_WriteCycle_ExportsNextPc()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x8000] = 0xA9; // LDA #$01 (taken BNE, sticky non-overlapped)
        memory[0x8001] = 0x01;
        memory[0x8002] = 0xD0; // BNE $8004
        memory[0x8003] = 0x00;
        memory[0x8004] = 0xAD; // LDA $9000
        memory[0x8005] = 0x00;
        memory[0x8006] = 0x90;
        memory[0x8007] = 0x29; // AND #$01
        memory[0x8008] = 0x01;
        memory[0x8009] = 0x8D; // STA $9100
        memory[0x800A] = 0x00;
        memory[0x800B] = 0x91;
        memory[0x9000] = 0x71;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        AdvanceToOpcode(cpu, 0x8D);
        while (cpu.DebugOpcode == 0x8D && cpu.DebugCycle != 1)
            cpu.Tick();

        Assert.Equal((byte)0x8D, cpu.DebugOpcode);
        Assert.Equal(1, cpu.DebugCycle);
        Assert.Equal((ushort)0x800C, cpu.PC);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001.
    /// Use case: Wolf64 sample 2060363. VICE JMP() is JUMP with no extra CLK
    ///   after the 3-byte FETCH, so the last JMP CLK exports the target.
    ///   After STA abs in a non-overlapped region, soft-defer kept opcode PC
    ///   (nPC=$FDDD mPC=$FF6B).
    /// Acceptance: JMP abs last CLK after that STA sequence is the target.
    /// </summary>
    [Fact]
    public void StaAbsThenJmpAbs_LastClk_ExportsTarget()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x8000] = 0xA9;
        memory[0x8001] = 0x01;
        memory[0x8002] = 0xD0;
        memory[0x8003] = 0x00;
        memory[0x8004] = 0xAD;
        memory[0x8005] = 0x00;
        memory[0x8006] = 0x90;
        memory[0x8007] = 0x29;
        memory[0x8008] = 0x01;
        memory[0x8009] = 0x8D;
        memory[0x800A] = 0x00;
        memory[0x800B] = 0x91;
        memory[0x800C] = 0x4C; // JMP $9001
        memory[0x800D] = 0x01;
        memory[0x800E] = 0x90;
        memory[0x9000] = 0x71;
        memory[0x9001] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        AdvanceToOpcode(cpu, 0x4C);
        while (cpu.DebugOpcode == 0x4C && cpu.DebugCycle != 0)
            cpu.Tick();

        Assert.Equal((byte)0x4C, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((ushort)0x9001, cpu.PC);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001.
    /// Use case: Wolf64 sample 2060383. VICE ST INC_PC then SET_ABS after JMP,
    ///   so STA abs write CLK exports opcode+3 (nPC=$FDF6 mPC=$FDF3).
    /// Acceptance: STA abs DebugCycle 1 after JMP abs is opcode+3.
    /// </summary>
    [Fact]
    public void JmpAbsThenStaAbs_WriteCycle_ExportsNextPc()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x8000] = 0x4C; // JMP $8003
        memory[0x8001] = 0x03;
        memory[0x8002] = 0x80;
        memory[0x8003] = 0xA9;
        memory[0x8004] = 0x40;
        memory[0x8005] = 0x8D; // STA $9100
        memory[0x8006] = 0x00;
        memory[0x8007] = 0x91;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        AdvanceToOpcode(cpu, 0x8D);
        while (cpu.DebugOpcode == 0x8D && cpu.DebugCycle != 1)
            cpu.Tick();

        Assert.Equal((byte)0x8D, cpu.DebugOpcode);
        Assert.Equal(1, cpu.DebugCycle);
        Assert.Equal((ushort)0x8008, cpu.PC);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001.
    /// Use case: Wolf64 sample 2060513. VICE CP(GET_ABS) does LOAD+CLK_INC
    ///   then flags+INC_PC with no extra CLK, so the last CMP abs CLK still
    ///   has opcode PC and pre-op P (nPC=$F6BF nP=$A4 mPC=$F6C2 mP=$27).
    /// Acceptance: After taken BNE, LDA abs of $FF, CMP abs of $FF, last CLK
    ///   of CMP keeps the opcode PC and N from A=$FF (Z not yet set).
    /// </summary>
    [Fact]
    public void CmpAbsAfterLdaAbs_LastClk_KeepsOpcodePcAndPreOpP()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x8000] = 0xA9;
        memory[0x8001] = 0x01;
        memory[0x8002] = 0xD0;
        memory[0x8003] = 0x00;
        memory[0x8004] = 0xAD;
        memory[0x8005] = 0x00;
        memory[0x8006] = 0x90;
        memory[0x8007] = 0xCD;
        memory[0x8008] = 0x00;
        memory[0x8009] = 0x90;
        memory[0x9000] = 0xFF;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        AdvanceToOpcode(cpu, 0xCD);
        while (cpu.DebugOpcode == 0xCD && cpu.DebugCycle != 0)
            cpu.Tick();

        Assert.Equal((byte)0xCD, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((ushort)0x8007, cpu.PC);
        Assert.Equal(0x80, cpu.P & 0x80);
        Assert.Equal(0x00, cpu.P & 0x02);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 sample 2407022. VICE CP(GET_ABS) LOAD+CLK_INC then
    ///   flags+INC_PC with no extra CLK, so the last CPX abs CLK still has
    ///   opcode PC and pre-op P (nPC=$EB37 nP=$26). Managed fused next-PC
    ///   and N (mPC=$EB3A mP=$A4). Previous LDX zp last CLK held opcode
    ///   (trail 0), same shape as CMP abs 2060513. Distinct from CMP abs
    ///   trail&gt;=1 fuse (2142604).
    /// Acceptance: After taken BNE, LDX zp of $00, CPX abs of $80, last CLK
    ///   of CPX keeps the opcode PC and Z from X=$00 (N not yet set).
    /// </summary>
    [Fact]
    public void CpxAbsAfterLdxZp_LastClk_KeepsOpcodePcAndPreOpP()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x0010] = 0x00;
        memory[0x9000] = 0x80;
        memory[0x8000] = 0xA9; // LDA #$01 (Z clear)
        memory[0x8001] = 0x01;
        memory[0x8002] = 0xD0; // BNE $8004 (taken)
        memory[0x8003] = 0x00;
        memory[0x8004] = 0xA6; // LDX $10
        memory[0x8005] = 0x10;
        memory[0x8006] = 0xEC; // CPX $9000
        memory[0x8007] = 0x00;
        memory[0x8008] = 0x90;
        memory[0x8009] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        AdvanceToOpcode(cpu, 0xEC);
        while (cpu.DebugOpcode == 0xEC && cpu.DebugCycle != 0)
            cpu.Tick();

        Assert.Equal((byte)0xEC, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((ushort)0x8006, cpu.PC);
        Assert.Equal(0x00, cpu.P & 0x80);
        Assert.Equal(0x02, cpu.P & 0x02);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001.
    /// Use case: Wolf64 sample 2142604. After taken BCC / LDA abs whose last
    ///   CLK already exported next-PC, VICE CP(GET_ABS) LOAD+CLK_INC then
    ///   flags+INC_PC with no extra CLK. Native last CMP CLK is opcode+3
    ///   with Z+C (nPC=$F6C2 nP=$27). Managed soft-deferred opcode PC and
    ///   pre-op P (mPC=$F6BF mP=$A4). Distinct from 2060513, where LDA last
    ///   CLK still held opcode so CMP FETCH trail was 0.
    /// Acceptance: LDA #$B0 / STA zp / BCC taken / LDA abs $FF / CMP abs $FF,
    ///   last CLK of CMP exports opcode+3, Z set, C set, N clear.
    /// </summary>
    [Fact]
    public void CmpAbsAfterTakenBccLdaAbs_LastClk_ExportsNextPcAndFlags()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x8000] = 0xA9; // LDA #$B0
        memory[0x8001] = 0xB0;
        memory[0x8002] = 0x85; // STA $02 (trail=2, full-length BCC)
        memory[0x8003] = 0x02;
        memory[0x8004] = 0x90; // BCC $8006 (taken, C=0)
        memory[0x8005] = 0x00;
        memory[0x8006] = 0xAD; // LDA $9000
        memory[0x8007] = 0x00;
        memory[0x8008] = 0x90;
        memory[0x8009] = 0xCD; // CMP $9000
        memory[0x800A] = 0x00;
        memory[0x800B] = 0x90;
        memory[0x800C] = 0xEA;
        memory[0x9000] = 0xFF;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        AdvanceToOpcode(cpu, 0xCD);
        while (cpu.DebugOpcode == 0xCD && cpu.DebugCycle != 0)
            cpu.Tick();

        Assert.Equal((byte)0xCD, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((ushort)0x800C, cpu.PC);
        Assert.Equal(0x00, cpu.P & 0x80);
        Assert.Equal(0x02, cpu.P & 0x02);
        Assert.Equal(0x01, cpu.P & 0x01);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 sample 2405677. After taken BCS / INY whose last CLK
    ///   already exported next-PC, VICE CP(GET_IMM)+INC_PC has no extra CLK.
    ///   Native CPY # last CLK is opcode+2 with N set (nPC=$EACF nP=$A4,
    ///   Y=$09 vs #$0A). Managed soft-deferred opcode PC and pre-op P
    ///   (mPC=$EACD mP=$25). Distinct from 2060513 (trail 0 hold).
    /// Acceptance: LDY #$08 / SEC / STA zp / BCS taken / INY / CPY #$0A,
    ///   last CLK of CPY exports opcode+2, N set, Z clear, C clear.
    /// </summary>
    [Fact]
    public void CpyImmAfterTakenBcsIny_LastClk_ExportsNextPcAndFlags()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x8000] = 0xA0; // LDY #$08
        memory[0x8001] = 0x08;
        memory[0x8002] = 0x38; // SEC
        memory[0x8003] = 0x85; // STA $02 (full-length BCS)
        memory[0x8004] = 0x02;
        memory[0x8005] = 0xB0; // BCS $8007 (taken)
        memory[0x8006] = 0x00;
        memory[0x8007] = 0xC8; // INY
        memory[0x8008] = 0xC0; // CPY #$0A (C clear)
        memory[0x8009] = 0x0A;
        memory[0x800A] = 0xB0; // BCS $800C (not taken)
        memory[0x800B] = 0x00;
        memory[0x800C] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        AdvanceToOpcode(cpu, 0xC0);
        while (cpu.DebugOpcode == 0xC0 && cpu.DebugCycle != 0)
            cpu.Tick();

        Assert.Equal((byte)0xC0, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((byte)0x09, cpu.Y);
        Assert.Equal((ushort)0x800A, cpu.PC);
        Assert.Equal(0x80, cpu.P & 0x80);
        Assert.Equal(0x00, cpu.P & 0x02);
        Assert.Equal(0x00, cpu.P & 0x01);

        AdvanceToOpcode(cpu, 0xB0);
        while (cpu.DebugOpcode == 0xB0 && cpu.DebugCycle != 0)
            cpu.Tick();

        Assert.Equal((byte)0xB0, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((ushort)0x800C, cpu.PC);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 sample 2406986. After fused CPY zp whose last CLK
    ///   already exported next-PC (2406984 nPC=$EAE7), VICE not-taken BEQ
    ///   INC_PC is visible on the last CLK (nPC=$EAE9). Managed held opcode
    ///   PC (mPC=$EAE7). skipHoldAfterFusedLoad covered CPY# and CMP abs;
    ///   CPY zp (0xC4) was missing. Do not restore sticky fuseImplied
    ///   through CPY (2093382).
    /// Acceptance: LDA #$4C / LDY #$2A / SEC / STA zp / BCS taken / PHA /
    ///   TAX / CPY $10 of $80 / BEQ not-taken, last CLK of BEQ exports
    ///   opcode+2.
    /// </summary>
    [Fact]
    public void BeqNotTakenAfterFusedCpyZp_LastClk_ExportsFallthrough()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x0010] = 0x80;
        memory[0x8000] = 0xA9; // LDA #$4C
        memory[0x8001] = 0x4C;
        memory[0x8002] = 0xA0; // LDY #$2A
        memory[0x8003] = 0x2A;
        memory[0x8004] = 0x38; // SEC
        memory[0x8005] = 0x85; // STA $02
        memory[0x8006] = 0x02;
        memory[0x8007] = 0xB0; // BCS $8009
        memory[0x8008] = 0x00;
        memory[0x8009] = 0x48; // PHA
        memory[0x800A] = 0xAA; // TAX
        memory[0x800B] = 0xC4; // CPY $10
        memory[0x800C] = 0x10;
        memory[0x800D] = 0xF0; // BEQ $800F (not taken)
        memory[0x800E] = 0x00;
        memory[0x800F] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        AdvanceToOpcode(cpu, 0xC4);
        while (cpu.DebugOpcode == 0xC4)
            cpu.Tick();
        Assert.Equal((byte)0xF0, cpu.DebugOpcode);
        while (cpu.DebugOpcode == 0xF0 && cpu.DebugCycle != 0)
            cpu.Tick();

        Assert.Equal((byte)0xF0, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((ushort)0x800F, cpu.PC);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 sample 2406995. Taken same-page BNE after STY abs
    ///   whose STORE CLK already exported next-PC. VICE BRANCH dummy
    ///   CLK_INC still has fall-through; JUMP has no CLK (nPC=$EAF0).
    ///   Managed staged-fallthrough JUMP on last CLK (mPC=$EB26).
    /// Acceptance: SEC / STA zp / BCS taken / PHA / LDY #$10 / STY abs /
    ///   BNE taken same-page, last CLK of BNE keeps fall-through PC.
    /// </summary>
    [Fact]
    public void TakenBneAfterFusedStyAbs_LastClk_KeepsFallthrough()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x8000] = 0x38; // SEC
        memory[0x8001] = 0x85; // STA $02
        memory[0x8002] = 0x02;
        memory[0x8003] = 0xB0; // BCS $8005
        memory[0x8004] = 0x00;
        memory[0x8005] = 0x48; // PHA
        memory[0x8006] = 0xA0; // LDY #$10 (fused last CLK already next-PC)
        memory[0x8007] = 0x10;
        memory[0x8008] = 0x8C; // STY $0400
        memory[0x8009] = 0x00;
        memory[0x800A] = 0x04;
        memory[0x800B] = 0xD0; // BNE $800F (taken, same page)
        memory[0x800C] = 0x02;
        memory[0x800D] = 0xEA;
        memory[0x800E] = 0xEA;
        memory[0x800F] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        AdvanceToOpcode(cpu, 0xD0);
        while (cpu.DebugOpcode == 0xD0 && cpu.DebugCycle != 0)
            cpu.Tick();

        Assert.Equal((byte)0xD0, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((ushort)0x800D, cpu.PC);

        cpu.Tick();
        Assert.Equal((ushort)0x800F, cpu.PC);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 sample 2406997. LDY zp directly after taken BNE.
    ///   VICE still exports pre-load Y on the data-read CLK (nY=$10).
    ///   Trail&gt;=1 GET_ZERO fuse (2406973 after JMP) must not fire when
    ///   the previous opcode is a branch.
    /// Acceptance: LDY #$10 / SEC / STA zp / BCS taken / BNE taken /
    ///   LDY $20 of $2A: data-read CLK (cycle 1) still has Y=$10.
    /// </summary>
    [Fact]
    public void LdyZpAfterTakenBne_DataRead_KeepsPreloadY()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x0020] = 0x2A;
        memory[0x8000] = 0xA0; // LDY #$10
        memory[0x8001] = 0x10;
        memory[0x8002] = 0x38; // SEC
        memory[0x8003] = 0x85; // STA $02
        memory[0x8004] = 0x02;
        memory[0x8005] = 0xB0; // BCS $8007
        memory[0x8006] = 0x00;
        memory[0x8007] = 0xD0; // BNE $800B (taken, Z clear)
        memory[0x8008] = 0x02;
        memory[0x8009] = 0xEA;
        memory[0x800A] = 0xEA;
        memory[0x800B] = 0xA4; // LDY $20
        memory[0x800C] = 0x20;
        memory[0x800D] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        AdvanceToOpcode(cpu, 0xA4);
        while (cpu.DebugOpcode == 0xA4 && cpu.DebugCycle != 1)
            cpu.Tick();

        Assert.Equal((byte)0xA4, cpu.DebugOpcode);
        Assert.Equal(1, cpu.DebugCycle);
        Assert.Equal((byte)0x10, cpu.Y);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 sample 2405849. After fused ROL A (last CLK already
    ///   next-PC), VICE ST INC_PC then SET_ABS STORE CLK so the write sample
    ///   exports opcode+3 (nPC=$EADA). Managed held opcode PC (mPC=$EAD7
    ///   trail==1 after implied, holdOpcodePc).
    /// Acceptance: SEC / STA zp / BCS taken / ROL A / STA abs, STA cycle 1
    ///   (write) exports opcode+3.
    /// </summary>
    [Fact]
    public void StaAbsAfterTakenBcsRolA_WriteSample_ExportsNextPc()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x8000] = 0xA9; // LDA #$FD
        memory[0x8001] = 0xFD;
        memory[0x8002] = 0x38; // SEC
        memory[0x8003] = 0x85; // STA $02
        memory[0x8004] = 0x02;
        memory[0x8005] = 0xB0; // BCS $8007
        memory[0x8006] = 0x00;
        memory[0x8007] = 0x2A; // ROL A
        memory[0x8008] = 0x8D; // STA $0400
        memory[0x8009] = 0x00;
        memory[0x800A] = 0x04;
        memory[0x800B] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        AdvanceToOpcode(cpu, 0x8D);
        while (cpu.DebugOpcode == 0x8D && cpu.DebugCycle != 1)
            cpu.Tick();

        Assert.Equal((byte)0x8D, cpu.DebugOpcode);
        Assert.Equal(1, cpu.DebugCycle);
        Assert.Equal((ushort)0x800B, cpu.PC);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 sample 2406425. LDA (zp),Y after an insn whose last
    ///   CLK already exported next-PC. VICE GET_IND_Y writes A then CLK_INC,
    ///   so the data-read CLK already has loaded A (nA=$4C). Managed kept
    ///   pre-load A in nonOvl (mA=$1F). Distinct from LDA (zp),Y directly
    ///   after a full-length taken branch, which still pre-loads on data-read.
    /// Acceptance: LDA #$1F / SEC / STA zp / BCS taken / CLC / LDA ($10),Y
    ///   of $4C: data-read CLK (cycle 1) has A=$4C.
    /// </summary>
    [Fact]
    public void LdaIndYAfterTakenBcsClc_DataRead_ShowsLoadedA()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x0010] = 0x00;
        memory[0x0011] = 0x90;
        memory[0x9000] = 0x4C;
        memory[0x8000] = 0xA9; // LDA #$1F (pre-load A)
        memory[0x8001] = 0x1F;
        memory[0x8002] = 0x38; // SEC
        memory[0x8003] = 0x85; // STA $02 (full-length BCS)
        memory[0x8004] = 0x02;
        memory[0x8005] = 0xB0; // BCS $8007
        memory[0x8006] = 0x00;
        memory[0x8007] = 0x18; // CLC (implied; last CLK already next-PC)
        memory[0x8008] = 0xB1; // LDA ($10),Y
        memory[0x8009] = 0x10;
        memory[0x800A] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        AdvanceToOpcode(cpu, 0xB1);
        while (cpu.DebugOpcode == 0xB1 && cpu.DebugCycle != 1)
            cpu.Tick();

        Assert.Equal((byte)0xB1, cpu.DebugOpcode);
        Assert.Equal(1, cpu.DebugCycle);
        Assert.Equal((byte)0x4C, cpu.A);

        while (cpu.DebugOpcode == 0xB1 && cpu.DebugCycle != 0)
            cpu.Tick();

        Assert.Equal((byte)0xB1, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((byte)0x4C, cpu.A);
        Assert.Equal((ushort)0x800A, cpu.PC);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 sample 4131416. LDA (zp),Y after STY zp. VICE
    ///   GET_IND_Y data-read CLK still has pre-load A (nA=$0D). Managed
    ///   committed A because STY last CLK already exported next-PC
    ///   (mA=$20). Store trail is not a GET_IND_Y A-commit.
    /// Acceptance: LDA #$0D / STY $02 / LDA ($10),Y of $20: data-read CLK
    ///   (cycle 1) still has A=$0D.
    /// </summary>
    [Fact]
    public void LdaIndYAfterStyZp_DataRead_StillShowsPreLoadA()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x0010] = 0x00;
        memory[0x0011] = 0x90;
        memory[0x9000] = 0x20;
        memory[0x8000] = 0xA9; // LDA #$0D
        memory[0x8001] = 0x0D;
        memory[0x8002] = 0x84; // STY $02
        memory[0x8003] = 0x02;
        memory[0x8004] = 0xB1; // LDA ($10),Y
        memory[0x8005] = 0x10;
        memory[0x8006] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        AdvanceToOpcode(cpu, 0xB1);
        while (cpu.DebugOpcode == 0xB1 && cpu.DebugCycle != 1)
            cpu.Tick();

        Assert.Equal((byte)0xB1, cpu.DebugOpcode);
        Assert.Equal(1, cpu.DebugCycle);
        Assert.Equal((byte)0x0D, cpu.A);

        while (cpu.DebugOpcode == 0xB1 && cpu.DebugCycle != 0)
            cpu.Tick();

        Assert.Equal((byte)0xB1, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((byte)0x20, cpu.A);
        Assert.Equal((ushort)0x8004, cpu.PC);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 sample 4131432. LDA (zp),Y after taken BNE (full-length).
    ///   VICE GET_IND_Y CLK still has opcode PC; SET_NZ+INC_PC have no extra
    ///   CLK. Managed cycle 0 armed deferred NZ, then the next host tick
    ///   cleared suppress and exported opcode+2 (nPC=$E606 mPC=$E608).
    /// Acceptance: LDA #$20 / BNE taken / LDA ($10),Y of $20: cycle 0 still
    ///   opcode PC. One more Tick stays at the LDA (zp),Y opcode PC (no
    ///   second INC_PC tick).
    /// </summary>
    [Fact]
    public void LdaIndYAfterTakenBne_DeferredNzTick_HoldsOpcodePc()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x0010] = 0xF0;
        memory[0x0011] = 0x90;
        memory[0x9110] = 0x20;
        memory[0x8000] = 0xA9; // LDA #$20 (Z=0)
        memory[0x8001] = 0x20;
        memory[0x8002] = 0xA0; // LDY #$20 (lo $F0 + Y page-crosses)
        memory[0x8003] = 0x20;
        memory[0x8004] = 0x38; // SEC
        memory[0x8005] = 0x85; // STA $02
        memory[0x8006] = 0x02;
        memory[0x8007] = 0xB0; // BCS $8009
        memory[0x8008] = 0x00;
        memory[0x8009] = 0xD0; // BNE $800D
        memory[0x800A] = 0x02;
        memory[0x800B] = 0xEA;
        memory[0x800C] = 0xEA;
        memory[0x800D] = 0xB1; // LDA ($10),Y
        memory[0x800E] = 0x10;
        memory[0x800F] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        AdvanceToOpcode(cpu, 0xB1);
        for (var i = 0; i < 16 && !cpu.DebugPendingDeferredNzUpdate; i++)
            cpu.Tick();

        Assert.True(cpu.DebugPendingDeferredNzUpdate);
        Assert.Equal((byte)0xB1, cpu.DebugOpcode);
        Assert.Equal((byte)0x20, cpu.A);
        Assert.Equal((ushort)0x800D, cpu.PC);

        cpu.Tick();

        Assert.Equal((byte)0xB1, cpu.DebugOpcode);
        Assert.Equal((ushort)0x800D, cpu.PC);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 sample 3029785. LDA (zp),Y after PHA whose last CLK
    ///   already exported next-PC. VICE LD(GET_IND_Y) is LOCAL_SET_NZ then
    ///   INC_PC with no extra CLK, so the last CLK already has Z from the
    ///   loaded A (nA=$01 nP=$24). Managed fused PC to opcode+2 but kept
    ///   pre-load Z from LSR (mP=$26).
    /// Acceptance: LDA #$00 / LSR A / BCS not-taken / PHA / LDA ($10),Y of
    ///   $01: last CLK (cycle 0) has A=$01, Z clear, PC at opcode+2.
    /// </summary>
    [Fact]
    public void LdaIndYAfterPha_LastClk_ExportsFallthroughPcAndClearsZ()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x0010] = 0x00;
        memory[0x0011] = 0x90;
        memory[0x9000] = 0x01;
        memory[0x8000] = 0xA9; // LDA #$00
        memory[0x8001] = 0x00;
        memory[0x8002] = 0x4A; // LSR A (Z=1, C=0)
        memory[0x8003] = 0xB0; // BCS not-taken
        memory[0x8004] = 0x00;
        memory[0x8005] = 0x48; // PHA
        memory[0x8006] = 0xB1; // LDA ($10),Y
        memory[0x8007] = 0x10;
        memory[0x8008] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        AdvanceToOpcode(cpu, 0xB1);
        while (cpu.DebugOpcode == 0xB1 && cpu.DebugCycle != 0)
            cpu.Tick();

        Assert.Equal((byte)0xB1, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((byte)0x01, cpu.A);
        Assert.Equal((ushort)0x8008, cpu.PC);
        Assert.Equal(0, cpu.P & 0x02);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 sample 3029797. ORA abs after not-taken BEQ whose
    ///   last CLK already exported next-PC. VICE ORA(GET_ABS) is
    ///   LOCAL_SET_NZ then INC_PC with no extra CLK (nPC=$EAC4 nP=$24).
    ///   Managed soft-deferred the ALU and held opcode PC plus pre-op N
    ///   (mPC=$EAC1 mP=$A4).
    /// Acceptance: LDA #$01 / CMP #$02 (N=1) / BEQ not-taken / ORA $9000 of
    ///   $00: last CLK has A=$01, N clear, PC at opcode+3.
    /// </summary>
    [Fact]
    public void OraAbsAfterNotTakenBeq_LastClk_ExportsFallthroughPcAndClearsN()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x9000] = 0x00;
        memory[0x8000] = 0xA9; // LDA #$01
        memory[0x8001] = 0x01;
        memory[0x8002] = 0xC9; // CMP #$02 (N=1, Z=0)
        memory[0x8003] = 0x02;
        memory[0x8004] = 0xF0; // BEQ not-taken
        memory[0x8005] = 0x00;
        memory[0x8006] = 0x0D; // ORA $9000
        memory[0x8007] = 0x00;
        memory[0x8008] = 0x90;
        memory[0x8009] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        AdvanceToOpcode(cpu, 0x0D);
        while (cpu.DebugOpcode == 0x0D && cpu.DebugCycle != 0)
            cpu.Tick();

        Assert.Equal((byte)0x0D, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((byte)0x01, cpu.A);
        Assert.Equal((ushort)0x8009, cpu.PC);
        Assert.Equal(0, cpu.P & 0x80);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 sample 3029800. STA abs after fused ORA abs. VICE
    ///   ST is INC_PC then SET_ABS STORE CLK_INC, so the write CLK already
    ///   shows opcode+3 (nPC=$EAC7). Managed held opcode PC (mPC=$EAC4).
    ///   Taken BPL then ADC zp still holds (2122622).
    /// Acceptance: LDA #$01 / ORA $9000 / STA $0400: write CLK (cycle 1)
    ///   PC is STA opcode+3.
    /// </summary>
    [Fact]
    public void StaAbsAfterOraAbs_WriteClk_ExportsFallthroughPc()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x9000] = 0x00;
        memory[0x8000] = 0xA9; // LDA #$01
        memory[0x8001] = 0x01;
        memory[0x8002] = 0x0D; // ORA $9000
        memory[0x8003] = 0x00;
        memory[0x8004] = 0x90;
        memory[0x8005] = 0x8D; // STA $0400
        memory[0x8006] = 0x00;
        memory[0x8007] = 0x04;
        memory[0x8008] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        AdvanceToOpcode(cpu, 0x8D);
        while (cpu.DebugOpcode == 0x8D && cpu.DebugCycle != 1)
            cpu.Tick();

        Assert.Equal((byte)0x8D, cpu.DebugOpcode);
        Assert.Equal(1, cpu.DebugCycle);
        Assert.Equal((ushort)0x8008, cpu.PC);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 sample 4132756. After EOR # whose last CLK still
    ///   showed the EOR opcode (nPC=$E68A nA pre-op), VICE STA zp ST is
    ///   INC_PC after the SET_ZERO STORE CLK, so the write CLK still
    ///   exports opcode PC (nPC=$E68C). Managed advanced to opcode+2
    ///   (mPC=$E68E). Trail>=2 STA zp last CLK still fuses (2571318).
    ///   STA abs after AND # still advances (2060359).
    /// Acceptance: LDA #$22 / SEC / STA zp / BCS taken / INC zp / JSR to
    ///   CMP #$22 / BNE not-taken / LDA zp of $00 / EOR #$01 / STA $02:
    ///   EOR last CLK stays at the EOR opcode; STA DebugCycle 1 (write) PC
    ///   is the STA opcode. Same prefix as Wolf64 4132742 then 4132756.
    /// </summary>
    [Fact]
    public void StaZpAfterEorImm_WriteClk_HoldsOpcodePc()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x0011] = 0x00;
        memory[0x8000] = 0xA9; // LDA #$22
        memory[0x8001] = 0x22;
        memory[0x8002] = 0x38; // SEC
        memory[0x8003] = 0x85; // STA $10
        memory[0x8004] = 0x10;
        memory[0x8005] = 0xB0; // BCS taken
        memory[0x8006] = 0x00;
        memory[0x8007] = 0xE6; // INC $10
        memory[0x8008] = 0x10;
        memory[0x8009] = 0x20; // JSR $9000
        memory[0x800A] = 0x00;
        memory[0x800B] = 0x90;
        memory[0x800C] = 0xEA;
        memory[0x9000] = 0xC9; // CMP #$22 (Z=1)
        memory[0x9001] = 0x22;
        memory[0x9002] = 0xD0; // BNE not-taken
        memory[0x9003] = 0x00;
        memory[0x9004] = 0xA5; // LDA $11 (A=$00)
        memory[0x9005] = 0x11;
        memory[0x9006] = 0x49; // EOR #$01
        memory[0x9007] = 0x01;
        memory[0x9008] = 0x85; // STA $02
        memory[0x9009] = 0x02;
        memory[0x900A] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        AdvanceToOpcode(cpu, 0x49);
        while (cpu.DebugOpcode == 0x49 && cpu.DebugCycle != 0)
            cpu.Tick();

        Assert.Equal((byte)0x49, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((ushort)0x9006, cpu.PC);

        AdvanceToOpcode(cpu, 0x85);
        while (cpu.DebugOpcode == 0x85 && cpu.DebugCycle != 1)
            cpu.Tick();

        Assert.Equal((byte)0x85, cpu.DebugOpcode);
        Assert.Equal(1, cpu.DebugCycle);
        Assert.Equal((byte)0x01, cpu.A);
        Assert.Equal((ushort)0x9008, cpu.PC);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 sample 4134474. After STX zp whose last CLK already
    ///   exported next-PC (4134472 nPC=$A494), VICE not-taken BCC BRANCH
    ///   INC_PC(2) is visible on the last CLK (nPC=$A496). Managed held
    ///   opcode PC (mPC=$A494). skipHoldAfterFusedLoad covered loads and
    ///   compares; STX zp (0x86) was missing. C=1 so BCC is not taken.
    /// Acceptance: SEC / LDA #$4C / TAX / BEQ not-taken / LDX #$FF /
    ///   STX $02 / BCC not-taken: last CLK of BCC exports opcode+2.
    /// </summary>
    [Fact]
    public void BccNotTakenAfterFusedStxZp_LastClk_ExportsFallthrough()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x8000] = 0x38; // SEC (C=1)
        memory[0x8001] = 0xA9; // LDA #$4C
        memory[0x8002] = 0x4C;
        memory[0x8003] = 0xAA; // TAX
        memory[0x8004] = 0xF0; // BEQ not-taken (Z=0)
        memory[0x8005] = 0x00;
        memory[0x8006] = 0xA2; // LDX #$FF
        memory[0x8007] = 0xFF;
        memory[0x8008] = 0x86; // STX $02
        memory[0x8009] = 0x02;
        memory[0x800A] = 0x90; // BCC not-taken (C=1)
        memory[0x800B] = 0x00;
        memory[0x800C] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        AdvanceToOpcode(cpu, 0x90);
        while (cpu.DebugOpcode == 0x90 && cpu.DebugCycle != 0)
            cpu.Tick();

        Assert.Equal((byte)0x90, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((ushort)0x800C, cpu.PC);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 sample 4134557. After not-taken BEQ whose last CLK
    ///   already exported fall-through (4134554 nPC=$A598), VICE BIT zp
    ///   GET_ZERO+INC_PC is visible on the last CLK (nPC=$A59A). Managed
    ///   soft-deferred opcode PC (mPC=$A598) because previous was a branch.
    ///   Taken BNE trail=1 still softs (c=522392).
    /// Acceptance: LDA #$4C / CMP #$00 / BEQ not-taken / BIT $10: last CLK
    ///   of BIT exports opcode+2.
    /// </summary>
    [Fact]
    public void BitZpAfterNotTakenBeq_LastClk_ExportsFallthroughPc()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x0010] = 0x00;
        memory[0x8000] = 0xA9; // LDA #$4C (Z=0)
        memory[0x8001] = 0x4C;
        memory[0x8002] = 0xC9; // CMP #$00
        memory[0x8003] = 0x00;
        memory[0x8004] = 0xF0; // BEQ not-taken
        memory[0x8005] = 0x00;
        memory[0x8006] = 0x24; // BIT $10
        memory[0x8007] = 0x10;
        memory[0x8008] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        AdvanceToOpcode(cpu, 0x24);
        while (cpu.DebugOpcode == 0x24 && cpu.DebugCycle != 0)
            cpu.Tick();

        Assert.Equal((byte)0x24, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((ushort)0x8008, cpu.PC);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 sample 3505560. Wait-loop LDA zp data-read frozen on
    ///   a badline (cycle 1, x=42-54). VICE GET_ZERO is not DO_INTERRUPT;
    ///   after unstall the following STA zp FETCHes (nPC=$E5D1 nS=$F3
    ///   nLastOp=$85). Managed sampled IRQ on the post-last-CLK tick
    ///   (mS=$F2 irqSeq=4).
    /// Acceptance: BEQ taken / LDA $10 / STA $11, IRQ pending, OnStolenCycle
    ///   at LDA cycle 1: STA zp FETCHes and S stays $F3.
    /// </summary>
    [Fact]
    public void LdaZpAfterStolenDataRead_DoesNotDispatchIrqBeforeFollowingSta()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0xFFFE] = 0x00;
        memory[0xFFFF] = 0x09;
        memory[0x0010] = 0x00;
        memory[0x8000] = 0xA9; // LDA #$00 (Z=1)
        memory[0x8001] = 0x00;
        memory[0x8002] = 0xF0; // BEQ $8004
        memory[0x8003] = 0x00;
        memory[0x8004] = 0xA5; // LDA $10
        memory[0x8005] = 0x10;
        memory[0x8006] = 0x85; // STA $11
        memory[0x8007] = 0x11;
        memory[0x8008] = 0xEA;
        memory[0x0900] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var irq = new InterruptLine(InterruptType.Irq);
        var cpu = new Mos6502(bus);
        var clock = new SystemClock(985_248, cpu, irq);
        clock.Register(cpu);
        cpu.Reset();
        cpu.P = 0x24; // I set so IRQ delay counts without dispatching
        cpu.S = 0xF3;
        cpu.PC = 0x8000;
        irq.Assert(new RtsTyaIrqSource());

        for (var i = 0; i < 40 && cpu.DebugOpcode != 0xA5; i++)
            clock.Step();
        Assert.Equal((byte)0xA5, cpu.DebugOpcode);
        while (cpu.DebugOpcode == 0xA5 && cpu.DebugCycle != 1)
            clock.Step();
        Assert.Equal(1, cpu.DebugCycle);
        cpu.P = (byte)(cpu.P & ~0x04);
        cpu.OnStolenCycle();

        byte sAfter = cpu.S;
        for (var i = 0; i < 16; i++)
        {
            clock.Step();
            sAfter = cpu.S;
            if (cpu.DebugOpcode == 0x85)
                break;
        }

        Assert.Equal((byte)0xF3, sAfter);
        Assert.Equal((byte)0x85, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugInterruptSequenceRemaining);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 sample 3899688. Wait-loop STA zp remaining apply
    ///   frozen on a badline (cycle 1, x=42-54). VICE STORE is not
    ///   DO_INTERRUPT; after unstall STA abs FETCHes (nLastOp=$8D nS=$F3).
    ///   Managed armed IRQ on the apply tick (mS=$F2 irqSeq=4).
    /// Acceptance: LDA #$00 / STA $11 / STA $0400, IRQ pending, OnStolenCycle
    ///   at STA zp cycle 1: STA abs FETCHes and S stays $F3.
    /// </summary>
    [Fact]
    public void StaZpAfterStolenApply_DoesNotDispatchIrqBeforeFollowingStaAbs()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0xFFFE] = 0x00;
        memory[0xFFFF] = 0x09;
        memory[0x8000] = 0xA9; // LDA #$00
        memory[0x8001] = 0x00;
        memory[0x8002] = 0x85; // STA $11
        memory[0x8003] = 0x11;
        memory[0x8004] = 0x8D; // STA $0400
        memory[0x8005] = 0x00;
        memory[0x8006] = 0x04;
        memory[0x8007] = 0xEA;
        memory[0x0900] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var irq = new InterruptLine(InterruptType.Irq);
        var cpu = new Mos6502(bus);
        var clock = new SystemClock(985_248, cpu, irq);
        clock.Register(cpu);
        cpu.Reset();
        cpu.P = 0x24;
        cpu.S = 0xF3;
        cpu.PC = 0x8000;
        irq.Assert(new RtsTyaIrqSource());

        for (var i = 0; i < 40 && cpu.DebugOpcode != 0x85; i++)
            clock.Step();
        Assert.Equal((byte)0x85, cpu.DebugOpcode);
        while (cpu.DebugOpcode == 0x85 && cpu.DebugCycle != 1)
            clock.Step();
        Assert.Equal(1, cpu.DebugCycle);
        cpu.P = (byte)(cpu.P & ~0x04);
        cpu.OnStolenCycle();

        byte sAfter = cpu.S;
        for (var i = 0; i < 16; i++)
        {
            clock.Step();
            sAfter = cpu.S;
            if (cpu.DebugOpcode == 0x8D)
                break;
        }

        Assert.Equal((byte)0xF3, sAfter);
        Assert.Equal((byte)0x8D, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugInterruptSequenceRemaining);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 sample 2406964. LDA abs,X after an insn whose last
    ///   CLK already exported next-PC. VICE GET_ABS_X writes A then CLK_INC,
    ///   so the data-read CLK already has loaded A (nA=$EB). Managed kept
    ///   pre-load A in nonOvl (mA=$81). Distinct from LDA abs,X directly
    ///   after a full-length taken branch, which still pre-loads on data-read.
    /// Acceptance: LDA #$81 / LDX #$00 / SEC / STA zp / BCS taken / PHA /
    ///   LDA $9000,X of $EB: data-read CLK (cycle 1) has A=$EB. PHA last CLK
    ///   already at next-PC (trail&gt;=1), matching Wolf64 2406961 then 2406964.
    /// </summary>
    [Fact]
    public void LdaAbsXAfterTakenBcsPha_DataRead_ShowsLoadedA()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x9000] = 0xEB;
        memory[0x8000] = 0xA9; // LDA #$81 (pre-load A)
        memory[0x8001] = 0x81;
        memory[0x8002] = 0xA2; // LDX #$00
        memory[0x8003] = 0x00;
        memory[0x8004] = 0x38; // SEC
        memory[0x8005] = 0x85; // STA $02
        memory[0x8006] = 0x02;
        memory[0x8007] = 0xB0; // BCS $8009
        memory[0x8008] = 0x00;
        memory[0x8009] = 0x48; // PHA (last CLK already next-PC)
        memory[0x800A] = 0xBD; // LDA $9000,X
        memory[0x800B] = 0x00;
        memory[0x800C] = 0x90;
        memory[0x800D] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        AdvanceToOpcode(cpu, 0xBD);
        while (cpu.DebugOpcode == 0xBD && cpu.DebugCycle != 1)
            cpu.Tick();

        Assert.Equal((byte)0xBD, cpu.DebugOpcode);
        Assert.Equal(1, cpu.DebugCycle);
        Assert.Equal((byte)0xEB, cpu.A);

        while (cpu.DebugOpcode == 0xBD && cpu.DebugCycle != 0)
            cpu.Tick();

        Assert.Equal((byte)0xBD, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((byte)0xEB, cpu.A);
        Assert.Equal((ushort)0x800D, cpu.PC);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 sample 2571284. LDX abs after LDA zp whose last CLK
    ///   already exported next-PC. VICE GET_ABS writes X then CLK_INC, so
    ///   the data-read CLK already has loaded X (nX=$0E). Managed kept
    ///   pre-load X in nonOvl (mX=$01).
    /// Acceptance: LDX #$01 / SEC / STA zp / BCS taken / PHA / LDX $9000
    ///   of $0E: data-read CLK (cycle 1) has X=$0E. Last CLK (cycle 0)
    ///   exports opcode+3 (VICE GET_ABS CLK_INC then INC_PC with no extra
    ///   CLK; Wolf64 2571285 nPC=$E5E0).
    /// </summary>
    [Fact]
    public void LdxAbsAfterTakenBcsPha_DataRead_ShowsLoadedX()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x9000] = 0x0E;
        memory[0x8000] = 0xA2; // LDX #$01 (pre-load X)
        memory[0x8001] = 0x01;
        memory[0x8002] = 0x38; // SEC
        memory[0x8003] = 0x85; // STA $02
        memory[0x8004] = 0x02;
        memory[0x8005] = 0xB0; // BCS $8007
        memory[0x8006] = 0x00;
        memory[0x8007] = 0x48; // PHA
        memory[0x8008] = 0xAE; // LDX $9000
        memory[0x8009] = 0x00;
        memory[0x800A] = 0x90;
        memory[0x800B] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        AdvanceToOpcode(cpu, 0xAE);
        while (cpu.DebugOpcode == 0xAE && cpu.DebugCycle != 1)
            cpu.Tick();

        Assert.Equal((byte)0xAE, cpu.DebugOpcode);
        Assert.Equal(1, cpu.DebugCycle);
        Assert.Equal((byte)0x0E, cpu.X);

        while (cpu.DebugOpcode == 0xAE && cpu.DebugCycle != 0)
            cpu.Tick();

        Assert.Equal((byte)0xAE, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((byte)0x0E, cpu.X);
        Assert.Equal((ushort)0x800B, cpu.PC);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 sample 2571293. After STY zp whose last CLK already
    ///   exported next-PC, VICE JSR() does STACK_PEEK CLK then INC_PC(2)+PUSH
    ///   high on the next CLK, so DebugCycle 3 already has PC=opcode+2 and
    ///   S decremented (nPC=$E5E6 nS=$F2). Managed non-overlapped STACK_PEEK
    ///   kept opcode PC and pre-push S (mPC=$E5E4 mS=$F3).
    /// Acceptance: LDY #$00 / CLC / BCC taken / LDA abs / STY $10 / JSR $9100
    ///   at JSR DebugCycle 3 has PC=opcode+2 and S=fetchS-1. LDA abs after
    ///   the taken BCC leaves the sticky non-overlapped FETCH phase that
    ///   Wolf64 still has at 2571293 (loadEarly path).
    /// </summary>
    [Fact]
    public void JsrAfterStyZp_Cycle3_ShowsIncPcAndPushHigh()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x9000] = 0x20;
        memory[0x8000] = 0xA0; // LDY #$00
        memory[0x8001] = 0x00;
        memory[0x8002] = 0x18; // CLC
        memory[0x8003] = 0x90; // BCC taken
        memory[0x8004] = 0x00;
        memory[0x8005] = 0xAD; // LDA $9000 (sticky nonOvl FETCH phase)
        memory[0x8006] = 0x00;
        memory[0x8007] = 0x90;
        memory[0x8008] = 0x84; // STY $10
        memory[0x8009] = 0x10;
        memory[0x800A] = 0x20; // JSR $9100
        memory[0x800B] = 0x00;
        memory[0x800C] = 0x91;
        memory[0x800D] = 0xEA;
        memory[0x9100] = 0x60;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        cpu.S = 0xF3;
        AdvanceToOpcode(cpu, 0x20);
        while (cpu.DebugOpcode == 0x20 && cpu.DebugCycle != 5)
            cpu.Tick();

        Assert.Equal((byte)0x20, cpu.DebugOpcode);
        Assert.Equal(5, cpu.DebugCycle);
        var sFetch = cpu.S;
        var jsrPc = cpu.PC;

        while (cpu.DebugOpcode == 0x20 && cpu.DebugCycle != 3)
            cpu.Tick();

        Assert.Equal((byte)0x20, cpu.DebugOpcode);
        Assert.Equal(3, cpu.DebugCycle);
        Assert.Equal((ushort)(jsrPc + 2), cpu.PC);
        Assert.Equal((byte)(sFetch - 1), cpu.S);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 sample 153101. VICE SET_ABS consumes two operand
    /// FETCH clocks before the store. A following JSR in the retained
    /// non-overlapped region is therefore still on its explicit STACK_PEEK
    /// at DebugCycle 3, with opcode PC and the pre-push stack pointer.
    /// Acceptance: a taken branch / LDA abs / STA abs / JSR sequence retains
    /// opcode PC and does not decrement S at JSR DebugCycle 3. The neighboring
    /// STY zp regression remains the counterexample because SET_ZERO consumes
    /// only one operand FETCH clock.
    /// </summary>
    [Fact]
    public void JsrAfterStaAbs_Cycle3StillPeeksStack()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x9000] = 0x0E;
        memory[0x8000] = 0xA9; // LDA #$00
        memory[0x8001] = 0x00;
        memory[0x8002] = 0x18; // CLC
        memory[0x8003] = 0x90; // BCC taken
        memory[0x8004] = 0x00;
        memory[0x8005] = 0xAD; // LDA $9000 (sticky nonOvl FETCH phase)
        memory[0x8006] = 0x00;
        memory[0x8007] = 0x90;
        memory[0x8008] = 0x8D; // STA $0010
        memory[0x8009] = 0x10;
        memory[0x800A] = 0x00;
        memory[0x800B] = 0x20; // JSR $9100
        memory[0x800C] = 0x00;
        memory[0x800D] = 0x91;
        memory[0x800E] = 0xEA;
        memory[0x9100] = 0x60;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        cpu.S = 0xF3;
        AdvanceToOpcode(cpu, 0x20);
        while (cpu.DebugOpcode == 0x20 && cpu.DebugCycle != 5)
            cpu.Tick();

        Assert.Equal((byte)0x20, cpu.DebugOpcode);
        Assert.Equal(5, cpu.DebugCycle);
        var sFetch = cpu.S;
        var jsrPc = cpu.PC;

        while (cpu.DebugOpcode == 0x20 && cpu.DebugCycle != 3)
            cpu.Tick();

        Assert.Equal((byte)0x20, cpu.DebugOpcode);
        Assert.Equal(3, cpu.DebugCycle);
        Assert.Equal(jsrPc, cpu.PC);
        Assert.Equal(sFetch, cpu.S);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001, TEST: TEST-X64SC-LOCKSTEP-001.
    /// VICE RTS completes its return JUMP without a clock. A JSR at the return
    /// target then executes both FETCH_OPCODE clocks and STACK_PEEK before its
    /// first return-address push, independent of earlier overlap history.
    /// </summary>
    [Fact]
    public void JsrAfterRts_Cycle3StillPeeksStack()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x8000] = 0x20; // JSR $8100
        memory[0x8001] = 0x00;
        memory[0x8002] = 0x81;
        memory[0x8003] = 0x20; // JSR $9000 after RTS
        memory[0x8004] = 0x00;
        memory[0x8005] = 0x90;
        memory[0x8006] = 0xEA;
        memory[0x8100] = 0x60; // RTS
        memory[0x9000] = 0x60;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        cpu.S = 0xF4;

        for (var i = 0; i < 32 && !(cpu.DebugOpcode == 0x20 && cpu.DebugOpcodeAddress == 0x8003); i++)
            cpu.Tick();
        while (cpu.DebugOpcode == 0x20 && cpu.DebugCycle != 5)
            cpu.Tick();

        Assert.Equal((ushort)0x8003, cpu.DebugOpcodeAddress);
        Assert.Equal(5, cpu.DebugCycle);
        var sAtFetch = cpu.S;

        while (cpu.DebugOpcode == 0x20 && cpu.DebugCycle != 3)
            cpu.Tick();

        Assert.Equal((byte)0x20, cpu.DebugOpcode);
        Assert.Equal(3, cpu.DebugCycle);
        Assert.Equal((ushort)0x8003, cpu.PC);
        Assert.Equal(sAtFetch, cpu.S);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001, TEST: TEST-X64SC-LOCKSTEP-001.
    /// VICE completes an immediate load's register and flag updates without a
    /// clock, then begins the following FETCH_OPCODE. When RTS returns to LDY #
    /// followed by JSR, that JSR therefore retains its STACK_PEEK at cycle 3.
    /// </summary>
    [Fact]
    public void JsrAfterSoftDeferredImmediateLoad_Cycle3StillPeeksStack()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x8000] = 0x20; // JSR $8100
        memory[0x8001] = 0x00;
        memory[0x8002] = 0x81;
        memory[0x8003] = 0xA0; // LDY #$27 after RTS
        memory[0x8004] = 0x27;
        memory[0x8005] = 0x20; // JSR $9000
        memory[0x8006] = 0x00;
        memory[0x8007] = 0x90;
        memory[0x8008] = 0xEA;
        memory[0x8100] = 0x60; // RTS
        memory[0x9000] = 0x60;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        cpu.S = 0xF4;

        for (var i = 0; i < 40 && !(cpu.DebugOpcode == 0x20 && cpu.DebugOpcodeAddress == 0x8005); i++)
            cpu.Tick();
        while (cpu.DebugOpcode == 0x20 && cpu.DebugCycle != 5)
            cpu.Tick();

        Assert.Equal((ushort)0x8005, cpu.DebugOpcodeAddress);
        Assert.Equal(5, cpu.DebugCycle);
        Assert.Equal((byte)0x27, cpu.Y);
        var sAtFetch = cpu.S;

        while (cpu.DebugOpcode == 0x20 && cpu.DebugCycle != 3)
            cpu.Tick();

        Assert.Equal((byte)0x20, cpu.DebugOpcode);
        Assert.Equal(3, cpu.DebugCycle);
        Assert.Equal((ushort)0x8005, cpu.PC);
        Assert.Equal(sAtFetch, cpu.S);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 sample 4132742. Taken BVS then INC zp then JSR:
    ///   VICE still STACK_PEEKs at JSR DebugCycle 3 (nPC=$E656 nS=$F3).
    ///   INC after ORA# (no branch lag) then JSR PUSHes (4132037 nS=$F2).
    /// Acceptance: LDA #$22 / SEC / STA $10 / BCS taken / INC $10 / JSR $9000:
    ///   JSR DebugCycle 3 still has fetch S and opcode PC.
    /// </summary>
    [Fact]
    public void JsrAfterIncZp_Cycle3_StillPeeksStack()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x8000] = 0xA9; // LDA #$22
        memory[0x8001] = 0x22;
        memory[0x8002] = 0x38; // SEC
        memory[0x8003] = 0x85; // STA $10
        memory[0x8004] = 0x10;
        memory[0x8005] = 0xB0; // BCS $8007
        memory[0x8006] = 0x00;
        memory[0x8007] = 0xE6; // INC $10
        memory[0x8008] = 0x10;
        memory[0x8009] = 0x20; // JSR $9000
        memory[0x800A] = 0x00;
        memory[0x800B] = 0x90;
        memory[0x800C] = 0xEA;
        memory[0x9000] = 0x60;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        cpu.S = 0xF3;
        AdvanceToOpcode(cpu, 0x20);
        while (cpu.DebugOpcode == 0x20 && cpu.DebugCycle != 5)
            cpu.Tick();

        Assert.Equal((byte)0x20, cpu.DebugOpcode);
        Assert.Equal(5, cpu.DebugCycle);
        var sFetch = cpu.S;
        var jsrPc = cpu.PC;

        while (cpu.DebugOpcode == 0x20 && cpu.DebugCycle != 3)
            cpu.Tick();

        Assert.Equal((byte)0x20, cpu.DebugOpcode);
        Assert.Equal(3, cpu.DebugCycle);
        Assert.Equal(jsrPc, cpu.PC);
        Assert.Equal(sFetch, cpu.S);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 sample 2571298. After overlapped JSR JUMP already
    ///   exported the callee PC, VICE TAY writes Y=A and INC_PC on the last
    ///   CLK (nY=$20 nPC=$EA14). Managed soft-deferred in sticky nonOvl
    ///   (mY=$00 mPC=$EA13). JMP abs trail==1 already fuses implied; JSR
    ///   JUMP is the same shape.
    /// Acceptance: LDY #$00 / CLC / BCC taken / LDA abs of $20 / STY $10 /
    ///   JSR to TAY: last CLK of TAY has Y=$20 and PC=opcode+1.
    /// </summary>
    [Fact]
    public void TayAfterJsr_LastClk_ExportsYFromAAndNextPc()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x9000] = 0x20;
        memory[0x8000] = 0xA0; // LDY #$00
        memory[0x8001] = 0x00;
        memory[0x8002] = 0x18; // CLC
        memory[0x8003] = 0x90; // BCC taken
        memory[0x8004] = 0x00;
        memory[0x8005] = 0xAD; // LDA $9000 (A=$20, sticky nonOvl)
        memory[0x8006] = 0x00;
        memory[0x8007] = 0x90;
        memory[0x8008] = 0x84; // STY $10
        memory[0x8009] = 0x10;
        memory[0x800A] = 0x20; // JSR $9100
        memory[0x800B] = 0x00;
        memory[0x800C] = 0x91;
        memory[0x9100] = 0xA8; // TAY
        memory[0x9101] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        AdvanceToOpcode(cpu, 0xA8);
        while (cpu.DebugOpcode == 0xA8 && cpu.DebugCycle != 0)
            cpu.Tick();

        Assert.Equal((byte)0xA8, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((byte)0x20, cpu.Y);
        Assert.Equal((ushort)0x9101, cpu.PC);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 sample 2571311. After overlapped JSR JUMP already
    ///   exported the callee PC, VICE LDA zp GET_ZERO writes A then CLK_INC
    ///   (nA=$F0 at $EA24 cycle 1). Managed kept pre-load A in sticky nonOvl
    ///   (mA=$02). Last CLK INC_PC has no extra CLK.
    /// Acceptance: CLC / BCC taken / LDA abs / STY zp / JSR to LDA $10 of
    ///   $F0: data-read CLK has A=$F0; last CLK exports opcode+2.
    /// </summary>
    [Fact]
    public void LdaZpAfterJsr_DataRead_ShowsLoadedA()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x0010] = 0xF0;
        memory[0x9000] = 0x02;
        memory[0x8000] = 0xA0; // LDY #$00
        memory[0x8001] = 0x00;
        memory[0x8002] = 0x18; // CLC
        memory[0x8003] = 0x90; // BCC taken
        memory[0x8004] = 0x00;
        memory[0x8005] = 0xAD; // LDA $9000 (A=$02, sticky nonOvl)
        memory[0x8006] = 0x00;
        memory[0x8007] = 0x90;
        memory[0x8008] = 0x84; // STY $02
        memory[0x8009] = 0x02;
        memory[0x800A] = 0x20; // JSR $9100
        memory[0x800B] = 0x00;
        memory[0x800C] = 0x91;
        memory[0x9100] = 0xA5; // LDA $10
        memory[0x9101] = 0x10;
        memory[0x9102] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        AdvanceToOpcode(cpu, 0xA5);
        while (cpu.DebugOpcode == 0xA5 && cpu.DebugCycle != 1)
            cpu.Tick();

        Assert.Equal((byte)0xA5, cpu.DebugOpcode);
        Assert.Equal(1, cpu.DebugCycle);
        Assert.Equal((byte)0xF0, cpu.A);

        while (cpu.DebugOpcode == 0xA5 && cpu.DebugCycle != 0)
            cpu.Tick();

        Assert.Equal((byte)0xA5, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((byte)0xF0, cpu.A);
        Assert.Equal((ushort)0x9102, cpu.PC);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 sample 2122636. After STA zp that held opcode PC on
    ///   the write CLK (trail==1), VICE LDA zp GET_ZERO still pre-loads A
    ///   (nA=$00). Trail>=2 after a fused STA write commits (2571317 nA=$04).
    ///   Last CLK still holds opcode PC (2122628 nPC=$B902).
    /// Acceptance: CLC / BCC taken / LDA abs of $F0 / STA $02 / LDA $10 of
    ///   $04: data-read CLK still has pre-load A=$F0; last CLK commits A=$04
    ///   and keeps opcode PC.
    /// </summary>
    [Fact]
    public void LdaZpAfterStaZp_DataRead_StillShowsPreLoadA()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x0010] = 0x04;
        memory[0x9000] = 0xF0;
        memory[0x8000] = 0x18; // CLC
        memory[0x8001] = 0x90; // BCC taken
        memory[0x8002] = 0x00;
        memory[0x8003] = 0xAD; // LDA $9000 (A=$F0, sticky nonOvl)
        memory[0x8004] = 0x00;
        memory[0x8005] = 0x90;
        memory[0x8006] = 0x85; // STA $02
        memory[0x8007] = 0x02;
        memory[0x8008] = 0xA5; // LDA $10
        memory[0x8009] = 0x10;
        memory[0x800A] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        AdvanceToOpcode(cpu, 0xA5);
        while (cpu.DebugOpcode == 0xA5 && cpu.DebugCycle != 1)
            cpu.Tick();

        Assert.Equal((byte)0xA5, cpu.DebugOpcode);
        Assert.Equal(1, cpu.DebugCycle);
        Assert.Equal((byte)0xF0, cpu.A);

        while (cpu.DebugOpcode == 0xA5 && cpu.DebugCycle != 0)
            cpu.Tick();

        Assert.Equal((byte)0xA5, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((byte)0x04, cpu.A);
        Assert.Equal((ushort)0x8008, cpu.PC);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 sample 2571317. After LDA zp whose last CLK already
    ///   exported next-PC, STA zp write is also at next-PC (trail>=2). VICE
    ///   GET_ZERO of the following LDA zp commits A on the data-read CLK
    ///   (nA=$04). Managed kept pre-load A=$F0.
    /// Acceptance: JSR to LDA $10 of $F0 / STA $02 / LDA $11 of $04:
    ///   second LDA zp data-read CLK has A=$04.
    /// </summary>
    [Fact]
    public void LdaZpAfterFusedLdaThenSta_DataRead_ShowsLoadedA()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x0010] = 0xF0;
        memory[0x0011] = 0x04;
        memory[0x9000] = 0x02;
        memory[0x8000] = 0xA0; // LDY #$00
        memory[0x8001] = 0x00;
        memory[0x8002] = 0x18; // CLC
        memory[0x8003] = 0x90; // BCC taken
        memory[0x8004] = 0x00;
        memory[0x8005] = 0xAD; // LDA $9000 (sticky nonOvl)
        memory[0x8006] = 0x00;
        memory[0x8007] = 0x90;
        memory[0x8008] = 0x84; // STY $02
        memory[0x8009] = 0x02;
        memory[0x800A] = 0x20; // JSR $9100
        memory[0x800B] = 0x00;
        memory[0x800C] = 0x91;
        memory[0x9100] = 0xA5; // LDA $10 (A=$F0, fused after JSR)
        memory[0x9101] = 0x10;
        memory[0x9102] = 0x85; // STA $02
        memory[0x9103] = 0x02;
        memory[0x9104] = 0xA5; // LDA $11
        memory[0x9105] = 0x11;
        memory[0x9106] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        AdvanceToOpcode(cpu, 0xA5);
        while (cpu.DebugOpcode == 0xA5)
            cpu.Tick();
        while (cpu.DebugOpcode == 0x85)
            cpu.Tick();
        Assert.Equal((byte)0xA5, cpu.DebugOpcode);
        while (cpu.DebugOpcode == 0xA5 && cpu.DebugCycle != 1)
            cpu.Tick();

        Assert.Equal((byte)0xA5, cpu.DebugOpcode);
        Assert.Equal(1, cpu.DebugCycle);
        Assert.Equal((byte)0x04, cpu.A);

        while (cpu.DebugOpcode == 0xA5 && cpu.DebugCycle != 0)
            cpu.Tick();

        Assert.Equal((byte)0xA5, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((byte)0x04, cpu.A);
        Assert.Equal((ushort)0x9106, cpu.PC);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 sample 2799411. After KERNAL IRQ vector FETCH of
    ///   PHA at $FF48, VICE PHA() is PUSH then CLK_INC with no check_ba,
    ///   so the write proceeds during BA (nS=$EF at RasterX 12). Managed
    ///   CanSteal at DebugCycle 2 stole that tick as a dummy (mS=$F0).
    /// Acceptance: PHA at DebugCycle 2 is not stealable. Tick decrements
    ///   S and lands on DebugCycle 1 with opcode PC still visible.
    /// </summary>
    [Fact]
    public void Pha_PushCycle_IsNotStealableAndDecrementsS()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x8000] = 0x48; // PHA
        memory[0x8001] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        cpu.S = 0xF0;
        AdvanceToOpcode(cpu, 0x48);

        Assert.Equal((byte)0x48, cpu.DebugOpcode);
        Assert.Equal(2, cpu.DebugCycle);
        Assert.False(cpu.CanStealCurrentCycle);

        cpu.Tick();

        Assert.Equal((byte)0x48, cpu.DebugOpcode);
        Assert.Equal(1, cpu.DebugCycle);
        Assert.Equal((byte)0xEF, cpu.S);
        Assert.Equal((ushort)0x8000, cpu.PC);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 sample 2865090. One KERNAL wait-loop iteration is
    ///   VICE LDA zp (3 CLK) + STA zp (3) + STA abs (4) + BEQ dummy (3) =
    ///   13 CLK_INC; JUMP has no CLK. Collapsed second FETCH CLK of LDA zp
    ///   and STA zp must count (two missing CLK_INC at 2865090: nIrqDelay=6
    ///   mIrqDelay=4). Do not count the latch CLK (2109675), do not add an
    ///   extra LDA FETCH host tick (2123641), and do not double BEQ/STA abs
    ///   FETCH (2175366 / 2224630).
    /// Acceptance: After IRQ is asserted, one full wait-loop from BEQ JUMP
    ///   to the next BEQ JUMP increases irq_delay_cycles by 11 (9 host CLK
    ///   plus the two collapsed zp FETCHes).
    /// </summary>
    [Fact]
    public void WaitLoopIteration_IrqPending_CountsThirteenViceClocks()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x8000] = 0xA5; // LDA $10
        memory[0x8001] = 0x10;
        memory[0x8002] = 0x85; // STA $11
        memory[0x8003] = 0x11;
        memory[0x8004] = 0x8D; // STA $0400
        memory[0x8005] = 0x00;
        memory[0x8006] = 0x04;
        memory[0x8007] = 0xF0; // BEQ $8000
        memory[0x8008] = 0xF7;
        memory[0x0010] = 0x00;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var irq = new InterruptLine(InterruptType.Irq);
        var cpu = new Mos6502(bus);
        var clock = new SystemClock(985_248, cpu, irq);
        clock.Register(cpu);
        cpu.Reset();
        cpu.P = 0x26; // I set so IRQ never dispatches
        cpu.PC = 0x8000;
        irq.Assert(new RtsTyaIrqSource());

        for (var i = 0; i < 40 && !(cpu.DebugOpcode == 0xF0 && cpu.DebugCycle == 0); i++)
            clock.Step();
        Assert.Equal((byte)0xF0, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);

        var delayAtJump = clock.DebugIrqDelayCycles;
        clock.Step();
        for (var i = 0; i < 40 && !(cpu.DebugOpcode == 0xF0 && cpu.DebugCycle == 0); i++)
            clock.Step();
        Assert.Equal((byte)0xF0, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);

        var delta = clock.DebugIrqDelayCycles - delayAtJump;
        Assert.True(delta == 11, $"wait-loop irq_delay delta={delta} (want 11: 9 host CLK + 2 collapsed zp FETCH)");
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 sample 2865090. Wait-loop STA abs / BEQ taken / LDA zp.
    ///   VICE DO_INTERRUPT before LDA FETCH when DELAYS is set and irq_delay
    ///   is already elapsed (nS=$F2 nLastOp=$1F0). Do not increment irq_delay
    ///   on the latch CLK (2109675).
    /// Acceptance: LDA #$00 / STA zp / STA abs / BEQ / LDA zp, IRQ pending,
    ///   I cleared after BEQ FETCH: S drops and LDA zp does not execute.
    /// </summary>
    [Fact]
    public void WaitLoopStaAbsBeq_ElapsedIrq_DispatchesBeforeLdaZp()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0xFFFE] = 0x00;
        memory[0xFFFF] = 0x09;
        memory[0x0010] = 0x00;
        memory[0x8000] = 0xA9; // LDA #$00 (Z=1)
        memory[0x8001] = 0x00;
        memory[0x8002] = 0x85; // STA $11
        memory[0x8003] = 0x11;
        memory[0x8004] = 0x8D; // STA $0400
        memory[0x8005] = 0x00;
        memory[0x8006] = 0x04;
        memory[0x8007] = 0xF0; // BEQ $8009
        memory[0x8008] = 0x00;
        memory[0x8009] = 0xA5; // LDA $10
        memory[0x800A] = 0x10;
        memory[0x800B] = 0xEA;
        memory[0x0900] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var irq = new InterruptLine(InterruptType.Irq);
        var cpu = new Mos6502(bus);
        var clock = new SystemClock(985_248, cpu, irq);
        clock.Register(cpu);
        cpu.Reset();
        cpu.P = 0x24;
        cpu.S = 0xF3;
        cpu.PC = 0x8000;
        irq.Assert(new RtsTyaIrqSource());

        for (var i = 0; i < 40 && cpu.DebugOpcode != 0xF0; i++)
            clock.Step();
        Assert.Equal((byte)0xF0, cpu.DebugOpcode);
        cpu.P = (byte)(cpu.P & ~0x04);

        byte sAfter = cpu.S;
        for (var i = 0; i < 20; i++)
        {
            clock.Step();
            sAfter = cpu.S;
            if (sAfter < 0xF3)
                break;
        }

        Assert.True(sAfter < 0xF3, $"IRQ did not push before LDA zp; S=${sAfter:X2} op=${cpu.DebugOpcode:X2}");
        Assert.NotEqual((byte)0xA5, cpu.DebugOpcode);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 sample 2406973. LDY zp after an insn whose last
    ///   CLK already exported next-PC. VICE LD(reg_y, GET_ZERO, 2) writes
    ///   Y then CLK_INC, so the data-read CLK already has loaded Y
    ///   (nY=$2A). Managed kept pre-load Y in nonOvl (mY=$41). Distinct
    ///   from LDY zp directly after a full-length taken branch, which
    ///   still pre-loads on data-read.
    /// Acceptance: LDY #$41 / SEC / STA zp / BCS taken / PHA /
    ///   LDY $10 of $2A: data-read CLK (cycle 1) has Y=$2A. PHA last CLK
    ///   already at next-PC (trail&gt;=1), matching Wolf64 2406971 then
    ///   2406973 (JMP last CLK at $EAE0, same trail&gt;=1 GET_ZERO).
    ///   Last CLK of LDY zp exports opcode+2.
    /// </summary>
    [Fact]
    public void LdyZpAfterTakenBcsPha_DataRead_ShowsLoadedY()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x0010] = 0x2A;
        memory[0x8000] = 0xA0; // LDY #$41 (pre-load Y)
        memory[0x8001] = 0x41;
        memory[0x8002] = 0x38; // SEC
        memory[0x8003] = 0x85; // STA $02
        memory[0x8004] = 0x02;
        memory[0x8005] = 0xB0; // BCS $8007
        memory[0x8006] = 0x00;
        memory[0x8007] = 0x48; // PHA (last CLK already next-PC)
        memory[0x8008] = 0xA4; // LDY $10
        memory[0x8009] = 0x10;
        memory[0x800A] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        AdvanceToOpcode(cpu, 0xA4);
        while (cpu.DebugOpcode == 0xA4 && cpu.DebugCycle != 1)
            cpu.Tick();

        Assert.Equal((byte)0xA4, cpu.DebugOpcode);
        Assert.Equal(1, cpu.DebugCycle);
        Assert.Equal((byte)0x2A, cpu.Y);

        while (cpu.DebugOpcode == 0xA4 && cpu.DebugCycle != 0)
            cpu.Tick();

        Assert.Equal((byte)0xA4, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((byte)0x2A, cpu.Y);
        Assert.Equal((ushort)0x800A, cpu.PC);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 sample 2406440. After fused CPY # whose last CLK
    ///   already exported next-PC, VICE INY writes Y and INC_PC on the last
    ///   CLK (nY=$2B nPC=$EACD). Managed soft-deferred in nonOvl (mY=$2A
    ///   mPC=$EACC). Distinct from trail-0 implied hold.
    /// Acceptance: LDY #$2A / SEC / STA zp / BCS taken / INY / CPY #$00 /
    ///   INY, last CLK of the second INY has Y=$2C and PC=opcode+1.
    /// </summary>
    [Fact]
    public void InyAfterFusedCpyImm_LastClk_ExportsNextPcAndY()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x8000] = 0xA0; // LDY #$2A
        memory[0x8001] = 0x2A;
        memory[0x8002] = 0x38; // SEC
        memory[0x8003] = 0x85; // STA $02
        memory[0x8004] = 0x02;
        memory[0x8005] = 0xB0; // BCS $8007
        memory[0x8006] = 0x00;
        memory[0x8007] = 0xC8; // INY (Y=$2B)
        memory[0x8008] = 0xC0; // CPY #$00 (fused after INY)
        memory[0x8009] = 0x00;
        memory[0x800A] = 0xC8; // INY (should fuse)
        memory[0x800B] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        AdvanceToOpcode(cpu, 0xC0);
        while (cpu.DebugOpcode == 0xC0)
            cpu.Tick();
        Assert.Equal((byte)0xC8, cpu.DebugOpcode);
        while (cpu.DebugOpcode == 0xC8 && cpu.DebugCycle != 0)
            cpu.Tick();

        Assert.Equal((byte)0xC8, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((byte)0x2C, cpu.Y);
        Assert.Equal((ushort)0x800B, cpu.PC);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 sample 2406440. After PLA whose last CLK already
    ///   exported next-PC, VICE INY writes Y and INC_PC on the last CLK
    ///   (nY=$2B nPC=$EACD). Managed soft-deferred (mY=$2A mPC=$EACC).
    ///   PHA/PHP already fused implied at trail==1; PLA/PLP did not.
    /// Acceptance: LDY #$2A / SEC / STA zp / BCS taken / PHA / PLA / INY,
    ///   last CLK of INY has Y=$2B and PC=opcode+1.
    /// </summary>
    [Fact]
    public void InyAfterPla_LastClk_ExportsNextPcAndY()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x8000] = 0xA0; // LDY #$2A
        memory[0x8001] = 0x2A;
        memory[0x8002] = 0xA9; // LDA #$1F
        memory[0x8003] = 0x1F;
        memory[0x8004] = 0x38; // SEC
        memory[0x8005] = 0x85; // STA $02
        memory[0x8006] = 0x02;
        memory[0x8007] = 0xB0; // BCS $8009
        memory[0x8008] = 0x00;
        memory[0x8009] = 0x48; // PHA
        memory[0x800A] = 0x68; // PLA
        memory[0x800B] = 0xC8; // INY
        memory[0x800C] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        AdvanceToOpcode(cpu, 0x68);
        while (cpu.DebugOpcode == 0x68)
            cpu.Tick();
        Assert.Equal((byte)0xC8, cpu.DebugOpcode);
        while (cpu.DebugOpcode == 0xC8 && cpu.DebugCycle != 0)
            cpu.Tick();

        Assert.Equal((byte)0xC8, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((byte)0x2B, cpu.Y);
        Assert.Equal((ushort)0x800C, cpu.PC);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001.
    /// Use case: Wolf64 sample 2142608. After CMP abs last CLK already at
    ///   next-PC with Z set, not-taken BNE last CLK is fall-through. VICE TAX
    ///   writes X and INC_PC on the last CLK (nPC=$F6C5 nX=$FF nP=$A5).
    ///   Managed soft-deferred in nonOvl (mPC=$F6C4 mX=$00 mP=$27).
    /// Acceptance: LDA #$FF / CMP #$FF / BNE not-taken / TAX, last CLK of TAX
    ///   has X=$FF, PC=opcode+1, N set, Z clear.
    /// </summary>
    [Fact]
    public void TaxAfterNotTakenBne_LastClk_ExportsNextPcAndX()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x8000] = 0xA9; // LDA #$B0
        memory[0x8001] = 0xB0;
        memory[0x8002] = 0x85; // STA $02 (full-length BCC, sticky nonOvl)
        memory[0x8003] = 0x02;
        memory[0x8004] = 0x90; // BCC taken
        memory[0x8005] = 0x00;
        memory[0x8006] = 0xAD; // LDA $9000
        memory[0x8007] = 0x00;
        memory[0x8008] = 0x90;
        memory[0x8009] = 0xCD; // CMP $9000 (Z=1)
        memory[0x800A] = 0x00;
        memory[0x800B] = 0x90;
        memory[0x800C] = 0xD0; // BNE not taken
        memory[0x800D] = 0x00;
        memory[0x800E] = 0xAA; // TAX
        memory[0x800F] = 0xEA;
        memory[0x9000] = 0xFF;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        AdvanceToOpcode(cpu, 0xAA);
        while (cpu.DebugOpcode == 0xAA && cpu.DebugCycle != 0)
            cpu.Tick();

        Assert.Equal((byte)0xAA, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((byte)0xFF, cpu.X);
        Assert.Equal((ushort)0x800F, cpu.PC);
        Assert.Equal(0x80, cpu.P & 0x80);
        Assert.Equal(0x00, cpu.P & 0x02);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001.
    /// Use case: Wolf64 sample 2142610. After fused TAX last CLK already at
    ///   next-PC, taken BMI FETCH still shows the opcode (2142609 nPC=$F6C5
    ///   cycle=2). VICE BRANCH INC_PC(2) has no extra CLK; the dummy CLK_INC
    ///   exports fall-through (nPC=$F6C7). Managed held opcode PC.
    /// Acceptance: Same STA/BCC/LDA/CMP/BNE/TAX chain as 2142608, then BMI
    ///   taken; BMI DebugCycle 1 exports opcode+2.
    /// </summary>
    [Fact]
    public void TaxThenTakenBmi_Cycle1_ExportsFallthrough()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x8000] = 0xA9; // LDA #$B0
        memory[0x8001] = 0xB0;
        memory[0x8002] = 0x85; // STA $02 (full-length BCC)
        memory[0x8003] = 0x02;
        memory[0x8004] = 0x90; // BCC taken
        memory[0x8005] = 0x00;
        memory[0x8006] = 0xAD; // LDA $9000
        memory[0x8007] = 0x00;
        memory[0x8008] = 0x90;
        memory[0x8009] = 0xCD; // CMP $9000 (Z=1)
        memory[0x800A] = 0x00;
        memory[0x800B] = 0x90;
        memory[0x800C] = 0xD0; // BNE not taken
        memory[0x800D] = 0x00;
        memory[0x800E] = 0xAA; // TAX (fused, N set)
        memory[0x800F] = 0x30; // BMI taken
        memory[0x8010] = 0x02;
        memory[0x8011] = 0xEA;
        memory[0x8013] = 0xEA;
        memory[0x9000] = 0xFF;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        AdvanceToOpcode(cpu, 0x30);
        while (cpu.DebugOpcode == 0x30 && cpu.DebugCycle > 1)
            cpu.Tick();

        Assert.Equal((byte)0x30, cpu.DebugOpcode);
        Assert.Equal(1, cpu.DebugCycle);
        Assert.True(
            cpu.PC == 0x8011,
            $"Wolf64 2142610 BMI dummy INC_PC; PC=${cpu.PC:X4} cyc={cpu.DebugCycle} "
            + $"trail={cpu.DebugPriorTrailingAtNextPc} full={cpu.DebugFullLengthTakenBranch} "
            + $"nonOvlF={cpu.DebugNonOverlappedFetchPhase} nonOvlR={cpu.DebugNonOverlappedRegion}");
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001.
    /// Use case: Wolf64 sample 2060519. Soft-deferred TAX last CLK still at
    ///   opcode, so taken BMI dummy CLK still exports the BMI opcode
    ///   (nPC=$F6C5). Staging dummy INC_PC for every full-length taken branch
    ///   (_cycle &gt;= 3) advanced managed to $F6C7 and regressed this sample.
    /// Acceptance: Taken BNE / LDA abs / CMP abs (trail-0 hold) / BNE
    ///   not-taken / TAX / BMI taken; BMI DebugCycle 1 keeps opcode PC.
    /// </summary>
    [Fact]
    public void SoftTaxThenTakenBmi_Cycle1_KeepsOpcodePc()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x8000] = 0xA9; // LDA #$01 (Z=0)
        memory[0x8001] = 0x01;
        memory[0x8002] = 0xD0; // BNE taken, offset 0
        memory[0x8003] = 0x00;
        memory[0x8004] = 0xAD; // LDA $9000
        memory[0x8005] = 0x00;
        memory[0x8006] = 0x90;
        memory[0x8007] = 0xCD; // CMP $9000 (trail-0 last CLK)
        memory[0x8008] = 0x00;
        memory[0x8009] = 0x90;
        memory[0x800A] = 0xD0; // BNE not taken
        memory[0x800B] = 0x00;
        memory[0x800C] = 0xAA; // TAX (soft, last CLK opcode)
        memory[0x800D] = 0x30; // BMI taken
        memory[0x800E] = 0x02;
        memory[0x800F] = 0xEA;
        memory[0x8011] = 0xEA;
        memory[0x9000] = 0xFF;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        AdvanceToOpcode(cpu, 0x30);
        while (cpu.DebugOpcode == 0x30 && cpu.DebugCycle > 1)
            cpu.Tick();

        Assert.Equal((byte)0x30, cpu.DebugOpcode);
        Assert.Equal(1, cpu.DebugCycle);
        Assert.True(
            cpu.PC == 0x800D,
            $"Wolf64 2060519 BMI dummy keeps opcode; PC=${cpu.PC:X4} cyc={cpu.DebugCycle} "
            + $"trail={cpu.DebugPriorTrailingAtNextPc} full={cpu.DebugFullLengthTakenBranch} "
            + $"nonOvlF={cpu.DebugNonOverlappedFetchPhase} nonOvlR={cpu.DebugNonOverlappedRegion}");
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 sample 2175365. Taken same-page BEQ JUMP is VICE JUMP
    ///   with no CLK; DO_INTERRUPT runs before the target FETCH. Managed
    ///   opened the IRQ sample on that extra JUMP tick, so PCH pushed one CLK
    ///   early (mS=$F2 nS=$F3) while native was still on the IRQ dummy.
    /// Acceptance: LDA #$00 / BEQ taken same-page with IRQ pending: the BEQ
    ///   JUMP tick (cycle 0, PC=target) does not decrement S (it may arm
    ///   irqSeq=6). DELAYS is +1 cycle, not skip-all: when delay is already
    ///   elapsed, IRQ takes the following LDA #$FF so A stays 0.
    /// </summary>
    [Fact]
    public void TakenSamePageBeq_JumpTick_DoesNotPushIrq()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0xFFFE] = 0x00;
        memory[0xFFFF] = 0x09;
        memory[0x8000] = 0xA9; // LDA #$00 (Z=1)
        memory[0x8001] = 0x00;
        memory[0x8002] = 0xF0; // BEQ $8004 (taken, same page)
        memory[0x8003] = 0x00;
        memory[0x8004] = 0xA9; // LDA #$FF
        memory[0x8005] = 0xFF;
        memory[0x8006] = 0xEA;
        memory[0x0900] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var irq = new InterruptLine(InterruptType.Irq);
        var cpu = new Mos6502(bus);
        var clock = new SystemClock(985_248, cpu, irq);
        clock.Register(cpu);
        cpu.Reset();
        cpu.P = 0x24; // I set so LDA last CLK does not dispatch
        cpu.S = 0xF3;
        cpu.PC = 0x8000;
        irq.Assert(new RtsTyaIrqSource());

        for (var i = 0; i < 24 && cpu.DebugOpcode != 0xF0; i++)
            clock.Step();
        Assert.Equal((byte)0xF0, cpu.DebugOpcode);
        cpu.P = (byte)(cpu.P & ~0x04);

        for (var i = 0; i < 8 && cpu.DebugCycle != 0; i++)
            clock.Step();

        Assert.Equal((byte)0xF0, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((ushort)0x8004, cpu.PC);
        Assert.Equal((byte)0xF3, cpu.S);
        Assert.True(
            cpu.DebugInterruptSequenceRemaining is 0 or 6,
            $"JUMP tick must not push; irqSeq={cpu.DebugInterruptSequenceRemaining}");
        Assert.Equal((byte)0x00, cpu.A);

        byte sAfter = cpu.S;
        for (var i = 0; i < 8; i++)
        {
            clock.Step();
            sAfter = cpu.S;
            if (sAfter < 0xF3)
                break;
        }

        Assert.True(sAfter < 0xF3, $"IRQ did not push; S=${sAfter:X2}");
        Assert.Equal((byte)0x00, cpu.A);
        Assert.NotEqual((byte)0xA9, cpu.DebugOpcode);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001, TEST: TEST-X64SC-LOCKSTEP-001.
    /// Use case: Wolf64 sample 14954. In the STA (zp),Y / INY / taken BNE
    /// loop, the IRQ delay becomes eligible on the branch's final dummy clock.
    /// Native exports the fall-through PC on that clock, then performs the
    /// no-clock JUMP before the following IRQ dummy cycle.
    /// Acceptance: IRQ may arm with six sequence cycles remaining, but the
    /// branch final-clock sample keeps the fall-through PC and stack pointer.
    /// </summary>
    [Fact]
    public void HeldStaIndYInyTakenBne_IrqArmedOnFinalClock_KeepsFallthroughVisible()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0xFFFE] = 0x00;
        memory[0xFFFF] = 0x09;
        memory[0x0010] = 0x00;
        memory[0x0011] = 0x90;
        memory[0x8000] = 0x91; // STA ($10),Y
        memory[0x8001] = 0x10;
        memory[0x8002] = 0xC8; // INY
        memory[0x8003] = 0xD0; // BNE $8000
        memory[0x8004] = 0xFB;
        memory[0x8005] = 0xEA;
        memory[0x0900] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var irq = new InterruptLine(InterruptType.Irq);
        var cpu = new Mos6502(bus)
        {
            P = 0x20,
            S = 0xF2,
            Y = 0
        };
        var clock = new SystemClock(985_248, cpu, irq);
        clock.Register(cpu);
        cpu.Reset();
        cpu.P = 0x20;
        cpu.S = 0xF2;
        cpu.Y = 0;
        cpu.PC = 0x8000;

        for (var i = 0; i < 64 && !(cpu.Y == 1 && cpu.DebugOpcode == 0x91 && cpu.DebugCycle == 0); i++)
            clock.Step();

        Assert.Equal((byte)0x91, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((byte)1, cpu.Y);
        irq.Assert(new RtsTyaIrqSource());

        for (var i = 0; i < 5; i++)
            clock.Step();

        Assert.Equal((byte)0xD0, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal(6, cpu.DebugInterruptSequenceRemaining);
        Assert.Equal((ushort)0x8005, cpu.PC);
        Assert.Equal((byte)0xF2, cpu.S);

        clock.Step();

        Assert.Equal(6, cpu.DebugInterruptSequenceRemaining);
        Assert.Equal((ushort)0x8000, cpu.PC);
        Assert.Equal((byte)0xF2, cpu.S);

        clock.Step();

        Assert.Equal(5, cpu.DebugInterruptSequenceRemaining);
        Assert.Equal((ushort)0x8000, cpu.PC);
        Assert.Equal((byte)0xF2, cpu.S);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001, TEST: TEST-X64SC-LOCKSTEP-001.
    /// Use case: Wolf64 sample 14977. After the branch-delayed IRQ sequence,
    ///   the handler PHA completes and VICE LDA zp GET_ZERO exposes the loaded
    ///   accumulator on DebugCycle 1. Managed retained the pre-load value.
    /// Acceptance: The data-read checkpoint after handler PHA has A=$35 and
    ///   records PHA as the immediately preceding opcode.
    /// </summary>
    [Fact]
    public void IrqHandlerPhaThenLdaZp_DataRead_ShowsLoadedA()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0xFFFE] = 0x00;
        memory[0xFFFF] = 0x09;
        memory[0x0010] = 0x00;
        memory[0x0011] = 0x90;
        memory[0x0012] = 0x35;
        memory[0x8000] = 0x91; // STA ($10),Y
        memory[0x8001] = 0x10;
        memory[0x8002] = 0xC8; // INY
        memory[0x8003] = 0xD0; // BNE $8000
        memory[0x8004] = 0xFB;
        memory[0x8005] = 0xEA;
        memory[0x0900] = 0x48; // PHA
        memory[0x0901] = 0xA5; // LDA $12
        memory[0x0902] = 0x12;
        memory[0x0903] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var irq = new InterruptLine(InterruptType.Irq);
        var cpu = new Mos6502(bus)
        {
            A = 0x20,
            P = 0x20,
            S = 0xF2,
            Y = 0
        };
        var clock = new SystemClock(985_248, cpu, irq);
        clock.Register(cpu);
        cpu.Reset();
        cpu.A = 0x20;
        cpu.P = 0x20;
        cpu.S = 0xF2;
        cpu.Y = 0;
        cpu.PC = 0x8000;

        for (var i = 0; i < 64 && !(cpu.Y == 1 && cpu.DebugOpcode == 0x91 && cpu.DebugCycle == 0); i++)
            clock.Step();

        Assert.Equal((byte)0x91, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        irq.Assert(new RtsTyaIrqSource());

        for (var i = 0; i < 32 && !(cpu.DebugOpcode == 0xA5 && cpu.DebugCycle == 1); i++)
            clock.Step();

        Assert.Equal((byte)0xA5, cpu.DebugOpcode);
        Assert.Equal(1, cpu.DebugCycle);
        Assert.Equal((byte)0x48, cpu.DebugPreviousOpcode);
        Assert.Equal((byte)0x35, cpu.A);
        cpu.P |= 0x80;
        Assert.NotEqual(0, cpu.P & 0x80);

        clock.Step();

        Assert.Equal((byte)0xA5, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((ushort)0x0903, cpu.PC);
        Assert.Equal((byte)0x35, cpu.A);
        Assert.Equal(0, cpu.P & 0x80);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001, TEST: TEST-X64SC-LOCKSTEP-001.
    /// Use case: Wolf64 sample 14997. The IRQ handler's not-taken BEQ is
    ///   followed by JSR while VIC mandatory BA is active. VICE has already
    ///   reached INC_PC plus PUSH high, a write cycle that must proceed.
    /// Acceptance: At JSR DebugCycle 4 the mandatory-steal predicate is false;
    ///   the next clock exports opcode+2 and decrements S.
    /// </summary>
    [Fact]
    public void IrqHandlerNotTakenBeqThenJsr_PushCycle_ProceedsDuringMandatoryBa()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0xFFFE] = 0x00;
        memory[0xFFFF] = 0x09;
        memory[0x0010] = 0x00;
        memory[0x0011] = 0x90;
        memory[0x0012] = 0x35;
        memory[0x8000] = 0x91; // STA ($10),Y
        memory[0x8001] = 0x10;
        memory[0x8002] = 0xC8; // INY
        memory[0x8003] = 0xD0; // BNE $8000
        memory[0x8004] = 0xFB;
        memory[0x8005] = 0xEA;
        memory[0x0900] = 0x48; // PHA
        memory[0x0901] = 0xA5; // LDA $12
        memory[0x0902] = 0x12;
        memory[0x0903] = 0x48; // PHA
        memory[0x0904] = 0xA9; // LDA #$35
        memory[0x0905] = 0x35;
        memory[0x0906] = 0x85; // STA $13
        memory[0x0907] = 0x13;
        memory[0x0908] = 0xAD; // LDA $9100
        memory[0x0909] = 0x00;
        memory[0x090A] = 0x91;
        memory[0x090B] = 0x29; // AND #$01
        memory[0x090C] = 0x01;
        memory[0x090D] = 0xF0; // BEQ $0910, not taken
        memory[0x090E] = 0x01;
        memory[0x090F] = 0x20; // JSR $0A00
        memory[0x0910] = 0x00;
        memory[0x0911] = 0x0A;
        memory[0x0014] = 0xFF;
        memory[0x0A00] = 0xA5; // LDA $14
        memory[0x0A01] = 0x14;
        memory[0x0A02] = 0x30; // BMI $0A06
        memory[0x0A03] = 0x02;
        memory[0x0A04] = 0xEA;
        memory[0x0A06] = 0x60; // RTS
        memory[0x9100] = 0x81;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var irq = new InterruptLine(InterruptType.Irq);
        var cpu = new Mos6502(bus)
        {
            A = 0x20,
            P = 0x20,
            S = 0xF2,
            Y = 0
        };
        var stealer = new ClockBaStealer();
        var clock = new SystemClock(985_248, cpu, irq);
        clock.Register(cpu);
        clock.Register(stealer);
        cpu.Reset();
        cpu.A = 0x20;
        cpu.P = 0x20;
        cpu.S = 0xF2;
        cpu.Y = 0;
        cpu.PC = 0x8000;

        for (var i = 0; i < 64 && !(cpu.Y == 1 && cpu.DebugOpcode == 0x91 && cpu.DebugCycle == 0); i++)
            clock.Step();
        irq.Assert(new RtsTyaIrqSource());

        for (var i = 0; i < 128 && !(cpu.DebugOpcode == 0x20 && cpu.DebugCycle == 4); i++)
            clock.Step();

        Assert.Equal((byte)0x20, cpu.DebugOpcode);
        Assert.Equal(4, cpu.DebugCycle);
        Assert.Equal((byte)0xF0, cpu.DebugPreviousOpcode);
        Assert.False(cpu.CanStealCurrentCycle);
        Assert.False(cpu.CanForceStealCurrentCycle);
        var sBeforePush = cpu.S;

        stealer.IsCpuCycleStolen = true;
        stealer.IsCpuCycleStealMandatory = true;
        clock.Step();

        Assert.Equal((byte)0x20, cpu.DebugOpcode);
        Assert.Equal(3, cpu.DebugCycle);
        Assert.Equal((ushort)0x0911, cpu.PC);
        Assert.Equal((byte)(sBeforePush - 1), cpu.S);

        stealer.IsCpuCycleStolen = false;
        stealer.IsCpuCycleStealMandatory = false;
        for (var i = 0; i < 32 && !(cpu.DebugOpcode == 0x30 && cpu.DebugCycle == 2); i++)
            clock.Step();

        Assert.Equal((byte)0x30, cpu.DebugOpcode);
        Assert.Equal(2, cpu.DebugCycle);
        Assert.False(cpu.CanStealCurrentCycle);
        Assert.False(cpu.CanForceStealCurrentCycle);

        stealer.IsCpuCycleStolen = true;
        stealer.IsCpuCycleStealMandatory = true;
        clock.Step();

        Assert.Equal((byte)0x30, cpu.DebugOpcode);
        Assert.Equal(1, cpu.DebugCycle);
        Assert.True(
            cpu.PC == 0x0A04,
            $"BMI dummy PC=${cpu.PC:X4}; trail=${cpu.DebugPriorTrailingAtNextPc} staged=${cpu.DebugTakenBranchStagedFallthrough}");

        stealer.IsCpuCycleStolen = false;
        stealer.IsCpuCycleStealMandatory = false;
        clock.Step();

        Assert.Equal((byte)0x30, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((ushort)0x0A04, cpu.PC);
        Assert.True(cpu.DebugBranchTargetFetchPending);

        for (var i = 0; i < 8 && !(cpu.DebugOpcode == 0x60 && cpu.DebugCycle == 4); i++)
            clock.Step();

        Assert.Equal((byte)0x60, cpu.DebugOpcode);
        Assert.Equal(4, cpu.DebugCycle);
        var sBeforeRtsPeek = cpu.S;
        clock.Step();

        Assert.Equal((byte)0x60, cpu.DebugOpcode);
        Assert.Equal(3, cpu.DebugCycle);
        Assert.Equal(sBeforeRtsPeek, cpu.S);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 sample 2175368. After taken same-page BEQ, VICE LDA zp
    ///   GET_ZERO last CLK is not DO_INTERRUPT; the next loop iteration samples
    ///   IRQ before STA FETCH (nPC=$E5CF nS=$F2 nLastOp=$A5). Managed either
    ///   IRQed on the LDA last CLK (2208208-shaped) or skipped STA's IRQ sample.
    /// Acceptance: LDA #$00 / BEQ taken / LDA zp / STA zp. IRQ is asserted on
    ///   the BEQ JUMP tick (fresh irq_clk, DELAYS +1 still unelapsed) so LDA zp
    ///   still runs. LDA zp last CLK does not start IRQ; STA zp does not
    ///   execute; IRQ pushes on the next tick.
    /// </summary>
    [Fact]
    public void TakenSamePageBeqThenLdaZp_LastClk_DoesNotPushIrqBeforeSta()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0xFFFE] = 0x00;
        memory[0xFFFF] = 0x09;
        memory[0x0010] = 0x00;
        memory[0x8000] = 0xA9; // LDA #$00 (Z=1)
        memory[0x8001] = 0x00;
        memory[0x8002] = 0xF0; // BEQ $8004 (taken, same page)
        memory[0x8003] = 0x00;
        memory[0x8004] = 0xA5; // LDA $10
        memory[0x8005] = 0x10;
        memory[0x8006] = 0x85; // STA $11
        memory[0x8007] = 0x11;
        memory[0x8008] = 0xEA;
        memory[0x0900] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var irq = new InterruptLine(InterruptType.Irq);
        var cpu = new Mos6502(bus);
        var clock = new SystemClock(985_248, cpu, irq);
        clock.Register(cpu);
        cpu.Reset();
        cpu.P = 0x24;
        cpu.S = 0xF3;
        cpu.PC = 0x8000;

        for (var i = 0; i < 24 && cpu.DebugOpcode != 0xF0; i++)
            clock.Step();
        Assert.Equal((byte)0xF0, cpu.DebugOpcode);
        cpu.P = (byte)(cpu.P & ~0x04);

        for (var i = 0; i < 8 && cpu.DebugCycle != 0; i++)
            clock.Step();
        Assert.Equal((byte)0xF0, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        irq.Assert(new RtsTyaIrqSource());

        for (var i = 0; i < 16 && cpu.DebugOpcode != 0xA5; i++)
            clock.Step();
        Assert.Equal((byte)0xA5, cpu.DebugOpcode);

        while (cpu.DebugOpcode == 0xA5 && cpu.DebugCycle != 0)
            clock.Step();

        Assert.Equal((byte)0xA5, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((byte)0xF3, cpu.S);
        Assert.Equal(0, cpu.DebugInterruptSequenceRemaining);

        byte sAfter = cpu.S;
        for (var i = 0; i < 8; i++)
        {
            clock.Step();
            sAfter = cpu.S;
            if (sAfter < 0xF3)
                break;
        }

        Assert.True(sAfter < 0xF3, $"IRQ did not push; S=${sAfter:X2}");
        Assert.Equal(0x00, memory[0x11]);
        Assert.NotEqual((byte)0x85, cpu.DebugOpcode);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 sample 2208208. x64sc <c>irq_delay_cycles</c> is still
    ///   below DELAYS+INTERRUPT_DELAY (2+1) at DO_INTERRUPT before STA FETCH
    ///   (nPC=$E5D1 nS=$F3 nLastOp=$85 nIrqClk=2208210). Managed treated the
    ///   pre-FETCH sample as elapsed and pushed (mS=$F2 irqSeq=4).
    /// Acceptance: Same BEQ/LDA zp/STA zp chain, IRQ asserted at LDA FETCH:
    ///   STA zp still executes and S is unchanged through that STA.
    /// </summary>
    [Fact]
    public void TakenSamePageBeqThenLdaZp_FreshIrq_StaZpStillRuns()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0xFFFE] = 0x00;
        memory[0xFFFF] = 0x09;
        memory[0x0010] = 0x00;
        memory[0x8000] = 0xA9; // LDA #$00
        memory[0x8001] = 0x00;
        memory[0x8002] = 0xF0; // BEQ taken
        memory[0x8003] = 0x00;
        memory[0x8004] = 0xA5; // LDA $10
        memory[0x8005] = 0x10;
        memory[0x8006] = 0x85; // STA $11
        memory[0x8007] = 0x11;
        memory[0x8008] = 0xEA;
        memory[0x0900] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var irq = new InterruptLine(InterruptType.Irq);
        var cpu = new Mos6502(bus);
        var clock = new SystemClock(985_248, cpu, irq);
        clock.Register(cpu);
        cpu.Reset();
        cpu.P = 0x20;
        cpu.S = 0xF3;
        cpu.PC = 0x8000;

        for (var i = 0; i < 32 && cpu.DebugOpcode != 0xA5; i++)
            clock.Step();
        Assert.Equal((byte)0xA5, cpu.DebugOpcode);
        while (cpu.DebugOpcode == 0xA5 && cpu.DebugCycle != 0)
            clock.Step();
        Assert.Equal(0, cpu.DebugCycle);
        irq.Assert(new RtsTyaIrqSource());

        byte? staOpcode = null;
        for (var i = 0; i < 12; i++)
        {
            clock.Step();
            if (cpu.DebugOpcode == 0x85)
            {
                staOpcode = 0x85;
                break;
            }

            if (cpu.S < 0xF3)
                break;
        }

        Assert.Equal((byte)0x85, staOpcode);
        Assert.Equal((byte)0xF3, cpu.S);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 sample 2208208 root cause. VICE
    ///   <c>interrupt_check_irq_delay</c> (mainc64cpu.c) treats
    ///   <c>OPCODE_DELAYS_INTERRUPT</c> as +1 on the threshold (need
    ///   <c>irq_delay_cycles &gt;= 3</c>), not skip-all. After a taken
    ///   same-page BEQ, <c>DO_INTERRUPT</c> before target FETCH still
    ///   dispatches when delay is already elapsed. Managed blanket-skipped
    ///   that IRQ, so the KERNAL CIA handler never ran <c>LDA $DC0D</c>
    ///   and ICR never acked (stale irq_clk, then a false IRQ at STA zp).
    /// Acceptance: LDA #$00 / BEQ taken / LDA zp with IRQ pending and I
    ///   cleared after BEQ FETCH: IRQ pushes before LDA zp executes.
    /// </summary>
    [Fact]
    public void TakenSamePageBeq_ElapsedIrq_DispatchesBeforeTargetFetch()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0xFFFE] = 0x00;
        memory[0xFFFF] = 0x09;
        memory[0x0010] = 0x00;
        memory[0x8000] = 0xA9; // LDA #$00 (Z=1)
        memory[0x8001] = 0x00;
        memory[0x8002] = 0xF0; // BEQ $8004 (taken, same page)
        memory[0x8003] = 0x00;
        memory[0x8004] = 0xA5; // LDA $10
        memory[0x8005] = 0x10;
        memory[0x8006] = 0x85; // STA $11
        memory[0x8007] = 0x11;
        memory[0x8008] = 0xEA;
        memory[0x0900] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var irq = new InterruptLine(InterruptType.Irq);
        var cpu = new Mos6502(bus);
        var clock = new SystemClock(985_248, cpu, irq);
        clock.Register(cpu);
        cpu.Reset();
        cpu.P = 0x24;
        cpu.S = 0xF3;
        cpu.PC = 0x8000;
        irq.Assert(new RtsTyaIrqSource());

        for (var i = 0; i < 24 && cpu.DebugOpcode != 0xF0; i++)
            clock.Step();
        Assert.Equal((byte)0xF0, cpu.DebugOpcode);
        cpu.P = (byte)(cpu.P & ~0x04);

        byte sAfter = cpu.S;
        for (var i = 0; i < 16; i++)
        {
            clock.Step();
            sAfter = cpu.S;
            if (sAfter < 0xF3)
                break;
        }

        Assert.True(sAfter < 0xF3, $"IRQ did not push before LDA zp; S=${sAfter:X2} op=${cpu.DebugOpcode:X2}");
        Assert.NotEqual((byte)0xA5, cpu.DebugOpcode);
        Assert.Equal(0x00, memory[0x11]);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 sample 2175366. irq_clk latches on STA abs last CLK
    ///   (<c>$8D</c> at <c>$E5D1</c>, mIrqClk=2175360, nIrqClk=2175366 with a
    ///   6-clock pipeline epoch). VICE JUMP has no <c>CLK_INC</c>, so
    ///   <c>irq_delay_cycles</c> is still 2 at <c>DO_INTERRUPT</c> before LDA
    ///   FETCH (DELAYS needs 3). Native FETCHes LDA (<c>nLastOp=$A5</c>
    ///   <c>nS=$F3</c>). Counting the extra JUMP host tick as a CLK made
    ///   managed dispatch (irqSeq=6).
    /// Acceptance: STA abs last CLK asserts IRQ, then taken same-page BEQ:
    ///   LDA zp still FETCHes and S stays <c>$F3</c>.
    /// </summary>
    [Fact]
    public void TakenSamePageBeq_IrqRoseOnPriorStaLastClk_DoesNotDispatchBeforeLda()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0xFFFE] = 0x00;
        memory[0xFFFF] = 0x09;
        memory[0x0010] = 0x00;
        memory[0x8000] = 0xA9; // LDA #$00 (Z=1)
        memory[0x8001] = 0x00;
        memory[0x8002] = 0x8D; // STA $0400
        memory[0x8003] = 0x00;
        memory[0x8004] = 0x04;
        memory[0x8005] = 0xF0; // BEQ $8007 (taken, same page)
        memory[0x8006] = 0x00;
        memory[0x8007] = 0xA5; // LDA $10
        memory[0x8008] = 0x10;
        memory[0x8009] = 0xEA;
        memory[0x0900] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var irq = new InterruptLine(InterruptType.Irq);
        var cpu = new Mos6502(bus);
        var clock = new SystemClock(985_248, cpu, irq);
        clock.Register(cpu);
        cpu.Reset();
        cpu.P = 0x20;
        cpu.S = 0xF3;
        cpu.PC = 0x8000;

        for (var i = 0; i < 24 && cpu.DebugOpcode != 0x8D; i++)
            clock.Step();
        Assert.Equal((byte)0x8D, cpu.DebugOpcode);
        while (cpu.DebugOpcode == 0x8D && cpu.DebugCycle > 1)
            clock.Step();
        Assert.Equal((byte)0x8D, cpu.DebugOpcode);
        Assert.Equal(1, cpu.DebugCycle);
        irq.Assert(new RtsTyaIrqSource());
        clock.Step();
        Assert.Equal(0, cpu.DebugCycle);

        for (var i = 0; i < 16 && cpu.DebugOpcode != 0xA5; i++)
            clock.Step();

        Assert.Equal((byte)0xA5, cpu.DebugOpcode);
        Assert.Equal((byte)0xF3, cpu.S);
        Assert.Equal(0, cpu.DebugInterruptSequenceRemaining);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 sample 2865090. CIA latches during STA abs STORE
    ///   (cycle 1, mIrqClk=2865084, RasterX 39). VICE irq_clk is set after
    ///   that CLK_INC's interrupt_delay, then BEQ FETCH two CLK_INC plus
    ///   dummy reach delay 3 at DO_INTERRUPT (nS=$F2 nLastOp=$1F0). Distinct
    ///   from 2175366, which latched on the apply tick (cycle 0) so delay
    ///   stayed 2 and LDA ran. Do not count the latch CLK itself (2109675)
    ///   and do not double BEQ FETCH (2175366).
    /// Acceptance: LDA #$00 / STA abs / BEQ / LDA zp. IRQ asserted on STA
    ///   abs cycle 2 so the next tick is the STORE CLK: apply tick counts
    ///   that STORE for irq_delay, then IRQ pushes before LDA zp FETCHes.
    /// </summary>
    [Fact]
    public void StaAbsStoreClkLatch_IrqDispatchesBeforeFollowingLda()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0xFFFE] = 0x00;
        memory[0xFFFF] = 0x09;
        memory[0x0010] = 0x00;
        memory[0x8000] = 0xA9; // LDA #$00 (Z=1)
        memory[0x8001] = 0x00;
        memory[0x8002] = 0x8D; // STA $0400
        memory[0x8003] = 0x00;
        memory[0x8004] = 0x04;
        memory[0x8005] = 0xF0; // BEQ $8007 (taken, same page)
        memory[0x8006] = 0x00;
        memory[0x8007] = 0xA5; // LDA $10
        memory[0x8008] = 0x10;
        memory[0x8009] = 0xEA;
        memory[0x0900] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var irq = new InterruptLine(InterruptType.Irq);
        var cpu = new Mos6502(bus);
        var clock = new SystemClock(985_248, cpu, irq);
        clock.Register(cpu);
        cpu.Reset();
        cpu.P = 0x20;
        cpu.S = 0xF3;
        cpu.PC = 0x8000;

        for (var i = 0; i < 24 && cpu.DebugOpcode != 0x8D; i++)
            clock.Step();
        Assert.Equal((byte)0x8D, cpu.DebugOpcode);
        while (cpu.DebugOpcode == 0x8D && cpu.DebugCycle > 2)
            clock.Step();
        Assert.Equal((byte)0x8D, cpu.DebugOpcode);
        Assert.Equal(2, cpu.DebugCycle);
        irq.Assert(new RtsTyaIrqSource());
        clock.Step();
        Assert.Equal(1, cpu.DebugCycle);
        var delayAfterStore = clock.DebugIrqDelayCycles;
        clock.Step();
        Assert.Equal(0, cpu.DebugCycle);
        var delayAfterApply = clock.DebugIrqDelayCycles;

        byte sAfter = cpu.S;
        for (var i = 0; i < 16; i++)
        {
            clock.Step();
            sAfter = cpu.S;
            if (sAfter < 0xF3)
                break;
        }

        Assert.True(
            delayAfterStore == 0,
            $"STORE latch tick counted irq_delay={delayAfterStore} (VICE sets irq_clk after interrupt_delay; 2109675)");
        Assert.True(
            delayAfterApply == 1,
            $"apply after STORE latch irq_delay={delayAfterApply} (want 1: count the STORE CLK on the apply host tick)");
        Assert.True(sAfter < 0xF3, $"IRQ did not push before LDA zp; S=${sAfter:X2} op=${cpu.DebugOpcode:X2} delayAfterApply={delayAfterApply}");
        Assert.NotEqual((byte)0xA5, cpu.DebugOpcode);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 sample 2224630. irq_clk latches on STA abs cycle 2
    ///   (mIrqClk=2224626). VICE ST is INC_PC (no CLK) then SET_ABS STORE
    ///   CLK_INC; the extra managed cycle-0 apply after the cycle-1 write is
    ///   not a CLK_INC. Counting it made delay&gt;=2 at BEQ FETCH (mS=$F2
    ///   irqSeq) while native dummy-exported BEQ (nPC=$E5D6 nLastOp=$F0
    ///   nS=$F3).
    /// Acceptance: LDA zp / STA zp / STA abs / BEQ taken. IRQ asserted on
    ///   STA abs cycle 2: BEQ still FETCHes and S stays $F3.
    /// </summary>
    [Fact]
    public void StaAbs_IrqRoseOnCycle2_DoesNotDispatchBeforeFollowingBeq()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0xFFFE] = 0x00;
        memory[0xFFFF] = 0x09;
        memory[0x0010] = 0x00;
        memory[0x8000] = 0xA5; // LDA $10 (Z=1)
        memory[0x8001] = 0x10;
        memory[0x8002] = 0x85; // STA $11
        memory[0x8003] = 0x11;
        memory[0x8004] = 0x8D; // STA $0400
        memory[0x8005] = 0x00;
        memory[0x8006] = 0x04;
        memory[0x8007] = 0xF0; // BEQ $8009 (taken, same page)
        memory[0x8008] = 0x00;
        memory[0x8009] = 0xA5; // LDA $10
        memory[0x800A] = 0x10;
        memory[0x800B] = 0xEA;
        memory[0x0900] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var irq = new InterruptLine(InterruptType.Irq);
        var cpu = new Mos6502(bus);
        var clock = new SystemClock(985_248, cpu, irq);
        clock.Register(cpu);
        cpu.Reset();
        cpu.P = 0x20;
        cpu.S = 0xF3;
        cpu.PC = 0x8000;

        for (var i = 0; i < 40 && cpu.DebugOpcode != 0x8D; i++)
            clock.Step();
        Assert.Equal((byte)0x8D, cpu.DebugOpcode);
        while (cpu.DebugOpcode == 0x8D && cpu.DebugCycle > 3)
            clock.Step();
        Assert.Equal((byte)0x8D, cpu.DebugOpcode);
        Assert.Equal(3, cpu.DebugCycle);
        irq.Assert(new RtsTyaIrqSource());
        clock.Step();
        Assert.Equal(2, cpu.DebugCycle);

        byte? beqOpcode = null;
        for (var i = 0; i < 16; i++)
        {
            clock.Step();
            if (cpu.DebugOpcode == 0xF0)
            {
                beqOpcode = 0xF0;
                break;
            }

            if (cpu.S < 0xF3)
                break;
        }

        Assert.Equal((byte)0xF0, beqOpcode);
        Assert.Equal((byte)0xF3, cpu.S);
        Assert.Equal(0, cpu.DebugInterruptSequenceRemaining);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 sample 2520240. STA abs at $E5D4 on a badline,
    ///   first BA-high cycle (RasterX 55) is VICE SET_ABS STORE CLK_INC.
    ///   DO_INTERRUPT is the next loop before the following FETCH. Elapsed
    ///   IRQ on that STORE CLK started irqSeq (mS=$F2) while native still
    ///   ran STA (nPC=$E5D6 nS=$F3 nLastOp=$F0).
    /// Acceptance: LDA #$00 / STA abs, IRQ asserted at DebugCycle 1, 15
    ///   BA-stolen clocks, then unstall. STA last CLK keeps S=$F3 and
    ///   irqSeq=0. The following tick may start IRQ before the next
    ///   instruction executes.
    /// </summary>
    [Fact]
    public void StaAbs_ElapsedIrq_LastClkDoesNotDispatch()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0xFFFE] = 0x00;
        memory[0xFFFF] = 0x09;
        memory[0x8000] = 0xA9; // LDA #$00
        memory[0x8001] = 0x00;
        memory[0x8002] = 0x8D; // STA $0400
        memory[0x8003] = 0x00;
        memory[0x8004] = 0x04;
        memory[0x8005] = 0xA5; // LDA $10
        memory[0x8006] = 0x10;
        memory[0x8007] = 0xEA;
        memory[0x0010] = 0x7F;
        memory[0x0900] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var irq = new InterruptLine(InterruptType.Irq);
        var cpu = new Mos6502(bus);
        var clock = new SystemClock(985_248, cpu, irq);
        clock.Register(cpu);
        cpu.Reset();
        cpu.P = 0x20;
        cpu.S = 0xF3;
        cpu.PC = 0x8000;

        var stealer = new ClockBaStealer();
        clock.Register(stealer);

        for (var i = 0; i < 40 && cpu.DebugOpcode != 0x8D; i++)
            clock.Step();
        Assert.Equal((byte)0x8D, cpu.DebugOpcode);
        while (cpu.DebugOpcode == 0x8D && cpu.DebugCycle > 1)
            clock.Step();
        Assert.Equal((byte)0x8D, cpu.DebugOpcode);
        Assert.Equal(1, cpu.DebugCycle);
        irq.Assert(new RtsTyaIrqSource());
        stealer.IsCpuCycleStolen = true;
        for (var steal = 0; steal < 15; steal++)
            clock.Step();
        stealer.IsCpuCycleStolen = false;
        clock.Step();

        Assert.Equal((byte)0x8D, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((byte)0xF3, cpu.S);
        Assert.Equal(0, cpu.DebugInterruptSequenceRemaining);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 sample 2142523. Unstolen STA abs last CLK is not
    ///   DO_INTERRUPT. Elapsed IRQ is sampled on the next tick before the
    ///   following FETCH (nS=$F2 nLastOp=$8D). Skipping that sample FETCHed
    ///   wait-loop BEQ.
    /// Acceptance: LDA #$00 / STA abs. IRQ asserted at DebugCycle 3. Last
    ///   CLK keeps S=$F3. Next tick starts IRQ (S drops) before LDA zp.
    /// </summary>
    [Fact]
    public void StaAbs_UnstolenElapsedIrq_DispatchesBeforeFollowingFetch()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0xFFFE] = 0x00;
        memory[0xFFFF] = 0x09;
        memory[0x8000] = 0xA9; // LDA #$00
        memory[0x8001] = 0x00;
        memory[0x8002] = 0x8D; // STA $0400
        memory[0x8003] = 0x00;
        memory[0x8004] = 0x04;
        memory[0x8005] = 0xA5; // LDA $10
        memory[0x8006] = 0x10;
        memory[0x0900] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var irq = new InterruptLine(InterruptType.Irq);
        var cpu = new Mos6502(bus);
        var clock = new SystemClock(985_248, cpu, irq);
        clock.Register(cpu);
        cpu.Reset();
        cpu.P = 0x20;
        cpu.S = 0xF3;
        cpu.PC = 0x8000;

        AdvanceToOpcode(cpu, 0xA9);
        while (cpu.DebugOpcode == 0xA9 && cpu.DebugCycle != 0)
            clock.Step();
        irq.Assert(new RtsTyaIrqSource());
        for (var i = 0; i < 24 && !(cpu.DebugOpcode == 0x8D && cpu.DebugCycle == 0); i++)
            clock.Step();
        Assert.Equal((byte)0x8D, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((byte)0xF3, cpu.S);
        Assert.Equal(0, cpu.DebugInterruptSequenceRemaining);

        clock.Step();
        Assert.True(cpu.S < 0xF3 || cpu.DebugInterruptSequenceRemaining > 0,
            $"unstolen STA abs elapsed IRQ must dispatch before LDA FETCH; S=${cpu.S:X2} irqSeq={cpu.DebugInterruptSequenceRemaining}");
        Assert.NotEqual((byte)0xA5, cpu.DebugOpcode);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 sample 2520240. STA abs write cycle stolen on a
    ///   badline. After unstall, native dummy-exports the following BEQ
    ///   (nPC=$E5D6 nS=$F3 nLastOp=$F0). Sampling IRQ as in 2142523 started
    ///   irqSeq (mS=$F2). DebugCycle 1 stays the write sample.
    /// Acceptance: LDA #$00 / STA abs / BEQ taken. IRQ asserted at STA
    ///   DebugCycle 1, 15 BA-stolen clocks, unstall last CLK S=$F3.
    ///   Next tick FETCHes BEQ. Dummy CLK is fall-through, S=$F3.
    /// </summary>
    [Fact]
    public void StaAbs_StolenWrite_FollowingBeqDummy_DoesNotDispatchIrq()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0xFFFE] = 0x00;
        memory[0xFFFF] = 0x09;
        memory[0x8000] = 0xA9; // LDA #$00 (Z=1)
        memory[0x8001] = 0x00;
        memory[0x8002] = 0x8D; // STA $0400
        memory[0x8003] = 0x00;
        memory[0x8004] = 0x04;
        memory[0x8005] = 0xF0; // BEQ $8007
        memory[0x8006] = 0x00;
        memory[0x8007] = 0xA5; // LDA $10
        memory[0x8008] = 0x10;
        memory[0x0900] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var irq = new InterruptLine(InterruptType.Irq);
        var cpu = new Mos6502(bus);
        var stealer = new ClockBaStealer();
        var clock = new SystemClock(985_248, cpu, irq);
        clock.Register(cpu);
        clock.Register(stealer);
        cpu.Reset();
        cpu.P = 0x20;
        cpu.S = 0xF3;
        cpu.PC = 0x8000;

        for (var i = 0; i < 40 && cpu.DebugOpcode != 0x8D; i++)
            clock.Step();
        while (cpu.DebugOpcode == 0x8D && cpu.DebugCycle > 1)
            clock.Step();
        Assert.Equal((byte)0x8D, cpu.DebugOpcode);
        Assert.Equal(1, cpu.DebugCycle);
        irq.Assert(new RtsTyaIrqSource());
        stealer.IsCpuCycleStolen = true;
        for (var steal = 0; steal < 15; steal++)
            clock.Step();
        stealer.IsCpuCycleStolen = false;
        clock.Step();
        Assert.Equal((byte)0x8D, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((byte)0xF3, cpu.S);
        Assert.Equal(0, cpu.DebugInterruptSequenceRemaining);

        clock.Step();
        Assert.Equal((byte)0xF0, cpu.DebugOpcode);
        Assert.Equal((byte)0xF3, cpu.S);
        Assert.Equal(0, cpu.DebugInterruptSequenceRemaining);

        while (cpu.DebugOpcode == 0xF0 && cpu.DebugCycle != 0)
            clock.Step();
        Assert.Equal((byte)0xF0, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((ushort)0x8007, cpu.PC);
        Assert.Equal((byte)0xF3, cpu.S);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 sample 2520240. STA abs p2 FETCH is VICE
    ///   FETCH_OPCODE check_ba. RasterX 12 is conditional BA (CPU still
    ///   runs: 2145723 nPC advanced). RasterX 13-54 is mandatory BA, so
    ///   that FETCH must freeze. Managed CanSteal is false at DebugCycle 2
    ///   (store nextCycle==1), so the CPU ran p2 during the badline and
    ///   arrived at the write with native still FETCHing (nPC=$E5D6).
    /// Acceptance: STA abs DebugCycle 2: CanSteal is false and CanForceSteal
    ///   is true. Mandatory BA keeps DebugCycle 2. After unstall the next
    ///   two ticks are still STA (p2 then STORE) with S unchanged.
    /// </summary>
    [Fact]
    public void StaAbs_P2Fetch_MandatoryBaFreezes_UnstallStillHasTwoClocks()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x8000] = 0xA9; // LDA #$00
        memory[0x8001] = 0x00;
        memory[0x8002] = 0x8D; // STA $0400
        memory[0x8003] = 0x00;
        memory[0x8004] = 0x04;
        memory[0x8005] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        var stealer = new ClockBaStealer();
        var clock = new SystemClock();
        clock.Register(cpu);
        clock.Register(stealer);
        cpu.Reset();
        cpu.P = 0x20;
        cpu.S = 0xF3;
        cpu.PC = 0x8000;

        for (var i = 0; i < 40 && cpu.DebugOpcode != 0x8D; i++)
            clock.Step();
        Assert.Equal((byte)0x8D, cpu.DebugOpcode);
        while (cpu.DebugOpcode == 0x8D && cpu.DebugCycle > 2)
            clock.Step();

        Assert.Equal((byte)0x8D, cpu.DebugOpcode);
        Assert.Equal(2, cpu.DebugCycle);
        Assert.False(
            cpu.CanStealCurrentCycle,
            "RasterX 12 conditional BA must still run STA abs p2 (2145723)");
        Assert.True(
            cpu.CanForceStealCurrentCycle,
            "RasterX 13-54 mandatory BA must freeze STA abs p2 FETCH");

        stealer.IsCpuCycleStolen = true;
        stealer.IsCpuCycleStealMandatory = true;
        for (var steal = 0; steal < 15; steal++)
            clock.Step();
        Assert.Equal((byte)0x8D, cpu.DebugOpcode);
        Assert.Equal(2, cpu.DebugCycle);
        Assert.Equal((ushort)0x8002, cpu.PC);
        Assert.Equal((byte)0xF3, cpu.S);

        stealer.IsCpuCycleStolen = false;
        stealer.IsCpuCycleStealMandatory = false;
        clock.Step();
        Assert.Equal((byte)0x8D, cpu.DebugOpcode);
        Assert.Equal(1, cpu.DebugCycle);
        Assert.Equal((byte)0xF3, cpu.S);

        clock.Step();
        Assert.Equal((byte)0x8D, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((byte)0xF3, cpu.S);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 sample 2323177. BEQ FETCH stalled on a badline
    ///   (cycle 2, x=43..54). VICE <c>interrupt_delay</c> still increments
    ///   on stolen CLK, so after unstall <c>irq_delay_cycles</c> is elapsed
    ///   and DO_INTERRUPT runs before LDA FETCH (nS=$F2 nLastOp=$F0).
    ///   Managed skipped delay increment on cpuSkipped, FETCHed LDA
    ///   (mS=$F3 opcode=$A5).
    /// Acceptance: IRQ asserted at BEQ FETCH, then 12 BA-stolen cycles:
    ///   after unstall IRQ pushes and LDA #$FF does not commit.
    /// </summary>
    [Fact]
    public void StolenTakenBeq_IrqPending_DispatchesBeforeTargetFetch()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0xFFFE] = 0x00;
        memory[0xFFFF] = 0x09;
        memory[0x8000] = 0xA9; // LDA #$00
        memory[0x8001] = 0x00;
        memory[0x8002] = 0xF0; // BEQ $8004
        memory[0x8003] = 0x00;
        memory[0x8004] = 0xA9; // LDA #$FF
        memory[0x8005] = 0xFF;
        memory[0x8006] = 0xEA;
        memory[0x0900] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var irq = new InterruptLine(InterruptType.Irq);
        var cpu = new Mos6502(bus);
        var stealer = new ClockBaStealer();
        var clock = new SystemClock(985_248, cpu, irq);
        clock.Register(cpu);
        clock.Register(stealer);
        cpu.Reset();
        cpu.P = 0x20;
        cpu.S = 0xF3;
        cpu.PC = 0x8000;

        for (var i = 0; i < 24 && cpu.DebugOpcode != 0xF0; i++)
            clock.Step();
        Assert.Equal((byte)0xF0, cpu.DebugOpcode);
        irq.Assert(new RtsTyaIrqSource());
        stealer.IsCpuCycleStolen = true;
        for (var steal = 0; steal < 12; steal++)
            clock.Step();
        stealer.IsCpuCycleStolen = false;

        byte sAfter = cpu.S;
        for (var i = 0; i < 16; i++)
        {
            clock.Step();
            sAfter = cpu.S;
            if (sAfter < 0xF3)
                break;
        }

        Assert.True(sAfter < 0xF3, $"IRQ did not push after steal; S=${sAfter:X2} op=${cpu.DebugOpcode:X2}");
        Assert.Equal((byte)0x00, cpu.A);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001, TEST: TEST-X64SC-LOCKSTEP-001.
    /// Upstream VICE performs both FETCH_OPCODE clocks before LDA immediate
    /// updates A. After STA (zp),Y and RTS, the second LDA fetch still exposes
    /// the opcode PC and pre-load A; the next tick commits A and fetches STA.
    /// </summary>
    [Fact]
    public void StaIndY_Rts_LdaImm_SecondFetchHoldsPreOpThenNextTickFetchesFollowingStaIndY()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x00D0] = 0x00;
        memory[0x00D1] = 0x04;
        memory[0x8000] = 0x20; // JSR $8100
        memory[0x8001] = 0x00;
        memory[0x8002] = 0x81;
        memory[0x8003] = 0xA9; // LDA #$20
        memory[0x8004] = 0x20;
        memory[0x8005] = 0x91; // STA ($D0),Y
        memory[0x8006] = 0xD0;
        memory[0x8100] = 0x91; // STA ($D0),Y
        memory[0x8101] = 0xD0;
        memory[0x8102] = 0x60; // RTS

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        cpu.Y = 0x03;
        cpu.A = 0x0E;

        for (var i = 0; i < 40 && !(cpu.DebugOpcode == 0xA9 && cpu.DebugCycle == 0 && cpu.PC == 0x8003); i++)
            cpu.Tick();

        Assert.Equal((byte)0xA9, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((byte)0x0E, cpu.A);
        Assert.Equal((ushort)0x8003, cpu.PC);

        cpu.Tick();

        Assert.Equal((byte)0x20, cpu.A);
        Assert.Equal((byte)0x91, cpu.DebugOpcode);
        Assert.Equal(5, cpu.DebugCycle);
        Assert.Equal(0, cpu.DebugPriorTrailingAtNextPc);
        Assert.Equal((ushort)0x8005, cpu.PC);

        cpu.Tick();

        Assert.Equal(4, cpu.DebugCycle);
        Assert.Equal((ushort)0x8005, cpu.PC);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001, TEST: TEST-X64SC-LOCKSTEP-001.
    /// Upstream VICE executes LD's register/flag update and INC_PC after the
    /// absolute-indexed data clock. With no exported next-PC checkpoint left
    /// behind, the following STA (zp),Y has consumed its overlapping first
    /// fetch and reaches ST's unclocked INC_PC(2) at cycle 4.
    /// </summary>
    [Fact]
    public void StaIndYAfterLdaAbsXWithoutTrailingCheckpoint_Cycle4AdvancesPc()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0xF0;
        memory[0xFFFD] = 0x80;
        memory[0x0010] = 0x00;
        memory[0x0011] = 0x04;
        memory[0x9007] = 0x2C;
        memory[0x9100] = 0x43;
        memory[0x80F0] = 0xA2; // LDX #$07
        memory[0x80F1] = 0x07;
        memory[0x80F2] = 0xA9; // LDA #$01
        memory[0x80F3] = 0x01;
        memory[0x80F4] = 0xD0; // BNE $8116
        memory[0x80F5] = 0x20;
        memory[0x8116] = 0xAD; // LDA $9100
        memory[0x8117] = 0x00;
        memory[0x8118] = 0x91;
        memory[0x8119] = 0xD0; // BNE $8120
        memory[0x811A] = 0x05;
        memory[0x8120] = 0xBD; // LDA $9000,X
        memory[0x8121] = 0x00;
        memory[0x8122] = 0x90;
        memory[0x8123] = 0x91; // STA ($10),Y
        memory[0x8124] = 0x10;
        memory[0x8125] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();

        for (var i = 0; i < 64 && !(cpu.DebugOpcode == 0x91 && cpu.DebugOpcodeAddress == 0x8123); i++)
            cpu.Tick();

        Assert.Equal((byte)0x91, cpu.DebugOpcode);
        Assert.Equal(5, cpu.DebugCycle);
        Assert.Equal((byte)0xBD, cpu.DebugPreviousOpcode);
        Assert.Equal(0, cpu.DebugPriorTrailingAtNextPc);
        Assert.Equal((ushort)0x8123, cpu.PC);

        cpu.Tick();

        Assert.Equal(4, cpu.DebugCycle);
        Assert.Equal((ushort)0x8125, cpu.PC);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001, TEST: TEST-X64SC-LOCKSTEP-001.
    /// When a preceding immediate load consumes the overlapping fetch phase,
    /// VICE reaches ST's unclocked INC_PC(2) on STA (zp),Y cycle 4. Branch and
    /// cold-entry paths retain their separate cycle-4 opcode-PC checkpoint.
    /// </summary>
    [Fact]
    public void StaIndYAfterImmediateLoadTrailingCheckpoint_Cycle4AdvancesPc()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x00D0] = 0x00;
        memory[0x00D1] = 0x04;
        memory[0x8000] = 0x20; // JSR $8100
        memory[0x8001] = 0x00;
        memory[0x8002] = 0x81;
        memory[0x8003] = 0xA8; // TAY, preserving the clean fetch region
        memory[0x8004] = 0xA9; // LDA #$20
        memory[0x8005] = 0x20;
        memory[0x8006] = 0x91; // STA ($D0),Y
        memory[0x8007] = 0xD0;
        memory[0x8008] = 0xEA;
        memory[0x8100] = 0x60;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        cpu.A = 0x0E;

        for (var i = 0; i < 48 && !(cpu.DebugOpcode == 0x91 && cpu.DebugOpcodeAddress == 0x8006); i++)
            cpu.Tick();

        Assert.Equal((byte)0x91, cpu.DebugOpcode);
        Assert.Equal(5, cpu.DebugCycle);
        Assert.Equal((byte)0xA9, cpu.DebugPreviousOpcode);
        Assert.Equal(1, cpu.DebugPriorTrailingAtNextPc);
        Assert.Equal((ushort)0x8006, cpu.PC);

        cpu.Tick();

        Assert.Equal(4, cpu.DebugCycle);
        Assert.Equal((ushort)0x8008, cpu.PC);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001, TEST: TEST-X64SC-LOCKSTEP-001.
    /// An early-advanced STA (zp),Y does not retain the held-store phase. Its
    /// following INY therefore completes on the normal final implied checkpoint.
    /// </summary>
    [Fact]
    public void InyAfterEarlyAdvancedStaIndY_LastClkCompletesTransfer()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x00D0] = 0x00;
        memory[0x00D1] = 0x04;
        memory[0x8000] = 0x20; // JSR $8100
        memory[0x8001] = 0x00;
        memory[0x8002] = 0x81;
        memory[0x8003] = 0xA8; // TAY
        memory[0x8004] = 0xA9; // LDA #$20
        memory[0x8005] = 0x20;
        memory[0x8006] = 0x91; // STA ($D0),Y
        memory[0x8007] = 0xD0;
        memory[0x8008] = 0xC8; // INY
        memory[0x8009] = 0xEA;
        memory[0x8100] = 0x60;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        cpu.A = 0x0E;

        for (var i = 0; i < 64 && !(cpu.DebugOpcode == 0xC8 && cpu.DebugOpcodeAddress == 0x8008); i++)
            cpu.Tick();

        Assert.Equal((byte)0xC8, cpu.DebugOpcode);
        Assert.Equal(1, cpu.DebugCycle);
        Assert.Equal((byte)0x0E, cpu.Y);
        Assert.Equal((ushort)0x8008, cpu.PC);

        cpu.Tick();

        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((byte)0x0F, cpu.Y);
        Assert.Equal((ushort)0x8009, cpu.PC);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001, TEST: TEST-X64SC-LOCKSTEP-001.
    /// A fused implied body consumes the overlapping phase before the following
    /// store. VICE therefore reaches STA (zp),Y's unclocked INC_PC at cycle 4.
    /// </summary>
    [Fact]
    public void StaIndYAfterFusedIny_Cycle4AdvancesPc()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x00D0] = 0x00;
        memory[0x00D1] = 0x04;
        memory[0x8000] = 0x20; // JSR $8100
        memory[0x8001] = 0x00;
        memory[0x8002] = 0x81;
        memory[0x8003] = 0xA8; // TAY
        memory[0x8004] = 0xA9; // LDA #$20
        memory[0x8005] = 0x20;
        memory[0x8006] = 0x91; // STA ($D0),Y
        memory[0x8007] = 0xD0;
        memory[0x8008] = 0xC8; // INY
        memory[0x8009] = 0x91; // STA ($D0),Y
        memory[0x800A] = 0xD0;
        memory[0x800B] = 0xEA;
        memory[0x8100] = 0x60;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        cpu.A = 0x0E;

        for (var i = 0; i < 80 && !(cpu.DebugOpcode == 0x91 && cpu.DebugOpcodeAddress == 0x8009); i++)
            cpu.Tick();

        Assert.Equal((byte)0x91, cpu.DebugOpcode);
        Assert.Equal(5, cpu.DebugCycle);
        Assert.Equal((byte)0xC8, cpu.DebugPreviousOpcode);
        Assert.Equal(0, cpu.DebugPriorTrailingAtNextPc);
        Assert.Equal((ushort)0x8009, cpu.PC);

        cpu.Tick();

        Assert.Equal(4, cpu.DebugCycle);
        Assert.Equal((ushort)0x800B, cpu.PC);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001, TEST: TEST-X64SC-LOCKSTEP-001.
    /// A staged store ends the prior implied-fusion chain after its entry phase
    /// is captured. That chain must not survive RTS and force a later held-store
    /// DEY to execute on its final fetch checkpoint.
    /// </summary>
    [Fact]
    public void HeldStaIndYAfterStagedStoreChain_DeyLastClkHoldsPreOp()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x00D0] = 0x00;
        memory[0x00D1] = 0x04;
        memory[0x8000] = 0x20; // JSR $8100
        memory[0x8001] = 0x00;
        memory[0x8002] = 0x81;
        memory[0x8003] = 0xA9; // LDA #$00 after RTS
        memory[0x8004] = 0x00;
        memory[0x8005] = 0x91; // held STA ($D0),Y
        memory[0x8006] = 0xD0;
        memory[0x8007] = 0x88; // DEY
        memory[0x8008] = 0xEA;
        memory[0x8100] = 0xA8; // TAY
        memory[0x8101] = 0xA9; // LDA #$20
        memory[0x8102] = 0x20;
        memory[0x8103] = 0x91; // early STA ($D0),Y
        memory[0x8104] = 0xD0;
        memory[0x8105] = 0xC8; // fused INY
        memory[0x8106] = 0x91; // staged store ends the chain
        memory[0x8107] = 0xD0;
        memory[0x8108] = 0x60; // RTS

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        cpu.A = 0x0E;

        for (var i = 0; i < 128 && !(cpu.DebugOpcode == 0x88 && cpu.DebugOpcodeAddress == 0x8007); i++)
            cpu.Tick();
        while (cpu.DebugOpcode == 0x88 && cpu.DebugCycle != 0)
            cpu.Tick();

        Assert.Equal((byte)0x88, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((byte)0x0F, cpu.Y);
        Assert.Equal((ushort)0x8007, cpu.PC);

        cpu.Tick();

        Assert.Equal((byte)0x0E, cpu.Y);
        Assert.Equal((ushort)0x8008, cpu.PC);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001,
    /// TEST: TEST-X64SC-LOCKSTEP-001.
    /// VICE interrupt_delay dispatches alarms before testing irq_clk and therefore
    /// counts an IRQ edge raised between CPU clocks on the immediately following
    /// CLK_INC. After held STA (zp),Y / DEY / taken BPL, that third delay clock
    /// dispatches IRQ before the branch target JSR can be fetched.
    /// </summary>
    [Fact]
    public void HeldStaIndY_DeyTakenBpl_IrqBetweenCpuClocks_DispatchesBeforeTargetJsr()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0xFFFE] = 0x00;
        memory[0xFFFF] = 0x90;
        memory[0x00D0] = 0x00;
        memory[0x00D1] = 0x04;
        memory[0x8000] = 0x20; // JSR $8100
        memory[0x8001] = 0x00;
        memory[0x8002] = 0x81;
        memory[0x8003] = 0xA9; // LDA #$00 after RTS
        memory[0x8004] = 0x00;
        memory[0x8005] = 0x91; // held STA ($D0),Y
        memory[0x8006] = 0xD0;
        memory[0x8007] = 0x88; // DEY
        memory[0x8008] = 0x10; // BPL $800C
        memory[0x8009] = 0x02;
        memory[0x800A] = 0xEA;
        memory[0x800B] = 0xEA;
        memory[0x800C] = 0x20; // JSR $8200, must not be fetched
        memory[0x800D] = 0x00;
        memory[0x800E] = 0x82;
        memory[0x8100] = 0xA8; // TAY
        memory[0x8101] = 0xA9; // LDA #$20
        memory[0x8102] = 0x20;
        memory[0x8103] = 0x91; // early STA ($D0),Y
        memory[0x8104] = 0xD0;
        memory[0x8105] = 0xC8; // fused INY
        memory[0x8106] = 0x91; // staged store ends the chain
        memory[0x8107] = 0xD0;
        memory[0x8108] = 0x60; // RTS
        memory[0x8200] = 0x60;
        memory[0x9000] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var irq = new InterruptLine(InterruptType.Irq);
        var cpu = new Mos6502(bus);
        var clock = new SystemClock(985_248, cpu, irq);
        var irqSource = new AfterCpuIrqSource(irq, cpu, 0x88, 0);
        clock.Register(cpu);
        clock.Register(irqSource);
        cpu.Reset();
        cpu.A = 0x0E;
        cpu.P = 0x20;
        cpu.S = 0xF0;

        for (var i = 0; i < 128 && !(cpu.DebugOpcode == 0x88 && cpu.DebugCycle == 1); i++)
            clock.Step();

        Assert.Equal((byte)0x88, cpu.DebugOpcode);
        Assert.Equal(1, cpu.DebugCycle);
        Assert.Equal((ushort)0x8007, cpu.PC);
        irqSource.Armed = true;

        clock.Step();

        Assert.Equal((byte)0x88, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal(0, clock.DebugIrqDelayCycles);

        clock.Step();

        Assert.Equal((byte)0x10, cpu.DebugOpcode);
        Assert.Equal(1, clock.DebugIrqDelayCycles);

        var targetJsrFetched = false;
        var history = new List<string>();
        for (var i = 0; i < 12 && cpu.S == 0xF0; i++)
        {
            clock.Step();
            targetJsrFetched |= cpu.DebugOpcode == 0x20 && cpu.DebugOpcodeAddress == 0x800C;
            history.Add(
                $"${i}: op=${cpu.DebugOpcode:X2} c=${cpu.DebugCycle} pc=${cpu.PC:X4} " +
                $"s=${cpu.S:X2} delay=${clock.DebugIrqDelayCycles} irqSeq=${cpu.DebugInterruptSequenceRemaining} " +
                $"sample=${clock.DebugFetchIrqNote}");
        }

        Assert.False(targetJsrFetched, string.Join(Environment.NewLine, history));
        Assert.True(cpu.S < 0xF0, $"IRQ did not push before target JSR; S=${cpu.S:X2}");
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001, TEST: TEST-X64SC-LOCKSTEP-001.
    /// A soft-deferred implied body commits before the following FETCH_OPCODE.
    /// When that following instruction is LDA immediate, VICE still exports the
    /// LDA opcode PC and pre-load accumulator on its second fetch clock.
    /// </summary>
    [Fact]
    public void SoftDeferredTaxThenLdaImm_SecondFetchHoldsPreOpState()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x8000] = 0x60; // RTS to JSR
        memory[0x8003] = 0x20; // JSR $8100
        memory[0x8004] = 0x00;
        memory[0x8005] = 0x81;
        memory[0x8006] = 0xEA;
        memory[0x01FE] = 0x02;
        memory[0x01FF] = 0x80;
        memory[0x8100] = 0xA2; // LDX #$00
        memory[0x8101] = 0x00;
        memory[0x8102] = 0x86; // STX $10
        memory[0x8103] = 0x10;
        memory[0x8104] = 0x8A; // TXA
        memory[0x8105] = 0x18; // CLC
        memory[0x8106] = 0x69; // ADC #$01
        memory[0x8107] = 0x01;
        memory[0x8108] = 0xAA; // TAX
        memory[0x8109] = 0xA9; // LDA #$08
        memory[0x810A] = 0x08;
        memory[0x810B] = 0x60;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        cpu.S = 0xFD;

        var history = new List<string>();
        for (var i = 0; i < 64 && !(cpu.DebugOpcode == 0xA9
            && cpu.DebugOpcodeAddress == 0x8109
            && cpu.DebugCycle == 1); i++)
        {
            cpu.Tick();
            history.Add(
                $"${i}: op=${cpu.DebugOpcode:X2} addr=${cpu.DebugOpcodeAddress:X4} " +
                $"c=${cpu.DebugCycle} pc=${cpu.PC:X4} a=${cpu.A:X2} x=${cpu.X:X2} " +
                $"softImpl=${cpu.DebugSoftDeferredImplied} follows=${cpu.DebugImmediateLoadFollowsSoftDeferredBody}");
        }

        Assert.Equal((byte)0xA9, cpu.DebugOpcode);
        Assert.Equal((ushort)0x8109, cpu.DebugOpcodeAddress);
        Assert.Equal(1, cpu.DebugCycle);
        Assert.Equal((byte)0x01, cpu.A);
        Assert.True(
            cpu.DebugImmediateLoadFollowsSoftDeferredBody,
            string.Join(Environment.NewLine, history));

        cpu.Tick();

        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((ushort)0x8109, cpu.PC);
        Assert.Equal((byte)0x01, cpu.A);
        Assert.True(cpu.DebugSoftDeferredImmediateLoad);

        cpu.Tick();

        Assert.Equal((byte)0x08, cpu.A);
        Assert.Equal((ushort)0x810B, cpu.PC);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001, TEST: TEST-X64SC-LOCKSTEP-001.
    /// Consecutive VICE LD(GET_IMM) bodies remain separated by the following
    /// instruction's two FETCH_OPCODE clocks. The second load's final fetch
    /// checkpoint still exposes its opcode PC and pre-load register value.
    /// </summary>
    [Fact]
    public void ConsecutiveImmediateLoads_SecondFetchHoldsFollowingLoadPreOpState()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x8000] = 0xA2; // LDX #$00
        memory[0x8001] = 0x00;
        memory[0x8002] = 0xA9; // LDA #$0E
        memory[0x8003] = 0x0E;
        memory[0x8004] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        cpu.A = 0x00;

        AdvanceToOpcode(cpu, 0xA9);
        while (cpu.DebugOpcode == 0xA9 && cpu.DebugCycle != 0)
            cpu.Tick();

        Assert.Equal((byte)0xA9, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((byte)0x00, cpu.A);
        Assert.Equal((ushort)0x8002, cpu.PC);

        cpu.Tick();

        Assert.Equal((byte)0x0E, cpu.A);
        Assert.Equal((byte)0xEA, cpu.DebugOpcode);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001, TEST: TEST-X64SC-LOCKSTEP-001.
    /// VICE ST executes INC_PC before its final STORE clock. The following
    /// immediate load therefore retains both FETCH_OPCODE checkpoints before
    /// LD(GET_IMM) updates the destination register.
    /// </summary>
    [Fact]
    public void StoreThenImmediateLoad_SecondFetchHoldsPreOpState()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x8000] = 0x20; // JSR $8100
        memory[0x8001] = 0x00;
        memory[0x8002] = 0x81;
        memory[0x8003] = 0x20; // JSR $9000 after RTS
        memory[0x8004] = 0x00;
        memory[0x8005] = 0x90;
        memory[0x8006] = 0xEA;
        memory[0x8100] = 0x60; // RTS
        memory[0x9000] = 0xA2; // LDX #$02
        memory[0x9001] = 0x02;
        memory[0x9002] = 0x86; // STX $10
        memory[0x9003] = 0x10;
        memory[0x9004] = 0xA9; // LDA #$00
        memory[0x9005] = 0x00;
        memory[0x9006] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        cpu.A = 0x0E;

        AdvanceToOpcode(cpu, 0xA9);
        while (cpu.DebugOpcode == 0xA9 && cpu.DebugCycle != 0)
            cpu.Tick();

        Assert.Equal((byte)0xA9, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal(1, cpu.DebugPriorTrailingAtNextPc);
        Assert.Equal((byte)0x0E, cpu.A);
        Assert.Equal((ushort)0x9004, cpu.PC);

        cpu.Tick();

        Assert.Equal((byte)0x00, cpu.A);
        Assert.Equal((byte)0xEA, cpu.DebugOpcode);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001, TEST: TEST-X64SC-LOCKSTEP-001.
    /// An unindexed VICE ST path with two exported next-PC checkpoints overlaps
    /// RTS's first FETCH clock. STACK_PEEK is therefore already complete when
    /// DebugCycle 3 exports the first pull.
    /// </summary>
    [Fact]
    public void RtsAfterStaZp_TwoTrailingCheckpointsFirstPullVisibleAtCycle3()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x8000] = 0x20; // JSR $8200
        memory[0x8001] = 0x00;
        memory[0x8002] = 0x82;
        memory[0x8003] = 0x20; // JSR $9000 after RTS
        memory[0x8004] = 0x00;
        memory[0x8005] = 0x90;
        memory[0x8006] = 0xEA;
        memory[0x8200] = 0x60; // RTS
        memory[0x9000] = 0xA9; // LDA #$00
        memory[0x9001] = 0x00;
        memory[0x9002] = 0xD0; // BNE not taken, enters clean-fetch region
        memory[0x9003] = 0x00;
        memory[0x9004] = 0xA2; // LDX #$02
        memory[0x9005] = 0x02;
        memory[0x9006] = 0x86; // STX $10
        memory[0x9007] = 0x10;
        memory[0x9008] = 0xA9; // LDA #$00
        memory[0x9009] = 0x00;
        memory[0x900A] = 0xA6; // LDX $10
        memory[0x900B] = 0x10;
        memory[0x900C] = 0x20; // JSR $9100
        memory[0x900D] = 0x00;
        memory[0x900E] = 0x91;
        memory[0x900F] = 0x60;
        memory[0x9100] = 0x8D; // STA $0400
        memory[0x9101] = 0x00;
        memory[0x9102] = 0x04;
        memory[0x9103] = 0x8E; // STX $0401
        memory[0x9104] = 0x01;
        memory[0x9105] = 0x04;
        memory[0x9106] = 0xA9; // LDA #$02
        memory[0x9107] = 0x02;
        memory[0x9108] = 0x69; // ADC #$60
        memory[0x9109] = 0x60;
        memory[0x910A] = 0x85; // STA $11
        memory[0x910B] = 0x11;
        memory[0x910C] = 0x60; // RTS

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();

        for (var i = 0;
            i < 96 && !(cpu.DebugOpcode == 0x60 && cpu.DebugOpcodeAddress == 0x910C);
            i++)
        {
            cpu.Tick();
        }

        Assert.Equal((ushort)0x910C, cpu.DebugOpcodeAddress);
        Assert.Equal(2, cpu.DebugPriorTrailingAtNextPc);
        var sAtFetch = cpu.S;
        while (cpu.DebugOpcode == 0x60 && cpu.DebugCycle != 3)
            cpu.Tick();

        Assert.Equal((byte)(sAtFetch + 1), cpu.S);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001, TEST: TEST-X64SC-LOCKSTEP-001.
    /// VICE LD performs its register and flag updates plus INC_PC after the
    /// addressing-mode read clock. One exported next-PC checkpoint overlaps
    /// the following implied instruction's first FETCH clock, so CLC completes
    /// on the second.
    /// </summary>
    [Fact]
    public void ClcAfterLdaZp_OneTrailingCheckpointCompletesFlagUpdate()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x0010] = 0x80;
        memory[0x0011] = 0x80;
        memory[0x8000] = 0x20; // JSR $8100
        memory[0x8001] = 0x00;
        memory[0x8002] = 0x81;
        memory[0x8003] = 0x20; // JSR $9000 after RTS
        memory[0x8004] = 0x00;
        memory[0x8005] = 0x90;
        memory[0x8006] = 0xEA;
        memory[0x8100] = 0x60; // RTS
        memory[0x9000] = 0x26; // ROL $10 sets carry
        memory[0x9001] = 0x10;
        memory[0x9002] = 0xA5; // LDA $11
        memory[0x9003] = 0x11;
        memory[0x9004] = 0x18; // CLC
        memory[0x9005] = 0x60;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();

        AdvanceToOpcode(cpu, 0x18);

        Assert.Equal(1, cpu.DebugCycle);
        Assert.Equal(1, cpu.DebugPriorTrailingAtNextPc);
        Assert.True((cpu.P & 0x01) != 0);
        Assert.Equal((ushort)0x9004, cpu.PC);

        cpu.Tick();

        Assert.True((cpu.P & 0x01) == 0);
        Assert.Equal((ushort)0x9005, cpu.PC);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001, TEST: TEST-X64SC-LOCKSTEP-001.
    /// VICE zero-page RMW's dummy and final write clocks consume the following
    /// fetch-only phase. LDA zero-page therefore exposes its loaded A on the
    /// GET_ZERO data-read clock.
    /// </summary>
    [Fact]
    public void LdaZpAfterRolZp_ThreeTrailingCheckpointsShowsLoadedAOnDataRead()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x0010] = 0x01;
        memory[0x0011] = 0x80;
        memory[0x8000] = 0x20; // JSR $8100
        memory[0x8001] = 0x00;
        memory[0x8002] = 0x81;
        memory[0x8003] = 0x20; // JSR $9000 after RTS
        memory[0x8004] = 0x00;
        memory[0x8005] = 0x90;
        memory[0x8006] = 0xEA;
        memory[0x8100] = 0x60; // RTS
        memory[0x9000] = 0xA9; // LDA #$00
        memory[0x9001] = 0x00;
        memory[0x9002] = 0x26; // ROL $10
        memory[0x9003] = 0x10;
        memory[0x9004] = 0xA5; // LDA $11
        memory[0x9005] = 0x11;
        memory[0x9006] = 0x60;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();

        AdvanceToOpcode(cpu, 0xA5);
        while (cpu.DebugOpcode == 0xA5 && cpu.DebugCycle != 1)
            cpu.Tick();

        Assert.Equal(3, cpu.DebugPriorTrailingAtNextPc);
        Assert.Equal((byte)0x80, cpu.A);
        Assert.Equal((ushort)0x9004, cpu.PC);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001, TEST: TEST-X64SC-LOCKSTEP-001.
    /// VICE zero-page RMW's dummy and final write clocks leave the following
    /// instruction past its fetch-only phase. ADC absolute therefore commits
    /// on its data-read checkpoint instead of adding a synthetic host hold.
    /// </summary>
    [Fact]
    public void AdcAbsAfterRolZp_ThreeTrailingCheckpointsCompletesAdd()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x0010] = 0x01;
        memory[0x0011] = 0x01;
        memory[0x8000] = 0x20; // JSR $8100
        memory[0x8001] = 0x00;
        memory[0x8002] = 0x81;
        memory[0x8003] = 0x20; // JSR $9000 after RTS
        memory[0x8004] = 0x00;
        memory[0x8005] = 0x90;
        memory[0x8006] = 0xEA;
        memory[0x8100] = 0x60; // RTS
        memory[0x9000] = 0xA9; // LDA #$50
        memory[0x9001] = 0x50;
        memory[0x9002] = 0x26; // ROL $10
        memory[0x9003] = 0x10;
        memory[0x9004] = 0x6D; // ADC $0011
        memory[0x9005] = 0x11;
        memory[0x9006] = 0x00;
        memory[0x9007] = 0x60;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        cpu.P = 0x20;

        AdvanceToOpcode(cpu, 0x6D);
        while (cpu.DebugOpcode == 0x6D && cpu.DebugCycle != 1)
            cpu.Tick();

        Assert.Equal(3, cpu.DebugPriorTrailingAtNextPc);
        Assert.Equal((byte)0x50, cpu.A);
        Assert.Equal((ushort)0x9004, cpu.PC);

        cpu.Tick();

        Assert.Equal((byte)0x51, cpu.A);
        Assert.Equal((ushort)0x9007, cpu.PC);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001, TEST: TEST-X64SC-LOCKSTEP-001.
    /// VICE zero-page RMW performs its dummy and final write clocks after
    /// INC_PC. Three exported next-PC checkpoints let the following
    /// accumulator ASL execute on its second FETCH_OPCODE clock.
    /// </summary>
    [Fact]
    public void AslAAfterRolZp_ThreeTrailingCheckpointsCompletesShift()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x0010] = 0x01;
        memory[0x8000] = 0x20; // JSR $8100
        memory[0x8001] = 0x00;
        memory[0x8002] = 0x81;
        memory[0x8003] = 0x20; // JSR $9000 after RTS
        memory[0x8004] = 0x00;
        memory[0x8005] = 0x90;
        memory[0x8006] = 0xEA;
        memory[0x8100] = 0x60; // RTS
        memory[0x9000] = 0xA9; // LDA #$14
        memory[0x9001] = 0x14;
        memory[0x9002] = 0x26; // ROL $10
        memory[0x9003] = 0x10;
        memory[0x9004] = 0x0A; // ASL A
        memory[0x9005] = 0x60;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();

        AdvanceToOpcode(cpu, 0x0A);

        Assert.Equal(1, cpu.DebugCycle);
        Assert.Equal(3, cpu.DebugPriorTrailingAtNextPc);
        Assert.Equal((byte)0x14, cpu.A);
        Assert.Equal((ushort)0x9004, cpu.PC);

        cpu.Tick();

        Assert.Equal((byte)0x28, cpu.A);
        Assert.Equal((ushort)0x9005, cpu.PC);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001, TEST: TEST-X64SC-LOCKSTEP-001.
    /// VICE unindexed ST uses INC_PC followed by its write clock. When that
    /// path leaves two next-PC checkpoints, the following implied TAX reaches
    /// its register update and INC_PC on the second FETCH_OPCODE clock.
    /// </summary>
    [Fact]
    public void StaZpThenTax_TwoTrailingCheckpointsCompletesTransfer()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x8000] = 0x20; // JSR $8200
        memory[0x8001] = 0x00;
        memory[0x8002] = 0x82;
        memory[0x8003] = 0x20; // JSR $9000 after RTS
        memory[0x8004] = 0x00;
        memory[0x8005] = 0x90;
        memory[0x8006] = 0xEA;
        memory[0x8200] = 0x60; // RTS
        memory[0x9000] = 0xA9; // LDA #$00
        memory[0x9001] = 0x00;
        memory[0x9002] = 0xD0; // BNE not taken, enters clean-fetch region
        memory[0x9003] = 0x00;
        memory[0x9004] = 0xA2; // LDX #$02
        memory[0x9005] = 0x02;
        memory[0x9006] = 0x86; // STX $10
        memory[0x9007] = 0x10;
        memory[0x9008] = 0xA9; // LDA #$00
        memory[0x9009] = 0x00;
        memory[0x900A] = 0xA6; // LDX $10
        memory[0x900B] = 0x10;
        memory[0x900C] = 0x20; // JSR $9100
        memory[0x900D] = 0x00;
        memory[0x900E] = 0x91;
        memory[0x900F] = 0x60;
        memory[0x9100] = 0x8D; // STA $0400
        memory[0x9101] = 0x00;
        memory[0x9102] = 0x04;
        memory[0x9103] = 0x8E; // STX $0401
        memory[0x9104] = 0x01;
        memory[0x9105] = 0x04;
        memory[0x9106] = 0xA9; // LDA #$00
        memory[0x9107] = 0x00;
        memory[0x9108] = 0x85; // STA $11
        memory[0x9109] = 0x11;
        memory[0x910A] = 0xAA; // TAX
        memory[0x910B] = 0x60;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();

        AdvanceToOpcode(cpu, 0xAA);

        Assert.Equal(1, cpu.DebugCycle);
        Assert.Equal(2, cpu.DebugPriorTrailingAtNextPc);
        Assert.Equal((byte)0x02, cpu.X);
        Assert.Equal((ushort)0x910A, cpu.PC);

        cpu.Tick();

        Assert.Equal((byte)0x00, cpu.X);
        Assert.Equal((ushort)0x910B, cpu.PC);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001, TEST: TEST-X64SC-LOCKSTEP-001.
    /// VICE ST absolute has already exported two next-PC checkpoints when the
    /// following immediate load reaches its second FETCH_OPCODE clock. The
    /// clock therefore also exposes LD(GET_IMM), unlike the one-checkpoint
    /// zero-page store path.
    /// </summary>
    [Fact]
    public void AbsoluteStoreThenImmediateLoad_SecondFetchCompletesLoad()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x8000] = 0xA2; // LDX #$02
        memory[0x8001] = 0x02;
        memory[0x8002] = 0x8E; // STX $0400
        memory[0x8003] = 0x00;
        memory[0x8004] = 0x04;
        memory[0x8005] = 0xA9; // LDA #$00
        memory[0x8006] = 0x00;
        memory[0x8007] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        cpu.A = 0x0E;

        AdvanceToOpcode(cpu, 0xA9);

        Assert.Equal(1, cpu.DebugCycle);
        Assert.Equal(2, cpu.DebugPriorTrailingAtNextPc);
        Assert.Equal((byte)0x0E, cpu.A);
        Assert.Equal((ushort)0x8005, cpu.PC);

        cpu.Tick();

        Assert.Equal((byte)0x00, cpu.A);
        Assert.Equal((ushort)0x8007, cpu.PC);

    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001, TEST: TEST-X64SC-LOCKSTEP-001.
    /// VICE FETCH_OPCODE performs two clocks before LDA immediate executes.
    /// A not-taken BEQ cannot make the immediate body visible on the second
    /// fetch checkpoint.
    /// </summary>
    [Fact]
    public void NotTakenBeqThenLdaImm_SecondFetch_HoldsPreOpAAndOpcodePc()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x8000] = 0xA9; // LDA #$01 (Z clear)
        memory[0x8001] = 0x01;
        memory[0x8002] = 0xD0; // taken BNE to BEQ
        memory[0x8003] = 0x00;
        memory[0x8004] = 0xF0; // BEQ not taken
        memory[0x8005] = 0x00;
        memory[0x8006] = 0xA9; // LDA #$25
        memory[0x8007] = 0x25;
        memory[0x8008] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        AdvanceToOpcode(cpu, 0xA9);
        for (var i = 0; i < 16 && !(cpu.DebugOpcode == 0xA9 && cpu.PC == 0x8006); i++)
            cpu.Tick();
        while (cpu.DebugOpcode == 0xA9 && cpu.DebugCycle != 0)
            cpu.Tick();

        Assert.Equal((byte)0xA9, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.True(
            cpu.A == 0x01 && cpu.PC == 0x8006,
            $"trail={cpu.DebugPriorTrailingAtNextPc} nonOvlF={cpu.DebugNonOverlappedFetchPhase} "
            + $"nonOvlR={cpu.DebugNonOverlappedRegion} soft={cpu.DebugSoftDeferredImmediateLoad}");

        cpu.Tick();

        Assert.Equal((byte)0x25, cpu.A);
        Assert.Equal((byte)0xEA, cpu.DebugOpcode);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001, TEST: TEST-X64SC-LOCKSTEP-001.
    /// VICE uses the same FETCH_OPCODE then LD(GET_IMM) order at a taken
    /// same-page branch target. The second LDA fetch exposes pre-load A and
    /// opcode PC; the following tick commits A without a body-only clock.
    /// </summary>
    [Fact]
    public void TakenBneThenLdaImm_SecondFetch_HoldsPreOpAAndOpcodePc()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x8000] = 0xA9; // LDA #$4C
        memory[0x8001] = 0x4C;
        memory[0x8002] = 0x38; // SEC
        memory[0x8003] = 0x85; // STA $02
        memory[0x8004] = 0x02;
        memory[0x8005] = 0xB0; // BCS $8007 (taken)
        memory[0x8006] = 0x00;
        memory[0x8007] = 0xA9; // LDA #$7F
        memory[0x8008] = 0x7F;
        memory[0x8009] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        for (var i = 0; i < 32 && !(cpu.DebugOpcode == 0xA9 && cpu.PC == 0x8007); i++)
            cpu.Tick();
        Assert.Equal((byte)0xA9, cpu.DebugOpcode);
        Assert.Equal((byte)0xB0, cpu.DebugPreviousOpcode);
        Assert.Equal(2, cpu.DebugPriorTrailingAtNextPc);
        Assert.True(cpu.DebugAfterFullLengthTakenBranch);
        Assert.Equal((ushort)0x8007, cpu.PC);
        while (cpu.DebugOpcode == 0xA9 && cpu.DebugCycle != 0)
            cpu.Tick();

        Assert.Equal((byte)0xA9, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((byte)0x4C, cpu.A);
        Assert.Equal((ushort)0x8007, cpu.PC);

        cpu.Tick();

        Assert.Equal((byte)0x7F, cpu.A);
        Assert.Equal((byte)0xEA, cpu.DebugOpcode);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001, TEST: TEST-X64SC-LOCKSTEP-001.
    /// A taken branch whose preceding store exported two next-PC checkpoints
    /// has already staged fall-through and completed its dummy clock before the
    /// target JUMP. VICE therefore completes target LD(GET_IMM) on its second
    /// FETCH checkpoint. A later non-load immediate with one exported next-PC
    /// checkpoint likewise consumes the overlapping phase before LDY GET_ZERO.
    /// </summary>
    [Fact]
    public void TakenBccAfterStaAbsThenLdaImm_CompletesAndFusesFollowingLdyZp()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x0010] = 0x00;
        memory[0x8000] = 0xA9; // LDA #$0F
        memory[0x8001] = 0x0F;
        memory[0x8002] = 0x18; // CLC
        memory[0x8003] = 0x69; // ADC #$00
        memory[0x8004] = 0x00;
        memory[0x8005] = 0x8D; // STA $0400
        memory[0x8006] = 0x00;
        memory[0x8007] = 0x04;
        memory[0x8008] = 0x90; // BCC $800D
        memory[0x8009] = 0x03;
        memory[0x800D] = 0xA9; // LDA #$01
        memory[0x800E] = 0x01;
        memory[0x800F] = 0x0A; // ASL A
        memory[0x8010] = 0x0A; // ASL A
        memory[0x8011] = 0x0A; // ASL A
        memory[0x8012] = 0x0A; // ASL A
        memory[0x8013] = 0x09; // ORA #$00
        memory[0x8014] = 0x00;
        memory[0x8015] = 0xA4; // LDY $10
        memory[0x8016] = 0x10;
        memory[0x8017] = 0x91; // STA ($20),Y
        memory[0x8018] = 0x20;
        memory[0x8019] = 0xA6; // LDX $10
        memory[0x801A] = 0x10;
        memory[0x801B] = 0xE8; // INX
        memory[0x801C] = 0xE0; // CPX #$02
        memory[0x801D] = 0x02;
        memory[0x801E] = 0x90; // BCC $7FE8 (page-cross)
        memory[0x801F] = 0xC8;
        memory[0x8020] = 0xEA; // fall-through
        memory[0x7FE8] = 0x86; // STX $10 at target
        memory[0x7FE9] = 0x10;
        memory[0x7FEA] = 0xA9; // LDA #$01
        memory[0x7FEB] = 0x01;
        memory[0x7FEC] = 0x0A; // ASL A
        memory[0x7FED] = 0x0A; // ASL A
        memory[0x7FEE] = 0x0A; // ASL A
        memory[0x7FEF] = 0x0A; // ASL A
        memory[0x7FF0] = 0x09; // ORA #$00
        memory[0x7FF1] = 0x00;
        memory[0x7FF2] = 0xA4; // LDY $11
        memory[0x7FF3] = 0x11;
        memory[0x7FF4] = 0x91; // STA ($20),Y
        memory[0x7FF5] = 0x20;
        memory[0x7FF6] = 0xA6; // LDX $11
        memory[0x7FF7] = 0x11;
        memory[0x7FF8] = 0xE8; // INX
        memory[0x7FF9] = 0xE0; // CPX #$00 (C set)
        memory[0x7FFA] = 0x00;
        memory[0x7FFB] = 0x90; // BCC $7FFD (not taken)
        memory[0x7FFC] = 0x00;
        memory[0x7FFD] = 0xA6; // LDX $12
        memory[0x7FFE] = 0x12;
        memory[0x0011] = 0x17;
        memory[0x0020] = 0x00;
        memory[0x0021] = 0x04;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        cpu.X = 0x08;
        cpu.Y = 0x08;

        AdvanceToOpcode(cpu, 0x8D);
        for (var i = 0; i < 24 && !(cpu.DebugOpcode == 0xA9 && cpu.DebugOpcodeAddress == 0x800D); i++)
            cpu.Tick();

        Assert.Equal((byte)0xA9, cpu.DebugOpcode);
        Assert.Equal(1, cpu.DebugCycle);
        Assert.Equal((byte)0x90, cpu.DebugPreviousOpcode);
        Assert.Equal(2, cpu.DebugPriorTrailingAtNextPc);
        Assert.True(cpu.DebugAfterFullLengthTakenBranch);
        Assert.Equal((byte)0x0F, cpu.A);
        Assert.Equal((ushort)0x800D, cpu.PC);

        cpu.Tick();

        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((byte)0x01, cpu.A);
        Assert.Equal((ushort)0x800F, cpu.PC);

        AdvanceToOpcode(cpu, 0xA4);
        Assert.Equal(2, cpu.DebugCycle);
        Assert.Equal((byte)0x09, cpu.DebugPreviousOpcode);
        Assert.Equal(1, cpu.DebugPriorTrailingAtNextPc);
        Assert.True(cpu.DebugNonOverlappedFetchPhase);
        Assert.Equal((byte)0x08, cpu.Y);

        cpu.Tick();

        Assert.Equal(1, cpu.DebugCycle);
        Assert.Equal((byte)0x00, cpu.Y);
        Assert.Equal((ushort)0x8015, cpu.PC);

        AdvanceToOpcode(cpu, 0xA6);
        Assert.Equal(2, cpu.DebugCycle);
        Assert.Equal((byte)0x91, cpu.DebugPreviousOpcode);
        Assert.Equal(5, cpu.DebugPriorTrailingAtNextPc);
        Assert.True(cpu.DebugNonOverlappedFetchPhase);
        Assert.Equal((byte)0x08, cpu.X);

        cpu.Tick();

        Assert.Equal(1, cpu.DebugCycle);
        Assert.Equal((byte)0x00, cpu.X);
        Assert.Equal((ushort)0x8019, cpu.PC);

        cpu.Tick();

        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((ushort)0x801B, cpu.PC);

        AdvanceToOpcode(cpu, 0x90);
        Assert.Equal((byte)0xE0, cpu.DebugPreviousOpcode);
        Assert.Equal(1, cpu.DebugPriorTrailingAtNextPc);
        Assert.True(cpu.DebugTakenBranchStagedFallthrough);

        for (var i = 0; i < 8 && cpu.DebugCycle != 0; i++)
            cpu.Tick();

        Assert.Equal((byte)0x90, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((ushort)0x8020, cpu.PC);

        AdvanceToOpcode(cpu, 0x86);
        while (cpu.DebugOpcode == 0x86 && cpu.DebugCycle > 1)
            cpu.Tick();

        Assert.Equal((byte)0x86, cpu.DebugOpcode);
        Assert.Equal(1, cpu.DebugCycle);
        Assert.Equal((byte)0x90, cpu.DebugPreviousOpcode);
        Assert.Equal(2, cpu.DebugPriorTrailingAtNextPc);
        Assert.True(cpu.DebugAfterFullLengthTakenBranch);
        Assert.Equal((ushort)0x7FE8, cpu.PC);

        AdvanceToOpcode(cpu, 0x90);
        AdvanceToOpcode(cpu, 0xA6);
        Assert.Equal(2, cpu.DebugCycle);
        Assert.Equal((byte)0x90, cpu.DebugPreviousOpcode);
        Assert.Equal(0, cpu.DebugPriorTrailingAtNextPc);
        Assert.False(cpu.DebugAfterFullLengthTakenBranch);
        Assert.Equal((byte)0x18, cpu.X);

        cpu.Tick();

        Assert.Equal(1, cpu.DebugCycle);
        Assert.Equal((byte)0x18, cpu.X);
        Assert.Equal((ushort)0x7FFD, cpu.PC);

        cpu.Tick();

        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((byte)0x00, cpu.X);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001, TEST: TEST-X64SC-LOCKSTEP-001.
    /// Wolf64 sample 85626 follows CPX immediate with a not-taken BCC.
    /// Native VICE still exposes the LDX opcode PC and pre-load X on that
    /// checkpoint; managed must preserve the same fetch phase.
    /// </summary>
    [Fact]
    public void LdxImmAfterCpxImmAndNotTakenBcc_SecondFetch_HoldsPreOpXAndOpcodePc()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x8000] = 0xE0; // CPX #$00 (C set for X=$20)
        memory[0x8001] = 0x00;
        memory[0x8002] = 0x90; // BCC $8004 (not taken)
        memory[0x8003] = 0x00;
        memory[0x8004] = 0xA2; // LDX #$00
        memory[0x8005] = 0x00;
        memory[0x8006] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        cpu.X = 0x20;
        AdvanceToOpcode(cpu, 0xA2);

        Assert.Equal((byte)0x90, cpu.DebugPreviousOpcode);
        Assert.Equal(1, cpu.DebugCycle);
        Assert.Equal((ushort)0x8004, cpu.PC);

        cpu.Tick();

        Assert.Equal((byte)0xA2, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.True(
            cpu.X == 0x20 && cpu.PC == 0x8004,
            $"trail={cpu.DebugPriorTrailingAtNextPc} nonOvlF={cpu.DebugNonOverlappedFetchPhase} "
            + $"nonOvlR={cpu.DebugNonOverlappedRegion} soft={cpu.DebugSoftDeferredImmediateLoad}");
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001.
    /// Use case: Wolf64 sample 2060380. After LDA #, VICE JMP last CLK is
    ///   the target (nPC=$FDF3). Soft-defer after an immediate load kept
    ///   the opcode PC.
    /// Acceptance: JMP abs last CLK after LDA # is the target.
    /// </summary>
    [Fact]
    public void LdaImmThenJmpAbs_LastClk_ExportsTarget()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x8000] = 0xA9;
        memory[0x8001] = 0x01;
        memory[0x8002] = 0xD0; // taken BNE arms non-overlapped
        memory[0x8003] = 0x00;
        memory[0x8004] = 0xA9;
        memory[0x8005] = 0x40;
        memory[0x8006] = 0x4C; // JMP $9001
        memory[0x8007] = 0x01;
        memory[0x8008] = 0x90;
        memory[0x9001] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        AdvanceToOpcode(cpu, 0x4C);
        while (cpu.DebugOpcode == 0x4C && cpu.DebugCycle != 0)
            cpu.Tick();

        Assert.Equal((byte)0x4C, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((ushort)0x9001, cpu.PC);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001.
    /// Use case: KERNAL <c>LDA $D012</c> / <c>BNE</c> (Wolf64 sample 2044617).
    /// After LDA last CLK, taken BNE dummy must export fall-through
    /// (opcode+2), not opcode+3.
    /// Acceptance: After LDA abs A!=0 then BNE, the first CLK after BNE's
    /// last opcode-PC sample is either the branch target or opcode+2.
    /// </summary>
    [Fact]
    public void LdaAbsThenTakenBne_NextClk_IsTargetOrFallThrough()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x8000] = 0xA9; // LDA #$01 (Z clear, taken BNE)
        memory[0x8001] = 0x01;
        memory[0x8002] = 0xD0; // BNE $8004
        memory[0x8003] = 0x00;
        memory[0x8004] = 0xAD; // LDA $9000
        memory[0x8005] = 0x00;
        memory[0x8006] = 0x90;
        memory[0x8007] = 0xD0; // BNE $8004 (KERNAL LDA $D012 / BNE shape)
        memory[0x8008] = 0xFB;
        memory[0x8009] = 0xEA;
        memory[0x9000] = 0x06;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        AdvanceToOpcode(cpu, 0xAD);
        var sawBneFetch = false;
        ushort? dummyPc = null;
        for (var i = 0; i < 32; i++)
        {
            cpu.Tick();
            if (cpu.DebugOpcode == 0xD0 && !sawBneFetch)
            {
                sawBneFetch = true;
                Assert.True(cpu.DebugCycle >= 2,
                    $"taken BNE after LDA abs should be 3-CLK (dummy); DebugCycle={cpu.DebugCycle} PC=${cpu.PC:X4} A=${cpu.A:X2}");
            }

            if (cpu.DebugOpcode == 0xD0 && cpu.DebugCycle == 1)
                dummyPc = cpu.PC;

            if (cpu.DebugOpcode == 0xD0 && cpu.DebugCycle == 0)
                break;
        }

        Assert.Equal((byte)0xD0, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.True(
            dummyPc is 0x8007 or 0x8009,
            $"BNE dummy CLK PC=${dummyPc:X4}; VICE INC_PC then dummy exports opcode or fall-through $8009");
        Assert.True(
            cpu.PC == 0x8004 || cpu.PC == 0x8009,
            $"BNE JUMP CLK PC=${cpu.PC:X4} opcode=${cpu.DebugOpcode:X2}");
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001.
    /// Use case: Wolf64 sample 2049502. Taken BNE dummy has already exported
    ///   fall-through. Bad-line <c>IsCpuCycleStolen</c> is false at RasterX 55
    ///   but <c>IsCpuCycleStealMandatory</c> is still true (lagged BA). If
    ///   <see cref="Mos6502.CanForceStealCurrentCycle"/> stays true because
    ///   <c>_branchTargetFetchPending</c> was set by JUMP, SystemClock skips
    ///   the target FETCH and PC stays fall-through ($FF63) while native JUMP
    ///   is $FF5E.
    /// Acceptance: After LDA abs then taken BNE cycle 0, force-steal is false
    ///   and the next Tick FETCHes the branch target.
    /// </summary>
    [Fact]
    public void TakenBne_CycleZeroAfterDummy_NextTickFetchesTargetNotForceStolen()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x8000] = 0xA9;
        memory[0x8001] = 0x01;
        memory[0x8002] = 0xD0;
        memory[0x8003] = 0x00;
        memory[0x8004] = 0xAD;
        memory[0x8005] = 0x00;
        memory[0x8006] = 0x90;
        memory[0x8007] = 0xD0;
        memory[0x8008] = 0xFB;
        memory[0x8009] = 0xEA;
        memory[0x9000] = 0x06;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        AdvanceToOpcode(cpu, 0xAD);
        for (var i = 0; i < 32 && !(cpu.DebugOpcode == 0xD0 && cpu.DebugCycle == 0); i++)
            cpu.Tick();

        Assert.Equal((byte)0xD0, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.False(
            cpu.CanForceStealCurrentCycle,
            "RasterX 55 mandatory BA lag must not skip the post-JUMP FETCH");
        Assert.NotEqual(cpu.DebugOpcodeAddress, cpu.PC);

        cpu.Tick();
        Assert.Equal((ushort)0x8004, cpu.PC);
        Assert.Equal((byte)0xAD, cpu.DebugOpcode);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 sample 4136566. Taken BNE from $A602 to $A5B8
    ///   page-crosses. VICE BRANCH dummy CLK_INC then a second CLK_INC
    ///   (un-fixed PC) both have check_ba; JUMP has no CLK. Native still
    ///   exports fall-through $A604 at RasterX 55. Managed skipped the
    ///   page-cross extra CLK (CanSteal false while pending) so FETCH of
    ///   $A5B8 ran at x=55. Same-page 2127622 FETCHes at x=55.
    /// Acceptance: LDA #$01 / BNE across a page: after JUMP, PC is
    ///   fall-through and CanSteal is true. Next Tick still fall-through.
    ///   Following Tick FETCHes the target.
    /// </summary>
    [Fact]
    public void TakenPageCrossBne_ExtraClk_StaysAtFallThroughAndIsStealable()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0xF0;
        memory[0xFFFD] = 0x80;
        memory[0x80F0] = 0xA9; // LDA #$01
        memory[0x80F1] = 0x01;
        memory[0x80F2] = 0xD0; // BNE $8114 (fall-through $80F4 + $20)
        memory[0x80F3] = 0x20;
        memory[0x80F4] = 0xEA;
        memory[0x8114] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        AdvanceToOpcode(cpu, 0xD0);
        while (cpu.DebugOpcode == 0xD0 && cpu.DebugCycle != 0)
            cpu.Tick();

        Assert.Equal((byte)0xD0, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((ushort)0x80F4, cpu.PC);
        Assert.True(
            cpu.CanStealCurrentCycle,
            "VICE page-cross dummy CLK_INC has check_ba");

        cpu.Tick();
        Assert.Equal((byte)0xD0, cpu.DebugOpcode);
        Assert.Equal((ushort)0x80F4, cpu.PC);

        cpu.Tick();
        Assert.Equal((ushort)0x8114, cpu.PC);
        Assert.Equal((byte)0xEA, cpu.DebugOpcode);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 sample 4138035. LDA abs,X after taken page-cross BNE
    ///   (short lag) on GET_ABS_X last CLK. VICE CLK_INC has check_ba.
    ///   `_pendingDeferredNzUpdateAfterBranch` made CanSteal false so RasterX
    ///   12 ran INC_PC (nPC=$A5B8 mPC=$A5BB). Short lag without BA still
    ///   FETCHes next (2004230 nPC=$E5AD).
    /// Acceptance: LDX #$00 / LDA #$01 / BNE across a page / LDA $9000,X of
    ///   $2C: last CLK has A=$2C, opcode PC, and CanSteal is true. OnStolenCycle
    ///   then Tick still opcode PC.
    /// </summary>
    [Fact]
    public void LdaAbsXAfterTakenPageCrossBne_DeferredNzTick_IsStealable()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0xF0;
        memory[0xFFFD] = 0x80;
        memory[0x9000] = 0x2C;
        memory[0x80F0] = 0xA2; // LDX #$00
        memory[0x80F1] = 0x00;
        memory[0x80F2] = 0xA9; // LDA #$01
        memory[0x80F3] = 0x01;
        memory[0x80F4] = 0xD0; // BNE $8116 (fall-through $80F6 + $20)
        memory[0x80F5] = 0x20;
        memory[0x8116] = 0xBD; // LDA $9000,X
        memory[0x8117] = 0x00;
        memory[0x8118] = 0x90;
        memory[0x8119] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        var stealer = new ClockBaStealer();
        var clock = new SystemClock();
        clock.Register(cpu);
        clock.Register(stealer);
        cpu.Reset();
        AdvanceToOpcode(cpu, 0xBD);
        while (cpu.DebugOpcode == 0xBD && cpu.DebugCycle != 0)
            clock.Step();

        Assert.Equal((byte)0xBD, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((byte)0x2C, cpu.A);
        var opcodePc = cpu.DebugOpcodeAddress;
        Assert.Equal(opcodePc, cpu.PC);
        Assert.True(
            cpu.CanStealCurrentCycle,
            "VICE GET_ABS_X CLK_INC has check_ba");

        stealer.IsCpuCycleStolen = true;
        clock.Step();
        Assert.Equal((byte)0xBD, cpu.DebugOpcode);
        Assert.Equal(opcodePc, cpu.PC);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 sample 4138175. After taken page-cross BNE then LDA
    ///   abs,X (short lag) then SEC, VICE SBC abs,Y GET_ABS_Y last CLK still
    ///   has opcode PC and pre-op A (nPC=$A5BC nA=$2C). Managed fused
    ///   SBC+INC_PC (mPC=$A5BF mA=$DC). Same SBC after DEX/INY/INX/LDA/SEC
    ///   with Y=0 still fuses (4134601).
    /// Acceptance: LDX #$07 / LDY #$66 / LDA #$01 / BNE across a page /
    ///   LDA $9000,X of $2C / SEC / SBC $90A0,Y of $50 (Y page-crosses):
    ///   last CLK has A=$2C and SBC opcode PC.
    /// </summary>
    [Fact]
    public void SbcAbsYAfterShortLagLdaAbsXThenSec_LastClk_HoldsOpcodePcAndPreOpA()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0xF0;
        memory[0xFFFD] = 0x80;
        memory[0x9007] = 0x2C;
        memory[0x90A0] = 0x00;
        memory[0x9106] = 0x50;
        memory[0x80F0] = 0xA2; // LDX #$07
        memory[0x80F1] = 0x07;
        memory[0x80F2] = 0xA0; // LDY #$66
        memory[0x80F3] = 0x66;
        memory[0x80F4] = 0xA9; // LDA #$01
        memory[0x80F5] = 0x01;
        memory[0x80F6] = 0xD0; // BNE $8118 (fall-through $80F8 + $20)
        memory[0x80F7] = 0x20;
        memory[0x8118] = 0xBD; // LDA $9000,X
        memory[0x8119] = 0x00;
        memory[0x811A] = 0x90;
        memory[0x811B] = 0x38; // SEC
        memory[0x811C] = 0xF9; // SBC $90A0,Y (Y=$66 page-crosses)
        memory[0x811D] = 0xA0;
        memory[0x811E] = 0x90;
        memory[0x811F] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        AdvanceToOpcode(cpu, 0xF9);
        while (cpu.DebugOpcode == 0xF9 && cpu.DebugCycle != 0)
            cpu.Tick();

        Assert.Equal((byte)0xF9, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((byte)0x2C, cpu.A);
        Assert.Equal((ushort)0x811C, cpu.PC);
        Assert.Equal(0, cpu.P & 0x80);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 sample 4134660. Same short-lag LDA abs,X / SEC /
    ///   SBC abs,Y path with Y=$03 (no page-cross). VICE fuses GET_ABS_Y+
    ///   SBC+INC_PC (nPC=$A5BF nA=$06).
    /// Acceptance: LDX #$00 / LDY #$03 / LDA #$01 / BNE across a page /
    ///   LDA $9000,X of $4C / SEC / SBC $9000,Y of $46: last CLK has A=$06
    ///   and PC at opcode+3.
    /// </summary>
    [Fact]
    public void SbcAbsYAfterShortLagLdaAbsXThenSec_NoPageCross_LastClk_Fuses()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0xF0;
        memory[0xFFFD] = 0x80;
        memory[0x9000] = 0x4C;
        memory[0x9003] = 0x46;
        memory[0x80F0] = 0xA2; // LDX #$00
        memory[0x80F1] = 0x00;
        memory[0x80F2] = 0xA0; // LDY #$03
        memory[0x80F3] = 0x03;
        memory[0x80F4] = 0xA9; // LDA #$01
        memory[0x80F5] = 0x01;
        memory[0x80F6] = 0xD0; // BNE $8118
        memory[0x80F7] = 0x20;
        memory[0x8118] = 0xBD; // LDA $9000,X
        memory[0x8119] = 0x00;
        memory[0x811A] = 0x90;
        memory[0x811B] = 0x38; // SEC
        memory[0x811C] = 0xF9; // SBC $9000,Y
        memory[0x811D] = 0x00;
        memory[0x811E] = 0x90;
        memory[0x811F] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        AdvanceToOpcode(cpu, 0xF9);
        while (cpu.DebugOpcode == 0xF9 && cpu.DebugCycle != 0)
            cpu.Tick();

        Assert.Equal((byte)0xF9, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((byte)0x06, cpu.A);
        Assert.Equal((ushort)0x811F, cpu.PC);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 sample 4134601. Same SBC abs,Y after SEC when LDA
    ///   abs,X was reached by fall-through (DEX/INY/INX), Y=0. VICE fuses
    ///   GET_ABS_Y+SBC+INC_PC (nPC=$A5BF nA=$07).
    /// Acceptance: LDA #$4C / LDX #$00 / LDY #$00 / SEC / SBC $9000,Y of
    ///   $45: last CLK has A=$07 and PC at opcode+3.
    /// </summary>
    [Fact]
    public void SbcAbsYAfterSec_NoBranchLag_LastClk_FusesAluAndNextPc()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x9000] = 0x45;
        memory[0x8000] = 0xA9; // LDA #$4C
        memory[0x8001] = 0x4C;
        memory[0x8002] = 0xA2; // LDX #$00
        memory[0x8003] = 0x00;
        memory[0x8004] = 0xA0; // LDY #$00
        memory[0x8005] = 0x00;
        memory[0x8006] = 0x38; // SEC
        memory[0x8007] = 0xF9; // SBC $9000,Y
        memory[0x8008] = 0x00;
        memory[0x8009] = 0x90;
        memory[0x800A] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        AdvanceToOpcode(cpu, 0xF9);
        while (cpu.DebugOpcode == 0xF9 && cpu.DebugCycle != 0)
            cpu.Tick();

        Assert.Equal((byte)0xF9, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((byte)0x07, cpu.A);
        Assert.Equal((ushort)0x800A, cpu.PC);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 sample 4138188. After page-cross SBC abs,Y last-CLK
    ///   hold, not-taken BEQ, CMP#, taken same-page BNE, LDX zp last CLK still
    ///   opcode PC. VICE INC(GET_ZERO)+SET_ZERO_RMW: GET_ZERO CLK_INC still
    ///   has opcode PC (nPC=$A5F7). Managed AdvanceVisiblePc(2) (mPC=$A5F9).
    /// Acceptance: That prefix then LDX $10 / INC $11: INC DebugCycle 2 PC is
    ///   the INC opcode address.
    /// </summary>
    [Fact]
    public void IncZpAfterSbcAbsYThenNotTakenBeqCmpBneLdxZp_Cycle2_HoldsOpcodePc()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0xF0;
        memory[0xFFFD] = 0x80;
        memory[0x0010] = 0x07;
        memory[0x0011] = 0x00;
        memory[0x9007] = 0x2C;
        memory[0x90A0] = 0x00;
        memory[0x9106] = 0x50;
        memory[0x80F0] = 0xA2; // LDX #$07
        memory[0x80F1] = 0x07;
        memory[0x80F2] = 0xA0; // LDY #$66
        memory[0x80F3] = 0x66;
        memory[0x80F4] = 0xA9; // LDA #$01
        memory[0x80F5] = 0x01;
        memory[0x80F6] = 0xD0; // BNE $8118
        memory[0x80F7] = 0x20;
        memory[0x8118] = 0xBD; // LDA $9000,X
        memory[0x8119] = 0x00;
        memory[0x811A] = 0x90;
        memory[0x811B] = 0x38; // SEC
        memory[0x811C] = 0xF9; // SBC $90A0,Y
        memory[0x811D] = 0xA0;
        memory[0x811E] = 0x90;
        memory[0x811F] = 0xF0; // BEQ $8121 (not taken, A=$DC)
        memory[0x8120] = 0x00;
        memory[0x8121] = 0xC9; // CMP #$00
        memory[0x8122] = 0x00;
        memory[0x8123] = 0xD0; // BNE $812D (same page)
        memory[0x8124] = 0x08;
        memory[0x812D] = 0xA6; // LDX $10
        memory[0x812E] = 0x10;
        memory[0x812F] = 0xE6; // INC $11
        memory[0x8130] = 0x11;
        memory[0x8131] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        AdvanceToOpcode(cpu, 0xA6);
        while (cpu.DebugOpcode == 0xA6 && cpu.DebugCycle != 0)
            cpu.Tick();

        Assert.Equal((byte)0xA6, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((ushort)0x812D, cpu.PC);

        AdvanceToOpcode(cpu, 0xE6);
        while (cpu.DebugOpcode == 0xE6 && cpu.DebugCycle != 2)
            cpu.Tick();

        Assert.Equal((byte)0xE6, cpu.DebugOpcode);
        Assert.Equal(2, cpu.DebugCycle);
        Assert.Equal((ushort)0x812F, cpu.PC);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 sample 4138251. After taken same-page BNE (full-length
    ///   dummy then JUMP), LDA abs,X GET_ABS_X data-read CLK still has pre-op
    ///   A (nA=$43). Managed committed A=$2C because trail from the BNE dummy
    ///   fused GET_ABS_X. VICE still pre-loads after a full-length taken
    ///   branch (_deferAbsoluteXLoadCompletionAfterBranch).
    /// Acceptance: LDX #$07 / LDA #$01 / BNE across a page / LDA $9100 of
    ///   $43 / BNE same page / LDA $9000,X of $2C: DebugCycle 1 has A=$43
    ///   and AfterFullLengthTakenBranch.
    /// </summary>
    [Fact]
    public void LdaAbsXAfterTakenSamePageBne_DataRead_HoldsPreOpA()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0xF0;
        memory[0xFFFD] = 0x80;
        memory[0x9007] = 0x2C;
        memory[0x9100] = 0x43;
        memory[0x80F0] = 0xA2; // LDX #$07
        memory[0x80F1] = 0x07;
        memory[0x80F2] = 0xA9; // LDA #$01
        memory[0x80F3] = 0x01;
        memory[0x80F4] = 0xD0; // BNE $8116
        memory[0x80F5] = 0x20;
        memory[0x8116] = 0xAD; // LDA $9100
        memory[0x8117] = 0x00;
        memory[0x8118] = 0x91;
        memory[0x8119] = 0xD0; // BNE $8120
        memory[0x811A] = 0x05;
        memory[0x8120] = 0xBD; // LDA $9000,X
        memory[0x8121] = 0x00;
        memory[0x8122] = 0x90;
        memory[0x8123] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        AdvanceToOpcode(cpu, 0xBD);
        while (cpu.DebugOpcode == 0xBD && cpu.DebugCycle != 1)
            cpu.Tick();

        Assert.Equal((byte)0xBD, cpu.DebugOpcode);
        Assert.Equal(1, cpu.DebugCycle);
        Assert.True(cpu.DebugAfterFullLengthTakenBranch);
        Assert.Equal((byte)0x43, cpu.A);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 sample 4139087. Taken BNE FETCH (cycle 2) frozen
    ///   through bad-line BA (x=41-54). First unstall at x=55 still opcode
    ///   PC (VICE FETCH_OPCODE). Next CLK is BRANCH dummy INC_PC
    ///   (nPC=$A604). Managed cycle 1 kept opcode $A602.
    /// Acceptance: LDA #$01 / BNE same page: steal DebugCycle 2, unstall,
    ///   next Tick is DebugCycle 1 at fall-through.
    /// </summary>
    [Fact]
    public void TakenBne_StolenFetch_Cycle1_ExportsFallThrough()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0xF0;
        memory[0xFFFD] = 0x80;
        memory[0x9100] = 0x43;
        memory[0x80F0] = 0xA2; // LDX #$07
        memory[0x80F1] = 0x07;
        memory[0x80F2] = 0xA9; // LDA #$01
        memory[0x80F3] = 0x01;
        memory[0x80F4] = 0xD0; // BNE $8116
        memory[0x80F5] = 0x20;
        memory[0x8116] = 0xAD; // LDA $9100
        memory[0x8117] = 0x00;
        memory[0x8118] = 0x91;
        memory[0x8119] = 0xD0; // BNE $8120
        memory[0x811A] = 0x05;
        memory[0x8120] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        var stealer = new ClockBaStealer();
        var clock = new SystemClock();
        clock.Register(cpu);
        clock.Register(stealer);
        cpu.Reset();
        AdvanceToOpcode(cpu, 0xAD);
        AdvanceToOpcode(cpu, 0xD0);
        while (cpu.DebugOpcode == 0xD0 && cpu.DebugCycle != 2)
            cpu.Tick();

        Assert.Equal((byte)0xD0, cpu.DebugOpcode);
        Assert.Equal(2, cpu.DebugCycle);
        Assert.Equal((ushort)0x8119, cpu.DebugOpcodeAddress);

        stealer.IsCpuCycleStolen = true;
        for (var i = 0; i < 14; i++)
            clock.Step();

        Assert.Equal(2, cpu.DebugCycle);
        Assert.Equal((ushort)0x8119, cpu.DebugOpcodeAddress);

        stealer.IsCpuCycleStolen = false;
        clock.Step();
        Assert.Equal((byte)0xD0, cpu.DebugOpcode);
        Assert.Equal(2, cpu.DebugCycle);
        Assert.Equal((ushort)0x8119, cpu.DebugOpcodeAddress);

        clock.Step();
        Assert.Equal((byte)0xD0, cpu.DebugOpcode);
        Assert.Equal(1, cpu.DebugCycle);
        Assert.Equal((ushort)0x811B, cpu.PC);

        clock.Step();
        clock.Step();
        Assert.Equal((byte)0xEA, cpu.DebugOpcode);
        Assert.Equal((ushort)0x8120, cpu.PC);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 sample 4134626. Taken BPL after LDA abs,Y (unstolen)
    ///   then INY. VICE last CLK still pre-op Y (nY=$01 nPC=$A5F9). Managed
    ///   fused INY (mY=$02 mPC=$A5FA) when JUMP skipped tgtPend after every
    ///   B9.
    /// Acceptance: LDY #$01 / LDA $9000,Y of $45 / BPL taken / INY: INY last
    ///   CLK has Y=$01 and INY opcode PC.
    /// </summary>
    [Fact]
    public void InyAfterTakenBplAfterLdaAbsY_LastClk_HoldsPreOpY()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x9000] = 0x45;
        memory[0x8000] = 0xA0; // LDY #$01
        memory[0x8001] = 0x01;
        memory[0x8002] = 0xB9; // LDA $9000,Y
        memory[0x8003] = 0x00;
        memory[0x8004] = 0x90;
        memory[0x8005] = 0x10; // BPL $8009
        memory[0x8006] = 0x02;
        memory[0x8007] = 0xEA;
        memory[0x8008] = 0xEA;
        memory[0x8009] = 0xC8; // INY
        memory[0x800A] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        AdvanceToOpcode(cpu, 0xC8);
        while (cpu.DebugOpcode == 0xC8 && cpu.DebugCycle != 0)
            cpu.Tick();

        Assert.Equal((byte)0xC8, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((byte)0x01, cpu.Y);
        Assert.Equal((ushort)0x8009, cpu.PC);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 sample 4146007. LDA abs,Y last CLK then BPL at
    ///   $A5FD. VICE DO_INTERRUPT before BPL FETCH (nS=$F8 nPC=$A5FD
    ///   nLastOp=$B9). Managed FETCHed BPL dummy then JUMP (mPC=$A5F9
    ///   irqSeq=6 mS=$F9 mDelays).
    /// Acceptance: INY / LDA $9000,Y of $45 / BPL taken. I set while delay
    ///   accumulates, then I cleared after LDA last CLK. Next FETCH is BPL:
    ///   IRQ arms/pushes, PC stays at BPL, INY does not commit Y.
    /// </summary>
    [Fact]
    public void BplAfterLdaAbsY_ElapsedIrq_PushesBeforeBplJump()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0xFFFE] = 0x00;
        memory[0xFFFF] = 0x09;
        for (var a = 0x9000; a < 0x9200; a++)
            memory[a] = 0x45;
        memory[0x8000] = 0xC8; // INY
        memory[0x8001] = 0xB9; // LDA $90A0,Y (page-cross when Y=$CB)
        memory[0x8002] = 0xA0;
        memory[0x8003] = 0x90;
        memory[0x8004] = 0x10; // BPL $8000 (taken, same page)
        memory[0x8005] = 0xFA;
        memory[0x0900] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var irq = new InterruptLine(InterruptType.Irq);
        var cpu = new Mos6502(bus);
        var clock = new SystemClock(985_248, cpu, irq);
        clock.Register(cpu);
        cpu.Reset();
        cpu.P = 0x24;
        cpu.S = 0xF9;
        cpu.Y = 0xCA;
        cpu.PC = 0x8000;
        irq.Assert(new RtsTyaIrqSource());

        for (var i = 0; i < 40 && !(cpu.DebugOpcode == 0xB9 && cpu.A == 0x45); i++)
            clock.Step();
        Assert.Equal((byte)0xB9, cpu.DebugOpcode);
        Assert.Equal((byte)0x45, cpu.A);
        for (var i = 0; i < 8 && cpu.DebugOpcode == 0xB9 && cpu.DebugCycle != 0; i++)
            clock.Step();
        Assert.Equal((byte)0xB9, cpu.DebugOpcode);
        Assert.True(clock.DebugIrqDelayCycles >= 2,
            $"irq_delay={clock.DebugIrqDelayCycles} must be elapsed before BPL FETCH");
        cpu.P = (byte)(cpu.P & ~0x04);

        var sAtLda = cpu.S;
        var yAtLda = cpu.Y;
        var trace = $"pre op=${cpu.DebugOpcode:X2} cyc={cpu.DebugCycle} PC=${cpu.PC:X4} S=${cpu.S:X2} Y=${cpu.Y:X2} delay={clock.DebugIrqDelayCycles} bound={cpu.IsInstructionBoundary} delays={cpu.LastOpcodeDelaysInterrupt} en={cpu.LastOpcodeEnablesIrq} supp={cpu.DebugSuppressBootstrapBoundary} irqSeq={cpu.DebugInterruptSequenceRemaining}";
        for (var i = 0; i < 8; i++)
        {
            clock.Step();
            trace += $" | t{i} op=${cpu.DebugOpcode:X2} cyc={cpu.DebugCycle} PC=${cpu.PC:X4} S=${cpu.S:X2} Y=${cpu.Y:X2} delay={clock.DebugIrqDelayCycles} bound={cpu.IsInstructionBoundary} delays={cpu.LastOpcodeDelaysInterrupt} supp={cpu.DebugSuppressBootstrapBoundary} irqSeq={cpu.DebugInterruptSequenceRemaining}";
            if (cpu.S < sAtLda || cpu.DebugInterruptSequenceRemaining > 0)
                break;
        }

        Assert.True(
            cpu.S < sAtLda || cpu.DebugInterruptSequenceRemaining > 0,
            $"IRQ did not start before BPL JUMP; {trace}");
        Assert.Equal((byte)0x45, cpu.A);
        Assert.Equal(yAtLda, cpu.Y);
        Assert.NotEqual((byte)0xC8, cpu.DebugOpcode);
        Assert.NotEqual((ushort)0x8000, cpu.PC);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 sample 4147654. After page-cross SBC abs,Y last CLK
    ///   held opcode PC, not-taken BEQ last CLK still opcode (nPC=$A5BF).
    ///   Managed exported fall-through $A5C1.
    /// Acceptance: LDA abs,X / SEC / SBC $90A0,Y page-cross / BEQ not-taken:
    ///   BEQ last CLK PC is the BEQ opcode address.
    /// </summary>
    [Fact]
    public void NotTakenBeqAfterSbcAbsYPageCross_LastClk_HoldsOpcodePc()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0xF0;
        memory[0xFFFD] = 0x80;
        memory[0x9007] = 0x2C;
        memory[0x90A0] = 0x00;
        memory[0x9106] = 0x50;
        memory[0x80F0] = 0xA2; // LDX #$07
        memory[0x80F1] = 0x07;
        memory[0x80F2] = 0xA0; // LDY #$66
        memory[0x80F3] = 0x66;
        memory[0x80F4] = 0xA9; // LDA #$01
        memory[0x80F5] = 0x01;
        memory[0x80F6] = 0xD0; // BNE $8118
        memory[0x80F7] = 0x20;
        memory[0x8118] = 0xBD; // LDA $9000,X
        memory[0x8119] = 0x00;
        memory[0x811A] = 0x90;
        memory[0x811B] = 0x38; // SEC
        memory[0x811C] = 0xF9; // SBC $90A0,Y
        memory[0x811D] = 0xA0;
        memory[0x811E] = 0x90;
        memory[0x811F] = 0xF0; // BEQ $8121 (not taken)
        memory[0x8120] = 0x00;
        memory[0x8121] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        AdvanceToOpcode(cpu, 0xF0);
        while (cpu.DebugOpcode == 0xF0 && cpu.DebugCycle != 0)
            cpu.Tick();

        Assert.Equal((byte)0xF0, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((ushort)0x811F, cpu.PC);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 sample 4147669. After INC zp last CLK already at
    ///   next-PC, INY last CLK still pre-op Y and opcode PC (nY=$CD
    ///   nPC=$A5F9). Managed fused INY (mY=$CE mPC=$A5FA). Extra deferred
    ///   complete tick lagged following LDA abs,Y (4138197 nA=$50 mA=$DC).
    /// Acceptance: INC zp / INY / LDA abs,Y: INY last CLK has pre-op Y and
    ///   INY opcode PC. Next Tick FETCHes LDA with Y already incremented
    ///   (no extra INY-only host tick).
    /// </summary>
    [Fact]
    public void InyAfterIncZp_LastClk_HoldsPreOpY_NextTickFetchesLda()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0xF0;
        memory[0xFFFD] = 0x80;
        memory[0x0010] = 0x07;
        memory[0x0011] = 0x00;
        memory[0x9007] = 0x2C;
        memory[0x90CD] = 0x45;
        memory[0x90CE] = 0x50;
        memory[0x90A0] = 0x00;
        memory[0x9106] = 0x50;
        memory[0x80F0] = 0xA2; // LDX #$07
        memory[0x80F1] = 0x07;
        memory[0x80F2] = 0xA0; // LDY #$CD
        memory[0x80F3] = 0xCD;
        memory[0x80F4] = 0xA9; // LDA #$01
        memory[0x80F5] = 0x01;
        memory[0x80F6] = 0xD0; // BNE $8118
        memory[0x80F7] = 0x20;
        memory[0x8118] = 0xBD; // LDA $9000,X
        memory[0x8119] = 0x00;
        memory[0x811A] = 0x90;
        memory[0x811B] = 0x38; // SEC
        memory[0x811C] = 0xF9; // SBC $90A0,Y
        memory[0x811D] = 0xA0;
        memory[0x811E] = 0x90;
        memory[0x811F] = 0xF0; // BEQ not taken
        memory[0x8120] = 0x00;
        memory[0x8121] = 0xC9; // CMP #$00
        memory[0x8122] = 0x00;
        memory[0x8123] = 0xD0; // BNE $812D
        memory[0x8124] = 0x08;
        memory[0x812D] = 0xA6; // LDX $10
        memory[0x812E] = 0x10;
        memory[0x812F] = 0xE6; // INC $11
        memory[0x8130] = 0x11;
        memory[0x8131] = 0xC8; // INY
        memory[0x8132] = 0xB9; // LDA $9000,Y
        memory[0x8133] = 0x00;
        memory[0x8134] = 0x90;
        memory[0x8135] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        AdvanceToOpcode(cpu, 0xC8);
        while (cpu.DebugOpcode == 0xC8 && cpu.DebugCycle != 0)
            cpu.Tick();

        Assert.Equal((byte)0xC8, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((byte)0xCD, cpu.Y);
        Assert.Equal((ushort)0x8131, cpu.PC);

        cpu.Tick();
        Assert.Equal((byte)0xCE, cpu.Y);
        Assert.Equal((byte)0xB9, cpu.DebugOpcode);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 sample 4147669 live lockstep. CLC after fused CMP#
    ///   sticks _fuseImpliedAfterIndyLoad through staged LDX zp / INC zp.
    ///   VICE INY last FETCH CLK still pre-op Y and opcode PC (nY=$CD
    ///   nPC=$A5F9). Sticky fuseImplied must not ExecuteOpcode on that CLK.
    /// Acceptance: CMP# / CLC / taken BNE / LDX zp / INC zp / INY: INY last
    ///   CLK has Y=$CD and INY opcode PC. Next Tick FETCHes LDA with Y=$CE
    ///   (no extra INY-only host tick).
    /// </summary>
    [Fact]
    public void InyAfterHeldIncZp_FuseImpliedSticky_LastClk_HoldsPreOpY()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0xE8;
        memory[0xFFFD] = 0x80;
        memory[0x0010] = 0x07;
        memory[0x0011] = 0x00;
        memory[0x0020] = 0xCD;
        memory[0x0021] = 0x01;
        memory[0x9007] = 0x2C;
        memory[0x90CD] = 0x45;
        memory[0x90CE] = 0x50;
        memory[0x90A0] = 0x00;
        memory[0x916D] = 0x00;
        memory[0x80E8] = 0xA5; // LDA $21
        memory[0x80E9] = 0x21;
        memory[0x80EA] = 0x48; // PHA
        memory[0x80EB] = 0x18; // CLC (fuses after PHA; sticks fuseImplied)
        memory[0x80EC] = 0x68; // PLA
        memory[0x80ED] = 0xA4; // LDY $20
        memory[0x80EE] = 0x20;
        memory[0x80EF] = 0xA6; // LDX $10
        memory[0x80F0] = 0x10;
        memory[0x80F1] = 0xD0; // BNE $8118
        memory[0x80F2] = 0x25;
        memory[0x8118] = 0xBD; // LDA $9000,X
        memory[0x8119] = 0x00;
        memory[0x811A] = 0x90;
        memory[0x811B] = 0x38; // SEC (fuseImplied keeps this fused, not a 1761 clear)
        memory[0x811C] = 0xF9; // SBC $90A0,Y page-cross
        memory[0x811D] = 0xA0;
        memory[0x811E] = 0x90;
        memory[0x811F] = 0xF0; // BEQ not taken
        memory[0x8120] = 0x00;
        memory[0x8121] = 0xC9; // CMP #$00 (soft-defer in nonOvl; does not clear fuseImplied)
        memory[0x8122] = 0x00;
        memory[0x8123] = 0xD0; // BNE $812D
        memory[0x8124] = 0x08;
        memory[0x812D] = 0xA6; // LDX $10
        memory[0x812E] = 0x10;
        memory[0x812F] = 0xE6; // INC $11
        memory[0x8130] = 0x11;
        memory[0x8131] = 0xC8; // INY
        memory[0x8132] = 0xB9; // LDA $9000,Y
        memory[0x8133] = 0x00;
        memory[0x8134] = 0x90;
        memory[0x8135] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        AdvanceToOpcode(cpu, 0xC8);
        while (cpu.DebugOpcode == 0xC8 && cpu.DebugCycle != 0)
            cpu.Tick();

        Assert.Equal((byte)0xC8, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.True(
            cpu.Y == 0xCD && cpu.PC == 0x8131,
            $"INY last CLK Y=${cpu.Y:X2} PC=${cpu.PC:X4} fuseImplied={cpu.DebugFuseImpliedAfterIndyLoad} inySoft={cpu.DebugInySoftAfterHeldInc}");

        cpu.Tick();
        Assert.Equal((byte)0xCE, cpu.Y);
        Assert.Equal((byte)0xB9, cpu.DebugOpcode);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 sample 4150680. Held INY then taken same-page BPL
    ///   frozen at DebugCycle 2 through a badline. Dummy CLK exports
    ///   fall-through; VICE JUMP has no CLK so the last host tick already
    ///   shows the INY target (nPC=$A5F9). Managed stayed at fall-through
    ///   $A5FF with tgtPend. Unstalled INY/BNE still exports fall-through.
    /// Acceptance: After INC zp / INY / BPL taken to INY, steal at cycle 2,
    ///   then BPL last CLK PC is the INY opcode, not BPL+2.
    /// </summary>
    [Fact]
    public void StolenTakenBplAfterHeldIny_JumpTick_ExportsTarget()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0xE8;
        memory[0xFFFD] = 0x80;
        memory[0x0010] = 0x07;
        memory[0x0011] = 0x00;
        memory[0x0020] = 0x6D;
        memory[0x0021] = 0x01;
        memory[0x9007] = 0x2C;
        memory[0x90A0] = 0x00;
        memory[0x910D] = 0x00;
        memory[0x80E8] = 0xA5; // LDA $21
        memory[0x80E9] = 0x21;
        memory[0x80EA] = 0x48; // PHA
        memory[0x80EB] = 0x18; // CLC
        memory[0x80EC] = 0x68; // PLA
        memory[0x80ED] = 0xA4; // LDY $20 (Y=$6D so INY leaves N clear)
        memory[0x80EE] = 0x20;
        memory[0x80EF] = 0xA6; // LDX $10
        memory[0x80F0] = 0x10;
        memory[0x80F1] = 0xD0; // BNE $8118
        memory[0x80F2] = 0x25;
        memory[0x8118] = 0xBD; // LDA $9000,X
        memory[0x8119] = 0x00;
        memory[0x811A] = 0x90;
        memory[0x811B] = 0x38; // SEC
        memory[0x811C] = 0xF9; // SBC $90A0,Y page-cross
        memory[0x811D] = 0xA0;
        memory[0x811E] = 0x90;
        memory[0x811F] = 0xF0; // BEQ not taken
        memory[0x8120] = 0x00;
        memory[0x8121] = 0xC9; // CMP #$00
        memory[0x8122] = 0x00;
        memory[0x8123] = 0xD0; // BNE $812D
        memory[0x8124] = 0x08;
        memory[0x812D] = 0xA6; // LDX $10
        memory[0x812E] = 0x10;
        memory[0x812F] = 0xE6; // INC $11
        memory[0x8130] = 0x11;
        memory[0x8131] = 0xC8; // INY
        memory[0x8132] = 0x10; // BPL $8131
        memory[0x8133] = 0xFD;
        memory[0x8134] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        AdvanceToOpcode(cpu, 0xC8);
        while (cpu.DebugOpcode == 0xC8 && cpu.DebugCycle != 0)
            cpu.Tick();
        Assert.Equal((byte)0x6D, cpu.Y);
        Assert.Equal((ushort)0x8131, cpu.PC);

        cpu.Tick();
        Assert.Equal((byte)0x10, cpu.DebugOpcode);
        Assert.True(
            cpu.DebugCycle >= 2,
            $"BPL after held INY must be full-length (cycle {cpu.DebugCycle})");

        for (var i = 0; i < 13; i++)
            cpu.OnStolenCycle();

        while (cpu.DebugOpcode == 0x10 && cpu.DebugCycle != 0)
            cpu.Tick();

        Assert.Equal((byte)0x10, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((ushort)0x8131, cpu.PC);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 sample 4150680. LDA abs,Y then taken same-page BPL
    ///   frozen at DebugCycle 2. Dummy CLK exports fall-through; VICE JUMP
    ///   has no CLK so the last host tick already shows the target
    ///   (nPC=$A5F9 nLastOp=$C8). Managed previous is $B9 so the stolen-B9
    ///   JUMP tick kept fall-through $A5FF. Page-cross stolen B9 still
    ///   FETCHes on the following tick (4139089).
    /// Acceptance: LDY #$01 / LDA $9000,Y of $45 / BPL taken to LDA: steal
    ///   at cycle 2, then BPL last CLK PC is the LDA opcode, not BPL+2.
    /// </summary>
    [Fact]
    public void StolenTakenBplAfterLdaAbsY_SamePage_JumpTick_ExportsTarget()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0xE8;
        memory[0xFFFD] = 0x80;
        memory[0x0010] = 0x07;
        memory[0x0011] = 0x00;
        memory[0x0020] = 0x6D;
        memory[0x0021] = 0x01;
        memory[0x9007] = 0x2C;
        memory[0x906E] = 0x45;
        memory[0x90A0] = 0x00;
        memory[0x910D] = 0x00;
        memory[0x80E8] = 0xA5; // LDA $21
        memory[0x80E9] = 0x21;
        memory[0x80EA] = 0x48; // PHA
        memory[0x80EB] = 0x18; // CLC
        memory[0x80EC] = 0x68; // PLA
        memory[0x80ED] = 0xA4; // LDY $20
        memory[0x80EE] = 0x20;
        memory[0x80EF] = 0xA6; // LDX $10
        memory[0x80F0] = 0x10;
        memory[0x80F1] = 0xD0; // BNE $8118
        memory[0x80F2] = 0x25;
        memory[0x8118] = 0xBD; // LDA $9000,X
        memory[0x8119] = 0x00;
        memory[0x811A] = 0x90;
        memory[0x811B] = 0x38; // SEC
        memory[0x811C] = 0xF9; // SBC $90A0,Y
        memory[0x811D] = 0xA0;
        memory[0x811E] = 0x90;
        memory[0x811F] = 0xF0; // BEQ not taken
        memory[0x8120] = 0x00;
        memory[0x8121] = 0xC9; // CMP #$00
        memory[0x8122] = 0x00;
        memory[0x8123] = 0xD0; // BNE $812D
        memory[0x8124] = 0x08;
        memory[0x812D] = 0xA6; // LDX $10
        memory[0x812E] = 0x10;
        memory[0x812F] = 0xE6; // INC $11
        memory[0x8130] = 0x11;
        memory[0x8131] = 0xC8; // INY
        memory[0x8132] = 0xB9; // LDA $9000,Y
        memory[0x8133] = 0x00;
        memory[0x8134] = 0x90;
        memory[0x8135] = 0x10; // BPL $8131
        memory[0x8136] = 0xFA;
        memory[0x8137] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        AdvanceToOpcode(cpu, 0xB9);
        while (cpu.DebugOpcode == 0xB9 && cpu.DebugCycle != 0)
            cpu.Tick();
        cpu.Tick();
        Assert.Equal((byte)0x10, cpu.DebugOpcode);
        Assert.True(
            cpu.DebugCycle >= 2,
            $"BPL after LDA abs,Y must be full-length (cycle {cpu.DebugCycle} prev=${cpu.DebugPreviousOpcode:X2})");
        Assert.Equal((byte)0xB9, cpu.DebugPreviousOpcode);

        for (var i = 0; i < 13; i++)
            cpu.OnStolenCycle();

        while (cpu.DebugOpcode == 0x10 && cpu.DebugCycle != 0)
            cpu.Tick();

        Assert.Equal((byte)0x10, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((ushort)0x8131, cpu.PC);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 sample 4150686. After stolen same-page BPL JUMP to
    ///   INY then fused INY, LDA abs,Y data-read is VICE GET_ABS_Y (nA=$4E
    ///   mA=$4F). Blanket trail>=1/implied-previous A-commit moved
    ///   FirstMismatch back to 4138196. Following unstolen BPL dummy CLK
    ///   is fall-through (Wolf64 4150689 nPC=$A5FF).
    /// Acceptance: Stolen BPL JUMP to INY, then LDA $9000,Y of $4E: data-read
    ///   CLK A is $4E. Next BPL DebugCycle 1 PC is BPL+2.
    /// </summary>
    [Fact]
    public void LdaAbsYAfterStolenSamePageBplThenIny_DataRead_CommitsA()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0xE8;
        memory[0xFFFD] = 0x80;
        memory[0x0010] = 0x07;
        memory[0x0011] = 0x00;
        memory[0x0020] = 0x6D;
        memory[0x0021] = 0x01;
        memory[0x9007] = 0x2C;
        memory[0x906E] = 0x45;
        memory[0x906F] = 0x4E;
        memory[0x9070] = 0xD4;
        memory[0x90A0] = 0x00;
        memory[0x910D] = 0x00;
        memory[0x9170] = 0x4C;
        memory[0x80E8] = 0xA5; // LDA $21
        memory[0x80E9] = 0x21;
        memory[0x80EA] = 0x48; // PHA
        memory[0x80EB] = 0x18; // CLC
        memory[0x80EC] = 0x68; // PLA
        memory[0x80ED] = 0xA4; // LDY $20
        memory[0x80EE] = 0x20;
        memory[0x80EF] = 0xA6; // LDX $10
        memory[0x80F0] = 0x10;
        memory[0x80F1] = 0xD0; // BNE $8118
        memory[0x80F2] = 0x25;
        memory[0x8118] = 0xBD; // LDA $9000,X
        memory[0x8119] = 0x00;
        memory[0x811A] = 0x90;
        memory[0x811B] = 0x38; // SEC
        memory[0x811C] = 0xF9; // SBC $90A0,Y
        memory[0x811D] = 0xA0;
        memory[0x811E] = 0x90;
        memory[0x811F] = 0xF0; // BEQ not taken
        memory[0x8120] = 0x00;
        memory[0x8121] = 0xC9; // CMP #$00
        memory[0x8122] = 0x00;
        memory[0x8123] = 0xD0; // BNE $812D
        memory[0x8124] = 0x08;
        memory[0x812D] = 0xA6; // LDX $10
        memory[0x812E] = 0x10;
        memory[0x812F] = 0xE6; // INC $11
        memory[0x8130] = 0x11;
        memory[0x8131] = 0xC8; // INY
        memory[0x8132] = 0xB9; // LDA $9000,Y
        memory[0x8133] = 0x00;
        memory[0x8134] = 0x90;
        memory[0x8135] = 0x10; // BPL $8131 (not taken when N set)
        memory[0x8136] = 0xFA;
        memory[0x8137] = 0xB9; // LDA $9100,Y
        memory[0x8138] = 0x00;
        memory[0x8139] = 0x91;
        memory[0x813A] = 0xD0; // BNE $80BD (page-cross $81 to $80)
        memory[0x813B] = 0x81;
        memory[0x813C] = 0xEA;
        memory[0x80BD] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        AdvanceToOpcode(cpu, 0xB9);
        while (cpu.DebugOpcode == 0xB9 && cpu.DebugCycle != 0)
            cpu.Tick();
        cpu.Tick();
        Assert.Equal((byte)0x10, cpu.DebugOpcode);
        while (cpu.DebugOpcode == 0x10 && cpu.DebugCycle < 2)
            cpu.Tick();
        for (var i = 0; i < 13; i++)
            cpu.OnStolenCycle();
        while (cpu.DebugOpcode == 0x10 && cpu.DebugCycle != 0)
            cpu.Tick();
        Assert.Equal((ushort)0x8131, cpu.PC);

        var aAtJump = cpu.A;
        Assert.NotEqual((byte)0x4E, aAtJump);

        cpu.Tick();
        Assert.Equal((byte)0xC8, cpu.DebugOpcode);
        while (cpu.DebugOpcode == 0xC8 && cpu.DebugCycle != 0)
            cpu.Tick();
        cpu.Tick();
        Assert.Equal((byte)0xB9, cpu.DebugOpcode);
        while (cpu.DebugOpcode == 0xB9 && cpu.DebugCycle != 1)
            cpu.Tick();

        Assert.Equal((byte)0xB9, cpu.DebugOpcode);
        Assert.Equal(1, cpu.DebugCycle);
        Assert.Equal((byte)0x4E, cpu.A);

        while (cpu.DebugOpcode == 0xB9 && cpu.DebugCycle != 0)
            cpu.Tick();
        Assert.Equal((byte)0xB9, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((ushort)0x8135, cpu.PC);

        cpu.Tick();
        Assert.Equal((byte)0x10, cpu.DebugOpcode);
        Assert.Equal((byte)0xB9, cpu.DebugPreviousOpcode);
        while (cpu.DebugOpcode == 0x10 && cpu.DebugCycle != 1)
            cpu.Tick();
        Assert.Equal((byte)0x10, cpu.DebugOpcode);
        Assert.Equal(1, cpu.DebugCycle);
        Assert.Equal((ushort)0x8137, cpu.PC);

        while (cpu.DebugOpcode == 0x10 && cpu.DebugCycle != 0)
            cpu.Tick();
        Assert.Equal((byte)0x10, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((ushort)0x8131, cpu.PC);

        cpu.Tick();
        Assert.Equal((byte)0xC8, cpu.DebugOpcode);
        while (cpu.DebugOpcode == 0xC8 && cpu.DebugCycle != 0)
            cpu.Tick();
        cpu.Tick();
        Assert.Equal((byte)0xB9, cpu.DebugOpcode);
        while (cpu.DebugOpcode == 0xB9 && cpu.DebugCycle != 1)
            cpu.Tick();
        Assert.Equal((byte)0xB9, cpu.DebugOpcode);
        Assert.Equal(1, cpu.DebugCycle);
        Assert.Equal((byte)0xD4, cpu.A);

        while (cpu.DebugOpcode == 0xB9 && cpu.DebugCycle != 0)
            cpu.Tick();
        Assert.Equal((byte)0xB9, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((byte)0xD4, cpu.A);
        Assert.Equal(0x80, cpu.P & 0x80);

        cpu.Tick();
        Assert.Equal((byte)0x10, cpu.DebugOpcode);
        while (cpu.DebugOpcode == 0x10 && cpu.DebugCycle != 0)
            cpu.Tick();
        cpu.Tick();
        Assert.Equal((byte)0xB9, cpu.DebugOpcode);
        Assert.Equal((byte)0x10, cpu.DebugPreviousOpcode);
        while (cpu.DebugOpcode == 0xB9 && cpu.DebugCycle != 1)
            cpu.Tick();
        Assert.Equal((byte)0xB9, cpu.DebugOpcode);
        Assert.Equal(1, cpu.DebugCycle);
        Assert.Equal((byte)0x4C, cpu.A);

        while (cpu.DebugOpcode == 0xB9 && cpu.DebugCycle != 0)
            cpu.Tick();
        cpu.Tick();
        Assert.Equal((byte)0xD0, cpu.DebugOpcode);
        Assert.Equal((byte)0xB9, cpu.DebugPreviousOpcode);
        while (cpu.DebugOpcode == 0xD0 && cpu.DebugCycle != 0)
            cpu.Tick();
        Assert.Equal((byte)0xD0, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((ushort)0x813C, cpu.PC);

        cpu.Tick();
        Assert.Equal((ushort)0x80BD, cpu.PC);
        Assert.Equal((byte)0xEA, cpu.DebugOpcode);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 sample 4134649. Unstolen taken page-cross BNE after
    ///   LDA abs,Y when dummy was collapsed onto the JUMP tick. VICE still
    ///   CLK_INCs the un-fixed PC (nPC=$A604). Staged dummy (4150708) FETCHes
    ///   next; that path is LdaAbsYAfterStolenSamePageBplThenIny. LDA # still
    ///   takes the extra CLK (4136566).
    /// Acceptance: Not-taken BPL then LDA abs,Y then taken page-cross BNE:
    ///   last CLK is fall-through; the next Tick stays fall-through; the
    ///   following Tick FETCHes the target.
    /// </summary>
    [Fact]
    public void UnstolenTakenPageCrossBneAfterLdaAbsY_JumpTickThenFetch_ExportsTarget()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0xF0;
        memory[0xFFFD] = 0x80;
        memory[0x9000] = 0x4C;
        memory[0x80F0] = 0xA0; // LDY #$00
        memory[0x80F1] = 0x00;
        memory[0x80F2] = 0xA9; // LDA #$80 (N set)
        memory[0x80F3] = 0x80;
        memory[0x80F4] = 0x10; // BPL not taken
        memory[0x80F5] = 0x00;
        memory[0x80F6] = 0xB9; // LDA $9000,Y
        memory[0x80F7] = 0x00;
        memory[0x80F8] = 0x90;
        memory[0x80F9] = 0xD0; // BNE $811B (fall-through $80FB + $20)
        memory[0x80FA] = 0x20;
        memory[0x80FB] = 0xEA;
        memory[0x811B] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        AdvanceToOpcode(cpu, 0xD0);
        Assert.Equal((byte)0xB9, cpu.DebugPreviousOpcode);
        while (cpu.DebugOpcode == 0xD0 && cpu.DebugCycle != 0)
            cpu.Tick();

        Assert.Equal((byte)0xD0, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((ushort)0x80FB, cpu.PC);

        cpu.Tick();
        Assert.Equal((byte)0xD0, cpu.DebugOpcode);
        Assert.Equal((ushort)0x80FB, cpu.PC);

        cpu.Tick();
        Assert.Equal((ushort)0x811B, cpu.PC);
        Assert.Equal((byte)0xEA, cpu.DebugOpcode);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 sample 4154106. Taken BEQ then STA abs,Y last CLK
    ///   already at next-PC; DEC zp GET_ZERO cycle 2 still has opcode PC and
    ///   pre-op P (nPC=$A60C nP=$23). Managed AdvanceVisiblePc plus NZ
    ///   (mPC=$A60E mP=$21). STA zp / BCS prefix makes the branch FETCH
    ///   full-length like 2423401. Do not hold every $99 trail>=1
    ///   (2125150 nPC=$BEAA).
    /// Acceptance: SEC / STA zp / BCS / STA $9000,Y / DEC $10 of $02: STA
    ///   FETCH has after-full-length; DEC DebugCycle 2 PC is the DEC opcode
    ///   and P still has Z set from LDA #$00.
    /// </summary>
    [Fact]
    public void DecZpAfterStaAbsYFollowingTakenBeq_Cycle2_HoldsOpcodePcAndPreOpP()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x0010] = 0x02;
        memory[0x8000] = 0xA9; // LDA #$00 (Z set)
        memory[0x8001] = 0x00;
        memory[0x8002] = 0x38; // SEC
        memory[0x8003] = 0x85; // STA $02
        memory[0x8004] = 0x02;
        memory[0x8005] = 0xB0; // BCS $8007
        memory[0x8006] = 0x00;
        memory[0x8007] = 0x99; // STA $9000,Y
        memory[0x8008] = 0x00;
        memory[0x8009] = 0x90;
        memory[0x800A] = 0xC6; // DEC $10
        memory[0x800B] = 0x10;
        memory[0x800C] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        for (var i = 0; i < 48 && cpu.DebugOpcode != 0x99; i++)
            cpu.Tick();
        Assert.Equal((byte)0x99, cpu.DebugOpcode);
        Assert.True(
            cpu.DebugAfterFullLengthTakenBranch,
            "STA abs,Y after BCS must be the full-length after-branch FETCH");
        AdvanceToOpcode(cpu, 0xC6);
        Assert.Equal((byte)0x99, cpu.DebugPreviousOpcode);
        while (cpu.DebugOpcode == 0xC6 && cpu.DebugCycle != 2)
            cpu.Tick();

        Assert.Equal((byte)0xC6, cpu.DebugOpcode);
        Assert.Equal(2, cpu.DebugCycle);
        Assert.Equal((ushort)0x800A, cpu.PC);
        Assert.Equal(0x02, cpu.P & 0x02);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 sample 2125150. DEC zp after STA abs,Y with no
    ///   taken-branch prefix. Native GET_ZERO cycle 2 already opcode+2
    ///   (nPC=$BEAA). Holding every previous $99 regressed this sample.
    /// Acceptance: LDA #$33 / STA $9000,Y / DEC $10: DEC DebugCycle 2 PC is
    ///   opcode+2.
    /// </summary>
    [Fact]
    public void DecZpAfterStaAbsYWithoutTakenBranch_Cycle2_AdvancesPc()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x0010] = 0x02;
        memory[0x8000] = 0xA9; // LDA #$33
        memory[0x8001] = 0x33;
        memory[0x8002] = 0x99; // STA $9000,Y
        memory[0x8003] = 0x00;
        memory[0x8004] = 0x90;
        memory[0x8005] = 0xC6; // DEC $10
        memory[0x8006] = 0x10;
        memory[0x8007] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        AdvanceToOpcode(cpu, 0xC6);
        Assert.Equal((byte)0x99, cpu.DebugPreviousOpcode);
        while (cpu.DebugOpcode == 0xC6 && cpu.DebugCycle != 2)
            cpu.Tick();

        Assert.Equal((byte)0xC6, cpu.DebugOpcode);
        Assert.Equal(2, cpu.DebugCycle);
        Assert.Equal((ushort)0x8007, cpu.PC);

    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 sample 4154411. After DEC zp last CLK already at
    ///   next-PC, LDX # GET_IMM last CLK still has opcode PC and pre-op X
    ///   (nPC=$ADA6 nX=$01). Managed fused X+INC_PC (mPC=$ADA8 mX=$00).
    ///   LDA # after full-length taken branch still fuses (2423401).
    /// Acceptance: Taken BNE / DEC $10 / LDX #$42: LDX last CLK X is still
    ///   the pre-op value and PC is the LDX opcode.
    /// </summary>
    [Fact]
    public void LdxImmAfterDecZpFollowingTakenBne_LastClk_HoldsPreOpXAndOpcodePc()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x0010] = 0x02;
        memory[0x8000] = 0xA9; // LDA #$01
        memory[0x8001] = 0x01;
        memory[0x8002] = 0xD0; // BNE $8006
        memory[0x8003] = 0x02;
        memory[0x8004] = 0xEA;
        memory[0x8006] = 0xC6; // DEC $10
        memory[0x8007] = 0x10;
        memory[0x8008] = 0xA2; // LDX #$42
        memory[0x8009] = 0x42;
        memory[0x800A] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        AdvanceToOpcode(cpu, 0xA2);
        Assert.Equal((byte)0xC6, cpu.DebugPreviousOpcode);
        var preOpX = cpu.X;
        Assert.NotEqual((byte)0x42, preOpX);
        while (cpu.DebugOpcode == 0xA2 && cpu.DebugCycle != 0)
            cpu.Tick();

        Assert.Equal((byte)0xA2, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal(preOpX, cpu.X);
        Assert.Equal((ushort)0x8008, cpu.PC);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 snapshot lockstep sample 7. INY / taken same-page
    ///   BNE / STA (zp),Y. VICE FETCH_OPCODE is two CLK at the STA opcode
    ///   PC, then ST INC_PC(2) with no CLK before INT_IND_Y_W. Native second
    ///   STA sample still $0F5F. Managed AdvanceVisiblePc at cycle 4
    ///   (mPC=$0F61).
    /// Acceptance: INY / BNE back to STA ($10),Y: STA DebugCycle 4 still
    ///   exports the STA opcode PC.
    /// </summary>
    [Fact]
    public void StaIndYAfterTakenBne_Cycle4_HoldsOpcodePc()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x02;
        memory[0xFFFD] = 0x80;
        memory[0x0010] = 0x00;
        memory[0x0011] = 0x04;
        memory[0x8000] = 0x91; // STA ($10),Y
        memory[0x8001] = 0x10;
        memory[0x8002] = 0xC8; // INY
        memory[0x8003] = 0xD0; // BNE $8000
        memory[0x8004] = 0xFB;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        cpu.Y = 0xD6;
        cpu.A = 0x00;
        AdvanceToOpcode(cpu, 0x91);
        Assert.Equal((byte)0xD0, cpu.DebugPreviousOpcode);
        Assert.Equal(5, cpu.DebugCycle);
        Assert.Equal((ushort)0x8000, cpu.PC);

        cpu.Tick();

        Assert.Equal((byte)0x91, cpu.DebugOpcode);
        Assert.Equal(4, cpu.DebugCycle);
        Assert.Equal((ushort)0x8000, cpu.PC);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001, TEST: TEST-X64SC-LOCKSTEP-001.
    /// Upstream VICE 6510dtvcore.c executes both FETCH_OPCODE clocks before
    /// ST increments PC and begins INT_IND_Y_W. The second fetch therefore
    /// still exports the opcode PC for every STA (zp),Y predecessor.
    /// </summary>
    [Fact]
    public void StaIndY_CycleFour_HoldsOpcodePc()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x0010] = 0x00;
        memory[0x0011] = 0x04;
        memory[0x8000] = 0x91; // STA ($10),Y
        memory[0x8001] = 0x10;
        memory[0x8002] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        cpu.Y = 0x20;
        cpu.A = 0x5A;
        AdvanceToOpcode(cpu, 0x91);
        Assert.Equal(5, cpu.DebugCycle);
        Assert.Equal((ushort)0x8000, cpu.PC);

        cpu.Tick();

        Assert.Equal((byte)0x91, cpu.DebugOpcode);
        Assert.Equal(4, cpu.DebugCycle);
        Assert.Equal((ushort)0x8000, cpu.PC);

        cpu.Tick();

        Assert.Equal((byte)0x91, cpu.DebugOpcode);
        Assert.Equal(3, cpu.DebugCycle);
        Assert.Equal((ushort)0x8002, cpu.PC);

        cpu.Tick();
        cpu.Tick();
        cpu.Tick();

        Assert.Equal((byte)0x91, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.True(cpu.ConsumedViceClockThisTick);
        Assert.False(cpu.IsInstructionBoundary);

        var stackBeforeInterrupt = cpu.S;
        cpu.P &= 0xFB;
        cpu.TrySampleInterruptBeforeFetch = cpu.Irq;
        cpu.Tick();

        // Ordinary pre-FETCH IRQ entry has completed VICE's two LOAD_DUMMY
        // checkpoints. The five remaining clocks begin with PUSH high.
        Assert.Equal(5, cpu.DebugInterruptSequenceRemaining);
        Assert.Equal(stackBeforeInterrupt, cpu.S);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001, TEST: TEST-X64SC-LOCKSTEP-001.
    /// Use case: Wolf64 sample 13. After INY / taken same-page BNE /
    ///   STA (zp),Y, VICE's final INY sample remains at the INY opcode with
    ///   the pre-operation Y value. The next host tick commits INY and fetches
    ///   the following BNE without an extra INY-only tick.
    /// Acceptance: the post-STA INY final sample holds Y=$D7 and PC=$8002;
    ///   the next Tick fetches BNE with Y=$D8.
    /// </summary>
    [Fact]
    public void InyAfterHeldStaIndY_LastClk_HoldsPreOpY_NextTickFetchesBne()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x02;
        memory[0xFFFD] = 0x80;
        memory[0x0010] = 0x00;
        memory[0x0011] = 0x04;
        memory[0x8000] = 0x91; // STA ($10),Y
        memory[0x8001] = 0x10;
        memory[0x8002] = 0xC8; // INY
        memory[0x8003] = 0xD0; // BNE $8000
        memory[0x8004] = 0xFB;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        cpu.Y = 0xD6;
        cpu.A = 0x00;

        AdvanceToOpcode(cpu, 0x91);
        while (cpu.DebugOpcode == 0x91)
            cpu.Tick();

        Assert.Equal((byte)0xC8, cpu.DebugOpcode);
        while (cpu.DebugOpcode == 0xC8 && cpu.DebugCycle != 0)
            cpu.Tick();

        Assert.Equal((byte)0xC8, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((byte)0xD7, cpu.Y);
        Assert.Equal((ushort)0x8002, cpu.PC);

        cpu.Tick();

        Assert.Equal((byte)0xD8, cpu.Y);
        Assert.Equal((byte)0xD0, cpu.DebugOpcode);
        Assert.Equal((ushort)0x8003, cpu.PC);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 sample 2127622. Taken same-page BNE dummy then JUMP
    ///   (no extra CLK). At RasterX 55 native already FETCHes the target
    ///   ($BEB2). Page-cross extra must not be added.
    /// Acceptance: LDA #$01 / BNE same page: after JUMP, next Tick FETCHes
    ///   the target.
    /// </summary>
    [Fact]
    public void TakenSamePageBne_AfterJump_FetchesTargetOnNextTick()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x8000] = 0xA9; // LDA #$01
        memory[0x8001] = 0x01;
        memory[0x8002] = 0xD0; // BNE $800A
        memory[0x8003] = 0x06;
        memory[0x8004] = 0xEA;
        memory[0x800A] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        AdvanceToOpcode(cpu, 0xD0);
        while (cpu.DebugOpcode == 0xD0 && cpu.DebugCycle != 0)
            cpu.Tick();

        Assert.Equal((byte)0xD0, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);

        cpu.Tick();
        Assert.Equal((ushort)0x800A, cpu.PC);
        Assert.Equal((byte)0xEA, cpu.DebugOpcode);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001.
    /// Use case: Wolf64 sample 2050006. Taken BNE dummy is still at DebugCycle 1
    ///   (opcode PC) through bad-line BA. At RasterX 55 native exports
    ///   fall-through while mandatory BA lag is still asserted. Force-steal
    ///   on cycle 1 would skip that dummy CLK.
    /// Acceptance: At BNE DebugCycle 1 after LDA abs, CanForceSteal is false;
    ///   the next Tick exports fall-through $8009.
    /// </summary>
    [Fact]
    public void TakenBne_DummyCycle_IsNotForceStealable_NextTickExportsFallThrough()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x8000] = 0xA9;
        memory[0x8001] = 0x01;
        memory[0x8002] = 0xD0;
        memory[0x8003] = 0x00;
        memory[0x8004] = 0xAD;
        memory[0x8005] = 0x00;
        memory[0x8006] = 0x90;
        memory[0x8007] = 0xD0;
        memory[0x8008] = 0xFB;
        memory[0x8009] = 0xEA;
        memory[0x9000] = 0x06;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        AdvanceToOpcode(cpu, 0xAD);
        for (var i = 0; i < 32 && !(cpu.DebugOpcode == 0xD0 && cpu.DebugCycle == 1); i++)
            cpu.Tick();

        Assert.Equal((byte)0xD0, cpu.DebugOpcode);
        Assert.Equal(1, cpu.DebugCycle);
        Assert.False(cpu.CanForceStealCurrentCycle);

        cpu.Tick();
        Assert.Equal((byte)0xD0, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((ushort)0x8009, cpu.PC);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001.
    /// Use case: Wolf64 sample 2050510. VICE FETCH_OPCODE for BNE is two
    ///   check_ba+CLK_INC samples at the opcode PC, then BRANCH INC_PC(2)
    ///   and a dummy LOAD+CLK_INC that exports fall-through. A bad-line
    ///   stall at DebugCycle 2 is still in FETCH. First unstall CLK must
    ///   keep opcode PC $8007, not dummy-INC_PC to $8009.
    /// Acceptance: OnStolenCycle at BNE DebugCycle 2 then Tick keeps
    ///   opcode PC and cycle 2; the following Tick exports fall-through.
    /// </summary>
    [Fact]
    public void TakenBne_StolenExtraFetchClk_NextTickKeepsOpcodePc()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x8000] = 0xA9;
        memory[0x8001] = 0x01;
        memory[0x8002] = 0xD0;
        memory[0x8003] = 0x00;
        memory[0x8004] = 0xAD;
        memory[0x8005] = 0x00;
        memory[0x8006] = 0x90;
        memory[0x8007] = 0xD0;
        memory[0x8008] = 0xFB;
        memory[0x8009] = 0xEA;
        memory[0x9000] = 0x06;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        AdvanceToOpcode(cpu, 0xAD);
        for (var i = 0; i < 32 && !(cpu.DebugOpcode == 0xD0 && cpu.DebugCycle == 2); i++)
            cpu.Tick();

        Assert.Equal((byte)0xD0, cpu.DebugOpcode);
        Assert.Equal(2, cpu.DebugCycle);
        for (var steal = 0; steal < 15; steal++)
            cpu.OnStolenCycle();
        cpu.Tick();
        Assert.Equal((ushort)0x8007, cpu.PC);
        Assert.Equal(2, cpu.DebugCycle);

        cpu.Tick();
        Assert.Equal((ushort)0x8009, cpu.PC);
        Assert.Equal(1, cpu.DebugCycle);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001.
    /// Use case: Wolf64 sample 2050516. After BA consumes the extra BNE FETCH
    ///   CLK, LDA abs last CLK must export next-PC ($FF61), not hold opcode
    ///   ($FF5E).
    /// Acceptance: Stolen extra BNE FETCH then LDA abs last CLK is $8007.
    /// </summary>
    [Fact]
    public void TakenBne_StolenExtraFetchClk_FollowingLdaLastClkExportsNextPc()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x8000] = 0xA9;
        memory[0x8001] = 0x01;
        memory[0x8002] = 0xD0;
        memory[0x8003] = 0x00;
        memory[0x8004] = 0xAD;
        memory[0x8005] = 0x00;
        memory[0x8006] = 0x90;
        memory[0x8007] = 0xD0;
        memory[0x8008] = 0xFB;
        memory[0x8009] = 0xEA;
        memory[0x9000] = 0x06;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        AdvanceToOpcode(cpu, 0xAD);
        for (var i = 0; i < 32 && !(cpu.DebugOpcode == 0xD0 && cpu.DebugCycle == 2); i++)
            cpu.Tick();

        for (var steal = 0; steal < 15; steal++)
            cpu.OnStolenCycle();

        for (var i = 0; i < 16 && cpu.DebugOpcode != 0xAD; i++)
            cpu.Tick();

        Assert.Equal((byte)0xAD, cpu.DebugOpcode);
        byte? aAtCycle1 = null;
        while (cpu.DebugOpcode == 0xAD && cpu.DebugCycle != 0)
        {
            cpu.Tick();
            if (cpu.DebugOpcode == 0xAD && cpu.DebugCycle == 1)
                aAtCycle1 = cpu.A;
        }

        Assert.Equal((byte)0x06, aAtCycle1);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((byte)0xAD, cpu.DebugOpcode);
        Assert.Equal((ushort)0x8007, cpu.PC);
    }

    private static Mos6502 CreateDeyBplCpu()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x8000] = 0xA0; // LDY #$02
        memory[0x8001] = 0x02;
        memory[0x8002] = 0x88; // DEY
        memory[0x8003] = 0x10; // BPL $8002
        memory[0x8004] = 0xFD;
        memory[0x8005] = 0xEA; // NOP fall-through

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        return cpu;
    }

    private static Mos6502 CreateInyBneCpu()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x8000] = 0xA0; // LDY #$00
        memory[0x8001] = 0x00;
        memory[0x8002] = 0xC8; // INY
        memory[0x8003] = 0xD0; // BNE $8002
        memory[0x8004] = 0xFD;
        memory[0x8005] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        return cpu;
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001.
    /// Use case: Wolf64 sample 2093352. SystemClock skips the CPU on
    ///   <see cref="ICpuCycleStealer"/> (the lockstep BA path). VICE INC zp
    ///   (6510dtvcore INC + GET_ZERO + SET_ZERO_RMW) does LOCAL_SET_NZ and
    ///   INC_PC(2) with no CLK, then dummy+store CLK_INC. Native last INC CLK
    ///   still exports opcode PC; FETCH of BNE exports opcode+2 with N clear
    ///   after INC $7E->$7F. A second RMW of $7E yields $80 and sets N
    ///   (Wolf64 mP=$A5 vs nP=$25). zp=$01 cannot catch that. Direct
    ///   <c>OnStolenCycle</c> is not this path: lockstep only notifies when
    ///   <c>NotifyOnStolenCycle</c> is set.
    /// Acceptance: Steal holds opcode PC. Last INC CLK keeps opcode PC and
    ///   a single INC. First FETCH CLK of the following BNE exports opcode+2,
    ///   N clear, and zp=$7F.
    /// </summary>
    [Fact]
    public void StolenZpInc_SystemClockBaSkip_FetchesNextOpcodeAtIncPc()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x0010] = 0x7E;
        memory[0x8000] = 0xA9; // LDA #$00 (Z set)
        memory[0x8001] = 0x00;
        memory[0x8002] = 0xD0; // BNE not taken
        memory[0x8003] = 0x00;
        memory[0x8004] = 0xE6; // INC $10
        memory[0x8005] = 0x10;
        memory[0x8006] = 0xD0; // BNE
        memory[0x8007] = 0x00;
        memory[0x8008] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        var stealer = new ClockBaStealer();
        var clock = new SystemClock();
        clock.Register(cpu);
        clock.Register(stealer);
        cpu.Reset();

        // Steal on the GET_ZERO CLK (managed cycle 3 -> 2), before the cycle-2
        // handler would INC_PC. VICE check_ba is on LOAD_ZERO, before INC_PC.
        for (var i = 0; i < 32 && !(cpu.DebugOpcode == 0xE6 && cpu.DebugCycle == 3); i++)
            clock.Step();

        Assert.Equal((byte)0xE6, cpu.DebugOpcode);
        Assert.Equal(3, cpu.DebugCycle);
        Assert.Equal((ushort)0x8004, cpu.PC);
        Assert.True(cpu.CanStealCurrentCycle);

        stealer.IsCpuCycleStolen = true;
        for (var steal = 0; steal < 20; steal++)
        {
            clock.Step();
            Assert.Equal((byte)0xE6, cpu.DebugOpcode);
            Assert.Equal(3, cpu.DebugCycle);
            Assert.Equal((ushort)0x8004, cpu.PC);
            Assert.True(cpu.DebugZpRmwPcDeferredFromSteal);
        }

        stealer.IsCpuCycleStolen = false;
        byte? bneP = null;
        ushort? bnePc = null;
        ushort? bneOpcodeAddress = null;
        for (var i = 0; i < 8; i++)
        {
            clock.Step();
            if (cpu.DebugOpcode != 0xD0)
            {
                Assert.True(
                    cpu.DebugOpcode == 0xE6 && cpu.PC == 0x8004 && (cpu.P & 0x80) == 0,
                    $"unstall[{i}] op=${cpu.DebugOpcode:X2} cyc={cpu.DebugCycle} PC=${cpu.PC:X4} P=${cpu.P:X2} zp=${memory[0x0010]:X2} stealDefer={cpu.DebugZpRmwPcDeferredFromSteal} staged={cpu.DebugStagedMemoryReadCompleted}");
                continue;
            }

            bneP = cpu.P;
            bnePc = cpu.PC;
            bneOpcodeAddress = cpu.DebugOpcodeAddress;
            break;
        }

        Assert.Equal((byte)0xD0, cpu.DebugOpcode);
        // VICE taken BNE dummy CLK exports fall-through (opcode+2), overlapping
        // the INC last CLK's FETCH1 (Wolf64 2093352 nPC=$F6A1).
        Assert.Equal((ushort)0x8008, bnePc);
        Assert.Equal((ushort)0x8006, bneOpcodeAddress);
        Assert.Equal(0, bneP!.Value & 0x80);
        Assert.Equal((byte)0x7F, memory[0x0010]);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001.
    /// Use case: Wolf64 sample 2142574. KERNAL UDTIM INC $A2 at $F69D already
    ///   exported $F69F on its last CLK. VICE FETCH of BNE is still $F69F.
    ///   Managed dummy-INC_PC on that FETCH ($F6A1) because previous opcode
    ///   was zp INC.
    /// Acceptance: After INC zp whose last CLK is already next-PC, first
    ///   taken BNE tick keeps the BNE opcode address.
    /// </summary>
    [Fact]
    public void IncZpLastClkAtNextPc_BneFetch_KeepsOpcodePc()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x0010] = 0x01;
        memory[0x8000] = 0xA9; // LDA #$01 (Z clear, BNE taken)
        memory[0x8001] = 0x01;
        memory[0x8002] = 0xE6; // INC $10
        memory[0x8003] = 0x10;
        memory[0x8004] = 0xD0; // BNE $8006
        memory[0x8005] = 0x00;
        memory[0x8006] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();

        for (var i = 0; i < 32 && cpu.DebugOpcode != 0xD0; i++)
            cpu.Tick();

        Assert.Equal((byte)0xD0, cpu.DebugOpcode);
        Assert.Equal((ushort)0x8004, cpu.PC);
    }
    ///   Native first LDA zp host tick already has A=$27 (GET_ZERO). Managed
    ///   still had pre-INC A=$77 on DebugCycle 2.
    /// Acceptance: After SystemClock BA skip of INC $10 ($26->$27) with
    ///   preload A=$77, the first FETCH tick of the following LDA $10 has
    ///   A=$27.
    /// </summary>
    [Fact]
    public void StolenZpIncThenLdaZp_FirstFetch_HasLoadedA()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x0010] = 0x26;
        memory[0x8000] = 0xA9; // LDA #$77
        memory[0x8001] = 0x77;
        memory[0x8002] = 0xE6; // INC $10
        memory[0x8003] = 0x10;
        memory[0x8004] = 0xA5; // LDA $10
        memory[0x8005] = 0x10;
        memory[0x8006] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        var stealer = new ClockBaStealer();
        var clock = new SystemClock();
        clock.Register(cpu);
        clock.Register(stealer);
        cpu.Reset();

        for (var i = 0; i < 32 && !(cpu.DebugOpcode == 0xE6 && cpu.DebugCycle >= 2); i++)
            clock.Step();

        Assert.Equal((byte)0xE6, cpu.DebugOpcode);
        Assert.Equal((byte)0x77, cpu.A);
        stealer.IsCpuCycleStolen = true;
        for (var steal = 0; steal < 13; steal++)
            clock.Step();
        stealer.IsCpuCycleStolen = false;

        byte? aAtLdaFetch = null;
        for (var i = 0; i < 12; i++)
        {
            clock.Step();
            if (cpu.DebugOpcode == 0xA5)
            {
                aAtLdaFetch = cpu.A;
                break;
            }
        }

        Assert.Equal((byte)0xA5, cpu.DebugOpcode);
        Assert.Equal((byte)0x27, aAtLdaFetch);

        while (cpu.DebugOpcode == 0xA5 && cpu.DebugCycle != 1)
            clock.Step();
        Assert.Equal(1, cpu.DebugCycle);
        Assert.Equal((ushort)0x8006, cpu.PC);

        clock.Step();
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((ushort)0x8006, cpu.PC);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001.
    /// Use case: Wolf64 sample 2129142. After BA-stolen zp INC, fused LDA zp
    ///   and fused CMP zp, VICE BRANCH dummy INC_PC is the first host tick of
    ///   taken BCS (nPC=$E6C1 fall-through). Managed FETCH still showed the
    ///   opcode ($E6BF) because sticky non-overlapped from the INC steal added
    ///   a full-length extra FETCH CLK.
    /// Acceptance: Same steal+INC+LDA+CMP chain, first BCS tick exports
    ///   opcode+2 (dummy INC_PC), not the opcode address.
    /// </summary>
    [Fact]
    public void StolenZpIncLdaCmpBcs_FirstTick_ExportsFallthrough()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x0010] = 0x26;
        memory[0x0011] = 0x00;
        memory[0x8000] = 0xA9; // LDA #$77
        memory[0x8001] = 0x77;
        memory[0x8002] = 0xE6; // INC $10
        memory[0x8003] = 0x10;
        memory[0x8004] = 0xA5; // LDA $10
        memory[0x8005] = 0x10;
        memory[0x8006] = 0xC5; // CMP $11 (A=$27 >= 0, C=1)
        memory[0x8007] = 0x11;
        memory[0x8008] = 0xB0; // BCS $800C (taken)
        memory[0x8009] = 0x02;
        memory[0x800A] = 0xEA;
        memory[0x800C] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        var stealer = new ClockBaStealer();
        var clock = new SystemClock();
        clock.Register(cpu);
        clock.Register(stealer);
        cpu.Reset();

        for (var i = 0; i < 32 && !(cpu.DebugOpcode == 0xE6 && cpu.DebugCycle >= 2); i++)
            clock.Step();
        stealer.IsCpuCycleStolen = true;
        for (var steal = 0; steal < 13; steal++)
            clock.Step();
        stealer.IsCpuCycleStolen = false;

        for (var i = 0; i < 24 && cpu.DebugOpcode != 0xB0; i++)
            clock.Step();

        Assert.Equal((byte)0xB0, cpu.DebugOpcode);
        Assert.Equal((ushort)0x800A, cpu.PC);

        while (cpu.DebugOpcode == 0xB0 && cpu.DebugCycle != 0)
            clock.Step();
        clock.Step();
        Assert.True(
            cpu.PC == 0x800C || cpu.DebugOpcode != 0xB0,
            $"BCS JUMP must leave fall-through; PC=${cpu.PC:X4} op=${cpu.DebugOpcode:X2} cyc={cpu.DebugCycle}");
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001.
    /// Use case: Wolf64 sample 2122119. Taken BNE then not-taken BPL: VICE
    ///   BRANCH not-taken INC_PC is visible on the last CLK (nPC=$B92B).
    ///   Managed held opcode PC (mPC=$B929).
    /// Acceptance: Last CLK of not-taken BPL after taken BNE exports
    ///   fall-through PC.
    /// </summary>
    [Fact]
    public void TakenBneThenNotTakenBpl_LastClk_ShowsFallthrough()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x8000] = 0xA9; // LDA #$80 (N set, Z clear)
        memory[0x8001] = 0x80;
        memory[0x8002] = 0xD0; // BNE $8004 (taken)
        memory[0x8003] = 0x00;
        memory[0x8004] = 0x10; // BPL $8006 (not taken)
        memory[0x8005] = 0x00;
        memory[0x8006] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();

        for (var i = 0; i < 32 && cpu.DebugOpcode != 0x10; i++)
            cpu.Tick();

        Assert.Equal((byte)0x10, cpu.DebugOpcode);

        for (var i = 0; i < 8 && cpu.DebugCycle != 0; i++)
            cpu.Tick();

        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((byte)0x10, cpu.DebugOpcode);
        Assert.Equal((ushort)0x8006, cpu.PC);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001.
    /// Use case: Wolf64 sample 2044582. JMP abs after STA zp that itself
    ///   followed a taken BMI (STA write already at next-PC, trail>=2): VICE
    ///   last JMP CLK already exports the target (nPC=$EA24). Soft-deferring
    ///   every STA zp JMP held opcode PC. Contrast 2122664: STA zp write held
    ///   opcode (trail<=1) so JMP last CLK stays opcode.
    /// Acceptance: Last CLK of that JMP is the target.
    /// </summary>
    [Fact]
    public void TakenBmiThenStaZpThenJmpAbs_LastClk_ExportsTarget()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x8000] = 0xA9; // LDA #$80 (N set)
        memory[0x8001] = 0x80;
        memory[0x8002] = 0x30; // BMI $8006 (taken)
        memory[0x8003] = 0x02;
        memory[0x8004] = 0xEA;
        memory[0x8005] = 0xEA;
        memory[0x8006] = 0x85; // STA $02
        memory[0x8007] = 0x02;
        memory[0x8008] = 0x4C; // JMP $8030
        memory[0x8009] = 0x30;
        memory[0x800A] = 0x80;
        memory[0x8030] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        AdvanceToOpcode(cpu, 0x4C);
        while (cpu.DebugOpcode == 0x4C && cpu.DebugCycle != 0)
            cpu.Tick();

        Assert.Equal((byte)0x4C, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((ushort)0x8030, cpu.PC);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001, TEST: TEST-X64SC-LOCKSTEP-001.
    /// VICE ST(SET_ABS_X) has exported three next-PC checkpoints before the
    /// following implied body. Its FETCH phase is therefore consumed and INX
    /// must expose its updated register and next PC on the final checkpoint.
    /// </summary>
    [Fact]
    public void StaAbsXWithThreeTrailingCheckpointsThenInx_CompletesOnFinalFetch()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x8000] = 0xD0; // BNE $8004 (taken)
        memory[0x8001] = 0x02;
        memory[0x8004] = 0xBD; // LDA $9000,X
        memory[0x8005] = 0x00;
        memory[0x8006] = 0x90;
        memory[0x8007] = 0x9D; // STA $9100,X
        memory[0x8008] = 0x00;
        memory[0x8009] = 0x91;
        memory[0x800A] = 0xE8; // INX
        memory[0x800B] = 0xE0; // CPX #$02
        memory[0x800C] = 0x02;
        memory[0x800D] = 0xD0; // BNE $800F (not taken)
        memory[0x800E] = 0x00;
        memory[0x800F] = 0xA2; // LDX #$00
        memory[0x8010] = 0x00;
        memory[0x8011] = 0xA9; // LDA #$20
        memory[0x8012] = 0x20;
        memory[0x8013] = 0xEA;
        memory[0x9001] = 0x00;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        cpu.X = 0x01;
        cpu.P = 0x20;

        AdvanceToOpcode(cpu, 0xE8);
        Assert.Equal(1, cpu.DebugCycle);
        Assert.Equal((byte)0x9D, cpu.DebugPreviousOpcode);
        Assert.Equal(3, cpu.DebugPriorTrailingAtNextPc);
        Assert.Equal((byte)0x01, cpu.X);

        cpu.Tick();

        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((byte)0x02, cpu.X);
        Assert.Equal((ushort)0x800B, cpu.PC);

        AdvanceToOpcode(cpu, 0xA2);
        Assert.False(cpu.DebugNonOverlappedFetchPhase);
        Assert.False(cpu.DebugNonOverlappedRegion);
        cpu.Tick();

        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((byte)0x00, cpu.X);
        Assert.Equal((ushort)0x8011, cpu.PC);

        AdvanceToOpcode(cpu, 0xA9);
        cpu.Tick();

        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((byte)0x20, cpu.A);
        Assert.Equal((ushort)0x8013, cpu.PC);
        Assert.Equal(0x00, cpu.P & 0x02);
        Assert.Equal(0x01, cpu.P & 0x01);
    }

    /// <summary>
    /// FR: FR-CPU-003, TR: TR-CYCLE-001, TEST: TEST-X64SC-LOCKSTEP-001.
    /// VICE GET_ABS performs LOAD on its final clock. When a soft-deferred
    /// immediate load leaves the following absolute load in a non-overlapped
    /// FETCH phase, the data-read checkpoint must still expose pre-load A.
    /// </summary>
    [Fact]
    public void LdaAbsAfterSoftDeferredImmediateLoad_DataReadKeepsPreloadA()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x8000] = 0x60; // RTS to JSR
        memory[0x8003] = 0x20; // JSR $8100 after RTS
        memory[0x8004] = 0x00;
        memory[0x8005] = 0x81;
        memory[0x01FE] = 0x02;
        memory[0x01FF] = 0x80;
        memory[0x8100] = 0xA9; // LDA #$1C
        memory[0x8101] = 0x1C;
        memory[0x8102] = 0x85; // STA $10
        memory[0x8103] = 0x10;
        memory[0x8104] = 0xA2; // LDX #$0F
        memory[0x8105] = 0x0F;
        memory[0x8106] = 0xAD; // LDA $9000
        memory[0x8107] = 0x00;
        memory[0x8108] = 0x90;
        memory[0x8109] = 0xEA;
        memory[0x9000] = 0x0F;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        cpu.S = 0xFD;

        AdvanceToOpcode(cpu, 0xAD);
        Assert.True(cpu.DebugNonOverlappedFetchPhase);

        while (cpu.DebugOpcode == 0xAD && cpu.DebugCycle != 1)
            cpu.Tick();

        Assert.Equal((byte)0xAD, cpu.DebugOpcode);
        Assert.Equal(1, cpu.DebugCycle);
        Assert.Equal((byte)0x1C, cpu.A);
        Assert.Equal((ushort)0x8106, cpu.PC);

        cpu.Tick();

        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((byte)0x0F, cpu.A);
        Assert.Equal((ushort)0x8106, cpu.PC);
    }

    /// <summary>
    /// FR: FR-CPU-003, TR: TR-CYCLE-001, TEST: TEST-X64SC-LOCKSTEP-001.
    /// VICE RTS completes its unclocked PC increment after the final stack LOAD.
    /// The following LDA absolute still exposes pre-load A on GET_ABS's
    /// data-read checkpoint even though the managed overlap flags are clear.
    /// </summary>
    [Theory]
    [InlineData((byte)0x60, (byte)0xAD)]
    [InlineData((byte)0x60, (byte)0xAE)]
    [InlineData((byte)0x60, (byte)0xAC)]
    [InlineData((byte)0x40, (byte)0xAD)]
    [InlineData((byte)0x40, (byte)0xAE)]
    [InlineData((byte)0x40, (byte)0xAC)]
    public void AbsoluteLoadAfterReturn_DataReadKeepsPreloadRegister(
        byte returnOpcode,
        byte loadOpcode)
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x8000] = returnOpcode;
        if (returnOpcode == 0x60)
        {
            memory[0x01FE] = 0x02;
            memory[0x01FF] = 0x80;
        }
        else
        {
            memory[0x01FD] = 0x20;
            memory[0x01FE] = 0x03;
            memory[0x01FF] = 0x80;
        }
        memory[0x8003] = loadOpcode; // LD{A|X|Y} $9000
        memory[0x8004] = 0x00;
        memory[0x8005] = 0x90;
        memory[0x8006] = 0xEA;
        memory[0x9000] = 0x00;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        cpu.S = returnOpcode == 0x60 ? (byte)0xFD : (byte)0xFC;
        cpu.P = 0x20;
        cpu.A = 0x35;
        cpu.X = 0x36;
        cpu.Y = 0x37;

        AdvanceToOpcode(cpu, loadOpcode);
        while (cpu.DebugOpcode == loadOpcode && cpu.DebugCycle != 1)
            cpu.Tick();

        Assert.Equal(loadOpcode, cpu.DebugOpcode);
        Assert.Equal(1, cpu.DebugCycle);
        Assert.Equal((byte)0x35, cpu.A);
        Assert.Equal((byte)0x36, cpu.X);
        Assert.Equal((byte)0x37, cpu.Y);
        Assert.Equal(0x00, cpu.P & 0x02);
        Assert.Equal((ushort)0x8003, cpu.PC);

        if (returnOpcode == 0x60)
        {
            cpu.Tick();

            Assert.Equal(0, cpu.DebugCycle);
            Assert.Equal(
                loadOpcode == 0xAD ? (byte)0x00 : (byte)0x35,
                cpu.A);
            Assert.Equal(
                loadOpcode == 0xAE ? (byte)0x00 : (byte)0x36,
                cpu.X);
            Assert.Equal(
                loadOpcode == 0xAC ? (byte)0x00 : (byte)0x37,
                cpu.Y);
            Assert.Equal(0x00, cpu.P & 0x02);
            Assert.Equal((ushort)0x8003, cpu.PC);
        }
    }

    /// <summary>
    /// FR: FR-CPU-003, TR: TR-CYCLE-001, TEST: TEST-X64SC-LOCKSTEP-001.
    /// VICE JMP performs its unclocked JUMP after the final operand clock.
    /// When that JUMP was soft-deferred, the target STA zero-page begins with
    /// a distinct FETCH and its store checkpoint must still expose opcode PC.
    /// </summary>
    [Fact]
    public void StaZpAfterSoftDeferredJmp_WriteCycleKeepsOpcodePc()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x8000] = 0x60; // RTS to JSR
        memory[0x8003] = 0x20; // JSR $8100 after RTS
        memory[0x8004] = 0x00;
        memory[0x8005] = 0x81;
        memory[0x01FE] = 0x02;
        memory[0x01FF] = 0x80;
        memory[0x8100] = 0xA9; // LDA #$1C
        memory[0x8101] = 0x1C;
        memory[0x8102] = 0x85; // STA $10
        memory[0x8103] = 0x10;
        memory[0x8104] = 0xA2; // LDX #$0F
        memory[0x8105] = 0x0F;
        memory[0x8106] = 0xAD; // LDA $9000
        memory[0x8107] = 0x00;
        memory[0x8108] = 0x90;
        memory[0x8109] = 0x4C; // JMP $8200
        memory[0x810A] = 0x00;
        memory[0x810B] = 0x82;
        memory[0x8200] = 0x85; // STA $11
        memory[0x8201] = 0x11;
        memory[0x8202] = 0xEA;
        memory[0x9000] = 0x0F;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        cpu.S = 0xFD;

        AdvanceToOpcode(cpu, 0x4C);
        Assert.True(cpu.DebugNonOverlappedFetchPhase);

        AdvanceToOpcode(cpu, 0x85);
        while (cpu.DebugOpcode == 0x85 && cpu.DebugCycle != 1)
            cpu.Tick();

        Assert.Equal((byte)0x85, cpu.DebugOpcode);
        Assert.Equal(1, cpu.DebugCycle);
        Assert.Equal((ushort)0x8200, cpu.PC);
        Assert.Equal((byte)0x0F, memory[0x0011]);
    }

    /// <summary>
    /// FR: FR-CPU-003, TR: TR-CYCLE-001, TEST: TEST-X64SC-LOCKSTEP-001.
    /// VICE's not-taken BRANCH can prefetch a fall-through RTS, but that fetch
    /// does not turn the preceding immediate compare into an RTS source-clock
    /// overlap. STACK_PEEK remains ahead of the first PULL, so S must not move
    /// on the RTS cycle-3 checkpoint.
    /// </summary>
    [Fact]
    public void NotTakenBccPrefetchedRts_CycleThreeKeepsStackPointer()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x0010] = 0x04;
        memory[0x8000] = 0xA6; // LDX $10
        memory[0x8001] = 0x10;
        memory[0x8002] = 0xE8; // INX
        memory[0x8003] = 0xE0; // CPX #$05 (Z/C set)
        memory[0x8004] = 0x05;
        memory[0x8005] = 0x90; // BCC $8007 (not taken)
        memory[0x8006] = 0x00;
        memory[0x8007] = 0x60; // RTS
        memory[0x01FE] = 0x20;
        memory[0x01FF] = 0x80;
        memory[0x8021] = 0x4C; // JMP $8030
        memory[0x8022] = 0x30;
        memory[0x8023] = 0x80;
        memory[0x8030] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        cpu.S = 0xFD;

        AdvanceToOpcode(cpu, 0x60);
        while (cpu.DebugOpcode == 0x60 && cpu.DebugCycle > 3)
            cpu.Tick();

        Assert.Equal((byte)0x60, cpu.DebugOpcode);
        Assert.Equal(3, cpu.DebugCycle);
        Assert.Equal((byte)0xE0, cpu.DebugPreviousOpcode);
        Assert.Equal(1, cpu.DebugPriorTrailingAtNextPc);
        Assert.Equal((byte)0xFD, cpu.S);

        AdvanceToOpcode(cpu, 0x4C);
        while (cpu.DebugOpcode == 0x4C && cpu.DebugCycle != 0)
            cpu.Tick();

        Assert.Equal((byte)0x4C, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((byte)0x60, cpu.DebugPreviousOpcode);
        Assert.Equal((ushort)0x8021, cpu.PC);
    }

    /// <summary>
    /// FR: FR-CPU-003, TR: TR-CYCLE-001.
    /// Use case: Wolf64 sample 2109682. VICE RTS() JUMP has no CLK; the next
    ///   mainloop is DO_INTERRUPT then FETCH of the return insn. IRQ delay
    ///   elapsed during RTS, so IRQ dummy/push runs at the return PC (TYA
    ///   $EA1B) without executing TYA (nA=$D8 nPC=$EA1B nS=$EF nLastOp=$60).
    ///   Managed FETCHed TYA, committed A=Y, then armed IRQ (mA=$20 mPC=$EA1C
    ///   mS=$F0 irqSeq=6).
    /// Acceptance: RTS to TYA with IRQ already pending does not complete TYA
    ///   (A stays the pre-TYA value). S decrements on the IRQ push while PC
    ///   remains the return address.
    /// </summary>
    [Fact]
    public void RtsToTya_IrqPending_DoesNotCommitTyaBeforeIrqPush()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0xFFFE] = 0x00;
        memory[0xFFFF] = 0x09;
        memory[0x8000] = 0x60; // RTS
        memory[0x8003] = 0x98; // TYA (return)
        memory[0x8004] = 0xEA;
        memory[0x01FE] = 0x02;
        memory[0x01FF] = 0x80;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var irq = new InterruptLine(InterruptType.Irq);
        var cpu = new Mos6502(bus)
        {
            A = 0xD8,
            Y = 0x20,
            S = 0xFD,
            P = 0x20
        };
        var clock = new SystemClock(985_248, cpu, irq);
        clock.Register(cpu);
        cpu.Reset();
        cpu.A = 0xD8;
        cpu.Y = 0x20;
        cpu.S = 0xFD;
        cpu.P = 0x20;
        cpu.PC = 0x8000;
        irq.Assert(new RtsTyaIrqSource());

        for (var i = 0; i < 16 && cpu.PC != 0x8003; i++)
            clock.Step();

        Assert.Equal((ushort)0x8003, cpu.PC);
        var sAtReturn = cpu.S;
        Assert.Equal((byte)0xD8, cpu.A);

        byte sAfter = cpu.S;
        for (var i = 0; i < 8; i++)
        {
            clock.Step();
            sAfter = cpu.S;
            if (sAfter < sAtReturn)
                break;
        }

        Assert.True(sAfter < sAtReturn, $"IRQ did not push; S=${sAfter:X2}");
        Assert.Equal((byte)0xD8, cpu.A);
        Assert.True(cpu.PC == 0x8003 || cpu.DebugInterruptSequenceRemaining > 0,
            $"TYA ran or IRQ missed; PC=${cpu.PC:X4} A=${cpu.A:X2} irqSeq={cpu.DebugInterruptSequenceRemaining}");
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use cases: Wolf64 samples 119162-119181. VICE LDX immediate completes
    ///   after its second FETCH checkpoint, so the following STX zero-page write
    ///   still exports the STX opcode PC. That staged-store chain establishes a
    ///   non-overlapped phase which later keeps STA (zp),Y's second FETCH at the
    ///   opcode PC after LDA abs,X has zero trailing checkpoints.
    /// Acceptance: STX cycle 1 holds its opcode PC, TXA soft-completes, and the
    ///   later non-overlapped LDA abs,X / STA (zp),Y keeps cycle 4 at STA.
    /// </summary>
    [Fact]
    public void RtsLdxImmThenStxZp_WriteCheckpointHoldsOpcodePc()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x0010] = 0x00;
        memory[0x0011] = 0x04;
        memory[0x8000] = 0x60; // RTS to $8003
        memory[0x01FE] = 0x02;
        memory[0x01FF] = 0x80;
        memory[0x8003] = 0xA2; // LDX #$00
        memory[0x8004] = 0x00;
        memory[0x8005] = 0x86; // STX $10
        memory[0x8006] = 0x10;
        memory[0x8007] = 0x8A; // TXA
        memory[0x8008] = 0x0A; // ASL A
        memory[0x8009] = 0x0A; // ASL A
        memory[0x800A] = 0x0A; // ASL A
        memory[0x800B] = 0xA8; // TAY
        memory[0x800C] = 0xA2; // LDX #$00
        memory[0x800D] = 0x00;
        memory[0x800E] = 0xBD; // LDA $9000,X
        memory[0x800F] = 0x00;
        memory[0x8010] = 0x90;
        memory[0x8011] = 0x91; // STA ($10),Y
        memory[0x8012] = 0x10;
        memory[0x8013] = 0xEA;
        memory[0x9000] = 0x5A;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        cpu.A = 0x61;
        cpu.S = 0xFD;

        AdvanceToOpcode(cpu, 0x86);
        while (cpu.DebugOpcode == 0x86 && cpu.DebugCycle > 1)
            cpu.Tick();

        Assert.Equal((byte)0x86, cpu.DebugOpcode);
        Assert.Equal(1, cpu.DebugCycle);
        Assert.Equal((ushort)0x8005, cpu.PC);
        Assert.Equal((byte)0x00, memory[0x0010]);

        cpu.Tick();

        Assert.Equal((ushort)0x8007, cpu.PC);

        AdvanceToOpcode(cpu, 0x8A);
        Assert.Equal(1, cpu.DebugCycle);
        cpu.Tick();

        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((byte)0x61, cpu.A);
        Assert.Equal((ushort)0x8007, cpu.PC);

        cpu.Tick();

        Assert.Equal((byte)0x00, cpu.A);
        Assert.Equal((ushort)0x8008, cpu.PC);

        AdvanceToOpcode(cpu, 0x91);

        Assert.Equal(5, cpu.DebugCycle);
        Assert.Equal((byte)0xBD, cpu.DebugPreviousOpcode);
        Assert.Equal(0, cpu.DebugPriorTrailingAtNextPc);
        Assert.True(cpu.DebugNonOverlappedFetchPhase);
        Assert.Equal((ushort)0x8011, cpu.PC);

        cpu.Tick();

        Assert.Equal(4, cpu.DebugCycle);
        Assert.Equal((ushort)0x8011, cpu.PC);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 sample 117450. VICE LDY zero-page assigns Y before the
    ///   GET_ZERO CLK_INC, but a preceding STA (zp),Y with only four exported
    ///   next-PC checkpoints has not consumed LDY's following fetch phase.
    /// Acceptance: LDY's data-read checkpoint retains the pre-load Y value and
    ///   the apply checkpoint commits the zero-page value.
    /// </summary>
    [Fact]
    public void StaIndYThenLdyZp_DataReadHoldsPreloadY()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x0010] = 0x00;
        memory[0x0011] = 0x90;
        memory[0x0020] = 0x09;
        memory[0x8000] = 0x40; // RTI to $8010
        memory[0x01EB] = 0x20;
        memory[0x01EC] = 0x10;
        memory[0x01ED] = 0x80;
        memory[0x8010] = 0xC8; // INY to $4F
        memory[0x8011] = 0x91; // STA ($10),Y
        memory[0x8012] = 0x10;
        memory[0x8013] = 0xA4; // LDY $20
        memory[0x8014] = 0x20;
        memory[0x8015] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        cpu.S = 0xEA;
        cpu.Y = 0x4E;

        AdvanceToOpcode(cpu, 0xA4);
        while (cpu.DebugOpcode == 0xA4 && cpu.DebugCycle > 1)
            cpu.Tick();

        Assert.Equal((byte)0xA4, cpu.DebugOpcode);
        Assert.Equal(1, cpu.DebugCycle);
        Assert.Equal((byte)0x4F, cpu.Y);

        cpu.Tick();

        Assert.Equal((byte)0x09, cpu.Y);
        Assert.Equal((ushort)0x8013, cpu.PC);
    }

    /// <summary>
    /// FR: FR-CPU-003, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 sample 117356. A normal main-loop DO_INTERRUPT sample
    ///   follows STA (zp),Y without a branch JUMP. The pre-fetch checkpoint has
    ///   already exported VICE's second IRQ dummy CLK, so the following clock
    ///   performs the PCH PUSH. Branch JUMP entry retains its separate cycle-6
    ///   shape because its no-clock JUMP has not exported the first dummy.
    /// Acceptance: After ordinary pre-fetch IRQ arming, sequence state is 5
    ///   with S unchanged; the next clock pushes PCH and reaches state 4.
    /// </summary>
    [Fact]
    public void StaIndYPrefetchIrq_ArmingCheckpointCompletesSecondDummy()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0xFFFE] = 0x00;
        memory[0xFFFF] = 0x09;
        memory[0x0010] = 0x00;
        memory[0x0011] = 0x90;
        memory[0x8000] = 0x91; // STA ($10),Y
        memory[0x8001] = 0x10;
        memory[0x8002] = 0xEA;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var irq = new InterruptLine(InterruptType.Irq);
        var cpu = new Mos6502(bus)
        {
            PC = 0x8000,
            S = 0xEE,
            P = 0x24
        };
        var clock = new SystemClock(985_248, cpu, irq);
        clock.Register(cpu);
        cpu.Reset();
        cpu.PC = 0x8000;
        cpu.S = 0xEE;
        cpu.P = 0x24;
        irq.Assert(new RtsTyaIrqSource());

        for (var i = 0; i < 32 && !(cpu.DebugOpcode == 0x91 && cpu.DebugCycle == 0); i++)
            clock.Step();

        Assert.Equal((byte)0x91, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        cpu.P = (byte)(cpu.P & ~0x04);

        for (var i = 0; i < 4 && cpu.DebugInterruptSequenceRemaining == 0; i++)
            clock.Step();

        Assert.Equal(5, cpu.DebugInterruptSequenceRemaining);
        Assert.Equal((byte)0xEE, cpu.S);

        clock.Step();

        Assert.Equal(4, cpu.DebugInterruptSequenceRemaining);
        Assert.Equal((byte)0xED, cpu.S);
        Assert.Equal((byte)0x80, memory[0x01EE]);
    }

    private sealed class RtsTyaIrqSource : IInterruptSource
    {
        public DeviceId Id { get; } = new(0x00FE);
        public DeviceId SourceId => Id;
        public string Name => "rts-tya-irq";
        public IReadOnlyList<IInterruptLine> ConnectedLines { get; } = [];
        public void Reset() { }
    }

    private sealed class AfterCpuIrqSource : IClockedDevice, IInterruptSource
    {
        private readonly IInterruptLine _line;
        private readonly Mos6502 _cpu;
        private readonly byte _opcode;
        private readonly int _cycle;

        public AfterCpuIrqSource(IInterruptLine line, Mos6502 cpu, byte opcode, int cycle)
        {
            _line = line;
            _cpu = cpu;
            _opcode = opcode;
            _cycle = cycle;
            ConnectedLines = [line];
        }

        public DeviceId Id { get; } = new(0x00FD);
        public DeviceId SourceId => Id;
        public string Name => "after-cpu-irq";
        public IReadOnlyList<IInterruptLine> ConnectedLines { get; }
        public uint ClockDivisor => 1;
        public ClockPhase Phase => ClockPhase.Phi2;
        public bool Armed { get; set; }
        public bool HasAsserted { get; private set; }

        public void Tick()
        {
            if (!Armed || HasAsserted || _cpu.DebugOpcode != _opcode || _cpu.DebugCycle != _cycle)
                return;

            _line.Assert(this);
            HasAsserted = true;
        }

        public void Reset()
        {
            Armed = false;
            HasAsserted = false;
            _line.Release(this);
        }
    }

    private sealed class ClockBaStealer : IClockedDevice, ICpuCycleStealer
    {
        public DeviceId Id { get; } = new(0x00BA);
        public string Name => "test-ba-stealer";
        public uint ClockDivisor => 1;
        public ClockPhase Phase => ClockPhase.Phi1;
        public bool IsCpuCycleStolen { get; set; }
        public bool IsCpuCycleStealMandatory { get; set; }
        public void Tick() { }
        public void Reset() => IsCpuCycleStolen = false;
    }

    private static Mos6502 CreateBneLdaAbsCpu(byte preloadA, byte loaded)
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x8000] = 0xA9; // LDA #preloadA
        memory[0x8001] = preloadA;
        memory[0x8002] = 0xD0; // BNE $8004 (taken, Z clear)
        memory[0x8003] = 0x00;
        memory[0x8004] = 0xAD; // LDA $9000
        memory[0x8005] = 0x00;
        memory[0x8006] = 0x90;
        memory[0x8007] = 0xEA;
        memory[0x9000] = loaded;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));
        var cpu = new Mos6502(bus);
        cpu.Reset();
        return cpu;
    }

    private static void AdvanceToOpcode(Mos6502 cpu, byte opcode)
    {
        for (var i = 0; i < 64 && cpu.DebugOpcode != opcode; i++)
            cpu.Tick();

        Assert.Equal(opcode, cpu.DebugOpcode);
    }

}
