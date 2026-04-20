using ViceSharp.Architectures;
using ViceSharp.Core;

namespace ViceSharp.TestHarness;

/// <summary>
/// VICE-style trace logger for cycle-accurate comparison
/// Output format: [F:000 L:001 C:01] PC:A000 A:00 X:00 Y:00 S:FD P:24
/// </summary>
public static class C64TraceLogger
{
    /// <summary>
    /// Generate trace file for specified number of cycles
    /// </summary>
    public static void GenerateTrace(string outputPath, int maxCycles)
    {
        var builder = new ArchitectureBuilder();
        using var machine = builder.Build(new C64NtscDescriptor());
        machine.Reset();
        
        using var writer = new StreamWriter(outputPath);
        
        long cycleCount = 0;
        int frame = 0;
        
        while (cycleCount < maxCycles)
        {
            var state = machine.GetState();
            
            // Format: [F:000 L:001 C:01] PC:A000 A:00 X:00 Y:00 S:FD P:24
            string line = $"[F:{frame:D3} L:{(cycleCount % 312):D3} C:{(cycleCount % 63):D2}] " +
                         $"PC:{state.PC:X4} A:{state.A:X2} X:{state.X:X2} Y:{state.Y:X2} " +
                         $"S:{state.S:X2} P:{state.P:X2}";
            
            writer.WriteLine(line);
            
            machine.StepInstruction();
            cycleCount++;
            
            if (cycleCount % 19656 == 0) frame++;
        }
        
        Console.WriteLine($"Trace generated: {cycleCount} cycles, {frame} frames");
        Console.WriteLine($"Output: {outputPath}");
    }
}
