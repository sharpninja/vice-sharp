using System.Buffers.Binary;

namespace ViceSharp.ViceCompatibility;

/// <summary>
/// External byte adapter for TR-VSFLOCKSTEP-RESUME-001 AC1. It does not execute,
/// patch, or replace VICE. Layouts are pinned to VICE commit
/// cc67418cba58e71d8e70fd353e9de47199ad2075: mainc64cpu.c:884-978,
/// interrupt.c:385-419, snapshot.c:693-699/815-848, snapshot.h:112,
/// and c64/c64.c:1481-1487. Existing fields are never reinterpreted.
/// </summary>
public static class X64ScSnapshotCompatibility
{
    private const int ModuleHeaderSize = 22;

    /// <summary>
    /// Validates an uncompressed C64SC VSF 2.0 container and its complete module
    /// chain, then expands MAINCPU 1.2/1.3 to the pinned 1.4 layout. Defaults for
    /// absent ANE/LXA logging and jammed state are zero, matching mainc64cpu.c.
    /// Current 1.4 bytes pass through unchanged. Input is never modified.
    /// </summary>
    /// <param name="input">Complete snapshot bytes, not a path or a module fragment.</param>
    /// <returns>A separately owned validated byte array.</returns>
    /// <exception cref="InvalidDataException">
    /// The container, module chain, machine, version, or exact CPU layout is
    /// malformed or unsupported. No partially normalized output is returned.
    /// </exception>
    /// <remarks>
    /// This byte contract does not establish native resume correctness. Native
    /// fixture resume and round-trip tests remain mandatory before integration.
    /// </remarks>
    public static byte[] Normalize(ReadOnlySpan<byte> input)
    {
        if (input.Length < 37 || !input[..19].SequenceEqual("VICE Snapshot File\x1a"u8))
            throw Invalid("Missing or truncated VSF header.");
        if (input[19] != 2 || input[20] != 0)
            throw Invalid("Only VSF container version 2.0 is supported.");
        // x64 and x64sc have distinct CPU layouts despite the MAINCPU name.
        if (!input.Slice(21, 6).SequenceEqual("C64SC\0"u8))
            throw Invalid("Only the C64SC machine layout is supported.");

        var offset = 37;
        if (input.Length >= 50 && input.Slice(37, 13).SequenceEqual("VICE Version\x1a"u8))
        {
            if (input.Length < 58) throw Invalid("Truncated VICE version header.");
            offset = 58;
        }

        var cpuOffset = -1;
        var cpuSize = 0;
        byte cpuMinor = 0;
        while (offset < input.Length)
        {
            var remaining = input.Length - offset;
            if (remaining < ModuleHeaderSize) throw Invalid("Truncated module header.");
            var size = BinaryPrimitives.ReadUInt32LittleEndian(input.Slice(offset + 18, 4));
            // Compare unsigned declared lengths before conversion or addition.
            if (size < ModuleHeaderSize || size > (uint)remaining)
                throw Invalid("Module length is outside the snapshot boundary.");
            if (input.Slice(offset, 8).SequenceEqual("MAINCPU\0"u8))
            {
                if (cpuOffset >= 0) throw Invalid("Duplicate MAINCPU module.");
                cpuMinor = input[offset + 17];
                if (input[offset + 16] != 1 || cpuMinor is < 2 or > 4)
                    throw Invalid("Unsupported MAINCPU version.");
                var expectedPayload = cpuMinor switch { 2 => 91, 3 => 99, _ => 103 };
                if (size != ModuleHeaderSize + expectedPayload)
                    throw Invalid("MAINCPU payload does not match its pinned layout.");
                cpuOffset = offset;
                cpuSize = (int)size;
            }
            offset += (int)size;
        }
        if (cpuOffset < 0) throw Invalid("Missing MAINCPU module.");

        // Do not return even an unchanged 1.4 copy until all later modules pass.
        if (cpuMinor == 4) return input.ToArray();
        var growth = cpuMinor == 2 ? 12 : 4;
        if (input.Length > int.MaxValue - growth) throw Invalid("Snapshot is too large.");
        var insertion = cpuOffset + ModuleHeaderSize + (cpuMinor == 2 ? 19 : 27);
        var output = new byte[input.Length + growth];
        input[..insertion].CopyTo(output);
        input[insertion..].CopyTo(output.AsSpan(insertion + growth));
        // The newly allocated gap contains precisely the missing zero defaults.
        output[cpuOffset + 17] = 4;
        BinaryPrimitives.WriteUInt32LittleEndian(output.AsSpan(cpuOffset + 18, 4),
            (uint)(cpuSize + growth));
        return output;
    }

    private static InvalidDataException Invalid(string message) => new(message);
}
