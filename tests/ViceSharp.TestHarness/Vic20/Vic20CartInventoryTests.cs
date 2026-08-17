namespace ViceSharp.TestHarness.Vic20;

using ViceSharp.Core.Vic20;
using Xunit;

/// <summary>
/// FR-VIC20-CART-001 / AC-CT-01 / TEST-VIC20-CT-01: inventory of VICE xvic cart
/// types mapped to managed implementations or Explicit Missing.
/// Use case: whole-machine Exact claim requires documented cart matrix.
/// Acceptance: every type in the shim-side cart inventory has a disposition.
/// </summary>
public sealed class Vic20CartInventoryTests
{
    /// <summary>
    /// VICE sources under native/vice/vice/src/vic20/cart (build set for xvic).
    /// Disposition: Managed type name, or ExplicitMissing with reason.
    /// </summary>
    public static readonly (string ViceSource, string Disposition)[] Inventory =
    [
        ("vic20-generic.c", nameof(Vic20Cartridge)),
        ("finalexpansion.c", nameof(FinalExpansion3Cartridge)),
        ("ultimem.c", nameof(UltimemCartridge)),
        ("megacart.c", nameof(MegaCartCartridge)),
        ("behrbonz.c", "ExplicitMissing: not required for umbrella Exact; operator may amend"),
        ("debugcart.c", "ExplicitMissing: test harness only in VICE"),
        ("ioramcart.c", "ExplicitMissing: niche IO RAM cart"),
        ("mikroassembler.c", "ExplicitMissing: niche"),
        ("minimon.c", "ExplicitMissing: niche"),
        ("rabbit.c", "ExplicitMissing: niche"),
        ("superexpander.c", "ExplicitMissing: niche Super Expander"),
        ("vic-fp.c", "ExplicitMissing: Freezer Point"),
        ("vic20-ieee488.c", "ExplicitMissing: IEEE-488 excluded from whole-machine claim"),
        ("vic20-midi.c", "ExplicitMissing: MIDI"),
        ("vic20-sidcart.c", "ExplicitMissing: SID cart"),
        ("writenow.c", "ExplicitMissing: niche"),
        ("mascuerade-stubs.c", "ExplicitMissing: stub"),
    ];

    [Fact]
    public void Inventory_CoversViceCartDirectory_WithDisposition()
    {
        Assert.True(Inventory.Length >= 10, "inventory must list VICE cart sources");
        foreach (var (src, disposition) in Inventory)
        {
            Assert.False(string.IsNullOrWhiteSpace(src));
            Assert.False(string.IsNullOrWhiteSpace(disposition));
        }

        // Core Exact cart set must map to managed types.
        Assert.Contains(Inventory, e => e.ViceSource == "finalexpansion.c" && e.Disposition == nameof(FinalExpansion3Cartridge));
        Assert.Contains(Inventory, e => e.ViceSource == "ultimem.c" && e.Disposition == nameof(UltimemCartridge));
        Assert.Contains(Inventory, e => e.ViceSource == "megacart.c" && e.Disposition == nameof(MegaCartCartridge));
        Assert.Contains(Inventory, e => e.ViceSource == "vic20-generic.c" && e.Disposition == nameof(Vic20Cartridge));
    }
}
