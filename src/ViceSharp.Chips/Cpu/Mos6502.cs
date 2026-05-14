using ViceSharp.Abstractions;

namespace ViceSharp.Chips.Cpu;

public partial class Mos6502 : IClockedDevice, IAddressSpace, ICpu
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
    public bool IsInstructionBoundary => !_suppressBootstrapBoundary && _cycle == 0;

    public void Irq()
    {
        // IRQ implementation - push PC and P to stack, set I flag, jump to IRQ vector
        // Only if I flag is clear
        if ((P & 0x04) == 0)
        {
            PushWord(PC);
            Push((byte)(P & ~0x10)); // Push P with B flag clear
            P |= 0x04; // Set I flag
            PC = Read(0xFFFE);
            PC |= (ushort)(Read(0xFFFF) << 8);
        }
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

    private readonly IBus _bus;

    public Mos6502(IBus bus)
    {
        _bus = bus;
    }

    private byte _opcode;
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
    private bool _pendingDeferredNzUpdateAfterBranch;
    private bool _pendingDeferredImmediateLoad;
    private bool _pendingDeferredImpliedRegisterCompletion;
    private ushort _stagedReturnAddress;
    private ushort _effectiveAddress;
    private byte _fetched;

    public void Tick()
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
            _instructionPC = _pc;
            _visiblePC = _instructionPC;
            _opcode = Read(_pc++);
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
            var fetchingIndexedLoadControlTarget = fetchingBranchTarget || fetchingCallTarget;
            _deferAbsoluteXLoadCompletionAfterBranch = fetchingIndexedLoadControlTarget && IsAbsoluteXLoadOpcode(_opcode);
            _deferAbsoluteYLoadCompletionAfterBranch = fetchingIndexedLoadControlTarget && IsAbsoluteYLoadOpcode(_opcode);
            _deferIndirectYLoadCompletionAfterBranch = (fetchingBranchTarget && IsIndirectYLoadOpcode(_opcode))
                || deferIndirectYLoadAfterBranchRmw;
            _deferZeroPageRmwPcAdvanceAfterBranch = fetchingBranchTarget && IsZeroPageIncrementDecrementOpcode(_opcode);
            _deferIndexedStorePcAdvanceAfterBranch = fetchingBranchTarget && IsIndexedAbsoluteStoreOpcode(_opcode);
            _deferZeroPageIndexedStorePcAdvanceAfterBranch = fetchingBranchTarget && IsZeroPageIndexedStoreOpcode(_opcode);
            _pendingDeferredImmediateLoad = false;
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
                var lo = Read((ushort)(_instructionPC + 1));
                var hi = Read(_pc);
                PC = (ushort)(lo | (hi << 8));
                _callTargetFetchPending = true;
                _suppressBootstrapBoundary = true;
                return true;
            default:
                return false;
        }
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
                if (stxAddress == 0xD016)
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
                var absoluteXValue = Read((ushort)(ReadAbsoluteOperand() + X));
                if (!_deferAbsoluteXLoadCompletionAfterBranch)
                {
                    A = absoluteXValue;
                }

                FinishStagedMemoryRead(3, absoluteXValue);
                return true;
            case 0xB9:
                var absoluteYValue = Read((ushort)(ReadAbsoluteOperand() + Y));
                if (!_deferAbsoluteYLoadCompletionAfterBranch)
                {
                    A = absoluteYValue;
                }

                FinishStagedMemoryRead(3, absoluteYValue);
                return true;
            case 0xB1:
                var indirectYValue = Read(ReadIndirectYOperand());
                if (!_deferIndirectYLoadCompletionAfterBranch)
                {
                    A = indirectYValue;
                }

                FinishStagedMemoryRead(2, indirectYValue);
                return true;
            case 0xD1:
                CompareStagedMemory(ReadIndirectYOperand(), 2);
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
                _delayNextFetch = true;
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
        _branchTargetFetchPending = true;
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
        if (address != 0xD016)
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

    public void Reset()
    {
        A = 0;
        X = 0;
        Y = 0;
        S = 0x00;
        P = 0x26;
        PC = _bus.Read(0xFFFC);
        PC |= (ushort)(_bus.Read(0xFFFD) << 8);
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
        _pendingDeferredNzUpdateAfterBranch = false;
        _pendingDeferredImmediateLoad = false;
        _pendingDeferredImpliedRegisterCompletion = false;
        _stagedReturnAddress = 0;
        _effectiveAddress = 0;
        _fetched = 0;
    }

    public virtual byte Read(ushort address) => _bus.Read(address);
    public virtual void Write(ushort address, byte value) => _bus.Write(address, value);
    public byte Peek(ushort address) => _bus.Peek(address);
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
