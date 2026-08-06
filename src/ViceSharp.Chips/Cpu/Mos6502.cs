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
    public int DebugCycle => _cycle;
    public byte DebugOpcode => _opcode;
    public bool DebugDelayNextFetch => _delayNextFetch;
    /// <summary>Trailing next-PC dwell captured at last instruction fetch (diagnostics).</summary>
    public int DebugPriorTrailingAtNextPc { get; private set; }
    public bool DebugFullLengthTakenBranch { get; private set; }
    public bool DebugNonOverlappedRegion => _nonOverlappedRegion;
    public bool DebugNonOverlappedFetchPhase => _nonOverlappedFetchPhase;
    public bool DebugStagedMemoryReadCompleted => _stagedMemoryReadCompleted;
    public int DebugInterruptSequenceRemaining => _interruptSequenceRemaining;
    public ushort DebugOpcodeAddress => _opcodeAddress;
    public bool CanStealCurrentCycle
    {
        get
        {
            if (_pendingDeferredNzUpdateAfterBranch ||
                _bootstrapCycles > 0 ||
                _pendingDeferredImmediateLoad ||
                _pendingDeferredImpliedRegisterCompletion ||
                _branchPageCrossExtraPending)
            {
                return false;
            }

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

            if (nextCycle == 1 && IsStoreOpcode(_opcode))
                return false;

            return IsReadSensitiveOpcode(_opcode);
        }
    }

    public bool CanForceStealCurrentCycle
    {
        get
        {
            if (_pendingDeferredNzUpdateAfterBranch ||
                _bootstrapCycles > 0 ||
                _pendingDeferredImmediateLoad ||
                _pendingDeferredImpliedRegisterCompletion ||
                _branchPageCrossExtraPending)
            {
                return false;
            }

            // TR-LOCKSTEP-VSF-001: same interrupt-sequence BA semantics as
            // CanStealCurrentCycle (reads stall, stack pushes proceed).
            if (_interruptSequenceRemaining > 0)
                return _interruptSequenceRemaining is 6 or 2 or 1;

            if (_cycle == 0)
                return _branchTargetFetchPending || _callTargetFetchPending;

            var nextCycle = _cycle - 1;
            return _opcode == 0x20 && nextCycle == 3;
        }
    }

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

        _interruptSequenceRemaining = 6;
        _interruptReturnPc = _pc;
        // The interrupted PC stays visible through the whole sequence (VICE
        // keeps exporting reg_pc until the JUMP after the vector fetch).
        _instructionPC = _interruptReturnPc;
        _visiblePC = _interruptReturnPc;
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
    private bool _delayNextFetch;
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
    /// VICE last_opcode_info DELAYS_INTERRUPT: set when a taken branch did not
    /// cross a page (6510dtvcore.c BRANCH else of PBC). Cleared at the next
    /// opcode fetch (SET_LAST_OPCODE of the following instruction).
    /// </summary>
    private bool _lastOpcodeDelaysInterrupt;
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
    private bool _callTargetFetchPending;
    private bool _deferImmediateLoadAfterBranch;
    private bool _deferImpliedRegisterCompletionAfterBranch;
    private bool _deferAbsoluteXLoadCompletionAfterBranch;
    private bool _deferAbsoluteYLoadCompletionAfterBranch;
    private bool _deferJsrPushAfterBranch;
    private bool _deferIndirectYLoadCompletionAfterBranch;
    private bool _deferZeroPageRmwPcAdvanceAfterBranch;
    private bool _deferNextIndirectYLoadAfterBranchRmw;
    private bool _deferIndexedStorePcAdvanceAfterBranch;
    private bool _deferZeroPageIndexedStorePcAdvanceAfterBranch;
    /// <summary>
    /// zp INC/DEC already applied NZ + modified value at the PC-advance CLK
    /// (VICE 6510core.c INC/DEC: LOCAL_SET_NZ + INC_PC before final STORE).
    /// Cycle 1 only performs the final write.
    /// </summary>
    private bool _zpRmwModifyCommitted;
    /// <summary>
    /// RTS first FETCH was overlapped with the previous instruction's last
    /// host-visible sample (VICE fuses post-body work with the next FETCH's
    /// first CLK_INC). Host-visible PULL schedule shifts one remaining-cycle
    /// earlier so S matches xvic mid-RTS (c=518540 after LDY #).
    /// </summary>
    private bool _rtsOverlappedFirstFetch;
    /// <summary>Opcode of the instruction that just finished (for RTS phase).</summary>
    private byte _previousOpcode;
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
    /// <summary>PLA after soft BIT: delay PULL one host CLK to match VICE STACK_PEEK.</summary>
    private bool _plaDeferPullOne;
    /// <summary>
    /// Soft-BIT phase: after the extra STACK_PEEK hold, perform PULL on the next
    /// host CLK with PC still at the opcode (VICE PLA: PULL + CLK_INC, then
    /// LOCAL_SET_NZ + INC_PC after that sample; c=522470 nPC=$EB18 mPC=$EB19).
    /// </summary>
    private bool _plaLatePullPending;
    /// <summary>
    /// RTI after soft-deferred body: one extra STACK_PEEK CLK before first PULL
    /// (c=522485 nS=$EB mS=$EC when managed pulled status early).
    /// </summary>
    private bool _rtiDeferFirstPullOne;
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
    private bool _applySkipSoftImmThisInsn;
    /// <summary>
    /// After fused LDA (zp),Y then fused INY/TAX chain in nonOvl, keep fusing
    /// implied ops until a non-implied is fetched (c=541175 INY, c=541177 TAX).
    /// </summary>
    private bool _fuseImpliedAfterIndyLoad;
    /// <summary>
    /// After fused LDA zp following a taken branch, the next not-taken branch's
    /// non-load imm fuses (c=541202 EOR#). Must not fire on plain BCC then CMP#
    /// (c=518756 nPC=opcode mPC advanced when over-fused).
    /// </summary>
    private bool _fuseNonLoadImmAfterLoadBranch;

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
    private bool _pendingDeferredImmediateLoad;
    /// <summary>
    /// Soft deferred immediate: apply A/X/Y at the start of the next instruction
    /// fetch without consuming an extra host-visible cycle (non-overlapped phase).
    /// </summary>
    private bool _softDeferredImmediateLoad;
    /// <summary>
    /// After non-overlapped LDA #, the following STA (zp),Y must keep the opcode
    /// PC for one extra host CLK before AdvanceVisiblePc (VICE: 2 FETCH at start,
    /// then INC_PC before INT_IND_Y_W body CLKs).
    /// </summary>
    private bool _holdIndYStorePcOneCycle;
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
        }
    }

    private void TickCore()
    {
        var fetchingBranchTarget = false;
        var fetchingCallTarget = false;
        if (_suppressBootstrapBoundary)
        {
            fetchingBranchTarget = _branchTargetFetchPending;
            fetchingCallTarget = _callTargetFetchPending;
            _branchTargetFetchPending = false;
            _callTargetFetchPending = false;
            _suppressBootstrapBoundary = false;
        }

        // One-tick post-JSR IRQ sample window ends before this tick's FETCH.
        _interruptSampleDespiteSuppress = false;

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
            return;
        }

        if (_bootstrapCycles > 0)
        {
            _bootstrapCycles--;
            _suppressBootstrapBoundary = true;
            return;
        }

        if (_pendingDeferredImmediateLoad)
        {
            CompleteDeferredImmediateLoad();
            // Pending-deferred imm ends on its own host tick (no fused FETCH).
            // VICE DO_INTERRUPT runs before the next insn FETCH — open the
            // post-tick SystemClock sample (NTSC c=520829 after branch+LDA#).
            // Soft-fused path still suppresses (c=522261).
            _suppressBootstrapBoundary = false;
            _interruptSampleDespiteSuppress = true;
            return;
        }

        if (_pendingDeferredImpliedRegisterCompletion)
        {
            CompleteDeferredImpliedRegisterCompletion();
            return;
        }

        if (_cycle == 0)
        {
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
            if (_pendingJmpTarget != 0 && !_softDeferJmpAbs)
            {
                _pc = _pendingJmpTarget;
                _pendingJmpTarget = 0;
                _nonOverlappedFetchPhase = true;
            }

            // Soft-deferred immediate from previous insn: commit A/X/Y while
            // _instructionPC / _opcode still name that previous immediate load.
            if (_softDeferredImmediateLoad)
            {
                CompleteDeferredImmediateLoad();
                _softDeferredImmediateLoad = false;
                // Keep _holdIndYStorePcOneCycle for the following STA (zp),Y.
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
                // Stack-pull ops after soft-deferred body (PLA after BIT/TAY,
                // RTI after PLA chain): VICE still on STACK_PEEK when managed
                // would pull early. Flags only consumed by PLA/RTI.
                _plaDeferPullOne = true;
                _rtiDeferFirstPullOne = true;
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

            // Preserve prior opcode before overwrite (RTS overlap phase uses it).
            _previousOpcode = _opcode;
            _opcode = Read(_pc++);
            // VICE SET_LAST_OPCODE of this insn replaces prior DELAYS_INTERRUPT
            // (only taken same-page BRANCH sets it again on JUMP).
            _lastOpcodeDelaysInterrupt = false;
            // One-instruction skip-soft-imm window: latch at fetch, always clear
            // the arm so staged cycle-0 paths (branch JUMP) cannot leave it sticky
            // for a later CMP# far from the staged JUMP (c=518756 over-fuse).
            _applySkipSoftImmThisInsn = _skipSoftImmAfterStagedTakenBranch
                && IsTwoByteImmediateOpcode(_opcode)
                && !IsImmediateLoadOpcode(_opcode);
            _skipSoftImmAfterStagedTakenBranch = false;
            // holdIndY is only for the instruction immediately after a soft-deferred
            // load/implied; clear if this fetch is not STA (zp),Y (c=519327 false hold).
            if (_opcode != 0x91)
                _holdIndYStorePcOneCycle = false;
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
            // after taken branch with trail 1-2 (c=541200 BCC). Keep hold when
            // trail==0 after load (c=518764 nPC=opcode mPC=fall-through).
            // Arm one-shot non-load-imm fuse only for the IMMEDIATELY following
            // insn after that BCC (c=541202 EOR#). Clear otherwise so a later
            // CMP# after a different BCC does not over-fuse (c=518756).
            // Also after CMP abs (c=559294 BNE nPC=fall-through mPC=opcode).
            var skipHoldAfterFusedLoad = (IsLoadOpcode(_previousOpcode)
                    || IsStagedCompareOpcode(_previousOpcode)
                    || IsUnstagedAbsoluteCompareOpcode(_previousOpcode))
                && priorTrailingAtNextPc is 1 or 2;
            if (_fuseNonLoadImmAfterLoadBranch
                && !(IsTwoByteImmediateOpcode(_opcode) && !IsImmediateLoadOpcode(_opcode)))
                _fuseNonLoadImmAfterLoadBranch = false;
            if (IsBranchOpcode(_opcode) && skipHoldAfterFusedLoad)
                _fuseNonLoadImmAfterLoadBranch = true;
            _notTakenBranchHoldFinalPc = IsBranchOpcode(_opcode) && cleanBranchFetch
                && priorTrailingAtNextPc < 3
                && !_fuseImpliedAfterIndyLoad
                && !skipHoldAfterFusedLoad;
            // Full-length taken branch when previous left 2 trailing dwells at next
            // PC (zp INC deferred), or when soft-deferred CMP just committed so
            // the first FETCH of this branch is not overlapped (cycle 5034).
            if (IsBranchOpcode(_opcode) && IsBranchTaken(_opcode) && cleanBranchFetch)
            {
                _cycle++;
                fullLengthTakenBranch = true;
            }

            DebugFullLengthTakenBranch = fullLengthTakenBranch;

            _stagedMemoryReadCompleted = false;
            _delayNextFetch = false;
            _stagedNzUpdate = false;
            _stagedNzValue = 0;
            _stagedCarryUpdate = false;
            _stagedCarryValue = false;
            _zpRmwModifyCommitted = false;
            _pendingPlpStatus = false;
            // BIT after RTS delayNextFetch: soft-apply one cycle earlier so the
            // host sample count matches VICE (c=522467 held late with cycle-0
            // soft). After branch / other trail, soft at cycle 0 (c=522392).
            _bitSoftDeferEarly = (_opcode is 0x2C or 0x24) && _fetchAfterRtsDelay;
            _fetchAfterRtsDelay = false;
            if (_opcode != 0x68)
            {
                _plaDeferPullOne = false;
                _plaLatePullPending = false;
            }

            if (_opcode != 0x40)
                _rtiDeferFirstPullOne = false;
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

            // VICE LD #imm (6510dtvcore LD after FETCH): INC_PC with no trailing
            // CLK_INC, so next opcode's first FETCH fuses into the first host
            // sample that already shows post-commit PC/Y. Managed LDY last tick
            // exports that PC without consuming RTS F1 (c=518540 nS ahead).
            // Absolute stores (STY abs at c=502759) and taken branches (c=30)
            // do not need early pull: their last CLK / JUMP coupling differs.
            // VICE RTS after a 2-byte immediate (LD#/AND#/CMP#/...): first PULL is
            // visible at dbgCyc=3 when prior trailing dwell is 1 (c=532024 nS ahead
            // of mS after AND # $29). Was limited to LDA/LDX/LDY # only.
            // trail==2 after taken branch or plain store: first FETCH overlaps so
            // VICE first PULL is visible at dbgCyc=3 (c=540341 after taken BCC;
            // c=559300 after STA following not-taken BNE). trail==2 after zp RMW
            // keeps STACK_PEEK (c=541125 after ROR zp: nS still pre-pull). Higher
            // trail after store keeps STACK_PEEK (c=5048 STA ind,Y trail=4).
            // trail==1 after taken BCC also keeps clean STACK_PEEK (c=541559).
            _rtsOverlappedFirstFetch = _opcode == 0x60
                && (((priorTrailingAtNextPc == 2)
                        && (_afterFullLengthTakenBranch
                            || IsStoreOpcode(_previousOpcode)))
                    || (priorTrailingAtNextPc == 1
                        && (IsTwoByteImmediateOpcode(_previousOpcode)
                            || IsLoadOpcode(_previousOpcode))
                        && !fetchingBranchTarget
                        && !afterSoftCompare
                        && !_nonOverlappedRegion
                        && !_nonOverlappedFetchPhase));

            var armAfterBranchLag = fetchingBranchTarget && !_afterFullLengthTakenBranch;
            _afterShortTakenBranchLag = armAfterBranchLag;
            _fullLengthTakenBranchCompleted = fullLengthTakenBranch;
            _deferImmediateLoadAfterBranch = armAfterBranchLag && IsImmediateLoadOpcode(_opcode);
            _deferImpliedRegisterCompletionAfterBranch = armAfterBranchLag && IsImpliedRegisterOrFlagOpcode(_opcode);
            // PLA after short taken branch: VICE still on STACK_PEEK when managed
            // would pull at cycle 1 (c=531993 nA=$00 mA=$18 after BEQ).
            if (armAfterBranchLag && _opcode == 0x68)
                _plaDeferPullOne = true;
            if (armAfterBranchLag && _opcode == 0x40)
                _rtiDeferFirstPullOne = true;
            // CMP zp family after taken-branch lag only (not after JSR trail>=2;
            // that regressed CPY at c=517861 where VICE already commits).
            // Short/full taken-branch targets soft-defer flags/PC (c=532264).
            // Also unstaged abs/abs,X/abs,Y compares (c=554366 CMP abs,X $DD
            // after taken BPL: nPC=opcode nP=pre-op mPC/mP already committed).
            _softDeferZpCompare = (IsZeroPageCompareOpcode(_opcode)
                    || IsUnstagedAbsoluteCompareOpcode(_opcode))
                && (armAfterBranchLag || _afterFullLengthTakenBranch);
            // JMP abs soft JUMP when first FETCH is clean non-overlapped (after
            // zp shift RMW c=539731, or soft-implied CLC c=539753). Not when
            // trail alone after STY (c=502749 nPC=target already).
            // After fused non-load imm trail==1 VICE already shows JUMP
            // (c=541205 nPC=$DC31 mPC=$DC98 when soft-held opcode).
            // After JSR trail>=2 VICE also shows JUMP (c=559254 nPC=$F734
            // mPC=$FFEA when soft-held after overlapped JSR).
            _softDeferJmpAbs = _opcode == 0x4C
                && (IsZeroPageShiftRmwOpcode(_previousOpcode)
                    || _nonOverlappedFetchPhase || _nonOverlappedRegion)
                && !(IsTwoByteImmediateOpcode(_previousOpcode)
                    && !IsImmediateLoadOpcode(_previousOpcode)
                    && priorTrailingAtNextPc == 1)
                && !(_previousOpcode == 0x20 && priorTrailingAtNextPc >= 2);
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
            _deferAbsoluteXLoadCompletionAfterBranch = fetchingIndexedLoadControlTarget && IsAbsoluteXLoadOpcode(_opcode);
            _deferAbsoluteYLoadCompletionAfterBranch = fetchingIndexedLoadControlTarget && IsAbsoluteYLoadOpcode(_opcode);
            // Delay A commit for (zp),Y after a full-length taken branch as well:
            // VICE still exports the pre-load A on the data-read CLK when the
            // first FETCH of this load was not overlapped (same host-visible
            // shape as the after-branch lag path without the extra cycle budget).
            _deferIndirectYLoadCompletionAfterBranch =
                ((armAfterBranchLag || _afterFullLengthTakenBranch) && IsIndirectYLoadOpcode(_opcode))
                || deferIndirectYLoadAfterBranchRmw;
            _deferZeroPageRmwPcAdvanceAfterBranch = armAfterBranchLag && IsZeroPageIncrementDecrementOpcode(_opcode);
            _deferIndexedStorePcAdvanceAfterBranch = armAfterBranchLag && IsIndexedAbsoluteStoreOpcode(_opcode);
            _deferZeroPageIndexedStorePcAdvanceAfterBranch = armAfterBranchLag && IsZeroPageIndexedStoreOpcode(_opcode);
            // Short taken branch: lag the following instruction by one CLK.
            if (armAfterBranchLag && IsAfterBranchBudgetExtendedOpcode(_opcode))
            {
                _cycle++;
            }

            // Stage fall-through at cycle 1 when:
            //  - budget 4+ (full-length + after-branch lag): c=540201
            //  - full-length with priorTrailing>=3 (c=540231)
            //  - full-length trail=2 after store (c=540337 BCC after STA zp)
            //  - full-length trail=1 after zp/ALU compare (c=541194 BNE after
            //    CPX zp nPC=fall-through mPC=opcode at dbgCyc=1)
            // Not: full-length trail=2 after INC (c=5005 keeps opcode at cyc 1)
            // Not: trail=0+nonOvl (c=5034) or lag alone (c=4997)
            _takenBranchStagedFallthrough = IsBranchOpcode(_opcode)
                && IsBranchTaken(_opcode)
                && fullLengthTakenBranch
                && (_cycle >= 4
                    || priorTrailingAtNextPc >= 3
                    || (priorTrailingAtNextPc == 2 && IsStoreOpcode(_previousOpcode))
                    || (priorTrailingAtNextPc == 1
                        && (IsZeroPageCompareOpcode(_previousOpcode)
                            || IsAbsoluteAluOpcode(_previousOpcode)
                            || IsImmediateLoadOpcode(_previousOpcode)
                            || IsTwoByteImmediateOpcode(_previousOpcode))));

            _pendingDeferredImmediateLoad = false;
            _indexedLoadPageCrossDelayConsumed = false;
            _stagedReturnAddress = 0;
            _effectiveAddress = 0;
            _fetched = 0;

        }

        _cycle--;

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
            // VICE 2-byte immediates (LD#/CMP#/AND#/...): GET_IMM + body + INC_PC
            // run after the second FETCH CLK is exported. Soft-defer the body so
            // host still samples pre-op regs/PC on that CLK when first-FETCH is
            // not overlapped (c=518756 CMP# in non-overlapped region after
            // not-taken BCC hold).
            // After staged multi-cycle taken branch JUMP, ALU imm is fused on
            // VICE (c=540204 ADC#). That skip is a ONE-instruction window only:
            // sticky skip fused CPX# much later (c=540896 nPC=$EB2D mPC=$EB2F).
            // Immediate loads with priorTrailing>=2 after plain (non-indexed)
            // store fuse (c=541168 LDY# after STY zp). After indexed store
            // trail=2 still softs (c=519253 LDX# after STA zp,X nX pre-load).
            // Non-load imm after not-taken branch trail==1 fuses only when the
            // branch followed a fused LDA-zp-after-taken-branch (c=541202 EOR#).
            // Plain BCC then CMP# still softs (c=518756).
            var skipSoftImmThisInsn = _applySkipSoftImmThisInsn;
            _applySkipSoftImmThisInsn = false;
            if ((_nonOverlappedFetchPhase || _nonOverlappedRegion)
                && IsTwoByteImmediateOpcode(_opcode))
            {
                var fuseImmLoadAfterPlainStore =
                    IsImmediateLoadOpcode(_opcode)
                    && DebugPriorTrailingAtNextPc >= 2
                    && IsStoreOpcode(_previousOpcode)
                    && !IsZeroPageIndexedStoreOpcode(_previousOpcode)
                    && !IsIndexedAbsoluteStoreOpcode(_previousOpcode);
                var fuseNonLoadImmAfterBranch =
                    !IsImmediateLoadOpcode(_opcode)
                    && _fuseNonLoadImmAfterLoadBranch
                    && IsBranchOpcode(_previousOpcode)
                    && DebugPriorTrailingAtNextPc == 1;
                // After fused ROL A / implied chain, LDA# fuses (c=541209
                // nA=$FF mA=$BF when soft-deferred with trail==0).
                var fuseImmLoadAfterImpliedChain =
                    IsImmediateLoadOpcode(_opcode) && _fuseImpliedAfterIndyLoad;
                // After LDA abs,X trail==1 in nonOvl, AND# fuses (c=559237
                // nA=$00 mA=$22 when soft-deferred).
                var fuseNonLoadImmAfterLoad =
                    !IsImmediateLoadOpcode(_opcode)
                    && IsLoadOpcode(_previousOpcode)
                    && DebugPriorTrailingAtNextPc == 1
                    && (_nonOverlappedFetchPhase || _nonOverlappedRegion);
                // After JMP abs trail==1, LDX# fuses (c=559256 nX=$00 mX=$ED).
                var fuseImmLoadAfterJmp =
                    IsImmediateLoadOpcode(_opcode)
                    && _previousOpcode == 0x4C
                    && DebugPriorTrailingAtNextPc == 1;
                if (skipSoftImmThisInsn
                    || fuseImmLoadAfterPlainStore
                    || fuseNonLoadImmAfterBranch
                    || fuseImmLoadAfterImpliedChain
                    || fuseNonLoadImmAfterLoad
                    || fuseImmLoadAfterJmp)
                {
                    if (fuseNonLoadImmAfterBranch)
                        _fuseNonLoadImmAfterLoadBranch = false;
                    // fall through to ExecuteOpcode (fused commit)
                }
                else
                {
                    // Soft path ends the load-branch fuse window.
                    _fuseNonLoadImmAfterLoadBranch = false;
                    if (IsImmediateLoadOpcode(_opcode))
                    {
                        _softDeferredImmediateLoad = true;
                        // _pc already past opcode; CompleteDeferredImmediateLoad will
                        // set it to instructionPC+2 when soft-applied at next fetch.
                    }
                    else
                    {
                        _softDeferredImpliedOp = true;
                        _softDeferredImpliedOpcode = _opcode;
                        _softDeferredImpliedInstructionPc = _opcodeAddress;
                    }

                    _holdIndYStorePcOneCycle = true;
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
            if ((_nonOverlappedFetchPhase || _nonOverlappedRegion)
                && IsAbsoluteAluOpcode(_opcode))
            {
                if (_previousOpcode == 0xB1 && DebugPriorTrailingAtNextPc == 1)
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
            if (IsImpliedRegisterOrFlagOpcode(_opcode)
                && ((_previousOpcode == 0xB1 && DebugPriorTrailingAtNextPc == 1)
                    || (_previousOpcode == 0x4C && DebugPriorTrailingAtNextPc == 1)
                    || ((_previousOpcode is 0x48 or 0x08)
                        && DebugPriorTrailingAtNextPc == 1)
                    || (_afterFullLengthTakenBranch
                        && DebugPriorTrailingAtNextPc >= 2)
                    || _fuseImpliedAfterIndyLoad))
            {
                _fuseImpliedAfterIndyLoad = true;
                // fall through to ExecuteOpcode (fused)
            }
            else if (IsImpliedRegisterOrFlagOpcode(_opcode)
                && ((_nonOverlappedFetchPhase || _nonOverlappedRegion)
                    || (IsZeroPageShiftRmwOpcode(_previousOpcode)
                        && DebugPriorTrailingAtNextPc == 2)))
            {
                // Soft after nonOvl or after zp shift trail==2 (c=543316 ROR A
                // nPC=opcode). trail==3 fuses (c=540980 nPC advanced mPC=opcode).
                _fuseImpliedAfterIndyLoad = false;
                _softDeferredImpliedOp = true;
                _softDeferredImpliedOpcode = _opcode;
                _softDeferredImpliedInstructionPc = _opcodeAddress;
                _holdIndYStorePcOneCycle = true;
                _visiblePC = _opcodeAddress;
                _suppressBootstrapBoundary = true;
                _nonOverlappedFetchPhase = true;
                return;
            }
            else if (!IsBranchOpcode(_opcode) && !IsAbsoluteAluOpcode(_opcode))
            {
                // End post-indy fuse chain. Keep flag through fused ALU and the
                // following branch so not-taken hold skips (c=541189 BMI after EOR).
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
            // after taken BNE trail=1 still softs (c=522392 nPC=opcode).
            // _bitSoftDeferEarly handles RTS-delay cycle-1 soft.
            if ((_opcode is 0x2C or 0x24) && !_bitSoftDeferEarly
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

            // PLA after soft BIT: late PULL on this CLK (A/S update, PC still
            // opcode). VICE then LOCAL_SET_NZ + INC_PC after the sample.
            if (_plaLatePullPending)
            {
                _plaLatePullPending = false;
                A = Pop();
                _pc = (ushort)(_instructionPC + 1);
                _visiblePC = _instructionPC;
                _suppressBootstrapBoundary = true;
                // NZ after this host sample (VICE LOCAL_SET_NZ post CLK_INC).
                _stagedNzUpdate = true;
                _stagedNzValue = A;
                _pendingSoftCompareCommit = true;
                // Following insn (TAY at c=522472) has a clean first FETCH: soft-defer
                // implied body so final CLK keeps pre-op Y/PC (VICE TAY + INC_PC).
                _nonOverlappedFetchPhase = true;
                // RTI after this PLA chain needs the same extra STACK_PEEK
                // (c=522485; flag is cleared if the next fetch is not RTI).
                _rtiDeferFirstPullOne = true;
                return;
            }

            _softDeferAfterNopChain = false;
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
        var nonOverlappedJsr = (_afterFullLengthTakenBranch
            || _nonOverlappedRegion
            || _nonOverlappedFetchPhase)
            && !(DebugPriorTrailingAtNextPc == 1
                && (IsTwoByteImmediateOpcode(_previousOpcode)
                    || _previousOpcode == 0x6C));
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
        var nonOverlappedJsr = (_afterFullLengthTakenBranch
            || _nonOverlappedRegion
            || _nonOverlappedFetchPhase)
            && !(DebugPriorTrailingAtNextPc == 1
                && (IsTwoByteImmediateOpcode(_previousOpcode)
                    || _previousOpcode == 0x6C));
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

        if (_stagedMemoryReadCompleted)
        {
            // PLP: S already advanced on the pull CLK; apply P + PC one sample later
            // (VICE 6510dtvcore.c PLP: PULL, CLK_INC, then LOCAL_SET_STATUS + INC_PC).
            if (_pendingPlpStatus)
            {
                // VICE LOCAL_SET_STATUS keeps P_BREAK from the pulled byte in
                // reg_p; LOCAL_STATUS ORs P_UNUSED. Host export therefore shows
                // B from the stack (c=518582 nP=$33 mP=$23 when B was cleared).
                P = (byte)(_fetched | 0x20);
                _pendingPlpStatus = false;
                _instructionPC = _pc;
                _visiblePC = _pc;
                _stagedMemoryReadCompleted = false;
                return true;
            }

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

                // Non-overlapped zp/abs/indexed loads: commit register on apply
                // tick (data-read CLK still showed the pre-load register).
                if (_nonOverlappedFetchPhase || _nonOverlappedRegion)
                {
                    switch (_opcode)
                    {
                        case 0xA5:
                        case 0xB5:
                        case 0xAD:
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
                            && (IsImmediateLoadOpcode(_previousOpcode)
                                || IsBranchOpcode(_previousOpcode))
                            && DebugPriorTrailingAtNextPc == 1)
                        || (_opcode is 0xA5
                            && ((IsBranchOpcode(_previousOpcode)
                                    && DebugPriorTrailingAtNextPc >= 1)
                                || (IsImpliedFlagOpcode(_previousOpcode)
                                    && DebugPriorTrailingAtNextPc == 0)
                                || (IsTwoByteImmediateOpcode(_previousOpcode)
                                    && !IsImmediateLoadOpcode(_previousOpcode)
                                    && DebugPriorTrailingAtNextPc == 1)))
                        || (_opcode is 0xBD
                            && IsImpliedRegisterOrFlagOpcode(_previousOpcode)
                            && DebugPriorTrailingAtNextPc == 0)
                        || (_opcode is 0xAD
                            && (_afterFullLengthTakenBranch
                                || _afterShortTakenBranchLag
                                || (IsBranchOpcode(_previousOpcode)
                                    && DebugPriorTrailingAtNextPc >= 1))));
                if ((_nonOverlappedFetchPhase || _nonOverlappedRegion)
                    && !loadFusedAfterPred
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

            var loadFusedPc = (
                (_opcode is 0xB1
                    && (IsImmediateLoadOpcode(_previousOpcode)
                        || IsBranchOpcode(_previousOpcode))
                    && DebugPriorTrailingAtNextPc == 1)
                || (_opcode is 0xA5
                    && ((IsBranchOpcode(_previousOpcode)
                            && DebugPriorTrailingAtNextPc >= 1)
                        || (IsImpliedFlagOpcode(_previousOpcode)
                            && DebugPriorTrailingAtNextPc == 0)
                        || (IsTwoByteImmediateOpcode(_previousOpcode)
                            && !IsImmediateLoadOpcode(_previousOpcode)
                            && DebugPriorTrailingAtNextPc == 1)))
                || (_opcode is 0xBD
                    && IsImpliedRegisterOrFlagOpcode(_previousOpcode)
                    && DebugPriorTrailingAtNextPc == 0)
                || (_opcode is 0xAD
                    && (_afterFullLengthTakenBranch
                        || _afterShortTakenBranchLag
                        || (IsBranchOpcode(_previousOpcode)
                            && DebugPriorTrailingAtNextPc >= 1))));
            if ((_nonOverlappedFetchPhase || _nonOverlappedRegion)
                && !loadFusedPc
                && _opcode is 0xB1 or 0xA5 or 0xA6 or 0xA4 or 0xB5 or 0xB6 or 0xB4
                    or 0xAD or 0xAE or 0xAC or 0xBD or 0xB9 or 0xBC or 0xBE)
            {
                // Keep opcode PC this tick; soft NZ/PC commit on next FETCH.
                _visiblePC = _opcodeAddress;
                _suppressBootstrapBoundary = true;
            }
            else
            {
                _instructionPC = _pc;
                _visiblePC = _pc;
                // NonOvl store apply must leave IsInstructionBoundary true so
                // SystemClock can arm IRQ on the same inter-instruction sample
                // VICE DO_INTERRUPT uses after ST (c=577682: native entered IRQ
                // with return PC at post-STA-zp $E5EC while managed suppress
                // skipped the sample and ran STA abs). PC export is already
                // next-PC above; do not suppress boundary here.
            }

            _stagedMemoryReadCompleted = false;
            return true;
        }

        if (_cycle == 0 && _deferImmediateLoadAfterBranch)
        {
            _deferImmediateLoadAfterBranch = false;
            _pendingDeferredImmediateLoad = true;
            _visiblePC = _instructionPC;
            _suppressBootstrapBoundary = true;
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

            // VICE INC/DEC zp (6510core.c): after load + dummy RMW write,
            // LOCAL_SET_NZ and INC_PC become visible before the final STORE CLK.
            // Absolute RMW already matches this; zp was advancing PC only and
            // lagging NZ until cycle 1 (diverge at c=517829: nP=$21 mP=$23).
            CommitZeroPageRmwModifyAndFlags();
            AdvanceVisiblePc(2);
            return true;
        }

        if (_cycle == 1 && IsZeroPageIncrementDecrementOpcode(_opcode) && _visiblePC == _opcodeAddress
            && !_stagedMemoryReadCompleted)
        {
            // Complete deferred Advance after the hold tick. VICE still pairs
            // LOCAL_SET_NZ with INC_PC on this same host sample.
            CommitZeroPageRmwModifyAndFlags();
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
            // Non-overlapped path after soft LDA #: hold opcode PC one more CLK
            // so host-visible matches VICE (2 FETCH at start before INC_PC).
            if (_holdIndYStorePcOneCycle)
            {
                _holdIndYStorePcOneCycle = false;
                _softDeferCompareCommit = true;
                _visiblePC = _opcodeAddress;
                return true;
            }

            AdvanceVisiblePc(2);
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
            return true;
        }

        // Between pointer-high (cycle 2) and data-read (cycle 1): VICE VIC
        // may refresh v_bus on the intervening phi2 (phi order VIA→VIC→CPU).
        // No CPU work here; VIC.Tick already ran before this CPU cycle.

        if (_cycle == 3 && IsIndirectYStoreOpcode(_opcode) && _visiblePC == _opcodeAddress)
        {
            // Complete the deferred advance after the hold tick.
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
                        && DebugPriorTrailingAtNextPc == 1);
                if (!(_nonOverlappedFetchPhase || _nonOverlappedRegion)
                    || fuseA5Early)
                    A = aZp;
                FinishStagedMemoryRead(2, aZp);
                return true;
            }
            case 0xA6:
            {
                var xZp = Read(ReadZeroPageOperand());
                // c=518761: non-overlapped LDX zp had mX early vs nX.
                if (!(_nonOverlappedFetchPhase || _nonOverlappedRegion))
                    X = xZp;
                FinishStagedMemoryRead(2, xZp);
                return true;
            }
            case 0xA4:
            {
                var yZp = Read(ReadZeroPageOperand());
                if (!(_nonOverlappedFetchPhase || _nonOverlappedRegion))
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
                // Non-overlapped LDA abs: pre-load A on data-read CLK (c=518803).
                // After taken branch trail>=1 VICE shows loaded A on data-read
                // (c=559287 nA=$FF mA=$B0 after BCC trail=2).
                if (!(_nonOverlappedFetchPhase || _nonOverlappedRegion)
                    || _afterFullLengthTakenBranch
                    || _afterShortTakenBranchLag
                    || (IsBranchOpcode(_previousOpcode)
                        && DebugPriorTrailingAtNextPc >= 1))
                    A = aAbs;
                FinishStagedMemoryRead(3, aAbs);
                return true;
            }
            case 0xAE:
            {
                var xAbs = Read(ReadAbsoluteOperand());
                if (!(_nonOverlappedFetchPhase || _nonOverlappedRegion))
                    X = xAbs;
                FinishStagedMemoryRead(3, xAbs);
                return true;
            }
            case 0xAC:
            {
                var yAbs = Read(ReadAbsoluteOperand());
                if (!(_nonOverlappedFetchPhase || _nonOverlappedRegion))
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
                    FinishStagedMemoryWrite(2);
                    return true;
                }

                // Fallback (e.g. after-branch defer returned false at cycle 2):
                // full RMW + NZ on this CLK so host still pairs flags with PC.
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
                // data-read (c=559234 nA=$22 mA=$0A).
                if (!_deferAbsoluteXLoadCompletionAfterBranch
                    && (!(_nonOverlappedFetchPhase || _nonOverlappedRegion)
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
                if (!_deferAbsoluteYLoadCompletionAfterBranch
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
                // BEQ nA=$3E mA=$9B when deferred).
                // Lag-shaped path: A lands on the data-read tick. Non-overlapped
                // callees defer A to the apply tick so pre-load A matches xvic.
                // After fused LDY# trail==1 or not-taken BEQ trail==1, VICE already
                // shows loaded A on data-read (c=541172 after LDY#; c=541183 after
                // BEQ nA=$3E mA=$9B when deferred).
                var indyCommitAOnDataRead = !_deferIndirectYLoadCompletionAfterBranch
                    && (!_nonOverlappedFetchPhase && !_nonOverlappedRegion
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
            case 0x68:
                // PLA (6510dtvcore.c:1368-1378): the PULL cycle exports the
                // incremented S and the pulled A; NZ and the PC advance become
                // visible on the next cycle via the staged apply.
                if (_plaDeferPullOne)
                {
                    // Hold one extra STACK_PEEK-shaped CLK after soft BIT.
                    // Real PULL runs on the following cycle-0 path (late pull)
                    // so host still samples opcode PC with new A/S (c=522470).
                    _plaDeferPullOne = false;
                    _plaLatePullPending = true;
                    _visiblePC = _opcodeAddress;
                    return true;
                }

                A = Pop();
                FinishStagedMemoryRead(1, A);
                return true;
            case 0x28:
                // PLP (6510dtvcore.c:1380-1396): PULL CLK exports S; status and
                // INC_PC apply after that sample. Was unstaged (Pop only at
                // cycle 0) so S lagged xvic at c=518581 (nS=$F9 mS=$F8).
                _fetched = Pop();
                _pc = (ushort)(_instructionPC + 1);
                _visiblePC = _instructionPC;
                _stagedMemoryReadCompleted = true;
                _pendingPlpStatus = true;
                return true;
            case 0x91:
                _bus.Write(ReadIndirectYOperand(), A);
                FinishStagedMemoryWrite(2);
                return true;
            default:
                return false;
        }
    }

    private bool TryExecuteCycleStagedRtsOpcode()
    {
        if (_opcode != 0x60)
        {
            return false;
        }

        // VICE 6510dtvcore.c RTS: STACK_PEEK, PULL, PULL, LOAD, JUMP after the
        // FETCH_OPCODE CLKs. Default pulls at cycle 2/1 match clean-fetch paths
        // (early-boot c=30, non-overlapped c=5048: first PULL not visible at
        // dbgCyc=3). Overlapped first FETCH (priorTrailing==1 after LDY # etc.)
        // means VICE already consumed one FETCH on the previous host sample, so
        // first PULL is visible at our dbgCyc=3 (c=518540 nS ahead of mS).
        switch (_cycle)
        {
            case 3 when _rtsOverlappedFirstFetch:
                _stagedReturnAddress = Pop();
                return true;
            case 2:
                if (_rtsOverlappedFirstFetch)
                {
                    _stagedReturnAddress |= (ushort)(Pop() << 8);
                    return true;
                }

                _stagedReturnAddress = Pop();
                return true;
            case 1:
                if (_rtsOverlappedFirstFetch)
                {
                    _ = Read(_stagedReturnAddress);
                    return true;
                }

                _stagedReturnAddress |= (ushort)(Pop() << 8);
                return true;
            case 0:
                _pc = (ushort)(_stagedReturnAddress + 1);
                // Leaving the non-overlapped callee region on RTS.
                _nonOverlappedRegion = false;
                _nonOverlappedFetchPhase = false;
                if (_rtsOverlappedFirstFetch)
                {
                    // Overlapped first-FETCH path is one host sample ahead of the
                    // clean RTS schedule: VICE JUMP is already visible on this
                    // CLK (c=518543 nPC=return mPC=RTS with delayNextFetch).
                    // Export return PC now and fetch the caller next tick.
                    _rtsOverlappedFirstFetch = false;
                    _visiblePC = _pc;
                    _instructionPC = _pc;
                    _suppressBootstrapBoundary = true;
                    return true;
                }

                _rtsOverlappedFirstFetch = false;
                _visiblePC = _instructionPC;
                _suppressBootstrapBoundary = true;
                if (Peek(_pc) == 0x60)
                {
                    // Same RTS prefetch convention as FinishStagedMemoryWrite:
                    // a following RTS expects an un-lagged entry, so skip the
                    // delayed-fetch tick and fetch it on the next cycle.
                    return true;
                }

                _delayNextFetch = true;
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Cycle-staged RTI (0x40; TR-LOCKSTEP-VSF-001), mirroring VICE's 6-cycle
    /// sequence (6510dtvcore.c RTI: fetch, dummy, stack peek, pull P, pull PCL,
    /// pull PCH): each pull cycle exports the incremented S; the pulled status
    /// becomes visible one cycle after its pull (assignment happens after that
    /// cycle's CLK_INC), and the return-address JUMP becomes visible on the
    /// final tick, exactly like the hosted per-cycle register export.
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
                if (_rtiDeferFirstPullOne)
                {
                    // Extra STACK_PEEK after soft body (c=522485).
                    _rtiDeferFirstPullOne = false;
                    _cycle = 4;
                    return true;
                }

                _fetched = Pop();
                return true;
            case 2:
                P = (byte)((_fetched & ~0x10) | (P & 0x10));
                _stagedReturnAddress = Pop();
                return true;
            case 1:
                _stagedReturnAddress |= (ushort)(Pop() << 8);
                return true;
            case 0:
                _pc = _stagedReturnAddress;
                _instructionPC = _pc;
                _visiblePC = _pc;
                // Leave non-overlapped region on RTI (same as RTS). Soft-deferred
                // LDA # after RTI was lagging VICE (c=522491 nA=$01 mA=$20).
                _nonOverlappedRegion = false;
                _nonOverlappedFetchPhase = false;
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

        // Taken multi-cycle: VICE BRANCH does INC_PC then dummy LOAD+CLK_INC
        // (exports fall-through) before JUMP. Short taken (2 host steps) keeps
        // opcode PC on cycle 1 and only shows fall-through at cycle 0.
        if (IsBranchTaken(_opcode) && _takenBranchStagedFallthrough && _cycle >= 1)
        {
            if (_cycle >= 2)
            {
                // Early host samples still at opcode (extra FETCH-shaped CLKs).
                _visiblePC = _instructionPC;
                return true;
            }

            // _cycle == 1: fall-through export (c=540201 nPC=$D92B).
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
        // Multi-cycle staged path already exported fall-through at cycle 1;
        // this CLK is VICE JUMP (c=540202 nPC=$D91D mPC=$D92B when still
        // fall-through). Short taken keeps fall-through visible here.
        if (_takenBranchStagedFallthrough)
        {
            _visiblePC = target;
            _skipSoftImmAfterStagedTakenBranch = true;
        }
        else
        {
            _visiblePC = fallThrough;
        }

        _suppressBootstrapBoundary = true;
        // VICE DO_INTERRUPT runs after BRANCH before the target FETCH. Keep
        // opcode-PC export via suppress, but open the IRQ sample (c=614627:
        // native entered IRQ with last_opcode=BEQ+DELAYS while managed ran LDA
        // at the target because suppress blocked the post-branch boundary).
        _interruptSampleDespiteSuppress = true;
        _takenBranchStagedFallthrough = false;
        if ((fallThrough & 0xFF00) != (target & 0xFF00))
        {
            // TR-LOCKSTEP-VSF-001: a taken branch across a page boundary costs
            // 4 native cycles (6510dtvcore.c BRANCH: the PBC fix-up cycle does
            // another dummy fetch and keeps exporting the un-fixed PC); consume
            // one extra tick before the target fetch.
            _branchPageCrossExtraPending = true;
            // PBC path does not set OPCODE_DELAYS_INTERRUPT (only the same-page
            // else branch of 6510dtvcore.c BRANCH does).
            _lastOpcodeDelaysInterrupt = false;
        }
        else
        {
            _branchTargetFetchPending = true;
            // VICE BRANCH same-page: OPCODE_DELAYS_INTERRUPT() so IRQ/NMI need
            // one extra cycle (mainviccpu.c interrupt_check_irq_delay).
            _lastOpcodeDelaysInterrupt = true;
        }

        return true;
    }

    private static bool IsBranchOpcode(byte opcode)
    {
        return opcode is 0x10 or 0x30 or 0x50 or 0x70 or 0x90 or 0xB0 or 0xD0 or 0xF0;
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
        _delayNextFetch = false;
        _stagedNzUpdate = false;
        _stagedNzValue = 0;
        _stagedCarryUpdate = false;
        _stagedCarryValue = false;
        _callTargetFetchPending = false;
        _deferImmediateLoadAfterBranch = false;
        _deferImpliedRegisterCompletionAfterBranch = false;
        _deferAbsoluteXLoadCompletionAfterBranch = false;
        _deferAbsoluteYLoadCompletionAfterBranch = false;
        _deferJsrPushAfterBranch = false;
        _deferIndirectYLoadCompletionAfterBranch = false;
        _deferZeroPageRmwPcAdvanceAfterBranch = false;
        _deferNextIndirectYLoadAfterBranchRmw = false;
        _deferIndexedStorePcAdvanceAfterBranch = false;
        _deferZeroPageIndexedStorePcAdvanceAfterBranch = false;
        _zpRmwModifyCommitted = false;
        _rtsOverlappedFirstFetch = false;
        _pendingPlpStatus = false;
        _bitSoftDeferEarly = false;
        _softDeferredBitLatched = false;
        _softDeferredBitValue = 0;
        _plaDeferPullOne = false;
        _plaLatePullPending = false;
        _rtiDeferFirstPullOne = false;
        _softDeferZpCompare = false;
        _softDeferJmpAbs = false;
        _pendingJmpTarget = 0;
        _takenBranchStagedFallthrough = false;
        _skipSoftImmAfterStagedTakenBranch = false;
        _nopHoldOpcodePcOnFinal = false;
        _nopChainHoldAfterBranch = false;
        _softDeferAfterNopChain = false;
        _indexedStorePcAdvanceWasDeferred = false;
        _indexedLoadPageCrossDelayConsumed = false;
        _pendingDeferredImmediateLoad = false;
        _softDeferredImmediateLoad = false;
        _holdIndYStorePcOneCycle = false;
        _softDeferCompareCommit = false;
        _pendingSoftCompareCommit = false;
        _softDeferredImpliedOp = false;
        _pendingDeferredImpliedRegisterCompletion = false;
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
            || opcode is 0xA5 or 0xA6 or 0xA4 or 0xB5 or 0xB6 or 0xB4 or 0xAD or 0xAE or 0xAC
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
        _pendingDeferredImmediateLoad = false;
        _softDeferredImmediateLoad = false;
        // After-branch deferred imm completes on the same host sample VICE uses
        // as the following opcode's first FETCH. Suppress IsInstructionBoundary
        // for the rest of this host step so SystemClock does not arm IRQ here
        // (c=522261 managed irq=6 while nPC continues into the next insn).
        // Cleared at the start of the next Tick before fetch; IRQ then waits
        // until that insn ends, matching VICE. Do not fall through to fetch
        // (that regressed focused 500k at c=4977).
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

    private void FinishStagedMemoryRead(int instructionLength, byte nzValue)
    {
        _pc = (ushort)(_instructionPC + instructionLength);
        _visiblePC = _instructionPC;
        _stagedMemoryReadCompleted = true;
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
        if ((_nonOverlappedRegion || _nonOverlappedFetchPhase)
            && IsStoreOpcode(_opcode)
            && _visiblePC == _opcodeAddress
            && !IsZeroPageIndexedStoreOpcode(_opcode)
            && !IsIndexedAbsoluteStoreOpcode(_opcode))
        {
            var trail = DebugPriorTrailingAtNextPc;
            var afterTakenBranch = _afterFullLengthTakenBranch || _afterShortTakenBranchLag;
            // Advance only when VICE has already INC_PC by the write sample:
            // load-then-store or not-taken-branch-then-store with trail==1.
            var advanceOnTrail1 = IsLoadOpcode(_previousOpcode)
                || (IsBranchOpcode(_previousOpcode) && !afterTakenBranch);
            var holdOpcodePc = trail == 0 || (trail == 1 && !advanceOnTrail1);
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
        _suppressBootstrapBoundary = true;
        _interruptSampleDespiteSuppress = false;
        _bootstrapCycles = ResetCycleDelay;
        _stagedMemoryReadCompleted = false;
        _delayNextFetch = false;
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
        _lastOpcodeDelaysInterrupt = false;
        _nonOverlappedFetchPhase = false;
        _nonOverlappedRegion = false;
        _deferZpRmwPcAdvanceOne = false;
        _nextJsrNonOverlapped = false;
        _notTakenBranchHoldFinalPc = false;
        _callTargetFetchPending = false;
        _fuseImpliedAfterIndyLoad = false;
        _fuseNonLoadImmAfterLoadBranch = false;
        _skipSoftImmAfterStagedTakenBranch = false;
        _applySkipSoftImmThisInsn = false;
        _deferImmediateLoadAfterBranch = false;
        _deferImpliedRegisterCompletionAfterBranch = false;
        _deferAbsoluteXLoadCompletionAfterBranch = false;
        _deferAbsoluteYLoadCompletionAfterBranch = false;
        _deferJsrPushAfterBranch = false;
        _deferIndirectYLoadCompletionAfterBranch = false;
        _deferZeroPageRmwPcAdvanceAfterBranch = false;
        _deferNextIndirectYLoadAfterBranchRmw = false;
        _deferIndexedStorePcAdvanceAfterBranch = false;
        _deferZeroPageIndexedStorePcAdvanceAfterBranch = false;
        _zpRmwModifyCommitted = false;
        _rtsOverlappedFirstFetch = false;
        _pendingPlpStatus = false;
        _bitSoftDeferEarly = false;
        _softDeferredBitLatched = false;
        _softDeferredBitValue = 0;
        _plaDeferPullOne = false;
        _plaLatePullPending = false;
        _rtiDeferFirstPullOne = false;
        _softDeferZpCompare = false;
        _softDeferJmpAbs = false;
        _pendingJmpTarget = 0;
        _takenBranchStagedFallthrough = false;
        _skipSoftImmAfterStagedTakenBranch = false;
        _nopHoldOpcodePcOnFinal = false;
        _nopChainHoldAfterBranch = false;
        _softDeferAfterNopChain = false;
        _indexedStorePcAdvanceWasDeferred = false;
        _indexedLoadPageCrossDelayConsumed = false;
        _pendingDeferredNzUpdateAfterBranch = false;
        _pendingDeferredImmediateLoad = false;
        _softDeferredImmediateLoad = false;
        _holdIndYStorePcOneCycle = false;
        _softDeferCompareCommit = false;
        _pendingSoftCompareCommit = false;
        _softDeferredImpliedOp = false;
        _pendingDeferredImpliedRegisterCompletion = false;
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
