using ViceSharp.Abstractions;

namespace ViceSharp.Core.Vic20;

/// <summary>
/// VIC-20 color nybble RAM at $9400-$97FF (FR-VIC20-002).
/// Real hardware stores only the low nibble; reads OR the high nibble from the
/// previous V-bus data (VICE <c>colorram_read</c> /
/// <c>vic20_v_bus_last_data</c>). Full-byte RAM here made LDA (zp),Y of color
/// cells return $0x instead of $Nx and broke every-cycle lockstep (c=577850
/// mA=$01 nA=$91 at $966E).
/// </summary>
public sealed class Vic20ColorRam : IMemory
{
    public const ushort StartAddress = 0x9400;
    public const ushort EndAddress = 0x97FF;
    private const int Size = 0x0400;

    private readonly byte[] _nibbles = new byte[Size];
    private readonly BasicBus _bus;

    public Vic20ColorRam(BasicBus bus)
    {
        _bus = bus;
    }

    public DeviceId Id => new(0x00019400);
    public string Name => "VIC-20 Color RAM";
    public Span<byte> Span => _nibbles;

    public bool HandlesAddress(ushort address)
        => address is >= StartAddress and <= EndAddress;

    public byte Read(ushort address)
    {
        var nibble = (byte)(_nibbles[address - StartAddress] & 0x0F);
        // VICE colorram_read: mem_ram[addr] | (vic20_v_bus_last_data & 0xf0)
        // then vic20_v_bus_last_data = result. VBusLastData is refreshed by
        // CPU V-bus region access and VIC display fetches (NoteVicDisplayFetch).
        var value = (byte)(nibble | (_bus.VBusLastData & 0xF0));
        _bus.NoteVBusData(value);
        return value;
    }

    public void Write(ushort address, byte value)
    {
        // VICE colorram_store: value & 0xf only; v_bus_last_data = value.
        _nibbles[address - StartAddress] = (byte)(value & 0x0F);
        _bus.NoteVBusData(value);
    }

    public byte Peek(ushort address)
    {
        // VICE colorram_peek: no v_bus side effect.
        var nibble = (byte)(_nibbles[address - StartAddress] & 0x0F);
        return (byte)(nibble | (_bus.VBusLastData & 0xF0));
    }

    public void Reset()
    {
        Array.Clear(_nibbles);
    }
}
