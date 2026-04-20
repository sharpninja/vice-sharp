using ViceSharp.Abstractions;
using ViceSharp.Core;
using ViceSharp.Architectures.EmptyMachine;
using ViceSharp.Monitor;

Console.WriteLine("ViceSharp - Commodore 64 Debug Monitor");
Console.WriteLine("=====================================");
Console.WriteLine();

// Parse command line args
bool runValidation = args.Contains("--validate");
bool showHelp = args.Contains("--help");
int cycles = 1000000;

for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--cycles" && i + 1 < args.Length)
    {
        cycles = int.Parse(args[++i]);
    }
}

if (showHelp)
{
    Console.WriteLine("Usage: ViceSharp.Console [options]");
    Console.WriteLine("  --validate    Run validation trace");
    Console.WriteLine("  --cycles N     Number of cycles to execute");
    Console.WriteLine("  --memory      Dump memory on exit");
    Console.WriteLine("  --break ADDR   Set breakpoint at address");
    Console.WriteLine();
    Console.WriteLine("Debug commands:");
    Console.WriteLine("  regs       Show CPU registers");
    Console.WriteLine("  mem ADDR   Dump memory from address");
    Console.WriteLine("  step      Step one instruction");
    Console.WriteLine("  run       Run until breakpoint");
    Console.WriteLine("  reset     Reset machine");
    return;
}

// Test Architecture Builder with Empty Machine
Console.WriteLine("Testing Architecture Builder...");

var builder = new ArchitectureBuilder();
var descriptor = new EmptyMachineDescriptor();
var machine = builder.Build(descriptor);

Console.WriteLine($"Successfully built machine: {machine.Architecture.MachineName}");
Console.WriteLine($"Bus initialized");
Console.WriteLine($"Clock: {machine.Clock.FrequencyHz} Hz");
Console.WriteLine($"Device registry contains {machine.Devices.Count} devices");
Console.WriteLine();

// Step clock
if (runValidation)
{
    Console.WriteLine($"Running validation trace for {cycles} cycles...");
    var tracePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"vicesharp_trace_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
    
    using var logger = new DeterministicTraceLogger(machine, tracePath);
    
    for (int i = 0; i < cycles; i++)
    {
        machine.Clock.Step();
        logger.LogInstruction();
        
        if ((i + 1) % 10000 == 0)
        {
            Console.WriteLine($"Progress: {i + 1}/{cycles} cycles");
        }
    }
    
    logger.Flush();
    Console.WriteLine();
    Console.WriteLine($"Trace written to: {tracePath}");
}
else
{
    Console.WriteLine("Running machine test...");
    for (int i = 0; i < cycles; i++)
    {
        machine.Clock.Step();
        
        if (i % 200000 == 0)
        {
            Console.WriteLine($"Executed {i} cycles");
        }
    }
}

Console.WriteLine();
Console.WriteLine($"Emulation completed - Total cycles: {machine.Clock.TotalCycles}");
