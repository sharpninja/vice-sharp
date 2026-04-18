namespace ViceSharp.Chips.IEC;

public sealed class DiskController
{
    public int Track { get; set; }
    public int Sector { get; set; }
    public bool MotorOn { get; set; }
    public bool WriteProtect { get; set; }

    public byte[] TrackBuffer = new byte[65536];

    public void Reset()
    {
        Track = 18;
        Sector = 0;
        MotorOn = false;
        WriteProtect = true;
    }

    public void StepIn()
    {
        if (Track > 1) Track--;
    }

    public void StepOut()
    {
        if (Track < 35) Track++;
    }

    public byte ReadByte(int offset)
    {
        return TrackBuffer[offset & 0xFFFF];
    }

    public void WriteByte(int offset, byte value)
    {
        if (!WriteProtect)
        {
            TrackBuffer[offset & 0xFFFF] = value;
        }
    }
}