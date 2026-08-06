using ViceSharp.Abstractions;

namespace ViceSharp.Core.Vic20;

/// <summary>
/// VIC-20 system RAM with expansion-aware mapping.
/// Only <see cref="Vic20MemoryLayout.IsInstalledRam"/> regions are claimed on the
/// bus; uninstalled BLK/3K fall through to <see cref="BasicBus"/> open-bus
/// (VICE <c>vic20_cpu_last_data</c> / <c>read_unconnected_c_bus</c>) so KERNAL
/// expansion probes see last-bus data, not a sticky store or fixed 0xFF.
/// </summary>
/// <remarks>FR-VIC20-002.</remarks>
public sealed class Vic20SystemRam : IMemory
{
    private readonly byte[] _memory = new byte[65536];
    private readonly Vic20ExpansionKind _expansion;

    public Vic20SystemRam(Vic20ExpansionKind expansion = Vic20ExpansionKind.Unexpanded)
    {
        _expansion = expansion;
        Id = new DeviceId(0x0101);
        Reset();
    }

    public DeviceId Id { get; }
    public string Name => "VIC-20 system RAM";
    public Span<byte> Span => _memory;
    public Vic20ExpansionKind Expansion => _expansion;

    /// <inheritdoc />
    /// <remarks>
    /// Uninstalled expansion is not claimed so the bus can implement VICE last-data
    /// open bus. Installed base/expansion RAM is normal R/W.
    /// </remarks>
    public bool HandlesAddress(ushort address)
        => Vic20MemoryLayout.IsInstalledRam(_expansion, address);

    public byte Read(ushort address) => _memory[address];

    public byte Peek(ushort address) => _memory[address];

    public void Write(ushort address, byte value) => _memory[address] = value;

    /// <summary>
    /// Power-on fill matching VICE VIC-20 factory RAM init
    /// (<c>ram.c</c>: RAMInitStartValue=255, RAMInitValueInvert=1, offset=0):
    /// alternating <c>0xFF</c>/<c>0x00</c> by address byte. Open-bus regions are
    /// not claimed on the bus; values written here are unused for uninstalled BLK.
    /// </summary>
    public void Reset()
    {
        // VICE vic20 factory: start_value=255, value_invert=1 => FF,00,FF,00,...
        for (var i = 0; i < _memory.Length; i++)
            _memory[i] = (byte)((i & 1) == 0 ? 0xFF : 0x00);
    }

    /// <summary>Force a store regardless of expansion (ROM load / factory only).</summary>
    public void LoadBytes(ushort startAddress, ReadOnlySpan<byte> data)
    {
        for (var i = 0; i < data.Length; i++)
            _memory[startAddress + i] = data[i];
    }
}
