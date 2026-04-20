using ViceSharp.Abstractions;
using ViceSharp.Chips.Cpu;
using ViceSharp.Core;

namespace ViceSharp.TestHarness;

/// <summary>
/// Basic boot test without native VICE dependency
/// </summary>
public static class BasicBootTest
{
    public static bool Run()
    {
        // Create minimal C64 environment
        var bus = new BasicBus();
        var clock = new SystemClock(985248); // PAL
        var cpu = new Mos6502(bus);
        
        // Initialize RAM
        for (int i = 0; i <= 0xFFFF; i++)
        {
            bus.Write((ushort)i, 0xFF);
        }
        
        // Write simple boot code at reset vector
        // LDA #$00 - load 0
        bus.Write(0xFFFC, 0xA9);
        bus.Write(0xFFFD, 0x00);
        // RTS - return
        bus.Write(0xFFFE, 0x60);
        
        cpu.Reset();
        
        Console.WriteLine($"After reset: PC=${cpu.PC:X4}, A=${cpu.A:X2}, X=${cpu.X:X2}, Y=${cpu.Y:X2}");
        Console.WriteLine($"Stack pointer: ${cpu.S:X2}");
        Console.WriteLine($"Status: ${cpu.P:X2}");
        
        // Should jump to reset vector (0xFCE2 for real C64, 0xFFFC for our test)
        bool pcValid = cpu.PC >= 0xFCE2 || cpu.PC == 0xFFFC;
        
        if (pcValid)
        {
            Console.WriteLine("✓ Boot test PASSED - CPU reset to valid vector");
            return true;
        }
        else
        {
            Console.WriteLine("✗ Boot test FAILED - Invalid reset vector");
            return false;
        }
    }
}
