namespace ViceSharp.Chips.IEC;

/// <summary>
/// High-level CBM DOS read view over a <see cref="D64Image"/>: directory
/// enumeration, CBM pattern file lookup, raw file-stream extraction (the byte
/// stream the KERNAL receives over IEC, including the 2-byte PRG load address),
/// and the synthetic "$" directory BASIC listing.
///
/// This is the host-side "vdrive" read logic that backs the KERNAL serial
/// traps when True Drive is OFF, mirroring VICE's vdrive-iec/vdrive-dir read
/// path (native/vice/vice/src/vdrive/). Names are handled as raw PETSCII so
/// pattern matching is byte-exact with what the KERNAL sends; no charset round
/// trip is performed.
/// </summary>
public sealed class D64FileSystem
{
    private const int SectorSize = 256;
    private const int DirectoryTrack = 18;
    private const int DirectoryFirstSector = 1;
    private const int DirectoryEntrySize = 32;
    private const int DirectoryEntryCount = 8;

    // Directory slot field offsets (relative to the start of a 32-byte slot).
    private const int SlotTypeOffset = 0;
    private const int SlotTrackOffset = 1;
    private const int SlotSectorOffset = 2;
    private const int SlotNameOffset = 3;
    private const int SlotNameLength = 16;
    private const int SlotBlocksLowOffset = 0x1E;
    private const int SlotBlocksHighOffset = 0x1F;

    // BAM (track 18 / sector 0) disk header fields for a 1541 image.
    private const int BamDiskNameOffset = 0x90;
    private const int BamDiskNameLength = 16;
    private const int BamDiskIdOffset = 0xA2;
    private const int BamDiskIdLength = 5;

    private const byte FileTypeMask = 0x07;
    private const byte FileTypePrg = 0x02;
    private const byte PetsciiPad = 0xA0;

    private readonly D64Image _image;

    public D64FileSystem(D64Image image)
    {
        ArgumentNullException.ThrowIfNull(image);
        _image = image;
    }

    /// <summary>Walk the directory chain (track 18) and return every live slot.</summary>
    public IReadOnlyList<D64DirectoryEntry> EnumerateDirectory()
    {
        var entries = new List<D64DirectoryEntry>();
        var track = DirectoryTrack;
        var sector = DirectoryFirstSector;
        var visited = new HashSet<(int, int)>();

        while (track != 0)
        {
            if (!IsValidSector(track, sector) || !visited.Add((track, sector)))
                break;

            var dir = _image.GetSector(track, sector);
            for (var index = 0; index < DirectoryEntryCount; index++)
            {
                var entryOffset = 2 + index * DirectoryEntrySize;
                var typeByte = dir[entryOffset + SlotTypeOffset];

                // VICE vdrive-dir: a slot with a zero type byte is empty/unused.
                if (typeByte == 0)
                    continue;

                var name = dir.Slice(entryOffset + SlotNameOffset, SlotNameLength).ToArray();
                var blocks = dir[entryOffset + SlotBlocksLowOffset]
                             | (dir[entryOffset + SlotBlocksHighOffset] << 8);

                entries.Add(new D64DirectoryEntry(
                    name,
                    typeByte,
                    dir[entryOffset + SlotTrackOffset],
                    dir[entryOffset + SlotSectorOffset],
                    blocks));
            }

            track = dir[0];
            sector = dir[1];
        }

        return entries;
    }

    /// <summary>
    /// Resolve a CBM filename pattern (raw PETSCII, optionally with a "0:" drive
    /// prefix and ",P,R"-style suffix) to its first matching live directory slot
    /// that has a real start sector. "*" matches the first entry.
    /// </summary>
    public bool TryFindFile(ReadOnlySpan<byte> patternPetscii, out D64DirectoryEntry entry)
    {
        var pattern = NormalizePattern(patternPetscii);

        foreach (var candidate in EnumerateDirectory())
        {
            if (candidate.StartTrack == 0)
                continue;

            if (MatchesPattern(pattern, candidate.NamePetscii))
            {
                entry = candidate;
                return true;
            }
        }

        entry = default;
        return false;
    }

    /// <summary>
    /// Read the raw byte stream of a file by following its sector chain. The
    /// returned bytes are exactly what the host receives over IEC: for a PRG the
    /// first two bytes are the little-endian load address followed by the
    /// program image. Mirrors VICE iec_read_sequential (link bytes at [0,1],
    /// data at [2..], final sector terminated by next-track==0 with the last
    /// valid byte index in [1]).
    /// </summary>
    public byte[] ReadFileStream(int startTrack, int startSector)
    {
        var bytes = new List<byte>();
        var track = startTrack;
        var sector = startSector;
        var visited = new HashSet<(int, int)>();

        while (track != 0)
        {
            if (!IsValidSector(track, sector) || !visited.Add((track, sector)))
                break;

            var block = _image.GetSector(track, sector);
            var nextTrack = block[0];
            var nextSector = block[1];

            if (nextTrack == 0)
            {
                // Final sector: byte[1] is the index of the last valid byte, so
                // (nextSector - 1) data bytes follow the 2-byte link header.
                if (nextSector >= 2)
                    bytes.AddRange(block.Slice(2, nextSector - 1).ToArray());
                break;
            }

            bytes.AddRange(block.Slice(2, SectorSize - 2).ToArray());
            track = nextTrack;
            sector = nextSector;
        }

        return bytes.ToArray();
    }

    /// <summary>
    /// Build the synthetic directory program returned by LOAD"$",8, faithful to
    /// VICE vdrive-dir.c: load address $0401, a reverse-video disk-name header
    /// line, one BASIC line per live file (line number = block count, text =
    /// quoted name + file type), and a "BLOCKS FREE." trailer, ending with the
    /// two zero link bytes that terminate a BASIC program.
    /// </summary>
    public byte[] BuildDirectoryListing()
    {
        var output = new List<byte>();

        // PRG load address $0401 (BASIC start on the directory's own line links).
        output.Add(0x01);
        output.Add(0x04);

        var bam = _image.GetSector(DirectoryTrack, 0);

        // Header line: link, line number 0, RVS-on, "name" id.
        output.Add(0x01);
        output.Add(0x01);
        output.Add(0x00);
        output.Add(0x00);
        output.Add(0x12); // RVS on
        output.Add(0x22); // quote
        AppendPadConverted(output, bam.Slice(BamDiskNameOffset, BamDiskNameLength));
        output.Add(0x22); // quote
        output.Add(0x20); // space
        AppendPadConverted(output, bam.Slice(BamDiskIdOffset, BamDiskIdLength));
        output.Add(0x00); // end of line

        foreach (var entry in EnumerateDirectory())
        {
            if (entry.StartTrack == 0)
                continue;

            output.Add(0x01); // link
            output.Add(0x01);
            output.Add((byte)(entry.Blocks & 0xFF));        // line number = blocks
            output.Add((byte)((entry.Blocks >> 8) & 0xFF));

            // Leading spaces so the opening quote right-justifies the block count.
            var leading = 1;
            if (entry.Blocks < 10) leading++;
            if (entry.Blocks < 100) leading++;
            for (var i = 0; i < leading; i++)
                output.Add(0x20);

            output.Add(0x22); // quote
            var nameLength = 0;
            while (nameLength < SlotNameLength && entry.NamePetscii[nameLength] != PetsciiPad)
                nameLength++;
            for (var i = 0; i < nameLength; i++)
                output.Add(entry.NamePetscii[i]);
            output.Add(0x22); // quote
            output.Add(0x20); // space after name

            foreach (var c in FileTypeMnemonic(entry.FileType))
                output.Add((byte)c);

            output.Add(0x00); // end of line
        }

        // "BLOCKS FREE." trailer line.
        var freeBlocks = CountFreeBlocks(bam);
        output.Add(0x01);
        output.Add(0x01);
        output.Add((byte)(freeBlocks & 0xFF));
        output.Add((byte)((freeBlocks >> 8) & 0xFF));
        foreach (var c in "BLOCKS FREE.")
            output.Add((byte)c);
        output.Add(0x00); // end of line

        // End of BASIC program: two zero link bytes.
        output.Add(0x00);
        output.Add(0x00);

        return output.ToArray();
    }

    /// <summary>Disk name from the BAM header (PETSCII, $A0 padding trimmed).</summary>
    public byte[] DiskNamePetscii()
    {
        var bam = _image.GetSector(DirectoryTrack, 0);
        return bam.Slice(BamDiskNameOffset, BamDiskNameLength).ToArray();
    }

    private static byte[] NormalizePattern(ReadOnlySpan<byte> pattern)
    {
        // Drop a "0:" / "1:" drive prefix and any ",P,R" trailing options.
        var start = 0;
        for (var i = 0; i < pattern.Length; i++)
        {
            if (pattern[i] == (byte)':')
            {
                start = i + 1;
                break;
            }
        }

        var end = pattern.Length;
        for (var i = start; i < pattern.Length; i++)
        {
            if (pattern[i] == (byte)',')
            {
                end = i;
                break;
            }
        }

        return pattern[start..end].ToArray();
    }

    private static bool MatchesPattern(ReadOnlySpan<byte> pattern, ReadOnlySpan<byte> name16)
    {
        var pi = 0;
        var ni = 0;

        while (pi < pattern.Length)
        {
            var pc = pattern[pi];
            if (pc == (byte)'*')
                return true; // matches the remainder of the name

            var nc = ni < name16.Length ? name16[ni] : PetsciiPad;
            if (nc == PetsciiPad)
                return false; // name is shorter than the (non-wildcard) pattern

            if (pc != (byte)'?' && pc != nc)
                return false;

            pi++;
            ni++;
        }

        var trailing = ni < name16.Length ? name16[ni] : PetsciiPad;
        return trailing == PetsciiPad;
    }

    private static void AppendPadConverted(List<byte> output, ReadOnlySpan<byte> field)
    {
        foreach (var b in field)
            output.Add(b == PetsciiPad ? (byte)0x20 : b);
    }

    private static string FileTypeMnemonic(byte typeByte) => (typeByte & FileTypeMask) switch
    {
        0x00 => "DEL",
        0x01 => "SEQ",
        FileTypePrg => "PRG",
        0x03 => "USR",
        0x04 => "REL",
        0x05 => "CBM",
        0x06 => "DIR",
        _ => "???"
    };

    private static int CountFreeBlocks(ReadOnlySpan<byte> bam)
    {
        // 1541 BAM: per-track entry of 4 bytes starting at 0x04; byte 0 of each
        // entry is the free-sector count. Tracks 1-35, excluding the directory
        // track 18 (as the real DOS reports it).
        var free = 0;
        for (var track = 1; track <= 35; track++)
        {
            if (track == DirectoryTrack)
                continue;
            free += bam[0x04 + (track - 1) * 4];
        }
        return free;
    }

    private static bool IsValidSector(int track, int sector)
    {
        var count = SectorsPerTrack(track);
        return count > 0 && sector >= 0 && sector < count;
    }

    private static int SectorsPerTrack(int track) => track switch
    {
        >= 1 and <= 17 => 21,
        >= 18 and <= 24 => 19,
        >= 25 and <= 30 => 18,
        >= 31 and <= 35 => 17,
        _ => 0
    };
}

/// <summary>One live 1541 directory slot.</summary>
public readonly record struct D64DirectoryEntry(
    byte[] NamePetscii,
    byte FileType,
    int StartTrack,
    int StartSector,
    int Blocks);
