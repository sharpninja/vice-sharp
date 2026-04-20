using ViceSharp.Abstractions;

namespace ViceSharp.Monitor;

public sealed class Monitor : IMonitor
{
    private readonly IMachine _machine;
    private readonly List<ushort> _breakpoints = new();
    public bool IsPaused { get; private set; }
    public Monitor(IMachine machine) => _machine = machine;

    public string ExecuteCommand(string command)
    {
        var parts = command.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return "";
        var cmd = parts[0].ToLowerInvariant();
        return cmd switch
        {
            "help" or "?" => ShowHelp(),
            "registers" or "r" => GetRegisters(),
            "step" or "z" => Step(parts),
            "next" or "n" => StepOver(),
            "mem" or "m" => DumpMemory(parts),
            "disass" or "d" => Disassemble(parts),
            "break" or "b" => AddBreakpoint(parts),
            "unbreak" or "ub" => RemoveBreakpoint(parts),
            "breaklist" or "bl" => ListBreakpoints(),
            "reset" => ResetMachine(),
            "exit" or "x" or "q" => "Exiting",
            "cycles" => ShowCycleCount(),
            _ => $"Unknown: {cmd}"
        };
    }

    private string ShowHelp() => "Monitor: r z n m d b ub bl reset cycles x";

    private string GetRegisters()
    {
        var s = _machine.GetState();
        var f = FormatFlags(s.P);
        return $"  ADDR A  X  Y  SP NV-BDIZC\n.{s.PC:X4} {s.A:X2} {s.X:X2} {s.Y:X2} {s.S:X2} {f}\nCycles: {s.Cycle}";
    }

    private static string FormatFlags(byte p)
    {
        var flags = new char[8];
        flags[0] = (p & 0x80) != 0 ? 'N' : '.';
        flags[1] = (p & 0x40) != 0 ? 'V' : '.';
        flags[2] = (p & 0x20) != 0 ? '-' : '.';
        flags[3] = (p & 0x10) != 0 ? 'B' : '.';
        flags[4] = (p & 0x08) != 0 ? 'D' : '.';
        flags[5] = (p & 0x04) != 0 ? 'I' : '.';
        flags[6] = (p & 0x02) != 0 ? 'Z' : '.';
        flags[7] = (p & 0x01) != 0 ? 'C' : '.';
        return new string(flags);
    }

    private string Step(string[] parts)
    {
        int count = parts.Length > 1 && int.TryParse(parts[1], out var c) ? c : 1;
        var results = new List<string>();
        for (int i = 0; i < count; i++)
        {
            _machine.StepInstruction();
            var s = _machine.GetState();
            if (_breakpoints.Contains(s.PC)) { results.Add($"BREAK ${s.PC:X4}"); IsPaused = true; break; }
            if (count <= 4) results.Add($"{s.PC:X4}  {_machine.Bus.Read(s.PC):X2} {Disasm(s.PC, _machine.Bus.Read(s.PC))}");
        }
        var f = _machine.GetState();
        results.Add($"PC: {f.PC:X4} A: {f.A:X2} X: {f.X:X2} Y: {f.Y:X2}");
        return string.Join("\n", results);
    }

    private string StepOver()
    {
        var start = _machine.GetState().PC;
        var op = _machine.Bus.Read(start);
        int target = start + (op == 0x20 ? 3 : 1);
        while (_machine.GetState().PC != target && !IsPaused)
        {
            _machine.StepInstruction();
            if (_breakpoints.Contains(_machine.GetState().PC)) IsPaused = true;
        }
        return GetRegisters();
    }

    private string DumpMemory(string[] parts)
    {
        ushort addr = 0;
        if (parts.Length > 1) ushort.TryParse(parts[1], System.Globalization.NumberStyles.HexNumber, null, out addr);
        var bus = _machine.Bus;
        var lines = new List<string>();
        for (int l = 0; l < 8; l++)
        {
            var off = (ushort)(addr + l * 16);
            var bytes = Enumerable.Range(0, 16).Select(i => bus.Read((ushort)(off + i))).ToArray();
            var ascii = new string(bytes.Select(b => b >= 0x20 && b < 0x7F ? (char)b : '.').ToArray());
            lines.Add($"{off:X4}  {BitConverter.ToString(bytes).Replace("-", " ")}  {ascii}");
        }
        return string.Join("\n", lines);
    }

    private string Disassemble(string[] parts)
    {
        ushort addr = _machine.GetState().PC;
        if (parts.Length > 1) ushort.TryParse(parts[1], System.Globalization.NumberStyles.HexNumber, null, out addr);
        int count = parts.Length > 2 && int.TryParse(parts[2], out var c) ? c : 8;
        var lines = new List<string>();
        for (int i = 0; i < count; i++)
        {
            var pc = addr;
            var op = _machine.Bus.Read(pc);
            lines.Add($"{pc:X4}  {op:X2} {Disasm(pc, op)}");
            addr = (ushort)(pc + OpLen(op));
        }
        return string.Join("\n", lines);
    }

    private static int OpLen(byte op) => op switch
    {
        0x00 or 0x08 or 0x10 or 0x18 or 0x28 or 0x30 or 0x38 or 0x40 or 0x48 or 0x50 or 0x58 or 0x60 or 0x68 or 0x70 or 0x78 or 0x88 or 0x8A or 0x98 or 0x9A or 0xA8 or 0xAA or 0xB8 or 0xBA or 0xC8 or 0xCA or 0xD8 or 0xEA or 0xF8 => 1,
        0x20 or 0x4C or 0x6C => 3,
        _ => 2
    };

    private string Disasm(ushort pc, byte op)
    {
        var b = _machine.Bus;
        var b1 = b.Read((ushort)(pc + 1));
        var b2 = b.Read((ushort)(pc + 2));
        return op switch
        {
            0x00 => "BRK", 0x08 => "PHP", 0x0A => "ASL A", 0x18 => "CLC", 0x28 => "PLP",
            0x38 => "SEC", 0x40 => "RTI", 0x48 => "PHA", 0x4A => "LSR A", 0x58 => "CLI",
            0x60 => "RTS", 0x68 => "PLA", 0x78 => "SEI", 0x88 => "DEY", 0x8A => "TXA",
            0x98 => "TYA", 0x9A => "TXS", 0xA8 => "TAY", 0xAA => "TAX", 0xB8 => "CLV",
            0xBA => "TSX", 0xC8 => "INY", 0xCA => "DEX", 0xD8 => "CLD", 0xEA => "NOP", 0xF8 => "SED",
            0x10 => $"BPL ${pc + 2 + (sbyte)b1:X4}", 0x30 => $"BMI ${pc + 2 + (sbyte)b1:X4}",
            0x50 => $"BVC ${pc + 2 + (sbyte)b1:X4}", 0x70 => $"BVS ${pc + 2 + (sbyte)b1:X4}",
            0x90 => $"BCC ${pc + 2 + (sbyte)b1:X4}", 0xB0 => $"BCS ${pc + 2 + (sbyte)b1:X4}",
            0xD0 => $"BNE ${pc + 2 + (sbyte)b1:X4}", 0xF0 => $"BEQ ${pc + 2 + (sbyte)b1:X4}",
            0x20 => $"JSR ${b2:X2}{b1:X2}", 0x4C => $"JMP ${b2:X2}{b1:X2}", 0x6C => $"JMP (${b2:X2}{b1:X2})",
            0x09 => $"ORA #${b1:X2}", 0x05 => $"ORA ${b1:X2}", 0x15 => $"ORA ${b1:X2},X",
            0x19 => $"ORA ${b2:X2}{b1:X2},Y", 0x1D => $"ORA ${b2:X2}{b1:X2},X",
            0x01 => $"ORA (${b1:X2},X)", 0x11 => $"ORA (${b1:X2}),Y",
            0x29 => $"AND #${b1:X2}", 0x25 => $"AND ${b1:X2}", 0x35 => $"AND ${b1:X2},X",
            0x39 => $"AND ${b2:X2}{b1:X2},Y", 0x3D => $"AND ${b2:X2}{b1:X2},X",
            0x21 => $"AND (${b1:X2},X)", 0x31 => $"AND (${b1:X2}),Y",
            0x49 => $"EOR #${b1:X2}", 0x45 => $"EOR ${b1:X2}", 0x55 => $"EOR ${b1:X2},X",
            0x59 => $"EOR ${b2:X2}{b1:X2},Y", 0x5D => $"EOR ${b2:X2}{b1:X2},X",
            0x41 => $"EOR (${b1:X2},X)", 0x51 => $"EOR (${b1:X2}),Y",
            0x69 => $"ADC #${b1:X2}", 0x65 => $"ADC ${b1:X2}", 0x75 => $"ADC ${b1:X2},X",
            0x79 => $"ADC ${b2:X2}{b1:X2},Y", 0x7D => $"ADC ${b2:X2}{b1:X2},X",
            0x61 => $"ADC (${b1:X2},X)", 0x71 => $"ADC (${b1:X2}),Y",
            0xA9 => $"LDA #${b1:X2}", 0xA5 => $"LDA ${b1:X2}", 0xB5 => $"LDA ${b1:X2},X",
            0xB9 => $"LDA ${b2:X2}{b1:X2},Y", 0xBD => $"LDA ${b2:X2}{b1:X2},X",
            0xA1 => $"LDA (${b1:X2},X)", 0xB1 => $"LDA (${b1:X2}),Y",
            0x85 => $"STA ${b1:X2}", 0x95 => $"STA ${b1:X2},X",
            0x99 => $"STA ${b2:X2}{b1:X2},Y", 0x9D => $"STA ${b2:X2}{b1:X2},X",
            0x81 => $"STA (${b1:X2},X)", 0x91 => $"STA (${b1:X2}),Y",
            0x86 => $"STX ${b1:X2}", 0x96 => $"STX ${b1:X2},Y",
            0x84 => $"STY ${b1:X2}", 0x94 => $"STY ${b1:X2},X",
            0xA2 => $"LDX #${b1:X2}", 0xA6 => $"LDX ${b1:X2}", 0xB6 => $"LDX ${b1:X2},Y",
            0xAE => $"LDX ${b2:X2}{b1:X2}", 0xBE => $"LDX ${b2:X2}{b1:X2},Y",
            0xA0 => $"LDY #${b1:X2}", 0xA4 => $"LDY ${b1:X2}", 0xB4 => $"LDY ${b1:X2},X",
            0xAC => $"LDY ${b2:X2}{b1:X2}", 0xBC => $"LDY ${b2:X2}{b1:X2},X",
            0x06 => $"ASL ${b1:X2}", 0x16 => $"ASL ${b1:X2},X",
            0x0E => $"ASL ${b2:X2}{b1:X2}", 0x1E => $"ASL ${b2:X2}{b1:X2},X",
            0x26 => $"ROL ${b1:X2}", 0x36 => $"ROL ${b1:X2},X",
            0x2E => $"ROL ${b2:X2}{b1:X2}", 0x3E => $"ROL ${b2:X2}{b1:X2},X",
            0x46 => $"LSR ${b1:X2}", 0x56 => $"LSR ${b1:X2},X",
            0x4E => $"LSR ${b2:X2}{b1:X2}", 0x5E => $"LSR ${b2:X2}{b1:X2},X",
            0x66 => $"ROR ${b1:X2}", 0x76 => $"ROR ${b1:X2},X",
            0x6E => $"ROR ${b2:X2}{b1:X2}", 0x7E => $"ROR ${b2:X2}{b1:X2},X",
            0xC1 => $"CMP (${b1:X2},X)", 0xC5 => $"CMP ${b1:X2}",
            0xC9 => $"CMP #${b1:X2}", 0xCD => $"CMP ${b2:X2}{b1:X2}",
            0xD1 => $"CMP (${b1:X2}),Y", 0xD5 => $"CMP ${b1:X2},X",
            0xD9 => $"CMP ${b2:X2}{b1:X2},Y", 0xDD => $"CMP ${b2:X2}{b1:X2},X",
            0xE0 => $"CPX #${b1:X2}", 0xE4 => $"CPX ${b1:X2}",
            0xEC => $"CPX ${b2:X2}{b1:X2}",
            0xC0 => $"CPY #${b1:X2}", 0xC4 => $"CPY ${b1:X2}",
            0xCC => $"CPY ${b2:X2}{b1:X2}",
            0x24 => $"BIT ${b1:X2}", 0x2C => $"BIT ${b2:X2}{b1:X2}",
            0xE6 => $"INC ${b1:X2}", 0xF6 => $"INC ${b1:X2},X",
            0xEE => $"INC ${b2:X2}{b1:X2}", 0xFE => $"INC ${b2:X2}{b1:X2},X",
            0xC6 => $"DEC ${b1:X2}", 0xD6 => $"DEC ${b1:X2},X",
            0xCE => $"DEC ${b2:X2}{b1:X2}", 0xDE => $"DEC ${b2:X2}{b1:X2},X",
            0xE8 => "INX",
            0x1A or 0x3A or 0x5A or 0x7A or 0xDA or 0xFA => "NOP",
            _ => $".db ${op:X2}"
        };
    }

    private string AddBreakpoint(string[] parts)
    {
        if (parts.Length < 2) return "Usage: b <address>";
        if (ushort.TryParse(parts[1], System.Globalization.NumberStyles.HexNumber, null, out var addr))
        {
            if (!_breakpoints.Contains(addr)) _breakpoints.Add(addr);
            return $"Breakpoint at ${addr:X4}";
        }
        return "Invalid address";
    }

    private string RemoveBreakpoint(string[] parts)
    {
        if (parts.Length < 2) return "Usage: ub <address>";
        if (ushort.TryParse(parts[1], System.Globalization.NumberStyles.HexNumber, null, out var addr))
        {
            _breakpoints.Remove(addr);
            return $"Removed ${addr:X4}";
        }
        return "Invalid address";
    }

    private string ListBreakpoints() => _breakpoints.Count == 0 ? "No breakpoints" : string.Join(", ", _breakpoints.Select(b => $"${b:X4}"));

    private string ResetMachine() { _machine.Reset(); return "Reset"; }

    private string ShowCycleCount() => $"Cycles: {_machine.GetState().Cycle}";

    RegisterSnapshot IMonitor.GetRegisters()
    {
        var s = _machine.GetState();
        return new RegisterSnapshot { A = s.A, X = s.X, Y = s.Y, S = s.S, P = s.P, PC = s.PC };
    }
}
