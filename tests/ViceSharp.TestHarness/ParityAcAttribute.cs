namespace ViceSharp.TestHarness;

using Xunit.v3;

/// <summary>
/// Requirement tag for a VICE-parity acceptance criterion
/// (artifacts/vice-parity-requirements/requirements.yaml).
/// </summary>
public enum ParityTag
{
    /// <summary>Managed already matches VICE; the test is a green-now regression lock.</summary>
    Faithful,

    /// <summary>Managed diverges from VICE; the test starts red and is a remediation target.</summary>
    Divergent,
}

/// <summary>
/// PLAN-VICEPARITY-001 P0-8 / TR-PARITY-GATE-001. Binds a test method to
/// exactly one acceptance criterion in requirements.yaml and drives the CI
/// quarantine. Emits traits:
/// Category=Parity always; Ac=&lt;TestId&gt; always (the coverage manifest key);
/// Category=ParityPending only while <see cref="Pending"/> is true, which
/// excludes the not-yet-remediated red test from the blocking Nuke Test
/// filter. Slice exit flips Pending to false, admitting the test to the
/// permanent gate. FAITHFUL criteria are regression locks and may never be
/// quarantined; the constructor enforces that invariant.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class ParityAcAttribute : Attribute, ITraitAttribute
{
    public ParityAcAttribute(string testId, ParityTag tag, bool pending = false)
    {
        if (string.IsNullOrWhiteSpace(testId))
            throw new ArgumentException("Parity AC test id is required.", nameof(testId));
        if (tag == ParityTag.Faithful && pending)
            throw new ArgumentException(
                $"FAITHFUL AC '{testId}' is a green-now regression lock and must never be quarantined (Pending).",
                nameof(pending));

        TestId = testId;
        Tag = tag;
        Pending = pending;
    }

    /// <summary>The requirements.yaml acceptance-criterion test id (e.g. TEST-SID-ENV-07).</summary>
    public string TestId { get; }

    /// <summary>FAITHFUL (regression lock) or DIVERGENT (remediation target).</summary>
    public ParityTag Tag { get; }

    /// <summary>True while the DIVERGENT test is still red and quarantined from the blocking gate.</summary>
    public bool Pending { get; }

    public IReadOnlyCollection<KeyValuePair<string, string>> GetTraits()
    {
        var traits = new List<KeyValuePair<string, string>>
        {
            new("Category", "Parity"),
            new("Ac", TestId),
        };
        if (Pending)
        {
            traits.Add(new("Category", "ParityPending"));
        }

        return traits;
    }
}
