using ViceSharp.Abstractions;
using ViceSharp.Chips.Cpu;

namespace ViceSharp.Monitor.Views;

public sealed class CpuStatusView
{
    private readonly Mos6502 _cpu;

    public CpuStatusView(Mos6502 cpu)
    {
        _cpu = cpu;
    }

    public byte A => _cpu.A;
    public byte X => _cpu.X;
    public byte Y => _cpu.Y;
    public byte S => _cpu.S;
    public byte P => _cpu.P;
    public ushort PC => _cpu.PC;

    public bool Carry => (P & 0x01) != 0;
    public bool Zero => (P & 0x02) != 0;
    public bool Interrupt => (P & 0x04) != 0;
    public bool Decimal => (P & 0x08) != 0;
    public bool Break => (P & 0x10) != 0;
    public bool Overflow => (P & 0x40) != 0;
    public bool Negative => (P & 0x80) != 0;
}