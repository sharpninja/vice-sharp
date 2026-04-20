using ViceSharp.Abstractions;

namespace ViceSharp.Core;

/// <summary>
/// Simple RAM device that handles all memory addresses.
/// Provides 64KB of addressable memory.
/// </summary>
public sealed class SimpleRam : IAddressSpace
{
    public DeviceId Id => new DeviceId(0x0100);
    public string Name => "64KB RAM";
    
    private readonly byte[] _memory = new byte[65536];

    public bool HandlesAddress(ushort address) => true;
    
    public byte Read(ushort address) => _memory[address];
    
    public void Write(ushort address, byte value) => _memory[address] = value;
    
    public byte Peek(ushort address) => _memory[address];
    
    /// <summary>
    /// Resets RAM to initial state.
    /// </summary>
    public void Reset()
    {
        InitializeC64();
    }
    
    /// <summary>
    /// Fill memory with a specific value.
    /// </summary>
    public void Fill(byte value)
    {
        Array.Fill(_memory, value);
    }
    
    /// <summary>
    /// Initialize memory with standard C64 pattern.
    /// </summary>
    public void InitializeC64()
    {
        // Zero page, stack, and user RAM = 0
        Array.Clear(_memory, 0, 0x0200);
        
        // Page 2 (stack) already zeroed
        
        // $0200-$03FF = 0 (reserved for system)
        // $0400-$07FF = Screen RAM (40x25 = 1000 chars) - initialize to spaces (0x20)
        Array.Fill(_memory, (byte)0x20, 0x0400, 0x0400); // 1KB screen RAM
        
        // $0800-$9FFF = 0xFF (empty RAM, typical after power-on)
        Array.Fill(_memory, (byte)0xFF, 0x0800, 0x9800);
        
        // $A000-$BFFF = BASIC ROM (loaded separately)
        // $C000-$CFFF = 0xFF
        // $D000-$DFFF = Character ROM (loaded separately)
        // $E000-$FFFB = KERNAL ROM (loaded separately)
        
        // Set reset vector at $FFFC-$FFFD to point to KERNAL reset
        // Real C64: $FCE2
        _memory[0xFFFC] = 0xE2;
        _memory[0xFFFD] = 0xFC;
    }

    /// <summary>
    /// Load ROM data directly into memory at specified address.
    /// Used for loading BASIC, KERNAL, and character ROMs.
    /// </summary>
    public void LoadRom(ushort startAddress, ReadOnlySpan<byte> data)
    {
        for (int i = 0; i < data.Length; i++)
        {
            _memory[startAddress + i] = data[i];
        }
    }
}
