using ViceSharp.Abstractions;

namespace ViceSharp.Core.Vic20;

/// <summary>
/// FR-VIC20-005. A plain VIC-20 BLK image, not FE3, Ultimem, or Mega-Cart.
/// An 8K image is BLK5 at $A000. A 16K image is BLK1+BLK2 at $2000.
/// A 24K image is BLK1 through BLK3 at $2000.
/// </summary>
public sealed class StandardBlockCartridge : IAddressSpace
{
    private readonly byte[] _image;
    private readonly ushort _base;

    public StandardBlockCartridge(ReadOnlySpan<byte> image)
    {
        if (image.Length is not (0x2000 or 0x4000 or 0x6000))
            throw new ArgumentException("A standard BLK image is 8K, 16K, or 24K.", nameof(image));

        _image = image.ToArray();
        _base = image.Length == 0x2000 ? (ushort)0xA000 : (ushort)0x2000;
    }

    public DeviceId Id => new(0x0C20);

    public string Name => "VIC-20 standard block cartridge";

    public bool HandlesAddress(ushort address)
        => address >= _base && address < _base + _image.Length;

    public byte Read(ushort address) => _image[address - _base];

    public byte Peek(ushort address) => Read(address);

    public void Write(ushort address, byte value)
    {
    }

    public void Reset()
    {
    }
}
