namespace ViceSharp.ViceCompatibility.SnapshotFixtures;

/// <summary>One fixture entry of the schema-1 state-transfer manifest (PLAN-VSFLOCKSTEP-001 lines 230 to 243).</summary>
public sealed class FixtureManifestEntry
{
    public string Path { get; set; } = "";
    public string Sha256 { get; set; } = "";
    public long Length { get; set; }
    public string Machine { get; set; } = "";
    public string Model { get; set; } = "";
    public string Command { get; set; } = "";
    public ulong TriggerClock { get; set; }
    public ulong WriteClock { get; set; }
    public string[] RomHashes { get; set; } = [];
    public string[] MediaHashes { get; set; } = [];
    public string[] Hosts { get; set; } = [];
    public string? ObserverSha256 { get; set; }
}
