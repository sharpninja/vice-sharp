using ViceSharp.Abstractions;
using ViceSharp.Core;
using ViceSharp.Architectures.EmptyMachine;
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
var descriptor = new EmptyMachineDescriptor();

IMachine machine;
machine = builder.Build(descriptor);

Console.WriteLine($"Machine: {machine.Architecture.MachineName}");
Console.WriteLine($"Clock: {machine.Clock.FrequencyHz} Hz");
Console.WriteLine($"Devices: {machine.Devices.Count}");

// Check for ROMs and load if available
string basicPath = Path.Combine(romPath, "C64", "basic");
string kernalPath = Path.Combine(romPath, "C64", "kernal");
string charsPath = Path.Combine(romPath, "C64", "characters");

if (File.Exists(basicPath) && File.Exists(kernalPath) && File.Exists(charsPath))
{
    Console.WriteLine("Loading C64 ROMs...");
    var bus = machine.Bus;
    
    var basic = await File.ReadAllBytesAsync(basicPath);
    for (int i = 0; i < basic.Length; i++)
        bus.Write((ushort)(0xA000 + i), basic[i]);
    
    var kernal = await File.ReadAllBytesAsync(kernalPath);
    for (int i = 0; i < kernal.Length; i++)
        bus.Write((ushort)(0xE000 + i), kernal[i]);
    
    var chars = await File.ReadAllBytesAsync(charsPath);
    for (int i = 0; i < chars.Length; i++)
        bus.Write((ushort)(0xD000 + i), chars[i]);
    
    Console.WriteLine($"  BASIC: {basic.Length} bytes");
    Console.WriteLine($"  KERNAL: {kernal.Length} bytes");
    Console.WriteLine($"  CHARS: {chars.Length} bytes");
}
else
{
    Console.WriteLine("No ROMs found in: " + Path.GetFullPath(Path.Combine(romPath, "C64")));
    Console.WriteLine("Using empty machine - CPU executes from RAM at $FFxx");
}

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
