namespace ViceSharp.Chips;

partial class Mos6510
{
    /// <summary>
    /// 6510 Instruction table
    /// </summary>
    private readonly Action[] Opcodes = new Action[256];

    /// <summary>
    /// Initialize opcode jump table
    /// </summary>
    private void InitOpcodes()
    {
        // Load instructions
        Opcodes[0xA9] = Op_LDA_Imm;
        Opcodes[0xA5] = Op_LDA_Zp;
        Opcodes[0xB5] = Op_LDA_ZpX;
        Opcodes[0xAD] = Op_LDA_Abs;
        Opcodes[0xBD] = Op_LDA_AbsX;
        Opcodes[0xB9] = Op_LDA_AbsY;
        Opcodes[0xA1] = Op_LDA_IndX;
        Opcodes[0xB1] = Op_LDA_IndY;

        // Store instructions
        Opcodes[0x85] = Op_STA_Zp;
        Opcodes[0x95] = Op_STA_ZpX;
        Opcodes[0x8D] = Op_STA_Abs;
        Opcodes[0x9D] = Op_STA_AbsX;
        Opcodes[0x99] = Op_STA_AbsY;
        Opcodes[0x81] = Op_STA_IndX;
        Opcodes[0x91] = Op_STA_IndY;

        // Arithmetic
        Opcodes[0x69] = Op_ADC_Imm;
        Opcodes[0x65] = Op_ADC_Zp;
        Opcodes[0x75] = Op_ADC_ZpX;
        Opcodes[0x6D] = Op_ADC_Abs;
        Opcodes[0x7D] = Op_ADC_AbsX;
        Opcodes[0x79] = Op_ADC_AbsY;
        Opcodes[0x61] = Op_ADC_IndX;
        Opcodes[0x71] = Op_ADC_IndY;

        Opcodes[0xE9] = Op_SBC_Imm;
        Opcodes[0xE5] = Op_SBC_Zp;
        Opcodes[0xF5] = Op_SBC_ZpX;
        Opcodes[0xED] = Op_SBC_Abs;
        Opcodes[0xFD] = Op_SBC_AbsX;
        Opcodes[0xF9] = Op_SBC_AbsY;
        Opcodes[0xE1] = Op_SBC_IndX;
        Opcodes[0xF1] = Op_SBC_IndY;

        // Register transfers
        Opcodes[0xAA] = Op_TAX;
        Opcodes[0xA8] = Op_TAY;
        Opcodes[0x8A] = Op_TXA;
        Opcodes[0x98] = Op_TYA;
        Opcodes[0xBA] = Op_TSX;
        Opcodes[0x9A] = Op_TXS;

        // Stack operations
        Opcodes[0x48] = Op_PHA;
        Opcodes[0x08] = Op_PHP;
        Opcodes[0x68] = Op_PLA;
        Opcodes[0x28] = Op_PLP;

        // Branches
        Opcodes[0x10] = Op_BPL;
        Opcodes[0x30] = Op_BMI;
        Opcodes[0x50] = Op_BVC;
        Opcodes[0x70] = Op_BVS;
        Opcodes[0x90] = Op_BCC;
        Opcodes[0xB0] = Op_BCS;
        Opcodes[0xD0] = Op_BNE;
        Opcodes[0xF0] = Op_BEQ;

        // Flags
        Opcodes[0x18] = Op_CLC;
        Opcodes[0x38] = Op_SEC;
        Opcodes[0x58] = Op_CLI;
        Opcodes[0x78] = Op_SEI;
        Opcodes[0xB8] = Op_CLV;
        Opcodes[0xD8] = Op_CLD;
        Opcodes[0xF8] = Op_SED;

        // Increment / Decrement
        Opcodes[0xE8] = Op_INX;
        Opcodes[0xC8] = Op_INY;
        Opcodes[0xCA] = Op_DEX;
        Opcodes[0x88] = Op_DEY;

        // No operation
        Opcodes[0xEA] = Op_NOP;
    }

    #region Instruction Implementations

    private void Op_LDA_Imm() { A = ReadImm(); SetNZ(A); }
    private void Op_LDA_Zp() { A = ReadZp(); SetNZ(A); }
    private void Op_LDA_ZpX() { A = ReadZpX(); SetNZ(A); }
    private void Op_LDA_Abs() { A = ReadAbs(); SetNZ(A); }
    private void Op_LDA_AbsX() { A = ReadAbsoluteX(ReadAbsAddr()); SetNZ(A); }
    private void Op_LDA_AbsY() { A = ReadAbsoluteY(ReadAbsAddr()); SetNZ(A); }
    private void Op_LDA_IndX() { A = ReadIndX(); SetNZ(A); }
    private void Op_LDA_IndY() { A = ReadIndY(); SetNZ(A); }

    private void Op_STA_Zp() { WriteZp(A); }
    private void Op_STA_ZpX() { WriteZpX(A); }
    private void Op_STA_Abs() { WriteAbs(A); }
    private void Op_STA_AbsX() { WriteAbsX(A); }
    private void Op_STA_AbsY() { WriteAbsY(A); }
    private void Op_STA_IndX() { WriteIndX(A); }
    private void Op_STA_IndY() { WriteIndY(A); }

    private void Op_ADC_Imm() { ADC(ReadImm()); }
    private void Op_ADC_Zp() { ADC(ReadZp()); }
    private void Op_ADC_ZpX() { ADC(ReadZpX()); }
    private void Op_ADC_Abs() { ADC(ReadAbs()); }
    private void Op_ADC_AbsX() { ADC(ReadAbsoluteX(ReadAbsAddr())); }
    private void Op_ADC_AbsY() { ADC(ReadAbsoluteY(ReadAbsAddr())); }
    private void Op_ADC_IndX() { ADC(ReadIndX()); }
    private void Op_ADC_IndY() { ADC(ReadIndY()); }

    private void Op_SBC_Imm() { SBC(ReadImm()); }
    private void Op_SBC_Zp() { SBC(ReadZp()); }
    private void Op_SBC_ZpX() { SBC(ReadZpX()); }
    private void Op_SBC_Abs() { SBC(ReadAbs()); }
    private void Op_SBC_AbsX() { SBC(ReadAbsoluteX(ReadAbsAddr())); }
    private void Op_SBC_AbsY() { SBC(ReadAbsoluteY(ReadAbsAddr())); }
    private void Op_SBC_IndX() { SBC(ReadIndX()); }
    private void Op_SBC_IndY() { SBC(ReadIndY()); }

    private void Op_TAX() { X = A; SetNZ(X); }
    private void Op_TAY() { Y = A; SetNZ(Y); }
    private void Op_TXA() { A = X; SetNZ(A); }
    private void Op_TYA() { A = Y; SetNZ(A); }
    private void Op_TSX() { X = SP; SetNZ(X); }
    private void Op_TXS() { SP = X; }

    private void Op_PHA() { Push(A); }
    private void Op_PHP() { Push(GetStatus()); }
    private void Op_PLA() { A = Pop(); SetNZ(A); }
    private void Op_PLP() { P = Pop(); SetNZ(P); }

    private void Op_BPL() { Branch((FlagN & 0x80) == 0); }
    private void Op_BMI() { Branch((FlagN & 0x80) != 0); }
    private void Op_BVC() { Branch((P & (byte)StatusFlags.Overflow) == 0); }
    private void Op_BVS() { Branch((P & (byte)StatusFlags.Overflow) != 0); }
    private void Op_BCC() { Branch((P & (byte)StatusFlags.Carry) == 0); }
    private void Op_BCS() { Branch((P & (byte)StatusFlags.Carry) != 0); }
    private void Op_BNE() { Branch(!FlagZ); }
    private void Op_BEQ() { Branch(FlagZ); }

    private void Op_CLC() { P &= unchecked((byte)~StatusFlags.Carry); }
    private void Op_SEC() { P |= (byte)StatusFlags.Carry; }
    private void Op_CLI() { P &= unchecked((byte)~StatusFlags.InterruptDisable); }
    private void Op_SEI() { P |= (byte)StatusFlags.InterruptDisable; }
    private void Op_CLV() { P &= unchecked((byte)~StatusFlags.Overflow); }
    private void Op_CLD() { P &= unchecked((byte)~StatusFlags.Decimal); }
    private void Op_SED() { P |= (byte)StatusFlags.Decimal; }

    private void Op_INX() { X++; SetNZ(X); }
    private void Op_INY() { Y++; SetNZ(Y); }
    private void Op_DEX() { X--; SetNZ(X); }
    private void Op_DEY() { Y--; SetNZ(Y); }

    private void Op_NOP() { }

    #endregion
}