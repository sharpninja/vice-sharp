namespace ViceSharp.Chips.IEC;

/// <summary>
/// Commodore IEC Serial Bus Protocol
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