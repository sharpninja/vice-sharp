using ViceSharp.Abstractions;
using ViceSharp.Architectures.C64;
using ViceSharp.Core;
using ViceSharp.Monitor;
using ViceSharp.RomFetch;

Console.WriteLine("ViceSharp - Commodore 64 Debug Monitor");
Console.WriteLine("=====================================");
Console.WriteLine();

// Parse command line args
int cycles = 100000;
string? traceFile = null;
string romPath = "roms";

for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--cycles" && i + 1 < args.Length)
    {
        cycles = int.Parse(args[++i]);
    }
    else if (args[i] == "--trace" && i + 1 < args.Length)
    {
        traceFile = args[++i];
    }
    else if (args[i] == "--roms" && i + 1 < args.Length)
    {
        romPath = args[++i];
    }
}

Console.WriteLine($"Building C64 emulation...");

// Create ROM provider
var romProvider = new RomProvider(romPath);
var builder = new ArchitectureBuilder(romProvider);
var descriptor = new C64Descriptor();

IMachine machine;
machine = builder.Build(descriptor);

Console.WriteLine($"Machine: {machine.Architecture.MachineName}");
Console.WriteLine($"Clock: {machine.Clock.FrequencyHz} Hz");
Console.WriteLine($"Devices: {machine.Devices.Count}");

Console.WriteLine();

machine.Reset();
var state = machine.GetState();
Console.WriteLine($"Initial PC: ${state.PC:X4}");

DeterministicTraceLogger? logger = null;
if (traceFile != null)
{
    logger = new DeterministicTraceLogger(machine, traceFile);
    Console.WriteLine($"Trace logging to: {traceFile}");
}

Console.WriteLine($"Executing {cycles} cycles...");

for (int i = 0; i < cycles; i++)
{
    machine.Clock.Step();
    logger?.LogInstruction();
}

logger?.Flush();

var finalState = machine.GetState();
Console.WriteLine($"Final PC: ${finalState.PC:X4}");
Console.WriteLine($"Total cycles: {machine.Clock.TotalCycles}");
Console.WriteLine();
Console.WriteLine("Emulation completed successfully!");
