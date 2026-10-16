namespace ViceSharp.Protocol;

/// <summary>
/// Shared management state for VIC-20 expansion carts (FE3, Ultimem, Mega-Cart).
/// </summary>
public sealed class ExpansionCartManageState
{
    public static IReadOnlyList<string> CartKinds { get; } =
        ["none", "fe3", "ultimem", "megacart"];

    public static IReadOnlyList<string> Fe3Presets { get; } =
        ["start", "flash", "super-rom", "rom-ram", "ram2", "none", "3k", "8k", "16k", "24k", "full", "super-ram"];

    public static IReadOnlyList<string> UltimemPresets { get; } =
        ["start", "blk5-rom", "ram-all"];

    public static IReadOnlyList<string> MegaCartPresets { get; } =
        ["start", "bank0", "nvram"];

    /// <summary>
    /// Placeholder list when no expansion cart is attached. Must be non-empty so a ComboBox
    /// SelectedItem of <c>start</c> remains in ItemsSource (empty ItemsSource + TwoWay
    /// SelectedItem not in list stack-overflows UWP on Apply/refresh).
    /// </summary>
    public static IReadOnlyList<string> NonePresets { get; } = ["start"];

    public string CartKind { get; set; } = "none";

    public bool WriteBack { get; set; }

    public string ConfigPreset { get; set; } = "start";

    public string ImagePath { get; set; } = "";

    public IReadOnlyList<string> AvailablePresets => CartKind.ToLowerInvariant() switch
    {
        "fe3" => Fe3Presets,
        "ultimem" => UltimemPresets,
        "megacart" => MegaCartPresets,
        _ => NonePresets,
    };
}
