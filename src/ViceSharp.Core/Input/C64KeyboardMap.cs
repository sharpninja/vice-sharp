using ViceSharp.Abstractions;

namespace ViceSharp.Core.Input;

public sealed class C64KeyboardMap : IKeyboardInputMap
{
    private const byte LeftShift = 0x0F;
    private const byte CursorRight = 0x02;
    private const byte CursorDown = 0x07;

    private static readonly byte[] NoKeys = [];

    private static readonly IReadOnlyDictionary<string, string[]> HostKeyAliases =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["Add"] = ["plus"],
            ["ArrowDown"] = ["Down"],
            ["ArrowLeft"] = ["Left"],
            ["ArrowRight"] = ["Right"],
            ["ArrowUp"] = ["Up"],
            ["At"] = ["at"],
            ["Asterisk"] = ["asterisk"],
            ["Back"] = ["BackSpace", "Backspace"],
            ["Backspace"] = ["BackSpace"],
            ["Cbm"] = ["Control_L"],
            ["Colon"] = ["colon"],
            ["Comma"] = ["comma"],
            ["Commodore"] = ["Control_L"],
            ["Control"] = ["Control_L"],
            ["Ctrl"] = ["Control_L"],
            ["Decimal"] = ["period"],
            ["Del"] = ["Delete"],
            ["Divide"] = ["slash"],
            ["Down"] = ["Down"],
            ["End"] = ["End"],
            ["Enter"] = ["Return"],
            ["Equals"] = ["equal"],
            ["Escape"] = ["Escape"],
            ["LeftAlt"] = ["Control_L"],
            ["LeftArrow"] = ["grave", "asciitilde"],
            ["LeftCtrl"] = ["Control_L"],
            ["LeftShift"] = ["Shift_L"],
            ["Minus"] = ["minus"],
            ["Multiply"] = ["asterisk"],
            ["Oem1"] = ["semicolon"],
            ["Oem2"] = ["slash"],
            ["Oem3"] = ["grave"],
            ["Oem7"] = ["quotedbl", "apostrophe"],
            ["OemComma"] = ["comma"],
            ["OemMinus"] = ["minus"],
            ["OemPeriod"] = ["period"],
            ["OemPlus"] = ["equal", "plus"],
            ["OemQuestion"] = ["slash", "question"],
            ["OemQuotes"] = ["quotedbl", "apostrophe"],
            ["OemSemicolon"] = ["semicolon"],
            ["OemTilde"] = ["grave"],
            ["PageDown"] = ["Page_Down"],
            ["PageUp"] = ["Page_Up", "Prior"],
            ["Period"] = ["period"],
            ["Plus"] = ["plus"],
            ["Pound"] = ["sterling"],
            ["PoundSign"] = ["sterling"],
            ["Quote"] = ["quotedbl", "apostrophe"],
            ["Quotes"] = ["quotedbl", "apostrophe"],
            ["Return"] = ["Return"],
            ["RightAlt"] = ["Control_L"],
            ["RightCtrl"] = ["Control_R", "Control_L"],
            ["RightShift"] = ["Shift_R"],
            ["Run/Stop"] = ["Escape"],
            ["RunStop"] = ["Escape"],
            ["Separator"] = ["comma"],
            ["Semicolon"] = ["semicolon"],
            ["Shift"] = ["Shift_L"],
            ["Slash"] = ["slash"],
            ["Space"] = ["space"],
            ["Subtract"] = ["minus"],
            ["UpArrow"] = ["backslash", "bar"]
        };

    private static readonly IReadOnlyDictionary<string, string[]> SymbolAliases =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["!"] = ["exclam"],
            ["\""] = ["quotedbl"],
            ["#"] = ["numbersign"],
            ["$"] = ["dollar"],
            ["%"] = ["percent"],
            ["&"] = ["ampersand"],
            ["'"] = ["apostrophe"],
            ["("] = ["parenleft"],
            [")"] = ["parenright"],
            ["*"] = ["asterisk"],
            ["+"] = ["plus"],
            [","] = ["comma"],
            ["-"] = ["minus"],
            ["."] = ["period"],
            ["/"] = ["slash"],
            [":"] = ["colon"],
            [";"] = ["semicolon"],
            ["<"] = ["less"],
            ["="] = ["equal"],
            [">"] = ["greater"],
            ["?"] = ["question"],
            ["@"] = ["at"],
            ["["] = ["bracketleft"],
            ["\\"] = ["backslash"],
            ["]"] = ["bracketright"],
            ["^"] = ["asciicircum"],
            ["_"] = ["underscore"],
            ["`"] = ["grave"],
            ["{"] = ["braceleft"],
            ["|"] = ["bar"],
            ["}"] = ["braceright"],
            ["~"] = ["asciitilde"]
        };

    private readonly IReadOnlyDictionary<string, C64KeyboardMapEntry[]> _entries;

    internal C64KeyboardMap(string name, IDictionary<string, List<C64KeyboardMapEntry>> entries)
    {
        Name = string.IsNullOrWhiteSpace(name) ? "C64 keyboard map" : name;
        _entries = entries.ToDictionary(
            entry => entry.Key,
            entry => entry.Value.ToArray(),
            StringComparer.OrdinalIgnoreCase);
    }

    public string Name { get; }

    public int Count => _entries.Count;

    public bool TryResolve(string key, out IReadOnlyList<byte> keyCodes)
    {
        keyCodes = NoKeys;
        if (string.IsNullOrWhiteSpace(key))
            return false;

        var trimmed = key.Trim();
        if (TryResolveCore(trimmed, out keyCodes))
            return true;

        foreach (var alias in EnumerateAliases(trimmed))
        {
            if (TryResolveCore(alias, out keyCodes))
                return true;
        }

        keyCodes = NoKeys;
        return false;
    }

    public static C64KeyboardMap CreateDefaultFallback()
    {
        var builder = new Builder();

        builder.Add("Back", [0x00]);
        builder.Add("Backspace", [0x00]);
        builder.Add("Delete", [0x00]);
        builder.Add("Del", [0x00]);
        builder.Add("Enter", [0x01]);
        builder.Add("Return", [0x01]);
        builder.Add("Right", [CursorRight]);
        builder.Add("ArrowRight", [CursorRight]);
        builder.Add("Left", [LeftShift, CursorRight]);
        builder.Add("ArrowLeft", [LeftShift, CursorRight]);
        builder.Add("Down", [CursorDown]);
        builder.Add("ArrowDown", [CursorDown]);
        builder.Add("Up", [LeftShift, CursorDown]);
        builder.Add("ArrowUp", [LeftShift, CursorDown]);
        builder.Add("F1", [0x04]);
        builder.Add("F2", [LeftShift, 0x04]);
        builder.Add("F3", [0x05]);
        builder.Add("F4", [LeftShift, 0x05]);
        builder.Add("F5", [0x06]);
        builder.Add("F6", [LeftShift, 0x06]);
        builder.Add("F7", [0x03]);
        builder.Add("F8", [LeftShift, 0x03]);
        builder.Add("LeftShift", [LeftShift]);
        builder.Add("RightShift", [0x34]);
        builder.Add("Shift", [LeftShift]);
        builder.Add("LeftCtrl", [0x3A]);
        builder.Add("RightCtrl", [0x3A]);
        builder.Add("Ctrl", [0x3A]);
        builder.Add("Control", [0x3A]);
        builder.Add("Space", [0x3C]);
        builder.Add("Home", [0x33]);
        builder.Add("Escape", [0x3F]);
        builder.Add("RunStop", [0x3F]);
        builder.Add("Run/Stop", [0x3F]);
        builder.Add("Commodore", [0x3D]);
        builder.Add("Cbm", [0x3D]);
        builder.Add("LeftAlt", [0x3D]);
        builder.Add("RightAlt", [0x3D]);
        builder.Add("+", [0x28]);
        builder.Add("Plus", [0x28]);
        builder.Add("Add", [0x28]);
        builder.Add("=", [0x35]);
        builder.Add("Equals", [0x35]);
        builder.Add("OemPlus", [0x35]);
        builder.Add("-", [0x2B]);
        builder.Add("Minus", [0x2B]);
        builder.Add("Subtract", [0x2B]);
        builder.Add("OemMinus", [0x2B]);
        builder.Add(".", [0x2C]);
        builder.Add("Period", [0x2C]);
        builder.Add("Decimal", [0x2C]);
        builder.Add("OemPeriod", [0x2C]);
        builder.Add(",", [0x2F]);
        builder.Add("Comma", [0x2F]);
        builder.Add("Separator", [0x2F]);
        builder.Add("OemComma", [0x2F]);
        builder.Add(":", [0x2D]);
        builder.Add("Colon", [0x2D]);
        builder.Add(";", [0x32]);
        builder.Add("Semicolon", [0x32]);
        builder.Add("OemSemicolon", [0x32]);
        builder.Add("Oem1", [0x32]);
        builder.Add("@", [0x2E]);
        builder.Add("At", [0x2E]);
        builder.Add("*", [0x31]);
        builder.Add("Asterisk", [0x31]);
        builder.Add("Multiply", [0x31]);
        builder.Add("/", [0x37]);
        builder.Add("Slash", [0x37]);
        builder.Add("Divide", [0x37]);
        builder.Add("OemQuestion", [0x37]);
        builder.Add("Oem2", [0x37]);
        builder.Add("?", [LeftShift, 0x37]);
        builder.Add("\"", [LeftShift, 0x3B]);
        builder.Add("Quote", [LeftShift, 0x3B]);
        builder.Add("Quotes", [LeftShift, 0x3B]);
        builder.Add("OemQuotes", [LeftShift, 0x3B]);
        builder.Add("Oem7", [LeftShift, 0x3B]);
        builder.Add("Pound", [0x30]);
        builder.Add("PoundSign", [0x30]);
        builder.Add("LeftArrow", [0x39]);
        builder.Add("OemTilde", [0x39]);
        builder.Add("Oem3", [0x39]);
        builder.Add("UpArrow", [0x36]);

        builder.Add("A", [0x0A]);
        builder.Add("B", [0x1C]);
        builder.Add("C", [0x14]);
        builder.Add("D", [0x12]);
        builder.Add("E", [0x0E]);
        builder.Add("F", [0x15]);
        builder.Add("G", [0x1A]);
        builder.Add("H", [0x1D]);
        builder.Add("I", [0x21]);
        builder.Add("J", [0x22]);
        builder.Add("K", [0x25]);
        builder.Add("L", [0x2A]);
        builder.Add("M", [0x24]);
        builder.Add("N", [0x27]);
        builder.Add("O", [0x26]);
        builder.Add("P", [0x29]);
        builder.Add("Q", [0x3E]);
        builder.Add("R", [0x11]);
        builder.Add("S", [0x0D]);
        builder.Add("T", [0x16]);
        builder.Add("U", [0x1E]);
        builder.Add("V", [0x1F]);
        builder.Add("W", [0x09]);
        builder.Add("X", [0x17]);
        builder.Add("Y", [0x19]);
        builder.Add("Z", [0x0C]);
        builder.Add("0", [0x23]);
        builder.Add("1", [0x38]);
        builder.Add("2", [0x3B]);
        builder.Add("3", [0x08]);
        builder.Add("4", [0x0B]);
        builder.Add("5", [0x10]);
        builder.Add("6", [0x13]);
        builder.Add("7", [0x18]);
        builder.Add("8", [0x1B]);
        builder.Add("9", [0x20]);

        return builder.Build("C64 host fallback keyboard map");
    }

    private bool TryResolveCore(string key, out IReadOnlyList<byte> keyCodes)
    {
        if (_entries.TryGetValue(key, out var entries) && entries.Length > 0)
        {
            keyCodes = entries[0].KeyCodes;
            return true;
        }

        keyCodes = NoKeys;
        return false;
    }

    private static IEnumerable<string> EnumerateAliases(string key)
    {
        var yielded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (HostKeyAliases.TryGetValue(key, out var aliases))
        {
            foreach (var alias in aliases)
            {
                if (yielded.Add(alias))
                    yield return alias;
            }
        }

        if (TryGetDigitAlias(key, out var digitAlias) && yielded.Add(digitAlias))
            yield return digitAlias;

        if (key.Length == 1)
        {
            if (SymbolAliases.TryGetValue(key, out var symbolAliases))
            {
                foreach (var alias in symbolAliases)
                {
                    if (yielded.Add(alias))
                        yield return alias;
                }
            }

            if (char.IsLetterOrDigit(key[0]))
            {
                var upper = char.ToUpperInvariant(key[0]).ToString();
                if (yielded.Add(upper))
                    yield return upper;
            }
        }
    }

    private static bool TryGetDigitAlias(string key, out string digit)
    {
        digit = string.Empty;

        if (key.Length == 2 &&
            key[0] is 'D' or 'd' &&
            char.IsDigit(key[1]))
        {
            digit = key[1].ToString();
            return true;
        }

        if (key.Length == 7 &&
            key.StartsWith("NumPad", StringComparison.OrdinalIgnoreCase) &&
            char.IsDigit(key[6]))
        {
            digit = key[6].ToString();
            return true;
        }

        if (key.Length == 7 &&
            key.StartsWith("Numpad", StringComparison.OrdinalIgnoreCase) &&
            char.IsDigit(key[6]))
        {
            digit = key[6].ToString();
            return true;
        }

        return false;
    }

    internal sealed class Builder
    {
        private readonly Dictionary<string, List<C64KeyboardMapEntry>> _entries = new(StringComparer.OrdinalIgnoreCase);

        public void Clear()
        {
            _entries.Clear();
        }

        public void Remove(string key)
        {
            _entries.Remove(key);
        }

        public void Add(string key, IReadOnlyList<byte> keyCodes, int shiftFlags = 0)
        {
            if (keyCodes.Count == 0)
                return;

            var primary = keyCodes[keyCodes.Count - 1];
            Add(new C64KeyboardMapEntry(
                key,
                (byte)(primary >> 3),
                (byte)(primary & 0x07),
                shiftFlags,
                keyCodes.ToArray()));
        }

        public void Add(C64KeyboardMapEntry entry)
        {
            if (!_entries.TryGetValue(entry.Key, out var entries))
            {
                entries = [];
                _entries[entry.Key] = entries;
            }

            if ((entry.ShiftFlags & C64VkmShiftFlags.AnotherDefinitionFollows) == 0 &&
                entries.All(existing => (existing.ShiftFlags & C64VkmShiftFlags.AnotherDefinitionFollows) == 0))
            {
                entries.Clear();
            }

            entries.Add(entry);
        }

        public C64KeyboardMap Build(string name)
        {
            return new C64KeyboardMap(name, _entries);
        }
    }
}

public sealed record C64KeyboardMapEntry(
    string Key,
    byte Row,
    byte Column,
    int ShiftFlags,
    IReadOnlyList<byte> KeyCodes);
