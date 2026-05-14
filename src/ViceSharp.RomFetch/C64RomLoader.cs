using System.Buffers.Binary;
using ViceSharp.Abstractions;

namespace ViceSharp.RomFetch;

/// <summary>
/// C64 Official ROM loader with checksum validation.
/// </summary>
public sealed class C64RomLoader
{
    private const string KernalNoneRomName = "kernal-none.bin";

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

    public static readonly RomDescriptor KernalRev1Rom = KernalRom with
    {
        Name = "KERNAL ROM 901227-01",
        Md5Hash = "1ae0ea224f2b291dafa2c20b990bb7d4",
        Sha1Hash = "87cc04d61fc748b82df09856847bb5c2754a2033"
    };

    public static readonly RomDescriptor KernalRev2Rom = KernalRom with
    {
        Name = "KERNAL ROM 901227-02",
        Md5Hash = "7360b296d64e18b88f6cf52289fd99a1",
        Sha1Hash = "0e2e4ee3f2d41f00bed72f9ab588b83e306fdb13"
    };

    public static readonly RomDescriptor KernalSx64Rom = KernalRom with
    {
        Name = "SX-64 KERNAL ROM 251104-04",
        Md5Hash = "187b8c713b51931e070872bd390b472a",
        Sha1Hash = "aa136e91ecf3c5ac64f696b3dbcbfc5ba0871c98"
    };

    public static readonly RomDescriptor KernalPet64Rom = KernalRom with
    {
        Name = "PET64 KERNAL ROM 901246-01",
        Md5Hash = "da92801e3a03b005b746a4dd0b639c7c",
        Sha1Hash = "6c4fa9465f6091b174df27dfe679499df447503c"
    };

    public static readonly RomDescriptor KernalJapaneseRom = KernalRom with
    {
        Name = "Japanese C64 KERNAL ROM 906145-02",
        Md5Hash = "479553fd53346ec84054f0b1c6237397",
        Sha1Hash = "4ff0f11e80f4b57430d8f0c3799ed0f0e0f4565d"
    };

    public static readonly RomDescriptor KernalGsRom = KernalRom with
    {
        Name = "C64GS KERNAL ROM 390852-01",
        Md5Hash = "ddee89b0fed19572da5245ea68ff11b5",
        Sha1Hash = "3ad6cc1837c679a11f551ad1cf1a32dd84ace719"
    };

    public static readonly RomDescriptor CharacterRom = new RomDescriptor
    {
        Name = "CHARACTER ROM",
        Address = 0xD000,
        Size = 4096,
        Md5Hash = "12a4202f5331d45af846af6c58fba946",
        Sha1Hash = "adc7c31e18c7c7413d54802ef2f4193da14711aa"
    };

    public static readonly RomDescriptor CharacterJapaneseRom = CharacterRom with
    {
        Name = "Japanese C64 character ROM 906143-02",
        Md5Hash = "cf32a93c0a693ed359a4f483ef6db53d",
        Sha1Hash = string.Empty
    };

    private static readonly IReadOnlyDictionary<string, RomDescriptor> BasicDescriptors =
        new Dictionary<string, RomDescriptor>(StringComparer.OrdinalIgnoreCase)
        {
            ["basic"] = BasicRom,
            ["basic-901226-01.bin"] = BasicRom
        };

    private static readonly IReadOnlyDictionary<string, RomDescriptor> KernalDescriptors =
        new Dictionary<string, RomDescriptor>(StringComparer.OrdinalIgnoreCase)
        {
            ["kernal"] = KernalRom,
            ["kernal-901227-03.bin"] = KernalRom,
            ["kernal-901227-02.bin"] = KernalRev2Rom,
            ["kernal-901227-01.bin"] = KernalRev1Rom,
            ["kernal-251104-04.bin"] = KernalSx64Rom,
            ["kernal-901246-01.bin"] = KernalPet64Rom,
            ["kernal-906145-02.bin"] = KernalJapaneseRom,
            ["kernal-390852-01.bin"] = KernalGsRom
        };

    private static readonly IReadOnlyDictionary<string, RomDescriptor> CharacterDescriptors =
        new Dictionary<string, RomDescriptor>(StringComparer.OrdinalIgnoreCase)
        {
            ["characters"] = CharacterRom,
            ["chargen-901225-01.bin"] = CharacterRom,
            ["chargen-906143-02.bin"] = CharacterJapaneseRom
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
        return LoadAllRoms(basic, kernal, character, "basic", "kernal", "characters");
    }

    /// <summary>
    /// Load a profile-selected C64 ROM set.
    /// </summary>
    public bool LoadAllRoms(
        ReadOnlySpan<byte> basic,
        ReadOnlySpan<byte> kernal,
        ReadOnlySpan<byte> character,
        string basicRomName,
        string kernalRomName,
        string characterRomName)
    {
        return LoadRom(basic, ResolveDescriptor(BasicDescriptors, basicRomName, BasicRom))
            && (IsKernalRomRequired(kernalRomName)
                ? LoadRom(kernal, ResolveDescriptor(KernalDescriptors, kernalRomName, KernalRom))
                : true)
            && LoadRom(character, ResolveDescriptor(CharacterDescriptors, characterRomName, CharacterRom));
    }

    private static bool IsKernalRomRequired(string kernalRomName)
        => !string.Equals(kernalRomName, KernalNoneRomName, StringComparison.OrdinalIgnoreCase);

    private static RomDescriptor ResolveDescriptor(
        IReadOnlyDictionary<string, RomDescriptor> descriptors,
        string romName,
        RomDescriptor fallback)
    {
        if (descriptors.TryGetValue(romName, out var descriptor))
            return descriptor;

        return fallback with
        {
            Name = romName,
            Md5Hash = string.Empty,
            Sha1Hash = string.Empty
        };
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
