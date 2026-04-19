using ViceSharp.Abstractions;
using ViceSharp.Chips.Cpu;

namespace ViceSharp.Chips;

/// <summary>
/// MOS 6510 CPU - inherits from Mos6502 with I/O port support at $00/$01.
/// </summary>
public sealed class Cpu6510 : Mos6502
{
    /// <summary>6510 I/O port (address $00)</summary>
    public byte Port0 { get; set; }
    
    /// <summary>6510 data direction register for port 0 (address $00)</summary>
    public byte Ddr0 { get; set; }
    
    /// <summary>6510 I/O port (address $01)</summary>
    public byte Port1 { get; set; }
    
    /// <summary>6510 data direction register for port 1 (address $01)</summary>
    public byte Ddr1 { get; set; }

    public Cpu6510(IBus bus) : base(bus) 
    {
        // Default: all bits output for port 1 (KERNAL ROM), port 0 input
        Ddr0 = 0x00;
        Ddr1 = 0x2F;
        Port1 = 0x2F; // KERNAL ROM enabled
    }

    /// <summary>
    /// Read from 6510 address space (handles $00/$01 port access)
    /// </summary>
    public override byte Read(ushort address)
    {
        switch (address & 0xFF)
        {
            case 0x00:
                // Port 0 read: return latch value based on DDR
                return (byte)((Port0 & Ddr0) | (0xFF & ~Ddr0));
            case 0x01:
                // Port 1 read: return latch value based on DDR
                return (byte)((Port1 & Ddr1) | (0xFF & ~Ddr1));
            default:
                return base.Read(address);
        }
    }

    /// <summary>
    /// Write to 6510 address space (handles $00/$01 port access)
    /// </summary>
    public override void Write(ushort address, byte value)
    {
        switch (address & 0xFF)
        {
            case 0x00:
                Ddr0 = value; // Data direction register
                break;
            case 0x01:
                Port1 = value; // I/O port
                break;
            default:
                base.Write(address, value);
                break;
        }
    }
}
