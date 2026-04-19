namespace ViceSharp.Chips;

/// <summary>
/// MOS 6526 Complex Interface Adapter (CIA)
/// Direct logic port from VICE cia.h
/// </summary>
public sealed class Mos6526
{
    #region Register Offsets

    public const byte PRA = 0x00;
    public const byte PRB = 0x01;
    public const byte DDRA = 0x02;
    public const byte DDRB = 0x03;
    public const byte TAL = 0x04;
    public const byte TAH = 0x05;
    public const byte TBL = 0x06;
    public const byte TBH = 0x07;
    public const byte TOD_TEN = 0x08;
    public const byte TOD_SEC = 0x09;
    public const byte TOD_MIN = 0x0A;
    public const byte TOD_HR = 0x0B;
    public const byte SDR = 0x0C;
    public const byte ICR = 0x0D;
    public const byte CRA = 0x0E;
    public const byte CRB = 0x0F;

    #endregion

    #region Control Register Bits

    [Flags]
    public enum ControlRegister : byte
    {
        Start = 0x01,
        PbOn = 0x02,
        OutMode = 0x04,
        RunMode = 0x08,
        Load = 0x10,
    }

    [Flags]
    public enum ControlRegisterA : byte
    {
        Start = 0x01,
        PbOn = 0x02,
        OutMode = 0x04,
        RunMode = 0x08,
        Load = 0x10,
        InMode = 0x20,
        SpMode = 0x40,
        TodIn = 0x80
    }

    [Flags]
    public enum ControlRegisterB : byte
    {
        Start = 0x01,
        PbOn = 0x02,
        OutMode = 0x04,
        RunMode = 0x08,
        Load = 0x10,
        InModeMask = 0x60,
        Alarm = 0x80
    }

    [Flags]
    public enum InterruptMask : byte
    {
        Set = 0x80,
        TimerA = 0x01,
        TimerB = 0x02,
        Tod = 0x04,
        Sdr = 0x08,
        Flag = 0x10
    }

    #endregion

    /// <summary>CIA registers</summary>
    public readonly byte[] Registers = new byte[16];

    /// <summary>Timer A counter</summary>
    public ushort TimerA;

    /// <summary>Timer B counter</summary>
    public ushort TimerB;

    /// <summary>Timer A latch</summary>
    public ushort TimerALatch;

    /// <summary>Timer B latch</summary>
    public ushort TimerBLatch;

    /// <summary>Interrupt flags</summary>
    public byte IrqFlags;

    /// <summary>Interrupt enable mask</summary>
    public byte IrqEnabled;
    
    // VICE-style: Keyboard matrix reference for port A
    private ViceSharp.Chips.Input.C64KeyboardMatrix? _keyboardMatrix;
    
    /// <summary>
    /// Wire keyboard matrix for VICE-style keyboard scanning on port A
    /// </summary>
    public void WireKeyboard(ViceSharp.Chips.Input.C64KeyboardMatrix keyboard)
    {
        _keyboardMatrix = keyboard;
    }
    
    /// <summary>
    /// Read port A with VICE-style keyboard matrix handling
    /// </summary>
    public byte ReadPortA()
    {
        byte value = Registers[PRA];
        byte ddra = Registers[DDRA];
        
        // If port A is in input mode (DDR=0), read from keyboard
        if (ddra == 0 && _keyboardMatrix != null)
        {
            // Keyboard columns are selected via port B
            return _keyboardMatrix.ReadRowState();
        }
        
        return value;
    }
    
    /// <summary>
    /// Write to port A (keyboard column selection via DDR)
    /// </summary>
    public void WritePortA(byte value)
    {
        Registers[PRA] = value;
    }
    
    /// <summary>
    /// Read port B with VICE-style keyboard handling
    /// </summary>
    public byte ReadPortB()
    {
        byte value = Registers[PRB];
        byte ddrb = Registers[DDRB];
        
        // Port B selects keyboard columns
        if (_keyboardMatrix != null)
        {
            // In input mode, port B reads column state
            byte colMask = ddrb == 0 ? (byte)0xFF : Registers[PRB];
            _keyboardMatrix.SetColumnMask(colMask);
        }
        
        return value;
    }
    
    /// <summary>
    /// Write to port B (keyboard column selection)
    /// </summary>
    public void WritePortB(byte value)
    {
        Registers[PRB] = value;
        if (_keyboardMatrix != null)
        {
            _keyboardMatrix.SetColumnMask(value);
        }
    }

    /// <summary>Port A data direction</summary>
    public byte PortADirection => Registers[DDRA];

    /// <summary>Port B data direction</summary>
    public byte PortBDirection => Registers[DDRB];

    /// <summary>Control register A</summary>
    public ControlRegisterA ControlA
    {
        get => (ControlRegisterA)Registers[CRA];
        set => Registers[CRA] = (byte)value;
    }

    /// <summary>Control register B</summary>
    public ControlRegisterB ControlB
    {
        get => (ControlRegisterB)Registers[CRB];
        set => Registers[CRB] = (byte)value;
    }

    /// <summary>
    /// Reset CIA to power on state
    /// </summary>
    public void Reset()
    {
        Array.Clear(Registers, 0, Registers.Length);

        TimerA = 0xFFFF;
        TimerB = 0xFFFF;
        TimerALatch = 0xFFFF;
        TimerBLatch = 0xFFFF;

        IrqFlags = 0;
        IrqEnabled = 0;

        // Reset state matches VICE
        Registers[DDRA] = 0;
        Registers[DDRB] = 0;
        Registers[CRA] = 0;
        Registers[CRB] = 0;
        Registers[ICR] = 0;
    }

    /// <summary>
    /// Read CIA register
    /// </summary>
    public byte Read(ushort address)
    {
        byte offset = (byte)(address & 0x0F);

        switch (offset)
        {
            case ICR:
                // Reading ICR clears all flags
                byte result = IrqFlags;
                IrqFlags = 0;
                return result;

            default:
                return Registers[offset];
        }
    }

    /// <summary>
    /// Write CIA register
    /// </summary>
    public void Write(ushort address, byte value)
    {
        byte offset = (byte)(address & 0x0F);

        switch (offset)
        {
            case TAL:
                TimerALatch = (ushort)((TimerALatch & 0xFF00) | value);
                Registers[TAL] = value;
                if ((ControlA & ControlRegisterA.Start) == 0)
                    TimerA = (ushort)((TimerA & 0xFF00) | value);
                break;

            case TAH:
                TimerALatch = (ushort)((TimerALatch & 0x00FF) | (value << 8));
                Registers[TAH] = value;
                if ((ControlA & ControlRegisterA.Start) == 0)
                    TimerA = (ushort)((TimerA & 0x00FF) | (value << 8));
                break;

            case TBL:
                TimerBLatch = (ushort)((TimerBLatch & 0xFF00) | value);
                Registers[TBL] = value;
                if ((ControlB & ControlRegisterB.Start) == 0)
                    TimerB = (ushort)((TimerB & 0xFF00) | value);
                break;

            case TBH:
                TimerBLatch = (ushort)((TimerBLatch & 0x00FF) | (value << 8));
                Registers[TBH] = value;
                if ((ControlB & ControlRegisterB.Start) == 0)
                    TimerB = (ushort)((TimerB & 0x00FF) | (value << 8));
                break;

            case ICR:
                if ((value & (byte)InterruptMask.Set) != 0)
                    IrqEnabled |= (byte)(value & 0x1F);
                else
                    IrqEnabled &= unchecked((byte)~(value & 0x1F));
                break;

            default:
                Registers[offset] = value;
                break;
        }
    }

    /// <summary>
    /// Execute single clock cycle
    /// </summary>
    public void Step()
    {
        // Timer A
        if ((ControlA & ControlRegisterA.Start) != 0)
        {
            if (TimerA == 0)
            {
                TimerA = TimerALatch;
                IrqFlags |= (byte)InterruptMask.TimerA;

                if ((ControlA & ControlRegisterA.RunMode) != 0)
                {
                    // One shot mode
                    ControlA &= ~ControlRegisterA.Start;
                }
            }
            else
            {
                TimerA--;
            }
        }

        // Timer B
        if ((ControlB & ControlRegisterB.Start) != 0)
        {
            if (TimerB == 0)
            {
                TimerB = TimerBLatch;
                IrqFlags |= (byte)InterruptMask.TimerB;

                if ((ControlB & ControlRegisterB.RunMode) != 0)
                {
                    // One shot mode
                    ControlB &= ~ControlRegisterB.Start;
                }
            }
            else
            {
                TimerB--;
            }
        }
    }
}