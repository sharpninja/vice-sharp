using ViceSharp.Abstractions;

namespace ViceSharp.Architectures.C64;

public enum C64VicIIModel
{
    Mos6569,
    Mos6569R1,
    Mos6567R56A,
    Mos6567R8,
    Mos6572,
    Mos8565,
    Mos8562
}

public enum C64SidModel
{
    Mos6581,
    Mos8580
}

public enum C64BoardModel
{
    Breadbox,
    BreadboxOld,
    C64C,
    Drean,
    SX64,
    PET64,
    Ultimax,
    C64GS,
    Japanese
}

public sealed record C64MachineProfile(
    string Id,
    string DisplayName,
    IReadOnlyList<string> Aliases,
    long NominalClockHz,
    VideoStandard VideoStandard,
    int CyclesPerLine,
    int RasterLines,
    C64VicIIModel VicII,
    C64SidModel Sid,
    C64BoardModel Board,
    string RomSet,
    string BasicRomName = C64ViceRomNames.Basic,
    string KernalRomName = C64ViceRomNames.KernalRev3,
    string CharacterRomName = C64ViceRomNames.Character,
    bool KeyboardEnabled = true,
    bool CartridgeBootExpected = false,
    C64SystemCoreDefinition? CoreDefinition = null) : IMachineProfile
{
    public string Family => "x64sc";

    public double RefreshRateHz => NominalClockHz / (double)(CyclesPerLine * RasterLines);

    public string VicIIModel => VicII.ToString();

    public string SidModel => Sid.ToString();

    public string BoardModel => Board.ToString();

    public ISystemCoreDefinition SystemCore { get; } =
        CoreDefinition ?? C64SystemCoreDefinitions.ForProfile(Board, KeyboardEnabled, CartridgeBootExpected);
}

public static class C64MachineProfiles
{
    public static C64MachineProfile Default => C64Pal;

    public static C64MachineProfile C64Pal { get; } = new(
        "c64",
        "Commodore 64 PAL",
        ["c64", "breadbox", "pal", "c64-pal", "commodore64"],
        985_248,
        VideoStandard.Pal,
        63,
        312,
        C64VicIIModel.Mos6569,
        C64SidModel.Mos6581,
        C64BoardModel.Breadbox,
        "C64");

    public static C64MachineProfile C64CPal { get; } = new(
        "c64c",
        "Commodore 64C PAL",
        ["c64c", "c64new", "newpal", "c64c-pal"],
        985_248,
        VideoStandard.Pal,
        63,
        312,
        C64VicIIModel.Mos8565,
        C64SidModel.Mos8580,
        C64BoardModel.C64C,
        "C64");

    public static C64MachineProfile C64OldPal { get; } = new(
        "c64old",
        "Commodore 64 old PAL",
        ["c64old", "oldpal", "c64old-pal"],
        985_248,
        VideoStandard.Pal,
        63,
        312,
        C64VicIIModel.Mos6569R1,
        C64SidModel.Mos6581,
        C64BoardModel.BreadboxOld,
        "C64",
        KernalRomName: C64ViceRomNames.KernalRev2);

    public static C64MachineProfile C64Ntsc { get; } = new(
        "ntsc",
        "Commodore 64 NTSC",
        ["ntsc", "c64ntsc", "c64-ntsc"],
        1_022_730,
        VideoStandard.Ntsc,
        65,
        263,
        C64VicIIModel.Mos6567R8,
        C64SidModel.Mos6581,
        C64BoardModel.Breadbox,
        "C64");

    public static C64MachineProfile C64CNtsc { get; } = new(
        "newntsc",
        "Commodore 64C NTSC",
        ["c64cntsc", "newntsc", "c64newntsc", "c64c-ntsc"],
        1_022_730,
        VideoStandard.Ntsc,
        65,
        263,
        C64VicIIModel.Mos8562,
        C64SidModel.Mos8580,
        C64BoardModel.C64C,
        "C64");

    public static C64MachineProfile C64OldNtsc { get; } = new(
        "oldntsc",
        "Commodore 64 old NTSC",
        ["oldntsc", "c64oldntsc", "c64old-ntsc"],
        1_022_730,
        VideoStandard.Ntsc,
        64,
        262,
        C64VicIIModel.Mos6567R56A,
        C64SidModel.Mos6581,
        C64BoardModel.BreadboxOld,
        "C64",
        KernalRomName: C64ViceRomNames.KernalRev1);

    public static C64MachineProfile C64PalN { get; } = new(
        "paln",
        "Commodore 64 PAL-N / Drean",
        ["paln", "drean", "c64-paln"],
        1_023_440,
        VideoStandard.Pal,
        65,
        312,
        C64VicIIModel.Mos6572,
        C64SidModel.Mos6581,
        C64BoardModel.Drean,
        "C64");

    public static C64MachineProfile SX64Pal { get; } = new(
        "sx64pal",
        "Commodore SX-64 PAL",
        ["sx64", "sx64pal", "sx64-pal"],
        985_248,
        VideoStandard.Pal,
        63,
        312,
        C64VicIIModel.Mos6569,
        C64SidModel.Mos6581,
        C64BoardModel.SX64,
        "C64",
        KernalRomName: C64ViceRomNames.KernalSx64);

    public static C64MachineProfile SX64Ntsc { get; } = new(
        "sx64ntsc",
        "Commodore SX-64 NTSC",
        ["sx64ntsc", "sx64-ntsc"],
        1_022_730,
        VideoStandard.Ntsc,
        65,
        263,
        C64VicIIModel.Mos6567R8,
        C64SidModel.Mos6581,
        C64BoardModel.SX64,
        "C64",
        KernalRomName: C64ViceRomNames.KernalSx64);

    public static C64MachineProfile PET64Pal { get; } = new(
        "pet64pal",
        "Commodore PET64 PAL",
        ["pet64", "pet64pal", "pet64-pal"],
        985_248,
        VideoStandard.Pal,
        63,
        312,
        C64VicIIModel.Mos6569,
        C64SidModel.Mos6581,
        C64BoardModel.PET64,
        "C64",
        KernalRomName: C64ViceRomNames.Kernal4064);

    public static C64MachineProfile PET64Ntsc { get; } = new(
        "pet64ntsc",
        "Commodore PET64 NTSC",
        ["pet64ntsc", "pet64-ntsc"],
        1_022_730,
        VideoStandard.Ntsc,
        65,
        263,
        C64VicIIModel.Mos6567R8,
        C64SidModel.Mos6581,
        C64BoardModel.PET64,
        "C64",
        KernalRomName: C64ViceRomNames.Kernal4064);

    public static C64MachineProfile Ultimax { get; } = new(
        "ultimax",
        "Commodore MAX / Ultimax",
        ["max", "ultimax"],
        1_022_730,
        VideoStandard.Ntsc,
        65,
        263,
        C64VicIIModel.Mos6567R8,
        C64SidModel.Mos6581,
        C64BoardModel.Ultimax,
        "C64",
        KernalRomName: C64ViceRomNames.KernalNone,
        CartridgeBootExpected: true);

    public static C64MachineProfile C64GS { get; } = new(
        "c64gs",
        "Commodore 64 Games System",
        ["gs", "c64gs"],
        985_248,
        VideoStandard.Pal,
        63,
        312,
        C64VicIIModel.Mos8565,
        C64SidModel.Mos8580,
        C64BoardModel.C64GS,
        "C64",
        KernalRomName: C64ViceRomNames.KernalGs,
        KeyboardEnabled: false,
        CartridgeBootExpected: true);

    public static C64MachineProfile C64Japanese { get; } = new(
        "c64jap",
        "Commodore 64 Japanese",
        ["jap", "c64jap"],
        1_022_730,
        VideoStandard.Ntsc,
        65,
        263,
        C64VicIIModel.Mos6567R8,
        C64SidModel.Mos6581,
        C64BoardModel.Japanese,
        "C64",
        KernalRomName: C64ViceRomNames.KernalJapanese,
        CharacterRomName: C64ViceRomNames.CharacterJapanese);

    public static IReadOnlyList<C64MachineProfile> All { get; } =
    [
        C64Pal,
        C64CPal,
        C64OldPal,
        C64Ntsc,
        C64CNtsc,
        C64OldNtsc,
        C64PalN,
        SX64Pal,
        SX64Ntsc,
        PET64Pal,
        PET64Ntsc,
        Ultimax,
        C64GS,
        C64Japanese
    ];

    private static readonly Dictionary<string, C64MachineProfile> ByAlias = All
        .SelectMany(profile => profile.Aliases.Append(profile.Id).Select(alias => (Alias: alias, Profile: profile)))
        .GroupBy(pair => pair.Alias, StringComparer.OrdinalIgnoreCase)
        .ToDictionary(group => group.Key, group => group.First().Profile, StringComparer.OrdinalIgnoreCase);

    public static C64MachineProfile Resolve(string selector)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(selector);

        if (ByAlias.TryGetValue(selector, out var profile))
            return profile;

        throw new ArgumentException($"Unknown x64sc C64 model selector '{selector}'.", nameof(selector));
    }

    public static bool TryResolve(string selector, out C64MachineProfile profile)
    {
        profile = Default;

        if (string.IsNullOrWhiteSpace(selector))
        {
            return false;
        }

        if (ByAlias.TryGetValue(selector, out var resolved))
        {
            profile = resolved;
            return true;
        }

        return false;
    }
}

public static class C64ViceRomNames
{
    public const string Basic = "basic-901226-01.bin";
    public const string Character = "chargen-901225-01.bin";
    public const string CharacterJapanese = "chargen-906143-02.bin";
    public const string KernalJapanese = "kernal-906145-02.bin";
    public const string KernalRev1 = "kernal-901227-01.bin";
    public const string KernalRev2 = "kernal-901227-02.bin";
    public const string KernalRev3 = "kernal-901227-03.bin";
    public const string KernalGs = "kernal-390852-01.bin";
    public const string KernalSx64 = "kernal-251104-04.bin";
    public const string Kernal4064 = "kernal-901246-01.bin";
    public const string KernalNone = "kernal-none.bin";
}
