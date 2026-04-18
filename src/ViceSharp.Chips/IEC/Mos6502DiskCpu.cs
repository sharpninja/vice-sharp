namespace ViceSharp.Chips.IEC;

/// <summary>
/// MOS 6502 CPU for 1541 Floppy Disk Drive
/// </summary>
public sealed class Mos6502DiskCpu
{
    public byte A { get; set; }
    public byte X { get; set; }
    public byte Y { get; set; }
    public byte S { get; set; } = 0xFF;
    public byte P { get; set; } = 0x04;
    public ushort PC { get; set; }

    public void Reset()
    {
        A = 0;
        X = 0;
        Y = 0;
        S = 0xFF;
        P = 0x04;
        PC = 0xFFFC;
    }

    public void Tick()
    {
    }
}