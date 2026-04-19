using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ViceSharp.TestHarness;

/// <summary>
/// Compares two trace logs line-by-line and produces a detailed diff report.
/// Compatible with VICE x64sc monitor format and VICE-Sharp deterministic logger format.
/// </summary>
public class TraceComparisonValidator
{
    public record TraceLine(
        int Frame,
        int RasterLine,
        int RasterCycle,
        ushort PC,
        byte A,
        byte X,
        byte Y,
        byte S,
        byte P,
        string FlagString,
        string RawLine
    );

    public record MismatchResult(
        long LineNumber,
        TraceLine Expected,
        TraceLine Actual,
        List<TraceLine> ContextBefore,
        List<TraceLine> ContextAfter
    );

    public record ComparisonResult(
        bool Success,
        long TotalLines,
        long MatchingLines,
        long MismatchedLines,
        List<MismatchResult> Mismatches,
        TimeSpan Duration
    );

    public ComparisonResult Compare(string expectedPath, string actualPath)
    {
        var startTime = DateTime.UtcNow;
        
        var expectedLines = ParseTraceFile(expectedPath);
        var actualLines = ParseTraceFile(actualPath);
        
        var mismatches = new List<MismatchResult>();
        long matchingLines = 0;
        
        var maxLines = Math.Min(expectedLines.Count, actualLines.Count);
        
        for (int i = 0; i < maxLines; i++)
        {
            var exp = expectedLines[i];
            var act = actualLines[i];
            
            if (!TraceLinesMatch(exp, act))
            {
                // Collect context (10 lines before/after)
                var beforeExpected = expectedLines.GetRange(Math.Max(0, i - 10), Math.Min(10, i));
                var beforeActual = actualLines.GetRange(Math.Max(0, i - 10), Math.Min(10, i));
                var afterExpected = expectedLines.GetRange(i + 1, Math.Min(10, expectedLines.Count - i - 1));
                var afterActual = actualLines.GetRange(i + 1, Math.Min(10, actualLines.Count - i - 1));
                
                mismatches.Add(new MismatchResult(
                    i + 1,
                    exp,
                    act,
                    new List<TraceLine>(), // Simplified for now
                    new List<TraceLine>()
                ));
            }
            else
            {
                matchingLines++;
            }
        }
        
        var duration = DateTime.UtcNow - startTime;
        
        return new ComparisonResult(
            Success: mismatches.Count == 0 && expectedLines.Count == actualLines.Count,
            TotalLines: maxLines,
            MatchingLines: matchingLines,
            MismatchedLines: mismatches.Count,
            Mismatches: mismatches,
            Duration: duration
        );
    }

    private bool TraceLinesMatch(TraceLine a, TraceLine b)
    {
        return a.Frame == b.Frame &&
               a.RasterLine == b.RasterLine &&
               a.RasterCycle == b.RasterCycle &&
               a.PC == b.PC &&
               a.A == b.A &&
               a.X == b.X &&
               a.Y == b.Y &&
               a.S == b.S &&
               a.P == b.P;
    }

    private List<TraceLine> ParseTraceFile(string path)
    {
        var lines = new List<TraceLine>();
        
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Trace file not found: {path}", path);
        }
        
        foreach (var line in File.ReadLines(path))
        {
            if (TryParseTraceLine(line, out var traceLine))
            {
                lines.Add(traceLine);
            }
        }
        
        return lines;
    }

    private bool TryParseTraceLine(string line, out TraceLine result)
    {
        result = default!;
        
        // Format: [Frame:Line:Cycle] PC A:XX X:XX Y:XX S:XX P:XX ZNVC:----
        // Example: [00000:001:001] C000 A:00 X:00 Y:00 S:FD P:24 ZNVC:------
        
        if (string.IsNullOrWhiteSpace(line) || !line.StartsWith('['))
            return false;
        
        try
        {
            // Parse frame:line:cycle
            var bracketEnd = line.IndexOf(']');
            if (bracketEnd < 0) return false;
            
            var timingPart = line[1..bracketEnd];
            var parts = timingPart.Split(':');
            if (parts.Length != 3) return false;
            
            var frame = int.Parse(parts[0]);
            var rasterLine = int.Parse(parts[1]);
            var rasterCycle = int.Parse(parts[2]);
            
            // Parse rest of line
            var rest = line[(bracketEnd + 2)..]; // Skip "] "
            var tokens = rest.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            var pc = ushort.Parse(tokens[0], System.Globalization.NumberStyles.HexNumber);
            
            byte a = 0, x = 0, y = 0, s = 0, p = 0;
            string flags = "";
            
            for (int i = 1; i < tokens.Length; i++)
            {
                var token = tokens[i];
                if (token.StartsWith("A:"))
                    a = byte.Parse(token[2..], System.Globalization.NumberStyles.HexNumber);
                else if (token.StartsWith("X:"))
                    x = byte.Parse(token[2..], System.Globalization.NumberStyles.HexNumber);
                else if (token.StartsWith("Y:"))
                    y = byte.Parse(token[2..], System.Globalization.NumberStyles.HexNumber);
                else if (token.StartsWith("S:"))
                    s = byte.Parse(token[2..], System.Globalization.NumberStyles.HexNumber);
                else if (token.StartsWith("P:"))
                    p = byte.Parse(token[2..], System.Globalization.NumberStyles.HexNumber);
                else if (token.StartsWith("ZNVC:"))
                    flags = token[5..];
            }
            
            result = new TraceLine(frame, rasterLine, rasterCycle, pc, a, x, y, s, p, flags, line);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public string GenerateReport(ComparisonResult result, string expectedPath, string actualPath)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("=== VICE-Sharp Cycle-Exact Validation Report ===");
        sb.AppendLine();
        sb.AppendLine($"Expected trace: {expectedPath}");
        sb.AppendLine($"Actual trace:   {actualPath}");
        sb.AppendLine();
        sb.AppendLine($"Total lines:    {result.TotalLines}");
        sb.AppendLine($"Matching:       {result.MatchingLines}");
        sb.AppendLine($"Mismatched:     {result.MismatchedLines}");
        sb.AppendLine($"Match rate:     {(result.TotalLines > 0 ? (result.MatchingLines * 100.0 / result.TotalLines).ToString("F2") : "N/A")}%");
        sb.AppendLine($"Duration:       {result.Duration.TotalSeconds:F3}s");
        sb.AppendLine();
        
        if (result.Success)
        {
            sb.AppendLine("✓ VALIDATION PASSED - Cycle-exact match confirmed!");
        }
        else
        {
            sb.AppendLine("✗ VALIDATION FAILED - Mismatches detected:");
            sb.AppendLine();
            
            foreach (var mismatch in result.Mismatches)
            {
                sb.AppendLine($"  Mismatch at line {mismatch.LineNumber}:");
                sb.AppendLine($"    Expected: {mismatch.Expected.RawLine}");
                sb.AppendLine($"    Actual:   {mismatch.Actual.RawLine}");
                sb.AppendLine($"    PC: ${mismatch.Expected.PC:X4} vs ${mismatch.Actual.PC:X4}");
                sb.AppendLine($"    A: ${mismatch.Expected.A:X2} vs ${mismatch.Actual.A:X2}");
                sb.AppendLine($"    X: ${mismatch.Expected.X:X2} vs ${mismatch.Actual.X:X2}");
                sb.AppendLine($"    Y: ${mismatch.Expected.Y:X2} vs ${mismatch.Actual.Y:X2}");
                sb.AppendLine($"    S: ${mismatch.Expected.S:X2} vs ${mismatch.Actual.S:X2}");
                sb.AppendLine($"    P: ${mismatch.Expected.P:X2} vs ${mismatch.Actual.P:X2}");
                sb.AppendLine();
            }
        }
        
        return sb.ToString();
    }
}