using System.Globalization;
using System.Text;

namespace ViceSharp.Chips.Input;

public static class C64VkmParser
{
    public static C64VkmParseResult Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var builder = new C64KeyboardMap.Builder();
        var diagnostics = new List<C64VkmDiagnostic>();
        var state = new ParserState(builder, diagnostics);
        var fullPath = Path.GetFullPath(path);

        ParseFile(fullPath, state, new HashSet<string>(StringComparer.OrdinalIgnoreCase));

        var mapName = Path.GetFileName(fullPath);
        return new C64VkmParseResult(builder.Build(mapName), diagnostics);
    }

    private static void ParseFile(string path, ParserState state, HashSet<string> includeStack)
    {
        if (!includeStack.Add(path))
        {
            state.AddDiagnostic(C64VkmDiagnosticSeverity.Error, $"VKM include cycle detected for '{path}'.", path, 0);
            return;
        }

        if (!File.Exists(path))
        {
            state.AddDiagnostic(C64VkmDiagnosticSeverity.Error, $"VKM file '{path}' does not exist.", path, 0);
            includeStack.Remove(path);
            return;
        }

        var directory = Path.GetDirectoryName(path) ?? Directory.GetCurrentDirectory();
        var inBlockComment = false;
        var lines = File.ReadAllLines(path);

        for (var index = 0; index < lines.Length; index++)
        {
            var lineNumber = index + 1;
            var line = StripComments(lines[index], ref inBlockComment).Trim();
            if (line.Length == 0)
                continue;

            var tokens = line.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0)
                continue;

            if (tokens[0].StartsWith('!'))
                ParseDirective(tokens, path, directory, lineNumber, state, includeStack);
            else
                ParseMapping(tokens, path, lineNumber, state);
        }

        includeStack.Remove(path);
    }

    private static void ParseDirective(
        string[] tokens,
        string path,
        string directory,
        int lineNumber,
        ParserState state,
        HashSet<string> includeStack)
    {
        switch (tokens[0].ToUpperInvariant())
        {
            case "!CLEAR":
                state.Builder.Clear();
                break;

            case "!INCLUDE":
                if (tokens.Length < 2)
                {
                    state.AddDiagnostic(C64VkmDiagnosticSeverity.Error, "!INCLUDE requires a filename.", path, lineNumber);
                    return;
                }

                ParseFile(Path.GetFullPath(Path.Combine(directory, tokens[1])), state, includeStack);
                break;

            case "!UNDEF":
                if (tokens.Length < 2)
                {
                    state.AddDiagnostic(C64VkmDiagnosticSeverity.Error, "!UNDEF requires a keysym.", path, lineNumber);
                    return;
                }

                state.Builder.Remove(tokens[1]);
                break;

            case "!LSHIFT":
                ParseModifierLocation(tokens, path, lineNumber, state, keyCode => state.LeftShift = keyCode);
                break;

            case "!RSHIFT":
                ParseModifierLocation(tokens, path, lineNumber, state, keyCode => state.RightShift = keyCode);
                break;

            case "!LCTRL":
                ParseModifierLocation(tokens, path, lineNumber, state, keyCode => state.LeftCtrl = keyCode);
                break;

            case "!LCBM":
                ParseModifierLocation(tokens, path, lineNumber, state, keyCode => state.LeftCbm = keyCode);
                break;

            case "!VSHIFT":
                ParseVirtualModifier(tokens, path, lineNumber, state, keyName => state.VirtualShift = keyName);
                break;

            case "!VCTRL":
                ParseVirtualModifier(tokens, path, lineNumber, state, keyName => state.VirtualCtrl = keyName);
                break;

            case "!VCBM":
                ParseVirtualModifier(tokens, path, lineNumber, state, keyName => state.VirtualCbm = keyName);
                break;

            case "!SHIFTL":
                ParseVirtualModifier(tokens, path, lineNumber, state, keyName => state.ShiftLock = keyName);
                break;

            default:
                state.AddDiagnostic(
                    C64VkmDiagnosticSeverity.Info,
                    $"Ignored unsupported VKM directive '{tokens[0]}'.",
                    path,
                    lineNumber);
                break;
        }
    }

    private static void ParseMapping(string[] tokens, string path, int lineNumber, ParserState state)
    {
        if (tokens.Length < 3)
        {
            state.AddDiagnostic(C64VkmDiagnosticSeverity.Warning, "Ignored incomplete VKM mapping line.", path, lineNumber);
            return;
        }

        var key = tokens[0];
        if (!TryParseInt(tokens[1], out var row) || !TryParseInt(tokens[2], out var column))
        {
            state.AddDiagnostic(C64VkmDiagnosticSeverity.Warning, $"Ignored VKM mapping for '{key}' with invalid row or column.", path, lineNumber);
            return;
        }

        if (row < 0)
        {
            var message = row is -1 or -2 or -5
                ? $"Ignored joystick/keypad VKM pseudo entry '{key}' with row {row}."
                : $"Ignored unsupported VKM pseudo entry '{key}' with row {row}.";
            state.AddDiagnostic(C64VkmDiagnosticSeverity.Warning, message, path, lineNumber);
            return;
        }

        if (row > 7 || column is < 0 or > 7)
        {
            state.AddDiagnostic(C64VkmDiagnosticSeverity.Warning, $"Ignored VKM mapping for '{key}' outside the C64 8x8 matrix.", path, lineNumber);
            return;
        }

        var shiftFlags = 0;
        if (tokens.Length >= 4 && !TryParseInt(tokens[3], out shiftFlags))
        {
            state.AddDiagnostic(C64VkmDiagnosticSeverity.Warning, $"Ignored VKM mapping for '{key}' with invalid shift flags.", path, lineNumber);
            return;
        }

        var primaryKeyCode = (byte)((row << 3) | column);
        var keyCodes = BuildKeySequence(primaryKeyCode, shiftFlags, state);
        state.Builder.Add(new C64KeyboardMapEntry(
            key,
            (byte)row,
            (byte)column,
            shiftFlags,
            keyCodes));
    }

    private static IReadOnlyList<byte> BuildKeySequence(byte primaryKeyCode, int shiftFlags, ParserState state)
    {
        var keyCodes = new List<byte>(4);

        if ((shiftFlags & C64VkmShiftFlags.CombineWithShift) != 0)
            AddDistinct(keyCodes, state.ResolveModifier(state.VirtualShift));

        if ((shiftFlags & C64VkmShiftFlags.CombineWithCbm) != 0)
            AddDistinct(keyCodes, state.ResolveModifier(state.VirtualCbm));

        if ((shiftFlags & C64VkmShiftFlags.CombineWithCtrl) != 0)
            AddDistinct(keyCodes, state.ResolveModifier(state.VirtualCtrl));

        AddDistinct(keyCodes, primaryKeyCode);
        return keyCodes.ToArray();
    }

    private static void AddDistinct(List<byte> keyCodes, byte keyCode)
    {
        if (!keyCodes.Contains(keyCode))
            keyCodes.Add(keyCode);
    }

    private static void ParseModifierLocation(
        string[] tokens,
        string path,
        int lineNumber,
        ParserState state,
        Action<byte> setKeyCode)
    {
        if (tokens.Length < 3 ||
            !TryParseInt(tokens[1], out var row) ||
            !TryParseInt(tokens[2], out var column) ||
            row is < 0 or > 7 ||
            column is < 0 or > 7)
        {
            state.AddDiagnostic(C64VkmDiagnosticSeverity.Error, $"{tokens[0]} requires row and column values in the C64 8x8 matrix.", path, lineNumber);
            return;
        }

        setKeyCode((byte)((row << 3) | column));
    }

    private static void ParseVirtualModifier(
        string[] tokens,
        string path,
        int lineNumber,
        ParserState state,
        Action<string> setKeyName)
    {
        if (tokens.Length < 2)
        {
            state.AddDiagnostic(C64VkmDiagnosticSeverity.Error, $"{tokens[0]} requires a modifier key name.", path, lineNumber);
            return;
        }

        setKeyName(tokens[1]);
    }

    private static bool TryParseInt(string token, out int value)
    {
        if (token.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return int.TryParse(
                token[2..],
                NumberStyles.HexNumber,
                CultureInfo.InvariantCulture,
                out value);
        }

        return int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    private static string StripComments(string line, ref bool inBlockComment)
    {
        var result = new StringBuilder(line.Length);
        var index = 0;

        while (index < line.Length)
        {
            if (inBlockComment)
            {
                var end = line.IndexOf("*/", index, StringComparison.Ordinal);
                if (end < 0)
                    return result.ToString();

                inBlockComment = false;
                index = end + 2;
                continue;
            }

            var hash = line.IndexOf('#', index);
            var block = line.IndexOf("/*", index, StringComparison.Ordinal);

            if (hash >= 0 && (block < 0 || hash < block))
            {
                result.Append(line, index, hash - index);
                break;
            }

            if (block >= 0)
            {
                result.Append(line, index, block - index);
                inBlockComment = true;
                index = block + 2;
                continue;
            }

            result.Append(line, index, line.Length - index);
            break;
        }

        return result.ToString();
    }

    private sealed class ParserState
    {
        public ParserState(C64KeyboardMap.Builder builder, List<C64VkmDiagnostic> diagnostics)
        {
            Builder = builder;
            Diagnostics = diagnostics;
        }

        public C64KeyboardMap.Builder Builder { get; }

        public List<C64VkmDiagnostic> Diagnostics { get; }

        public byte LeftShift { get; set; } = 0x0F;

        public byte RightShift { get; set; } = 0x34;

        public byte LeftCtrl { get; set; } = 0x3A;

        public byte LeftCbm { get; set; } = 0x3D;

        public string VirtualShift { get; set; } = "RSHIFT";

        public string VirtualCtrl { get; set; } = "LCTRL";

        public string VirtualCbm { get; set; } = "LCBM";

        public string ShiftLock { get; set; } = "LSHIFT";

        public byte ResolveModifier(string keyName)
        {
            return keyName.ToUpperInvariant() switch
            {
                "LSHIFT" => LeftShift,
                "RSHIFT" => RightShift,
                "LCTRL" => LeftCtrl,
                "LCBM" => LeftCbm,
                _ => RightShift
            };
        }

        public void AddDiagnostic(C64VkmDiagnosticSeverity severity, string message, string? path, int lineNumber)
        {
            Diagnostics.Add(new C64VkmDiagnostic(severity, message, path, lineNumber));
        }
    }
}
