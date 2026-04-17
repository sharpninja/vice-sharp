namespace ViceSharp.Chips.Cpu;

partial class Mos6502
{
    private static readonly byte[] OpCycleCounts = new byte[256]
    {
        /* 00 */ 7, 6, 2, 8, 3, 3, 5, 5, 3, 2, 2, 2, 4, 4, 6, 6,
        /* 10 */ 2, 5, 2, 8, 4, 4, 6, 6, 2, 4, 2, 7, 4, 4, 6, 7,
        /* 20 */ 6, 6, 2, 8, 3, 3, 5, 5, 4, 2, 2, 2, 4, 4, 6, 6,
        /* 30 */ 2, 5, 2, 8, 4, 4, 6, 6, 2, 4, 2, 7, 4, 4, 6, 7,
        /* 40 */ 6, 6, 2, 8, 3, 3, 5, 5, 3, 2, 2, 2, 3, 4, 6, 6,
        /* 50 */ 2, 5, 2, 8, 4, 4, 6, 6, 2, 4, 2, 7, 4, 4, 6, 7,
        /* 60 */ 6, 6, 2, 8, 3, 3, 5, 5, 4, 2, 2, 2, 5, 4, 6, 6,
        /* 70 */ 2, 5, 2, 8, 4, 4, 6, 6, 2, 4, 2, 7, 4, 4, 6, 7,
        /* 80 */ 2, 6, 2, 6, 3, 3, 3, 3, 2, 2, 2, 2, 4, 4, 4, 4,
        /* 90 */ 2, 6, 2, 6, 4, 4, 4, 4, 2, 5, 2, 5, 5, 5, 5, 5,
        /* A0 */ 2, 6, 2, 6, 3, 3, 3, 3, 2, 2, 2, 2, 4, 4, 4, 4,
        /* B0 */ 2, 5, 2, 5, 4, 4, 4, 4, 2, 4, 2, 4, 4, 4, 4, 4,
        /* C0 */ 2, 6, 2, 8, 3, 3, 5, 5, 2, 2, 2, 2, 4, 4, 6, 6,
        /* D0 */ 2, 5, 2, 8, 4, 4, 6, 6, 2, 4, 2, 7, 4, 4, 6, 7,
        /* E0 */ 2, 6, 2, 8, 3, 3, 5, 5, 2, 2, 2, 2, 4, 4, 6, 6,
        /* F0 */ 2, 5, 2, 8, 4, 4, 6, 6, 2, 4, 2, 7, 4, 4, 6, 7
    };

    private byte Fetch()
    {
        return _bus.Read(PC++);
    }

    private ushort FetchWord()
    {
        byte lo = Fetch();
        byte hi = Fetch();
        return (ushort)(lo | (hi << 8));
    }

    public int ExecuteInstruction()
    {
        byte opcode = Fetch();
        byte cycles = OpCycleCounts[opcode];

        ExecuteOpcode(opcode);

        return cycles;
    }

    private void ExecuteOpcode(byte opcode)
    {
        switch (opcode)
        {
            // LDA - Load Accumulator
            case 0xA9: A = _bus.Read(Immediate()); UpdateNZ(A); break;
            case 0xA5: A = _bus.Read(ZeroPage()); UpdateNZ(A); break;
            case 0xB5: A = _bus.Read(ZeroPageX()); UpdateNZ(A); break;
            case 0xAD: A = _bus.Read(Absolute()); UpdateNZ(A); break;
            case 0xBD: A = _bus.Read(AbsoluteX()); UpdateNZ(A); break;
            case 0xB9: A = _bus.Read(AbsoluteY()); UpdateNZ(A); break;
            case 0xA1: A = _bus.Read(IndirectX()); UpdateNZ(A); break;
            case 0xB1: A = _bus.Read(IndirectY()); UpdateNZ(A); break;

            // LDX - Load X Register
            case 0xA2: X = _bus.Read(Immediate()); UpdateNZ(X); break;
            case 0xA6: X = _bus.Read(ZeroPage()); UpdateNZ(X); break;
            case 0xB6: X = _bus.Read(ZeroPageY()); UpdateNZ(X); break;
            case 0xAE: X = _bus.Read(Absolute()); UpdateNZ(X); break;
            case 0xBE: X = _bus.Read(AbsoluteY()); UpdateNZ(X); break;

            // LDY - Load Y Register
            case 0xA0: Y = _bus.Read(Immediate()); UpdateNZ(Y); break;
            case 0xA4: Y = _bus.Read(ZeroPage()); UpdateNZ(Y); break;
            case 0xB4: Y = _bus.Read(ZeroPageX()); UpdateNZ(Y); break;
            case 0xAC: Y = _bus.Read(Absolute()); UpdateNZ(Y); break;
            case 0xBC: Y = _bus.Read(AbsoluteX()); UpdateNZ(Y); break;

            // NOP - No Operation
            case 0xEA: break;

            // BRK - Force Interrupt
            case 0x00:
                PushWord(PC);
                Push(P);
                P |= 0x04; // Set Interrupt Disable flag
                PC = _bus.Read(0xFFFE);
                PC |= (ushort)(_bus.Read(0xFFFF) << 8);
                break;

            // RTI - Return from Interrupt
            case 0x40:
                P = Pop();
                PC = PopWord();
                break;

            // RTS - Return from Subroutine
            case 0x60:
                PC = PopWord();
                PC++;
                break;

            // JMP - Jump
            case 0x4C: PC = Absolute(); break;
            case 0x6C: PC = Indirect(); break;

            // JSR - Jump to Subroutine
            case 0x20:
                ushort addr = Absolute();
                PushWord((ushort)(PC - 1));
                PC = addr;
                break;

            // STA - Store Accumulator
            case 0x85: _bus.Write(ZeroPage(), A); break;
            case 0x95: _bus.Write(ZeroPageX(), A); break;
            case 0x8D: _bus.Write(Absolute(), A); break;
            case 0x9D: _bus.Write(AbsoluteX(), A); break;
            case 0x99: _bus.Write(AbsoluteY(), A); break;
            case 0x81: _bus.Write(IndirectX(), A); break;
            case 0x91: _bus.Write(IndirectY(), A); break;

            // STX - Store X Register
            case 0x86: _bus.Write(ZeroPage(), X); break;
            case 0x96: _bus.Write(ZeroPageY(), X); break;
            case 0x8E: _bus.Write(Absolute(), X); break;

            // STY - Store Y Register
            case 0x84: _bus.Write(ZeroPage(), Y); break;
            case 0x94: _bus.Write(ZeroPageX(), Y); break;
            case 0x8C: _bus.Write(Absolute(), Y); break;

            // TAX - Transfer Accumulator to X
            case 0xAA: X = A; UpdateNZ(X); break;

            // TAY - Transfer Accumulator to Y
            case 0xA8: Y = A; UpdateNZ(Y); break;

            // TXA - Transfer X to Accumulator
            case 0x8A: A = X; UpdateNZ(A); break;

            // TYA - Transfer Y to Accumulator
            case 0x98: A = Y; UpdateNZ(A); break;

            // TXS - Transfer X to Stack Pointer
            case 0x9A: S = X; break;

            // TSX - Transfer Stack Pointer to X
            case 0xBA: X = S; UpdateNZ(X); break;

            // INX - Increment X Register
            case 0xE8: X++; UpdateNZ(X); break;

            // INY - Increment Y Register
            case 0xC8: Y++; UpdateNZ(Y); break;

            // DEX - Decrement X Register
            case 0xCA: X--; UpdateNZ(X); break;

            // DEY - Decrement Y Register
            case 0x88: Y--; UpdateNZ(Y); break;

            // CLC - Clear Carry Flag
            case 0x18: P &= 0xFE; break;

            // SEC - Set Carry Flag
            case 0x38: P |= 0x01; break;

            // CLI - Clear Interrupt Disable
            case 0x58: P &= 0xFB; break;

            // SEI - Set Interrupt Disable
            case 0x78: P |= 0x04; break;

            // CLV - Clear Overflow Flag
            case 0xB8: P &= 0xBF; break;

            // SED - Set Decimal Flag
            case 0xF8: P |= 0x08; break;

            // CLD - Clear Decimal Flag
            case 0xD8: P &= 0xF7; break;

            // PHA - Push Accumulator
            case 0x48: Push(A); break;

            // PHP - Push Processor Status
            case 0x08: Push((byte)(P | 0x10)); break;

            // PLA - Pull Accumulator
            case 0x68: A = Pop(); UpdateNZ(A); break;

            // PLP - Pull Processor Status
            case 0x28: P = (byte)((Pop() & ~0x10) | (P & 0x10)); break;

            // BIT - Bit Test
            case 0x24:
                byte value = _bus.Read(ZeroPage());
                P &= 0x3D;
                P |= (byte)(value & 0xC0);
                if ((A & value) == 0) P |= 0x02;
                break;
            case 0x2C:
                value = _bus.Read(Absolute());
                P &= 0x3D;
                P |= (byte)(value & 0xC0);
                if ((A & value) == 0) P |= 0x02;
                break;

            // NOP undocumented variants
            case 0x1A:
            case 0x3A:
            case 0x5A:
            case 0x7A:
            case 0xDA:
            case 0xFA:
                break;

            // INC - Increment Memory
            case 0xE6:
                value = _bus.Read(ZeroPage());
                value++;
                _bus.Write(ZeroPage(), value);
                UpdateNZ(value);
                break;
            case 0xF6:
                value = _bus.Read(ZeroPageX());
                value++;
                _bus.Write(ZeroPageX(), value);
                UpdateNZ(value);
                break;
            case 0xEE:
                value = _bus.Read(Absolute());
                value++;
                _bus.Write(Absolute(), value);
                UpdateNZ(value);
                break;
            case 0xFE:
                value = _bus.Read(AbsoluteX());
                value++;
                _bus.Write(AbsoluteX(), value);
                UpdateNZ(value);
                break;

            // DEC - Decrement Memory
            case 0xC6:
                value = _bus.Read(ZeroPage());
                value--;
                _bus.Write(ZeroPage(), value);
                UpdateNZ(value);
                break;
            case 0xD6:
                value = _bus.Read(ZeroPageX());
                value--;
                _bus.Write(ZeroPageX(), value);
                UpdateNZ(value);
                break;
            case 0xCE:
                value = _bus.Read(Absolute());
                value--;
                _bus.Write(Absolute(), value);
                UpdateNZ(value);
                break;
            case 0xDE:
                value = _bus.Read(AbsoluteX());
                value--;
                _bus.Write(AbsoluteX(), value);
                UpdateNZ(value);
                break;

            // CMP - Compare Accumulator
            case 0xC9: Compare(A, Immediate()); break;
            case 0xC5: Compare(A, ZeroPage()); break;
            case 0xD5: Compare(A, ZeroPageX()); break;
            case 0xCD: Compare(A, Absolute()); break;
            case 0xDD: Compare(A, AbsoluteX()); break;
            case 0xD9: Compare(A, AbsoluteY()); break;
            case 0xC1: Compare(A, IndirectX()); break;
            case 0xD1: Compare(A, IndirectY()); break;

            // CPX - Compare X Register
            case 0xE0: Compare(X, Immediate()); break;
            case 0xE4: Compare(X, ZeroPage()); break;
            case 0xEC: Compare(X, Absolute()); break;

            // CPY - Compare Y Register
            case 0xC0: Compare(Y, Immediate()); break;
            case 0xC4: Compare(Y, ZeroPage()); break;
            case 0xCC: Compare(Y, Absolute()); break;

            // Branch Instructions
            case 0x10: if ((P & 0x80) == 0) BranchRelative(); break; // BPL
            case 0x30: if ((P & 0x80) != 0) BranchRelative(); break; // BMI
            case 0x50: if ((P & 0x40) == 0) BranchRelative(); break; // BVC
            case 0x70: if ((P & 0x40) != 0) BranchRelative(); break; // BVS
            case 0x90: if ((P & 0x01) == 0) BranchRelative(); break; // BCC
            case 0xB0: if ((P & 0x01) != 0) BranchRelative(); break; // BCS
            case 0xD0: if ((P & 0x02) == 0) BranchRelative(); break; // BNE
            case 0xF0: if ((P & 0x02) != 0) BranchRelative(); break; // BEQ

            // Logical AND
            case 0x29: A &= _bus.Read(Immediate()); UpdateNZ(A); break;
            case 0x25: A &= _bus.Read(ZeroPage()); UpdateNZ(A); break;
            case 0x35: A &= _bus.Read(ZeroPageX()); UpdateNZ(A); break;
            case 0x2D: A &= _bus.Read(Absolute()); UpdateNZ(A); break;
            case 0x3D: A &= _bus.Read(AbsoluteX()); UpdateNZ(A); break;
            case 0x39: A &= _bus.Read(AbsoluteY()); UpdateNZ(A); break;
            case 0x21: A &= _bus.Read(IndirectX()); UpdateNZ(A); break;
            case 0x31: A &= _bus.Read(IndirectY()); UpdateNZ(A); break;

            // Logical OR
            case 0x09: A |= _bus.Read(Immediate()); UpdateNZ(A); break;
            case 0x05: A |= _bus.Read(ZeroPage()); UpdateNZ(A); break;
            case 0x15: A |= _bus.Read(ZeroPageX()); UpdateNZ(A); break;
            case 0x0D: A |= _bus.Read(Absolute()); UpdateNZ(A); break;
            case 0x1D: A |= _bus.Read(AbsoluteX()); UpdateNZ(A); break;
            case 0x19: A |= _bus.Read(AbsoluteY()); UpdateNZ(A); break;
            case 0x01: A |= _bus.Read(IndirectX()); UpdateNZ(A); break;
            case 0x11: A |= _bus.Read(IndirectY()); UpdateNZ(A); break;

            // Logical Exclusive OR
            case 0x49: A ^= _bus.Read(Immediate()); UpdateNZ(A); break;
            case 0x45: A ^= _bus.Read(ZeroPage()); UpdateNZ(A); break;
            case 0x55: A ^= _bus.Read(ZeroPageX()); UpdateNZ(A); break;
            case 0x4D: A ^= _bus.Read(Absolute()); UpdateNZ(A); break;
            case 0x5D: A ^= _bus.Read(AbsoluteX()); UpdateNZ(A); break;
            case 0x59: A ^= _bus.Read(AbsoluteY()); UpdateNZ(A); break;
            case 0x41: A ^= _bus.Read(IndirectX()); UpdateNZ(A); break;
            case 0x51: A ^= _bus.Read(IndirectY()); UpdateNZ(A); break;

            // Arithmetic Shift Left
            case 0x0A: A = ASL(A); break;
            case 0x06: _bus.Write(ZeroPage(), ASL(_bus.Read(ZeroPage()))); break;
            case 0x16: _bus.Write(ZeroPageX(), ASL(_bus.Read(ZeroPageX()))); break;
            case 0x0E: _bus.Write(Absolute(), ASL(_bus.Read(Absolute()))); break;
            case 0x1E: _bus.Write(AbsoluteX(), ASL(_bus.Read(AbsoluteX()))); break;

            // Logical Shift Right
            case 0x4A: A = LSR(A); break;
            case 0x46: _bus.Write(ZeroPage(), LSR(_bus.Read(ZeroPage()))); break;
            case 0x56: _bus.Write(ZeroPageX(), LSR(_bus.Read(ZeroPageX()))); break;
            case 0x4E: _bus.Write(Absolute(), LSR(_bus.Read(Absolute()))); break;
            case 0x5E: _bus.Write(AbsoluteX(), LSR(_bus.Read(AbsoluteX()))); break;

            // Rotate Left
            case 0x2A: A = ROL(A); break;
            case 0x26: _bus.Write(ZeroPage(), ROL(_bus.Read(ZeroPage()))); break;
            case 0x36: _bus.Write(ZeroPageX(), ROL(_bus.Read(ZeroPageX()))); break;
            case 0x2E: _bus.Write(Absolute(), ROL(_bus.Read(Absolute()))); break;
            case 0x3E: _bus.Write(AbsoluteX(), ROL(_bus.Read(AbsoluteX()))); break;

            // Rotate Right
            case 0x6A: A = ROR(A); break;
            case 0x66: _bus.Write(ZeroPage(), ROR(_bus.Read(ZeroPage()))); break;
            case 0x76: _bus.Write(ZeroPageX(), ROR(_bus.Read(ZeroPageX()))); break;
            case 0x6E: _bus.Write(Absolute(), ROR(_bus.Read(Absolute()))); break;
            case 0x7E: _bus.Write(AbsoluteX(), ROR(_bus.Read(AbsoluteX()))); break;

            // ADC - Add with Carry
            case 0x69: ADC(_bus.Read(Immediate())); break;
            case 0x65: ADC(_bus.Read(ZeroPage())); break;
            case 0x75: ADC(_bus.Read(ZeroPageX())); break;
            case 0x6D: ADC(_bus.Read(Absolute())); break;
            case 0x7D: ADC(_bus.Read(AbsoluteX())); break;
            case 0x79: ADC(_bus.Read(AbsoluteY())); break;
            case 0x61: ADC(_bus.Read(IndirectX())); break;
            case 0x71: ADC(_bus.Read(IndirectY())); break;

            // SBC - Subtract with Carry
            case 0xE9: SBC(_bus.Read(Immediate())); break;
            case 0xE5: SBC(_bus.Read(ZeroPage())); break;
            case 0xF5: SBC(_bus.Read(ZeroPageX())); break;
            case 0xED: SBC(_bus.Read(Absolute())); break;
            case 0xFD: SBC(_bus.Read(AbsoluteX())); break;
            case 0xF9: SBC(_bus.Read(AbsoluteY())); break;
            case 0xE1: SBC(_bus.Read(IndirectX())); break;
            case 0xF1: SBC(_bus.Read(IndirectY())); break;

            default:
                // Unimplemented opcode
                break;
        }
    }

    private void UpdateNZ(byte value)
    {
        P = (byte)(P & ~(0x02 | 0x80));
        if (value == 0) P |= 0x02;
        if ((value & 0x80) != 0) P |= 0x80;
    }

    private void SetFlag(byte mask)
    {
        P |= mask;
    }

    private void ClearFlag(byte mask)
    {
        P = (byte)(P & ~mask);
    }

    private void Push(byte value)
    {
        _bus.Write((ushort)(0x0100 + S--), value);
    }

    private byte Pop()
    {
        return _bus.Read((ushort)(0x0100 + ++S));
    }

    private void PushWord(ushort value)
    {
        Push((byte)(value >> 8));
        Push((byte)value);
    }

    private ushort PopWord()
    {
        byte lo = Pop();
        byte hi = Pop();
        return (ushort)(lo | (hi << 8));
    }

    private void Compare(byte register, ushort address)
    {
        byte value = _bus.Read(address);
        ushort result = (ushort)(register - value);

        if (register >= value)
            P |= 0x01;
        else
            P &= 0xFE;

        UpdateNZ((byte)result);
    }

    private void BranchRelative()
    {
        sbyte offset = (sbyte)Fetch();
        PC = (ushort)(PC + offset);
    }

    private byte ASL(byte value)
    {
        if ((value & 0x80) != 0)
            P |= 0x01;
        else
            P &= 0xFE;

        value <<= 1;
        UpdateNZ(value);
        return value;
    }

    private byte LSR(byte value)
    {
        if ((value & 0x01) != 0)
            P |= 0x01;
        else
            P &= 0xFE;

        value >>= 1;
        UpdateNZ(value);
        return value;
    }

    private byte ROL(byte value)
    {
        byte carry = (byte)(P & 0x01);

        if ((value & 0x80) != 0)
            P |= 0x01;
        else
            P &= 0xFE;

        value = (byte)((value << 1) | carry);
        UpdateNZ(value);
        return value;
    }

    private byte ROR(byte value)
    {
        byte carry = (byte)((P & 0x01) << 7);

        if ((value & 0x01) != 0)
            P |= 0x01;
        else
            P &= 0xFE;

        value = (byte)((value >> 1) | carry);
        UpdateNZ(value);
        return value;
    }

    private void ADC(byte value)
    {
        int carry = (P & 0x01);
        int result = A + value + carry;

        // Overflow flag
        if (((A ^ result) & (value ^ result) & 0x80) != 0)
            P |= 0x40;
        else
            P &= 0xBF;

        // Carry flag
        if (result > 0xFF)
            P |= 0x01;
        else
            P &= 0xFE;

        A = (byte)result;
        UpdateNZ(A);
    }

    private void SBC(byte value)
    {
        int carry = (P & 0x01) ^ 0x01;
        int result = A - value - carry;

        // Overflow flag
        if (((A ^ result) & (~value ^ result) & 0x80) != 0)
            P |= 0x40;
        else
            P &= 0xBF;

        // Carry flag
        if (result >= 0)
            P |= 0x01;
        else
            P &= 0xFE;

        A = (byte)result;
        UpdateNZ(A);
    }
}
