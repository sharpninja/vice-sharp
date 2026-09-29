using ViceSharp.Abstractions;

namespace ViceSharp.Chips.Cpu;

public partial class Mos6502 : IClockedDevice, IAddressSpace, ICpu, ICpuCycleStealTarget
{
    private const int ResetCycleDelay = 1;

    public DeviceId Id => new DeviceId(0x0001);
    public string Name => "MOS 6502 CPU";
    public uint ClockDivisor => 1;
    public ClockPhase Phase => ClockPhase.Phi2;

    // Registers
    public byte A;
    public byte X;
    public byte Y;
    public byte S;
    private ushort _pc;
    private ushort _instructionPC;
    private ushort _visiblePC;
    public ushort PC
    {
        // Host-visible PC export is independent of interrupt-sample openings.
        // Suppress still forces _visiblePC so post-JSR non-overlapped last CLK
        // keeps exportPc (c=5012 nPC=$FDB7) while IsInstructionBoundary can
        // open for VICE DO_INTERRUPT before callee FETCH.
        get => (!_suppressBootstrapBoundary && _cycle == 0 && _interruptSequenceRemaining == 0)
            ? _pc
            : _visiblePC;
        set
        {
            _pc = value;
            _instructionPC = value;
            _visiblePC = value;
        }
    }
    public byte Flags { get => P; set => P = value; }
    public byte P;

    // FR-CPUTICK-001: this CPU's own executed-cycle counter (incremented per Tick()).
    private long _executedCycles;
    public long ExecutedCycles => _executedCycles;

    public bool IsInstructionBoundary =>
        _cycle == 0
        && _interruptSequenceRemaining == 0
        && (!_suppressBootstrapBoundary || _interruptSampleDespiteSuppress);
    /// <summary>
    /// VICE <c>OPINFO_DELAYS_INTERRUPT</c> / <c>OPCODE_DELAYS_INTERRUPT</c>:
    /// taken branch with no page-boundary cross delays IRQ/NMI by one extra
    /// cycle (<c>mainviccpu.c</c> <c>interrupt_check_irq_delay</c> irq_clk++).
    /// </summary>
    public bool LastOpcodeDelaysInterrupt => _lastOpcodeDelaysInterrupt;

    /// <summary>
    /// VICE <c>OPINFO_ENABLES_IRQ</c> / <c>OPCODE_ENABLES_IRQ</c>: CLI that
    /// cleared I. <c>interrupt_check_irq_delay</c> must not dispatch; it
    /// latches IK_IRQPEND so the following instruction runs first
    /// (Wolf64 2060428 nS=$FF mS=$FE).
    /// </summary>
    public bool LastOpcodeEnablesIrq => _lastOpcodeEnablesIrq;

    /// <summary>
    /// False on host ticks that have no VICE <c>CLK_INC</c> (taken same-page
    /// BRANCH JUMP, RTS JUMP <c>delayNextFetch</c>). <c>SystemClock</c> must
    /// not increment <c>irq_delay_cycles</c> on those ticks.
    /// </summary>
    public bool ConsumedViceClockThisTick { get; private set; } = true;

    /// <summary>
    /// VICE <c>FETCH_OPCODE</c> is two <c>CLK_INC</c> (plus <c>fetch_tab</c>
    /// on later ticks). A 3+ cycle opcode's first host FETCH tick is those
    /// two clocks; <c>SystemClock</c> adds this many to irq_delay.
    /// </summary>
    public int ViceClocksThisTick { get; private set; } = 1;

    public int DebugCycle => _cycle;
    public byte DebugOpcode => _opcode;
    public bool DebugDelayNextFetch => _delayNextFetch;
    /// <summary>Trailing next-PC dwell captured at last instruction fetch (diagnostics).</summary>
    public int DebugPriorTrailingAtNextPc { get; private set; }
    public bool DebugFullLengthTakenBranch { get; private set; }
    public bool DebugNonOverlappedRegion => _nonOverlappedRegion;
    public bool DebugNonOverlappedFetchPhase => _nonOverlappedFetchPhase;
    public bool DebugAfterLdaAbsHold => _afterLdaAbsHold;
    public bool DebugHoldAbsLoadOpcodeLastClk => _holdAbsLoadOpcodeLastClk;
    public bool DebugStagedMemoryReadCompleted => _stagedMemoryReadCompleted;
    public bool DebugZpRmwPcDeferredFromSteal => _zpRmwPcDeferredFromSteal;
    public int DebugInterruptSequenceRemaining => _interruptSequenceRemaining;
    public ushort DebugOpcodeAddress => _opcodeAddress;
    public bool DebugSoftDeferredImmediateLoad => _softDeferredImmediateLoad;
    public bool DebugImmediateLoadFollowsSoftDeferredBody => _immediateLoadFollowsSoftDeferredBody;
    public bool DebugSoftDeferredImplied => _softDeferredImpliedOp;
    public bool DebugSoftDeferAfterNopChain => _softDeferAfterNopChain;
    public bool DebugPendingDeferredNzUpdate => _pendingDeferredNzUpdateAfterBranch;
    public bool DebugPendingDeferredImplied => _pendingDeferredImpliedRegisterCompletion;
    public bool DebugFuseImpliedAfterIndyLoad => _fuseImpliedAfterIndyLoad;
    public bool DebugInySoftAfterHeldInc => _inySoftAfterHeldInc;
    public bool DebugSuppressBootstrapBoundary => _suppressBootstrapBoundary;
    public int DebugBootstrapCycles => _bootstrapCycles;
    public bool DebugAfterShortTakenBranchLag => _afterShortTakenBranchLag;
    public bool DebugAfterFullLengthTakenBranch => _afterFullLengthTakenBranch;
    public bool DebugSkipAbsLoadLastClkHold => _skipAbsLoadLastClkHold;
    public bool DebugLoadAEarlyAfterStagedBranch => _loadAEarlyAfterStagedBranch;
    public bool DebugBranchTargetFetchPending => _branchTargetFetchPending;
    public byte DebugPreviousOpcode => _previousOpcode;
    public bool DebugTakenBranchStagedFallthrough => _takenBranchStagedFallthrough;
    public bool DebugStolenTakenBranchAfterAbsY => _stolenTakenBranchAfterAbsY;
    public bool DebugNotifyOnStolenCycle => NotifyOnStolenCycle;
    public bool DebugCanForceSteal => CanForceStealCurrentCycle;
    public bool CanStealCurrentCycle
    {
        get
        {
            if (_bootstrapCycles > 0)
            {
                return false;
            }

            // Page-cross extra CLK is VICE BRANCH dummy CLK_INC (check_ba).
            // Blocking steal ran that CLK during BA so RasterX 55 FETCHed
            // the target (Wolf64 4136566 nPC=$A604 mPC=$A5B8). Same-page
            // JUMP has no extra CLK (2127622 nPC=$BEB2).
            // TR-LOCKSTEP-VSF-001: interrupt-sequence cycles follow VICE's BA
            // semantics (6510dtvcore.c DO_INTERRUPT/DO_IRQBRK): the dummy
            // fetch and the two vector reads go through check_ba (stealable),
            // the three stack pushes are writes and proceed during BA-low.
            if (_interruptSequenceRemaining > 0)
                return _interruptSequenceRemaining is 6 or 2 or 1;

            if (_cycle == 0)
                return true;

            var nextCycle = _cycle - 1;
            if (_opcode == 0x20)
                return nextCycle is not 2 and not 3;

            // Branch DebugCycle 2 transitions into the dummy INC_PC clock.
            // VICE does not check BA on that transition, so it must proceed
            // even while a bad-line steal is asserted (Wolf64 sample 15005).
            if (IsBranchOpcode(_opcode) && nextCycle == 1)
                return false;

            // Store write and PHA/PHP PUSH have no VICE check_ba (STORE /
            // PHA CLK_INC). DebugCycle 2 is the tick that decrements to 1
            // and performs that write (Wolf64 2799411 nS=$EF mS=$F0 when
            // PHA cycle 2 was stolen on a badline).
            if (nextCycle == 1 && (IsStoreOpcode(_opcode) || IsStackPushOpcode(_opcode)))
                return false;

            // VICE FETCH always calls check_ba, including implied DEY/INY
            // (mainc64cpu.c FETCH_OPCODE). Treating only "read-sensitive"
            // opcodes as stealable let DEY complete at RasterX 12 while native
            // froze (Wolf64 sample 2031315 nPC=$EA0E mPC=$EA0F).
            return true;
        }
    }

    /// <summary>
    /// When true, the next <see cref="Tick"/> emits the VICE FETCH <c>CLK_INC</c>
    /// delayed by BA steal (opcode PC) without decrementing <c>_cycle</c>.
    /// </summary>
    private bool _baDelayedFetchClk;

    /// <summary>
    /// VICE LD+GET_ABS: INC_PC runs after the GET_ABS CLK_INC with no extra
    /// clock, so the last LDA abs CLK still exports the opcode PC (Wolf64
    /// sample 2044614 nPC=$FF5E mPC=$FF61).
    /// </summary>
    private bool _holdAbsLoadOpcodeLastClk;
    private bool _afterLdaAbsHold;
    private bool _holdTakenBranchOpcodePc;
    private bool _stolenTakenBranchAfterAbsY;
    private bool _fetchAfterStolenB9Jump;
    private bool _skipAbsLoadLastClkHold;
    private bool _loadAEarlyAfterStagedBranch;
    private bool _ldaSkippedLastClkHold;

    /// <inheritdoc />
    public void OnStolenCycle()
    {
        // VICE INC/DEC zp: INC_PC has no CLK. BA skip must not lose that;
        // keep opcode PC until FETCH of the next insn (Wolf64 2093351 nPC=$F69F).
        // Check this before branch-dummy returns so a sticky fallthrough flag
        // cannot swallow the zp RMW notify.
        if (IsZeroPageIncrementDecrementOpcode(_opcode) && _cycle > 0)
        {
            _zpRmwPcDeferredFromSteal = true;
            _dummyTakenBneAfterStolenInc = true;
            return;
        }

        // Extra FETCH CLK of a 3-CLK taken BNE is still VICE FETCH_OPCODE
        // (opcode PC). check_ba delays that CLK; first unstall must not
        // INC_PC yet (Wolf64 2050510 nPC=$FF61 mPC=$FF63). Dummy INC_PC is
        // the following host cycle (6510dtvcore BRANCH after FETCH).
        // Do not arm staged fallthrough for every stolen taken-BNE FETCH:
        // baDelayed dummy at x=55 breaks the KERNAL wait loop (2053534
        // nPC=$FF63) and skipping baDelayed dummy at x=55 is early vs
        // Wolf64 4139087 (nPC=$A602 at x=55, nPC=$A604 at x=56).
        if (_holdTakenBranchOpcodePc && _cycle >= 2)
        {
            _baDelayedFetchClk = true;
            _holdTakenBranchOpcodePc = false;
            _takenBranchStagedFallthrough = true;
            _skipAbsLoadLastClkHold = true;
            _loadAEarlyAfterStagedBranch = true;
            return;
        }

        if (_holdTakenBranchOpcodePc)
            return;

        // LDA abs,Y then taken BNE: stolen FETCH is still VICE FETCH_OPCODE.
        // First unstall keeps opcode PC (4139087 nPC=$A602 at x=55); next CLK
        // is BRANCH dummy INC_PC (nPC=$A604 at x=56). Do not use this for
        // LDA abs $AD (KERNAL $FF61 already dummies at x=55, 2053534).
        if (IsBranchOpcode(_opcode) && IsBranchTaken(_opcode) && _cycle >= 2
            && _previousOpcode == 0xB9)
        {
            _baDelayedFetchClk = true;
            _takenBranchStagedFallthrough = true;
            _stolenTakenBranchAfterAbsY = true;
            return;
        }

        // Stolen taken BPL/BNE after INY/DEY: dummy INC_PC after unstall,
        // then VICE JUMP has no CLK so that host tick already shows the
        // target (Wolf64 4150680 nPC=$A5F9 after dummy $A5FF). Do not use
        // baDelayed here: that dummies KERNAL $AD wait-loop at x=56
        // (2053534 nPC=$FF63 at x=55). Unstalled INY/BNE still exports
        // fall-through (no steal, this arm does not run).
        if (IsBranchOpcode(_opcode) && IsBranchTaken(_opcode) && _cycle >= 2
            && _previousOpcode is 0xC8 or 0x88)
        {
            _takenBranchStagedFallthrough = true;
            _stolenTakenBranchAfterAbsY = true;
            return;
        }

        // Further BA clocks on the same dummy must not latch delayed FETCH:
        // that froze DebugCycle=2 at x=56 (Wolf64 2050511 nPC=$FF63).
        if (_takenBranchStagedFallthrough)
            return;

        // STA abs write cycle stolen on a badline: after unstall, FETCH the
        // following BEQ dummy (Wolf64 2520240 nPC=$E5D6 nS=$F3). Unstolen
        // STA still samples IRQ before the next FETCH (2142523 nS=$F2).
        if (_opcode == 0x8D && _cycle == 1)
        {
            _staAbsWriteCycleStolen = true;
            return;
        }

        // LDA zp data-read stolen on a badline: GET_ZERO is not DO_INTERRUPT.
        // After unstall, FETCH the following STA (Wolf64 3505560 nPC=$E5D1
        // nS=$F3 vs mS=$F2 irqSeq).
        if (_opcode == 0xA5 && _cycle == 1)
        {
            _ldaZpDataReadStolen = true;
            return;
        }

        // STA zp apply remaining stolen on a badline: STORE CLK is not
        // DO_INTERRUPT. After unstall, FETCH the following STA abs
        // (Wolf64 3899688 nLastOp=$8D nS=$F3 vs mS=$F2 irqSeq).
        // Cycle 0 last-CLK dwell is the same STORE (Wolf64 2717304).
        if (_opcode == 0x85 && _cycle <= 1)
        {
            _staZpApplyStolen = true;
            _skipIrqSampleAtNextFetch = true;
            return;
        }

        // STA abs p2 FETCH frozen by mandatory BA: VICE check_ba loops CLK
        // without an extra dummy. First unstall must run the FETCH.
        if (_opcode == 0x8D)
            return;

        // Delay the next FETCH CLK without `--` so the first unstall still
        // runs this micro-op (JSR STACK_PEEK at Wolf64 2018254; LDA/BPL tests).
        if (_cycle > 0 && _interruptSequenceRemaining == 0)
            _baDelayedFetchClk = true;
    }

    public bool CanForceStealCurrentCycle
    {
        get
        {
            if (_pendingDeferredNzUpdateAfterBranch ||
                _bootstrapCycles > 0 ||
                _branchPageCrossExtraPending)
            {
                return false;
            }

            // TR-LOCKSTEP-VSF-001: same interrupt-sequence BA semantics as
            // CanStealCurrentCycle (reads stall, stack pushes proceed).
            if (_interruptSequenceRemaining > 0)
                return _interruptSequenceRemaining is 6 or 2 or 1;

            if (_cycle == 0)
            {
                // FETCH after taken-branch JUMP is check_ba / conditional BA
                // (CanStealCurrentCycle). Must not be mandatory: RasterX 55 is
                // IsCpuCycleStealMandatory while IsCpuCycleStolen is already
                // false, and a sticky _branchTargetFetchPending would skip the
                // target FETCH (Wolf64 2049502 nPC=$FF5E mPC=$FF63). Stealing
                // the 2-CLK JUMP at RasterX 55 matches 4136566 and fails
                // 2127622 (native already at the target).
                return false;
            }

            var nextCycle = _cycle - 1;
            if (_opcode == 0x20)
            {
                return nextCycle == 3
                    && !(IsBranchOpcode(_previousOpcode)
                        && !IsBranchTaken(_previousOpcode));
            }

            // Extra after-LDA FETCH CLK (DebugCycle>=2) is force-stolen on BA
            // including RasterX 55 lag (Wolf64 2050510 native still frozen).
            // Dummy CLK (DebugCycle 1) is conditional only (Wolf64 2050006).
            // STA abs p2 FETCH (DebugCycle 2/3) is VICE FETCH_OPCODE check_ba.
            // CanSteal is false here (store nextCycle==1) so RasterX 12 still
            // runs (2145723). Mandatory BA (x=13-54) must freeze via this
            // force-steal (Wolf64 2520240). Cycle 1 is the write: no check_ba.
            return (IsBranchOpcode(_opcode) && _cycle > 2)
                || (_opcode == 0x8D && _cycle > 1);
        }
    }

    /// <inheritdoc />
    public bool NotifyOnStolenCycle =>
        _holdTakenBranchOpcodePc
        || _takenBranchStagedFallthrough
        || (IsBranchOpcode(_opcode) && IsBranchTaken(_opcode) && _cycle > 1
            && _previousOpcode == 0xB9)
        || (IsBranchOpcode(_opcode) && IsBranchTaken(_opcode) && _cycle > 1
            && _previousOpcode is 0xC8 or 0x88)
        || (IsZeroPageIncrementDecrementOpcode(_opcode) && _cycle > 0)
        || (_opcode == 0x8D && _cycle == 1)
        || (_opcode == 0xA5 && _cycle == 1)
        || (_opcode == 0x85 && _cycle <= 1);

    private bool _staAbsWriteCycleStolen;
    private bool _ldaZpDataReadStolen;
    private bool _staZpApplyStolen;
    /// <summary>
    /// VICE STORE/GET_ZERO on a stolen write/data-read is not DO_INTERRUPT.
    /// The following FETCH must run (wait-loop BEQ dummy, STA zp, STA abs)
    /// instead of sampling IRQ (Wolf64 2520240 / 3505560 / 3899688).
    /// </summary>
    private bool _skipIrqSampleAtNextFetch;

    /// <summary>
    /// Arms the IRQ dispatch sequence (TR-LOCKSTEP-VSF-001). Mirrors VICE's
    /// 7-cycle x64sc DO_INTERRUPT IRQ path (6510dtvcore.c:354-407 with
    /// DO_IRQBRK at :314-350): two dummy fetches at PC, PCH/PCL pushes, status
    /// push with B clear, then I is set and the $FFFE/$FFFF vector is read over
    /// two cycles; the JUMP becomes visible on the handler's first fetch cycle
    /// (the hosted per-cycle register export in c64cpusc.c CLK_INC). The system
    /// clock calls this at an instruction boundary, i.e. at the end of the tick
    /// that under this core's one-cycle-lag convention coincides with the
    /// native sequence's FIRST dummy cycle, so 6 explicit ticks remain (dummy,
    /// three pushes, two vector reads). A no-op when I is set or a sequence is
    /// already in flight. Tick() consumes the armed sequence one cycle at a
    /// time so BA steals can interleave exactly as on the single-cycle core.
    /// </summary>
    public void Irq()
    {
        if ((P & 0x04) != 0 || _interruptSequenceRemaining > 0)
            return;

        var visiblePcAtBoundary = _visiblePC;
        _interruptSequenceRemaining = 6;
        _interruptReturnPc = _pc;
        // The interrupted PC stays visible through the whole sequence (VICE
        // keeps exporting reg_pc until the JUMP after the vector fetch).
        // A taken branch can arm IRQ on its final dummy-clock checkpoint. VICE
        // has not executed the no-clock JUMP at that exported checkpoint yet,
        // so preserve whichever PC the branch path made bus-visible. The return
        // PC is still the already-computed branch target (Wolf64 sample 14954).
        _instructionPC = _interruptReturnPc;
        _visiblePC = _branchIrqArmingDummy
            ? visiblePcAtBoundary
            : _interruptReturnPc;
    }

    public void Nmi()
    {
        // NMI implementation - push PC and P to stack, clear I flag, jump to NMI vector
        PushWord(PC);
        Push((byte)(P & ~0x10)); // Push P with B flag clear
        P |= 0x04; // Set I flag (IRQs disabled during NMI)
        PC = Read(0xFFFA);
        PC |= (ushort)(Read(0xFFFB) << 8);
    }

    /// <summary>
    /// One cycle of the armed IRQ dispatch sequence (TR-LOCKSTEP-VSF-001),
    /// counting <see cref="_interruptSequenceRemaining"/> down 6..1. Micro-op
    /// order and visible register timing mirror VICE's x64sc DO_INTERRUPT +
    /// DO_IRQBRK (6510dtvcore.c:314-407) with the sequence's first dummy cycle
    /// absorbed by the arming boundary tick (this core's one-cycle-lag
    /// convention): cycle 6 dummy-reads the interrupted PC, cycles 5/4/3 push
    /// PCH/PCL/P (B clear; S decrements are visible on those cycles), cycle 2
    /// sets I and reads $FFFE, cycle 1 reads $FFFF and latches the new PC while
    /// the VISIBLE PC stays at the interrupted address until the handler's
    /// first fetch cycle, exactly like the hosted per-cycle register export
    /// (JUMP exported by the next CLK_INC in c64cpusc.c).
    /// </summary>
    private void ExecuteInterruptSequenceCycle()
    {
        switch (_interruptSequenceRemaining)
        {
            case 6:
                Read(_interruptReturnPc);
                break;
            case 5:
                // VICE DO_INTERRUPT (6510dtvcore.c): LOCAL_SET_BREAK(0) after the
                // dummy reads and before PUSH PCH (c=522269 nP=$21 mP=$31 when B
                // stayed set in managed P).
                P = (byte)(P & ~0x10);
                Push((byte)(_interruptReturnPc >> 8));
                break;
            case 4:
                Push((byte)_interruptReturnPc);
                break;
            case 3:
                Push((byte)(P & ~0x10));
                break;
            case 2:
                P |= 0x04;
                _interruptVector = Read(0xFFFE);
                break;
            case 1:
                _interruptVector |= (ushort)(Read(0xFFFF) << 8);
                _pc = _interruptVector;
                // The interrupted PC stays visible through this cycle (VICE
                // exports the JUMP only at the handler's first fetch cycle);
                // _delayNextFetch consumes that fetch cycle next tick, flipping
                // the visible PC to the handler and re-establishing the
                // one-cycle lag for the handler's first instruction.
                _instructionPC = _interruptReturnPc;
                _visiblePC = _interruptReturnPc;
                _suppressBootstrapBoundary = true;
                _delayNextFetch = true;
                break;
        }

        _interruptSequenceRemaining--;
    }

    private readonly IBus _bus;
    private IPubSub? _pubSub;

    public Func<ushort, bool>? ShouldDeferAbsoluteStore { get; set; }
    public Func<ushort, bool>? ShouldDelayNextFetchAfterWrite { get; set; }

    /// <summary>
    /// Optional pre-FETCH interrupt sample (SystemClock). Invoked after a
    /// soft-deferred body commits and before the next opcode is read, matching
    /// VICE DO_INTERRUPT before FETCH_OPCODE.
    /// </summary>
    public Action? TrySampleInterruptBeforeFetch { get; set; }

    /// <summary>
    /// Optional KERNAL-trap hook (the VICE serial-trap equivalent). Invoked at
    /// each instruction boundary with the address about to be fetched. If it
    /// returns true the trapped instruction is skipped: the handler has already
    /// mutated registers/memory and set <see cref="PC"/> to the routine's resume
    /// address. Used to service virtual (non-true-drive) disk I/O without
    /// bit-banging the IEC bus. Null on true-drive and cycle-parity rigs.
    /// </summary>
    public Func<ushort, bool>? SerialTrapHook { get; set; }

    public Mos6502(IBus bus)
    {
        _bus = bus;
    }

    public void ConnectPubSub(IPubSub pubSub)
    {
        _pubSub = pubSub ?? throw new ArgumentNullException(nameof(pubSub));
    }

    private byte _opcode;
    private ushort _currentInstructionPc; // fetch address of the in-flight instruction (for the completed-instruction publish)
    private bool _instructionExecuted; // guards the instruction-completed publish against the first (pre-fetch) boundary
    private int _cycle;
    private int _bootstrapCycles;
    private bool _suppressBootstrapBoundary;
    /// <summary>
    /// Open SystemClock interrupt sampling while suppress still forces host
    /// PC export to _visiblePC (post-JSR JUMP before callee FETCH; VICE
    /// DO_INTERRUPT in the main loop).
    /// </summary>
    private bool _interruptSampleDespiteSuppress;
    private bool _stagedMemoryReadCompleted;
    /// <summary>
    /// True when the staged load destination was written before the VICE
    /// data-read CLK checkpoint. LD applies N/Z and INC_PC immediately after
    /// that checkpoint, before the next exported clock.
    /// </summary>
    private bool _stagedLoadRegisterVisibleAtReadCheckpoint;
    private bool _delayNextFetch;
    /// <summary>
    /// After RTS JUMP (no CLK), VICE DO_INTERRUPT runs before FETCH of the
    /// return insn. Sample IRQ on the next host tick, not on the JUMP tick
    /// (Wolf64 2093250) and not after the return insn completes (2109682).
    /// </summary>
    private bool _sampleIrqBeforeNextFetch;
    /// <summary>
    /// Taken-branch JUMP has no VICE CLK. The next host tick is DO_INTERRUPT
    /// before target FETCH; Irq() on that tick is the first dummy (seq=6) and
    /// must not also ExecuteInterruptSequenceCycle (Wolf64 2175365).
    /// </summary>
    private bool _branchIrqArmingDummy;
    private bool _stagedNzUpdate;
    private byte _stagedNzValue;
    private bool _stagedCarryUpdate;
    private bool _stagedCarryValue;
    private bool _branchTargetFetchPending;
    private bool _branchPageCrossExtraPending;
    /// <summary>
    /// Host-visible cycles the previous instruction spent with PC already at the
    /// next opcode address. Drives host-visible taken-branch length vs xvic.
    /// </summary>
    private int _trailingCyclesAtNextPc;
    private int _currentInsnTrailingAtNextPc;
    /// <summary>Opcode address of the instruction currently in progress (stable for the whole instruction).</summary>
    private ushort _opcodeAddress;
    private bool _fullLengthTakenBranchCompleted;
    /// <summary>True while executing the instruction that immediately follows a full-length taken branch.</summary>
    private bool _afterFullLengthTakenBranch;
    /// <summary>
    /// True while executing the instruction that immediately follows a short
    /// taken branch (armAfterBranchLag path). Drives zp,X shift commit cycle.
    /// </summary>
    private bool _afterShortTakenBranchLag;
    /// <summary>
    /// After BA-skip zp INC, VICE GET_ZERO of the following LDA zp is visible
    /// on that LDA's first host tick (Wolf64 2129136 nA=$27 mA=$77).
    /// </summary>
    private bool _fuseLdaZpAfterStolenInc;
    /// <summary>
    /// CMP zp after that LDA: VICE CP GET_ZERO+INC_PC is visible on DebugCycle 1
    /// (Wolf64 2129140 nPC=$E6BF nP=$31).
    /// </summary>
    private bool _fuseCmpZpAfterStolenIncLda;
    /// <summary>
    /// After that fused CMP, VICE BRANCH dummy INC_PC is the first host tick
    /// (Wolf64 2129142 nPC=$E6C1). Sticky non-overlapped from the INC steal
    /// must not add a full-length extra FETCH CLK.
    /// </summary>
    private bool _overlapNextTakenBranchDummy;
    /// <summary>
    /// Overlapped FETCH already consumed the first BRANCH CLK, so a page-cross
    /// must not add another un-fixed-PC tick after JUMP (Wolf64 2129144
    /// nPC=$E700 mPC=$E6C1).
    /// </summary>
    private bool _skipBranchPageCrossExtra;
    /// <summary>
    /// BA-skip zp INC already INC_PC'd; FETCH of the following taken BNE is
    /// VICE dummy INC_PC (Wolf64 2093352 nPC=$F6A1). Unstalled INC that
    /// already showed next-PC does not dummy-export (Wolf64 2142574 nPC=$F69F).
    /// </summary>
    private bool _dummyTakenBneAfterStolenInc;
    /// <summary>
    /// VICE last_opcode_info DELAYS_INTERRUPT: set when a taken branch did not
    /// cross a page (6510dtvcore.c BRANCH else of PBC). Cleared at the next
    /// opcode fetch (SET_LAST_OPCODE of the following instruction).
    /// </summary>
    private bool _lastOpcodeDelaysInterrupt;
    private bool _lastOpcodeEnablesIrq;
    /// <summary>
    /// After a full-length taken branch (and its non-overlapped JSR), the next
    /// instruction also lacks first-FETCH overlap until phase re-couples.
    /// </summary>
    private bool _nonOverlappedFetchPhase;
    /// <summary>
    /// Sticky for the whole non-overlapped subroutine region (full-length BNE
    /// through RTS), so lag-shaped shortcuts (e.g. STA collapsing into RTS)
    /// stay off until return.
    /// </summary>
    private bool _nonOverlappedRegion;
    /// <summary>Trailing dwell captured at last instruction fetch (for branch resolve).</summary>
    private int _branchFetchPriorTrailing;
    /// <summary>Not-taken branch must hold opcode PC on final CLK (clean first FETCH).</summary>
    private bool _notTakenBranchHoldFinalPc;
    /// <summary>Defer zp RMW PC advance one CLK after not-taken branch with clean FETCH.</summary>
    private bool _deferZpRmwPcAdvanceOne;
    /// <summary>JSR following that deferred zp RMW has a clean first FETCH.</summary>
    private bool _nextJsrNonOverlapped;
    /// <summary>
    /// INC zp cycle 2 still opcode PC (trail 0). Following INY last FETCH CLK
    /// keeps pre-op Y (Wolf64 4147669). INC cycle 2 already at next-PC fuses
    /// INY (4134618).
    /// </summary>
    private bool _inySoftAfterHeldInc;
    private bool _callTargetFetchPending;
    private bool _callTargetFetchNonOverlapped;

    private bool _deferImpliedRegisterCompletionAfterBranch;
    private bool _deferAbsoluteXLoadCompletionAfterBranch;
    private bool _deferAbsoluteYLoadCompletionAfterBranch;
    /// <summary>
    /// Stolen same-page taken BPL JUMP has no CLK. The following INY then
    /// LDA abs,Y GET_ABS_Y commits A on the data-read CLK (Wolf64 4150686
    /// nA=$4E mA=$4F). Do not use trail>=1/implied-previous (4138196).
    /// </summary>
    private bool _fuseAbsYAfterStolenSamePageBranch;
    /// <summary>
    /// STA abs,Y after a full-length taken branch already exported next-PC.
    /// VICE DEC/INC zp GET_ZERO still holds opcode PC (Wolf64 4154106).
    /// Do not hold every $99 trail>=1 (2125150 nPC already opcode+2).
    /// </summary>
    private bool _holdZpIncDecAfterStaAbsY;
    private bool _deferJsrPushAfterBranch;
    private bool _deferIndirectYLoadCompletionAfterBranch;
    /// <summary>
    /// VICE INT_IND_Y_R extra CLK when pointer-low+Y page-crosses.
    /// </summary>
    private bool _indyPageCrossedThisInsn;
    private bool _deferZeroPageRmwPcAdvanceAfterBranch;
    private bool _deferNextIndirectYLoadAfterBranchRmw;
    /// <summary>
    /// BA skipped the zp INC/DEC cycle-2 INC_PC. Apply before FETCH of the next opcode.
    /// </summary>
    private bool _zpRmwPcDeferredFromSteal;
    private bool _deferIndexedStorePcAdvanceAfterBranch;
    private bool _deferZeroPageIndexedStorePcAdvanceAfterBranch;
    /// <summary>
    /// zp INC/DEC already applied NZ + modified value at the PC-advance CLK
    /// (VICE 6510core.c INC/DEC: LOCAL_SET_NZ + INC_PC before final STORE).
    /// Cycle 1 only performs the final write.
    /// </summary>
    private bool _zpRmwModifyCommitted;
    /// <summary>
    /// Last CLK of an implied flag op (CLC/CLI/SEC/SEI/...) already exported
    /// next-PC through the host PC getter. Wolf64 2407215: CLC at $E5C8 last
    /// CLK shows $E5C9 so RTS FETCH is overlapped. Visible-PC trail can stay 0
    /// because _visiblePC lagged at the CLC opcode. Not a blanket previous-flag
    /// key (that pulled RTS early at 2103097).
    /// </summary>
    private bool _impliedFlagLastClkExportedNextPc;
    /// <summary>Opcode of the instruction that just finished (for RTS phase).</summary>
    private byte _previousOpcode;
    /// <summary>
    /// PLA pulled A pending NZ/PC application after the PULL CLK and before the
    /// next opcode's first FETCH CLK.
    /// </summary>
    private bool _pendingPlaCompletion;
    /// <summary>
    /// PLP pulled status byte pending apply after the PULL CLK (VICE PLP:
    /// LOCAL_SET_STATUS after CLK_INC on the pull).
    /// </summary>
    private bool _pendingPlpStatus;
    /// <summary>BIT soft-defer at cycle 1 when first FETCH was overlapped.</summary>
    private bool _bitSoftDeferEarly;
    /// <summary>
    /// Soft-deferred BIT latched data-read (VICE GET_ABS/GET_ZERO once). Soft-apply
    /// must not re-LOAD: post-defer VIA/CLK advance can change T1 and N/V/Z
    /// (NTSC c=521027 nP=$E5 mP=$A5 after BIT $9124 when re-ExecuteOpcode re-read).
    /// </summary>
    private bool _softDeferredBitLatched;
    private byte _softDeferredBitValue;
    /// <summary>Next instruction fetch follows RTS delayNextFetch (phase for BIT).</summary>
    private bool _fetchAfterRtsDelay;
    /// <summary>
    /// CMP zp/zx (and CPX/CPY zp) after clean taken-branch fetch: soft-defer body
    /// so final CLK keeps pre-op P/PC (c=532264 nPC=$E8FE mPC=$E900 nP=$20 mP=$21).
    /// </summary>
    private bool _softDeferZpCompare;
    /// <summary>
    /// JMP abs after clean FETCH (e.g. after zp shift RMW trail=2): VICE still
    /// exports opcode PC on the final CLK; JUMP is after that sample (c=539731
    /// nPC=$E712 mPC=$ED5B).
    /// </summary>
    private bool _softDeferJmpAbs;
    private ushort _pendingJmpTarget;
    /// <summary>
    /// The current instruction is the target reached when a soft-deferred JMP
    /// applied its unclocked JUMP immediately before this instruction's FETCH.
    /// Preserve that distinct VICE source phase through the target body only.
    /// </summary>
    private bool _targetInstructionFollowsSoftDeferredJmp;
    /// <summary>
    /// Taken branch with multi-cycle host budget (full-length and/or after-branch
    /// lag): export fall-through PC at cycle 1 to match VICE INC_PC+dummy before
    /// JUMP (c=540201 nPC=$D92B mPC=$D929 on BPL).
    /// </summary>
    private bool _takenBranchStagedFallthrough;
    /// <summary>
    /// After staged multi-cycle taken JUMP, next non-load 2-byte imm is fused
    /// (c=540204 ADC#); skip one soft-defer. Latched at fetch into
    /// _applySkipSoftImmThisInsn so staged cycle-0 handlers cannot leave the
    /// arm sticky across unrelated later CMP# (c=518756).
    /// </summary>
    private bool _skipSoftImmAfterStagedTakenBranch;
    /// <summary>
    /// VICE ST(SET_ABS) followed by a full staged taken branch has already
    /// exported the dummy clock before its target JUMP. A target immediate LD
    /// therefore completes on its second FETCH checkpoint (Wolf64 119344).
    /// </summary>
    private bool _skipImmediateLoadAfterAbsoluteStoreBranch;
    private bool _applySkipSoftImmThisInsn;
    /// <summary>
    /// A branch whose immediate predecessor owns the JUMP checkpoint reaches
    /// its target on the following FETCH. Latch that source phase onto
    /// exactly the target instruction so zero-page ST keeps GET_ZERO PC timing.
    /// </summary>
    private bool _deferredBranchJumpTargetFetch;
    private bool _targetInstructionFollowsDeferredBranchJump;
    /// <summary>
    /// After fused LDA (zp),Y then fused INY/TAX chain in nonOvl, keep fusing
    /// implied ops until a non-implied is fetched (c=541175 INY, c=541177 TAX).
    /// </summary>
    private bool _fuseImpliedAfterIndyLoad;
    /// <summary>
    /// STA/STX/STY after fused implied (ROL A / INY): VICE ST INC_PC is already
    /// visible on the write sample (Wolf64 2405849 nPC=$EADA). Latched at FETCH
    /// before the implied-fuse chain is cleared.
    /// </summary>
    private bool _advanceStorePcAfterFusedImplied;
    /// <summary>
    /// LDA (zp),Y last CLK already INC_PC when previous exported next-PC
    /// (Wolf64 2406426 nPC=$EAB9). Latched at FETCH.
    /// </summary>
    private bool _fuseIndyLoadLastClkPc;
    /// <summary>
    /// After fused LDA zp following a taken branch, the next not-taken branch's
    /// non-load imm fuses (c=541202 EOR#). Must not fire on plain BCC then CMP#
    /// (c=518756 nPC=opcode mPC advanced when over-fused).
    /// </summary>
    private bool _fuseNonLoadImmAfterLoadBranch;
    /// <summary>
    /// VICE can execute a two-byte non-load immediate body in the FETCH phase
    /// already exposed by a fused implied predecessor. Preserve that source-order
    /// phase through exactly one following branch so its fall-through instruction
    /// does not manufacture another FETCH checkpoint.
    /// </summary>
    private bool _fusedNonLoadImmediateConsumedFetch;
    private bool _branchFollowsFusedNonLoadImmediate;

    /// <summary>
    /// After a short taken branch, NOP's final host sample must still export the
    /// opcode PC (VICE INC_PC after the last FETCH CLK). The hold phase sticks
    /// across a run of NOPs (c=518739 first, c=518741 second) until a non-NOP.
    /// </summary>
    private bool _nopHoldOpcodePcOnFinal;
    private bool _nopChainHoldAfterBranch;
    /// <summary>
    /// After a post-branch NOP chain, the next instruction's first FETCH is fused
    /// on VICE with the last NOP's INC_PC; soft-defer 2-byte immediate commit so
    /// the final host sample still shows the opcode PC (c=518751 nPC=$E823 mPC=$E825).
    /// </summary>
    private bool _softDeferAfterNopChain;
    private bool _indexedStorePcAdvanceWasDeferred;
    private bool _indexedLoadPageCrossDelayConsumed;
    private bool _pendingDeferredNzUpdateAfterBranch;
    /// <summary>
    /// Soft deferred immediate: apply A/X/Y at the start of the next instruction
    /// fetch without consuming an extra host-visible cycle (non-overlapped phase).
    /// </summary>
    private bool _softDeferredImmediateLoad;
    /// <summary>
    /// True only for a JSR fetched on the same host tick that an immediate load
    /// completes its unclocked LD body.
    /// </summary>
    private bool _jsrFollowsSoftDeferredImmediateLoad;
    /// <summary>
    /// The current immediate load was fetched on the tick that a soft-deferred
    /// implied body completed, so its second FETCH clock remains distinct.
    /// </summary>
    private bool _immediateLoadFollowsSoftDeferredBody;
    /// <summary>
    /// The preceding immediate load completed on its current FETCH checkpoint
    /// instead of creating a distinct second-fetch phase. Consecutive immediate
    /// loads inherit that VICE source ordering.
    /// </summary>
    private bool _immediateLoadCompletedWithoutDistinctFetch;
    /// <summary>
    /// After the held STA (zp),Y path, a following INY/DEY must keep its
    /// opcode PC and pre-operation register value on the final host sample.
    /// The implied operation commits during the following fetch.
    /// </summary>
    private bool _softDeferImpliedAfterHeldIndYStore;
    /// <summary>
    /// After that STA path, following CMP (zp),Y must export old flags + opcode PC
    /// on its final CLK (VICE CP applies flags/INC_PC after the last CLK sample).
    /// </summary>
    private bool _softDeferCompareCommit;
    private bool _pendingSoftCompareCommit;
    private bool _softDeferredImpliedOp;
    private byte _softDeferredImpliedOpcode;
    private ushort _softDeferredImpliedInstructionPc;
    private bool _pendingDeferredImpliedRegisterCompletion;
    /// <summary>
    /// A not-taken branch may fetch a fall-through RTS inside the branch body.
    /// That prefetch does not make the branch's prior opcode an RTS overlap.
    /// </summary>
    private bool _rtsPrefetchedByNotTakenBranch;
    private ushort _stagedReturnAddress;
    private ushort _effectiveAddress;
    private byte _fetched;
    private int _interruptSequenceRemaining;
    private ushort _interruptReturnPc;
    private ushort _interruptVector;

    public void Tick()
    {
        // Per-CPU executed-cycle counter (FR-CPUTICK-001): Tick() is invoked once per
        // cycle this CPU actually executes - the clock skips it on stolen cycles - so a
        // simple increment here counts executed cycles only, independently of the shared
        // system clock and of any other CPU in the rig.
        _executedCycles++;
        ConsumedViceClockThisTick = true;
        ViceClocksThisTick = 1;

        // Track host-visible PC dwell after every path (staged handlers return early).
        try
        {
            TickCore();
        }
        finally
        {
            if (_visiblePC != _opcodeAddress)
            {
                _currentInsnTrailingAtNextPc++;
                _trailingCyclesAtNextPc = _currentInsnTrailingAtNextPc;
            }

            if (_cycle == 0 && IsImpliedFlagOpcode(_opcode))
            {
                var exported = !_suppressBootstrapBoundary ? _pc : _visiblePC;
                _impliedFlagLastClkExportedNextPc = exported != _opcodeAddress;
            }
            else if (_cycle == 0)
            {
                _impliedFlagLastClkExportedNextPc = false;
            }
        }
    }

    private void TickCore()
    {
        var completedImmediateLoadBeforeFetch = false;
        var fetchingBranchTarget = false;
        var fetchingNonOverlappedCallTarget = false;
        if (_suppressBootstrapBoundary)
        {
            fetchingBranchTarget = _branchTargetFetchPending;
            fetchingNonOverlappedCallTarget =
                _callTargetFetchPending && _callTargetFetchNonOverlapped;
            _suppressBootstrapBoundary = false;
            // RTS/taken-branch JUMP delayNextFetch tick must not drop the
            // pending target FETCH (Wolf64 2122119 BPL after BNE).
            if (!_delayNextFetch)
            {
                _branchTargetFetchPending = false;
                _callTargetFetchPending = false;
                _callTargetFetchNonOverlapped = false;
            }
        }

        if (_fetchAfterStolenB9Jump)
        {
            // JUMP tick already exported fall-through. This host tick is
            // FETCH of the target (Wolf64 4139089 nPC=$A5B8). Do not consume
            // a leftover page-cross extra CLK: that set tgtPend and returned
            // without FETCHing.
            _fetchAfterStolenB9Jump = false;
            _baDelayedFetchClk = false;
            _delayNextFetch = false;
            _branchPageCrossExtraPending = false;
            _cycle = 0;
        }

        // One-tick post-JSR IRQ sample window ends before this tick's FETCH.
        _interruptSampleDespiteSuppress = false;

        if (_baDelayedFetchClk && _cycle > 0)
        {
            // Consume the delayed FETCH CLK_INC only while the branch is still
            // in its operand-fetch phase. If it were consumed on the first
            // unstalled raster X of a steal burst, dummy/fall-through ran one
            // CLK too soon vs VICE (c=2029945).
            _baDelayedFetchClk = false;
            _visiblePC = _instructionPC;
            _suppressBootstrapBoundary = true;
            return;
        }

        if (_sampleIrqBeforeNextFetch)
        {
            _sampleIrqBeforeNextFetch = false;
            var interruptWasInactive = _interruptSequenceRemaining == 0;
            TrySampleInterruptBeforeFetch?.Invoke();
            if (interruptWasInactive && _interruptSequenceRemaining > 0 && !_branchIrqArmingDummy)
            {
                // Ordinary pre-FETCH entry reaches this checkpoint after VICE's
                // second LOAD_DUMMY CLK_INC. The following host clock therefore
                // starts with PUSH PCH (sequence state 5). A taken-branch JUMP
                // uses the separate path below because its JUMP has no CLK and
                // has not yet exported the first dummy checkpoint.
                if (!(_afterFullLengthTakenBranch || _afterShortTakenBranchLag))
                    _interruptSequenceRemaining = 5;
                return;
            }
            if (_branchIrqArmingDummy)
            {
                _branchIrqArmingDummy = false;
                if (_interruptSequenceRemaining > 0)
                {
                    // JUMP tick already armed IRQ (seq=6) without executing the
                    // first dummy. The no-clock branch JUMP becomes visible on
                    // this first IRQ dummy checkpoint, then the push lands on
                    // the same host tick native drops S (Wolf64 14955/2224633).
                    _instructionPC = _interruptReturnPc;
                    _visiblePC = _interruptReturnPc;
                    // This checkpoint is the first of VICE's two IRQ dummy
                    // reads. Keep sequence state at 6 so the normal case-6
                    // cycle performs the second dummy before PCH is pushed.
                    Read(_interruptReturnPc);
                    return;
                }
            }
        }

        if (_interruptSequenceRemaining > 0)
        {
            ExecuteInterruptSequenceCycle();
            return;
        }

        if (_branchPageCrossExtraPending)
        {
            // TR-LOCKSTEP-VSF-001: the taken-branch page-cross fix-up cycle
            // (native BRANCH C4); the fall-through PC stays visible and the
            // target fetch (with its after-branch defer arming) runs next tick.
            _branchPageCrossExtraPending = false;
            _branchTargetFetchPending = true;
            _suppressBootstrapBoundary = true;
            return;
        }

        if (_pendingDeferredNzUpdateAfterBranch)
        {
            // Do not steal the final cycle of a staged (zp),Y load/compare; finish
            // that apply first so A/flags land on the native data-read export.
            if (!(_stagedMemoryReadCompleted && (_opcode is 0xB1 or 0xD1)))
            {
                CompleteDeferredNzUpdateAfterBranch();
                return;
            }
        }

        if (_delayNextFetch)
        {
            _instructionPC = _pc;
            _visiblePC = _pc;
            _delayNextFetch = false;
            // Following fetch is phase-coupled like a clean first FETCH for BIT
            // host-sample count (c=522467 held late after RTS return).
            _fetchAfterRtsDelay = true;
            // VICE RTS/IRQ JUMP has no CLK; DO_INTERRUPT is paired with the
            // next FETCH. This host tick only exports the jump PC.
            // Sampling IRQ here stole LDA #$20 (Wolf64 2093250). The following
            // tick samples before FETCH (Wolf64 2109682 TYA).
            _sampleIrqBeforeNextFetch = true;
            _suppressBootstrapBoundary = true;
            ConsumedViceClockThisTick = false;
            return;
        }

        if (_bootstrapCycles > 0)
        {
            _bootstrapCycles--;
            _suppressBootstrapBoundary = true;
            return;
        }

        if (_pendingDeferredImpliedRegisterCompletion)
        {
            CompleteDeferredImpliedRegisterCompletion();
            return;
        }

        // VICE completes PLA/PLP post-pull work without a CLK, then continues
        // directly into DO_INTERRUPT and the next FETCH_OPCODE. Apply that work
        // before the instruction-boundary block and do not create a host sample.
        if (_pendingPlaCompletion)
        {
            _pendingPlaCompletion = false;
            UpdateNZ(A);
            _pc = (ushort)(_instructionPC + 1);
            _visiblePC = _pc;
            _nonOverlappedFetchPhase = true;
        }

        if (_pendingPlpStatus)
        {
            _pendingPlpStatus = false;
            P = (byte)(_fetched | 0x20);
            _pc = (ushort)(_instructionPC + 1);
            _visiblePC = _pc;
            _nonOverlappedFetchPhase = true;
        }

        if (_cycle == 0)
        {
            if (_zpRmwPcDeferredFromSteal
                || (IsZeroPageIncrementDecrementOpcode(_opcode)
                    && _instructionExecuted
                    && _visiblePC == _opcodeAddress))
            {
                // VICE INC_PC(2) once. If visible PC is still the INC opcode,
                // step to opcode+2. If dummy/store already exported next-PC
                // (KERNAL INC $A2 at $F69D shows $F69F), do not add 2 again
                // (that skipped BNE $F69F and landed on INC $A1; Wolf64
                // 2093353 nPC=$F6A7 SEC vs mPC=$F6A1 E6).
                var afterBaSkip = _zpRmwPcDeferredFromSteal;
                if (_visiblePC == _opcodeAddress)
                    _pc = (ushort)(_opcodeAddress + 2);
                else
                    _pc = _visiblePC;
                _visiblePC = _pc;
                _zpRmwPcDeferredFromSteal = false;
                if (afterBaSkip)
                {
                    // Following FETCH is VICE FETCH_OPCODE (not overlapped
                    // with INC's last CLK). Taken BNE then gets the dummy
                    // CLK (Wolf64 2093352 nPC=$F6A1 vs mPC=$F69F).
                    _nonOverlappedFetchPhase = true;
                    _nonOverlappedRegion = true;
                    // LDA zp after that INC: GET_ZERO is already visible on
                    // the first host tick (Wolf64 2129136 nA=$27).
                    _fuseLdaZpAfterStolenInc = true;
                    _dummyTakenBneAfterStolenInc = true;
                }
            }

            // Instruction boundary: the previous instruction has fully executed.
            // Publish it (opcode + post-execution registers) for diagnostic / pacing
            // subscribers. Gated on a live subscriber so an unobserved run pays only
            // a null + count check per instruction; pure notification, so cycle parity
            // is unaffected.
            if (_instructionExecuted && _pubSub is { SubscriptionCount: > 0 })
            {
                _pubSub.Publish(
                    CpuInstructionCompletedEvent.Topic,
                    new CpuInstructionCompletedEvent(_currentInstructionPc, _opcode, A, X, Y, S, P, _pc));
            }

            // Soft-deferred JMP abs: apply JUMP before fetching the target insn.
            // Target's first FETCH is clean (c=539733 INX nX/nPC lag without this).
            // Clear the one-instruction provenance at every boundary, then set it
            // only when this FETCH is reached by the deferred unclocked JUMP.
            _targetInstructionFollowsSoftDeferredJmp = false;
            if (_pendingJmpTarget != 0 && !_softDeferJmpAbs)
            {
                _pc = _pendingJmpTarget;
                _pendingJmpTarget = 0;
                _nonOverlappedFetchPhase = true;
                _targetInstructionFollowsSoftDeferredJmp = true;
            }

            // Soft-deferred immediate from previous insn: commit A/X/Y while
            // _instructionPC / _opcode still name that previous immediate load.
            if (_softDeferredImmediateLoad)
            {
                CompleteDeferredImmediateLoad();
                _softDeferredImmediateLoad = false;
                // LD performs its register/flag updates and INC_PC after GET_IMM's
                // final CLK. Preserve that source phase for the opcode fetched below.
                completedImmediateLoadBeforeFetch = true;
                // VICE DO_INTERRUPT before next FETCH. Soft-deferred imm commits
                // on the same host tick as that FETCH — sample IRQ first
                // (NTSC c=520829). If IRQ arms, skip FETCH this tick.
                _suppressBootstrapBoundary = false;
                _interruptSampleDespiteSuppress = true;
                TrySampleInterruptBeforeFetch?.Invoke();
                if (_interruptSequenceRemaining > 0)
                    return;
                _suppressBootstrapBoundary = true;
                _interruptSampleDespiteSuppress = false;
            }

            // Soft-deferred implied (CLC/TAX/TXA/...) or post-NOP-chain
            // 2-byte immediate: commit after last-CLK sample.
            var afterSoftBody = false;
            if (_softDeferredImpliedOp)
            {
                var savedOpcode = _opcode;
                var savedInsnPc = _instructionPC;
                var softOp = _softDeferredImpliedOpcode;
                _opcode = softOp;
                _instructionPC = _softDeferredImpliedInstructionPc;
                if (_softDeferredBitLatched && softOp is 0x2C or 0x24)
                {
                    // VICE BIT(GET_*): one LOAD, then LOCAL_SET_N/V/Z + INC_PC
                    // with no extra CLK (6510dtvcore.c). Apply latched GET only.
                    var bitVal = _softDeferredBitValue;
                    P = (byte)((P & 0x3D) | (bitVal & 0xC0));
                    if ((A & bitVal) == 0)
                        P |= 0x02;
                    // SoftDefer left PC at opcode+1; abs needs +2 more operand
                    // bytes (zp +1) so next FETCH is the following insn.
                    _pc = (ushort)(_softDeferredImpliedInstructionPc + (softOp == 0x2C ? 3 : 2));
                    _softDeferredBitLatched = false;
                }
                else
                {
                    ExecuteOpcode(softOp);
                }

                _opcode = savedOpcode;
                _instructionPC = savedInsnPc;
                _softDeferredImpliedOp = false;
                // Following insn has a clean first FETCH (same as afterSoftCompare).
                afterSoftBody = true;
            }

            // Soft-deferred CMP: apply C/NZ after VICE's last-CLK sample.
            var afterSoftCompare = false;
            if (_pendingSoftCompareCommit)
            {
                if (_stagedCarryUpdate)
                {
                    if (_stagedCarryValue)
                        P |= 0x01;
                    else
                        P &= 0xFE;
                    _stagedCarryUpdate = false;
                }

                if (_stagedNzUpdate)
                {
                    UpdateNZ(_stagedNzValue);
                    _stagedNzUpdate = false;
                }

                _pendingSoftCompareCommit = false;
                // Next instruction's first FETCH is not overlapped (CMP left PC
                // on its opcode address through its final CLK).
                afterSoftCompare = true;
            }

            afterSoftCompare = afterSoftCompare || afterSoftBody;

            _instructionPC = _pc;
            _visiblePC = _instructionPC;
            _currentInstructionPc = _pc; // the instruction about to be fetched here
            _opcodeAddress = _pc;

            // KERNAL serial-bus trap (VICE virtual device traps). If a trap fires
            // it has set PC to the routine's resume address; skip the trapped
            // instruction and re-fetch from there on the next cycle. The hook is
            // a no-op (returns false) unless a virtual disk is being addressed,
            // so cycle-accurate behaviour is unchanged in every other case.
            if (SerialTrapHook is not null && SerialTrapHook(_pc))
            {
                return;
            }

            // Capture previous instruction's trailing dwell, then start fresh.
            var priorTrailingAtNextPc = _trailingCyclesAtNextPc;
            DebugPriorTrailingAtNextPc = priorTrailingAtNextPc;
            _trailingCyclesAtNextPc = 0;
            _currentInsnTrailingAtNextPc = 0;

            // VICE DO_INTERRUPT before every FETCH (6510dtvcore mainloop).
            // DELAYS only raises the irq_delay threshold (+1), it does not
            // skip the check. Sampling only when DELAYS is set let BPL after
            // LDA abs,Y run and JUMP before IRQ (Wolf64 4146007 nS=$F8
            // nPC=$A5FD nLastOp=$B9 vs mPC=$A5F9 irqSeq=6). Stolen STA/LDA
            // zp STORE/GET_ZERO is not that boundary (2520240 / 3505560 /
            // 3899688): FETCH the following insn instead.
            if (!_skipIrqSampleAtNextFetch)
            {
                TrySampleInterruptBeforeFetch?.Invoke();
                if (_interruptSequenceRemaining > 0)
                    return;
            }
            _skipIrqSampleAtNextFetch = false;

            // Preserve the prior opcode for instruction-entry phase classification.
            _previousOpcode = _opcode;
            _opcode = Read(_pc++);
            _immediateLoadFollowsSoftDeferredBody =
                afterSoftBody && IsImmediateLoadOpcode(_opcode);
            _jsrFollowsSoftDeferredImmediateLoad =
                completedImmediateLoadBeforeFetch && _opcode == 0x20;
            if (_softDeferImpliedAfterHeldIndYStore
                && _opcode != 0xC8
                && _opcode != 0x88)
            {
                _softDeferImpliedAfterHeldIndYStore = false;
            }
            _advanceStorePcAfterFusedImplied = IsStoreOpcode(_opcode)
                && IsImpliedRegisterOrFlagOpcode(_previousOpcode)
                && (_fuseImpliedAfterIndyLoad || priorTrailingAtNextPc >= 1);
            // Staged stores return before the generic cycle-0 chain cleanup.
            // Preserve their entry phase above, then end the prior implied chain.
            if (IsStoreOpcode(_opcode))
                _fuseImpliedAfterIndyLoad = false;
            if (_fuseLdaZpAfterStolenInc)
            {
                if (_opcode == 0xA5)
                {
                    A = Read(ReadZeroPageOperand());
                    _fuseCmpZpAfterStolenIncLda = true;
                }
                else
                    _fuseLdaZpAfterStolenInc = false;
            }

            if (_dummyTakenBneAfterStolenInc
                && !(IsBranchOpcode(_opcode) && IsBranchTaken(_opcode)))
                _dummyTakenBneAfterStolenInc = false;
            // VICE SET_LAST_OPCODE of this insn replaces prior DELAYS_INTERRUPT
            // and ENABLES_IRQ (only taken same-page BRANCH sets delay again on JUMP).
            _lastOpcodeDelaysInterrupt = false;
            _lastOpcodeEnablesIrq = false;
            // One-instruction skip-soft-imm windows latch at the target fetch
            // after a staged branch JUMP, then always clear so cycle-0 handlers
            // cannot leave either arm sticky across unrelated immediate bodies.
            var skipImmediateLoadAfterFusedImmediateBranch =
                _branchFollowsFusedNonLoadImmediate
                && IsBranchOpcode(_previousOpcode)
                && !IsBranchTaken(_previousOpcode)
                && IsImmediateLoadOpcode(_opcode);
            _applySkipSoftImmThisInsn =
                (_skipSoftImmAfterStagedTakenBranch
                    && IsTwoByteImmediateOpcode(_opcode)
                    && !IsImmediateLoadOpcode(_opcode))
                || ((_skipImmediateLoadAfterAbsoluteStoreBranch
                        || skipImmediateLoadAfterFusedImmediateBranch)
                    && IsImmediateLoadOpcode(_opcode));
            _branchFollowsFusedNonLoadImmediate =
                IsBranchOpcode(_opcode) && _fusedNonLoadImmediateConsumedFetch;
            _fusedNonLoadImmediateConsumedFetch = false;
            _skipSoftImmAfterStagedTakenBranch = false;
            _skipImmediateLoadAfterAbsoluteStoreBranch = false;
            _targetInstructionFollowsDeferredBranchJump = _deferredBranchJumpTargetFetch;
            _deferredBranchJumpTargetFetch = false;
            // deferZpRmwPcAdvanceOne is only for the zp INC/DEC immediately after a
            // not-taken branch clean FETCH; sticky flag delayed INC NZ/PC by one
            // cycle far from any branch (c=522317 nP=$25 mP=$27 nPC advanced).
            if (!IsZeroPageIncrementDecrementOpcode(_opcode))
                _deferZpRmwPcAdvanceOne = false;
            _instructionExecuted = true; // a real instruction has now been fetched/executed
            _cycle = GetCycleCount(_opcode);
            // Match host-visible xvic taken-branch length (VICE BRANCH is always
            // 3 CLK; first FETCH may be overlapped with the previous opcode's
            // trailing export at this same address).
            // Empirically vs xvic GetPC after each step_cycle:
            //   dwell 1 (e.g. CMP last cycle only): 2 host-visible branch steps
            //   dwell 2 (e.g. zp INC after deferred mid-PC advance): 3 host steps
            //   dwell 3 (e.g. zp INC with early AdvanceVisiblePc): 2 host steps
            // Full-length taken budget only for the dwell==2 case (cycle 5005).
            var fullLengthTakenBranch = false;
            _branchFetchPriorTrailing = priorTrailingAtNextPc;
            // Clean first FETCH (not overlapped with previous last export).
            var cleanBranchFetch = priorTrailingAtNextPc == 2 || afterSoftCompare
                || _nonOverlappedRegion || _nonOverlappedFetchPhase;
            // Not-taken: hold opcode PC on final CLK when first FETCH is clean
            // (incl. sticky non-overlapped region after soft BIT/imm; c=522430).
            // Skip hold when priorTrailing>=3: VICE already exports fall-through
            // on that final CLK (c=540321 nPC=$D92B mPC=opcode with hold).
            // Skip hold after fused INY/TAX chain (c=541179) or fused LDA zp
            // after taken branch with trail 1-2 (c=541200 BCC). After fused
            // STX zp trail 1-2, not-taken BCC last CLK already INC_PC
            // (Wolf64 4134474 nPC=$A496). Keep hold when trail==0 after load
            // (c=518764 nPC=opcode mPC=fall-through).
            // Arm one-shot non-load-imm fuse only for the IMMEDIATELY following
            // insn after that BCC (c=541202 EOR#). Clear otherwise so a later
            // CMP# after a different BCC does not over-fuse (c=518756).
            // Also after CMP abs (c=559294 BNE nPC=fall-through mPC=opcode).
            var skipHoldAfterFusedLoad = (IsLoadOpcode(_previousOpcode)
                    || IsStagedCompareOpcode(_previousOpcode)
                    || IsUnstagedAbsoluteCompareOpcode(_previousOpcode)
                    || IsZeroPageCompareOpcode(_previousOpcode)
                    || IsStoreOpcode(_previousOpcode)
                    || _previousOpcode is 0x2C or 0x24
                    || (IsTwoByteImmediateOpcode(_previousOpcode)
                        && !IsImmediateLoadOpcode(_previousOpcode)))
                && priorTrailingAtNextPc is 1 or 2;
            if (_fuseNonLoadImmAfterLoadBranch
                && !(IsTwoByteImmediateOpcode(_opcode) && !IsImmediateLoadOpcode(_opcode))
                && !IsBranchOpcode(_opcode))
                _fuseNonLoadImmAfterLoadBranch = false;
            if (IsBranchOpcode(_opcode) && skipHoldAfterFusedLoad)
                _fuseNonLoadImmAfterLoadBranch = true;
            // After taken BNE, VICE not-taken BPL last CLK already INC_PC
            // (Wolf64 2122119 nPC=$B92B mPC=$B929). The RTS/JMP delay tick
            // can consume _branchTargetFetchPending before this FETCH, so
            // previous-branch is the reliable skip.
            var skipHoldAfterTakenBranch = IsBranchOpcode(_previousOpcode);
            // After SBC abs,Y page-cross last-CLK hold, VICE not-taken BEQ
            // still exports opcode PC (Wolf64 4147654 nPC=$A5BF). Sticky
            // fuseImplied from an earlier INY must not skip that hold.
            _notTakenBranchHoldFinalPc = IsBranchOpcode(_opcode) && cleanBranchFetch
                && priorTrailingAtNextPc < 3
                && !(_fuseImpliedAfterIndyLoad && !IsAbsoluteAluOpcode(_previousOpcode))
                && !skipHoldAfterFusedLoad
                && !skipHoldAfterTakenBranch;
            // Full-length taken branch when previous left 2 trailing dwells at next
            // PC (zp INC deferred), or when soft-deferred CMP just committed so
            // the first FETCH of this branch is not overlapped (cycle 5034).
            // Fused CMP after stolen INC already overlapped FETCH1: this host
            // tick is VICE dummy INC_PC (Wolf64 2129142 nPC=$E6C1). Do not add
            // a full-length extra FETCH from sticky non-overlapped.
            var overlapTakenBranchDummy = _overlapNextTakenBranchDummy;
            _overlapNextTakenBranchDummy = false;
            if (IsBranchOpcode(_opcode) && IsBranchTaken(_opcode) && overlapTakenBranchDummy)
            {
                var fallThrough = (ushort)(_opcodeAddress + 2);
                _pc = fallThrough;
                _visiblePC = fallThrough;
                _skipBranchPageCrossExtra = true;
            }
            else if (IsBranchOpcode(_opcode) && IsBranchTaken(_opcode)
                && _dummyTakenBneAfterStolenInc)
            {
                // BA-skip INC: FETCH of BNE is VICE dummy INC_PC (2093352).
                // Steal trail at next-PC is large so do not require
                // cleanBranchFetch. Unstalled INC (2142574) never sets this.
                var fallThrough = (ushort)(_opcodeAddress + 2);
                _pc = fallThrough;
                _visiblePC = fallThrough;
                _dummyTakenBneAfterStolenInc = false;
                _cycle++;
                fullLengthTakenBranch = true;
            }
            else if (IsBranchOpcode(_opcode) && IsBranchTaken(_opcode) && cleanBranchFetch)
            {
                _cycle++;
                fullLengthTakenBranch = true;
            }

            if (IsBranchOpcode(_opcode) && IsBranchTaken(_opcode) && _afterLdaAbsHold
                && !fullLengthTakenBranch)
            {
                _cycle++;
                fullLengthTakenBranch = true;
            }
            else if (!IsBranchOpcode(_opcode))
            {
                _afterLdaAbsHold = false;
            }

            // Wolf64 2044616: native dummy CLK of BNE after LDA $D012 still
            // exports opcode PC $FF61, not fall-through $FF63. Keep opcode PC
            // on extra dummy CLKs; JUMP at cycle 0.
            _holdTakenBranchOpcodePc = IsBranchOpcode(_opcode)
                && IsBranchTaken(_opcode)
                && _afterLdaAbsHold
                && !_skipAbsLoadLastClkHold
                && !_ldaSkippedLastClkHold;

            DebugFullLengthTakenBranch = fullLengthTakenBranch;

            _stagedMemoryReadCompleted = false;
            _stagedLoadRegisterVisibleAtReadCheckpoint = false;
            _delayNextFetch = false;
            _stagedNzUpdate = false;
            _stagedNzValue = 0;
            _stagedCarryUpdate = false;
            _stagedCarryValue = false;
            _zpRmwModifyCommitted = false;
            if (_holdZpIncDecAfterStaAbsY && !IsZeroPageIncrementDecrementOpcode(_opcode))
                _holdZpIncDecAfterStaAbsY = false;
            _pendingPlaCompletion = false;
            _pendingPlpStatus = false;
            // BIT after RTS delayNextFetch: soft-apply one cycle earlier so the
            // host sample count matches VICE (c=522467 held late with cycle-0
            // soft). After branch / other trail, soft at cycle 0 (c=522392).
            _bitSoftDeferEarly = (_opcode is 0x2C or 0x24) && _fetchAfterRtsDelay;
            _fetchAfterRtsDelay = false;
            var deferIndirectYLoadAfterBranchRmw = _deferNextIndirectYLoadAfterBranchRmw && IsIndirectYLoadOpcode(_opcode);
            _deferNextIndirectYLoadAfterBranchRmw = false;
            // Consume full-length-branch credit for THIS instruction (e.g. JSR after BNE).
            // Also JSR after deferred zp RMW (not-taken BNE -> INC -> JSR at 20761).
            var jsrAfterDeferredRmw = _opcode == 0x20 && _nextJsrNonOverlapped;
            if (jsrAfterDeferredRmw)
                _nextJsrNonOverlapped = false;
            _afterFullLengthTakenBranch = (fetchingBranchTarget && _fullLengthTakenBranchCompleted)
                || jsrAfterDeferredRmw;
            if (_afterFullLengthTakenBranch)
            {
                _nonOverlappedFetchPhase = true;
                _nonOverlappedRegion = true;
            }

            if (_opcode == 0x99 && _afterFullLengthTakenBranch)
                _holdZpIncDecAfterStaAbsY = true;

            var armAfterBranchLag = fetchingBranchTarget && !_afterFullLengthTakenBranch;
            _afterShortTakenBranchLag = armAfterBranchLag;
            _fullLengthTakenBranchCompleted = fullLengthTakenBranch;

            _deferImpliedRegisterCompletionAfterBranch = armAfterBranchLag && IsImpliedRegisterOrFlagOpcode(_opcode);
            // CMP zp family after taken-branch lag only (not after JSR trail>=2;
            // that regressed CPY at c=517861 where VICE already commits).
            // Short/full taken-branch targets soft-defer flags/PC (c=532264).
            // Also unstaged abs/abs,X/abs,Y compares (c=554366 CMP abs,X $DD
            // after taken BPL: nPC=opcode nP=pre-op mPC/mP already committed).
            _softDeferZpCompare = (IsZeroPageCompareOpcode(_opcode)
                    || IsUnstagedAbsoluteCompareOpcode(_opcode))
                && (armAfterBranchLag || _afterFullLengthTakenBranch);
            // VICE CP(GET_ABS): GET_ABS CLK_INC still has opcode PC / pre-op P
            // when CMP FETCH was not overlapped (Wolf64 2060513 nPC=$F6BF nP=$A4).
            // After LDA abs whose last CLK already exported next-PC (trail>=1),
            // flags+INC_PC are visible on the last CMP CLK (Wolf64 2142604
            // nPC=$F6C2 nP=$27).
            if (_opcode == 0xCD
                && (_nonOverlappedFetchPhase || _nonOverlappedRegion)
                && priorTrailingAtNextPc == 0)
                _softDeferCompareCommit = true;
            // JMP abs soft JUMP when first FETCH is clean non-overlapped (after
            // zp shift RMW c=539731, or soft-implied CLC c=539753). Not when
            // trail alone after STY (c=502749 nPC=target already).
            // After fused non-load imm trail==1 VICE already shows JUMP
            // (c=541205 nPC=$DC31 mPC=$DC98 when soft-held opcode).
            // After JSR trail>=2 VICE also shows JUMP (c=559254 nPC=$F734
            // mPC=$FFEA when soft-held after overlapped JSR).
            // STA zp whose write CLK already exported next-PC (trail>=2): VICE
            // JMP last CLK is the target (Wolf64 2044582 $EA24, 2122101 $B8D2).
            // STA zp that held opcode until its last CLK (trail<=1): VICE JMP
            // last FETCH CLK still opcode (Wolf64 2122664 nPC=$B91A).
            var staZpHeldWrite = _previousOpcode == 0x85 && priorTrailingAtNextPc <= 1;
            _softDeferJmpAbs = _opcode == 0x4C
                && (IsZeroPageShiftRmwOpcode(_previousOpcode)
                    || _nonOverlappedFetchPhase || _nonOverlappedRegion
                    || staZpHeldWrite
                    // RTS LOAD is the last clock; its increment/JUMP are
                    // unclocked. The target JMP therefore begins a distinct
                    // FETCH sequence whose final operand clock still shows the
                    // JMP opcode before JMP's own unclocked target update.
                    || _previousOpcode == 0x60)
                && !(IsTwoByteImmediateOpcode(_previousOpcode)
                    && !IsImmediateLoadOpcode(_previousOpcode)
                    && priorTrailingAtNextPc == 1)
                && !(_previousOpcode == 0x20 && priorTrailingAtNextPc >= 2)
                // VICE JMP() is JUMP with no extra CLK; after STA abs or LDA#
                // the last JMP CLK already exports the target
                // (Wolf64 2060363 nPC=$FDDD; 2060380 nPC=$FDF3).
                && !(IsStoreOpcode(_previousOpcode) && !staZpHeldWrite)
                && !IsImmediateLoadOpcode(_previousOpcode);
            // Short taken branch -> NOP chain: hold opcode PC on each NOP's final
            // CLK (VICE INC_PC after last FETCH). Sticks across consecutive NOPs
            // so the second padding NOP after the branch also matches (c=518741).
            if (_opcode == 0xEA && (armAfterBranchLag || _nopChainHoldAfterBranch))
            {
                _nopHoldOpcodePcOnFinal = true;
                _nopChainHoldAfterBranch = true;
                _softDeferAfterNopChain = false;
            }
            else
            {
                // Leaving the chain: next insn needs soft-defer of its commit so
                // host still samples opcode PC on its final FETCH CLK (c=518751).
                _softDeferAfterNopChain = _nopChainHoldAfterBranch && _opcode != 0xEA;
                _nopHoldOpcodePcOnFinal = false;
                _nopChainHoldAfterBranch = false;
            }
            _deferJsrPushAfterBranch = armAfterBranchLag && _opcode == 0x20;
            var fetchingIndexedLoadControlTarget = armAfterBranchLag;
            // Full-length taken BNE then LDA abs,X: VICE GET_ABS_X data-read
            // still has pre-op A (Wolf64 4138251 nA=$43 mA=$2C). Same defer
            // as (zp),Y after full-length. Short lag already used armAfterBranchLag.
            _deferAbsoluteXLoadCompletionAfterBranch =
                (fetchingIndexedLoadControlTarget || _afterFullLengthTakenBranch)
                && IsAbsoluteXLoadOpcode(_opcode);
            _deferAbsoluteYLoadCompletionAfterBranch = fetchingIndexedLoadControlTarget && IsAbsoluteYLoadOpcode(_opcode);
            // Delay A commit for (zp),Y after a full-length taken branch as well:
            // VICE still exports the pre-load A on the data-read CLK when the
            // first FETCH of this load was not overlapped (same host-visible
            // shape as the after-branch lag path without the extra cycle budget).
            _deferIndirectYLoadCompletionAfterBranch =
                ((armAfterBranchLag || _afterFullLengthTakenBranch) && IsIndirectYLoadOpcode(_opcode))
                || deferIndirectYLoadAfterBranchRmw;
            // Latch before the implied-fuse flag is cleared on this FETCH.
            // VICE GET_IND_Y INC_PC has no extra CLK (Wolf64 2406426 nPC=$EAB9).
            _fuseIndyLoadLastClkPc = IsIndirectYLoadOpcode(_opcode)
                && !_deferIndirectYLoadCompletionAfterBranch
                && !IsStoreOpcode(_previousOpcode)
                && (priorTrailingAtNextPc >= 1
                    || _fuseImpliedAfterIndyLoad
                    || IsImpliedRegisterOrFlagOpcode(_previousOpcode)
                    || _previousOpcode is 0x48 or 0x08);
            _deferZeroPageRmwPcAdvanceAfterBranch = armAfterBranchLag && IsZeroPageIncrementDecrementOpcode(_opcode);
            // Taken branch then zp INC/DEC then JSR: VICE still STACK_PEEKs
            // at JSR DebugCycle 3 (Wolf64 4132742). Sticky through TYA/CLC
            // to a later JSR over-peeked (2003258 nS=$FC). INC after ORA#
            // then JSR PUSHes (4132037 nS=$F2).
            if (_opcode != 0x20 && !IsZeroPageIncrementDecrementOpcode(_opcode))
                _nextJsrNonOverlapped = false;
            if (IsZeroPageIncrementDecrementOpcode(_opcode)
                && (armAfterBranchLag || fetchingBranchTarget || _afterFullLengthTakenBranch))
                _nextJsrNonOverlapped = true;
            _deferIndexedStorePcAdvanceAfterBranch = armAfterBranchLag && IsIndexedAbsoluteStoreOpcode(_opcode);
            _deferZeroPageIndexedStorePcAdvanceAfterBranch = armAfterBranchLag && IsZeroPageIndexedStoreOpcode(_opcode);
            // A short taken-branch target or JSR callee begins with a distinct
            // first FETCH checkpoint. Preserve that clock for opcodes whose
            // staged body otherwise assumes it overlapped the predecessor.
            if ((armAfterBranchLag || fetchingNonOverlappedCallTarget)
                && IsAfterBranchBudgetExtendedOpcode(_opcode))
            {
                _cycle++;
            }

            // VICE FETCH_OPCODE is two CLK_INC. LDA zp / STA zp first host
            // FETCH tick is those two clocks. Do not double BEQ FETCH
            // (2175366 elapsed too early) or STA abs FETCH (2224630).
            // Wolf64 2865090 wait-loop irq_delay 9 vs 13: these two extras
            // are the missing CLK_INC.
            if (_opcode is 0xA5 or 0x85)
                ViceClocksThisTick = 2;

            // Stage fall-through at cycle 1 when:
            //  - budget 4+ (full-length + after-branch lag): c=540201
            //  - full-length with priorTrailing>=3 (c=540231)
            //  - full-length trail=2 after store (c=540337 BCC after STA zp)
            //  - full-length trail=1 after zp/ALU compare (c=541194 BNE after
            //    CPX zp nPC=fall-through mPC=opcode at dbgCyc=1)
            //  - previous implied was fused (Wolf64 2142610 BMI after TAX:
            //    host last CLK already next-PC via _pc, but _visiblePC stayed
            //    at the TAX opcode so trail==0). VICE BRANCH dummy CLK_INC
            //    exports INC_PC(2). Soft implied does not set this flag
            //    (Wolf64 2060519 nPC still opcode). Do not use _cycle>=3:
            //    that dummy-exports every full-length taken branch.
            // Not: full-length trail=2 after INC (c=5005 keeps opcode at cyc 1)
            // Not: trail=0+nonOvl (c=5034) or lag alone (c=4997)
            _takenBranchStagedFallthrough = IsBranchOpcode(_opcode)
                && IsBranchTaken(_opcode)
                && fullLengthTakenBranch
                && !_holdTakenBranchOpcodePc
                && (_cycle >= 4
                    || priorTrailingAtNextPc >= 3
                    || (priorTrailingAtNextPc == 2 && IsStoreOpcode(_previousOpcode))
                    || (priorTrailingAtNextPc == 1
                        && (IsZeroPageCompareOpcode(_previousOpcode)
                            || IsAbsoluteAluOpcode(_previousOpcode)
                            || IsImmediateLoadOpcode(_previousOpcode)
                            || _previousOpcode == 0xA5
                            || IsTwoByteImmediateOpcode(_previousOpcode)
                            || IsImpliedRegisterOrFlagOpcode(_previousOpcode)
                            || _previousOpcode == 0xB9))
                    || (_fuseImpliedAfterIndyLoad
                        && IsImpliedRegisterOrFlagOpcode(_previousOpcode)));
            if (IsBranchOpcode(_opcode) && IsBranchTaken(_opcode)
                && (_skipAbsLoadLastClkHold || _ldaSkippedLastClkHold))
                _takenBranchStagedFallthrough = true;
            if (IsBranchOpcode(_opcode))
            {
                _afterLdaAbsHold = false;
                _skipAbsLoadLastClkHold = false;
                _ldaSkippedLastClkHold = false;
            }

    
            _indexedLoadPageCrossDelayConsumed = false;
            _rtsPrefetchedByNotTakenBranch = false;
            _stagedReturnAddress = 0;
            _effectiveAddress = 0;
            _indyPageCrossedThisInsn = false;
            _fetched = 0;

        }

        _cycle--;

        if (_cycle == 0
            && IsZeroPageIncrementDecrementOpcode(_opcode)
            && _visiblePC == _opcodeAddress)
        {
            // Last INC/DEC CLK: VICE still exports opcode PC (Wolf64 2093351
            // nPC=$F69F). Do not ExecuteOpcode (Fetch/PC++ -> opcode+1).
            _pc = _visiblePC;
            _suppressBootstrapBoundary = true;
            return;
        }

        if (_cycle == 0 && _opcode == 0xEC)
        {
            // VICE CP(GET_ABS): last CLK is LOAD+CLK_INC with opcode PC /
            // pre-op P; flags+INC_PC run after that sample (Wolf64 2407022
            // nPC=$EB37 nP=$26 mPC=$EB3A mP=$A4). Unstaged ExecuteOpcode
            // bundled GET_ABS with INC_PC. CMP abs 0xCD is staged so trail 0
            // vs trail>=1 selects apply vs fuse; 0xEC last CLK is always GET_ABS.
            var cpxValue = Read(ReadAbsoluteOperand());
            _stagedCarryUpdate = true;
            _stagedCarryValue = X >= cpxValue;
            _stagedNzUpdate = true;
            _stagedNzValue = (byte)(X - cpxValue);
            _pendingSoftCompareCommit = true;
            _pc = (ushort)(_instructionPC + 3);
            _visiblePC = _opcodeAddress;
            _suppressBootstrapBoundary = true;
            _nonOverlappedFetchPhase = true;
            return;
        }

        if (_cycle == 0
            && _opcode == 0xC5
            && _visiblePC == (ushort)(_instructionPC + 2))
        {
            // Fused CMP zp already committed on DebugCycle 1 (Wolf64 2129140).
            // Do not ExecuteOpcode on last CLK: that added one more PC++
            // (Wolf64 2129141 nPC=$E6BF mPC=$E6C0).
            _pc = _visiblePC;
            _suppressBootstrapBoundary = true;
            return;
        }

        if (_holdAbsLoadOpcodeLastClk && _cycle == 0 && _opcode == 0xAD)
        {
            _holdAbsLoadOpcodeLastClk = false;
            _visiblePC = _opcodeAddress;
            _pc = (ushort)(_opcodeAddress + 3);
            _nonOverlappedFetchPhase = true;
            _afterLdaAbsHold = true;
            _suppressBootstrapBoundary = true;
            return;
        }

        if (_holdAbsLoadOpcodeLastClk && _opcode != 0xAD)
            _holdAbsLoadOpcodeLastClk = false;

        if (TryExecuteCycleStagedOpcode())
        {
            return;
        }

        if (TryDeferImpliedRegisterCompletionAfterBranch())
        {
            return;
        }

        if (_cycle == 0)
        {
            // VICE always orders FETCH_OPCODE's two CLK_INC operations before
            // LD(GET_IMM), but the first fetch clock can share the host checkpoint
            // already exported by the preceding instruction. Classify that overlap
            // from the immediately preceding opcode, not sticky branch-region state:
            // control transfers JUMP after their final clock, stores and RMW end
            // on a write clock, while PHA -> LDA # advances through the body here.
            var skipSoftImmThisInsn = _applySkipSoftImmThisInsn;
            _applySkipSoftImmThisInsn = false;
            var previousImmediateLoadCompletedWithoutDistinctFetch =
                _immediateLoadCompletedWithoutDistinctFetch;
            _immediateLoadCompletedWithoutDistinctFetch = false;
            var previousControlTransferHasDistinctSecondFetch =
                EndsWithUnclockedControlTransfer(_previousOpcode)
                && (!IsBranchOpcode(_previousOpcode)
                    || _afterFullLengthTakenBranch
                    || _afterShortTakenBranchLag
                    // A fused implied chain has already consumed the branch's
                    // fall-through FETCH. VICE therefore executes immediate LD
                    // after that checkpoint instead of exposing a distinct one.
                    || !_fuseImpliedAfterIndyLoad);
            var immediateLoadHasDistinctSecondFetch =
                _immediateLoadFollowsSoftDeferredBody
                || previousControlTransferHasDistinctSecondFetch
                || (IsImmediateLoadOpcode(_previousOpcode)
                    && !previousImmediateLoadCompletedWithoutDistinctFetch)
                || (IsStoreOpcode(_previousOpcode)
                    && DebugPriorTrailingAtNextPc == 1)
                || IsZeroPageIncrementDecrementOpcode(_previousOpcode);
            if (IsImmediateLoadOpcode(_opcode)
                && immediateLoadHasDistinctSecondFetch
                && !skipSoftImmThisInsn)
            {
                _fuseNonLoadImmAfterLoadBranch = false;
                _softDeferredImmediateLoad = true;
                _visiblePC = _opcodeAddress;
                _suppressBootstrapBoundary = true;
                return;
            }

            if (IsImmediateLoadOpcode(_opcode))
                _immediateLoadCompletedWithoutDistinctFetch = true;

            // Non-load immediates retain their existing overlap classification.
            // Their ALU/compare bodies also have no clock after GET_IMM. Record
            // when a fused implied predecessor already supplied that FETCH phase,
            // even in the ordinary overlapped region where no soft defer is needed.
            if (IsTwoByteImmediateOpcode(_opcode)
                && !IsImmediateLoadOpcode(_opcode)
                && IsImpliedRegisterOrFlagOpcode(_previousOpcode)
                && _fuseImpliedAfterIndyLoad)
            {
                _fusedNonLoadImmediateConsumedFetch = true;
            }

            // The managed host checkpoint can already be aligned past
            // FETCH_OPCODE when the preceding instruction exported an overlapping clock.
            if ((_nonOverlappedFetchPhase || _nonOverlappedRegion)
                && IsTwoByteImmediateOpcode(_opcode)
                && !IsImmediateLoadOpcode(_opcode))
            {
                var fuseNonLoadImmAfterBranch =
                    _fuseNonLoadImmAfterLoadBranch
                    && IsBranchOpcode(_previousOpcode)
                    && DebugPriorTrailingAtNextPc == 1;
                var fuseNonLoadImmAfterLoad =
                    IsLoadOpcode(_previousOpcode)
                    && DebugPriorTrailingAtNextPc == 1
                    && (_nonOverlappedFetchPhase || _nonOverlappedRegion);
                var fuseNonLoadImmAfterAluImm =
                    IsTwoByteImmediateOpcode(_previousOpcode)
                    && !IsImmediateLoadOpcode(_previousOpcode);
                var fuseNonLoadImmAfterFusedImplied =
                    IsImpliedRegisterOrFlagOpcode(_previousOpcode)
                    && (_fuseImpliedAfterIndyLoad
                        || DebugPriorTrailingAtNextPc >= 1);
                if (skipSoftImmThisInsn
                    || fuseNonLoadImmAfterBranch
                    || fuseNonLoadImmAfterLoad
                    || fuseNonLoadImmAfterAluImm
                    || fuseNonLoadImmAfterFusedImplied)
                {
                    if (fuseNonLoadImmAfterBranch)
                        _fuseNonLoadImmAfterLoadBranch = false;
                    // Fall through to ExecuteOpcode for an already-aligned body.
                }
                else
                {
                    _fuseNonLoadImmAfterLoadBranch = false;
                    _softDeferredImpliedOp = true;
                    _softDeferredImpliedOpcode = _opcode;
                    _softDeferredImpliedInstructionPc = _opcodeAddress;
                    _visiblePC = _opcodeAddress;
                    _suppressBootstrapBoundary = true;
                    _nonOverlappedFetchPhase = true;
                    return;
                }
            }

            // VICE abs/zp ALU (ORA/AND/EOR/ADC/SBC): GET_ABS/GET_ZERO CLK exports
            // pre-op A/PC; body + INC_PC after that sample (c=519285 nA=$03
            // mA=$1F nPC=opcode mPC=next). Soft-defer when non-overlapped.
            // After fused LDA (zp),Y trail==1 VICE fuses the ALU body on the
            // final CLK (c=541187 EOR zp nA=$1E mA=$3E when soft-deferred).
            // Keep the post-indy fuse chain so following not-taken BMI does not
            // hold opcode PC (c=541189 nPC=$DC6D mPC=$DC6B).
            var sbcAbsYPageCross = false;
            if (_opcode == 0xF9)
            {
                var sbcAbsYBase = ReadAbsoluteOperand();
                var sbcAbsYEff = (ushort)(sbcAbsYBase + Y);
                sbcAbsYPageCross = (sbcAbsYBase & 0xFF00) != (sbcAbsYEff & 0xFF00);
            }

            if (((_nonOverlappedFetchPhase || _nonOverlappedRegion)
                    && IsAbsoluteAluOpcode(_opcode))
                || sbcAbsYPageCross)
            {
                // After fused LDA (zp),Y trail==1, or after a not-taken
                // branch whose last CLK already exported next-PC, VICE
                // ORA/AND/... (GET_ABS)+INC_PC has no extra CLK
                // (Wolf64 3029797 nPC=$EAC4 nP=$24). SBC abs,Y GET_ABS_Y
                // last CLK still opcode PC when Y page-crosses (Wolf64
                // 4138175 Y=$66). Y=0 / Y=$03 no-cross still fuses
                // (4134601 / 4134660).
                if (!sbcAbsYPageCross
                    && ((_previousOpcode == 0xB1 && DebugPriorTrailingAtNextPc == 1)
                        || (IsBranchOpcode(_previousOpcode)
                            && DebugPriorTrailingAtNextPc >= 1
                            && !IsBranchTaken(_previousOpcode))
                        || PriorInstructionConsumesFollowingFetchPhase(
                            _previousOpcode,
                            DebugPriorTrailingAtNextPc)
                        || _fuseImpliedAfterIndyLoad))
                {
                    _fuseImpliedAfterIndyLoad = true;
                    // fall through to ExecuteOpcode (fused)
                }
                else
                {
                    _softDeferredImpliedOp = true;
                    _softDeferredImpliedOpcode = _opcode;
                    _softDeferredImpliedInstructionPc = _opcodeAddress;
                    _visiblePC = _opcodeAddress;
                    _suppressBootstrapBoundary = true;
                    _nonOverlappedFetchPhase = true;
                    return;
                }
            }

            // VICE implied ops (CLC/TAX/etc.): flag/register write and INC_PC run
            // after the final CLK is exported. Soft-defer the write when first
            // FETCH is not overlapped so host samples the pre-op P/regs.
            // After fused LDA (zp),Y trail==1, VICE fuses INY then TAX (c=541175,
            // c=541177). Sticky chain until a non-implied is fetched.
            // After fused JMP abs trail==1, ROL A also fuses (c=541207 nA=$BF
            // mA=$DF when soft-deferred).
            // After full-length taken branch trail>=2, SEC fuses (c=559266
            // nP=$25 mP=$24 when soft-deferred after BNE).
            // After PHA/PHP trail==1 (IRQ handler TXA at c=577692 nPC=next
            // mPC=opcode when soft in nonOvl): VICE INC_PC is on the final CLK.
            // After overlapped JSR JUMP (trail>=1) TAY fuses Y+INC_PC
            // (Wolf64 2571298 nY=$20 nPC=$EA14). Sticky one instruction so
            // following LDA # fuses (2571300). Do not treat STA as a fuse
            // entry (that over-fused INX after STA abs,X at 2407031).
            var fuseTayAfterJsr = _opcode == 0xA8
                && _previousOpcode == 0x20
                && DebugPriorTrailingAtNextPc >= 1;
            // VICE INY last FETCH CLK still has pre-op Y when INC zp cycle 2
            // held opcode PC (Wolf64 4147669 nY=$CD nPC=$A5F9). Sticky
            // fuseImplied from an earlier CLC/INY must not ExecuteOpcode on
            // that CLK. Soft-defer via the else-if below; next FETCH commits
            // with no extra host tick (4138197 extra tick lagged LDA abs,Y).
            var holdInyAfterHeldInc = _inySoftAfterHeldInc
                && _opcode is 0xC8 or 0x88
                && IsZeroPageIncrementDecrementOpcode(_previousOpcode);
            if (IsImpliedRegisterOrFlagOpcode(_opcode)
                && !holdInyAfterHeldInc
                && ((_previousOpcode == 0xB1 && DebugPriorTrailingAtNextPc == 1)
                    || (_previousOpcode == 0x4C && DebugPriorTrailingAtNextPc == 1)
                    || ((_previousOpcode is 0x48 or 0x08 or 0x68 or 0x28)
                        && DebugPriorTrailingAtNextPc >= 1)
                    || (_afterFullLengthTakenBranch
                        && DebugPriorTrailingAtNextPc >= 2)
                    || (IsBranchOpcode(_previousOpcode)
                        && !_afterFullLengthTakenBranch
                        && !_afterShortTakenBranchLag
                        && DebugPriorTrailingAtNextPc >= 1)
                    || (IsTwoByteImmediateOpcode(_previousOpcode)
                        && !IsImmediateLoadOpcode(_previousOpcode)
                        && DebugPriorTrailingAtNextPc >= 1)
                    // VICE ST with SET_ZERO/SET_ABS has only the final write
                    // CLK after INC_PC. Two exported next-PC checkpoints mean
                    // the following implied body shares its second FETCH clock.
                    // Indexed stores use an additional dummy CLK and retain the
                    // soft path (Wolf64 2407031).
                    || PriorInstructionConsumesFollowingFetchPhase(
                        _previousOpcode,
                        DebugPriorTrailingAtNextPc)
                    || _fuseImpliedAfterIndyLoad
                    || fuseTayAfterJsr))
            {
                _fuseImpliedAfterIndyLoad = true;
                // fall through to ExecuteOpcode (fused)
            }
            else if (IsImpliedRegisterOrFlagOpcode(_opcode)
                && ((_nonOverlappedFetchPhase || _nonOverlappedRegion)
                    // ST's INC_PC is unclocked and its write CLK is the last
                    // exported checkpoint. Until the store has enough trailing
                    // checkpoints to consume the following FETCH phase, the
                    // next implied body remains after that FETCH CLK.
                    || (IsStoreOpcode(_previousOpcode)
                        && !PriorInstructionConsumesFollowingFetchPhase(
                            _previousOpcode,
                            DebugPriorTrailingAtNextPc))
                    || (IsZeroPageShiftRmwOpcode(_previousOpcode)
                        && DebugPriorTrailingAtNextPc == 2)
                    // After full-length taken branch then zp INC, VICE INY last
                    // FETCH CLK still has pre-op Y (Wolf64 4147669 nY=$CD
                    // nPC=$A5F9). Short-lag INC then INY already fuses
                    // (4134618 nY=$01). Soft-defer: next FETCH commits INY
                    // with no extra host tick (4138197 extra tick lagged LDA).
                    || (_opcode is 0xC8 or 0x88
                        && IsZeroPageIncrementDecrementOpcode(_previousOpcode)
                        && _inySoftAfterHeldInc)
                    || (_opcode is 0xC8 or 0x88
                        && _softDeferImpliedAfterHeldIndYStore)))
            {
                // Soft after nonOvl or after zp shift trail==2 (c=543316 ROR A
                // nPC=opcode). trail==3 fuses (c=540980 nPC advanced mPC=opcode).
                _fuseImpliedAfterIndyLoad = false;
                _inySoftAfterHeldInc = false;
                _softDeferImpliedAfterHeldIndYStore = false;
                _softDeferredImpliedOp = true;
                _softDeferredImpliedOpcode = _opcode;
                _softDeferredImpliedInstructionPc = _opcodeAddress;
                _visiblePC = _opcodeAddress;
                _suppressBootstrapBoundary = true;
                _nonOverlappedFetchPhase = true;
                return;
            }
            else if (!IsBranchOpcode(_opcode) && !IsAbsoluteAluOpcode(_opcode))
            {
                // End post-indy fuse chain. Keep flag through fused ALU and the
                // following branch so not-taken hold skips (c=541189 BMI after EOR).
                // Do not keep it through CPY#/CMP#: that over-fused BNE at
                // Wolf64 2093382.
                _fuseImpliedAfterIndyLoad = false;
            }

            // NOP after short taken branch: VICE still exports opcode PC on the
            // final FETCH CLK (INC_PC is after that sample). Suppress boundary
            // so GetState returns visible opcode PC, not the post-fetch next PC.
            if (_nopHoldOpcodePcOnFinal)
            {
                _nopHoldOpcodePcOnFinal = false;
                _visiblePC = _opcodeAddress;
                _suppressBootstrapBoundary = true;
                return;
            }

            // After post-branch NOP chain: soft-defer 2-byte immediate body
            // (CMP#/AND#/etc.) so final FETCH sample keeps opcode PC; commit at
            // the next instruction's first tick (VICE INC_PC after last FETCH).
            if (_softDeferAfterNopChain && IsTwoByteImmediateOpcode(_opcode))
            {
                _softDeferredImpliedOp = true;
                _softDeferredImpliedOpcode = _opcode;
                _softDeferredImpliedInstructionPc = _opcodeAddress;
                _softDeferAfterNopChain = false;
                _visiblePC = _opcodeAddress;
                _suppressBootstrapBoundary = true;
                return;
            }

            // BIT abs/zp soft-defer when clean first FETCH, after branch, or
            // priorTrailing>=2. After LDA# trail=1 VICE fuses BIT (c=540355);
            // after AND#/ALU imm trail>=1 VICE also fuses (Wolf64 2423374
            // nPC=$EAF5 nP=$27 mPC=$EAF2 mP=$25). After taken BNE trail=1
            // still softs (c=522392 nPC=opcode).
            // _bitSoftDeferEarly handles RTS-delay cycle-1 soft.
            var fuseBitAfterImm = IsTwoByteImmediateOpcode(_previousOpcode)
                && DebugPriorTrailingAtNextPc >= 1;
            // After zp ASL/ROL/LSR/ROR whose last CLK already exported
            // next-PC, VICE BIT(GET_ZERO)+INC_PC has no extra CLK
            // (Wolf64 4131976 nPC=$E646).
            var fuseBitAfterZpShiftRmw = IsZeroPageShiftRmwOpcode(_previousOpcode)
                && DebugPriorTrailingAtNextPc >= 1;
            // After not-taken BEQ whose last CLK already exported
            // fall-through, VICE BIT last CLK INC_PCs (Wolf64 4134557
            // nPC=$A59A). Taken BNE trail=1 still softs (c=522392).
            var fuseBitAfterNotTakenBranch = IsBranchOpcode(_previousOpcode)
                && DebugPriorTrailingAtNextPc >= 1
                && !(_afterFullLengthTakenBranch || _afterShortTakenBranchLag);
            if ((_opcode is 0x2C or 0x24) && !_bitSoftDeferEarly && !fuseBitAfterImm
                && !fuseBitAfterZpShiftRmw
                && !fuseBitAfterNotTakenBranch
                && (_nonOverlappedFetchPhase || _nonOverlappedRegion
                    || DebugPriorTrailingAtNextPc >= 2
                    || IsBranchOpcode(_previousOpcode)))
            {
                SoftDeferBitBody();
                return;
            }

            // CMP zp/zx after taken-branch clean FETCH: soft-defer Compare body.
            if (_softDeferZpCompare)
            {
                _softDeferZpCompare = false;
                _softDeferredImpliedOp = true;
                _softDeferredImpliedOpcode = _opcode;
                _softDeferredImpliedInstructionPc = _opcodeAddress;
                _visiblePC = _opcodeAddress;
                _suppressBootstrapBoundary = true;
                return;
            }

            // zp shift RMW already committed mid-instruction. Suppress boundary
            // so SystemClock does not sample IRQ one CLK early vs VICE after the
            // RMW final sample (c=540734 nS=$F5 mS=$F4 on first IRQ push).
            if (_zpRmwModifyCommitted && IsZeroPageShiftRmwOpcode(_opcode))
            {
                _zpRmwModifyCommitted = false;
                _suppressBootstrapBoundary = true;
                return;
            }

            // JMP abs: soft-hold opcode PC; JUMP applies on the next fetch tick.
            if (_softDeferJmpAbs && _opcode == 0x4C)
            {
                _softDeferJmpAbs = false;
                _pendingJmpTarget = ReadAbsoluteOperand();
                _pc = _opcodeAddress;
                _visiblePC = _opcodeAddress;
                _suppressBootstrapBoundary = true;
                return;
            }

            _softDeferAfterNopChain = false;
            if (IsZeroPageIncrementDecrementOpcode(_opcode))
            {
                // VICE last INC CLK still exports opcode PC; INC_PC is visible
                // on the next FETCH_OPCODE. ExecuteOpcode uses Fetch()/PC++
                // which would clobber PC to opcode+1 and RMW a second time.
                _pc = _visiblePC;
                _suppressBootstrapBoundary = true;
                return;
            }

            ExecuteOpcode(_opcode);
        }
    }

    /// <summary>
    /// VICE BIT+GET_*: data-read host sample keeps opcode PC / pre-BIT P;
    /// N/V/Z + INC_PC apply with no extra CLK (soft on next tick from latched GET).
    /// </summary>
    private void SoftDeferBitBody()
    {
        // Single GET (VICE GET_ABS / GET_ZERO). Soft-apply uses this value only.
        byte value = _opcode == 0x2C
            ? Read(ReadAbsoluteOperand())
            : Read(ReadZeroPageOperand());
        _softDeferredBitLatched = true;
        _softDeferredBitValue = value;
        _softDeferredImpliedOp = true;
        _softDeferredImpliedOpcode = _opcode;
        _softDeferredImpliedInstructionPc = _opcodeAddress;
        _visiblePC = _opcodeAddress;
        _pc = (ushort)(_opcodeAddress + 1);
        _cycle = 0;
        _bitSoftDeferEarly = false;
        _suppressBootstrapBoundary = true;
    }

    private bool IsNonOverlappedJsr()
    {
        return (_jsrFollowsSoftDeferredImmediateLoad
                || _afterFullLengthTakenBranch
                || _nonOverlappedRegion
                || _nonOverlappedFetchPhase
                // RTS resumes with an unclocked JUMP before the return-target
                // FETCH, so a following JSR retains its explicit STACK_PEEK.
                || _previousOpcode == 0x60)
            && !(DebugPriorTrailingAtNextPc == 1
                && (IsTwoByteImmediateOpcode(_previousOpcode)
                    || _previousOpcode == 0x6C))
            // VICE absolute unindexed stores consumed both operand FETCH
            // clocks before SET_ABS, so a following JSR in a retained
            // non-overlapped region still exposes its explicit STACK_PEEK.
            // SET_ZERO and the indexed store forms retain the observed
            // next-PC overlap represented by the trailing-clock exception.
            && !(IsStoreOpcode(_previousOpcode)
                && !IsAbsoluteUnindexedStoreOpcode(_previousOpcode)
                && DebugPriorTrailingAtNextPc is 1 or 2)
            // A not-taken branch already exported its fall-through PC. Its
            // following JSR is on INC_PC plus PUSH high at DebugCycle 3, so
            // mandatory BA cannot suppress that write (Wolf64 sample 14997).
            && !(IsBranchOpcode(_previousOpcode)
                && !IsBranchTaken(_previousOpcode));
    }

    private bool TryExecuteCycleStagedOpcode()
    {
        if (_opcode != 0x20)
        {
            return TryExecuteCycleStagedMemoryReadOpcode();
        }

        // Clean first FETCH (full-length branch or sticky non-overlapped region
        // after not-taken hold): VICE keeps PC at JSR through STACK_PEEK before
        // INC_PC+PUSH (c=518797 nPC=$E8B5 mPC=$E8B7 nS=$F2 mS=$F1).
        // Exception: after a 2-byte imm with trail==1 (LDY# then JSR), or after
        // JMP ind trail==1, VICE is already on INC_PC+PUSH at dbgCyc=3 even
        // inside a non-overlapped region (c=541157; c=559248 nPC=$EAC1 nS=$EC
        // mPC=$EABF mS=$ED after JMP ind).
        // After STY/STA zp whose last CLK already exported next-PC (trail 1-2),
        // VICE is also on INC_PC+PUSH at dbgCyc=3 (Wolf64 2571293 nPC=$E5E6
        // nS=$F2). Keep trail>=4 STA (zp),Y STACK_PEEK (c=5048).
        var nonOverlappedJsr = IsNonOverlappedJsr();
        switch (_cycle)
        {
            case 5:
                _visiblePC = _instructionPC;
                return true;
            case 4:
                _visiblePC = _instructionPC;
                return true;
            case 3:
                if (_deferJsrPushAfterBranch)
                {
                    // Short taken-branch lag: one extra peek-shaped CLK.
                    _deferJsrPushAfterBranch = false;
                    _cycle = 4;
                    _visiblePC = _instructionPC;
                    _suppressBootstrapBoundary = true;
                    return true;
                }

                if (nonOverlappedJsr)
                {
                    // Clean first FETCH: keep PC at the opcode for the STACK_PEEK
                    // CLK as well (VICE: 2 FETCH + PEEK all export reg_pc at JSR).
                    _visiblePC = _instructionPC;
                    return true;
                }

                // Overlapped first FETCH (common after lag-shaped previous insn):
                // this CLK is already the post-peek INC_PC + PUSH high.
                _pc = (ushort)(_instructionPC + 2);
                _visiblePC = _pc;
                Push((byte)(_pc >> 8));
                return true;
            case 2:
                if (nonOverlappedJsr && _visiblePC == _instructionPC)
                {
                    // Clean path: INC_PC + PUSH high on this CLK.
                    _pc = (ushort)(_instructionPC + 2);
                    _visiblePC = _pc;
                    Push((byte)(_pc >> 8));
                    return true;
                }

                Push((byte)_pc);
                return true;
            case 1:
                if (nonOverlappedJsr)
                {
                    Push((byte)_pc);
                    return true;
                }

                return false;
            case 0:
                CompleteJsrTargetFetch();
                return true;
            default:
                return false;
        }
    }

    private void CompleteJsrTargetFetch()
    {
        var source = _instructionPC;
        var lo = Read((ushort)(_instructionPC + 1));
        // _pc is already instructionPC+2 after the push phase (VICE loads MSB here).
        var hi = Read(_pc);
        var target = (ushort)(lo | (hi << 8));
        var returnPc = (ushort)(source + 3);
        // Same non-overlapped predicate as the push-phase switch (trail==1 after
        // 2-byte imm or JMP ind is overlapped end-to-end: c=541160; c=559248).
        var nonOverlappedJsr = IsNonOverlappedJsr();
        if (nonOverlappedJsr)
        {
            // VICE JUMP runs after the final CLK_INC export, so this cycle still
            // shows the post-INC_PC address; target is live for the next FETCH.
            var exportPc = _pc;
            _pc = target;
            _instructionPC = exportPc;
            _visiblePC = exportPc;
            // Callee starts without first-FETCH overlap.
            _nonOverlappedFetchPhase = true;
        }
        else
        {
            // Overlapped path: host-visible last CLK already matches target
            // (phase coupling with previous instruction).
            PC = target;
        }

        PublishControlTransfer(source, target, returnPc, 0x20);
        _cycle = 0;
        _callTargetFetchPending = true;
        _callTargetFetchNonOverlapped = nonOverlappedJsr;
        // Keep suppress so host PC export stays on exportPc for non-overlapped
        // JSR last CLK (c=5012 nPC=$FDB7 mPC would jump to target without it).
        // Overlapped path sets PC=target on all views; suppress is harmless.
        _suppressBootstrapBoundary = true;
        // VICE 6510core.c main loop: after JSR JUMP, DO_INTERRUPT runs before
        // FETCH of the callee. Open interrupt sampling despite suppress so
        // SystemClock can arm IRQ on this tick (c=540740 nS=$F2 mS=$F3 when
        // sample was closed: native pushed, managed ran ROR at $D9B0).
        _interruptSampleDespiteSuppress = true;
    }

    private void PublishControlTransfer(ushort source, ushort target, ushort returnPc, byte opcode)
    {
        _pubSub?.Publish(CpuControlTransferEvent.Topic, new CpuControlTransferEvent(source, target, returnPc, opcode));
    }

    private bool TryExecuteCycleStagedMemoryReadOpcode()
    {
        if (TryExecuteCycleStagedBranchOpcode())
        {
            return true;
        }

        if (TryExecuteCycleStagedRtsOpcode())
        {
            return true;
        }

        if (TryExecuteCycleStagedRtiOpcode())
        {
            return true;
        }

        if (TryExecuteCycleStagedStackPullOpcode())
        {
            return true;
        }

        if (_stagedMemoryReadCompleted)
        {
            var absoluteLoadAfterRts =
                _previousOpcode == 0x60
                && _opcode is 0xAD or 0xAE or 0xAC;
            if (_deferAbsoluteXLoadCompletionAfterBranch)
            {
                A = _stagedNzValue;
                _deferAbsoluteXLoadCompletionAfterBranch = false;
                _pendingDeferredNzUpdateAfterBranch = true;
                _stagedMemoryReadCompleted = false;
                _suppressBootstrapBoundary = true;
                return true;
            }

            if (_deferAbsoluteYLoadCompletionAfterBranch)
            {
                A = _stagedNzValue;
                _deferAbsoluteYLoadCompletionAfterBranch = false;
                _pendingDeferredNzUpdateAfterBranch = true;
                _stagedMemoryReadCompleted = false;
                _suppressBootstrapBoundary = true;
                return true;
            }

            if (_deferIndirectYLoadCompletionAfterBranch)
            {
                A = _stagedNzValue;
                _deferIndirectYLoadCompletionAfterBranch = false;
                _pendingDeferredNzUpdateAfterBranch = true;
                _stagedMemoryReadCompleted = false;
                _visiblePC = _opcodeAddress;
                _suppressBootstrapBoundary = true;
                return true;
            }

            // VICE CP: flags + INC_PC after the final data-read CLK export. On the
            // non-overlapped path after STA (zp),Y, keep old flags and opcode PC
            // this tick; soft-commit at the next instruction's first FETCH.
            if (_softDeferCompareCommit && IsStagedCompareOpcode(_opcode))
            {
                _softDeferCompareCommit = false;
                _pendingSoftCompareCommit = true;
                _visiblePC = _opcodeAddress;
                _suppressBootstrapBoundary = true;
                _stagedMemoryReadCompleted = false;
                return true;
            }

            if (_stagedCarryUpdate)
            {
                if (_stagedCarryValue)
                    P |= 0x01;
                else
                    P &= 0xFE;

                _stagedCarryUpdate = false;
            }

            if (_stagedNzUpdate)
            {
                // LDA (zp),Y: VICE LOAD once on the data CLK (GET_IND_Y). Do not
                // re-read on apply: post-LOAD CLK_INC runs vic_cycle and can
                // refresh v_bus high nibble, so a second color-RAM read yields
                // a different A (c=577851 nA=$91 mA=$B1 after both had $91 on
                // the data-read sample).
                if (_opcode is 0xB1)
                    A = _stagedNzValue;
                else if (_opcode is 0xAD)
                {
                    var holdLastClk = !_skipAbsLoadLastClkHold && !_loadAEarlyAfterStagedBranch
                        && (_afterFullLengthTakenBranch
                            || _afterShortTakenBranchLag);
                    A = holdLastClk ? Read(ReadAbsoluteOperand()) : _stagedNzValue;
                }

                // Non-overlapped zp/abs/indexed loads: commit register on apply
                // tick (data-read CLK still showed the pre-load register).
                if (_nonOverlappedFetchPhase
                    || _nonOverlappedRegion
                    || absoluteLoadAfterRts)
                {
                    switch (_opcode)
                    {
                        case 0xA5:
                        case 0xB5:
                        case 0xBD:
                        case 0xB9:
                            A = _stagedNzValue;
                            break;
                        case 0xA6:
                        case 0xB6:
                        case 0xAE:
                        case 0xBE:
                            X = _stagedNzValue;
                            break;
                        case 0xA4:
                        case 0xB4:
                        case 0xAC:
                        case 0xBC:
                            Y = _stagedNzValue;
                            break;
                    }
                }

                // Non-overlapped loads: host may still sample pre-op P on the
                // data-read / apply CLK. Soft-defer NZ (reg already committed).
                // Exception: loads that already showed the register on data-read
                // must pair NZ+PC on apply (c=541173 B1 after LDY#; c=541183 B1
                // after BEQ; c=541197 LDA zp after BNE).
                var loadFusedAfterPred = (
                        (_opcode is 0xB1
                            && (_fuseIndyLoadLastClkPc
                                || ((IsImmediateLoadOpcode(_previousOpcode)
                                        || IsBranchOpcode(_previousOpcode))
                                    && DebugPriorTrailingAtNextPc == 1)))
                        || (_opcode == 0xA5
                            && _stagedLoadRegisterVisibleAtReadCheckpoint)
                        || (_opcode is 0xBD
                            && IsImpliedRegisterOrFlagOpcode(_previousOpcode)
                            && DebugPriorTrailingAtNextPc == 0)
                        || (_opcode is 0xA4 or 0xA6 or 0xAE
                            && DebugPriorTrailingAtNextPc >= 1
                            && !(_afterFullLengthTakenBranch || _afterShortTakenBranchLag))
                        || (_opcode is 0xAD && !absoluteLoadAfterRts)
                        || (_opcode is 0xB9 && _fuseAbsYAfterStolenSamePageBranch)
                        || (_opcode is 0xB9
                            && _previousOpcode == 0x10
                            && DebugPriorTrailingAtNextPc >= 1));
                var softDeferLoadCompletion =
                    ((_nonOverlappedFetchPhase || _nonOverlappedRegion)
                        && !loadFusedAfterPred)
                    || absoluteLoadAfterRts;
                if (softDeferLoadCompletion
                    && _opcode is 0xB1 or 0xA5 or 0xA6 or 0xA4 or 0xB5 or 0xB6 or 0xB4
                        or 0xAD or 0xAE or 0xAC or 0xBD or 0xB9 or 0xBC or 0xBE)
                {
                    _pendingSoftCompareCommit = true;
                    _stagedCarryUpdate = false;
                }
                else
                {
                    UpdateNZ(_stagedNzValue);
                    _stagedNzUpdate = false;
                }
            }
            else if (_opcode is 0xB1)
            {
                // Same one-shot LOAD rule as the _stagedNzUpdate path above.
                A = _stagedNzValue;
                UpdateNZ(A);
            }

            var predecessorConsumedLoadFetch =
                PriorInstructionConsumesFollowingFetchPhase(
                    _previousOpcode,
                    DebugPriorTrailingAtNextPc)
                || (IsStackPushOpcode(_previousOpcode)
                    && DebugPriorTrailingAtNextPc >= 1)
                || _previousOpcode is 0x4C or 0x6C;
            var loadFusedPc = (
                (_opcode is 0xB1 && _fuseIndyLoadLastClkPc)
                || (_opcode == 0xA5
                    && _stagedLoadRegisterVisibleAtReadCheckpoint)
                || (_opcode is 0xBD
                    && !_deferAbsoluteXLoadCompletionAfterBranch
                    && (DebugPriorTrailingAtNextPc >= 1
                        || (IsImpliedRegisterOrFlagOpcode(_previousOpcode)
                            && DebugPriorTrailingAtNextPc == 0)))
                || (_opcode is 0xB9 && _fuseAbsYAfterStolenSamePageBranch)
                || (_opcode is 0xB9
                    && _previousOpcode == 0x10
                    && DebugPriorTrailingAtNextPc >= 1)
                || (_opcode is 0xA4 or 0xA6 or 0xAE
                    && (predecessorConsumedLoadFetch
                        || (IsBranchOpcode(_previousOpcode)
                            && DebugPriorTrailingAtNextPc >= 1))
                    && !(_afterFullLengthTakenBranch || _afterShortTakenBranchLag))
                // LDA abs inherits the predecessor's source phase. A not-taken
                // branch or a predecessor that already consumed the following FETCH
                // exports next PC on GET_ABS. Otherwise non-overlapped GET_ABS keeps
                // opcode PC through its final clock.
                || (_opcode is 0xAD
                    && !(_afterFullLengthTakenBranch || _afterShortTakenBranchLag)
                    && ((IsBranchOpcode(_previousOpcode)
                            && !IsBranchTaken(_previousOpcode))
                        || predecessorConsumedLoadFetch)));
            var holdLoadOpcodePc = ((_nonOverlappedFetchPhase || _nonOverlappedRegion)
                    && !loadFusedPc
                    && _opcode is 0xB1 or 0xA5 or 0xA6 or 0xA4 or 0xB5 or 0xB6 or 0xB4
                        or 0xAD or 0xAE or 0xAC or 0xBD or 0xB9 or 0xBC or 0xBE)
                || (_opcode == 0xB1 && IsStoreOpcode(_previousOpcode) && !loadFusedPc)
                || absoluteLoadAfterRts;
            if (holdLoadOpcodePc
                && !(_opcode == 0xAD && (_skipAbsLoadLastClkHold
                    || (_loadAEarlyAfterStagedBranch && _afterFullLengthTakenBranch))))
            {
                // Keep opcode PC this tick; soft NZ/PC commit on next FETCH.
                // LDA (zp),Y after STY zp: VICE GET_IND_Y last CLK still
                // opcode PC (Wolf64 4131417 nPC=$E606). Next FETCH is not
                // overlapped (Wolf64 4131419 CMP# nPC=$E608 nP=$21).
                _visiblePC = _opcodeAddress;
                _suppressBootstrapBoundary = true;
                _nonOverlappedFetchPhase = true;
            }
            else
            {
                _instructionPC = _pc;
                _visiblePC = _pc;
                if (_opcode is 0xB9)
                    _fuseAbsYAfterStolenSamePageBranch = false;
                // NonOvl store apply must leave IsInstructionBoundary true so
                // SystemClock can arm IRQ on the same inter-instruction sample
                // VICE DO_INTERRUPT uses after ST (c=577682: native entered IRQ
                // with return PC at post-STA-zp $E5EC while managed suppress
                // skipped the sample and ran STA abs). PC export is already
                // next-PC above; do not suppress boundary here.
                // LDA zp after taken same-page branch: VICE GET_ZERO CLK_INC
                // then INC_PC with no extra CLK. Opening this fused last CLK
                // started IRQ before STA FETCH (Wolf64 2208208 nPC=$E5D1
                // nLastOp=$85 mS=$F2). Same shape as deferred-imm suppress
                // (c=522261).
                if (_opcode == 0xA5
                    && (_afterFullLengthTakenBranch || _afterShortTakenBranchLag
                        || IsBranchOpcode(_previousOpcode)))
                {
                    // This fused last CLK is VICE GET_ZERO, not DO_INTERRUPT.
                    // Sample IRQ on the next tick before STA FETCH (2175368
                    // nS=$F2 nPC=$E5CF). If irq_clk has not elapsed, STA runs
                    // (2208208 nPC=$E5D1). GET_ZERO INC_PC has no extra CLK;
                    // counting this host tick as CLK_INC made delay>=2 at STA
                    // FETCH (Wolf64 2208208 irqSeq=5 vs nLastOp=$85).
                    _suppressBootstrapBoundary = true;
                    if (_ldaZpDataReadStolen)
                        _skipIrqSampleAtNextFetch = true;
                    else
                        _sampleIrqBeforeNextFetch = true;
                    _ldaZpDataReadStolen = false;
                    ConsumedViceClockThisTick = false;
                }
                else if (IsStoreOpcode(_opcode))
                {
                    // VICE ST: INC_PC (no CLK) then the addressing-mode
                    // STORE CLK_INC. Plain store writes are exported on cycle 1,
                    // so their cycle-0 apply consumes no additional VICE clock.
                    // STA (zp),Y maps its final SET_IND_Y store checkpoint to
                    // cycle 0, so keep that clock counted for IRQ delay before
                    // the following fetch.
                    if (!IsIndirectYStoreOpcode(_opcode))
                    {
                        ConsumedViceClockThisTick = false;
                    }
                    else
                    {
                        // The STORE checkpoint is not yet the main-loop
                        // DO_INTERRUPT boundary. Sample on the next Tick before
                        // fetching the following opcode, without adding a clock.
                        _suppressBootstrapBoundary = true;
                        _sampleIrqBeforeNextFetch = true;
                    }
                    if (_opcode == 0x85 && _staZpApplyStolen)
                    {
                        _suppressBootstrapBoundary = true;
                        _skipIrqSampleAtNextFetch = true;
                        _staZpApplyStolen = false;
                    }
                    if (_opcode == 0x8D)
                    {
                        // STORE CLK is not DO_INTERRUPT (suppress). Unstolen
                        // path samples IRQ before the following FETCH
                        // (2142523 nS=$F2). Stolen write cycle: FETCH the
                        // wait-loop BEQ dummy instead (2520240 nPC=$E5D6).
                        _suppressBootstrapBoundary = true;
                        if (_staAbsWriteCycleStolen)
                            _skipIrqSampleAtNextFetch = true;
                        else
                            _sampleIrqBeforeNextFetch = true;
                        _staAbsWriteCycleStolen = false;
                    }
                }
            }

            _stagedMemoryReadCompleted = false;
            _stagedLoadRegisterVisibleAtReadCheckpoint = false;
            if (_opcode == 0xA5)
                _fuseLdaZpAfterStolenInc = false;
            if (_opcode == 0xAD
                && (_afterFullLengthTakenBranch
                    || _afterShortTakenBranchLag))
            {
                // Taken BNE after LDA abs still needs the extra dummy CLK
                // even when BA already consumed one FETCH CLK (Wolf64 2050518).
                _afterLdaAbsHold = true;
                if (!_skipAbsLoadLastClkHold
                    && !(_loadAEarlyAfterStagedBranch && _afterFullLengthTakenBranch))
                {
                    // VICE GET_ABS last CLK still has opcode PC; INC_PC is after
                    // CLK_INC with no extra clock (Wolf64 2126189 nPC=$F6BC).
                    // Sticky _loadAEarlyAfterStagedBranch from an earlier JUMP
                    // must not skip this hold on a short taken BCC.
                    // Do not arm this on every $AD: that adds a 5th LDA cycle
                    // and breaks First10000 at c=130.
                    _holdAbsLoadOpcodeLastClk = true;
                    _nonOverlappedFetchPhase = true;
                    _visiblePC = _opcodeAddress;
                    _suppressBootstrapBoundary = true;
                }
                else
                    _ldaSkippedLastClkHold = true;
            }

            // _skipAbsLoadLastClkHold is consumed at the following BNE FETCH.
            if (_opcode == 0xAD)
            {
                _loadAEarlyAfterStagedBranch = false;
            }

            return true;
        }

        if (_cycle == 2 && IsZeroPageIncrementDecrementOpcode(_opcode))
        {
            if (_deferZeroPageRmwPcAdvanceAfterBranch)
            {
                _deferZeroPageRmwPcAdvanceAfterBranch = false;
                _deferNextIndirectYLoadAfterBranchRmw = true;
                return false;
            }

            if (_deferZpRmwPcAdvanceOne)
            {
                // Hold opcode PC one more CLK (extra FETCH after not-taken BNE).
                _deferZpRmwPcAdvanceOne = false;
                _nextJsrNonOverlapped = true;
                _visiblePC = _opcodeAddress;
                return true;
            }

            // After a load whose last CLK still held opcode PC (trail==0),
            // VICE GET_ZERO has not INC_PCd yet at INC cycle 2
            // (Wolf64 4138188 nPC=$A5F7 mPC=$A5F9 after LDX zp). Trail>=1
            // still advances here (c=517829). STA abs,Y after a full-length
            // taken branch is the 4154106 hold; other $99 still advance
            // (2125150 nPC=$BEAA).
            if (DebugPriorTrailingAtNextPc == 0)
            {
                _visiblePC = _opcodeAddress;
                _inySoftAfterHeldInc = true;
                return true;
            }

            if (_holdZpIncDecAfterStaAbsY)
            {
                _holdZpIncDecAfterStaAbsY = false;
                _visiblePC = _opcodeAddress;
                return true;
            }

            // VICE INC/DEC zp (6510core.c): after load + dummy RMW write,
            // LOCAL_SET_NZ and INC_PC become visible before the final STORE CLK.
            // Absolute RMW already matches this; zp was advancing PC only and
            // lagging NZ until cycle 1 (diverge at c=517829: nP=$21 mP=$23).
            CommitZeroPageRmwModifyAndFlags();
            if (_zpRmwPcDeferredFromSteal)
            {
                return true;
            }

            AdvanceVisiblePc(2);
            return true;
        }

        if (_cycle == 1 && IsZeroPageIncrementDecrementOpcode(_opcode) && _visiblePC == _opcodeAddress
            && !_stagedMemoryReadCompleted)
        {
            // VICE LOCAL_SET_NZ then INC_PC with no CLK, then dummy/store
            // CLK_INC. Hosted lockstep still exports opcode PC on those CLKs
            // (Wolf64 2093350-51 nPC=$F69F). After BA skip, commit NZ/STORE
            // here but keep opcode PC until the next FETCH.
            CommitZeroPageRmwModifyAndFlags();
            if (!_zpRmwPcDeferredFromSteal)
                AdvanceVisiblePc(2);
            // do not return - allow cycle==1 final-store switch to run
        }

        // ASL/LSR/ROL/ROR: VICE SET_C/NZ + INC_PC before final STORE.
        // Host-visible modify phase (zp 5-cycle / zp,X 6-cycle):
        //   cycle 1: zp,X after taken branch target (short c=539721 or full);
        //            zp after branch trail==0 (c=541100); prior shift trail==2
        //   cycle 2: default including after not-taken branch (c=543280 ASL
        //            zp,X after BCS nPC advanced at dbgCyc=2 when cycle-1 lag)
        if (!_zpRmwModifyCommitted && IsZeroPageShiftRmwOpcode(_opcode))
        {
            var zpX = IsZeroPageXShiftRmwOpcode(_opcode);
            var commitAtCycle1 =
                (zpX && (_afterFullLengthTakenBranch || _afterShortTakenBranchLag))
                || (!zpX && IsBranchOpcode(_previousOpcode)
                    && DebugPriorTrailingAtNextPc == 0)
                || (IsZeroPageShiftRmwOpcode(_previousOpcode)
                    && DebugPriorTrailingAtNextPc == 2);
            if ((_cycle == 1 && commitAtCycle1) || (_cycle == 2 && !commitAtCycle1))
            {
                CommitZeroPageShiftRmwModifyAndFlags();
                AdvanceVisiblePc(2);
                return true;
            }
        }

        if (_cycle == 4 && IsIndirectYStoreOpcode(_opcode))
        {
            // VICE FETCH_OPCODE performs two CLK_INC calls before ST executes
            // its unclocked INC_PC(2). A completed memory-addressed LD body has
            // already performed its own unclocked register/flag/INC_PC work after
            // the addressing read. When overlap remains enabled, the following
            // store consumes its first FETCH even if a taken-branch load defer
            // reports zero trailing checkpoints. An explicit non-overlapped phase
            // and immediate LD retain their distinct two-FETCH ordering. Other
            // predecessors use exported source-clock thresholds.
            var predecessorCompletedLoadBody =
                IsLoadOpcode(_previousOpcode)
                && !IsImmediateLoadOpcode(_previousOpcode)
                && !(_nonOverlappedFetchPhase || _nonOverlappedRegion);
            if (_advanceStorePcAfterFusedImplied
                || predecessorCompletedLoadBody
                || PriorInstructionConsumesFollowingFetchPhase(
                    _previousOpcode,
                    DebugPriorTrailingAtNextPc))
            {
                AdvanceVisiblePc(2);
            }
            else
            {
                _softDeferCompareCommit = true;
                _softDeferImpliedAfterHeldIndYStore = true;
                _visiblePC = _opcodeAddress;
            }

            return true;
        }

        // LDA/CMP (zp),Y: stage pointer fetches across cycles so the data-read
        // CLK does not re-touch V-bus (VICE INT_IND_Y_R: LOAD_ZERO lo/hi on
        // earlier CLKs, LOAD data last). Re-fetching hi on the data CLK was
        // overwriting VIC-refreshed v_bus_last_data with pointer-high $96 and
        // forcing color RAM open-bus to $9x (c=1316731 mA=$91 nA=$01).
        if (_cycle == 4 && IsIndirectYLoadOrCompareOpcode(_opcode))
        {
            // FETCH zp operand (already at _instructionPC+1 from opcode fetch;
            // re-read for V-bus / open-bus fidelity).
            _ = Read((ushort)(_instructionPC + 1));
            return true;
        }

        if (_cycle == 3 && IsIndirectYLoadOrCompareOpcode(_opcode))
        {
            var zp = Read((ushort)(_instructionPC + 1));
            _fetched = Read(zp); // pointer low
            return true;
        }

        if (_cycle == 2 && IsIndirectYLoadOrCompareOpcode(_opcode))
        {
            var zp = Read((ushort)(_instructionPC + 1));
            var lo = _fetched;
            var hi = Read((byte)(zp + 1));
            _effectiveAddress = (ushort)((lo | (hi << 8)) + Y);
            // VICE INT_IND_Y_R extra CLK when lo+Y page-crosses. After a
            // full-length taken branch that extra CLK is the GET_IND_Y last
            // CLK (opcode PC). No-cross: last CLK already INC_PC so the
            // deferred-NZ tick is the next FETCH (Wolf64 4131820 nPC=$E608).
            _indyPageCrossedThisInsn = (lo + Y) > 0xFF;
            return true;
        }

        // Between pointer-high (cycle 2) and data-read (cycle 1): VICE VIC
        // may refresh v_bus on the intervening phi2 (phi order VIA→VIC→CPU).
        // No CPU work here; VIC.Tick already ran before this CPU cycle.

        if (_cycle == 3 && IsIndirectYStoreOpcode(_opcode))
        {
            // VICE ST executes INC_PC(2) after the second fetch checkpoint and
            // before the first INT_IND_Y_W pointer-read checkpoint.
            AdvanceVisiblePc(2);
            return true;
        }

        if (IsStagedAbsoluteRmwOpcode(_opcode) && TryExecuteStagedAbsoluteRmwCycle())
        {
            return true;
        }

        if (_cycle == 2 && IsIndexedAbsoluteStoreOpcode(_opcode))
        {
            if (_deferIndexedStorePcAdvanceAfterBranch)
            {
                _deferIndexedStorePcAdvanceAfterBranch = false;
                _indexedStorePcAdvanceWasDeferred = true;
                return false;
            }

            // Non-overlapped clean first FETCH: VICE ST does INC_PC only after
            // both FETCH CLKs (6510dtvcore ST + SET_ABS_*). Hold opcode PC here;
            // FinishStagedMemoryWrite on cycle 1 makes the advance host-visible.
            if (_nonOverlappedRegion || _nonOverlappedFetchPhase)
            {
                _visiblePC = _opcodeAddress;
                return true;
            }

            AdvanceVisiblePc(3);
            return true;
        }

        if (_cycle == 2 && IsZeroPageIndexedStoreOpcode(_opcode))
        {
            if (_deferZeroPageIndexedStorePcAdvanceAfterBranch)
            {
                _deferZeroPageIndexedStorePcAdvanceAfterBranch = false;
                _indexedStorePcAdvanceWasDeferred = true;
                return false;
            }

            // Non-overlapped STY/STA/STX zp,index (c=518814 nPC=$E568 mPC=$E56A):
            // VICE SET_ZERO_X runs after FETCH; INC_PC is not visible on the
            // second host sample. Hold opcode PC; cycle-1 write advances it.
            if (_nonOverlappedRegion || _nonOverlappedFetchPhase)
            {
                _visiblePC = _opcodeAddress;
                return true;
            }

            AdvanceVisiblePc(2);
            return true;
        }

        if (_cycle != 1)
        {
            return false;
        }

        switch (_opcode)
        {
            case 0xC5 when _fuseCmpZpAfterStolenIncLda:
            {
                var cmpVal = Read(ReadZeroPageOperand());
                if (A >= cmpVal)
                    P |= 0x01;
                else
                    P &= 0xFE;
                UpdateNZ((byte)(A - cmpVal));
                _pc = (ushort)(_instructionPC + 2);
                _visiblePC = _pc;
                _fuseCmpZpAfterStolenIncLda = false;
                _overlapNextTakenBranchDummy = true;
                _suppressBootstrapBoundary = true;
                return true;
            }
            case 0xA5:
            {
                var aZp = Read(ReadZeroPageOperand());
                // Non-overlapped: VICE exports pre-load A on the data-read CLK
                // (same shape as LDA (zp),Y). Overlapped path commits now.
                // After taken branch trail>=1 VICE already shows loaded A on
                // data-read (c=541197 nA=$20 mA=$1E after BNE trail=2).
                // After taken branch trail>=1, after SEC/CLC-class flag op
                // trail==0 (c=559268), or after non-load imm trail==1 (c=559273
                // LDA zp after SBC# nA=$00 mA=$02 when deferred). Not after
                // DEX/INX (c=539747 over-fuse).
                var fuseA5Early = (IsBranchOpcode(_previousOpcode)
                        && DebugPriorTrailingAtNextPc >= 1)
                    || (IsImpliedFlagOpcode(_previousOpcode)
                        && DebugPriorTrailingAtNextPc == 0)
                    || (IsTwoByteImmediateOpcode(_previousOpcode)
                        && !IsImmediateLoadOpcode(_previousOpcode)
                        && DebugPriorTrailingAtNextPc == 1)
                    || (_previousOpcode == 0x20 && DebugPriorTrailingAtNextPc >= 1)
                    // PHA completes its push before the following FETCH. VICE
                    // GET_ZERO therefore exposes LDA zp's loaded A on the
                    // data-read checkpoint (Wolf64 sample 14977).
                    || _previousOpcode == 0x48
                    || PriorInstructionConsumesFollowingFetchPhase(
                        _previousOpcode,
                        DebugPriorTrailingAtNextPc)
                    || _fuseLdaZpAfterStolenInc;
                var registerVisibleAtReadCheckpoint =
                    !(_nonOverlappedFetchPhase || _nonOverlappedRegion)
                    || fuseA5Early;
                if (registerVisibleAtReadCheckpoint)
                    A = aZp;
                FinishStagedMemoryRead(2, aZp, registerVisibleAtReadCheckpoint);
                if (_fuseLdaZpAfterStolenInc)
                {
                    // VICE GET_ZERO CLK_INC then INC_PC with no extra CLK
                    // (Wolf64 2129137 nPC=$E6BD mPC=$E6BB). Keep the flag
                    // through last CLK so apply does not re-hold opcode PC
                    // (Wolf64 2129138 nPC=$E6BD mPC=$E6BB).
                    _visiblePC = _pc;
                }
                return true;
            }
            case 0xA6:
            {
                var xZp = Read(ReadZeroPageOperand());
                // LDX and LDY zero-page share VICE's LD(GET_ZERO) ordering.
                // Expose the loaded register on GET_ZERO's data clock only when
                // the predecessor's source clocks consumed the overlapping
                // fetch phase. Taken-branch target entry remains distinct.
                var predecessorConsumedFetch =
                    PriorInstructionConsumesFollowingFetchPhase(
                        _previousOpcode,
                        DebugPriorTrailingAtNextPc)
                    || (IsStackPushOpcode(_previousOpcode)
                        && DebugPriorTrailingAtNextPc >= 1)
                    || _previousOpcode is 0x4C or 0x6C;
                // A not-taken branch has no JUMP phase. When its offset FETCH
                // exported at least one next-PC checkpoint, fall-through
                // LD(GET_ZERO) shares that consumed phase and exposes X on the
                // data clock. A zero-checkpoint branch keeps pre-load X.
                var notTakenBranchConsumedFetch = IsBranchOpcode(_previousOpcode)
                    && DebugPriorTrailingAtNextPc >= 1;
                var fuseA6Early = (predecessorConsumedFetch
                        || notTakenBranchConsumedFetch)
                    && !(_afterFullLengthTakenBranch || _afterShortTakenBranchLag);
                if (!(_nonOverlappedFetchPhase || _nonOverlappedRegion)
                    || fuseA6Early)
                    X = xZp;
                FinishStagedMemoryRead(2, xZp);
                return true;
            }
            case 0xA4:
            {
                var yZp = Read(ReadZeroPageOperand());
                // Non-overlapped: keep pre-load Y on the data-read checkpoint
                // unless the predecessor has already consumed LDY's following
                // FETCH phase. VICE LD assigns Y before GET_ZERO CLK_INC; the
                // host sees the assignment at that checkpoint only when the
                // predecessor's source-ordered trailing clocks overlap it.
                // Control-transfer JUMP enters directly at the target FETCH.
                var predecessorConsumedFetch =
                    PriorInstructionConsumesFollowingFetchPhase(
                        _previousOpcode,
                        DebugPriorTrailingAtNextPc)
                    || (IsStackPushOpcode(_previousOpcode)
                        && DebugPriorTrailingAtNextPc >= 1)
                    || _previousOpcode is 0x4C or 0x6C;
                // A not-taken branch has no JUMP phase. When its offset FETCH
                // exported at least one next-PC checkpoint, fall-through
                // LD(GET_ZERO) shares that consumed phase and exposes Y on the
                // data clock. A zero-checkpoint branch keeps pre-load Y.
                var notTakenBranchConsumedFetch = IsBranchOpcode(_previousOpcode)
                    && DebugPriorTrailingAtNextPc >= 1;
                var fuseA4Early = (predecessorConsumedFetch
                        || notTakenBranchConsumedFetch)
                    && !(_afterFullLengthTakenBranch || _afterShortTakenBranchLag);
                if (!(_nonOverlappedFetchPhase || _nonOverlappedRegion)
                    || fuseA4Early)
                    Y = yZp;
                FinishStagedMemoryRead(2, yZp);
                return true;
            }
            case 0xB5:
            {
                var aZpx = Read((byte)(ReadZeroPageOperand() + X));
                if (!(_nonOverlappedFetchPhase || _nonOverlappedRegion))
                    A = aZpx;
                FinishStagedMemoryRead(2, aZpx);
                return true;
            }
            case 0xB6:
            {
                var xZpy = Read((byte)(ReadZeroPageOperand() + Y));
                if (!(_nonOverlappedFetchPhase || _nonOverlappedRegion))
                    X = xZpy;
                FinishStagedMemoryRead(2, xZpy);
                return true;
            }
            case 0xB4:
            {
                var yZpx = Read((byte)(ReadZeroPageOperand() + X));
                if (!(_nonOverlappedFetchPhase || _nonOverlappedRegion))
                    Y = yZpx;
                FinishStagedMemoryRead(2, yZpx);
                return true;
            }
            case 0x85:
                _bus.Write(ReadZeroPageOperand(), A);
                FinishStagedMemoryWrite(2);
                return true;
            case 0x86:
                _bus.Write(ReadZeroPageOperand(), X);
                FinishStagedMemoryWrite(2);
                return true;
            case 0x96:
                _bus.Write((byte)(ReadZeroPageOperand() + Y), X);
                FinishStagedMemoryWrite(2);
                DelayNextFetchAfterDeferredIndexedStorePcAdvance();
                return true;
            case 0x84:
                _bus.Write(ReadZeroPageOperand(), Y);
                FinishStagedMemoryWrite(2);
                return true;
            case 0x95:
                _bus.Write((byte)(ReadZeroPageOperand() + X), A);
                FinishStagedMemoryWrite(2);
                DelayNextFetchAfterDeferredIndexedStorePcAdvance();
                return true;
            case 0x94:
                _bus.Write((byte)(ReadZeroPageOperand() + X), Y);
                FinishStagedMemoryWrite(2);
                DelayNextFetchAfterDeferredIndexedStorePcAdvance();
                return true;
            case 0x2C when _bitSoftDeferEarly:
            case 0x24 when _bitSoftDeferEarly:
                SoftDeferBitBody();
                return true;
            case 0xAD:
            {
                var aAbs = Read(ReadAbsoluteOperand());
                // VICE FETCH_OPCODE($AD) is 3 CLK (fetch_tab=1) then GET_ABS
                // LOAD+CLK_INC. JUMP has no CLK.
                // Short taken BCC: cycle 1 is FETCH of p2 (Wolf64 2126188 nA=$B0).
                // Full-length without BA dummy-steal: cycle 1 still pre-load
                // (Wolf64 2044662 nA=$06). Stolen dummy consumes a FETCH CLK
                // (_loadAEarlyAfterStagedBranch) so cycle 1 is GET_ABS
                // (Wolf64 2050522 nA=$D012). Not-taken overlaps FETCH (2060353).
                var afterTakenBranch = _afterFullLengthTakenBranch
                    || _afterShortTakenBranchLag
                    || (IsBranchOpcode(_previousOpcode) && IsBranchTaken(_previousOpcode));
                var afterNotTakenBranch =
                    IsBranchOpcode(_previousOpcode) && !IsBranchTaken(_previousOpcode);
                var nonOverlappedAbsoluteLoad =
                    _nonOverlappedFetchPhase || _nonOverlappedRegion;
                var predecessorConsumedFetch =
                    PriorInstructionConsumesFollowingFetchPhase(
                        _previousOpcode,
                        DebugPriorTrailingAtNextPc);
                // RTS performs LOAD+CLK_INC, then increments and JUMPs without
                // another clock. Its target's LD(GET_ABS) therefore reaches the
                // data-read checkpoint before the destination register is
                // host-visible. RTI's final PULL phase maps differently.
                var afterRts = _previousOpcode == 0x60;
                if (afterNotTakenBranch
                    || (!afterTakenBranch
                        && !afterRts
                        && (!nonOverlappedAbsoluteLoad || predecessorConsumedFetch))
                    || (_afterFullLengthTakenBranch && _loadAEarlyAfterStagedBranch))
                {
                    A = aAbs;
                }
                FinishStagedMemoryRead(3, aAbs);
                return true;
            }
            case 0xAE:
            {
                var xAbs = Read(ReadAbsoluteOperand());
                // VICE GET_ABS writes X then CLK_INC. When previous already
                // exported next-PC (trail>=1), data-read shows loaded X
                // (Wolf64 2571284 nX=$0E). After a taken branch or RTS target
                // entry it still exposes pre-load X. Other overlapped paths
                // already commit.
                var afterRts = _previousOpcode == 0x60;
                var ldxAbsCommitXOnDataRead = !afterRts
                    && !_afterFullLengthTakenBranch
                    && !_afterShortTakenBranchLag
                    && !IsBranchOpcode(_previousOpcode)
                    && (DebugPriorTrailingAtNextPc >= 1 || _fuseImpliedAfterIndyLoad);
                if (!afterRts
                    && (ldxAbsCommitXOnDataRead
                        || !(_nonOverlappedFetchPhase || _nonOverlappedRegion)))
                    X = xAbs;
                FinishStagedMemoryRead(3, xAbs);
                return true;
            }
            case 0xAC:
            {
                var yAbs = Read(ReadAbsoluteOperand());
                var afterRts = _previousOpcode == 0x60;
                if (!afterRts
                    && !(_nonOverlappedFetchPhase || _nonOverlappedRegion))
                    Y = yAbs;
                FinishStagedMemoryRead(3, yAbs);
                return true;
            }
            case 0x8D:
                _bus.Write(ReadAbsoluteOperand(), A);
                FinishStagedMemoryWrite(3);
                return true;
            case 0x8E:
                var stxAddress = ReadAbsoluteOperand();
                if (ShouldDeferAbsoluteStore?.Invoke(stxAddress) == true)
                {
                    return false;
                }

                _bus.Write(stxAddress, X);
                FinishStagedMemoryWrite(3);
                return true;
            case 0x8C:
                _bus.Write(ReadAbsoluteOperand(), Y);
                FinishStagedMemoryWrite(3);
                return true;
            case 0x9D:
                _bus.Write((ushort)(ReadAbsoluteOperand() + X), A);
                FinishStagedMemoryWrite(3);
                DelayNextFetchAfterDeferredIndexedStorePcAdvance();
                return true;
            case 0x99:
                _bus.Write((ushort)(ReadAbsoluteOperand() + Y), A);
                FinishStagedMemoryWrite(3);
                DelayNextFetchAfterDeferredIndexedStorePcAdvance();
                return true;
            case 0xC6:
            case 0xE6:
                // Final STORE CLK after NZ+PC already match VICE INC/DEC macro.
                if (_zpRmwModifyCommitted)
                {
                    _bus.Write(_effectiveAddress, _fetched);
                    _zpRmwModifyCommitted = false;
                    if (_zpRmwPcDeferredFromSteal)
                    {
                        // Dummy+store CLK_INC still export opcode PC; INC_PC
                        // becomes visible on the following FETCH_OPCODE.
                        // Do not assign _opcodeAddress: it can lag the visible
                        // opcode PC (Wolf64 2093350 mPC=$F69D nPC=$F69F).
                        _stagedMemoryReadCompleted = true;
                        return true;
                    }

                    FinishStagedMemoryWrite(2);
                    return true;
                }

                // Fallback (e.g. after-branch defer returned false at cycle 2):
                // full RMW + NZ on this CLK so host still pairs flags with PC.
                if (_zpRmwPcDeferredFromSteal)
                {
                    CommitZeroPageRmwModifyAndFlags();
                    _bus.Write(_effectiveAddress, _fetched);
                    _zpRmwModifyCommitted = false;
                    _stagedMemoryReadCompleted = true;
                    return true;
                }

                if (_opcode == 0xE6)
                    IncrementStagedMemory(ReadZeroPageOperand(), 2);
                else
                    DecrementStagedMemory(ReadZeroPageOperand(), 2);
                return true;
            case 0x06:
            case 0x16:
            case 0x26:
            case 0x36:
            case 0x46:
            case 0x56:
            case 0x66:
            case 0x76:
                // Shift RMW already committed at cycle 2 (zp) or 1 (zp,X).
                // Keep flag set until cycle 0 so ExecuteOpcode is skipped
                // (clearing here caused a second Advance/PC bump: c=539964 mPC=$E8C6).
                if (_zpRmwModifyCommitted)
                    return true;

                CommitZeroPageShiftRmwModifyAndFlags();
                AdvanceVisiblePc(2);
                return true;
            case 0xBD:
                var absoluteXBase = ReadAbsoluteOperand();
                if (TryDelayIndexedLoadPageCross(absoluteXBase, X))
                {
                    return true;
                }

                var absoluteXValue = Read((ushort)(absoluteXBase + X));
                // Non-overlapped: pre-load A on data-read CLK (c=519271 nA=$FF mA=$E4).
                // After TSX/implied trail==0 VICE already shows loaded A on
                // data-read (c=559234 nA=$22 mA=$0A). After previous last CLK
                // already at next-PC (trail>=1), GET_ABS_X is this CLK
                // (Wolf64 2406964 nA=$EB mA=$81). Directly after full-length
                // taken branch still pre-loads (_deferAbsoluteXLoadCompletionAfterBranch).
                if (!_deferAbsoluteXLoadCompletionAfterBranch
                    && (!(_nonOverlappedFetchPhase || _nonOverlappedRegion)
                        || DebugPriorTrailingAtNextPc >= 1
                        || (IsImpliedRegisterOrFlagOpcode(_previousOpcode)
                            && DebugPriorTrailingAtNextPc == 0)))
                {
                    A = absoluteXValue;
                }

                FinishStagedMemoryRead(3, absoluteXValue);
                return true;
            case 0xB9:
                var absoluteYBase = ReadAbsoluteOperand();
                if (TryDelayIndexedLoadPageCross(absoluteYBase, Y))
                {
                    return true;
                }

                var absoluteYValue = Read((ushort)(absoluteYBase + Y));
                var fuseAbsYAfterNotTakenBpl = _previousOpcode == 0x10
                    && DebugPriorTrailingAtNextPc >= 1;
                if ((_fuseAbsYAfterStolenSamePageBranch || fuseAbsYAfterNotTakenBpl)
                    && !_deferAbsoluteYLoadCompletionAfterBranch)
                {
                    A = absoluteYValue;
                }
                else if (!_deferAbsoluteYLoadCompletionAfterBranch
                    && !(_nonOverlappedFetchPhase || _nonOverlappedRegion))
                {
                    A = absoluteYValue;
                }

                FinishStagedMemoryRead(3, absoluteYValue);
                return true;
            case 0xBC:
                var ldyBase = ReadAbsoluteOperand();
                if (TryDelayIndexedLoadPageCross(ldyBase, X))
                {
                    return true;
                }

                var ldyAbsX = Read((ushort)(ldyBase + X));
                if (!(_nonOverlappedFetchPhase || _nonOverlappedRegion))
                    Y = ldyAbsX;
                FinishStagedMemoryRead(3, ldyAbsX);
                return true;
            case 0xBE:
                var ldxBase = ReadAbsoluteOperand();
                if (TryDelayIndexedLoadPageCross(ldxBase, Y))
                {
                    return true;
                }

                var ldxAbsY = Read((ushort)(ldxBase + Y));
                if (!(_nonOverlappedFetchPhase || _nonOverlappedRegion))
                    X = ldxAbsY;
                FinishStagedMemoryRead(3, ldxAbsY);
                return true;
            case 0xB1:
            {
                // Effective address staged on cycles 4/3/2; data-read only here
                // so V-bus is not clobbered by a same-CLK pointer re-fetch.
                var indirectYValue = Read(_effectiveAddress);
                // Lag-shaped path: A lands on the data-read tick. Non-overlapped
                // callees defer A to the apply tick so pre-load A matches xvic.
                // After fused LDY# trail==1 or not-taken BEQ trail==1, VICE already
                // shows loaded A on data-read (c=541172 after LDY#; c=541183 after
                // BEQ nA=$3E mA=$9B when deferred). After any previous insn
                // that already exported next-PC (trail>=1), GET_IND_Y is the
                // data-read CLK (Wolf64 2406425 nA=$4C mA=$1F).
                var indyCommitAOnDataRead = !_deferIndirectYLoadCompletionAfterBranch
                    && !IsStoreOpcode(_previousOpcode)
                    && (!_nonOverlappedFetchPhase && !_nonOverlappedRegion
                        || DebugPriorTrailingAtNextPc >= 1
                        || _fuseImpliedAfterIndyLoad
                        || ((IsImmediateLoadOpcode(_previousOpcode)
                                || IsBranchOpcode(_previousOpcode))
                            && DebugPriorTrailingAtNextPc == 1));
                if (indyCommitAOnDataRead)
                    A = indirectYValue;
                FinishStagedMemoryRead(2, indirectYValue);
                return true;
            }
            case 0xCD:
                CompareStagedMemory(ReadAbsoluteOperand(), 3);
                return true;
            case 0xD1:
                CompareStagedMemory(_effectiveAddress, 2);
                return true;
            case 0x48:
                Push(A);
                FinishStagedStackPush();
                return true;
            case 0x08:
                Push((byte)(P | 0x10));
                FinishStagedStackPush();
                return true;
            case 0x91:
                _bus.Write(ReadIndirectYOperand(), A);
                FinishStagedMemoryWrite(2);
                return true;
            default:
                return false;
        }
    }

    private bool TryExecuteCycleStagedStackPullOpcode()
    {
        if (_opcode is not (0x68 or 0x28))
        {
            return false;
        }

        // VICE PLA/PLP: FETCH, FETCH, STACK_PEEK, PULL. The status/NZ and
        // INC_PC work follows the pull checkpoint without another CLK.
        if (_cycle == 1)
        {
            _ = Read((ushort)(0x0100 | S));
            return true;
        }

        if (_cycle != 0)
        {
            return false;
        }

        _fetched = Pop();
        _visiblePC = _instructionPC;
        _suppressBootstrapBoundary = true;
        if (_opcode == 0x68)
        {
            A = _fetched;
            _pendingPlaCompletion = true;
        }
        else
        {
            _pendingPlpStatus = true;
        }

        return true;
    }

    private bool TryExecuteCycleStagedRtsOpcode()
    {
        if (_opcode != 0x60)
        {
            return false;
        }

        // VICE 6510dtvcore.c RTS is invariant for x64sc/xvic because both hosts
        // define SKIP_CYCLE as zero:
        //   FETCH_OPCODE CLK, FETCH_OPCODE CLK, STACK_PEEK CLK,
        //   PULL low CLK, PULL high CLK, LOAD(return-address) CLK,
        //   then increment and JUMP with no CLK.
        // A predecessor that already exported enough next-PC checkpoints
        // overlaps the first FETCH with its final source clock. The source
        // sequence stays unchanged, but each remaining host checkpoint is one
        // micro-operation farther along.
        var priorConsumesFetchPhase =
            !_rtsPrefetchedByNotTakenBranch
            && PriorInstructionConsumesFollowingFetchPhase(
                _previousOpcode,
                DebugPriorTrailingAtNextPc);
        switch (_cycle)
        {
            case 3:
                if (priorConsumesFetchPhase)
                    _stagedReturnAddress = Pop();
                else
                    _ = Read((ushort)(0x0100 | S));
                return true;
            case 2:
                if (priorConsumesFetchPhase)
                    _stagedReturnAddress |= (ushort)(Pop() << 8);
                else
                    _stagedReturnAddress = Pop();
                return true;
            case 1:
                if (priorConsumesFetchPhase)
                {
                    CompleteRtsReturn();
                    _cycle = 0;
                }
                else
                {
                    _stagedReturnAddress |= (ushort)(Pop() << 8);
                }
                return true;
            case 0:
                CompleteRtsReturn();
                return true;
            default:
                return false;
        }
    }

    private void CompleteRtsReturn()
    {
        _ = Read(_stagedReturnAddress);
        _pc = (ushort)(_stagedReturnAddress + 1);
        // LOAD's CLK exports the RTS PC. On the next Tick, VICE resumes with
        // the no-clock JUMP, samples interrupts, and reaches the return
        // instruction's first FETCH CLK.
        _visiblePC = _instructionPC;
        _suppressBootstrapBoundary = true;
        _rtsPrefetchedByNotTakenBranch = false;
        _nonOverlappedRegion = false;
        _nonOverlappedFetchPhase = false;
    }

    /// <summary>
    /// Cycle-staged RTI (0x40; TR-CYCLE-001), mirroring VICE's invariant
    /// six-clock sequence: two FETCH clocks, STACK_PEEK, pull P, pull PCL, and
    /// pull PCH. Status application and JUMP occur after their preceding clock
    /// checkpoints and therefore do not create extra host samples.
    /// </summary>
    private bool TryExecuteCycleStagedRtiOpcode()
    {
        if (_opcode != 0x40)
        {
            return false;
        }

        switch (_cycle)
        {
            case 3:
                _ = Read((ushort)(0x0100 | S));
                return true;
            case 2:
                _fetched = Pop();
                return true;
            case 1:
                P = (byte)(_fetched | 0x20);
                _stagedReturnAddress = Pop();
                return true;
            case 0:
                _stagedReturnAddress |= (ushort)(Pop() << 8);
                _pc = _stagedReturnAddress;
                _visiblePC = _instructionPC;
                _suppressBootstrapBoundary = true;
                _nonOverlappedRegion = false;
                _nonOverlappedFetchPhase = true;
                return true;
            default:
                return false;
        }
    }

    private bool TryExecuteCycleStagedBranchOpcode()
    {
        if (!IsBranchOpcode(_opcode))
        {
            return false;
        }

        var fallThrough = (ushort)(_instructionPC + 2);

        if (IsBranchTaken(_opcode) && _holdTakenBranchOpcodePc && _cycle >= 1)
        {
            _visiblePC = _instructionPC;
            return true;
        }

        // Taken multi-cycle: VICE BRANCH does INC_PC then dummy LOAD+CLK_INC
        // (exports fall-through) before JUMP. Short taken (2 host steps) keeps
        // opcode PC on cycle 1 and only shows fall-through at cycle 0.
        if (IsBranchTaken(_opcode) && _takenBranchStagedFallthrough && _cycle >= 1)
        {
            if (_cycle >= 2)
            {
                var afterIncDummy = (ushort)(_opcodeAddress + 2);
                if (IsZeroPageIncrementDecrementOpcode(_previousOpcode)
                    && (_dummyTakenBneAfterStolenInc
                        || _visiblePC == afterIncDummy))
                {
                    // After stolen zp INC, this host tick is VICE BRANCH dummy
                    // (Wolf64 2093352 nPC=$F6A1). Do not reset PC back to the
                    // BNE opcode after FETCH already exported fall-through.
                    _pc = afterIncDummy;
                    _visiblePC = afterIncDummy;
                    _dummyTakenBneAfterStolenInc = false;
                    return true;
                }

                // Early host samples still at opcode (extra FETCH-shaped CLKs).
                _visiblePC = _instructionPC;
                return true;
            }

            // _cycle == 1: fall-through export (c=540201 nPC=$D92B).
            // After stolen zp INC, VICE already dummy-exported fall-through
            // on the previous host CLK; this CLK is JUMP (2093353 nPC=$F6A7).
            // Unstalled INC: FETCH was still the BNE opcode (2142574 nPC=$F69F);
            // this CLK is the dummy INC_PC (2142575 nPC=$F6A1).
            if (IsZeroPageIncrementDecrementOpcode(_previousOpcode))
            {
                var afterIncDummy = (ushort)(_opcodeAddress + 2);
                if (_visiblePC == afterIncDummy)
                {
                    var offset = (sbyte)Read((ushort)(_opcodeAddress + 1));
                    var jumpTarget = (ushort)(fallThrough + offset);
                    _pc = jumpTarget;
                    _visiblePC = jumpTarget;
                    _instructionPC = jumpTarget;
                    _cycle = 0;
                    _nonOverlappedRegion = false;
                    _nonOverlappedFetchPhase = false;
                    _suppressBootstrapBoundary = true;
                    return true;
                }

                _pc = afterIncDummy;
                _visiblePC = afterIncDummy;
                _suppressBootstrapBoundary = true;
                return true;
            }

            _pc = fallThrough;
            _visiblePC = fallThrough;
            _suppressBootstrapBoundary = true;
            return true;
        }

        if (_cycle != 0)
        {
            return false;
        }

        if (!IsBranchTaken(_opcode))
        {
            // VICE not-taken: last FETCH CLK still exports the opcode address;
            // INC_PC runs after that sample. Hold MUST win before RTS prefetch
            // (c=532304 nPC=$E906 mPC=$E908 when PrefetchOpcodeAt skipped hold).
            if (_notTakenBranchHoldFinalPc)
            {
                _pc = fallThrough;
                _visiblePC = _instructionPC;
                _suppressBootstrapBoundary = true;
                // Following zp INC needs one extra FETCH-shaped CLK at its opcode
                // before AdvanceVisiblePc (cycle 20756).
                _deferZpRmwPcAdvanceOne = true;
                // Fall-through insn also has a clean first FETCH (ROR A at 195424).
                _nonOverlappedFetchPhase = true;
                _nonOverlappedRegion = true;
                _notTakenBranchHoldFinalPc = false;
                return true;
            }

            if (Peek(fallThrough) == 0x60)
            {
                PrefetchOpcodeAt(fallThrough);
                _rtsPrefetchedByNotTakenBranch = true;
                return true;
            }

            // Not-taken without hold (e.g. priorTrailing>=3): VICE already
            // exported fall-through; clear sticky non-overlapped so following
            // SEC/implied is fused not soft-lagged (c=540323 nP=$A1 mP=$A0).
            if (_branchFetchPriorTrailing >= 3)
            {
                _nonOverlappedRegion = false;
                _nonOverlappedFetchPhase = false;
            }

            return false;
        }

        var target = (ushort)(fallThrough + (sbyte)Read((ushort)(_instructionPC + 1)));
        _pc = target;
        var skipJumpExtraTick = _stolenTakenBranchAfterAbsY;
        // Multi-cycle staged path already exported fall-through at cycle 1;
        // this CLK is VICE JUMP (c=540202 nPC=$D91D mPC=$D92B when still
        // fall-through). Short taken keeps fall-through visible here.
        // After STY abs whose STORE CLK already exported next-PC, VICE dummy
        // CLK_INC is this last CLK (fall-through); JUMP has no CLK
        // (Wolf64 2406995 nPC=$EAF0 mPC=$EB26). STA abs ($8D) still JUMPs
        // (KERNAL wait-loop BEQ at 2141796 nPC=$E5CD).
        // After LDA abs,Y stolen FETCH, cycle 0 is that JUMP with no CLK
        // (Wolf64 4139088 nPC=$A604 mPC=$A5B8).
        // Stolen BPL after INY: dummy already exported fall-through; VICE
        // JUMP has no CLK so this sample is the target (Wolf64 4150680
        // nPC=$A5F9 mPC=$A5FF).
        var samePageTaken = (fallThrough & 0xFF00) == (target & 0xFF00);
        // VICE immediate bodies execute their ALU/compare work after GET_IMM's
        // final clock. When that source phase leaves one exported next-PC
        // checkpoint, the staged branch dummy owns the current host sample and
        // the unclocked JUMP cannot expose target until the following FETCH.
        var predecessorOwnsJumpCheckpoint =
            _branchFetchPriorTrailing == 1
            && IsTwoByteImmediateOpcode(_previousOpcode);
        _deferredBranchJumpTargetFetch = predecessorOwnsJumpCheckpoint;
        if ((skipJumpExtraTick || _takenBranchStagedFallthrough)
            && samePageTaken
            && !predecessorOwnsJumpCheckpoint
            && _previousOpcode != 0x8C
            && _previousOpcode != 0xA5)
        {
            // Same-page taken BPL after LDA abs,Y: dummy already exported
            // fall-through; VICE JUMP has no CLK so this sample is the target
            // (Wolf64 4150680 stolen nPC=$A5F9; 4150690 unstolen nPC=$A5F9
            // mPC=$A5FF). Page-cross stolen B9 still FETCHes on the following
            // tick (4139089 $A604 to $A5B8). STY abs keeps fall-through.
            _visiblePC = target;
            _skipSoftImmAfterStagedTakenBranch = true;
            _skipImmediateLoadAfterAbsoluteStoreBranch =
                _branchFetchPriorTrailing == 2
                && IsAbsoluteUnindexedStoreOpcode(_previousOpcode);
            _loadAEarlyAfterStagedBranch = true;
            if (_previousOpcode == 0xB9)
                _fuseAbsYAfterStolenSamePageBranch = true;
        }
        else if (_takenBranchStagedFallthrough
            && !predecessorOwnsJumpCheckpoint
            && _previousOpcode != 0x8C
            && _previousOpcode != 0xA5
            && _previousOpcode != 0xB9)
        {
            _visiblePC = target;
            _skipSoftImmAfterStagedTakenBranch = true;
            _skipImmediateLoadAfterAbsoluteStoreBranch =
                _branchFetchPriorTrailing == 2
                && IsAbsoluteUnindexedStoreOpcode(_previousOpcode);
            _loadAEarlyAfterStagedBranch = true;
        }
        else
        {
            _visiblePC = fallThrough;
        }
        _stolenTakenBranchAfterAbsY = false;
        _fetchAfterStolenB9Jump = false;
        _holdTakenBranchOpcodePc = false;
        _suppressBootstrapBoundary = true;
        _baDelayedFetchClk = false;
        _delayNextFetch = false;
        if (skipJumpExtraTick)
            _fetchAfterStolenB9Jump = true;
        // VICE BRANCH JUMP has no CLK. DO_INTERRUPT is the next loop iteration
        // before FETCH of the target. Opening the IRQ *execute* on this extra
        // JUMP tick pushed PCH one CLK early (Wolf64 2175365 mS=$F2 nS=$F3).
        // Sample here despite suppress so an already-elapsed delay can arm
        // IRQ (Wolf64 2224633 nS=$F2 on the push cycle). Do not Execute the
        // first dummy on this tick; the next host tick is the arming dummy.
        _interruptSampleDespiteSuppress = true;
        if (!skipJumpExtraTick)
        {
            _sampleIrqBeforeNextFetch = true;
            _branchIrqArmingDummy = true;
        }
        var dummyAlreadyExportedFallthrough = _takenBranchStagedFallthrough;
        _takenBranchStagedFallthrough = false;
        if (skipJumpExtraTick)
        {
            // Stolen B9 dummy already exported fall-through. VICE JUMP has no
            // CLK and this BNE is same-page ($A604 to $A5B8). Do not arm the
            // page-cross extra tick: that left opcode $D0 while native FETCHed
            // $A5B8 (Wolf64 4139089). Next host tick FETCHes the target.
            _lastOpcodeDelaysInterrupt = true;
            _branchTargetFetchPending = true;
            _branchPageCrossExtraPending = false;
            ConsumedViceClockThisTick = false;
        }
        else if ((fallThrough & 0xFF00) != (target & 0xFF00))
        {
            // TR-LOCKSTEP-VSF-001: a taken branch across a page boundary costs
            // 4 native cycles (6510dtvcore.c BRANCH: the PBC fix-up cycle does
            // another dummy fetch and keeps exporting the un-fixed PC); consume
            // one extra tick before the target fetch.
            // Overlapped FETCH already spent that first CLK (Wolf64 2129144).
            // After STY abs the dummy CLK is this last CLK; JUMP has no extra
            // tick (Wolf64 2406996 nPC=$EB26 vs extra $EAF0).
            if (_previousOpcode == 0x8C)
            {
                _lastOpcodeDelaysInterrupt = false;
            }
            else if (_skipBranchPageCrossExtra || dummyAlreadyExportedFallthrough)
            {
                // Dummy CLK already exported fall-through on a prior host tick.
                // VICE extra CLK_INC is this JUMP tick (un-fixed PC); JUMP has
                // no further CLK, so the next sample FETCHes the target
                // (Wolf64 4150708 nPC=$A5B8). Collapsed dummy+JUMP still needs
                // the following extra CLK (4134649 nPC=$A604; 4136566 LDA #).
                _branchTargetFetchPending = true;
                _lastOpcodeDelaysInterrupt = false;
            }
            else
            {
                _branchPageCrossExtraPending = true;
                _lastOpcodeDelaysInterrupt = false;
            }
        }
        else
        {
            // VICE BRANCH same-page: OPCODE_DELAYS_INTERRUPT() so IRQ/NMI need
            // one extra cycle (mainviccpu.c interrupt_check_irq_delay).
            _lastOpcodeDelaysInterrupt = true;
            if (_previousOpcode == 0x8C)
            {
                // Dummy CLK already exported fall-through; JUMP has no extra
                // host tick (Wolf64 2406996 nPC=$EB26 vs tgtPend still $EAF0).
            }
            else
            {
                _branchTargetFetchPending = true;
                // The cycle-0 host checkpoint still contains BRANCH's
                // LOAD_DUMMY + CLK_INC before the unclocked JUMP. It therefore
                // counts for interrupt delay. Staged paths that exported the
                // dummy earlier are handled above and leave only JUMP here.
            }
        }

        _skipBranchPageCrossExtra = false;
        return true;
    }

    private static bool IsBranchOpcode(byte opcode)
    {
        return opcode is 0x10 or 0x30 or 0x50 or 0x70 or 0x90 or 0xB0 or 0xD0 or 0xF0;
    }

    /// <summary>
    /// Control-flow opcodes whose VICE bodies update PC with JUMP after their
    /// final CLK_INC. The following FETCH therefore starts a distinct phase.
    /// </summary>
    private static bool EndsWithUnclockedControlTransfer(byte opcode)
    {
        return IsBranchOpcode(opcode) || opcode is 0x00 or 0x20 or 0x40 or 0x4C or 0x60 or 0x6C;
    }

    private bool IsBranchTaken(byte opcode)
    {
        return opcode switch
        {
            0x10 => (P & 0x80) == 0,
            0x30 => (P & 0x80) != 0,
            0x50 => (P & 0x40) == 0,
            0x70 => (P & 0x40) != 0,
            0x90 => (P & 0x01) == 0,
            0xB0 => (P & 0x01) != 0,
            0xD0 => (P & 0x02) == 0,
            0xF0 => (P & 0x02) != 0,
            _ => false
        };
    }

    private void PrefetchOpcodeAt(ushort address)
    {
        _instructionPC = address;
        _visiblePC = address;
        _pc = address;
        _opcode = Read(_pc++);
        _cycle = Math.Max(0, GetCycleCount(_opcode) - 1);
        _stagedMemoryReadCompleted = false;
        _stagedLoadRegisterVisibleAtReadCheckpoint = false;
        _delayNextFetch = false;
        _sampleIrqBeforeNextFetch = false;
        _stagedNzUpdate = false;
        _stagedNzValue = 0;
        _stagedCarryUpdate = false;
        _stagedCarryValue = false;
        _callTargetFetchPending = false;
        _callTargetFetchNonOverlapped = false;

        _deferImpliedRegisterCompletionAfterBranch = false;
        _deferAbsoluteXLoadCompletionAfterBranch = false;
        _deferAbsoluteYLoadCompletionAfterBranch = false;
        _fuseAbsYAfterStolenSamePageBranch = false;
        _holdZpIncDecAfterStaAbsY = false;
        _deferJsrPushAfterBranch = false;
        _deferIndirectYLoadCompletionAfterBranch = false;
        _deferZeroPageRmwPcAdvanceAfterBranch = false;
        _deferNextIndirectYLoadAfterBranchRmw = false;
        _zpRmwPcDeferredFromSteal = false;
        _deferIndexedStorePcAdvanceAfterBranch = false;
        _deferZeroPageIndexedStorePcAdvanceAfterBranch = false;
        _zpRmwModifyCommitted = false;
        _impliedFlagLastClkExportedNextPc = false;
        _pendingPlaCompletion = false;
        _pendingPlpStatus = false;
        _bitSoftDeferEarly = false;
        _softDeferredBitLatched = false;
        _softDeferredBitValue = 0;
        _softDeferZpCompare = false;
        _softDeferJmpAbs = false;
        _pendingJmpTarget = 0;
        _targetInstructionFollowsSoftDeferredJmp = false;
        _takenBranchStagedFallthrough = false;
        _stolenTakenBranchAfterAbsY = false;
        _fetchAfterStolenB9Jump = false;
        _holdTakenBranchOpcodePc = false;
        _skipSoftImmAfterStagedTakenBranch = false;
        _skipImmediateLoadAfterAbsoluteStoreBranch = false;
        _deferredBranchJumpTargetFetch = false;
        _targetInstructionFollowsDeferredBranchJump = false;
        _nopHoldOpcodePcOnFinal = false;
        _nopChainHoldAfterBranch = false;
        _softDeferAfterNopChain = false;
        _indexedStorePcAdvanceWasDeferred = false;
        _indexedLoadPageCrossDelayConsumed = false;

        _softDeferredImmediateLoad = false;
        _jsrFollowsSoftDeferredImmediateLoad = false;
        _immediateLoadFollowsSoftDeferredBody = false;
        _immediateLoadCompletedWithoutDistinctFetch = false;
        _softDeferImpliedAfterHeldIndYStore = false;
        _softDeferCompareCommit = false;
        _pendingSoftCompareCommit = false;
        _softDeferredImpliedOp = false;
        _pendingDeferredImpliedRegisterCompletion = false;
        _rtsPrefetchedByNotTakenBranch = false;
        _stagedReturnAddress = 0;
        _effectiveAddress = 0;
        _fetched = 0;
    }

    private ushort ReadZeroPageOperand()
    {
        return Read((ushort)(_instructionPC + 1));
    }

    private static bool IsImmediateLoadOpcode(byte opcode)
    {
        return opcode is 0xA0 or 0xA2 or 0xA9;
    }

    /// <summary>
    /// 2-byte immediate ops whose VICE body runs INC_PC with no trailing CLK_INC
    /// after FETCH (LD/CP/ALU #imm). Used for post-NOP-chain soft-defer.
    /// </summary>
    private static bool IsTwoByteImmediateOpcode(byte opcode)
    {
        return IsImmediateLoadOpcode(opcode)
            || opcode is 0x29 or 0x09 or 0x49 or 0x69 or 0xE9 or 0xC9 or 0xE0 or 0xC0;
    }

    /// <summary>Unstaged zero-page compares (not CD/D1 staged path).</summary>
    private static bool IsZeroPageCompareOpcode(byte opcode)
    {
        return opcode is 0xC5 or 0xD5 or 0xE4 or 0xC4;
    }

    /// <summary>
    /// Unstaged absolute/indexed compares (ExecuteOpcode path, not CD/D1
    /// staged CompareStagedMemory). Soft-deferred after taken branch like zp
    /// compares so final CLK keeps pre-op P/PC.
    /// </summary>
    private static bool IsUnstagedAbsoluteCompareOpcode(byte opcode)
    {
        return opcode is 0xDD or 0xD9 or 0xEC or 0xCC;
    }

    /// <summary>
    /// Implied flag ops (CLC/SEC/CLI/SEI/CLV/CLD/SED), not register transfers
    /// like DEX/INX/TAX that leave a different host-visible load schedule.
    /// </summary>
    private static bool IsImpliedFlagOpcode(byte opcode)
    {
        return opcode is 0x18 or 0x38 or 0x58 or 0x78 or 0xB8 or 0xD8 or 0xF8;
    }

    /// <summary>
    /// Absolute ALU ops executed via unstaged ExecuteOpcode (not cycle-staged
    /// CMP abs / RMW). VICE GET_ABS + ORA/AND/... apply A after the data-read
    /// CLK export.
    /// </summary>
    private static bool IsAbsoluteAluOpcode(byte opcode)
    {
        return opcode is 0x0D or 0x2D or 0x4D or 0x6D or 0xED
            or 0x1D or 0x3D or 0x5D or 0x7D or 0xFD
            or 0x19 or 0x39 or 0x59 or 0x79 or 0xF9
            or 0x05 or 0x25 or 0x45 or 0x65 or 0xE5
            or 0x15 or 0x35 or 0x55 or 0x75 or 0xF5;
    }

    private static bool IsImpliedRegisterOrFlagOpcode(byte opcode)
    {
        return opcode is
            0x0A or // ASL A
            0x18 or // CLC
            0x2A or // ROL A
            0x38 or // SEC
            0x4A or // LSR A
            0x58 or // CLI
            0x6A or // ROR A
            0x78 or // SEI
            0x88 or // DEY
            0x8A or // TXA
            0x98 or // TYA
            0x9A or // TXS
            0xA8 or // TAY
            0xAA or // TAX
            0xB8 or // CLV
            0xBA or // TSX
            0xC8 or // INY
            0xCA or // DEX
            0xD8 or // CLD
            0xE8 or // INX
            0xF8;   // SED
    }

    private static bool IsAbsoluteXLoadOpcode(byte opcode)
    {
        return opcode is 0xBD;
    }

    private static bool IsAbsoluteYLoadOpcode(byte opcode)
    {
        return opcode is 0xB9;
    }

    private static bool IsIndirectYLoadOrCompareOpcode(byte opcode)
        => opcode is 0xB1 or 0xD1;

    private static bool IsIndexedAbsoluteStoreOpcode(byte opcode)
    {
        return opcode is 0x99 or 0x9D;
    }

    private static bool IsZeroPageIndexedStoreOpcode(byte opcode)
    {
        return opcode is 0x94 or 0x95 or 0x96;
    }

    private static bool IsZeroPageIncrementDecrementOpcode(byte opcode)
    {
        return opcode is 0xC6 or 0xE6;
    }

    private static bool IsIndirectYStoreOpcode(byte opcode)
    {
        return opcode is 0x91;
    }

    private static bool IsIndirectYLoadOpcode(byte opcode)
    {
        return opcode is 0xB1;
    }

    /// <summary>
    /// Compare opcodes whose read + flag commit are cycle-staged in
    /// <see cref="TryExecuteCycleStagedMemoryReadOpcode"/> (read at the native
    /// data-read cycle, flags/PC first visible one cycle later, matching the
    /// hosted x64sc per-cycle register export in c64cpusc.c CLK_INC).
    /// </summary>
    private static bool IsStagedCompareOpcode(byte opcode)
    {
        // CMP abs (0xCD) and CMP (zp),Y (0xD1) share the staged read + flag commit path.
        return opcode is 0xCD or 0xD1;
    }

    /// <summary>
    /// Opcodes that restore the one-cycle lag after a taken branch through a
    /// plain +1 cycle budget (TR-LOCKSTEP-VSF-001) because no dedicated
    /// after-branch defer path covers them. A taken branch costs 3 native
    /// cycles but resolves in 2 ticks here; each following instruction must
    /// absorb the missing cycle so its staged reads/writes land on the native
    /// access cycles and its commit on the native export cycle (the next
    /// instruction's first CLK_INC in the hosted c64cpusc.c core). Covers the
    /// staged compare and absolute-RMW families, control transfers (JMP abs /
    /// JMP ind and the branch family itself, so chained taken branches keep
    /// the 3-cycle cost), the staged zp/abs loads and stores, the staged stack
    /// pushes, the staged indexed loads LDY abs,X / LDX abs,Y, CMP (zp),Y and
    /// the 2-cycle immediate ALU family. Excluded: classes with a dedicated
    /// after-branch defer path (immediate loads A0/A2/A9, implied register
    /// ops, JSR, LDA abs,X / abs,Y / (zp),Y, zp INC/DEC, indexed stores),
    /// STX abs (0x8E, whose ShouldDeferAbsoluteStore hook already reroutes
    /// I/O stores to the unstaged path with correct after-branch timing), and
    /// the multi-cycle stack ops (RTS/RTI/PLA/PLP/BRK) whose staged offsets
    /// encode their own measured native timing.
    /// </summary>
    private static bool IsAfterBranchBudgetExtendedOpcode(byte opcode)
    {
        return IsStagedCompareOpcode(opcode)
            || IsStagedAbsoluteRmwOpcode(opcode)
            || IsBranchOpcode(opcode)
            || opcode is 0x4C or 0x6C or 0xBC or 0xBE or 0xD1
            || opcode is 0x29 or 0x09 or 0x49 or 0x69 or 0xE9 or 0xC9 or 0xE0 or 0xC0
            || opcode is 0x8D or 0x8C or 0x85 or 0x86 or 0x84
            || opcode is 0xA5 or 0xA6 or 0xA4 or 0xB5 or 0xB6 or 0xB4 or 0xAE or 0xAC
            || opcode is 0x48 or 0x08;
    }

    private void AdvanceVisiblePc(int instructionLength)
    {
        _pc = (ushort)(_instructionPC + instructionLength);
        _visiblePC = _pc;
        // Trailing dwell at next-PC is counted at end of Tick while
        // _visiblePC != _instructionPC (instructionPC stays at the opcode).
    }

    /// <summary>
    /// VICE 6510core.c INC/DEC: load, dummy RMW write, LOCAL_SET_NZ, then
    /// INC_PC; final STORE is the next CLK. Host-visible P must match nP on
    /// the same sample as the advanced PC (not lag to the final write).
    /// </summary>
    private void CommitZeroPageRmwModifyAndFlags()
    {
        var address = ReadZeroPageOperand();
        var current = Read(address);
        // 6502 RMW dummy write of the unmodified value (VICE DUMMY_STORE_ABS_RMW).
        _bus.Write(address, current);
        var result = _opcode == 0xE6 ? (byte)(current + 1) : (byte)(current - 1);
        _effectiveAddress = address;
        _fetched = result;
        UpdateNZ(result);
        _zpRmwModifyCommitted = true;
    }

    private static bool IsZeroPageShiftRmwOpcode(byte opcode)
    {
        // ASL/ROL/LSR/ROR zp and zp,X (not accumulator forms).
        return opcode is 0x06 or 0x16 or 0x26 or 0x36 or 0x46 or 0x56 or 0x66 or 0x76;
    }

    private static bool IsZeroPageXShiftRmwOpcode(byte opcode)
    {
        return opcode is 0x16 or 0x36 or 0x56 or 0x76;
    }

    private static bool IsLoadOpcode(byte opcode)
    {
        return IsImmediateLoadOpcode(opcode)
            || opcode is 0xA5 or 0xA6 or 0xA4 or 0xB5 or 0xB6 or 0xB4
            or 0xAD or 0xAE or 0xAC or 0xBD or 0xB9 or 0xBC or 0xBE
            or 0xA1 or 0xB1;
    }

    /// <summary>
    /// VICE ASL/LSR/ROL/ROR + SET_ZERO(_X)_RMW: SET_C/NZ and INC_PC before the
    /// final STORE host sample (6510dtvcore.c ASL macro order).
    /// </summary>
    private void CommitZeroPageShiftRmwModifyAndFlags()
    {
        var address = _opcode is 0x16 or 0x36 or 0x56 or 0x76
            ? (ushort)(byte)(ReadZeroPageOperand() + X)
            : ReadZeroPageOperand();
        var current = Read(address);
        _bus.Write(address, current); // RMW dummy write
        byte result = _opcode switch
        {
            0x06 or 0x16 => ApplyAsl(current),
            0x26 or 0x36 => ApplyRol(current),
            0x46 or 0x56 => ApplyLsr(current),
            _ => ApplyRor(current)
        };
        _bus.Write(address, result);
        _effectiveAddress = address;
        _fetched = result;
        _zpRmwModifyCommitted = true;
    }

    private byte ApplyAsl(byte value)
    {
        if ((value & 0x80) != 0)
            P |= 0x01;
        else
            P &= 0xFE;
        value = (byte)(value << 1);
        UpdateNZ(value);
        return value;
    }

    private byte ApplyLsr(byte value)
    {
        if ((value & 0x01) != 0)
            P |= 0x01;
        else
            P &= 0xFE;
        value = (byte)(value >> 1);
        UpdateNZ(value);
        return value;
    }

    private byte ApplyRol(byte value)
    {
        var carryIn = (byte)(P & 0x01);
        if ((value & 0x80) != 0)
            P |= 0x01;
        else
            P &= 0xFE;
        value = (byte)((value << 1) | carryIn);
        UpdateNZ(value);
        return value;
    }

    private byte ApplyRor(byte value)
    {
        var carryIn = (byte)((P & 0x01) << 7);
        if ((value & 0x01) != 0)
            P |= 0x01;
        else
            P &= 0xFE;
        value = (byte)((value >> 1) | carryIn);
        UpdateNZ(value);
        return value;
    }

    private void IncrementStagedMemory(ushort address, int instructionLength)
    {
        var value = (byte)(Read(address) + 1);
        _bus.Write(address, value);
        UpdateNZ(value);
        FinishStagedMemoryWrite(instructionLength);
    }

    private void DecrementStagedMemory(ushort address, int instructionLength)
    {
        var value = (byte)(Read(address) - 1);
        _bus.Write(address, value);
        UpdateNZ(value);
        FinishStagedMemoryWrite(instructionLength);
    }

    private void CompareStagedMemory(ushort address, int instructionLength)
    {
        var value = Read(address);
        _pc = (ushort)(_instructionPC + instructionLength);
        _visiblePC = _instructionPC;
        _stagedMemoryReadCompleted = true;
        _stagedCarryUpdate = true;
        _stagedCarryValue = A >= value;
        _stagedNzUpdate = true;
        _stagedNzValue = (byte)(A - value);
    }

    private void CompleteDeferredImmediateLoad()
    {
        var value = Read((ushort)(_instructionPC + 1));
        switch (_opcode)
        {
            case 0xA0:
                Y = value;
                break;
            case 0xA2:
                X = value;
                break;
            case 0xA9:
                A = value;
                break;
        }

        UpdateNZ(value);
        _pc = (ushort)(_instructionPC + 2);
        _visiblePC = _pc;

        _softDeferredImmediateLoad = false;
        // LD(GET_IMM) has no clock after FETCH_OPCODE's second CLK_INC.
        // The caller applies this body, performs VICE's DO_INTERRUPT check, and
        // continues into the following FETCH without inventing a body-only clock.
        _suppressBootstrapBoundary = true;
    }

    private void CompleteDeferredImpliedRegisterCompletion()
    {
        ExecuteOpcode(_opcode);
        _instructionPC = _pc;
        _visiblePC = _pc;
        _pendingDeferredImpliedRegisterCompletion = false;
    }


    private bool TryDeferImpliedRegisterCompletionAfterBranch()
    {
        if (!_deferImpliedRegisterCompletionAfterBranch || _cycle != 0)
            return false;

        _deferImpliedRegisterCompletionAfterBranch = false;
        _pendingDeferredImpliedRegisterCompletion = true;
        _visiblePC = _instructionPC;
        _suppressBootstrapBoundary = true;
        return true;
    }

    private void CompleteDeferredNzUpdateAfterBranch()
    {
        if (_stagedCarryUpdate)
        {
            if (_stagedCarryValue)
                P |= 0x01;
            else
                P &= 0xFE;

            _stagedCarryUpdate = false;
        }

        if (_stagedNzUpdate)
        {
            UpdateNZ(_stagedNzValue);
            _stagedNzUpdate = false;
        }

        // Full-length taken branch plus (zp),Y page-cross: VICE extra CLK is
        // GET_IND_Y (opcode PC). Wolf64 4131432 nPC=$E606. No page-cross:
        // this tick is the next FETCH (4131820 nPC=$E608). Short lag still
        // INC_PCs (KERNAL RAM fill c=5386 nPC=$FD70).
        if (_afterFullLengthTakenBranch && _indyPageCrossedThisInsn)
        {
            _visiblePC = _opcodeAddress;
            _suppressBootstrapBoundary = true;
        }
        else
        {
            // Extra tick is the next FETCH (no page-cross / short lag).
            // Leave the following CMP# overlapped so it fuses (Wolf64
            // 4131822 nPC=$E60A nP=$23).
            _nonOverlappedFetchPhase = false;
            _nonOverlappedRegion = false;
        }

        _pendingDeferredNzUpdateAfterBranch = false;
    }

    private ushort ReadAbsoluteOperand()
    {
        var lo = Read((ushort)(_instructionPC + 1));
        var hi = Read((ushort)(_instructionPC + 2));
        return (ushort)(lo | (hi << 8));
    }

    private bool TryDelayIndexedLoadPageCross(ushort baseAddress, byte index)
    {
        if (_indexedLoadPageCrossDelayConsumed)
        {
            _indexedLoadPageCrossDelayConsumed = false;
            return false;
        }

        var effectiveAddress = (ushort)(baseAddress + index);
        if ((baseAddress & 0xFF00) == (effectiveAddress & 0xFF00))
        {
            return false;
        }

        _indexedLoadPageCrossDelayConsumed = true;
        _cycle = 2;
        return true;
    }

    private ushort ReadIndirectYOperand()
    {
        var ptr = Read((ushort)(_instructionPC + 1));
        var lo = Read(ptr);
        var hi = Read((byte)(ptr + 1));
        return (ushort)((lo | (hi << 8)) + Y);
    }

    private void FinishStagedMemoryRead(
        int instructionLength,
        byte nzValue,
        bool registerVisibleAtReadCheckpoint = false)
    {
        _pc = (ushort)(_instructionPC + instructionLength);
        _visiblePC = _instructionPC;
        _stagedMemoryReadCompleted = true;
        _stagedLoadRegisterVisibleAtReadCheckpoint = registerVisibleAtReadCheckpoint;
        _stagedNzUpdate = true;
        _stagedNzValue = nzValue;
    }

    /// <summary>
    /// Absolute-addressed read-modify-write opcodes with a cycle-staged
    /// execution path (TR-LOCKSTEP-VSF-001). INC abs (0xEE), DEC abs (0xCE)
    /// and DEC abs,X (0xDE) - the classic $D019 acknowledge idioms (the RMW
    /// dummy write of the unmodified value performs the acknowledge, exactly
    /// as in VICE).
    /// </summary>
    private static bool IsStagedAbsoluteRmwOpcode(byte opcode)
    {
        return opcode is 0xEE or 0xCE or 0xDE;
    }

    /// <summary>
    /// One staged cycle of an absolute(,X) RMW opcode (TR-LOCKSTEP-VSF-001),
    /// mirroring VICE's INC/DEC abs and abs,X (6510dtvcore.c INC/DEC +
    /// SET_ABS_RMW / INT_ABS_RMW / INT_ABS_I_RMW): the abs,X form's un-fixed
    /// page dummy read on _cycle 4, the data read on _cycle 3, the 6502 RMW
    /// dummy write of the UNMODIFIED value on _cycle 2 - which is what
    /// acknowledges write-sensitive registers like $D019 - together with the
    /// PC advance and NZ flags becoming visible ("PC incremented before the
    /// first write access", 6510dtvcore.c), then the modified-value write on
    /// _cycle 1 with the staged-completed apply consuming the final lag cycle.
    /// </summary>
    private bool TryExecuteStagedAbsoluteRmwCycle()
    {
        switch (_cycle)
        {
            case 4 when _opcode == 0xDE:
                var baseAddress = ReadAbsoluteOperand();
                Read((ushort)((baseAddress & 0xFF00) | ((baseAddress + X) & 0xFF)));
                return true;
            case 3:
                _effectiveAddress = _opcode == 0xDE
                    ? (ushort)(ReadAbsoluteOperand() + X)
                    : ReadAbsoluteOperand();
                _fetched = Read(_effectiveAddress);
                return true;
            case 2:
                _bus.Write(_effectiveAddress, _fetched);
                _fetched = _opcode == 0xEE ? (byte)(_fetched + 1) : (byte)(_fetched - 1);
                UpdateNZ(_fetched);
                AdvanceVisiblePc(3);
                return true;
            case 1:
                _bus.Write(_effectiveAddress, _fetched);
                if (Peek(_pc) == 0x60)
                {
                    // Same RTS prefetch convention as FinishStagedMemoryWrite:
                    // RTS's staged offsets expect an un-lagged entry, so the
                    // idle apply tick is skipped when RTS follows.
                    _cycle = 0;
                    return true;
                }

                _stagedMemoryReadCompleted = true;
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Completes a staged stack push (PHA/PHP; TR-LOCKSTEP-VSF-001): the push
    /// itself just executed on this tick (native exports the decremented S at
    /// the push cycle, 6510dtvcore.c:1354-1366 PUSH + CLK_INC), while the PC
    /// advance (INC_PC after that CLK_INC) only becomes visible on the next
    /// cycle via the staged-completed apply path.
    /// </summary>
    private void FinishStagedStackPush()
    {
        _pc = (ushort)(_instructionPC + 1);
        _visiblePC = _instructionPC;
        _stagedMemoryReadCompleted = true;
    }

    private void FinishStagedMemoryWrite(int instructionLength)
    {
        _pc = (ushort)(_instructionPC + instructionLength);
        // Non-overlapped plain (non-indexed) stores: VICE ST runs INC_PC after
        // both FETCH CLKs, so the managed write sample (2nd of 3) still matches
        // F2 with opcode PC (c=519274 STA zp). Indexed stores (zp,X / abs,X)
        // already held through their extra dummy CLK; on the write sample VICE
        // has advanced (c=518815 STY zp,X nPC=next) so export next-PC here.
        // NonOvl plain store write-sample PC hold (VICE ST INC_PC after both
        // FETCH CLKs; write sample often still exports opcode PC):
        //  - trail==0: hold (c=519274 STA zp; c=522414 STY zp)
        //  - trail==1 after store: hold (c=522418 STA abs after STY)
        //  - trail==1 after taken-branch target: hold (c=553377 STX after BCS)
        //  - trail==1 after load: advance (c=557339 STA after LDA zp)
        //  - trail==1 after not-taken branch: advance (c=559296 STA after BNE)
        //  - trail>=2: advance (c=541162/541165)
        var storeFollowsUnconsumedImmediateFetch =
            IsImmediateLoadOpcode(_previousOpcode)
            && DebugPriorTrailingAtNextPc == 0;
        if ((_nonOverlappedRegion
                || _nonOverlappedFetchPhase
                || storeFollowsUnconsumedImmediateFetch)
            && IsStoreOpcode(_opcode)
            && _visiblePC == _opcodeAddress
            && !IsZeroPageIndexedStoreOpcode(_opcode)
            && !IsIndexedAbsoluteStoreOpcode(_opcode))
        {
            var trail = DebugPriorTrailingAtNextPc;
            var afterTakenBranch = _afterFullLengthTakenBranch || _afterShortTakenBranchLag;
            var aluImmPrevious = IsTwoByteImmediateOpcode(_previousOpcode)
                && !IsImmediateLoadOpcode(_previousOpcode);
            var afterJmp = _previousOpcode == 0x4C;
            // A normally completed JMP has already exposed its target fetch, so
            // the following ST can advance on its write checkpoint. A soft-held
            // JMP applies JUMP immediately before the target's distinct FETCH.
            // SET_ZERO has only one operand-fetch clock and therefore still
            // exposes opcode PC here; SET_ABS has crossed its second FETCH.
            var softDeferredJmpZeroPageTarget =
                _targetInstructionFollowsSoftDeferredJmp
                && instructionLength == 2;
            var advanceAfterJmp =
                afterJmp && !softDeferredJmpZeroPageTarget;
            // Advance when VICE ST INC_PC has already run before SET_ABS CLK:
            // load/not-taken-branch trail==1, ALU #imm, or an already-exposed
            // JMP target fetch (Wolf64 2060359 and 2060383).
            var advanceOnTrail1 = IsLoadOpcode(_previousOpcode)
                || (IsBranchOpcode(_previousOpcode) && !afterTakenBranch)
                || aluImmPrevious
                || advanceAfterJmp
                || _advanceStorePcAfterFusedImplied
                || IsAbsoluteAluOpcode(_previousOpcode);
            // STA zp STORE CLK after EOR # whose last CLK still showed the
            // EOR opcode (trail==0): VICE SET_ZERO CLK still exports STA
            // opcode PC (Wolf64 4132756 nPC=$E68C mPC=$E68E). ALU #imm still
            // advances STA abs at trail==0 (2060359).
            var aluImmAdvancesAbsStore = aluImmPrevious && _opcode != 0x85;
            // When a predecessor owns the control-transfer checkpoint, the
            // target FETCH begins on the next host clock. VICE GET_ZERO then
            // exports the target opcode address before INC_PC. Keep each source
            // phase scoped to the single latched target instruction.
            var deferredJumpTargetZeroPageStoreHolds =
                (_targetInstructionFollowsDeferredBranchJump
                    && instructionLength == 2)
                || softDeferredJmpZeroPageTarget;
            var holdOpcodePc = (trail == 0 && !aluImmAdvancesAbsStore && !advanceAfterJmp
                    && !_advanceStorePcAfterFusedImplied)
                || (trail == 1 && !advanceOnTrail1)
                || deferredJumpTargetZeroPageStoreHolds;
            if (holdOpcodePc)
            {
                _visiblePC = _opcodeAddress;
                _suppressBootstrapBoundary = true;
                _stagedMemoryReadCompleted = true;
                return;
            }
        }

        _visiblePC = _pc;
        // Lag-shaped shortcut: collapse into the next RTS early. Skip for the
        // whole non-overlapped subroutine region so STA keeps its full VICE
        // cycle budget before RTS (cycle 5048 stack lag).
        if (Peek(_pc) == 0x60 && !_nonOverlappedRegion)
        {
            _cycle = 0;
            return;
        }

        _stagedMemoryReadCompleted = true;
    }

    private void DelayNextFetchAfterMappedIoWrite(ushort address)
    {
        if (ShouldDelayNextFetchAfterWrite?.Invoke(address) != true)
        {
            return;
        }

        _delayNextFetch = true;
        _suppressBootstrapBoundary = true;
    }

    private void DelayNextFetchAfterDeferredIndexedStorePcAdvance()
    {
        if (!_indexedStorePcAdvanceWasDeferred)
        {
            return;
        }

        _indexedStorePcAdvanceWasDeferred = false;
        _delayNextFetch = true;
    }

    /// <summary>
    /// Snapshot-resume state injection (TR-LOCKSTEP-VSF-001): adopt a .vsf MAINCPU
    /// register file mid-run and restart execution with VICE x64sc resume semantics.
    /// Mirrors the hosted native bootstrap in native/vice/vice/src/mainc64cpu.c
    /// (maincpu_mainloop VICE_SHIM_HOSTED block): the register file is imported,
    /// execution JUMPs to the restored PC, and the per-run micro-op bookkeeping
    /// (opcode latch, staged/deferred completions, last_opcode_info equivalent) is
    /// cleared so the in-flight instruction RESTARTS from its first cycle. The
    /// one-cycle resume stagger matches this core's visible-commit convention: the
    /// managed pipeline runs one cycle behind the native per-cycle register export
    /// (hosted CLK_INC in c64cpusc.c exports the committed instruction during the
    /// NEXT instruction's first cycle), so the first tick after resume burns one
    /// cycle before the boundary fetch, exactly like <see cref="Reset"/> does.
    /// </summary>
    internal void InjectSnapshotResumeState(byte a, byte x, byte y, byte s, byte p, ushort pc)
    {
        A = a;
        X = x;
        Y = y;
        S = s;
        P = p;
        PC = pc;
        ResetInFlightState();
    }

    public void Reset()
    {
        _executedCycles = 0;
        A = 0;
        X = 0;
        Y = 0;
        S = 0x00;
        P = 0x26;
        PC = _bus.Read(0xFFFC);
        PC |= (ushort)(_bus.Read(0xFFFD) << 8);
        ResetInFlightState();
    }

    /// <summary>
    /// Clears the in-flight instruction micro-state and arms the one-cycle
    /// bootstrap stagger shared by <see cref="Reset"/> and
    /// <see cref="InjectSnapshotResumeState"/> (the native hosted bootstrap
    /// clears last_opcode_info/stolen_cycles/check_ba_low the same way before
    /// re-entering the fetch loop; mainc64cpu.c VICE_SHIM_HOSTED block).
    /// </summary>
    private void ResetInFlightState()
    {
        _opcode = 0;
        _cycle = 0;
        _baDelayedFetchClk = false;
        _holdAbsLoadOpcodeLastClk = false;
        _afterLdaAbsHold = false;
        _holdTakenBranchOpcodePc = false;
        _skipAbsLoadLastClkHold = false;
        _loadAEarlyAfterStagedBranch = false;
        _ldaSkippedLastClkHold = false;
        _suppressBootstrapBoundary = true;
        _interruptSampleDespiteSuppress = false;
        _bootstrapCycles = ResetCycleDelay;
        _stagedMemoryReadCompleted = false;
        _stagedLoadRegisterVisibleAtReadCheckpoint = false;
        _delayNextFetch = false;
        _sampleIrqBeforeNextFetch = false;
        _staAbsWriteCycleStolen = false;
        _ldaZpDataReadStolen = false;
        _staZpApplyStolen = false;
        _skipIrqSampleAtNextFetch = false;
        _branchIrqArmingDummy = false;
        _stagedNzUpdate = false;
        _stagedNzValue = 0;
        _stagedCarryUpdate = false;
        _stagedCarryValue = false;
        _branchTargetFetchPending = false;
        _branchPageCrossExtraPending = false;
        _trailingCyclesAtNextPc = 0;
        _currentInsnTrailingAtNextPc = 0;
        _fullLengthTakenBranchCompleted = false;
        _afterFullLengthTakenBranch = false;
        _afterShortTakenBranchLag = false;
        _fuseLdaZpAfterStolenInc = false;
        _fuseCmpZpAfterStolenIncLda = false;
        _overlapNextTakenBranchDummy = false;
        _skipBranchPageCrossExtra = false;
        _dummyTakenBneAfterStolenInc = false;
        _lastOpcodeDelaysInterrupt = false;
        _lastOpcodeEnablesIrq = false;
        _nonOverlappedFetchPhase = false;
        _nonOverlappedRegion = false;
        _deferZpRmwPcAdvanceOne = false;
        _nextJsrNonOverlapped = false;
        _inySoftAfterHeldInc = false;
        _notTakenBranchHoldFinalPc = false;
        _callTargetFetchPending = false;
        _callTargetFetchNonOverlapped = false;
        _fuseImpliedAfterIndyLoad = false;
        _advanceStorePcAfterFusedImplied = false;
        _fuseIndyLoadLastClkPc = false;
        _indyPageCrossedThisInsn = false;
        _fuseNonLoadImmAfterLoadBranch = false;
        _fusedNonLoadImmediateConsumedFetch = false;
        _branchFollowsFusedNonLoadImmediate = false;
        _skipSoftImmAfterStagedTakenBranch = false;
        _skipImmediateLoadAfterAbsoluteStoreBranch = false;
        _applySkipSoftImmThisInsn = false;

        _deferImpliedRegisterCompletionAfterBranch = false;
        _deferAbsoluteXLoadCompletionAfterBranch = false;
        _deferAbsoluteYLoadCompletionAfterBranch = false;
        _fuseAbsYAfterStolenSamePageBranch = false;
        _holdZpIncDecAfterStaAbsY = false;
        _deferJsrPushAfterBranch = false;
        _deferIndirectYLoadCompletionAfterBranch = false;
        _deferZeroPageRmwPcAdvanceAfterBranch = false;
        _deferNextIndirectYLoadAfterBranchRmw = false;
        _zpRmwPcDeferredFromSteal = false;
        _deferIndexedStorePcAdvanceAfterBranch = false;
        _deferZeroPageIndexedStorePcAdvanceAfterBranch = false;
        _zpRmwModifyCommitted = false;
        _impliedFlagLastClkExportedNextPc = false;
        _pendingPlaCompletion = false;
        _pendingPlpStatus = false;
        _bitSoftDeferEarly = false;
        _softDeferredBitLatched = false;
        _softDeferredBitValue = 0;
        _softDeferZpCompare = false;
        _softDeferJmpAbs = false;
        _pendingJmpTarget = 0;
        _targetInstructionFollowsSoftDeferredJmp = false;
        _takenBranchStagedFallthrough = false;
        _stolenTakenBranchAfterAbsY = false;
        _fetchAfterStolenB9Jump = false;
        _holdTakenBranchOpcodePc = false;
        _skipSoftImmAfterStagedTakenBranch = false;
        _skipImmediateLoadAfterAbsoluteStoreBranch = false;
        _deferredBranchJumpTargetFetch = false;
        _targetInstructionFollowsDeferredBranchJump = false;
        _nopHoldOpcodePcOnFinal = false;
        _nopChainHoldAfterBranch = false;
        _softDeferAfterNopChain = false;
        _indexedStorePcAdvanceWasDeferred = false;
        _indexedLoadPageCrossDelayConsumed = false;
        _pendingDeferredNzUpdateAfterBranch = false;

        _softDeferredImmediateLoad = false;
        _jsrFollowsSoftDeferredImmediateLoad = false;
        _immediateLoadFollowsSoftDeferredBody = false;
        _immediateLoadCompletedWithoutDistinctFetch = false;
        _softDeferImpliedAfterHeldIndYStore = false;
        _softDeferCompareCommit = false;
        _pendingSoftCompareCommit = false;
        _softDeferredImpliedOp = false;
        _pendingDeferredImpliedRegisterCompletion = false;
        _rtsPrefetchedByNotTakenBranch = false;
        _stagedReturnAddress = 0;
        _effectiveAddress = 0;
        _fetched = 0;
        _interruptSequenceRemaining = 0;
        _interruptReturnPc = 0;
        _interruptVector = 0;
    }

    public virtual byte Read(ushort address) => _bus.Read(address);
    public virtual void Write(ushort address, byte value) => _bus.Write(address, value);
    public byte Peek(ushort address) => _bus.Peek(address);

    private static bool IsReadSensitiveOpcode(byte opcode)
    {
        return opcode switch
        {
            0xA9 or 0xA5 or 0xB5 or 0xAD or 0xBD or 0xB9 or 0xA1 or 0xB1 or
            0xA2 or 0xA6 or 0xB6 or 0xAE or 0xBE or
            0xA0 or 0xA4 or 0xB4 or 0xAC or 0xBC or
            0x24 or 0x2C or
            0xC9 or 0xC5 or 0xD5 or 0xCD or 0xDD or 0xD9 or 0xC1 or 0xD1 or
            0xE0 or 0xE4 or 0xEC or
            0xC0 or 0xC4 or 0xCC or
            0x29 or 0x25 or 0x35 or 0x2D or 0x3D or 0x39 or 0x21 or 0x31 or
            0x09 or 0x05 or 0x15 or 0x0D or 0x1D or 0x19 or 0x01 or 0x11 or
            0x49 or 0x45 or 0x55 or 0x4D or 0x5D or 0x59 or 0x41 or 0x51 or
            0x69 or 0x65 or 0x75 or 0x6D or 0x7D or 0x79 or 0x61 or 0x71 or
            0xE9 or 0xE5 or 0xF5 or 0xED or 0xFD or 0xF9 or 0xE1 or 0xF1 or
            0xE6 or 0xF6 or 0xEE or 0xFE or
            0xC6 or 0xD6 or 0xCE or 0xDE or
            0x06 or 0x16 or 0x0E or 0x1E or
            0x46 or 0x56 or 0x4E or 0x5E or
            0x26 or 0x36 or 0x2E or 0x3E or
            0x66 or 0x76 or 0x6E or 0x7E or
            0xA7 or 0xB7 or 0xAF or 0xBF or 0xA3 or 0xB3 or 0xAB or
            0x10 or 0x30 or 0x50 or 0x70 or 0x90 or 0xB0 or 0xD0 or 0xF0 or
            0x20 or 0x60 or
            0x85 or 0x95 or 0x8D or 0x9D or 0x99 or 0x81 or 0x91 or
            0x86 or 0x96 or 0x8E or
            0x84 or 0x94 or 0x8C or
            0x87 or 0x97 or 0x8F or 0x83 => true,
            _ => false
        };
    }

    private static bool IsStoreOpcode(byte opcode)
    {
        return opcode switch
        {
            0x85 or 0x95 or 0x8D or 0x9D or 0x99 or 0x81 or 0x91 or
            0x86 or 0x96 or 0x8E or
            0x84 or 0x94 or 0x8C or
            0x87 or 0x97 or 0x8F or 0x83 => true,
            _ => false
        };
    }

    private static bool IsUnindexedStoreOpcode(byte opcode)
    {
        return opcode is
            0x84 or 0x85 or 0x86 or 0x87 or
            0x8C or 0x8D or 0x8E or 0x8F;
    }

    private static bool IsAbsoluteUnindexedStoreOpcode(byte opcode)
    {
        return opcode is 0x8C or 0x8D or 0x8E or 0x8F;
    }

    private static bool PriorInstructionConsumesFollowingFetchPhase(
        byte opcode,
        int trailingNextPcCheckpoints)
    {
        // VICE LD performs its register/flag updates and INC_PC after the
        // addressing-mode read CLK. Immediate ALU/compare bodies apply after
        // GET_IMM with no further CLK, so one exported next-PC checkpoint also
        // consumes the following fetch phase. ST with SET_ZERO/SET_ABS has only
        // its final write CLK after INC_PC. SET_ZERO_RMW adds dummy and final
        // write clocks. SET_IND_Y exposes pointer-low, pointer-high, dummy, and
        // store clocks after INC_PC; a fifth next-PC checkpoint means its fetch
        // phase was already consumed. Once those source clocks export enough
        // next-PC checkpoints, the following instruction body shares its last
        // FETCH.
        return (IsLoadOpcode(opcode)
                && trailingNextPcCheckpoints >= 1)
            || (IsTwoByteImmediateOpcode(opcode)
                && !IsImmediateLoadOpcode(opcode)
                && trailingNextPcCheckpoints >= 1)
            || (IsUnindexedStoreOpcode(opcode)
                && trailingNextPcCheckpoints >= 2)
            // SET_ABS_X/Y adds its indexed dummy and STORE clocks after the
            // operand fetches. Three exported next-PC checkpoints consume the
            // following FETCH phase before the next instruction body.
            || (IsIndexedAbsoluteStoreOpcode(opcode)
                && trailingNextPcCheckpoints >= 3)
            || (IsZeroPageShiftRmwOpcode(opcode)
                && trailingNextPcCheckpoints >= 3)
            || (IsIndirectYStoreOpcode(opcode)
                && trailingNextPcCheckpoints >= 5);
    }

    private static bool IsStackPushOpcode(byte opcode)
    {
        return opcode is 0x48 or 0x08;
    }

    private enum AddressingMode
    {
        Implied,
        Immediate,
        ZeroPage,
        ZeroPageX,
        ZeroPageY,
        Absolute,
        AbsoluteX,
        AbsoluteY,
        Indirect,
        IndirectX,
        IndirectY,
        Relative
    }

    private partial int GetCycleCount(byte opcode);
    private partial AddressingMode GetAddressingMode(byte opcode);
    private partial bool ExecuteAddressing(AddressingMode mode);
    private partial bool IsPageBoundaryCycleRequired(byte opcode);
    private partial void ExecuteOpcode(byte opcode);

    public bool HandlesAddress(ushort address) => false;
}
