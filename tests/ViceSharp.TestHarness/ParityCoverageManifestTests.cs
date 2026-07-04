namespace ViceSharp.TestHarness;

using System.Reflection;
using Xunit;
using YamlDotNet.Serialization;

/// <summary>
/// PLAN-VICEPARITY-001 P0-8 / TR-PARITY-GATE-001. Coverage manifest for the
/// VICE-parity requirements (artifacts/vice-parity-requirements/
/// requirements.yaml: 38 FRs, 466 ACs, each with a unique test id). Proves
/// that every authored [ParityAc] test binds to a real AC with the right tag,
/// that FAITHFUL regression locks are never quarantined, and that coverage
/// only ever grows (ratchet). Dropping, duplicating, or mislabeling an AC is
/// a test failure, never a silent gap.
/// </summary>
public sealed class ParityCoverageManifestTests
{
    /// <summary>
    /// Coverage ratchet: the minimum number of distinct ACs that must carry an
    /// authored [ParityAc] test. 167 = every FAITHFUL lock (168 minus
    /// TEST-SID-FILTER-6581-19, re-tagged DIVERGENT during authoring: the
    /// managed 0x70 mode mask is the same defect as FR-SID-MIXVOL AC-01).
    /// +17 = slice V1 (PLAN-VICEPARITY-001): all DIVERGENT ACs of FR-VIC-CYCLE
    /// (8), FR-VIC-FETCH (5) and FR-VIC-MATRIX-ADDR (4) authored in
    /// VicCycleDivergentParityTests.
    /// +9 = slice S1 (PLAN-VICEPARITY-001): the DIVERGENT FR-SID-ENV criteria
    /// (AC-07/AC-08/AC-50) and the clock-dispatch DIVERGENT FR-SID-CLOCK
    /// criteria (AC-01/AC-02/AC-03/AC-06/AC-07/AC-09) authored in
    /// SidEnvDivergentParityTests.
    /// +6 = slice S2 (PLAN-VICEPARITY-001): the OSC3-side DIVERGENT
    /// FR-SID-OSC3ENV3 criteria (AC-01..AC-06) authored in
    /// SidWaveOsc3DivergentParityTests. FR-SID-OSC3ENV3 AC-07 and the
    /// DIVERGENT FR-SID-WAVE-ACC criteria (AC-02/AC-05/AC-06) were stopped
    /// in S2: their VICE-exact fixes conflict with the FAITHFUL locks
    /// TEST-SID-CLOCK-11, TEST-SID-VOICE-07 and TEST-SID-VOICE-09 (see the
    /// S2 slice report).
    /// +15 = slice V2 (PLAN-VICEPARITY-001): all DIVERGENT ACs of
    /// FR-VIC-RASTER-IRQ (AC-02/AC-09/AC-11), FR-VIC-REGISTERS
    /// (AC-12/AC-13/AC-14/AC-15) and FR-VIC-LIGHTPEN
    /// (AC-01/AC-05..AC-10/AC-14) authored in
    /// VicIrqRegsLightpenDivergentParityTests. FR-VIC-LIGHTPEN AC-01 and
    /// FR-VIC-REGISTERS AC-15 were stopped in V2: their VICE-exact fixes
    /// conflict with the FAITHFUL locks TEST-VIC-LIGHTPEN-03/04 and
    /// TEST-VIC-REGISTERS-10/11 (see the V2 slice report).
    /// +14 = slice V5 (PLAN-VICEPARITY-001): all DIVERGENT ACs of
    /// FR-VIC-SPRITE-DMA (AC-01..AC-14) authored in
    /// VicSpriteDmaDivergentParityTests. PART 1 V5 also flipped
    /// TEST-VIC-LIGHTPEN-01 and TEST-VIC-REGISTERS-15 from pending to
    /// active (2 additional covered, already counted from V2 authoring).
    /// +13 = slice S3 (PLAN-VICEPARITY-001): the S2-stopped FR-SID-WAVE-ACC
    /// (AC-02/AC-05/AC-06) and FR-SID-OSC3ENV3 AC-07 (unblocked by the S3
    /// relock of TEST-SID-VOICE-07/09 and TEST-SID-CLOCK-11), plus every
    /// DIVERGENT criterion of FR-SID-WAVE-SAWTRI (AC-01..AC-04) and
    /// FR-SID-WAVE-PULSE (AC-01/AC-02/AC-03/AC-04/AC-06), authored in
    /// SidWaveCoreDivergentParityTests.
    /// +10 = slice S4/S5 combined (PLAN-VICEPARITY-001): every DIVERGENT AC of
    /// FR-SID-WAVE-SYNC (AC-01/AC-04), FR-SID-WAVE-RING (AC-01/AC-02/AC-03)
    /// and FR-SID-WAVE-TESTBIT (AC-03/AC-04/AC-05/AC-06/AC-08) authored in
    /// SidSyncRingTestbitDivergentParityTests. RING and TESTBIT-03 were already
    /// correct in S3; SYNC and TESTBIT-04/05/06/08 remediated in this slice.
    /// +11 = slice V3 (PLAN-VICEPARITY-001): all DIVERGENT ACs of
    /// FR-VIC-DRAW-GFX (AC-01/02/03/04/06/14/15) and FR-VIC-XSCROLL
    /// (AC-01/02/03/04) authored in VicGraphicsPipelineDivergentParityTests.
    /// These prove the new per-cycle PixelSequencer (VicIi/PixelSequencer.cs,
    /// a port of draw_graphics/draw_graphics8 from viciisc/vicii-draw-cycle.c)
    /// reproduces the gbuf shift register, xscroll_pipe latch, gbuf_mc_flop
    /// pair-holding and pipe0-&gt;pipe1 double-buffering that the retired
    /// geometric renderer could not. All 11 admitted green.
    /// +15 = slice V4 (PLAN-VICEPARITY-001): all DIVERGENT ACs of
    /// FR-VIC-DRAW-COLOR (AC-01..AC-10) and FR-VIC-DISPLAYMODE
    /// (AC-03/04/05/06/09) authored in VicColorDisplayModeDivergentParityTests.
    /// Proves the draw_colors8 Cregs pipeline (vicii-draw-cycle.c:627-663):
    /// identity-mapped cregs init, symbolic-code resolution through Cregs (not
    /// live _regs), one-pixel 6569 ring delay, 8565 no-delay, 8565 grey-dot,
    /// update_cregs transfer, DbufOffset per-cycle tracking, MonitorColorStore
    /// immediate path, and mid-line colour change at pixel granularity. The
    /// five FR-VIC-DISPLAYMODE ACs admit the V3 per-cycle ECM/BMM/MCM
    /// mode-edge logic to the parity manifest. All 15 admitted green.
    /// +15 = slice V6 (PLAN-VICEPARITY-001): DIVERGENT ACs of
    /// FR-VIC-SPRITE-RENDER / FR-VIC-SPRITE-PRIORITY / FR-VIC-SPRITE-COLLISION
    /// authored in VicSpriteRenderDivergentParityTests, proving the per-pixel
    /// sprite draw (sbuf shift + xpos trigger + expansion/mc flops), the
    /// winner-first behind-priority test over the graphics pri_buffer, and
    /// per-pixel collision latching, all consuming V3's PixelSequencer and
    /// V5's mc/mcbase/exp_flop DMA. All 15 admitted green.
    /// Rises by each slice's DIVERGENT count; the final slice pins
    /// covered == 466. MUST never be lowered. Current authored distinct
    /// [ParityAc] ids = 364 (335 prior + 9 V7 FR-VIC-BORDER AC-01..07/11/12
    /// + 20 S8 FR-SID-VOICE/MIXVOL AC).
    /// </summary>
    private const int ExpectedMinCovered = 364;

    private const int ExpectedFrCount = 38;
    private const int ExpectedAcCount = 466;

    private sealed class RequirementsDoc
    {
        [YamlMember(Alias = "requirements")]
        public List<RequirementNode> Requirements { get; set; } = [];
    }

    private sealed class RequirementNode
    {
        [YamlMember(Alias = "id")]
        public string Id { get; set; } = string.Empty;

        [YamlMember(Alias = "chip")]
        public string Chip { get; set; } = string.Empty;

        [YamlMember(Alias = "acceptanceCriteria")]
        public List<AcceptanceNode> AcceptanceCriteria { get; set; } = [];
    }

    private sealed class AcceptanceNode
    {
        [YamlMember(Alias = "id")]
        public string Id { get; set; } = string.Empty;

        [YamlMember(Alias = "tag")]
        public string Tag { get; set; } = string.Empty;

        [YamlMember(Alias = "test")]
        public string Test { get; set; } = string.Empty;

        [YamlMember(Alias = "state")]
        public string State { get; set; } = string.Empty;

        [YamlMember(Alias = "finding")]
        public string Finding { get; set; } = string.Empty;
    }

    private static string ResolveRequirementsPath()
    {
        var attribute = typeof(ParityCoverageManifestTests).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == "ParityRequirementsPath");
        Assert.False(string.IsNullOrWhiteSpace(attribute?.Value), "ParityRequirementsPath assembly metadata missing");
        var path = Path.GetFullPath(attribute!.Value!);
        Assert.True(File.Exists(path), $"requirements artifact not found: {path}");
        return path;
    }

    private static RequirementsDoc LoadRequirements()
    {
        var deserializer = new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .Build();
        using var reader = new StreamReader(ResolveRequirementsPath());
        var doc = deserializer.Deserialize<RequirementsDoc>(reader);
        Assert.NotNull(doc);
        return doc;
    }

    private static IReadOnlyList<(MethodInfo Method, ParityAcAttribute Ac)> ReflectParityTests()
    {
        return typeof(ParityCoverageManifestTests).Assembly.GetTypes()
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            .Select(m => (Method: m, Ac: m.GetCustomAttribute<ParityAcAttribute>()))
            .Where(x => x.Ac is not null)
            .Select(x => (x.Method, x.Ac!))
            .ToList();
    }

    /// <summary>
    /// FR: FR-SID-ENV (representative), TR: TR-PARITY-GATE-001, TEST: TEST-PARITY-MANIFEST-01.
    /// Use case: the manifest is only trustworthy if the requirements artifact
    /// itself is intact and its test ids are globally unique.
    /// Acceptance: requirements.yaml parses to exactly 38 requirements and 466
    /// acceptance criteria; every AC carries a non-empty unique test id and a
    /// FAITHFUL or DIVERGENT tag consistent with its green-now/red-now state.
    /// </summary>
    [Fact]
    public void RequirementsArtifact_Carries466UniqueTestIds()
    {
        var doc = LoadRequirements();

        Assert.Equal(ExpectedFrCount, doc.Requirements.Count);
        var acs = doc.Requirements.SelectMany(r => r.AcceptanceCriteria).ToList();
        Assert.Equal(ExpectedAcCount, acs.Count);

        Assert.All(acs, ac => Assert.False(string.IsNullOrWhiteSpace(ac.Test), $"AC {ac.Id} missing test id"));
        Assert.Equal(acs.Count, acs.Select(ac => ac.Test).Distinct(StringComparer.Ordinal).Count());

        Assert.All(acs, ac => Assert.True(
            ac.Tag is "FAITHFUL" or "DIVERGENT",
            $"AC {ac.Test} carries unknown tag '{ac.Tag}'"));
        Assert.All(acs, ac => Assert.True(
            (ac.Tag == "FAITHFUL" && ac.State == "green-now") || (ac.Tag == "DIVERGENT" && ac.State == "red-now"),
            $"AC {ac.Test} tag '{ac.Tag}' inconsistent with state '{ac.State}'"));

        // P0-6 crosswalk: the finding id links each AC to the
        // PLAN-VICEPARITY-001 audit (artifacts/vice-parity-requirements/
        // crosswalk.md). Every DIVERGENT AC exists because something was
        // found, so it must cite a 2-3 digit audit finding number or the
        // literal "new" (authoring-time discoveries in the artifact's
        // newFindings header). FAITHFUL ACs may cite one or be blank
        // (regression locks with no divergence finding).
        Assert.All(acs, ac =>
        {
            var finding = ac.Finding ?? string.Empty;
            bool cited = finding == "new"
                || (finding.Length is 2 or 3 && finding.All(char.IsAsciiDigit));
            Assert.True(
                cited || (ac.Tag == "FAITHFUL" && finding.Length == 0),
                $"AC {ac.Test} ({ac.Tag}) finding id '{finding}' is not a PLAN-VICEPARITY-001 finding, 'new', or a blank FAITHFUL lock");
        });
    }

    /// <summary>
    /// FR: FR-SID-ENV (representative), TR: TR-PARITY-GATE-001, TEST: TEST-PARITY-MANIFEST-02.
    /// Use case: every authored parity test must bind to a real AC so coverage
    /// counts mean what they claim.
    /// Acceptance: no two [ParityAc] methods share a test id; every authored
    /// test id exists in requirements.yaml; the authored tag matches the
    /// artifact tag; no FAITHFUL test is Pending (also enforced at attribute
    /// construction).
    /// </summary>
    [Fact]
    public void AuthoredParityTests_BindToArtifactExactly()
    {
        var doc = LoadRequirements();
        var byTestId = doc.Requirements
            .SelectMany(r => r.AcceptanceCriteria)
            .ToDictionary(ac => ac.Test, StringComparer.Ordinal);

        var authored = ReflectParityTests();

        var duplicates = authored.GroupBy(x => x.Ac.TestId, StringComparer.Ordinal)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        Assert.True(duplicates.Count == 0, $"duplicate ParityAc test ids: {string.Join(", ", duplicates)}");

        foreach (var (method, ac) in authored)
        {
            Assert.True(
                byTestId.TryGetValue(ac.TestId, out var artifactAc),
                $"{method.DeclaringType?.Name}.{method.Name} cites unknown AC test id '{ac.TestId}'");
            var expectedTag = artifactAc!.Tag == "FAITHFUL" ? ParityTag.Faithful : ParityTag.Divergent;
            Assert.True(
                ac.Tag == expectedTag,
                $"{ac.TestId}: authored tag {ac.Tag} but artifact says {artifactAc.Tag}");
            Assert.False(
                ac.Tag == ParityTag.Faithful && ac.Pending,
                $"{ac.TestId}: FAITHFUL lock is quarantined (Pending)");
        }
    }

    /// <summary>
    /// FR: FR-SID-ENV (representative), TR: TR-PARITY-GATE-001, TEST: TEST-PARITY-MANIFEST-03.
    /// Use case: parity coverage must only grow; a slice that drops authored
    /// ACs would silently shrink the gate.
    /// Acceptance: the number of distinct authored [ParityAc] test ids is at
    /// least the recorded ratchet baseline (raised as slices land; strict 466
    /// at project completion).
    /// </summary>
    [Fact]
    public void AuthoredCoverage_MeetsRatchet()
    {
        var covered = ReflectParityTests()
            .Select(x => x.Ac.TestId)
            .Distinct(StringComparer.Ordinal)
            .Count();
        Assert.True(
            covered >= ExpectedMinCovered,
            $"parity coverage regressed: {covered} authored ACs, ratchet requires >= {ExpectedMinCovered}");
    }

    /// <summary>
    /// FR: FR-SID-ENV (representative), TR: TR-PARITY-GATE-001, TEST: TEST-PARITY-MANIFEST-04.
    /// Use case: the CI quarantine depends on the exact traits the attribute
    /// emits; the blocking Test filter excludes Category=ParityPending while
    /// ParityTest selects all Category=Parity tests.
    /// Acceptance: Pending emits Category=Parity + Ac + Category=ParityPending;
    /// non-Pending emits no ParityPending; constructing a FAITHFUL Pending
    /// attribute throws (locks can never be quarantined).
    /// </summary>
    [Fact]
    public void ParityAcAttribute_EmitsQuarantineTraitsExactly()
    {
        var pending = new ParityAcAttribute("TEST-SID-ENV-07", ParityTag.Divergent, pending: true).GetTraits();
        Assert.Contains(new KeyValuePair<string, string>("Category", "Parity"), pending);
        Assert.Contains(new KeyValuePair<string, string>("Ac", "TEST-SID-ENV-07"), pending);
        Assert.Contains(new KeyValuePair<string, string>("Category", "ParityPending"), pending);

        var admitted = new ParityAcAttribute("TEST-SID-ENV-07", ParityTag.Divergent, pending: false).GetTraits();
        Assert.Contains(new KeyValuePair<string, string>("Category", "Parity"), admitted);
        Assert.DoesNotContain(new KeyValuePair<string, string>("Category", "ParityPending"), admitted);

        Assert.Throws<ArgumentException>(() => new ParityAcAttribute("TEST-SID-ENV-01", ParityTag.Faithful, pending: true));
    }
}
