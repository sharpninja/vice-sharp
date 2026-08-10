using ViceSharp.Core.Vic20;

namespace ViceSharp.Core.FlashCarts;

/// <summary>Built-in flash/ROM cart image profiles (VICE-aligned sizes).</summary>
public static class FlashCartProfiles
{
    public const int Bank8K = 0x2000;

    /// <summary>VIC-20 Final Expansion 3 flash: 512 KiB, 64 x 8K banks.</summary>
    public static IFlashCartProfile Fe3 { get; } = new FixedProfile(
        "fe3",
        "Final Expansion 3 (512K flash)",
        FinalExpansion3Cartridge.FlashSize,
        Bank8K,
        emptyFill: 0xFF,
        secondarySizeBytes: 0);

    /// <summary>VIC-20 Ultimem default image: 1 MiB, 8K banks.</summary>
    public static IFlashCartProfile Ultimem { get; } = new FixedProfile(
        "ultimem",
        "Ultimem (1MB image)",
        UltimemCartridge.DefaultImageSize,
        Bank8K,
        emptyFill: 0xFF,
        secondarySizeBytes: 0);

    /// <summary>VIC-20 Mega-Cart ROM (1 MiB) + 8K NVRAM secondary.</summary>
    public static IFlashCartProfile MegaCart { get; } = new FixedProfile(
        "megacart",
        "Mega-Cart (1MB ROM + 8K NVRAM)",
        MegaCartCartridge.LowRomSize,
        Bank8K,
        emptyFill: 0xFF,
        secondarySizeBytes: MegaCartCartridge.NvramSize);

    /// <summary>
    /// C64 EasyFlash-style banked flash layout (structure for reuse).
    /// Default 1 MiB = 128 x 8K banks (UI for EasyFlash is out of band).
    /// </summary>
    public static IFlashCartProfile EasyFlash { get; } = new FixedProfile(
        "easyflash",
        "EasyFlash (C64 layout, 8K banks)",
        Bank8K * 128,
        Bank8K,
        emptyFill: 0xFF,
        secondarySizeBytes: 0);

    public static IReadOnlyList<IFlashCartProfile> All { get; } =
        [Fe3, Ultimem, MegaCart, EasyFlash];

    public static IFlashCartProfile? TryGet(string id)
        => All.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));

    private sealed class FixedProfile : IFlashCartProfile
    {
        public FixedProfile(
            string id,
            string displayName,
            int imageSizeBytes,
            int bankSizeBytes,
            byte emptyFill,
            int secondarySizeBytes)
        {
            if (imageSizeBytes <= 0 || bankSizeBytes <= 0 || imageSizeBytes % bankSizeBytes != 0)
                throw new ArgumentException("Image size must be a positive multiple of bank size.");
            Id = id;
            DisplayName = displayName;
            ImageSizeBytes = imageSizeBytes;
            BankSizeBytes = bankSizeBytes;
            EmptyFill = emptyFill;
            SecondarySizeBytes = secondarySizeBytes;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public int ImageSizeBytes { get; }
        public int BankSizeBytes { get; }
        public int BankCount => ImageSizeBytes / BankSizeBytes;
        public byte EmptyFill { get; }
        public int SecondarySizeBytes { get; }
    }
}
