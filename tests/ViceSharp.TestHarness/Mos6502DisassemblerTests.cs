using System.Collections.Generic;
using ViceSharp.Monitor;
using Xunit;

namespace ViceSharp.TestHarness;

/// <summary>
/// FR/TR/TEST: FR-MONITOR-DISASM-001 / TR-MONITOR-DISASM-001 / TEST-MONITOR-DISASM-001.
///
/// Use case: the monitor disassembler must decode every one of the 256 6502
/// opcodes with the correct instruction length and VICE-style text, so a live
/// trace of real C64 demo code (full of absolute-mode stores like
/// <c>STA $D012</c> and some undocumented opcodes) never desyncs.
///
/// Acceptance: <see cref="Mos6502Disassembler.OpcodeLength(byte)"/> matches the
/// canonical NMOS 6502 length table for all 256 opcodes; documented opcodes
/// decode to the expected text; and walking a known instruction stream advances
/// by the right number of bytes (no desync on absolute-mode opcodes).
/// </summary>
public sealed class Mos6502DisassemblerTests
{
    // Canonical NMOS 6502 instruction length per opcode (16 rows x 16), independent
    // of the implementation's own addressing-mode table. Derived from the standard
    // opcode matrix (includes the undocumented opcodes VICE decodes).
    private static readonly byte[] CanonicalLength =
    {
        /* 00 */ 1,2,1,2, 2,2,2,2, 1,2,1,2, 3,3,3,3,
        /* 10 */ 2,2,1,2, 2,2,2,2, 1,3,1,3, 3,3,3,3,
        /* 20 */ 3,2,1,2, 2,2,2,2, 1,2,1,2, 3,3,3,3,
        /* 30 */ 2,2,1,2, 2,2,2,2, 1,3,1,3, 3,3,3,3,
        /* 40 */ 1,2,1,2, 2,2,2,2, 1,2,1,2, 3,3,3,3,
        /* 50 */ 2,2,1,2, 2,2,2,2, 1,3,1,3, 3,3,3,3,
        /* 60 */ 1,2,1,2, 2,2,2,2, 1,2,1,2, 3,3,3,3,
        /* 70 */ 2,2,1,2, 2,2,2,2, 1,3,1,3, 3,3,3,3,
        /* 80 */ 2,2,2,2, 2,2,2,2, 1,2,1,2, 3,3,3,3,
        /* 90 */ 2,2,1,2, 2,2,2,2, 1,3,1,3, 3,3,3,3,
        /* A0 */ 2,2,2,2, 2,2,2,2, 1,2,1,2, 3,3,3,3,
        /* B0 */ 2,2,1,2, 2,2,2,2, 1,3,1,3, 3,3,3,3,
        /* C0 */ 2,2,2,2, 2,2,2,2, 1,2,1,2, 3,3,3,3,
        /* D0 */ 2,2,1,2, 2,2,2,2, 1,3,1,3, 3,3,3,3,
        /* E0 */ 2,2,2,2, 2,2,2,2, 1,2,1,2, 3,3,3,3,
        /* F0 */ 2,2,1,2, 2,2,2,2, 1,3,1,3, 3,3,3,3,
    };

    /// <summary>
    /// FR: FR-MONITOR-DISASM-001, TR: TR-MONITOR-DISASM-001, TEST: TEST-MONITOR-DISASM-001.
    /// Use case: a disassembly walk advances the PC by the decoded instruction length,
    /// so a single wrong length desyncs every following line of a monitor trace.
    /// Acceptance: <see cref="Mos6502Disassembler.OpcodeLength(byte)"/> equals the
    /// canonical NMOS 6502 length table (documented and undocumented opcodes alike)
    /// for all 256 opcodes; any mismatch fails with the offending opcodes listed.
    /// </summary>
    [Fact]
    public void OpcodeLength_MatchesCanonical6502Table_ForAll256Opcodes()
    {
        var mismatches = new List<string>();
        for (int op = 0; op <= 0xFF; op++)
        {
            var actual = Mos6502Disassembler.OpcodeLength((byte)op);
            if (actual != CanonicalLength[op])
                mismatches.Add($"${op:X2}: expected {CanonicalLength[op]} got {actual}");
        }
        Assert.True(mismatches.Count == 0,
            "Opcode length mismatches (would desync disassembly):\n" + string.Join("\n", mismatches));
    }

    [Theory]
    // Representative legal opcodes, one per addressing mode, including the ones
    // the previous stub got wrong (absolute stores/loads, the whole SBC family).
    [InlineData(new byte[] { 0xA9, 0x01 }, "LDA #$01")]
    [InlineData(new byte[] { 0xA5, 0x12 }, "LDA $12")]
    [InlineData(new byte[] { 0xB5, 0x12 }, "LDA $12,X")]
    [InlineData(new byte[] { 0xAD, 0x12, 0xD0 }, "LDA $D012")]   // abs load - was missing
    [InlineData(new byte[] { 0xBD, 0x00, 0x10 }, "LDA $1000,X")]
    [InlineData(new byte[] { 0xB9, 0x00, 0x10 }, "LDA $1000,Y")]
    [InlineData(new byte[] { 0xA1, 0x12 }, "LDA ($12,X)")]
    [InlineData(new byte[] { 0xB1, 0x12 }, "LDA ($12),Y")]
    [InlineData(new byte[] { 0x8D, 0x14, 0x03 }, "STA $0314")]   // abs store - was broken
    [InlineData(new byte[] { 0x9D, 0x00, 0x10 }, "STA $1000,X")]
    [InlineData(new byte[] { 0x99, 0x00, 0x10 }, "STA $1000,Y")]
    [InlineData(new byte[] { 0x91, 0x12 }, "STA ($12),Y")]
    [InlineData(new byte[] { 0x8E, 0x1A, 0xD0 }, "STX $D01A")]
    [InlineData(new byte[] { 0xE9, 0x05 }, "SBC #$05")]          // SBC family - was missing
    [InlineData(new byte[] { 0xED, 0x00, 0x20 }, "SBC $2000")]
    [InlineData(new byte[] { 0x0A }, "ASL A")]
    [InlineData(new byte[] { 0x0E, 0x00, 0x20 }, "ASL $2000")]
    [InlineData(new byte[] { 0x20, 0x00, 0x18 }, "JSR $1800")]
    [InlineData(new byte[] { 0x4C, 0x31, 0xEA }, "JMP $EA31")]
    [InlineData(new byte[] { 0x6C, 0xFE, 0xFF }, "JMP ($FFFE)")]
    [InlineData(new byte[] { 0x78 }, "SEI")]
    [InlineData(new byte[] { 0xEA }, "NOP")]
    public void Decode_LegalOpcode_MatchesExpectedText(byte[] bytes, string expected)
    {
        const ushort at = 0x1000;
        var mem = new byte[0x10000];
        for (int i = 0; i < bytes.Length; i++) mem[at + i] = bytes[i];
        var text = Mos6502Disassembler.Decode(at, a => mem[a]);
        Assert.Equal(expected, text);
    }

    /// <summary>
    /// FR: FR-MONITOR-DISASM-001, TR: TR-MONITOR-DISASM-001, TEST: TEST-MONITOR-DISASM-001.
    /// Use case: branch instructions carry a signed relative offset; the monitor must
    /// display the resolved absolute target address, not the raw operand byte.
    /// Acceptance: decoding BNE with offset +$05 at $1000 yields exactly "BNE $1007"
    /// and BPL with offset -$05 yields exactly "BPL $0FFD" (forward and backward
    /// targets computed from the end of the 2-byte instruction).
    /// </summary>
    [Fact]
    public void Decode_RelativeBranch_ResolvesTargetAddress()
    {
        const ushort at = 0x1000;
        var mem = new byte[0x10000];
        mem[at] = 0xD0; mem[at + 1] = 0x05;      // BNE +5 from 0x1002 -> 0x1007
        Assert.Equal("BNE $1007", Mos6502Disassembler.Decode(at, a => mem[a]));

        mem[at] = 0x10; mem[at + 1] = 0xFB;      // BPL -5 from 0x1002 -> 0x0FFD
        Assert.Equal("BPL $0FFD", Mos6502Disassembler.Decode(at, a => mem[a]));
    }

    /// <summary>
    /// FR: FR-MONITOR-DISASM-001, TR: TR-MONITOR-DISASM-001, TEST: TEST-MONITOR-DISASM-001.
    /// Use case: the Pieces-of-Light loader stub at $0B72 (SEI, LDA/STA $0314 and $0315,
    /// LDX, STX $D01A) is the real code stream whose absolute-mode stores desynced the
    /// previous partial disassembler stub.
    /// Acceptance: walking the 16-byte stub with OpcodeLength/Decode yields exactly the
    /// seven expected instruction texts in order, proving the stream never desyncs on
    /// absolute-mode stores.
    /// </summary>
    [Fact]
    public void WalkStream_LoaderStub_DoesNotDesyncOnAbsoluteStores()
    {
        // The Pieces-of-Light loader stub at $0B72 (the case that desynced before):
        // SEI / LDA #$1B / STA $0314 / LDA #$15 / STA $0315 / LDX #$01 / STX $D01A
        var stub = new byte[] { 0x78, 0xA9, 0x1B, 0x8D, 0x14, 0x03, 0xA9, 0x15, 0x8D, 0x15, 0x03, 0xA2, 0x01, 0x8E, 0x1A, 0xD0 };
        const ushort at = 0x0B72;
        var mem = new byte[0x10000];
        for (int i = 0; i < stub.Length; i++) mem[at + i] = stub[i];

        var decoded = new List<string>();
        ushort pc = at;
        ushort end = (ushort)(at + stub.Length);
        while (pc < end)
        {
            decoded.Add(Mos6502Disassembler.Decode(pc, a => mem[a]));
            pc += Mos6502Disassembler.OpcodeLength(mem[pc]);
        }

        Assert.Equal(
            new[] { "SEI", "LDA #$1B", "STA $0314", "LDA #$15", "STA $0315", "LDX #$01", "STX $D01A" },
            decoded);
    }

    /// <summary>
    /// FR: FR-MONITOR-DISASM-001, TR: TR-MONITOR-DISASM-001, TEST: TEST-MONITOR-DISASM-001.
    /// Use case: real demo code contains undocumented opcodes (JAM, SLO, NOP variants,
    /// SHY, the $EB SBC duplicate); the disassembler must still advance by the correct
    /// instruction width so the stream stays aligned.
    /// Acceptance: <see cref="Mos6502Disassembler.OpcodeLength(byte)"/> returns the
    /// canonical 1/2/3-byte length for each sampled undocumented opcode (exact equality).
    /// </summary>
    [Theory]
    [InlineData(0x02, 1)]  // JAM
    [InlineData(0x03, 2)]  // SLO ($nn,X)
    [InlineData(0x0F, 3)]  // SLO $nnnn
    [InlineData(0x1A, 1)]  // NOP (implied, undocumented)
    [InlineData(0x80, 2)]  // NOP #$nn (undocumented)
    [InlineData(0x9C, 3)]  // SHY $nnnn,X
    [InlineData(0xEB, 2)]  // SBC #$nn (undocumented duplicate)
    public void OpcodeLength_UndocumentedOpcodes_KeepStreamAligned(int opcode, int expectedLength)
        => Assert.Equal(expectedLength, Mos6502Disassembler.OpcodeLength((byte)opcode));
}
