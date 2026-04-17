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
}