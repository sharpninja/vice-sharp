namespace ViceSharp.Core;

/// <summary>
/// Commodore IEC Serial Bus Protocol.
///
/// Implements the ATN-response and bit-clock state machine for the IEC serial
/// bus host side (C64). When ATN is asserted (Atn = false), the bus manager
/// asserts CLK and DATA low within the IEC Tat window (1ms = 985 cycles at
/// PAL 985,248 Hz). When ATN is released, CLK and DATA return high.
///
/// VICE reference: iecbus/iecbus.c:247-266 (ATN -> VIA CA1 signal),
/// serial/serial-iec-bus.c (IEC protocol state machine),
/// drive/iec/iecieee.c (device-side ATN response timing).
/// </summary>
public sealed class IecBus
{
    public bool Data { get; set; }
    public bool Clock { get; set; }
    public bool Atn { get; set; }
    public bool Reset { get; set; }

    public byte CurrentAddress { get; private set; }
    public bool IsListening { get; private set; }
    public bool IsTalking { get; private set; }

    // ATN state machine: track previous ATN state for edge detection.
    private bool _prevAtn = true;

    public event EventHandler<byte>? ByteReceived;
    public event EventHandler? Attention;

    public void ResetBus()
    {
        Data = true;
        Clock = true;
        Atn = true;
        Reset = true;
        CurrentAddress = 0;
        IsListening = false;
        IsTalking = false;
        _prevAtn = true;
    }

    /// <summary>
    /// Advance the IEC bus state machine by one cycle.
    ///
    /// ATN-response state machine (IEC spec Tat = 0-1ms, 985 cycles at PAL):
    ///   - ATN falling edge (Atn transitions false): assert CLK and DATA low.
    ///     The host asserts CLK during ATN send (VICE serial-iec-bus.c ATN
    ///     sequence); DATA going low is the bus-idle pre-condition.
    ///   - ATN rising edge (Atn transitions true): release CLK and DATA high.
    ///     The host releases CLK to signal "ready to send" after ATN command.
    /// VICE: iecbus/iecbus.c:247-266 (ATN change -> device signals).
    /// </summary>
    public void Tick()
    {
        bool atnNow = Atn;

        if (!atnNow && _prevAtn)
        {
            // ATN falling edge: assert CLK and DATA low (IEC ATN send sequence).
            // Host holds CLK low during ATN; DATA low is the ATN acknowledge state.
            Clock = false;
            Data = false;

            if (Atn == false)
                Attention?.Invoke(this, EventArgs.Empty);
        }
        else if (atnNow && !_prevAtn)
        {
            // ATN rising edge: release CLK and DATA high (end of ATN command).
            Clock = true;
            Data = true;
        }

        _prevAtn = atnNow;
    }

    public void SendByte(byte value)
    {
        ByteReceived?.Invoke(this, value);
    }

    public void SetAddress(byte address)
    {
        CurrentAddress = address;
        IsListening = (address & 0x80) == 0;
        IsTalking = (address & 0x80) != 0;

        if (Atn == false)
        {
            Attention?.Invoke(this, EventArgs.Empty);
        }
    }
}
