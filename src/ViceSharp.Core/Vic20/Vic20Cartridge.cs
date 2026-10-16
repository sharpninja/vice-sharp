using ViceSharp.Abstractions;

namespace ViceSharp.Core.Vic20;

/// <summary>
/// MVP VIC-20 cartridge image: raw payload mapped into a BLK region (default BLK5 $A000).
/// </summary>
/// <remarks>FR-VIC20-005.</remarks>
public sealed class Vic20Cartridge : IAddressSpace
{
    private readonly byte[] _image;
    private readonly ushort _start;
    private readonly ushort _end;

    public Vic20Cartridge(ReadOnlySpan<byte> image, ushort mapStart = 0xA000)
    {
        if (image.IsEmpty)
            throw new ArgumentException("Cartridge image is empty.", nameof(image));
        if (image.Length > 0x4000)
            throw new ArgumentException("MVP cart image max is 16KB.", nameof(image));

        _image = image.ToArray();
        _start = mapStart;
        _end = (ushort)(mapStart + _image.Length - 1);
        Id = new DeviceId(0x0C01);
    }

    public DeviceId Id { get; }
    public string Name => "VIC-20 cartridge";
    public ushort MapStart => _start;
    public ushort MapEnd => _end;
    public int Size => _image.Length;

    public void Reset() { }

    public bool HandlesAddress(ushort address)
        => address >= _start && address <= _end;

    public byte Read(ushort address) => _image[address - _start];
    public byte Peek(ushort address) => Read(address);
    public void Write(ushort address, byte value) { /* ROM cart */ }

    /// <summary>
    /// Load a raw .bin/.prg cart. If the first two bytes look like a load address
    /// (typical .prg), skip them and map the payload at that address when in BLK range.
    /// </summary>
    public static Vic20Cartridge LoadFromFile(string path)
    {
        var bytes = File.ReadAllBytes(path);
        if (bytes.Length >= 3 && bytes[0] + (bytes[1] << 8) is >= 0x2000 and <= 0xBFFF)
        {
            var loadAddr = (ushort)(bytes[0] | (bytes[1] << 8));
            return new Vic20Cartridge(bytes.AsSpan(2), loadAddr);
        }

        return new Vic20Cartridge(bytes, 0xA000);
    }

    /// <summary>Build from an in-memory PRG (load-addr header optional).</summary>
    public static Vic20Cartridge FromPrg(ReadOnlySpan<byte> prg)
    {
        if (prg.Length >= 3)
        {
            var loadAddr = (ushort)(prg[0] | (prg[1] << 8));
            if (loadAddr is >= 0x2000 and <= 0xBFFF)
                return new Vic20Cartridge(prg[2..], loadAddr);
        }

        return new Vic20Cartridge(prg, 0xA000);
    }
}
