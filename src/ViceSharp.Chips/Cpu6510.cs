namespace ViceSharp.Chips;

/// <summary>
/// MOS 6510/8500 CPU
/// </summary>
public sealed class Cpu6510
{
    /// <summary>Accumulator</summary>
    public byte A;

    /// <summary>X Index Register</summary>
    public byte X;

    /// <summary>Y Index Register</summary>
    public byte Y;

    /// <summary>Status Register</summary>
    public byte P;

    /// <summary>Stack Pointer</summary>
    public byte S;

    /// <summary>Program Counter</summary>
    public ushort PC;

    /// <summary>Current Opcode</summary>
    public byte Opcode;

    /// <summary>
    /// Status Flags
    /// </summary>
    public bool N { get => (P & 0x80) != 0; set => P = (byte)(value ? P | 0x80 : P & ~0x80); }
    public bool V { get => (P & 0x40) != 0; set => P = (byte)(value ? P | 0x40 : P & ~0x40); }
    public bool B { get => (P & 0x10) != 0; set => P = (byte)(value ? P | 0x10 : P & ~0x10); }
    public bool D { get => (P & 0x08) != 0; set => P = (byte)(value ? P | 0x08 : P & ~0x08); }
    public bool I { get => (P & 0x04) != 0; set => P = (byte)(value ? P | 0x04 : P & ~0x04); }
    public bool Z { get => (P & 0x02) != 0; set => P = (byte)(value ? P | 0x02 : P & ~0x02); }
    public bool C { get => (P & 0x01) != 0; set => P = (byte)(value ? P | 0x01 : P & ~0x01); }

    /// <summary>
    /// Reset CPU
    /// </summary>
    public void Reset()
    {
        A = 0;
        X = 0;
        Y = 0;
        P = 0x34;
        S = 0xFF;
        PC = 0xFFFC;
    }

    /// <summary>
    /// Set Negative and Zero flags
    /// </summary>
    public void SetFlagsNZ(byte value)
    {
        Z = value == 0;
        N = (value & 0x80) != 0;
    }

    private Func<ushort, byte> _read = _ => 0;
    private Action<ushort, byte> _write = (_, _) => { };

    /// <summary>
    /// Attach memory bus
    /// </summary>
    public void AttachMemory(Func<ushort, byte> read, Action<ushort, byte> write)
    {
        _read = read;
        _write = write;
    }

    /// <summary>
    /// Read byte from memory
    /// </summary>
    public byte Read(ushort address) => _read(address);

    /// <summary>
    /// Write byte to memory
    /// </summary>
    public void Write(ushort address, byte value) => _write(address, value);

    /// <summary>
    /// Execute next instruction
    /// </summary>
    public void Step()
    {
        // Fetch opcode
        Opcode = Read(PC++);

        // Execute instruction
        _opTable[Opcode]();
    }

    private readonly Action[] _opTable;

    /// <summary>
    /// Initialize opcode table
    /// </summary>
    public Cpu6510()
    {
        _opTable = new Action[256];

        // LDA Immediate
        _opTable[0xA9] = () =>
        {
            A = Read(PC++);
            SetFlagsNZ(A);
        };

        // LDX Immediate
        _opTable[0xA2] = () =>
        {
            X = Read(PC++);
            SetFlagsNZ(X);
        };

        // LDY Immediate
        _opTable[0xA0] = () =>
        {
            Y = Read(PC++);
            SetFlagsNZ(Y);
        };

        // INX
        _opTable[0xE8] = () =>
        {
            X++;
            SetFlagsNZ(X);
        };

        // INY
        _opTable[0xC8] = () =>
        {
            Y++;
            SetFlagsNZ(Y);
        };

        // DEX
        _opTable[0xCA] = () =>
        {
            X--;
            SetFlagsNZ(X);
        };

        // DEY
        _opTable[0x88] = () =>
        {
            Y--;
            SetFlagsNZ(Y);
        };

        // NOP
        _opTable[0xEA] = () => { };
    }
}
