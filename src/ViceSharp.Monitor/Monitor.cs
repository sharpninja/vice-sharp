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
            if (count <= 4)
            {
                var op = _machine.Bus.Peek(s.PC);
                results.Add($"{s.PC:X4}  {op:X2} {Mos6502Disassembler.Decode(s.PC, a => _machine.Bus.Peek(a))}");
            }
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
        var lines = Disassemble(addr, count)
            .Select(entry => $"{entry.Address:X4}  {BitConverter.ToString(entry.Bytes).Replace("-", " ")} {entry.Text}");
        return string.Join("\n", lines);
    }

    public IReadOnlyList<DisassemblyEntry> Disassemble(ushort address, int count)
    {
        if (count <= 0)
            return [];

        var entries = new List<DisassemblyEntry>(count);
        for (int i = 0; i < count; i++)
        {
            var pc = address;
            var op = _machine.Bus.Peek(pc);
            var length = Mos6502Disassembler.OpcodeLength(op);
            var bytes = new byte[length];
            for (var offset = 0; offset < length; offset++)
                bytes[offset] = _machine.Bus.Peek((ushort)(pc + offset));

            var nextAddress = (ushort)(pc + length);
            entries.Add(new DisassemblyEntry(pc, bytes, Mos6502Disassembler.Decode(pc, a => _machine.Bus.Peek(a)), length, nextAddress));
            address = nextAddress;
        }

        return entries;
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

    IReadOnlyList<DisassemblyEntry> IMonitor.Disassemble(ushort address, int count) => Disassemble(address, count);
}
