using System;
using System.IO;
using System.Linq;
using System.Text;
using ViceSharp.Monitor;
using Xunit;

namespace ViceSharp.TestHarness;

/// <summary>
/// FR/TR/TEST: FR-MONITOR-DISASM-001 / TR-MONITOR-DISASM-001 / TEST-MONITOR-DISASM-002.
///
/// Exploratory: disassemble the LIVE (decrunched) IRQ handler of the Pieces-of-Light
/// demo from a user-staged .vsf snapshot using vice#'s own Mos6502Disassembler -
/// proving the in-house disassembler handles real, fully-decrunched demo code (the
/// thing the partial stub could not). Skips when the snapshot is absent.
/// </summary>
public sealed class LiveHandlerTraceTests
{
    private readonly ITestOutputHelper _out;
    public LiveHandlerTraceTests(ITestOutputHelper output) => _out = output;

    [Fact]
    public void Trace_LiveIrqHandler_FromUserSnapshot()
    {
        var path = @"F:\GitHub\vice-sharp\vice-snapshot-20260630171307.vsf";
        if (!File.Exists(path)) { Assert.Skip("user snapshot not present"); return; }

        var b = File.ReadAllBytes(path);
        int c64mem = FindModuleData(b, "C64MEM");
        int ramBase = c64mem + 4; // C64MEM: port0, port1, 2x reserved, then 64K RAM
        byte Peek(ushort a) => b[ramBase + a];

        ushort vec = (ushort)(Peek(0x0314) | (Peek(0x0315) << 8));
        byte d011 = Peek(0xD011), d016 = Peek(0xD016), d018 = Peek(0xD018), d012 = Peek(0xD012);
        _out.WriteLine($"IRQ $0314 -> ${vec:X4}   D011={d011:X2} D012={d012:X2} D016={d016:X2} D018={d018:X2}");
        _out.WriteLine("---- live handler disassembly (vice# Mos6502Disassembler) ----");

        ushort pc = vec;
        for (int i = 0; i < 64; i++)
        {
            var op = Peek(pc);
            var len = Mos6502Disassembler.OpcodeLength(op);
            var bytes = string.Join(" ", Enumerable.Range(0, len).Select(k => Peek((ushort)(pc + k)).ToString("X2")));
            _out.WriteLine($"{pc:X4}  {bytes,-9}{Mos6502Disassembler.Decode(pc, Peek)}");
            pc = (ushort)(pc + len);
            if (op == 0x40 || op == 0x4C) break; // RTI or JMP - end of straight-line run
        }
    }

    private static int FindModuleData(byte[] b, string name)
    {
        int pos = 58;
        while (pos + 22 <= b.Length)
        {
            var moduleName = Encoding.ASCII.GetString(b, pos, 16).TrimEnd('\0', ' ');
            uint size = BitConverter.ToUInt32(b, pos + 18);
            if (moduleName == name) return pos + 22;
            if (size < 22 || pos + (int)size > b.Length) break;
            pos += (int)size;
        }
        throw new InvalidOperationException($"Module '{name}' not found.");
    }
}
