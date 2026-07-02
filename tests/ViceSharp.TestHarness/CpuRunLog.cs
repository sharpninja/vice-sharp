using System.Globalization;

namespace ViceSharp.TestHarness;

/// <summary>
/// One per-cycle CPU-state record of a lockstep run log (TR-LOCKSTEP-VSF-001): the
/// master-cycle counter plus the full 6502 register file at that cycle boundary,
/// captured BEFORE the cycle executes.
/// </summary>
/// <param name="Cycle">Master-cycle counter (0 = the resume point, before any step).</param>
/// <param name="PC">Program counter.</param>
/// <param name="A">Accumulator.</param>
/// <param name="X">X index register.</param>
/// <param name="Y">Y index register.</param>
/// <param name="S">Stack pointer.</param>
/// <param name="P">Processor status flags.</param>
public readonly record struct CpuRunLogEntry(long Cycle, ushort PC, byte A, byte X, byte Y, byte S, byte P);

/// <summary>
/// Deterministic text persistence for per-cycle CPU run logs so a log SAVED from a
/// native VICE run can be reloaded and replayed against the managed core offline
/// (TR-LOCKSTEP-VSF-001 / TR-DET-001). v1 format: first line
/// "# vice-sharp cpu-runlog v1", a "# source: ..." provenance comment, then one
/// "cycle,pc,a,x,y,s,p" line per cycle - every field lowercase invariant hex, LF
/// newlines, UTF-8 without BOM - so identical logs are byte-identical across
/// machines and locales.
/// </summary>
public static class CpuRunLog
{
    /// <summary>Version header written as the first line of every v1 run log.</summary>
    public const string HeaderLine = "# vice-sharp cpu-runlog v1";

    /// <summary>
    /// Saves <paramref name="entries"/> to <paramref name="path"/> in the v1 text
    /// format, overwriting any existing file. <paramref name="source"/> is recorded
    /// in the "# source: ..." provenance comment.
    /// </summary>
    public static void Save(string path, IEnumerable<CpuRunLogEntry> entries, string source = "unspecified")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(entries);

        using var writer = new StreamWriter(path, append: false);
        writer.NewLine = "\n";
        writer.WriteLine(HeaderLine);
        writer.WriteLine($"# source: {source}");
        foreach (var entry in entries)
        {
            writer.WriteLine(string.Create(
                CultureInfo.InvariantCulture,
                $"{entry.Cycle:x},{entry.PC:x4},{entry.A:x2},{entry.X:x2},{entry.Y:x2},{entry.S:x2},{entry.P:x2}"));
        }
    }

    /// <summary>
    /// Loads a v1 run log from <paramref name="path"/>. The first line must be
    /// <see cref="HeaderLine"/>; further "#" lines are comments; every data line
    /// must carry exactly the 7 hex fields cycle,pc,a,x,y,s,p. Throws
    /// <see cref="InvalidDataException"/> (with path and line number) on any
    /// malformed content so a corrupt fixture never silently truncates a run.
    /// </summary>
    public static IReadOnlyList<CpuRunLogEntry> Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var entries = new List<CpuRunLogEntry>();
        var lineNumber = 0;
        var sawHeader = false;
        foreach (var rawLine in File.ReadLines(path))
        {
            lineNumber++;
            var line = rawLine.Trim();
            if (line.Length == 0)
                continue;

            if (line.StartsWith('#'))
            {
                if (!sawHeader)
                {
                    if (!string.Equals(line, HeaderLine, StringComparison.Ordinal))
                        throw new InvalidDataException($"{path}:{lineNumber}: expected header '{HeaderLine}', found '{line}'.");
                    sawHeader = true;
                }

                continue;
            }

            if (!sawHeader)
                throw new InvalidDataException($"{path}:{lineNumber}: data before the '{HeaderLine}' header.");

            var fields = line.Split(',');
            if (fields.Length != 7)
                throw new InvalidDataException($"{path}:{lineNumber}: expected 7 comma-separated hex fields, found {fields.Length}.");

            try
            {
                entries.Add(new CpuRunLogEntry(
                    long.Parse(fields[0], NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                    ushort.Parse(fields[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                    byte.Parse(fields[2], NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                    byte.Parse(fields[3], NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                    byte.Parse(fields[4], NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                    byte.Parse(fields[5], NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                    byte.Parse(fields[6], NumberStyles.HexNumber, CultureInfo.InvariantCulture)));
            }
            catch (Exception ex) when (ex is FormatException or OverflowException)
            {
                throw new InvalidDataException($"{path}:{lineNumber}: malformed hex field in '{line}'.", ex);
            }
        }

        if (!sawHeader)
            throw new InvalidDataException($"{path}: missing '{HeaderLine}' header.");

        return entries;
    }
}
