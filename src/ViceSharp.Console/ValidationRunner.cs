// This file is part of ViceSharp.
// Copyright (C) 2026 ViceSharp Contributors
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License along
// with this program; if not, write to the Free Software Foundation, Inc.,
// 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.

using ViceSharp.Abstractions;
using ViceSharp.Core;
using ViceSharp.Monitor;

namespace ViceSharp.Console;

/// <summary>
/// Console runner for cycle-exact validation against VICE reference traces.
/// </summary>
public static class ValidationRunner
{
    private const int DefaultTraceCycles = 10000;
    private const int FramesToRun = 10;

    public static int RunValidation(string? expectedTracePath = null, int cycles = DefaultTraceCycles)
    {
        System.Console.WriteLine("=== VICE-Sharp Cycle-Exact Validation Runner ===");
        System.Console.WriteLine();

        // Create a basic C64-like machine for testing
        var machine = CreateTestMachine();
        
        System.Console.WriteLine($"Machine: {machine.Architecture.MachineName}");
        System.Console.WriteLine($"Clock: {machine.Clock.FrequencyHz} Hz");
        System.Console.WriteLine($"Running {cycles} cycles ({FramesToRun} frames)...");
        System.Console.WriteLine();

        // Run emulation and capture trace
        var tracePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"vicesharp_trace_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
        
        using var logger = new DeterministicTraceLogger(machine, tracePath);
        
        var startTime = DateTime.UtcNow;
        
        for (int i = 0; i < cycles; i++)
        {
            machine.Clock.Step();
            
            // Log every instruction
            logger.LogInstruction();
            
            if ((i + 1) % 1000 == 0)
            {
                System.Console.Write($"\rProgress: {i + 1}/{cycles} cycles ({100.0 * (i + 1) / cycles:F1}%)");
            }
        }
        
        logger.Flush();
        
        var duration = DateTime.UtcNow - startTime;
        System.Console.WriteLine();
        System.Console.WriteLine();
        System.Console.WriteLine($"Completed in {duration.TotalSeconds:F3}s");
        System.Console.WriteLine($"Trace written to: {tracePath}");
        System.Console.WriteLine();

        // Compare with expected trace if provided
        if (!string.IsNullOrEmpty(expectedTracePath) && System.IO.File.Exists(expectedTracePath))
        {
            System.Console.WriteLine("Comparing with expected trace...");
            System.Console.WriteLine();
            
            // Simple line-by-line comparison
            var result = CompareTraces(expectedTracePath, tracePath);
            
            if (result.Success)
            {
                System.Console.WriteLine("VALIDATION PASSED");
                System.Console.WriteLine($"  Total lines: {result.TotalLines}");
                System.Console.WriteLine($"  Match rate: {result.MatchRate:F2}%");
                return 0;
            }
            else
            {
                System.Console.WriteLine("VALIDATION FAILED");
                System.Console.WriteLine($"  Total lines: {result.TotalLines}");
                System.Console.WriteLine($"  Matching: {result.MatchingLines}");
                System.Console.WriteLine($"  Mismatched: {result.MismatchedLines}");
                System.Console.WriteLine($"  First mismatch at line: {result.FirstMismatchLine}");
                return 1;
            }
        }

        System.Console.WriteLine("No expected trace provided. Trace file ready for manual comparison.");
        System.Console.WriteLine();
        
        // Output sample of trace
        System.Console.WriteLine("=== Sample Trace (first 20 lines) ===");
        using (var reader = new System.IO.StreamReader(tracePath))
        {
            for (int i = 0; i < 20 && !reader.EndOfStream; i++)
            {
                System.Console.WriteLine(reader.ReadLine());
            }
        }
        
        return 0;
    }

    private record ComparisonResult(
        bool Success,
        long TotalLines,
        long MatchingLines,
        long MismatchedLines,
        double MatchRate,
        int FirstMismatchLine
    );

    private static ComparisonResult CompareTraces(string expectedPath, string actualPath)
    {
        var expectedLines = System.IO.File.ReadLines(expectedPath).ToList();
        var actualLines = System.IO.File.ReadLines(actualPath).ToList();
        
        long matching = 0;
        int firstMismatch = -1;
        
        var minLines = System.Math.Min(expectedLines.Count, actualLines.Count);
        
        for (int i = 0; i < minLines; i++)
        {
            if (expectedLines[i] == actualLines[i])
            {
                matching++;
            }
            else if (firstMismatch < 0)
            {
                firstMismatch = i + 1;
            }
        }
        
        var total = System.Math.Max(expectedLines.Count, actualLines.Count);
        var rate = total > 0 ? 100.0 * matching / minLines : 0;
        
        return new ComparisonResult(
            Success: matching == minLines && expectedLines.Count == actualLines.Count,
            TotalLines: total,
            MatchingLines: matching,
            MismatchedLines: minLines - matching,
            MatchRate: rate,
            FirstMismatchLine: firstMismatch
        );
    }

    private static IMachine CreateTestMachine()
    {
        var builder = new ArchitectureBuilder();
        
        // Use EmptyMachineDescriptor for testing - creates minimal machine
        var descriptor = new Architectures.EmptyMachine.EmptyMachineDescriptor();
        
        return builder.Build(descriptor);
    }
}
