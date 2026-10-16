using ViceSharp.Abstractions;

namespace ViceSharp.Host.Runtime;

/// <summary>
/// FR-UIDROP-002: load a Commodore PRG (little-endian load address + payload)
/// into the current machine bus. BASIC-start is the live TXTTAB at $2B/$2C
/// (C64 $0801; VIC-20 unexpanded $1001, +3K $0401, +8K $1201).
/// </summary>
public static class PrgMemoryLoader
{
    /// <summary>BASIC TXTTAB (start of program).</summary>
    public const ushort BasicTxtTab = 0x002B;

    /// <summary>BASIC VARTAB (start of variables / end of program).</summary>
    public const ushort BasicVarTab = 0x002D;

    /// <summary>BASIC ARYTAB (start of arrays).</summary>
    public const ushort BasicAryTab = 0x002F;

    /// <summary>BASIC STREND (end of strings).</summary>
    public const ushort BasicStrEnd = 0x0031;

    /// <summary>
    /// Write a PRG payload at its load address. When the load address equals a
    /// non-zero TXTTAB, update VARTAB/ARYTAB/STREND and report <c>Ran</c>.
    /// </summary>
    public static PrgLoadResult Load(IBus bus, ReadOnlySpan<byte> prg)
    {
        ArgumentNullException.ThrowIfNull(bus);

        if (prg.Length < 3)
        {
            return PrgLoadResult.Fail("PRG must include a 2-byte load address and at least one data byte.");
        }

        var loadAddress = (ushort)(prg[0] | (prg[1] << 8));
        var payload = prg[2..];
        var end = loadAddress + payload.Length;
        if (end > 0x10000)
        {
            return PrgLoadResult.Fail("PRG payload overruns the 16-bit address space.");
        }

        for (var i = 0; i < payload.Length; i++)
            bus.Write((ushort)(loadAddress + i), payload[i]);

        var txtTab = ReadWord(bus, BasicTxtTab);
        var atBasicStart = txtTab != 0 && loadAddress == txtTab;
        if (atBasicStart)
        {
            var endAddress = (ushort)end;
            WriteWord(bus, BasicVarTab, endAddress);
            WriteWord(bus, BasicAryTab, endAddress);
            WriteWord(bus, BasicStrEnd, endAddress);
        }

        return new PrgLoadResult(true, loadAddress, payload.Length, atBasicStart, null);
    }

    private static ushort ReadWord(IBus bus, ushort address)
        => (ushort)(bus.Peek(address) | (bus.Peek((ushort)(address + 1)) << 8));

    private static void WriteWord(IBus bus, ushort address, ushort value)
    {
        bus.Write(address, (byte)value);
        bus.Write((ushort)(address + 1), (byte)(value >> 8));
    }
}

/// <summary>Result of <see cref="PrgMemoryLoader.Load"/>.</summary>
public readonly record struct PrgLoadResult(
    bool Success,
    ushort LoadAddress,
    int ByteCount,
    bool Ran,
    string? Error)
{
    /// <summary>Failed load; no bytes were written.</summary>
    public static PrgLoadResult Fail(string error)
        => new(false, 0, 0, false, error);
}
