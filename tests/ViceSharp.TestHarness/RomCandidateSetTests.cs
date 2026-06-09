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
    /// <summary>
    /// FR-CFG-001, TR-SYSTEM-CORE-001.
    /// Use case: A fresh ROM candidate set starts from the built-in catalog.
    /// Acceptance: Default candidates match the catalog for representative system roles.
    /// </summary>
    [Fact]
    public void Defaults_AreSeededFromCatalog()
    {
        var set = new RomCandidateSet();

        Assert.Equal(ViceRomCatalog.Candidates("C64", "kernal"), set.GetCandidates("C64", "kernal"));
        Assert.Equal(ViceRomCatalog.Candidates("PLUS4", "function-lo"), set.GetCandidates("PLUS4", "function-lo"));
    }

    /// <summary>
    /// FR-CFG-001, TR-SYSTEM-CORE-001.
    /// Use case: Users can add a ROM candidate after catalog defaults.
    /// Acceptance: AddCandidate appends a new filename as the lowest preference.
    /// </summary>
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

    /// <summary>
    /// FR-CFG-001, TR-SYSTEM-CORE-001.
    /// Use case: Users can insert a ROM candidate at a specific preference.
    /// Acceptance: AddCandidate with index 0 makes that candidate first.
    /// </summary>
    [Fact]
    public void AddCandidate_AtIndex_InsertsAtThatPreference()
    {
        var set = new RomCandidateSet();

        set.AddCandidate("C64", "kernal", "preferred.bin", index: 0);

        Assert.Equal("preferred.bin", set.GetCandidates("C64", "kernal").First());
    }

    /// <summary>
    /// FR-CFG-001, TR-SYSTEM-CORE-001.
    /// Use case: Duplicate ROM candidate additions should be idempotent.
    /// Acceptance: AddCandidate returns false and keeps one case-insensitive entry.
    /// </summary>
    [Fact]
    public void AddCandidate_Duplicate_IsIgnored()
    {
        var set = new RomCandidateSet();
        var existing = set.GetCandidates("C64", "kernal").First();

        var added = set.AddCandidate("C64", "kernal", existing);

        Assert.False(added);
        Assert.Equal(1, set.GetCandidates("C64", "kernal").Count(n => string.Equals(n, existing, System.StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// FR-CFG-001, TR-SYSTEM-CORE-001.
    /// Use case: User override text can introduce a new system role.
    /// Acceptance: AddCandidate creates the missing role and stores the candidate.
    /// </summary>
    [Fact]
    public void AddCandidate_ForUnknownSystemRole_CreatesEntry()
    {
        var set = new RomCandidateSet();

        var added = set.AddCandidate("C64", "cartridge", "my-cart.bin");

        Assert.True(added);
        Assert.Equal(new[] { "my-cart.bin" }, set.GetCandidates("C64", "cartridge"));
    }

    /// <summary>
    /// FR-CFG-001, TR-SYSTEM-CORE-001.
    /// Use case: Users can reorder an existing ROM candidate.
    /// Acceptance: MoveCandidate places the selected candidate at the requested index.
    /// </summary>
    [Fact]
    public void MoveCandidate_ReordersPreference()
    {
        var set = new RomCandidateSet();
        var last = set.GetCandidates("C64", "kernal").Last();

        var moved = set.MoveCandidate("C64", "kernal", last, newIndex: 0);

        Assert.True(moved);
        Assert.Equal(last, set.GetCandidates("C64", "kernal").First());
    }

    /// <summary>
    /// FR-CFG-001, TR-SYSTEM-CORE-001.
    /// Use case: Users can replace a role's candidate order with custom text.
    /// Acceptance: SetOrder stores the ordered unique candidate list.
    /// </summary>
    [Fact]
    public void SetOrder_ReplacesWithUserOrderAndDedupes()
    {
        var set = new RomCandidateSet();

        set.SetOrder("C64", "kernal", new[] { "b.bin", "a.bin", "b.bin" });

        Assert.Equal(new[] { "b.bin", "a.bin" }, set.GetCandidates("C64", "kernal"));
    }

    /// <summary>
    /// FR-CFG-001, TR-SYSTEM-CORE-001.
    /// Use case: Text overrides should add and reorder candidates across systems.
    /// Acceptance: ApplyOverrides updates C64 and VIC20 candidate order as specified.
    /// </summary>
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

    /// <summary>
    /// FR-CFG-001, TR-SYSTEM-CORE-001.
    /// Use case: Users can discard custom ordering for one role.
    /// Acceptance: ResetRole restores the catalog default candidates.
    /// </summary>
    [Fact]
    public void ResetRole_RevertsToCatalogDefault()
    {
        var set = new RomCandidateSet();
        var original = ViceRomCatalog.Candidates("C64", "kernal");

        set.SetOrder("C64", "kernal", new[] { "only.bin" });
        set.ResetRole("C64", "kernal");

        Assert.Equal(original, set.GetCandidates("C64", "kernal"));
    }

    /// <summary>
    /// FR-CFG-001, TR-SYSTEM-CORE-001.
    /// Use case: Rendered override text should preserve candidate order.
    /// Acceptance: RenderOverrides output round-trips through ApplyOverrides.
    /// </summary>
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
