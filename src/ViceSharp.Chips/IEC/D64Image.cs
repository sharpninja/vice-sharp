namespace ViceSharp.Chips.IEC;

/// <summary>
/// D64 Disk Image File Format Reader/Writer
/// </summary>
public sealed class D64Image
{
    private readonly byte[] _diskData;

    public const int DiskSize35Track = 174848;
    private const int SectorSize = 256;
    private const int DirectoryTrack = 18;
    private const int DirectoryFirstSector = 1;
    private const int DirectoryEntrySize = 32;
    private const int DirectoryEntryCount = 8;
    private const byte FileTypeMask = 0x07;
    private const byte FileTypePrg = 0x02;

    public D64Image()
    {
        _diskData = new byte[DiskSize35Track];
    }

    public D64Image(byte[] data)
    {
        if (data.Length != DiskSize35Track)
            throw new ArgumentException("D64 disk image must be 174,848 bytes.", nameof(data));

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

    /// <summary>
    /// Raw image bytes (174,848 for a 35-track D64). Read-only view; the
    /// underlying buffer is owned by this instance and persists for its
    /// lifetime. Used by D64DiskImageDevice.CommitToStream + tests that
    /// need byte-exact image comparison.
    /// </summary>
    public ReadOnlySpan<byte> RawData => _diskData;

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

    public bool TryReadFirstProgram(out D64ProgramFile? program, out string error)
    {
        program = null;
        error = string.Empty;

        var directoryTrack = DirectoryTrack;
        var directorySector = DirectoryFirstSector;
        var visitedDirectorySectors = new HashSet<(int Track, int Sector)>();

        while (directoryTrack != 0)
        {
            if (!IsValidSector(directoryTrack, directorySector))
            {
                error = $"Directory points to invalid sector {directoryTrack}/{directorySector}.";
                return false;
            }

            if (!visitedDirectorySectors.Add((directoryTrack, directorySector)))
            {
                error = "Directory sector chain contains a loop.";
                return false;
            }

            var directory = GetSector(directoryTrack, directorySector);
            for (var index = 0; index < DirectoryEntryCount; index++)
            {
                var entryOffset = 2 + index * DirectoryEntrySize;
                var fileType = (byte)(directory[entryOffset] & FileTypeMask);
                var fileTrack = directory[entryOffset + 1];
                var fileSector = directory[entryOffset + 2];

                if (fileTrack == 0 || fileType != FileTypePrg)
                    continue;

                var fileName = DecodeDirectoryFileName(directory.Slice(entryOffset + 3, 16));
                return TryReadProgram(fileName, fileTrack, fileSector, out program, out error);
            }

            directoryTrack = directory[0];
            directorySector = directory[1];
        }

        error = "D64 image does not contain a PRG directory entry.";
        return false;
    }
    
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

    private bool TryReadProgram(
        string fileName,
        int startTrack,
        int startSector,
        out D64ProgramFile? program,
        out string error)
    {
        program = null;
        error = string.Empty;

        var fileBytes = new List<byte>();
        var track = startTrack;
        var sector = startSector;
        var visitedSectors = new HashSet<(int Track, int Sector)>();

        while (track != 0)
        {
            if (!IsValidSector(track, sector))
            {
                error = $"PRG '{fileName}' points to invalid sector {track}/{sector}.";
                return false;
            }

            if (!visitedSectors.Add((track, sector)))
            {
                error = $"PRG '{fileName}' sector chain contains a loop.";
                return false;
            }

            var block = GetSector(track, sector);
            var nextTrack = block[0];
            var nextSector = block[1];
            if (nextTrack == 0)
            {
                if (nextSector < 2)
                {
                    error = $"PRG '{fileName}' final sector has an invalid byte count.";
                    return false;
                }

                fileBytes.AddRange(block.Slice(2, nextSector - 1).ToArray());
                break;
            }

            fileBytes.AddRange(block.Slice(2, SectorSize - 2).ToArray());
            track = nextTrack;
            sector = nextSector;
        }

        if (fileBytes.Count < 3)
        {
            error = $"PRG '{fileName}' is too short.";
            return false;
        }

        var loadAddress = (ushort)(fileBytes[0] | (fileBytes[1] << 8));
        program = new D64ProgramFile(fileName, loadAddress, fileBytes.Skip(2).ToArray());
        return true;
    }

    private static bool IsValidSector(int track, int sector)
    {
        var sectorCount = GetSectorsPerTrack(track);
        return sectorCount > 0 && sector >= 0 && sector < sectorCount;
    }

    private static string DecodeDirectoryFileName(ReadOnlySpan<byte> nameBytes)
    {
        Span<char> chars = stackalloc char[nameBytes.Length];
        var length = 0;

        foreach (var value in nameBytes)
        {
            if (value == 0xA0)
                break;

            chars[length++] = value switch
            {
                >= 0x41 and <= 0x5A => (char)value,
                >= 0x30 and <= 0x39 => (char)value,
                0x20 => ' ',
                0x2D => '-',
                0x5F => '_',
                _ => '?'
            };
        }

        return length == 0 ? "<unnamed>" : new string(chars[..length]);
    }
}

public sealed record D64ProgramFile(string FileName, ushort LoadAddress, byte[] Payload)
{
    public ushort EndAddress => (ushort)(LoadAddress + Payload.Length);
}
