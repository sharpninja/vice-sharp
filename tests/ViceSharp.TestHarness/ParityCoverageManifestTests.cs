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
    /// authored [ParityAc] test. Starts at 0 (gate infra lands before the test
    /// suites), rises to 168 when the FAITHFUL locks land, then by each
    /// slice's DIVERGENT count. The final slice pins covered == 466.
    /// MUST never be lowered.
    /// </summary>
    private const int ExpectedMinCovered = 0;

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
