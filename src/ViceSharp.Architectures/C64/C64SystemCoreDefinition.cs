using ViceSharp.Abstractions;

namespace ViceSharp.Architectures.C64;

public enum C64PlaPolicy
{
    Standard,
    Ultimax,
    CartridgeRequired
}

public enum C64BusPolicy
{
    Standard,
    Portable,
    Max,
    GameSystem
}

public sealed record C64SystemCoreDefinition(
    string Id,
    string DisplayName,
    C64BoardModel Board,
    C64PlaPolicy Pla,
    C64BusPolicy Bus,
    bool KeyboardMatrixConnected,
    bool TapePortConnected,
    bool IecBusConnected,
    bool Cia2Connected,
    bool CartridgeBootExpected,
    IReadOnlyDictionary<string, string>? Traits = null) : ISystemCoreDefinition
{
    public string Family => "x64sc";

    public string BoardPolicy => Board.ToString();

    public string AddressDecoderPolicy => Pla.ToString();

    public string BusPolicy => Bus.ToString();

    IReadOnlyDictionary<string, string> ISystemCoreDefinition.Traits => Traits ?? EmptyTraits;

    private static readonly IReadOnlyDictionary<string, string> EmptyTraits =
        new Dictionary<string, string>();
}

public static class C64SystemCoreDefinitions
{
    public static C64SystemCoreDefinition ForProfile(
        C64BoardModel board,
        bool keyboardEnabled,
        bool cartridgeBootExpected)
    {
        var pla = board == C64BoardModel.Ultimax
            ? C64PlaPolicy.Ultimax
            : cartridgeBootExpected ? C64PlaPolicy.CartridgeRequired : C64PlaPolicy.Standard;

        var bus = board switch
        {
            C64BoardModel.SX64 => C64BusPolicy.Portable,
            C64BoardModel.Ultimax => C64BusPolicy.Max,
            C64BoardModel.C64GS => C64BusPolicy.GameSystem,
            _ => C64BusPolicy.Standard
        };
        var tapePortConnected = board is not (C64BoardModel.SX64 or C64BoardModel.C64GS);
        var iecBusConnected = board is not (C64BoardModel.Ultimax or C64BoardModel.C64GS);
        var cia2Connected = board is not C64BoardModel.Ultimax;

        var id = $"x64sc:{board.ToString().ToLowerInvariant()}:{pla.ToString().ToLowerInvariant()}";
        var traits = new Dictionary<string, string>
        {
            ["board"] = board.ToString(),
            ["pla"] = pla.ToString(),
            ["bus"] = bus.ToString(),
            ["tapePort"] = tapePortConnected ? "connected" : "absent",
            ["iecBus"] = iecBusConnected ? "connected" : "absent",
            ["cia2"] = cia2Connected ? "connected" : "absent"
        };

        return new C64SystemCoreDefinition(
            id,
            $"{board} system core",
            board,
            pla,
            bus,
            keyboardEnabled,
            tapePortConnected,
            iecBusConnected,
            cia2Connected,
            cartridgeBootExpected,
            traits);
    }
}
