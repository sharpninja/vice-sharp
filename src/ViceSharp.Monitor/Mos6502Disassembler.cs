using System;

namespace ViceSharp.Monitor;

/// <summary>
/// Complete, length-accurate MOS 6502 / 6510 disassembler used by the monitor.
/// Covers all 256 opcodes - including the undocumented (illegal) set, using
/// VICE's mnemonics - so disassembly never desyncs on absolute-mode stores
/// (<c>STA $D012</c>) or illegal opcodes that real C64 demo code is full of.
///
/// FR/TR: FR-MONITOR-DISASM-001 / TR-MONITOR-DISASM-001.
/// </summary>
public static class Mos6502Disassembler
{
    /// <summary>6502 addressing modes (determines operand format and instruction length).</summary>
    public enum AddrMode
    {
        /// <summary>Implied / no operand (1 byte).</summary>
        Implied,
        /// <summary>Accumulator (1 byte, e.g. <c>ASL A</c>).</summary>
        Accumulator,
        /// <summary>Immediate (2 bytes, <c>#$nn</c>).</summary>
        Immediate,
        /// <summary>Zero page (2 bytes, <c>$nn</c>).</summary>
        ZeroPage,
        /// <summary>Zero page,X (2 bytes, <c>$nn,X</c>).</summary>
        ZeroPageX,
        /// <summary>Zero page,Y (2 bytes, <c>$nn,Y</c>).</summary>
        ZeroPageY,
        /// <summary>Absolute (3 bytes, <c>$nnnn</c>).</summary>
        Absolute,
        /// <summary>Absolute,X (3 bytes, <c>$nnnn,X</c>).</summary>
        AbsoluteX,
        /// <summary>Absolute,Y (3 bytes, <c>$nnnn,Y</c>).</summary>
        AbsoluteY,
        /// <summary>Indirect (3 bytes, <c>($nnnn)</c>, JMP only).</summary>
        Indirect,
        /// <summary>Indexed indirect (2 bytes, <c>($nn,X)</c>).</summary>
        IndirectX,
        /// <summary>Indirect indexed (2 bytes, <c>($nn),Y</c>).</summary>
        IndirectY,
        /// <summary>Relative branch (2 bytes, target shown as absolute).</summary>
        Relative,
    }

    /// <summary>One opcode-table entry: VICE mnemonic plus addressing mode.</summary>
    /// <param name="Mnemonic">Three/four letter mnemonic (VICE naming for illegals).</param>
    /// <param name="Mode">Addressing mode.</param>
    public readonly record struct OpInfo(string Mnemonic, AddrMode Mode);

    private const AddrMode IMP = AddrMode.Implied, ACC = AddrMode.Accumulator, IMM = AddrMode.Immediate,
        ZP = AddrMode.ZeroPage, ZPX = AddrMode.ZeroPageX, ZPY = AddrMode.ZeroPageY,
        ABS = AddrMode.Absolute, ABX = AddrMode.AbsoluteX, ABY = AddrMode.AbsoluteY,
        IND = AddrMode.Indirect, IZX = AddrMode.IndirectX, IZY = AddrMode.IndirectY, REL = AddrMode.Relative;

    // Full 256-entry opcode matrix. Undocumented opcodes use VICE's mnemonics
    // (JAM, SLO, RLA, SRE, RRA, SAX, LAX, DCP, ISB, ANC, ASR, ARR, ANE, LXA,
    // SBX, LAS, SHA, SHX, SHY, SHS, USBC) and NOOP for the undocumented NOPs.
    private static readonly OpInfo[] Table =
    {
        /* 00 */ new("BRK", IMP), new("ORA", IZX), new("JAM", IMP), new("SLO", IZX), new("NOOP", ZP), new("ORA", ZP), new("ASL", ZP), new("SLO", ZP), new("PHP", IMP), new("ORA", IMM), new("ASL", ACC), new("ANC", IMM), new("NOOP", ABS), new("ORA", ABS), new("ASL", ABS), new("SLO", ABS),
        /* 10 */ new("BPL", REL), new("ORA", IZY), new("JAM", IMP), new("SLO", IZY), new("NOOP", ZPX), new("ORA", ZPX), new("ASL", ZPX), new("SLO", ZPX), new("CLC", IMP), new("ORA", ABY), new("NOOP", IMP), new("SLO", ABY), new("NOOP", ABX), new("ORA", ABX), new("ASL", ABX), new("SLO", ABX),
        /* 20 */ new("JSR", ABS), new("AND", IZX), new("JAM", IMP), new("RLA", IZX), new("BIT", ZP), new("AND", ZP), new("ROL", ZP), new("RLA", ZP), new("PLP", IMP), new("AND", IMM), new("ROL", ACC), new("ANC", IMM), new("BIT", ABS), new("AND", ABS), new("ROL", ABS), new("RLA", ABS),
        /* 30 */ new("BMI", REL), new("AND", IZY), new("JAM", IMP), new("RLA", IZY), new("NOOP", ZPX), new("AND", ZPX), new("ROL", ZPX), new("RLA", ZPX), new("SEC", IMP), new("AND", ABY), new("NOOP", IMP), new("RLA", ABY), new("NOOP", ABX), new("AND", ABX), new("ROL", ABX), new("RLA", ABX),
        /* 40 */ new("RTI", IMP), new("EOR", IZX), new("JAM", IMP), new("SRE", IZX), new("NOOP", ZP), new("EOR", ZP), new("LSR", ZP), new("SRE", ZP), new("PHA", IMP), new("EOR", IMM), new("LSR", ACC), new("ASR", IMM), new("JMP", ABS), new("EOR", ABS), new("LSR", ABS), new("SRE", ABS),
        /* 50 */ new("BVC", REL), new("EOR", IZY), new("JAM", IMP), new("SRE", IZY), new("NOOP", ZPX), new("EOR", ZPX), new("LSR", ZPX), new("SRE", ZPX), new("CLI", IMP), new("EOR", ABY), new("NOOP", IMP), new("SRE", ABY), new("NOOP", ABX), new("EOR", ABX), new("LSR", ABX), new("SRE", ABX),
        /* 60 */ new("RTS", IMP), new("ADC", IZX), new("JAM", IMP), new("RRA", IZX), new("NOOP", ZP), new("ADC", ZP), new("ROR", ZP), new("RRA", ZP), new("PLA", IMP), new("ADC", IMM), new("ROR", ACC), new("ARR", IMM), new("JMP", IND), new("ADC", ABS), new("ROR", ABS), new("RRA", ABS),
        /* 70 */ new("BVS", REL), new("ADC", IZY), new("JAM", IMP), new("RRA", IZY), new("NOOP", ZPX), new("ADC", ZPX), new("ROR", ZPX), new("RRA", ZPX), new("SEI", IMP), new("ADC", ABY), new("NOOP", IMP), new("RRA", ABY), new("NOOP", ABX), new("ADC", ABX), new("ROR", ABX), new("RRA", ABX),
        /* 80 */ new("NOOP", IMM), new("STA", IZX), new("NOOP", IMM), new("SAX", IZX), new("STY", ZP), new("STA", ZP), new("STX", ZP), new("SAX", ZP), new("DEY", IMP), new("NOOP", IMM), new("TXA", IMP), new("ANE", IMM), new("STY", ABS), new("STA", ABS), new("STX", ABS), new("SAX", ABS),
        /* 90 */ new("BCC", REL), new("STA", IZY), new("JAM", IMP), new("SHA", IZY), new("STY", ZPX), new("STA", ZPX), new("STX", ZPY), new("SAX", ZPY), new("TYA", IMP), new("STA", ABY), new("TXS", IMP), new("SHS", ABY), new("SHY", ABX), new("STA", ABX), new("SHX", ABY), new("SHA", ABY),
        /* A0 */ new("LDY", IMM), new("LDA", IZX), new("LDX", IMM), new("LAX", IZX), new("LDY", ZP), new("LDA", ZP), new("LDX", ZP), new("LAX", ZP), new("TAY", IMP), new("LDA", IMM), new("TAX", IMP), new("LXA", IMM), new("LDY", ABS), new("LDA", ABS), new("LDX", ABS), new("LAX", ABS),
        /* B0 */ new("BCS", REL), new("LDA", IZY), new("JAM", IMP), new("LAX", IZY), new("LDY", ZPX), new("LDA", ZPX), new("LDX", ZPY), new("LAX", ZPY), new("CLV", IMP), new("LDA", ABY), new("TSX", IMP), new("LAS", ABY), new("LDY", ABX), new("LDA", ABX), new("LDX", ABY), new("LAX", ABY),
        /* C0 */ new("CPY", IMM), new("CMP", IZX), new("NOOP", IMM), new("DCP", IZX), new("CPY", ZP), new("CMP", ZP), new("DEC", ZP), new("DCP", ZP), new("INY", IMP), new("CMP", IMM), new("DEX", IMP), new("SBX", IMM), new("CPY", ABS), new("CMP", ABS), new("DEC", ABS), new("DCP", ABS),
        /* D0 */ new("BNE", REL), new("CMP", IZY), new("JAM", IMP), new("DCP", IZY), new("NOOP", ZPX), new("CMP", ZPX), new("DEC", ZPX), new("DCP", ZPX), new("CLD", IMP), new("CMP", ABY), new("NOOP", IMP), new("DCP", ABY), new("NOOP", ABX), new("CMP", ABX), new("DEC", ABX), new("DCP", ABX),
        /* E0 */ new("CPX", IMM), new("SBC", IZX), new("NOOP", IMM), new("ISB", IZX), new("CPX", ZP), new("SBC", ZP), new("INC", ZP), new("ISB", ZP), new("INX", IMP), new("SBC", IMM), new("NOP", IMP), new("USBC", IMM), new("CPX", ABS), new("SBC", ABS), new("INC", ABS), new("ISB", ABS),
        /* F0 */ new("BEQ", REL), new("SBC", IZY), new("JAM", IMP), new("ISB", IZY), new("NOOP", ZPX), new("SBC", ZPX), new("INC", ZPX), new("ISB", ZPX), new("SED", IMP), new("SBC", ABY), new("NOOP", IMP), new("ISB", ABY), new("NOOP", ABX), new("SBC", ABX), new("INC", ABX), new("ISB", ABX),
    };

    /// <summary>Length in bytes (1-3) of the instruction for the given addressing mode.</summary>
    /// <param name="mode">Addressing mode.</param>
    /// <returns>Instruction length in bytes.</returns>
    public static int ModeLength(AddrMode mode) => mode switch
    {
        AddrMode.Implied or AddrMode.Accumulator => 1,
        AddrMode.Absolute or AddrMode.AbsoluteX or AddrMode.AbsoluteY or AddrMode.Indirect => 3,
        _ => 2,
    };

    /// <summary>Returns the opcode-table entry (mnemonic + addressing mode) for an opcode byte.</summary>
    /// <param name="opcode">Opcode byte.</param>
    /// <returns>The decoded <see cref="OpInfo"/>.</returns>
    public static OpInfo Lookup(byte opcode) => Table[opcode];

    /// <summary>Instruction length in bytes (1-3) for the given opcode byte.</summary>
    /// <param name="opcode">Opcode byte.</param>
    /// <returns>Instruction length in bytes.</returns>
    public static byte OpcodeLength(byte opcode) => (byte)ModeLength(Table[opcode].Mode);

    /// <summary>
    /// Decodes the instruction at <paramref name="pc"/> into VICE-style assembly text,
    /// reading operand bytes through <paramref name="peek"/> (a non-intrusive memory read).
    /// </summary>
    /// <param name="pc">Program counter of the opcode.</param>
    /// <param name="peek">Non-intrusive byte reader.</param>
    /// <returns>Disassembled instruction text (e.g. <c>STA $D012</c>).</returns>
    public static string Decode(ushort pc, Func<ushort, byte> peek)
    {
        var (mne, mode) = Table[peek(pc)];
        byte b1 = peek((ushort)(pc + 1));
        byte b2 = peek((ushort)(pc + 2));
        ushort w = (ushort)(b1 | (b2 << 8));
        return mode switch
        {
            AddrMode.Implied => mne,
            AddrMode.Accumulator => $"{mne} A",
            AddrMode.Immediate => $"{mne} #${b1:X2}",
            AddrMode.ZeroPage => $"{mne} ${b1:X2}",
            AddrMode.ZeroPageX => $"{mne} ${b1:X2},X",
            AddrMode.ZeroPageY => $"{mne} ${b1:X2},Y",
            AddrMode.Absolute => $"{mne} ${w:X4}",
            AddrMode.AbsoluteX => $"{mne} ${w:X4},X",
            AddrMode.AbsoluteY => $"{mne} ${w:X4},Y",
            AddrMode.Indirect => $"{mne} (${w:X4})",
            AddrMode.IndirectX => $"{mne} (${b1:X2},X)",
            AddrMode.IndirectY => $"{mne} (${b1:X2}),Y",
            AddrMode.Relative => $"{mne} ${(ushort)(pc + 2 + (sbyte)b1):X4}",
            _ => $".db ${peek(pc):X2}",
        };
    }
}
