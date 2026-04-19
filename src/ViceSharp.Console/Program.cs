using ViceSharp.Abstractions;
using ViceSharp.Core;
using ViceSharp.Architectures.EmptyMachine;

Console.WriteLine("ViceSharp - Commodore Emulator");
Console.WriteLine("==============================");

// Test Architecture Builder with Empty Machine
Console.WriteLine("Testing Architecture Builder...");

var builder = new ArchitectureBuilder();
var descriptor = new EmptyMachineDescriptor();
var machine = builder.Build(descriptor);

Console.WriteLine($"✓ Successfully built machine: {machine.Architecture.MachineName}");
Console.WriteLine($"✓ Bus initialized");
Console.WriteLine($"✓ Clock initialized @ {machine.Clock.FrequencyHz} Hz");
Console.WriteLine($"✓ Device registry contains {machine.Devices.Count} devices");

Console.WriteLine();
Console.WriteLine("Running empty machine test...");

// Step clock
for (int i = 0; i < 1000000; i++)
{
    machine.Clock.Step();
    
    if (i % 200000 == 0)
    {
        Console.WriteLine($"Executed {i} cycles");
    }
}

Console.WriteLine();
Console.WriteLine("Emulation completed successfully");
Console.WriteLine($"Total cycles: {machine.Clock.TotalCycles}");
