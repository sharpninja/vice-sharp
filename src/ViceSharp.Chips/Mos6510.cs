namespace ViceSharp.Chips;

/// <summary>
/// MOS 6510 CPU Core
/// Direct logic port from VICE 6510core.c
/// </summary>
public sealed partial class Mos6510
{
    /// <summary>Accumulator</summary>
    public byte A;

    /// <summary>X index register</summary>
    public byte X;

    /// <summary>Y index register</summary>
    public byte Y;

    /// <summary>Stack pointer</summary>
    public byte SP;

    /// <summary>Program counter</summary>
    public ushort PC;

    /// <summary>Processor status register</summary>
    public byte P;

    /// <summary>Negative flag cache</summary>
    public byte FlagN;

    /// <summary>Zero flag cache</summary>
    public bool FlagZ;

    /// <summary>Current clock counter</summary>
    public ulong Clock;

    /// <summary>Interrupt pending flag</summary>
    public bool IrqPending;

    /// <summary>NMI pending flag</summary>
    public bool NmiPending;

    /// <summary>Reset pending flag</summary>
    public bool ResetPending;

    /// <summary>
    /// Processor status flag masks
    /// </summary>
    [Flags]
    public enum StatusFlags : byte
    {
        Carry = 0x01,
        Zero = 0x02,
        InterruptDisable = 0x04,
        Decimal = 0x08,
        Break = 0x10,
        Unused = 0x20,
        Overflow = 0x40,
        Negative = 0x80
    }

    /// <summary>
    /// Reset CPU to power on state
    /// </summary>
    public void Reset()
    {
        A = 0;
        X = 0;
        Y = 0;
        SP = 0xFF;
        PC = 0;
        P = 0x34; // Interrupt disable, unused bit set
        FlagN = 0;
        FlagZ = false;
        Clock = 0;
        IrqPending = false;
        NmiPending = false;
        ResetPending = false;
    }

    /// <summary>
    /// Set Negative and Zero flags
    /// Exact logic from VICE LOCAL_SET_NZ
    /// </summary>
    private void SetNZ(byte value)
    {
        FlagZ = value == 0;
        FlagN = (byte)(value & 0x80);
    }

    /// <summary>
    /// Get current status register
    /// Exact logic from VICE LOCAL_STATUS
    /// </summary>
    private byte GetStatus()
    {
        byte status = (byte)(P & ~((byte)StatusFlags.Zero | (byte)StatusFlags.Negative | (byte)StatusFlags.Unused));
        status |= (byte)StatusFlags.Unused;

        if (FlagZ)
            status |= (byte)StatusFlags.Zero;

        status |= FlagN;

        return status;
    }

    /// <summary>
    /// Read with absolute X addressing
    /// Exact page crossing logic from VICE
    /// </summary>
    private byte ReadAbsoluteX(ushort address)
    {
        if (((address & 0xFF) + X) > 0xFF)
        {
            // Page boundary crossed: dummy read, +1 cycle
            Clock += 1;
        }

        return 0; // TODO: ReadMemory((ushort)(address + X));
    }

    /// <summary>
    /// Read with absolute Y addressing
    /// Exact page crossing logic from VICE
    /// </summary>
    private byte ReadAbsoluteY(ushort address)
    {
        if (((address & 0xFF) + Y) > 0xFF)
        {
            // Page boundary crossed: dummy read, +1 cycle
            Clock += 1;
        }

        return 0; // TODO: ReadMemory((ushort)(address + Y));
    }

    /// <summary>
    /// ADC Add with Carry
    /// Exact logic from VICE
    /// </summary>
    private void ADC(byte value)
    {
        bool carry = (P & (byte)StatusFlags.Carry) != 0;
        int result = A + value + (carry ? 1 : 0);

        P &= unchecked((byte)~(StatusFlags.Carry | StatusFlags.Overflow));

        if (result > 0xFF)
            P |= (byte)StatusFlags.Carry;

        if (((A ^ result) & (value ^ result) & 0x80) != 0)
            P |= (byte)StatusFlags.Overflow;

        A = (byte)result;
        SetNZ(A);
    }

    /// <summary>
    /// SBC Subtract with Carry
    /// Exact logic from VICE
    /// </summary>
    private void SBC(byte value)
    {
        bool carry = (P & (byte)StatusFlags.Carry) != 0;
        int result = A - value - (carry ? 0 : 1);

        P &= unchecked((byte)~(StatusFlags.Carry | StatusFlags.Overflow));

        if (result >= 0)
            P |= (byte)StatusFlags.Carry;

        if (((A ^ result) & (~value ^ result) & 0x80) != 0)
            P |= (byte)StatusFlags.Overflow;

        A = (byte)result;
        SetNZ(A);
    }

    /// <summary>
    /// Addressing mode helpers
    /// </summary>
    private byte ReadImm() { return 0; }
    private byte ReadZp() { return 0; }
    private byte ReadZpX() { return 0; }
    private byte ReadAbs() { return 0; }
    private ushort ReadAbsAddr() { return 0; }
    private byte ReadIndX() { return 0; }
    private byte ReadIndY() { return 0; }

    private void WriteZp(byte value) { }
    private void WriteZpX(byte value) { }
    private void WriteAbs(byte value) { }
    private void WriteAbsX(byte value) { }
    private void WriteAbsY(byte value) { }
    private void WriteIndX(byte value) { }
    private void WriteIndY(byte value) { }

    private void Push(byte value) { }
    private byte Pop() { return 0; }
    private void Branch(bool condition) { }

    /// <summary>
    /// Execute single clock cycle
    /// </summary>
    public void Step()
    {
        Clock++;

        // TODO: Implement opcode execution
    }
}
