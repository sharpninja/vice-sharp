namespace ViceSharp.Chips.Cpu;

partial class Mos6502
{
    #pragma warning disable CS0169
    private ushort _addr;
    #pragma warning restore CS0169

    private ushort Implied() => 0;
    private ushort Immediate() => PC++;
    private ushort ZeroPage() => Fetch();
    private ushort ZeroPageX() => (byte)(Fetch() + X);
    private ushort ZeroPageY() => (byte)(Fetch() + Y);
    private ushort Absolute() => FetchWord();
    private ushort AbsoluteX() => (ushort)(FetchWord() + X);
    private ushort AbsoluteY() => (ushort)(FetchWord() + Y);
    private ushort Indirect()
    {
        ushort ptr = FetchWord();
        byte lo = _bus.Read(ptr);
        byte hi = _bus.Read((ushort)(ptr + 1));
        return (ushort)(lo | (hi << 8));
    }
    private ushort IndirectX()
    {
        byte ptr = (byte)(Fetch() + X);
        byte lo = _bus.Read(ptr);
        byte hi = _bus.Read((byte)(ptr + 1));
        return (ushort)(lo | (hi << 8));
    }
    private ushort IndirectY()
    {
        byte ptr = Fetch();
        byte lo = _bus.Read(ptr);
        byte hi = _bus.Read((byte)(ptr + 1));
        return (ushort)((lo | (hi << 8)) + Y);
    }
    private ushort Relative()
    {
        sbyte offset = (sbyte)Fetch();
        return (ushort)(PC + offset);
    }
}