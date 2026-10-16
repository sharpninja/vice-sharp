using System.Globalization;
using System.Runtime.CompilerServices;
using ViceSharp.Abstractions;

namespace ViceSharp.Monitor;

public sealed class Monitor : IMonitor
{
    private static readonly ConditionalWeakTable<IMachine, WatchList> WatchLists = new();

    private readonly IMachine _machine;
    private readonly List<ushort> _breakpoints = new();

    // cpu, ram, rom, io, or drive. Selection must not write the PLA port.
    private string _bank = "cpu";

    public bool IsPaused { get; private set; }
    public Monitor(IMachine machine) => _machine = machine;

    private WatchList Watches => WatchLists.GetValue(_machine, static _ => new WatchList());

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
            "bank" => SelectBank(parts),
            "fill" => FillMemory(parts),
            "watch" => Watch(parts),
            "unwatch" => Unwatch(parts),
            "reset" => ResetMachine(),
            "exit" or "x" or "q" => "Exiting",
            "cycles" => ShowCycleCount(),
            _ => $"Unknown: {cmd}"
        };
    }

    private string ShowHelp() => "Monitor: r z n m d b ub bl bank fill watch unwatch reset cycles x";

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
        if (parts.Length > 1) ushort.TryParse(parts[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out addr);
        var lines = new List<string>();
        for (int l = 0; l < 8; l++)
        {
            var off = (ushort)(addr + l * 16);
            var bytes = Enumerable.Range(0, 16).Select(i => ReadView((ushort)(off + i))).ToArray();
            var ascii = new string(bytes.Select(b => b >= 0x20 && b < 0x7F ? (char)b : '.').ToArray());
            lines.Add($"{off:X4}  {BitConverter.ToString(bytes).Replace("-", " ")}  {ascii}");
        }
        return string.Join("\n", lines);
    }

    private string SelectBank(string[] parts)
    {
        if (parts.Length < 2)
            return _bank;

        var name = parts[1].ToLowerInvariant();
        if (name is not ("cpu" or "ram" or "rom" or "io" or "drive"))
            return "bad bank";

        _bank = name;
        return _bank;
    }

    private string FillMemory(string[] parts)
    {
        if (parts.Length < 5)
            return "usage: fill ram <start> <end> <value>";
        if (!parts[1].Equals("ram", StringComparison.OrdinalIgnoreCase))
            return "fill supports ram";
        if (!TryHex16(parts[2], out var start) || !TryHex16(parts[3], out var end) || !TryHex8(parts[4], out var value))
            return "bad fill";
        if (end < start)
            return "bad range";
        if (_machine.Devices.GetByRole(DeviceRole.SystemRam) is not IMemory ram)
            return "no ram";

        var span = ram.Span;
        for (int address = start; address <= end && address < span.Length; address++)
            span[address] = value;

        return "filled";
    }

    private byte ReadView(ushort address)
    {
        switch (_bank)
        {
            case "ram":
                return ReadSystemRam(address);
            case "rom":
                return ReadRomImage(address);
            case "cpu":
                return _machine.Bus.Read(address);
            case "io":
                return ReadIo(address);
            case "drive":
                return ReadDrive(address);
            default:
                return _machine.Bus.Read(address);
        }
    }

    private byte ReadRomImage(ushort address)
    {
        if (_machine.Devices.GetByRole(DeviceRole.SystemRam) is IFixedRomImage images
            && images.TryPeekRom(address, out var image))
            return image;

        foreach (var role in new[] { DeviceRole.KernalRom, DeviceRole.BasicRom, DeviceRole.ChargenRom })
        {
            if (_machine.Devices.GetByRole(role) is IAddressSpace rom && rom.HandlesAddress(address))
                return rom.Peek(address);
        }

        return 0xFF;
    }

    private byte ReadSystemRam(ushort address)
    {
        if (_machine.Devices.GetByRole(DeviceRole.SystemRam) is not IMemory ram)
            return 0;

        var span = ram.Span;
        return address < span.Length ? span[address] : (byte)0;
    }

    private byte ReadIo(ushort address)
    {
        if (_machine.Devices.GetByRole(DeviceRole.VideoChip) is IAddressSpace video)
        {
            if (video.HandlesAddress(address))
                return video.Read(address);

            // Chips that decode a register index still see $D000 as sprite X.
            var offset = (ushort)(address & 0x3F);
            if (address is >= 0xD000 and <= 0xD3FF && video.HandlesAddress(offset))
                return video.Read(offset);
        }

        if (address is >= 0xD400 and <= 0xD7FF
            && _machine.Devices.GetByRole(DeviceRole.AudioChip) is IAddressSpace sid)
        {
            // Peek: OSC3/ENV3 Read updates the SID data bus.
            return sid.Peek(address);
        }

        if (address is >= 0xDC00 and <= 0xDCFF
            && _machine.Devices.GetByRole(DeviceRole.Cia1) is IAddressSpace cia1
            && cia1.HandlesAddress(address))
        {
            return cia1.Peek(address);
        }

        if (address is >= 0xDD00 and <= 0xDDFF
            && _machine.Devices.GetByRole(DeviceRole.Cia2) is IAddressSpace cia2
            && cia2.HandlesAddress(address))
        {
            return cia2.Peek(address);
        }

        return 0;
    }

    private byte ReadDrive(ushort address)
    {
        if (TryPeekRole(DeviceRole.DriveRam, address, out var value)
            || TryPeekRole(DeviceRole.DriveRom, address, out value)
            || TryPeekRole(DeviceRole.DriveVia, address, out value))
        {
            return value;
        }

        return 0;
    }

    private bool TryPeekRole(DeviceRole role, ushort address, out byte value)
    {
        value = 0;
        if (_machine.Devices.GetByRole(role) is not IAddressSpace space || !space.HandlesAddress(address))
            return false;

        value = space.Peek(address);
        return true;
    }

    private string Watch(string[] parts)
    {
        if (parts.Length < 2)
            return ListWatches();

        if (parts.Length >= 4 && parts[1].Equals("reorder", StringComparison.OrdinalIgnoreCase))
            return ReorderWatches(parts);

        var tokens = parts.Skip(1).ToArray();
        var format = WatchFormat.Hex;
        if (tokens.Length > 0 && TryParseFormat(tokens[^1], out var parsed))
        {
            format = parsed;
            tokens = tokens[..^1];
        }

        if (tokens.Length == 0)
            return "usage: watch <expr>";

        var built = BuildWatch(string.Join(' ', tokens), format);
        if (built is null)
            return "bad watch";

        Watches.Items.Add(built);
        return Render(built);
    }

    private string Unwatch(string[] parts)
    {
        if (parts.Length < 2 || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var index) || index < 1)
            return "usage: unwatch <n>";

        var items = Watches.Items;
        if (index > items.Count)
            return "no such watch";

        items.RemoveAt(index - 1);
        return "removed";
    }

    private string ListWatches()
    {
        var items = Watches.Items;
        if (items.Count == 0)
            return "none";

        var lines = new string[items.Count];
        for (int i = 0; i < items.Count; i++)
            lines[i] = $"{i + 1} {Render(items[i])}";

        return string.Join("\n", lines);
    }

    private string ReorderWatches(string[] parts)
    {
        if (!int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var from)
            || !int.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var to))
            return "usage: watch reorder <from> <to>";

        var items = Watches.Items;
        if (from < 1 || to < 1 || from > items.Count || to > items.Count)
            return "no such watch";

        var item = items[from - 1];
        items.RemoveAt(from - 1);
        items.Insert(to - 1, item);
        return ListWatches();
    }

    private string Render(WatchItem watch)
    {
        string value;
        try
        {
            value = FormatWatch(watch);
        }
        catch (FormatException)
        {
            value = "";
        }
        catch (OverflowException)
        {
            value = "";
        }

        var text = value.Length == 0 ? watch.Label : watch.Label + " " + value;
        if (watch.Disruptive)
            text += " disruptive";
        return text;
    }

    private string FormatWatch(WatchItem watch)
    {
        switch (watch.Kind)
        {
            case WatchKind.Register:
                return FormatScalar(ReadRegister(watch.Register), watch.Format, watch.Register == "PC");
            case WatchKind.Memory:
                return FormatScalar(ReadWatchByte(watch.Start), watch.Format, word: false);
            case WatchKind.Range:
                int count = watch.End - watch.Start + 1;
                if (count > 256)
                    count = 256;
                var shown = new string[count];
                for (int i = 0; i < count; i++)
                    shown[i] = FormatScalar(ReadWatchByte((ushort)(watch.Start + i)), watch.Format, word: false);
                return string.Join(' ', shown);
            default:
                return FormatScalar(Evaluate(watch.Expression), watch.Format, word: true);
        }
    }

    private int ReadRegister(string name)
    {
        var state = _machine.GetState();
        return name switch
        {
            "A" => state.A,
            "X" => state.X,
            "Y" => state.Y,
            "SP" => state.S,
            "PC" => state.PC,
            _ => 0
        };
    }

    private byte ReadWatchByte(ushort address)
    {
        // OSC3 ($D41B) is a destructive SID read. Report it, do not perform it.
        if (address == 0xD41B)
            return 0;

        var value = _machine.Bus.Read(address);

        // $D020-$D02E float the high nibble (a color of 14 reads back as $FE).
        // The watch reports the 0-15 color, which is the byte that was stored.
        if (address is >= 0xD000 and <= 0xD3FF)
        {
            int register = address & 0x3F;
            if (register is >= 0x20 and <= 0x2E)
                value = (byte)(value & 0x0F);
        }

        return value;
    }

    private static string FormatScalar(int value, WatchFormat format, bool word)
    {
        switch (format)
        {
            case WatchFormat.Decimal:
                return value.ToString(CultureInfo.InvariantCulture);
            case WatchFormat.Binary:
                return Convert.ToString(value, 2);
            case WatchFormat.Petscii:
                var pet = (byte)value;
                return pet is >= 0x20 and <= 0x7E ? ((char)pet).ToString() : ".";
            default:
                return word
                    ? ((ushort)value).ToString("X4", CultureInfo.InvariantCulture)
                    : ((byte)value).ToString("X2", CultureInfo.InvariantCulture);
        }
    }

    private WatchItem? BuildWatch(string expr, WatchFormat format)
    {
        if (TryParseRegister(expr, out var register))
        {
            return new WatchItem
            {
                Kind = WatchKind.Register,
                Format = format,
                Label = register,
                Register = register
            };
        }

        if (TryParseMem(expr, out var start, out var end, out var isRange))
        {
            if (isRange && end < start)
                return null;
            if (!isRange)
                end = start;

            var hitsOsc3 = isRange ? start <= 0xD41B && end >= 0xD41B : start == 0xD41B;
            return new WatchItem
            {
                Kind = isRange ? WatchKind.Range : WatchKind.Memory,
                Format = format,
                Label = isRange ? $"[${start:X4}:${end:X4}]" : $"[${start:X4}]",
                Start = start,
                End = end,
                Disruptive = hitsOsc3
            };
        }

        if (expr.Contains('[') || expr.Contains('+') || expr.Contains('-') || expr.Contains('*') || expr.Contains('/'))
        {
            return new WatchItem
            {
                Kind = WatchKind.Expression,
                Format = format,
                Label = expr.ToUpperInvariant(),
                Expression = expr,
                Disruptive = expr.Contains("D41B", StringComparison.OrdinalIgnoreCase)
            };
        }

        return null;
    }

    private int Evaluate(string expr)
    {
        int index = 0;
        int value = ParseAdd(expr, ref index);
        Skip(expr, ref index);
        if (index != expr.Length)
            throw new FormatException();
        return value;
    }

    private int ParseAdd(string expr, ref int index)
    {
        int value = ParseMul(expr, ref index);
        while (true)
        {
            Skip(expr, ref index);
            if (index >= expr.Length || (expr[index] != '+' && expr[index] != '-'))
                return value;

            char op = expr[index++];
            int rhs = ParseMul(expr, ref index);
            value = op == '+' ? value + rhs : value - rhs;
        }
    }

    private int ParseMul(string expr, ref int index)
    {
        int value = ParseFactor(expr, ref index);
        while (true)
        {
            Skip(expr, ref index);
            if (index >= expr.Length || (expr[index] != '*' && expr[index] != '/'))
                return value;

            char op = expr[index++];
            int rhs = ParseFactor(expr, ref index);
            if (op == '*')
                value *= rhs;
            else
                value = rhs == 0 ? 0 : value / rhs;
        }
    }

    private int ParseFactor(string expr, ref int index)
    {
        Skip(expr, ref index);
        if (index >= expr.Length)
            throw new FormatException();

        if (expr[index] == '(')
        {
            index++;
            int nested = ParseAdd(expr, ref index);
            Skip(expr, ref index);
            if (index >= expr.Length || expr[index] != ')')
                throw new FormatException();
            index++;
            return nested;
        }

        if (expr[index] == '[')
        {
            int end = expr.IndexOf(']', index);
            if (end < 0)
                throw new FormatException();
            var token = expr[index..(end + 1)];
            index = end + 1;
            if (!TryParseMem(token, out var address, out _, out var isRange) || isRange)
                throw new FormatException();
            return ReadWatchByte(address);
        }

        if (expr[index] == '$')
        {
            index++;
            int start = index;
            while (index < expr.Length && IsHex(expr[index]))
                index++;
            if (start == index)
                throw new FormatException();
            return Convert.ToInt32(expr[start..index], 16);
        }

        if (char.IsDigit(expr[index]))
        {
            int start = index;
            while (index < expr.Length && char.IsDigit(expr[index]))
                index++;
            return int.Parse(expr[start..index], CultureInfo.InvariantCulture);
        }

        int nameStart = index;
        while (index < expr.Length && char.IsLetter(expr[index]))
            index++;
        if (nameStart == index || !TryParseRegister(expr[nameStart..index], out var register))
            throw new FormatException();
        return ReadRegister(register);
    }

    private static void Skip(string expr, ref int index)
    {
        while (index < expr.Length && expr[index] == ' ')
            index++;
    }

    private static bool TryParseRegister(string text, out string name)
    {
        name = text.ToUpperInvariant();
        return name is "A" or "X" or "Y" or "SP" or "PC";
    }

    private static bool TryParseMem(string text, out ushort start, out ushort end, out bool isRange)
    {
        start = 0;
        end = 0;
        isRange = false;
        if (text.Length < 4 || text[0] != '[' || text[^1] != ']')
            return false;

        var body = text[1..^1];
        if (body.Length == 0 || body[0] != '$')
            return false;

        body = body[1..];
        var colon = body.IndexOf(':');
        if (colon < 0)
            return TryHex16(body, out start);

        isRange = true;
        var right = body[(colon + 1)..];
        if (right.StartsWith('$'))
            right = right[1..];
        return TryHex16(body[..colon], out start) && TryHex16(right, out end);
    }

    private static bool TryParseFormat(string text, out WatchFormat format)
    {
        switch (text.ToLowerInvariant())
        {
            case "hex":
                format = WatchFormat.Hex;
                return true;
            case "decimal":
                format = WatchFormat.Decimal;
                return true;
            case "binary":
                format = WatchFormat.Binary;
                return true;
            case "ascii":
            case "petscii":
                format = WatchFormat.Petscii;
                return true;
            default:
                format = WatchFormat.Hex;
                return false;
        }
    }

    private static bool TryHex16(string text, out ushort value)
    {
        if (text.StartsWith('$'))
            text = text[1..];
        return ushort.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
    }

    private static bool TryHex8(string text, out byte value)
    {
        if (text.StartsWith('$'))
            text = text[1..];
        return byte.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
    }

    private static bool IsHex(char c)
        => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');

    private string Disassemble(string[] parts)
    {
        ushort addr = _machine.GetState().PC;
        if (parts.Length > 1) ushort.TryParse(parts[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out addr);
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
        if (ushort.TryParse(parts[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var addr))
        {
            if (!_breakpoints.Contains(addr)) _breakpoints.Add(addr);
            return $"Breakpoint at ${addr:X4}";
        }
        return "Invalid address";
    }

    private string RemoveBreakpoint(string[] parts)
    {
        if (parts.Length < 2) return "Usage: ub <address>";
        if (ushort.TryParse(parts[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var addr))
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

    private enum WatchKind { Register, Memory, Range, Expression }

    private enum WatchFormat { Hex, Decimal, Binary, Petscii }

    private sealed class WatchItem
    {
        public WatchKind Kind { get; init; }
        public WatchFormat Format { get; init; }
        public string Label { get; init; } = "";
        public string Register { get; init; } = "";
        public string Expression { get; init; } = "";
        public ushort Start { get; init; }
        public ushort End { get; init; }
        public bool Disruptive { get; init; }
    }

    private sealed class WatchList
    {
        public List<WatchItem> Items { get; } = [];
    }
}
