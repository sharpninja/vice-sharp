namespace ViceSharp.Library.ViewModels;

/// <summary>
/// FR-ROMM-BROWSE-001. The Commodore machine the emulator is configured for. The library is always
/// scoped to the active machine (there is no in-library platform picker).
/// </summary>
public enum LibraryMachine
{
    /// <summary>Commodore 64.</summary>
    C64 = 0,

    /// <summary>Commodore 128.</summary>
    C128 = 1,

    /// <summary>Commodore Plus/4.</summary>
    Plus4 = 2,

    /// <summary>Commodore VIC-20.</summary>
    Vic20 = 3,

    /// <summary>Commodore PET.</summary>
    Pet = 4,
}
