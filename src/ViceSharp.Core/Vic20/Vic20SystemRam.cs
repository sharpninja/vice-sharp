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
    /// Clear only installed RAM; open-bus regions stay non-sticky (no store).
    /// Does not call C64-specific power-on patterns.
    /// </summary>
    public void Reset()
    {
        Array.Clear(_memory);
    }

    /// <summary>Force a store regardless of expansion (ROM load / factory only).</summary>
    public void LoadBytes(ushort startAddress, ReadOnlySpan<byte> data)
    {
        for (var i = 0; i < data.Length; i++)
            _memory[startAddress + i] = data[i];
    }
}
