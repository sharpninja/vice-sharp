using ViceSharp.Abstractions;

namespace ViceSharp.Architectures.Vic20;

/// <summary>
/// Board-level policy for a VIC-20 system core.
/// </summary>
/// <remarks>
/// FR-PRF-005, FR-VIC20-002, FR-VIC20-006.
/// </remarks>
public sealed record Vic20SystemCoreDefinition(
    string Id,
    string DisplayName,
    string BoardPolicy,
    string AddressDecoderPolicy,
    string BusPolicy,
    bool KeyboardMatrixConnected,
    bool TapePortConnected,
    bool IecBusConnected,
    DriveModel DefaultDriveModel,
    string DefaultDriveDosRomName,
    bool CartridgeBootExpected = false,
    IReadOnlyDictionary<string, string>? Traits = null) : ISystemCoreDefinition
{
    public string Family => "xvic";

    /// <summary>VIC-20 has no second CIA; always false.</summary>
    public bool Cia2Connected => false;

    IReadOnlyDictionary<string, string> ISystemCoreDefinition.Traits => Traits ?? EmptyTraits;

    private static readonly IReadOnlyDictionary<string, string> EmptyTraits =
        new Dictionary<string, string>();

    public static Vic20SystemCoreDefinition Unexpanded(DriveModel defaultDrive, string dosRomName)
    {
        var traits = new Dictionary<string, string>
        {
            ["board"] = "Unexpanded",
            ["decoder"] = "Unexpanded",
            ["bus"] = "Standard",
            ["tapePort"] = "connected",
            ["iecBus"] = "connected",
            ["defaultDrive"] = defaultDrive.ToString(),
            ["defaultDriveDos"] = dosRomName
        };

        return new Vic20SystemCoreDefinition(
            "xvic:unexpanded",
            "VIC-20 unexpanded",
            "Unexpanded",
            "Unexpanded",
            "Standard",
            KeyboardMatrixConnected: true,
            TapePortConnected: true,
            IecBusConnected: true,
            DefaultDriveModel: defaultDrive,
            DefaultDriveDosRomName: dosRomName,
            CartridgeBootExpected: false,
            Traits: traits);
    }
}
