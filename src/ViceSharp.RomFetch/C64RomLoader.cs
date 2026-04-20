using System.Buffers.Binary;
using ViceSharp.Abstractions;

namespace ViceSharp.RomFetch;

/// <summary>
/// C64 Official ROM loader with checksum validation.
/// </summary>
public sealed class C64RomLoader
{
    public static readonly RomDescriptor BasicRom = new RomDescriptor
    {
        Name = "BASIC ROM",
        Address = 0xA000,
        Size = 8192,
        Md5Hash = "57af4ae21d4b705c2991d98e55e65057",
        Sha1Hash = "790153939283a7080421f8c910206e6040f4a687"
    };

    public static readonly RomDescriptor KernalRom = new RomDescriptor
    {
        Name = "KERNAL ROM",
        Address = 0xE000,
        Size = 8192,
        Md5Hash = "39065497630802346bce175ab5387a5e",
        Sha1Hash = "1d5036ae56b4749a6c3618685863cc378dd500d8"
    };

    public static readonly RomDescriptor CharacterRom = new RomDescriptor
    {
        Name = "CHARACTER ROM",
        Address = 0xD000,
        Size = 4096,
        Md5Hash = "12a41f0341afca68a79056340ddc8840",
        Sha1Hash = "f32e04c649814e3f182450a1797b14d778106c99"
    };

    private readonly IBus _bus;

    public C64RomLoader(IBus bus)
    {
        _bus = bus;
    }

    /// <summary>
    /// Load ROM image into memory with checksum validation
    /// </summary>
    public bool LoadRom(ReadOnlySpan<byte> data, RomDescriptor descriptor, bool validateHash = true)
    {
        if (data.Length != descriptor.Size)
            return false;

        if (validateHash && !string.IsNullOrEmpty(descriptor.Md5Hash))
        {
            var hash = ComputeMd5(data);
            if (!string.Equals(hash, descriptor.Md5Hash, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        for (int i = 0; i < descriptor.Size; i++)
        {
            _bus.Write((ushort)(descriptor.Address + i), data[i]);
        }

        return true;
    }
    
    /// <summary>
    /// Load ROM from file path
    /// </summary>
    public bool LoadRomFromFile(string path, RomDescriptor descriptor)
    {
        if (!File.Exists(path))
            return false;
        
        var data = File.ReadAllBytes(path);
        return LoadRom(data, descriptor);
    }
    
    private static string ComputeMd5(ReadOnlySpan<byte> data)
    {
        using var md5 = System.Security.Cryptography.MD5.Create();
        var hash = md5.ComputeHash(data.ToArray());
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// Load all standard C64 ROMs
    /// </summary>
    public bool LoadAllRoms(ReadOnlySpan<byte> basic, ReadOnlySpan<byte> kernal, ReadOnlySpan<byte> character)
    {
        return LoadRom(basic, BasicRom)
            && LoadRom(kernal, KernalRom)
            && LoadRom(character, CharacterRom);
    }
}

/// <summary>
/// ROM image descriptor
/// </summary>
public record RomDescriptor
{
    public string Name { get; init; } = string.Empty;
    public ushort Address { get; init; }
    public int Size { get; init; }
    public string Md5Hash { get; init; } = string.Empty;
    public string Sha1Hash { get; init; } = string.Empty;
}