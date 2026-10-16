using ViceSharp.Abstractions;
using ViceSharp.Architectures.C1541;

namespace ViceSharp.Architectures.Vic20;

/// <summary>
/// VIC chip revision used by a VIC-20 profile (maps to <see cref="IMachineProfile.VicIIModel"/> string).
/// </summary>
public enum Vic20VicModel
{
    /// <summary>MOS 6561 PAL.</summary>
    Mos6561,

    /// <summary>MOS 6560 NTSC.</summary>
    Mos6560
}

/// <summary>
/// Concrete VIC-20 machine model profile (PAL/NTSC, clocks, ROMs, default 1540 drive).
/// </summary>
/// <remarks>
/// FR-PRF-005, FR-VIC20-001, FR-VIC20-006.
/// Clocks match VICE <c>vic20.h</c>: PAL 1108405/71/312, NTSC 1022727/65/261.
/// </remarks>
public sealed record Vic20MachineProfile(
    string Id,
    string DisplayName,
    IReadOnlyList<string> Aliases,
    long NominalClockHz,
    VideoStandard VideoStandard,
    int CyclesPerLine,
    int RasterLines,
    Vic20VicModel Vic,
    string RomSet,
    string BasicRomName = Vic20ViceRomNames.Basic,
    string KernalRomName = Vic20ViceRomNames.KernalPal,
    string CharacterRomName = Vic20ViceRomNames.Character,
    bool KeyboardEnabled = true,
    bool CartridgeBootExpected = false,
    DriveModel DefaultDriveModel = DriveModel.C1540,
    string DefaultDriveDosRomName = C1541ViceRomNames.Dos1540,
    Vic20Expansion Expansion = Vic20Expansion.Unexpanded,
    Vic20SystemCoreDefinition? CoreDefinition = null) : IMachineProfile
{
    public string Family => "xvic";

    public double RefreshRateHz => NominalClockHz / (double)(CyclesPerLine * RasterLines);

    /// <summary>Exposes VIC-I model through the shared profile string used by C64 as VicIIModel.</summary>
    public string VicIIModel => Vic.ToString();

    /// <summary>VIC-20 has no SID; empty marker for shared profile surface.</summary>
    public string SidModel => "None";

    public string BoardModel => Expansion == Vic20Expansion.Unexpanded
        ? "Unexpanded"
        : $"Exp{(int)Expansion}K";

    public ISystemCoreDefinition SystemCore { get; } =
        CoreDefinition ?? Vic20SystemCoreDefinition.Unexpanded(DefaultDriveModel, DefaultDriveDosRomName);
}

/// <summary>
/// Built-in VIC-20 profile catalog and alias resolver.
/// </summary>
public static class Vic20MachineProfiles
{
    public static Vic20MachineProfile Default => Vic20Pal;

    public static Vic20MachineProfile Vic20Pal { get; } = new(
        "vic20",
        "Commodore VIC-20 PAL",
        ["vic20", "vic20pal", "pal", "xvic", "commodore-vic20"],
        1_108_405,
        VideoStandard.Pal,
        71,
        312,
        Vic20VicModel.Mos6561,
        Vic20ViceRomNames.ArchitectureKey,
        KernalRomName: Vic20ViceRomNames.KernalPal);

    public static Vic20MachineProfile Vic20Ntsc { get; } = new(
        "vic20ntsc",
        "Commodore VIC-20 NTSC",
        ["vic20ntsc", "ntsc", "xvic-ntsc"],
        1_022_727,
        VideoStandard.Ntsc,
        65,
        261,
        Vic20VicModel.Mos6560,
        Vic20ViceRomNames.ArchitectureKey,
        KernalRomName: Vic20ViceRomNames.KernalNtsc);

    public static IReadOnlyList<Vic20MachineProfile> All { get; } =
    [
        Vic20Pal,
        Vic20Ntsc
    ];

    private static readonly Dictionary<string, Vic20MachineProfile> ByAlias = All
        .SelectMany(profile => profile.Aliases.Append(profile.Id).Select(alias => (Alias: alias, Profile: profile)))
        .GroupBy(pair => pair.Alias, StringComparer.OrdinalIgnoreCase)
        .ToDictionary(group => group.Key, group => group.First().Profile, StringComparer.OrdinalIgnoreCase);

    public static Vic20MachineProfile Resolve(string selector)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(selector);

        if (ByAlias.TryGetValue(selector, out var profile))
            return profile;

        throw new ArgumentException($"Unknown xvic VIC-20 model selector '{selector}'.", nameof(selector));
    }

    public static bool TryResolve(string selector, out Vic20MachineProfile profile)
    {
        profile = Default;

        if (string.IsNullOrWhiteSpace(selector))
            return false;

        if (ByAlias.TryGetValue(selector, out var resolved))
        {
            profile = resolved;
            return true;
        }

        return false;
    }
}
