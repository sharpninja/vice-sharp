using ViceSharp.Abstractions;
using ViceSharp.Core;
using ViceSharp.Architectures.EmptyMachine;
using ViceSharp.Monitor;

Console.WriteLine("ViceSharp - Commodore Emulator");
Console.WriteLine("==============================");
Console.WriteLine();

// Parse command line args
bool runValidation = args.Contains("--validate");
int cycles = 1000000;

for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--cycles" && i + 1 < args.Length)
    {
        cycles = int.Parse(args[++i]);
    }
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
