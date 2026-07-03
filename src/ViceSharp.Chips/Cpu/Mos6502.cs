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
        get => IsInstructionBoundary ? _pc : _visiblePC;
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

    public bool IsInstructionBoundary => !_suppressBootstrapBoundary && _cycle == 0 && _interruptSequenceRemaining == 0;
    public int DebugCycle => _cycle;
    public byte DebugOpcode => _opcode;
    public bool DebugDelayNextFetch => _delayNextFetch;
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
    private bool _stagedMemoryReadCompleted;
    private bool _delayNextFetch;
    private bool _stagedNzUpdate;
    private byte _stagedNzValue;
    private bool _stagedCarryUpdate;
    private bool _stagedCarryValue;
    private bool _branchTargetFetchPending;
    private bool _branchPageCrossExtraPending;
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
    private bool _indexedStorePcAdvanceWasDeferred;
    private bool _indexedLoadPageCrossDelayConsumed;
    private bool _pendingDeferredNzUpdateAfterBranch;
    private bool _pendingDeferredImmediateLoad;
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
            CompleteDeferredNzUpdateAfterBranch();
            return;
        }

        if (_delayNextFetch)
        {
            _instructionPC = _pc;
            _visiblePC = _pc;
            _delayNextFetch = false;
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

            _instructionPC = _pc;
            _visiblePC = _instructionPC;
            _currentInstructionPc = _pc; // the instruction about to be fetched here

            // KERNAL serial-bus trap (VICE virtual device traps). If a trap fires
            // it has set PC to the routine's resume address; skip the trapped
            // instruction and re-fetch from there on the next cycle. The hook is
            // a no-op (returns false) unless a virtual disk is being addressed,
            // so cycle-accurate behaviour is unchanged in every other case.
            if (SerialTrapHook is not null && SerialTrapHook(_pc))
            {
                return;
            }

            _opcode = Read(_pc++);
            _instructionExecuted = true; // a real instruction has now been fetched/executed
            _cycle = GetCycleCount(_opcode);
            _stagedMemoryReadCompleted = false;
            _delayNextFetch = false;
            _stagedNzUpdate = false;
            _stagedNzValue = 0;
            _stagedCarryUpdate = false;
            _stagedCarryValue = false;
            var deferIndirectYLoadAfterBranchRmw = _deferNextIndirectYLoadAfterBranchRmw && IsIndirectYLoadOpcode(_opcode);
            _deferNextIndirectYLoadAfterBranchRmw = false;
            _deferImmediateLoadAfterBranch = fetchingBranchTarget && IsImmediateLoadOpcode(_opcode);
            _deferImpliedRegisterCompletionAfterBranch = fetchingBranchTarget && IsImpliedRegisterOrFlagOpcode(_opcode);
            _deferJsrPushAfterBranch = fetchingBranchTarget && _opcode == 0x20;
            var fetchingIndexedLoadControlTarget = fetchingBranchTarget;
            _deferAbsoluteXLoadCompletionAfterBranch = fetchingIndexedLoadControlTarget && IsAbsoluteXLoadOpcode(_opcode);
            _deferAbsoluteYLoadCompletionAfterBranch = fetchingIndexedLoadControlTarget && IsAbsoluteYLoadOpcode(_opcode);
            _deferIndirectYLoadCompletionAfterBranch = (fetchingBranchTarget && IsIndirectYLoadOpcode(_opcode))
                || deferIndirectYLoadAfterBranchRmw;
            _deferZeroPageRmwPcAdvanceAfterBranch = fetchingBranchTarget && IsZeroPageIncrementDecrementOpcode(_opcode);
            _deferIndexedStorePcAdvanceAfterBranch = fetchingBranchTarget && IsIndexedAbsoluteStoreOpcode(_opcode);
            _deferZeroPageIndexedStorePcAdvanceAfterBranch = fetchingBranchTarget && IsZeroPageIndexedStoreOpcode(_opcode);
            // TR-LOCKSTEP-VSF-001: a taken branch costs 3 native cycles
            // (6510dtvcore.c BRANCH: fetch, operand, dummy fetch + JUMP) but this
            // core resolves it in 2 ticks and re-establishes the one-cycle lag by
            // deferring the FOLLOWING instruction. For opcode classes without a
            // dedicated defer path the extension is a plain +1 cycle budget: a
            // staged compare's read at _cycle == 1 then lands on the native read
            // cycle (CMP abs C4) and its staged apply on the native commit-export
            // cycle (the next instruction's first CLK_INC in the hosted
            // c64cpusc.c core); an unstaged control transfer (JMP indirect)
            // commits on the native export cycle.
            if (fetchingBranchTarget && IsAfterBranchBudgetExtendedOpcode(_opcode))
            {
                _cycle++;
            }
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
            ExecuteOpcode(_opcode);
        }
    }

    private bool TryExecuteCycleStagedOpcode()
    {
        if (_opcode != 0x20)
        {
            return TryExecuteCycleStagedMemoryReadOpcode();
        }

        switch (_cycle)
        {
            case 3:
                if (_deferJsrPushAfterBranch)
                {
                    _deferJsrPushAfterBranch = false;
                    _cycle = 4;
                    _visiblePC = _instructionPC;
                    _suppressBootstrapBoundary = true;
                    return true;
                }

                _pc = (ushort)(_instructionPC + 2);
                _visiblePC = _pc;
                Push((byte)(_pc >> 8));
                return true;
            case 2:
                Push((byte)_pc);
                return true;
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
        var hi = Read(_pc);
        var target = (ushort)(lo | (hi << 8));
        var returnPc = (ushort)(source + 3);
        PC = target;
        PublishControlTransfer(source, target, returnPc, 0x20);
        _cycle = 0;
        _callTargetFetchPending = true;
        _suppressBootstrapBoundary = true;
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

            _instructionPC = _pc;
            _visiblePC = _pc;
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

            AdvanceVisiblePc(2);
            return true;
        }

        if (_cycle == 4 && IsIndirectYStoreOpcode(_opcode))
        {
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
                A = Read(ReadZeroPageOperand());
                FinishStagedMemoryRead(2, A);
                return true;
            case 0xA6:
                X = Read(ReadZeroPageOperand());
                FinishStagedMemoryRead(2, X);
                return true;
            case 0xA4:
                Y = Read(ReadZeroPageOperand());
                FinishStagedMemoryRead(2, Y);
                return true;
            case 0xB5:
                A = Read((byte)(ReadZeroPageOperand() + X));
                FinishStagedMemoryRead(2, A);
                return true;
            case 0xB6:
                X = Read((byte)(ReadZeroPageOperand() + Y));
                FinishStagedMemoryRead(2, X);
                return true;
            case 0xB4:
                Y = Read((byte)(ReadZeroPageOperand() + X));
                FinishStagedMemoryRead(2, Y);
                return true;
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
            case 0xAD:
                A = Read(ReadAbsoluteOperand());
                FinishStagedMemoryRead(3, A);
                return true;
            case 0xAE:
                X = Read(ReadAbsoluteOperand());
                FinishStagedMemoryRead(3, X);
                return true;
            case 0xAC:
                Y = Read(ReadAbsoluteOperand());
                FinishStagedMemoryRead(3, Y);
                return true;
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
                DecrementStagedMemory(ReadZeroPageOperand(), 2);
                return true;
            case 0xE6:
                IncrementStagedMemory(ReadZeroPageOperand(), 2);
                return true;
            case 0xBD:
                var absoluteXBase = ReadAbsoluteOperand();
                if (TryDelayIndexedLoadPageCross(absoluteXBase, X))
                {
                    return true;
                }

                var absoluteXValue = Read((ushort)(absoluteXBase + X));
                if (!_deferAbsoluteXLoadCompletionAfterBranch)
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
                if (!_deferAbsoluteYLoadCompletionAfterBranch)
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

                Y = Read((ushort)(ldyBase + X));
                FinishStagedMemoryRead(3, Y);
                return true;
            case 0xBE:
                var ldxBase = ReadAbsoluteOperand();
                if (TryDelayIndexedLoadPageCross(ldxBase, Y))
                {
                    return true;
                }

                X = Read((ushort)(ldxBase + Y));
                FinishStagedMemoryRead(3, X);
                return true;
            case 0xB1:
                var indirectYValue = Read(ReadIndirectYOperand());
                if (!_deferIndirectYLoadCompletionAfterBranch)
                {
                    A = indirectYValue;
                }

                FinishStagedMemoryRead(2, indirectYValue);
                return true;
            case 0xCD:
                CompareStagedMemory(ReadAbsoluteOperand(), 3);
                return true;
            case 0xD1:
                CompareStagedMemory(ReadIndirectYOperand(), 2);
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
                A = Pop();
                FinishStagedMemoryRead(1, A);
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

        switch (_cycle)
        {
            case 2:
                _stagedReturnAddress = Pop();
                return true;
            case 1:
                _stagedReturnAddress |= (ushort)(Pop() << 8);
                return true;
            case 0:
                _pc = (ushort)(_stagedReturnAddress + 1);
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
                return true;
            default:
                return false;
        }
    }

    private bool TryExecuteCycleStagedBranchOpcode()
    {
        if (_cycle != 0 || !IsBranchOpcode(_opcode))
        {
            return false;
        }

        var fallThrough = (ushort)(_instructionPC + 2);
        if (!IsBranchTaken(_opcode))
        {
            if (Peek(fallThrough) == 0x60)
            {
                PrefetchOpcodeAt(fallThrough);
                return true;
            }

            return false;
        }

        var target = (ushort)(fallThrough + (sbyte)Read((ushort)(_instructionPC + 1)));
        _pc = target;
        _visiblePC = fallThrough;
        _suppressBootstrapBoundary = true;
        if ((fallThrough & 0xFF00) != (target & 0xFF00))
        {
            // TR-LOCKSTEP-VSF-001: a taken branch across a page boundary costs
            // 4 native cycles (6510dtvcore.c BRANCH: the PBC fix-up cycle does
            // another dummy fetch and keeps exporting the un-fixed PC); consume
            // one extra tick before the target fetch.
            _branchPageCrossExtraPending = true;
        }
        else
        {
            _branchTargetFetchPending = true;
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
        _indexedStorePcAdvanceWasDeferred = false;
        _indexedLoadPageCrossDelayConsumed = false;
        _pendingDeferredImmediateLoad = false;
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

    private static bool IsImpliedRegisterOrFlagOpcode(byte opcode)
    {
        return opcode is
            0x18 or // CLC
            0x38 or // SEC
            0x58 or // CLI
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
        return opcode is 0xCD;
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
        _visiblePC = _pc;
        if (Peek(_pc) == 0x60)
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
        _bootstrapCycles = ResetCycleDelay;
        _stagedMemoryReadCompleted = false;
        _delayNextFetch = false;
        _stagedNzUpdate = false;
        _stagedNzValue = 0;
        _stagedCarryUpdate = false;
        _stagedCarryValue = false;
        _branchTargetFetchPending = false;
        _branchPageCrossExtraPending = false;
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
        _indexedStorePcAdvanceWasDeferred = false;
        _indexedLoadPageCrossDelayConsumed = false;
        _pendingDeferredNzUpdateAfterBranch = false;
        _pendingDeferredImmediateLoad = false;
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
