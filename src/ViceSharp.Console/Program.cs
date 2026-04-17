using ViceSharp.Core;
using ViceSharp.Architectures.C64;

Console.WriteLine("ViceSharp - Commodore 64 Emulator");
Console.WriteLine("=================================");

// Create system components
var bus = new SystemBus();
var clock = new SystemClock();
var interruptController = new InterruptController();

// Create and initialize C64 machine
var c64 = new Commodore64(bus, clock, interruptController);

Console.WriteLine("System initialized.");
Console.WriteLine("Starting emulation...");

c64.Reset();

// Run for 1 million cycles
for (int i = 0; i < 1000000; i++)
{
    c64.Step();
    
    if (i % 100000 == 0)
    {
        Console.WriteLine($"Executed {i} cycles");
    }
}

Console.WriteLine("Emulation completed successfully.");
Console.WriteLine("Press any key to exit.");
Console.ReadKey();
