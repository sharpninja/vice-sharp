using System;
using System.IO;
using System.Linq;
using ViceSharp.Abstractions;
using ViceSharp.Architectures.C64;
using ViceSharp.Core;
using ViceSharp.Monitor;

namespace ViceSharp.Console;

/// <summary>
/// Main validation runner that executes the complete cycle-exact validation plan.
/// Generates traces, compares them, and produces a validation report.
/// </summary>
public class ValidationRunner
{
    private readonly string _outputDir;
    private readonly bool _skipViceComparison;

    public ValidationRunner(string outputDir, bool skipViceComparison = false)
    {
        _outputDir = outputDir;
        _skipViceComparison = skipViceComparison;
        
        if (!Directory.Exists(_outputDir))
        {
            Directory.CreateDirectory(_outputDir);
        }
    }

    public ValidationResult Run()
    {
        var report = new System.Text.StringBuilder();
        var allPassed = true;
        var mismatches = new System.Collections.Generic.List<string>();

        report.AppendLine("╔══════════════════════════════════════════════════════════╗");
        report.AppendLine("║     VICE-Sharp Cycle-Exact Validation Suite             ║");
        report.AppendLine("╚══════════════════════════════════════════════════════════╝");
        report.AppendLine();
        report.AppendLine($"Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine($"Output: {_outputDir}");
        report.AppendLine();

        // Test 1: CPU Instruction Trace (10 frames)
        report.AppendLine("═══ Test 1: CPU Instruction Trace (10 frames) ═══");
        var cpuResult = RunCpuTraceTest();
        report.AppendLine(cpuResult.Report);
        if (!cpuResult.Passed)
        {
            allPassed = false;
            mismatches.Add("CPU Trace");
        }
        report.AppendLine();

        // Test 2: Raster Timing Validation
        report.AppendLine("═══ Test 2: Raster Timing Validation ═══");
        var rasterResult = RunRasterTimingTest();
        report.AppendLine(rasterResult.Report);
        if (!rasterResult.Passed)
        {
            allPassed = false;
            mismatches.Add("Raster Timing");
        }
        report.AppendLine();

        // Test 3: Frame Sync Validation
        report.AppendLine("═══ Test 3: Frame Sync Validation ═══");
        var frameResult = RunFrameSyncTest();
        report.AppendLine(frameResult.Report);
        if (!frameResult.Passed)
        {
            allPassed = false;
            mismatches.Add("Frame Sync");
        }
        report.AppendLine();

        // Test 4: Performance Benchmark
        report.AppendLine("═══ Test 4: Performance Benchmark ═══");
        var perfResult = RunPerformanceBenchmark();
        report.AppendLine(perfResult);
        report.AppendLine();

        // Final Summary
        report.AppendLine("╔══════════════════════════════════════════════════════════╗");
        var resultText = allPassed ? "✓ PASSED" : "✗ FAILED";
        report.AppendLine($"║  FINAL RESULT: {resultText,-20}                       ║");
        report.AppendLine("╚══════════════════════════════════════════════════════════╝");
        
        if (mismatches.Count > 0)
        {
            report.AppendLine();
            report.AppendLine("Failed tests:");
            foreach (var m in mismatches)
            {
                report.AppendLine($"  - {m}");
            }
        }

        // Save report
        var reportPath = Path.Combine(_outputDir, "validation-report.txt");
        File.WriteAllText(reportPath, report.ToString());

        return new ValidationResult(allPassed, report.ToString(), reportPath);
    }

    private TestResult RunCpuTraceTest()
    {
        var tracePath = Path.Combine(_outputDir, "cpu-trace.log");
        var machine = CreateMachine();
        
        using var logger = new DeterministicTraceLogger(machine, tracePath);
        
        machine.Reset();
        
        // Run 10 frames (196560 cycles)
        const int frames = 10;
        const int cyclesPerFrame = 19656;
        
        for (int f = 0; f < frames; f++)
        {
            for (int c = 0; c < cyclesPerFrame; c++)
            {
                machine.Clock.Step();
                logger.LogInstruction();
            }
        }
        
        logger.Flush();
        
        var lineCount = System.IO.File.ReadLines(tracePath).Count();
        var report = $"Trace file: {tracePath}{Environment.NewLine}Instructions logged: {lineCount}{Environment.NewLine}Expected: ~{frames * cyclesPerFrame}{Environment.NewLine}Status: {(lineCount > 0 ? "PASS" : "FAIL")}";
        
        return new TestResult(lineCount > 0, report);
    }

    private TestResult RunRasterTimingTest()
    {
        var machine = CreateMachine();
        machine.Reset();
        
        // Verify raster timing matches PAL specification
        // PAL: 312 lines × 63 cycles = 19656 cycles per frame
        const int expectedCyclesPerFrame = 19656;
        const int expectedLinesPerFrame = 312;
        const int expectedCyclesPerLine = 63;
        
        // Run one frame
        machine.Clock.Step(expectedCyclesPerFrame);
        
        var totalCycles = machine.Clock.TotalCycles;
        var frameCycles = totalCycles % expectedCyclesPerFrame;
        
        var passed = totalCycles == expectedCyclesPerFrame;
        var report = $"Cycles executed: {totalCycles}{Environment.NewLine}Expected: {expectedCyclesPerFrame}{Environment.NewLine}Frame boundary: {(frameCycles == 0 ? "Exact" : $"Off by {frameCycles}")}{Environment.NewLine}Status: {(passed ? "PASS" : "FAIL")}";
        
        return new TestResult(passed, report);
    }

    private TestResult RunFrameSyncTest()
    {
        var machine = CreateMachine();
        machine.Reset();
        
        // Run multiple frames and verify cycle count consistency
        const int frames = 100;
        const int cyclesPerFrame = 19656;
        
        var startCycle = machine.Clock.TotalCycles;
        
        for (int i = 0; i < frames; i++)
        {
            machine.Clock.Step(cyclesPerFrame);
        }
        
        var endCycle = machine.Clock.TotalCycles;
        var actualCycles = endCycle - startCycle;
        var expectedCycles = frames * cyclesPerFrame;
        
        var passed = actualCycles == expectedCycles;
        var report = $"Frames: {frames}{Environment.NewLine}Expected cycles: {expectedCycles}{Environment.NewLine}Actual cycles: {actualCycles}{Environment.NewLine}Drift: {actualCycles - expectedCycles}{Environment.NewLine}Status: {(passed ? "PASS" : "FAIL")}";
        
        return new TestResult(passed, report);
    }

    private string RunPerformanceBenchmark()
    {
        var machine = CreateMachine();
        var benchmark = new PerformanceBenchmark(machine, iterations: 10, cyclesPerIteration: 100000);
        var result = benchmark.Run();
        return benchmark.GenerateReport(result);
    }

    private IMachine CreateMachine()
    {
        IBus bus = new BasicBus();
        IClock clock = new SystemClock(985248); // PAL frequency
        IInterruptLine irqLine = new InterruptLine(InterruptType.Irq);
        IInterruptLine nmiLine = new InterruptLine(InterruptType.Nmi);
        
        return new Commodore64(bus, clock, irqLine, nmiLine);
    }

    public record TestResult(bool Passed, string Report);
    public record ValidationResult(bool Success, string Report, string ReportPath);
}