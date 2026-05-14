namespace ViceSharp.Abstractions;

/// <summary>
/// Describes a concrete machine model variant supported by an emulator family.
/// </summary>
public interface IMachineProfile
{
    /// <summary>Stable profile identifier used by host APIs and tests.</summary>
    string Id { get; }

    /// <summary>Human-readable model name.</summary>
    string DisplayName { get; }

    /// <summary>Accepted selector aliases for this profile.</summary>
    IReadOnlyList<string> Aliases { get; }

    /// <summary>Emulator family this model belongs to, for example x64sc.</summary>
    string Family { get; }

    /// <summary>Nominal CPU clock in Hz.</summary>
    long NominalClockHz { get; }

    /// <summary>Video timing family.</summary>
    VideoStandard VideoStandard { get; }

    /// <summary>CPU cycles per raster line.</summary>
    int CyclesPerLine { get; }

    /// <summary>Total raster lines per frame.</summary>
    int RasterLines { get; }

    /// <summary>Refresh rate computed from clock and frame geometry.</summary>
    double RefreshRateHz { get; }

    /// <summary>Expected VIC-II model identifier.</summary>
    string VicIIModel { get; }

    /// <summary>Expected SID model identifier.</summary>
    string SidModel { get; }

    /// <summary>Board or machine wiring variant.</summary>
    string BoardModel { get; }

    /// <summary>System core policy that defines board buses, glue logic, and chip interconnect.</summary>
    ISystemCoreDefinition SystemCore { get; }

    /// <summary>Required ROM set key.</summary>
    string RomSet { get; }

    /// <summary>Required BASIC ROM resource name.</summary>
    string BasicRomName { get; }

    /// <summary>Required KERNAL ROM resource name.</summary>
    string KernalRomName { get; }

    /// <summary>Required character generator ROM resource name.</summary>
    string CharacterRomName { get; }

    /// <summary>True when the model exposes the normal C64 keyboard matrix.</summary>
    bool KeyboardEnabled { get; }

    /// <summary>True when cartridge insertion is part of the model's normal boot path.</summary>
    bool CartridgeBootExpected { get; }
}

/// <summary>
/// Architecture descriptor with a concrete model profile.
/// </summary>
public interface IProfiledArchitectureDescriptor : IArchitectureDescriptor
{
    /// <summary>Concrete machine profile for this descriptor.</summary>
    IMachineProfile MachineProfile { get; }
}
