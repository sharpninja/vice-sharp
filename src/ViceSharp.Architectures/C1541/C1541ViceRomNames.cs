namespace ViceSharp.Architectures.C1541;

/// <summary>
/// Canonical 1541 (and family) drive ROM filenames as bundled by upstream
/// VICE under native/vice/vice/data/DRIVES/. Each ROM is 16384 bytes and is
/// mapped at $C000-$FFFF inside the drive 6502's address space.
/// </summary>
public static class C1541ViceRomNames
{
    /// <summary>Standard 1541 DOS (16384 bytes).</summary>
    public const string Dos1541 = "dos1541-325302-01+901229-05.bin";

    /// <summary>1541-II DOS (16384 bytes).</summary>
    public const string Dos1541Ii = "dos1541ii-251968-03.bin";

    /// <summary>1541-C / 1540 DOS (16384 bytes).</summary>
    public const string Dos1540 = "dos1540-325302+3-01.bin";

    /// <summary>Expected size of every 1541-family DOS ROM image.</summary>
    public const int Dos1541RomSize = 0x4000;

    /// <summary>Architecture key passed to IRomProvider for drive ROMs.</summary>
    public const string ArchitectureKey = "DRIVES";
}
