namespace ViceSharp.TestHarness;

using System.Linq;
using ViceSharp.RomFetch;
using Xunit;

/// <summary>
/// BRANDING/ROM-CANDIDATES-001: the built-in <see cref="ViceRomCatalog"/> is a
/// default ordering of candidate ROM filenames per system/role; a user must be
/// able to (a) add their own candidate dumps and (b) reorder preference.
/// <see cref="RomCandidateSet"/> is the mutable, per-instance overlay seeded
/// from the catalog that the resolver consults.
/// </summary>
public sealed class RomCandidateSetTests
{
    [Fact]
    public void Defaults_AreSeededFromCatalog()
    {
        var set = new RomCandidateSet();

        Assert.Equal(ViceRomCatalog.Candidates("C64", "kernal"), set.GetCandidates("C64", "kernal"));
        Assert.Equal(ViceRomCatalog.Candidates("PLUS4", "function-lo"), set.GetCandidates("PLUS4", "function-lo"));
    }

    [Fact]
    public void AddCandidate_AppendsAsLowestPreference()
    {
        var set = new RomCandidateSet();
        var before = set.GetCandidates("C64", "kernal").Count;

        var added = set.AddCandidate("C64", "kernal", "my-patched-kernal.bin");

        Assert.True(added);
        Assert.Equal(before + 1, set.GetCandidates("C64", "kernal").Count);
        Assert.Equal("my-patched-kernal.bin", set.GetCandidates("C64", "kernal").Last());
    }

    [Fact]
    public void AddCandidate_AtIndex_InsertsAtThatPreference()
    {
        var set = new RomCandidateSet();

        set.AddCandidate("C64", "kernal", "preferred.bin", index: 0);

        Assert.Equal("preferred.bin", set.GetCandidates("C64", "kernal").First());
    }

    [Fact]
    public void AddCandidate_Duplicate_IsIgnored()
    {
        var set = new RomCandidateSet();
        var existing = set.GetCandidates("C64", "kernal").First();

        var added = set.AddCandidate("C64", "kernal", existing);

        Assert.False(added);
        Assert.Equal(1, set.GetCandidates("C64", "kernal").Count(n => string.Equals(n, existing, System.StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public void AddCandidate_ForUnknownSystemRole_CreatesEntry()
    {
        var set = new RomCandidateSet();

        var added = set.AddCandidate("C64", "cartridge", "my-cart.bin");

        Assert.True(added);
        Assert.Equal(new[] { "my-cart.bin" }, set.GetCandidates("C64", "cartridge"));
    }

    [Fact]
    public void MoveCandidate_ReordersPreference()
    {
        var set = new RomCandidateSet();
        var last = set.GetCandidates("C64", "kernal").Last();

        var moved = set.MoveCandidate("C64", "kernal", last, newIndex: 0);

        Assert.True(moved);
        Assert.Equal(last, set.GetCandidates("C64", "kernal").First());
    }

    [Fact]
    public void SetOrder_ReplacesWithUserOrderAndDedupes()
    {
        var set = new RomCandidateSet();

        set.SetOrder("C64", "kernal", new[] { "b.bin", "a.bin", "b.bin" });

        Assert.Equal(new[] { "b.bin", "a.bin" }, set.GetCandidates("C64", "kernal"));
    }

    [Fact]
    public void ApplyOverrides_AddsAndReordersFromText()
    {
        var set = new RomCandidateSet();

        set.ApplyOverrides(
            "# user ROM candidate overrides\n" +
            "C64.kernal = custom-kernal.bin, kernal-901227-03.bin\n" +
            "\n" +
            "VIC20.basic = my-vic-basic.bin\n");

        Assert.Equal(new[] { "custom-kernal.bin", "kernal-901227-03.bin" }, set.GetCandidates("C64", "kernal"));
        Assert.Equal(new[] { "my-vic-basic.bin" }, set.GetCandidates("VIC20", "basic"));
    }

    [Fact]
    public void ResetRole_RevertsToCatalogDefault()
    {
        var set = new RomCandidateSet();
        var original = ViceRomCatalog.Candidates("C64", "kernal");

        set.SetOrder("C64", "kernal", new[] { "only.bin" });
        set.ResetRole("C64", "kernal");

        Assert.Equal(original, set.GetCandidates("C64", "kernal"));
    }

    [Fact]
    public void RenderOverrides_RoundTripsThroughApplyOverrides()
    {
        var set = new RomCandidateSet();
        set.AddCandidate("C64", "kernal", "extra.bin", index: 0);
        set.MoveCandidate("PLUS4", "kernal", set.GetCandidates("PLUS4", "kernal").Last(), 0);

        var rendered = set.RenderOverrides();
        var roundTripped = new RomCandidateSet();
        roundTripped.ApplyOverrides(rendered);

        Assert.Equal(set.GetCandidates("C64", "kernal"), roundTripped.GetCandidates("C64", "kernal"));
        Assert.Equal(set.GetCandidates("PLUS4", "kernal"), roundTripped.GetCandidates("PLUS4", "kernal"));
    }
}
