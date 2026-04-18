namespace ViceSharp.Chips.IEC;

public sealed class IecDrive
{
    public byte DriveNumber { get; init; }
    public bool IsOnline { get; set; }

    public void Reset()
    {
    }

    public byte Read()
    {
        return 0x00;
    }

    public void Write(byte value)
    {
    }
}