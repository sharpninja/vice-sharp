using ViceSharp.Abstractions;

namespace ViceSharp.Chips.Cpu;

public sealed partial class Mos6502 : IClockedDevice, IAddressSpace, ICpu
{
    public DeviceId Id => new DeviceId(0x0001);
    public string Name => "MOS 6502 CPU";
    public uint ClockDivisor => 1;
    public ClockPhase Phase => ClockPhase.Phi2;

    // Registers
    public byte A;
    public byte X;
    public byte Y;
    public byte S;
    public ushort PC { get; set; }
    public byte Flags { get => P; set => P = value; }
    public byte P;

    public void Irq()
    {
        // IRQ implementation pending
    }

    public void Nmi()
    {
        // NMI implementation pending
    }

    private readonly IBus _bus;

    public Mos6502(IBus bus)
    {
        _bus = bus;
    }

    private byte _opcode;
    private int _cycle;
    private ushort _effectiveAddress;
    private byte _fetched;

    public void Tick()
    {
        if (_cycle == 0)
        {
            _opcode = Read(PC++);
            _cycle = GetCycleCount(_opcode);
            _effectiveAddress = 0;
            _fetched = 0;
            
            AddressingMode mode = GetAddressingMode(_opcode);
            bool pageCrossed = ExecuteAddressing(mode);
            
            if (pageCrossed && IsPageBoundaryCycleRequired(_opcode))
                _cycle++;
        }

        _cycle--;

        if (_cycle == 0)
        {
            ExecuteOpcode(_opcode);
        }
    }

    public void Reset()
    {
        A = 0;
        X = 0;
        Y = 0;
        S = 0xFD;
        P = 0x24;
        PC = _bus.Read(0xFFFC);
        PC |= (ushort)(_bus.Read(0xFFFD) << 8);
    }

    public byte Read(ushort address) => _bus.Read(address);
    public void Write(ushort address, byte value) => _bus.Write(address, value);
    public byte Peek(ushort address) => _bus.Peek(address);
    private enum AddressingMode
    {
        Implied,
        Immediate,
        ZeroPage,
        ZeroPageX,
        ZeroPageY,
        Absolute,
        AbsoluteX,
        AbsoluteY,
        Indirect,
        IndirectX,
        IndirectY,
        Relative
    }

    private partial int GetCycleCount(byte opcode);
    private partial AddressingMode GetAddressingMode(byte opcode);
    private partial bool ExecuteAddressing(AddressingMode mode);
    private partial bool IsPageBoundaryCycleRequired(byte opcode);
    private partial void ExecuteOpcode(byte opcode);

    public bool HandlesAddress(ushort address) => false;
}
