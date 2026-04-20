namespace ViceSharp.Chips.IEC;

/// <summary>
/// D64 Disk Image File Format Reader/Writer
/// </summary>
public sealed class D64Image
{
    private readonly byte[] _diskData;

    public const int DiskSize35Track = 174848;

    public D64Image()
    {
        _diskData = new byte[DiskSize35Track];
    }

    public D64Image(byte[] data)
    {
        _diskData = data;
    }

    public byte ReadSectorByte(int track, int sector, int offset)
    {
        int position = GetSectorOffset(track, sector) + offset;
        return _diskData[position];
    }

    public void WriteSectorByte(int track, int sector, int offset, byte value)
    {
        int position = GetSectorOffset(track, sector) + offset;
        _diskData[position] = value;
    }

    public Span<byte> GetSector(int track, int sector)
    {
        int position = GetSectorOffset(track, sector);
        return _diskData.AsSpan(position, 256);
    }

    private static int GetSectorOffset(int track, int sector)
    {
        int offset = 0;

        for (int t = 1; t < track; t++)
        {
            offset += GetSectorsPerTrack(t) * 256;
        }

        offset += sector * 256;
        return offset;
    }

    private static int GetSectorsPerTrack(int track)
    {
        return track switch
        {
            >= 1 and <= 17 => 21,
            >= 18 and <= 24 => 19,
            >= 25 and <= 30 => 18,
            >= 31 and <= 35 => 17,
            _ => 0
        };
    }

    public void Format()
    {
        Array.Clear(_diskData);

        // Format BAM
        Span<byte> bam = GetSector(18, 0);
        bam[0x00] = 0x12;
        bam[0x01] = 0x01;
        bam[0xA2] = 0xA0;
        bam[0xA3] = 0xA0;
        bam[0xA4] = 0xA0;
    }

    public byte[] ToArray() => _diskData.ToArray();
    
    /// <summary>
    /// VICE-style: Read sector into buffer
    /// </summary>
    public bool ReadSector(int track, int sector, byte[] buffer)
    {
        if (buffer.Length < 256) return false;
        GetSector(track, sector).CopyTo(buffer);
        return true;
    }
    
    /// <summary>
    /// VICE-style: Write sector from buffer
    /// </summary>
    public bool WriteSector(int track, int sector, byte[] buffer)
    {
        if (buffer.Length < 256) return false;
        buffer.AsSpan(0, 256).CopyTo(GetSector(track, sector));
        return true;
    }
}
