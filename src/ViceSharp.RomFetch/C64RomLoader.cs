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
        Md5Hash = "57af4ae21d4b705c2991d98ed5c1f7b8",
        Sha1Hash = "79015323128650c742a3694c9429aa91f355905e"
    };

    public static readonly RomDescriptor KernalRom = new RomDescriptor
    {
        Name = "KERNAL ROM",
        Address = 0xE000,
        Size = 8192,
        Md5Hash = "39065497630802346bce17963f13c092",
        Sha1Hash = "1d503e56df85a62fee696e7618dc5b4e781df1bb"
    };

    public static readonly RomDescriptor CharacterRom = new RomDescriptor
    {
        Name = "CHARACTER ROM",
        Address = 0xD000,
        Size = 4096,
        Md5Hash = "12a4202f5331d45af846af6c58fba946",
        Sha1Hash = "adc7c31e18c7c7413d54802ef2f4193da14711aa"
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
