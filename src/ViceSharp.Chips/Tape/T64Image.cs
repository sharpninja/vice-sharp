namespace ViceSharp.Chips.Tape;

/// <summary>
/// C64S T64 tape container (directory of PRG-like files). Not pulse data like TAP; used by
/// emulators as a file archive. Attach path extracts the first program and materializes a D64.
/// Spec: Schepers T64.TXT / VICE file formats.
/// </summary>
public sealed class T64Image
{
    private readonly byte[] _data;
    private readonly int _maxEntries;
    private readonly int _usedEntries;

    private T64Image(byte[] data, int maxEntries, int usedEntries)
    {
        _data = data;
        _maxEntries = maxEntries;
        _usedEntries = usedEntries;
    }

    /// <summary>True when <paramref name="imageData"/> begins with a T64 signature ("C64...").</summary>
    public static bool IsT64(ReadOnlySpan<byte> imageData) =>
        imageData.Length >= 64
        && imageData[0] == (byte)'C'
        && imageData[1] == (byte)'6'
        && imageData[2] == (byte)'4';

    /// <summary>Opens a T64 container, or returns false when the signature/header is invalid.</summary>
    public static bool TryOpen(ReadOnlySpan<byte> imageData, out T64Image? image)
    {
        image = null;
        if (!IsT64(imageData))
        {
            return false;
        }

        int maxEntries = imageData[0x22] | (imageData[0x23] << 8);
        int usedEntries = imageData[0x24] | (imageData[0x25] << 8);
        if (maxEntries <= 0 || maxEntries > 4096)
        {
            // Some files leave maxEntries zero; fall back to used or a small default directory.
            maxEntries = usedEntries > 0 ? usedEntries : 30;
        }

        long dirBytes = 64L + (32L * maxEntries);
        if (imageData.Length < dirBytes)
        {
            return false;
        }

        image = new T64Image(imageData.ToArray(), maxEntries, usedEntries);
        return true;
    }

    /// <summary>
    /// Extracts the first usable file as a PRG stream (2-byte little-endian load address + body).
    /// File size is derived from the next entry's offset (Schepers workaround for broken end addresses).
    /// </summary>
    public bool TryExtractFirstProgram(out byte[] prgWithLoadAddress)
    {
        prgWithLoadAddress = Array.Empty<byte>();
        var entries = new List<(int Start, int End, int Offset, int Index)>();

        for (var i = 0; i < _maxEntries; i++)
        {
            int baseOff = 0x40 + (i * 32);
            if (baseOff + 32 > _data.Length)
            {
                break;
            }

            byte c64sType = _data[baseOff];
            byte fileType = _data[baseOff + 1];
            // Free / empty slots: type 0 with no useful content.
            if (c64sType == 0 && fileType == 0)
            {
                continue;
            }

            // Snapshot / reserved: skip non-normal tape files when c64sType > 1 and fileType==0.
            if (c64sType > 1 && fileType == 0)
            {
                continue;
            }

            int load = _data[baseOff + 2] | (_data[baseOff + 3] << 8);
            int end = _data[baseOff + 4] | (_data[baseOff + 5] << 8);
            int offset = _data[baseOff + 8]
                | (_data[baseOff + 9] << 8)
                | (_data[baseOff + 10] << 16)
                | (_data[baseOff + 11] << 24);

            if (offset < 0 || offset >= _data.Length)
            {
                continue;
            }

            entries.Add((load, end, offset, i));
        }

        if (entries.Count == 0)
        {
            return false;
        }

        entries.Sort((a, b) => a.Offset.CompareTo(b.Offset));
        var first = entries[0];
        int nextOffset = entries.Count > 1 ? entries[1].Offset : _data.Length;
        if (nextOffset <= first.Offset)
        {
            return false;
        }

        int bodyLen = nextOffset - first.Offset;
        // Reject absurd lengths.
        if (bodyLen <= 0 || bodyLen > 0x10000)
        {
            // Fall back to end-start when offset delta looks wrong.
            int fromEnd = first.End - first.Start;
            if (fromEnd > 0 && fromEnd <= 0x10000 && first.End != 0xC3C6)
            {
                bodyLen = fromEnd;
            }
            else
            {
                return false;
            }
        }

        if (first.Offset + bodyLen > _data.Length)
        {
            bodyLen = _data.Length - first.Offset;
        }

        if (bodyLen <= 0)
        {
            return false;
        }

        prgWithLoadAddress = new byte[bodyLen + 2];
        prgWithLoadAddress[0] = (byte)(first.Start & 0xFF);
        prgWithLoadAddress[1] = (byte)((first.Start >> 8) & 0xFF);
        Buffer.BlockCopy(_data, first.Offset, prgWithLoadAddress, 2, bodyLen);
        return true;
    }
}
