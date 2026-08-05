namespace ViceSharp.Library.ViewModels;

/// <summary>
/// FR-ROMM-LAUNCH-001. The nature of an acquired media file, independent of which drive/slot it
/// attaches to.
/// </summary>
public enum MediaKind
{
    /// <summary>A disk image (.d64/.g64/.d71/.d81).</summary>
    Disk = 0,

    /// <summary>A tape image (.tap/.t64).</summary>
    Tape = 1,

    /// <summary>A cartridge image (.crt/.bin/.rom).</summary>
    Cartridge = 2,

    /// <summary>A raw program (.prg): loadable by tooling but not attachable to a media slot.</summary>
    Program = 3,
}
