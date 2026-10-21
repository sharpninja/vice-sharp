namespace ViceSharp.ViceCompatibility.SnapshotFixtures;

/// <summary>The VSF container framing the manifest records (pinned snapshot.c layout).</summary>
public sealed class ContainerFraming
{
    public int MagicBytes { get; set; } = 19;
    public int ContainerVersionBytes { get; set; } = 2;
    public int MachineFieldBytes { get; set; } = 16;
    public int CurrentHeaderBytes { get; set; } = 58;
    public int LegacyHeaderBytes { get; set; } = 37;
    public int ModuleHeaderBytes { get; set; } = 22;
    public string IntegerEncoding { get; set; } = "little-endian";
    public string ModuleLengthRule { get; set; } = "inclusive of the 22-byte module header";
}
