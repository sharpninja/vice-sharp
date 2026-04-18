namespace ViceSharp.Chips.IEC;

/// <summary>
/// MOS 6522 VIA controller for 1541 Floppy Disk Drive
/// </summary>
public sealed class Via6522
{
    public byte PortA { get; set; }
    public byte PortB { get; set; }
    public byte Ddra { get; set; }
    public byte Ddrb { get; set; }

    public ushort Timer1 { get; set; }
    public ushort Timer2 { get; set; }

    public byte Ier { get; set; }
    public byte Ifr { get; set; }

    public void Reset()
    {
        PortA = 0;
        PortB = 0;
        Ddra = 0;
        Ddrb = 0;
        Timer1 = 0xFFFF;
        Timer2 = 0xFFFF;
        Ier = 0;
        Ifr = 0;
    }

    public byte Read(byte register)
    {
        return register switch
        {
            0x00 => PortA,
            0x01 => PortB,
            0x02 => Ddra,
            0x03 => Ddrb,
            0x04 => (byte)(Timer1 & 0xFF),
            0x05 => (byte)(Timer1 >> 8),
            0x08 => (byte)(Timer2 & 0xFF),
            0x09 => (byte)(Timer2 >> 8),
            0x0D => Ier,
            0x0E => Ifr,
            _ => 0x00
        };
    }

    public void Write(byte register, byte value)
    {
        switch (register)
        {
            case 0x00: PortA = value; break;
            case 0x01: PortB = value; break;
            case 0x02: Ddra = value; break;
            case 0x03: Ddrb = value; break;
            case 0x04: Timer1 = (ushort)((Timer1 & 0xFF00) | value); break;
            case 0x05: Timer1 = (ushort)((value << 8) | (Timer1 & 0x00FF)); break;
            case 0x08: Timer2 = (ushort)((Timer2 & 0xFF00) | value); break;
            case 0x09: Timer2 = (ushort)((value << 8) | (Timer2 & 0x00FF)); break;
            case 0x0D: Ier = value; break;
            case 0x0E: Ifr = value; break;
        }
    }
}