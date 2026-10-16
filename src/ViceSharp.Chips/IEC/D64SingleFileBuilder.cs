namespace ViceSharp.Chips.IEC;

/// <summary>
/// Builds a 35-track D64 that contains a single PRG (with load address). Used to surface T64
/// archives on the virtual 1541 so LOAD"*",8,1 / autostart can run them.
/// </summary>
public static class D64SingleFileBuilder
{
    private const int DirectoryTrack = 18;

    /// <summary>
    /// Creates a D64 image byte array containing <paramref name="prgWithLoadAddress"/> as one PRG.
    /// </summary>
    /// <param name="prgWithLoadAddress">PRG stream (2-byte load address + body).</param>
    /// <param name="fileName">Up to 16 PETSCII-ish characters for the directory name.</param>
    public static byte[] FromPrg(ReadOnlySpan<byte> prgWithLoadAddress, string fileName = "PROGRAM")
    {
        if (prgWithLoadAddress.Length < 2)
        {
            throw new ArgumentException("PRG must include a 2-byte load address.", nameof(prgWithLoadAddress));
        }

        var image = new D64Image(new byte[D64Image.DiskSize35Track]);
        image.Format();

        // Populate free-sector counts so the image looks like a normal DOS disk.
        var bam = image.GetSector(DirectoryTrack, 0);
        bam[0] = DirectoryTrack;
        bam[1] = 1;
        for (var bamTrack = 1; bamTrack <= 35; bamTrack++)
        {
            bam[0x04 + ((bamTrack - 1) * 4)] = (byte)SectorsPerTrack(bamTrack);
        }

        int track = 17;
        int sector = 0;
        int startTrack = track;
        int startSector = sector;
        int offset = 0;
        int remaining = prgWithLoadAddress.Length;
        int blocks = 0;

        while (remaining > 0)
        {
            var block = image.GetSector(track, sector);
            int dataBytes = Math.Min(254, remaining);
            bool last = remaining <= 254;

            if (last)
            {
                block[0] = 0;
                // Last valid byte index within the sector (VICE: nextSector holds last index).
                block[1] = (byte)(1 + dataBytes);
                prgWithLoadAddress.Slice(offset, dataBytes).CopyTo(block.Slice(2));
                blocks++;
                break;
            }

            if (!TryAdvanceSector(ref track, ref sector))
            {
                throw new InvalidOperationException("PRG is too large for a 35-track D64.");
            }

            block[0] = (byte)track;
            block[1] = (byte)sector;
            prgWithLoadAddress.Slice(offset, 254).CopyTo(block.Slice(2));
            offset += 254;
            remaining -= 254;
            blocks++;
        }

        var dir = image.GetSector(DirectoryTrack, 1);
        dir[0] = 0;
        dir[1] = 0xFF;
        // VICE slot layout: type/track/sector/name/blocks at +2/+3/+4/+5/+30
        dir[2] = 0x82; // closed PRG
        dir[3] = (byte)startTrack;
        dir[4] = (byte)startSector;
        WritePetsciiName(dir.Slice(5, 16), fileName);
        dir[30] = (byte)(blocks & 0xFF);
        dir[31] = (byte)((blocks >> 8) & 0xFF);

        // Account for used sectors on BAM free counts (best-effort).
        DeductBamFree(bam, startTrack, startSector, blocks);
        // Directory sectors 0 and 1 used.
        if (bam[0x04 + ((DirectoryTrack - 1) * 4)] >= 2)
        {
            bam[0x04 + ((DirectoryTrack - 1) * 4)] -= 2;
        }

        return image.ToArray();
    }

    private static void WritePetsciiName(Span<byte> dest, string name)
    {
        for (var i = 0; i < 16; i++)
        {
            dest[i] = i < name.Length
                ? (byte)char.ToUpperInvariant(name[i] > 127 ? '?' : name[i])
                : (byte)0xA0;
        }
    }

    private static void DeductBamFree(Span<byte> bam, int startTrack, int startSector, int blocks)
    {
        int t = startTrack;
        int s = startSector;
        for (var i = 0; i < blocks; i++)
        {
            int idx = 0x04 + ((t - 1) * 4);
            if (idx < bam.Length && bam[idx] > 0)
            {
                bam[idx]--;
            }

            if (!TryAdvanceSector(ref t, ref s))
            {
                break;
            }
        }
    }

    private static bool TryAdvanceSector(ref int track, ref int sector)
    {
        int max = SectorsPerTrack(track);
        sector++;
        if (sector < max)
        {
            return true;
        }

        sector = 0;
        track++;
        // Skip directory track for data.
        if (track == DirectoryTrack)
        {
            track = 19;
        }

        return track <= 35;
    }

    private static int SectorsPerTrack(int track) => track switch
    {
        >= 1 and <= 17 => 21,
        >= 18 and <= 24 => 19,
        >= 25 and <= 30 => 18,
        >= 31 and <= 35 => 17,
        _ => 0,
    };
}
