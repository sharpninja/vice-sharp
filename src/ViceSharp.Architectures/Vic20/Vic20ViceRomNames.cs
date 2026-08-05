namespace ViceSharp.Architectures.Vic20;

/// <summary>
/// Canonical VIC-20 ROM filenames as shipped under the VICE data tree
/// <c>VIC20/</c>. Note that kernal images use a dot separator in the part
/// number (VICE catalog quirk), unlike most other machine families.
/// </summary>
/// <remarks>
/// FR-PRF-005, FR-VIC20-002.
/// </remarks>
public static class Vic20ViceRomNames
{
    /// <summary>Architecture key passed to <see cref="Abstractions.IRomProvider"/>.</summary>
    public const string ArchitectureKey = "VIC20";

    /// <summary>BASIC V2 (8192 bytes) at $C000-$DFFF.</summary>
    public const string Basic = "basic-901486-01.bin";

    /// <summary>PAL KERNAL (8192 bytes) at $E000-$FFFF. VICE default for PAL.</summary>
    public const string KernalPal = "kernal.901486-07.bin";

    /// <summary>NTSC KERNAL (8192 bytes) at $E000-$FFFF. VICE default for NTSC.</summary>
    public const string KernalNtsc = "kernal.901486-06.bin";

    /// <summary>Older kernal revision still listed in the VICE catalog.</summary>
    public const string KernalRev2 = "kernal.901486-02.bin";

    /// <summary>Character generator (4096 bytes) at $8000-$8FFF.</summary>
    public const string Character = "chargen-901460-03.bin";

    /// <summary>Alternate character ROM listed in the VICE catalog.</summary>
    public const string CharacterRev2 = "chargen-901460-02.bin";

    /// <summary>BASIC ROM size in bytes.</summary>
    public const int BasicRomSize = 0x2000;

    /// <summary>KERNAL ROM size in bytes.</summary>
    public const int KernalRomSize = 0x2000;

    /// <summary>Character ROM size in bytes.</summary>
    public const int CharacterRomSize = 0x1000;
}
