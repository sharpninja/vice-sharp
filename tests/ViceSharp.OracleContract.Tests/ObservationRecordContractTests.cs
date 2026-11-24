using NSubstitute;
using static ViceSharp.OracleContract.Tests.ContractAssertions;

namespace ViceSharp.OracleContract.Tests;

/// <summary>
/// Phase C step 1 observation record, as received from an external wrapper before any comparison.
/// Every field is nullable so a missing field is representable and is never defaulted or inferred.
/// FR-VICE-001 AC4; TR-VICE-ORACLE-001 AC3/AC5; TEST-VICE-001 AC4. See OBSERVATION-RECORD-SLICE.md.
/// </summary>
/// <param name="HalfCycle">Zero-based half-cycle index since the observed reset.</param>
/// <param name="Phase">Clock phase of the half-cycle: PHI1 or PHI2.</param>
/// <param name="Aec">Address enable control line level (true = high).</param>
/// <param name="Ba">Bus available line level (true = high).</param>
/// <param name="Access">Bus access kind: read, write, idle or dummy.</param>
/// <param name="Address">16-bit bus address.</param>
/// <param name="Data">8-bit data bus value; required for every access kind (fail closed, never inferred).</param>
/// <param name="InterruptMask">VICE enum cpu_int pending mask (interrupt.h: IK_NMI 1 to IK_IRQPEND 64), 0 to 0x7F.</param>
/// <param name="StolenCycle">True when the CPU is held off the bus in this half-cycle.</param>
/// <param name="Clock">VICE CLOCK (types.h: uint64_t) at this half-cycle.</param>
public sealed record ObservationWire(long? HalfCycle, string? Phase, bool? Aec, bool? Ba, string? Access,
    int? Address, int? Data, int? InterruptMask, bool? StolenCycle, ulong? Clock);

/// <summary>Literal valid records and the single-fault variants derived from them; expectations are fixture data.</summary>
public static class ObservationRecordFixtures
{
    /// <summary>The ten fields named by HANDOFF.lockstep-plan.md Phase C step 1, in record order.</summary>
    public static readonly string[] Fields =
        ["HalfCycle", "Phase", "Aec", "Ba", "Access", "Address", "Data", "InterruptMask", "StolenCycle", "Clock"];

    /// <summary>Valid records covering both phases, every access kind, BA/AEC low, interrupts and the clock range.</summary>
    public static ObservationWire Valid(string name) => name switch
    {
        "phi2-cpu-read" => new(41, "PHI2", true, true, "read", 0xFFFC, 0xE2, 0, false, 20),
        "phi2-cpu-write" => new(57, "PHI2", true, true, "write", 0x0100, 0x7F, 0, false, 28),
        "phi1-idle" => new(40, "PHI1", false, true, "idle", 0x3FFF, 0xFF, 0, false, 20),
        "phi2-dummy-read" => new(61, "PHI2", true, true, "dummy", 0x01FD, 0x00, 0, false, 30),
        "ba-low-stolen" => new(1001, "PHI2", false, false, "idle", 0x0400, 0x20, 0, true, 500),
        "irq-pending" => new(2049, "PHI2", true, true, "read", 0xC000, 0xA9, 0x02, false, 1024),
        "nmi-and-irq" => new(2051, "PHI2", true, true, "read", 0xC001, 0x00, 0x03, false, 1025),
        "all-pending-bits" => new(9, "PHI2", true, true, "read", 0x0000, 0x2F, 0x7F, false, 4),
        "zero-half-cycle" => new(0, "PHI1", false, true, "idle", 0x0000, 0x00, 0, false, 0),
        "maximum-clock" => new(long.MaxValue, "PHI2", true, true, "read", 0xFFFF, 0xFF, 0, false, ulong.MaxValue),
        _ => throw new ArgumentOutOfRangeException(nameof(name))
    };

    public static IEnumerable<object[]> ValidCases => new[]
    {
        "phi2-cpu-read", "phi2-cpu-write", "phi1-idle", "phi2-dummy-read", "ba-low-stolen",
        "irq-pending", "nmi-and-irq", "all-pending-bits", "zero-half-cycle", "maximum-clock"
    }.Select(n => new object[] { n });

    /// <summary>One missing-field record per field, from phi2-cpu-read.</summary>
    public static IEnumerable<object[]> MissingCases => Fields.Select(f => new object[] { f });

    /// <summary>Out-of-domain values, one fault per case. Bool and CLOCK fields have no malformed value beyond missing.</summary>
    public static IEnumerable<object[]> MalformedCases => new (string Field, string Label)[]
    {
        ("HalfCycle", "negative"), ("Phase", "phi3"), ("Phase", "lowercase"), ("Phase", "empty"),
        ("Access", "fetch"), ("Access", "uppercase"), ("Access", "empty"),
        ("Address", "negative"), ("Address", "above-16-bit"), ("Data", "negative"), ("Data", "above-8-bit"),
        ("InterruptMask", "negative"), ("InterruptMask", "unknown-bit"),
    }.Select(c => new object[] { c.Field, c.Label });

    public static ObservationWire WithMissing(ObservationWire r, string field) => field switch
    {
        "HalfCycle" => r with { HalfCycle = null }, "Phase" => r with { Phase = null },
        "Aec" => r with { Aec = null }, "Ba" => r with { Ba = null }, "Access" => r with { Access = null },
        "Address" => r with { Address = null }, "Data" => r with { Data = null },
        "InterruptMask" => r with { InterruptMask = null }, "StolenCycle" => r with { StolenCycle = null },
        "Clock" => r with { Clock = null },
        _ => throw new ArgumentOutOfRangeException(nameof(field))
    };

    public static ObservationWire WithMalformed(ObservationWire r, string field, string label) => (field, label) switch
    {
        ("HalfCycle", "negative") => r with { HalfCycle = -1 },
        ("Phase", "phi3") => r with { Phase = "PHI3" }, ("Phase", "lowercase") => r with { Phase = "phi2" },
        ("Phase", "empty") => r with { Phase = "" },
        ("Access", "fetch") => r with { Access = "fetch" }, ("Access", "uppercase") => r with { Access = "READ" },
        ("Access", "empty") => r with { Access = "" },
        ("Address", "negative") => r with { Address = -1 }, ("Address", "above-16-bit") => r with { Address = 0x10000 },
        ("Data", "negative") => r with { Data = -1 }, ("Data", "above-8-bit") => r with { Data = 0x100 },
        ("InterruptMask", "negative") => r with { InterruptMask = -1 },
        ("InterruptMask", "unknown-bit") => r with { InterruptMask = 0x80 },
        _ => throw new ArgumentOutOfRangeException(nameof(label))
    };

    /// <summary>Expected rejection: the failing field by name and the record's half-cycle when that field is present.</summary>
    public static Decision Rejection(string kind, string field, ObservationWire r) =>
        new(false, $"{kind}:{field}", field == "HalfCycle" ? null : r.HalfCycle);
}

/// <summary>Mocks only: proves the shared assertions discriminate. No validator implementation exists here.</summary>
public sealed class ObservationRecordContractTests
{
    [Theory]
    [MemberData(nameof(ObservationRecordFixtures.ValidCases), MemberType = typeof(ObservationRecordFixtures))]
    public void Valid_record_is_accepted_at_its_half_cycle(string name)
    {
        var record = ObservationRecordFixtures.Valid(name);
        ProveDecision(record, new Decision(true, null, record.HalfCycle));
    }

    [Theory]
    [MemberData(nameof(ObservationRecordFixtures.MissingCases), MemberType = typeof(ObservationRecordFixtures))]
    public void Missing_field_is_rejected_by_name_and_never_defaulted(string field)
    {
        var record = ObservationRecordFixtures.WithMissing(ObservationRecordFixtures.Valid("phi2-cpu-read"), field);
        ProveDecision(record, ObservationRecordFixtures.Rejection("Missing", field, record));
    }

    [Theory]
    [MemberData(nameof(ObservationRecordFixtures.MalformedCases), MemberType = typeof(ObservationRecordFixtures))]
    public void Malformed_field_is_rejected_by_name(string field, string label)
    {
        var record = ObservationRecordFixtures.WithMalformed(ObservationRecordFixtures.Valid("phi2-cpu-read"), field, label);
        ProveDecision(record, ObservationRecordFixtures.Rejection("Malformed", field, record));
    }

    /// <summary>A validator that accepts a missing-field record fails the shared assertion (no silent defaulting).</summary>
    [Theory]
    [MemberData(nameof(ObservationRecordFixtures.MissingCases), MemberType = typeof(ObservationRecordFixtures))]
    public void Validator_that_accepts_a_missing_field_is_rejected(string field)
    {
        var record = ObservationRecordFixtures.WithMissing(ObservationRecordFixtures.Valid("phi2-cpu-read"), field);
        var lenient = Substitute.For<IContractProbe<ObservationWire>>();
        lenient.Evaluate(record).Returns(new Decision(true, null, record.HalfCycle));
        Rejects(() => AssertProbe(lenient, record, ObservationRecordFixtures.Rejection("Missing", field, record)));
    }

    /// <summary>A rejection that names the wrong field fails, so a real validator cannot report a generic error.</summary>
    [Theory]
    [MemberData(nameof(ObservationRecordFixtures.MissingCases), MemberType = typeof(ObservationRecordFixtures))]
    public void Rejection_naming_another_field_is_rejected(string field)
    {
        var record = ObservationRecordFixtures.WithMissing(ObservationRecordFixtures.Valid("phi2-cpu-read"), field);
        var other = ObservationRecordFixtures.Fields.First(f => f != field);
        var wrong = Substitute.For<IContractProbe<ObservationWire>>();
        wrong.Evaluate(record).Returns(ObservationRecordFixtures.Rejection("Missing", other, record) with { HalfCycle = record.HalfCycle });
        Rejects(() => AssertProbe(wrong, record, ObservationRecordFixtures.Rejection("Missing", field, record)));
    }

    /// <summary>Coverage guard: ten fields, one missing case each; every malformed case names a field with a value domain.</summary>
    [Fact]
    public void Every_field_has_a_missing_control_and_malformed_controls_name_known_fields()
    {
        Assert.Equal(10, ObservationRecordFixtures.Fields.Length);
        Assert.Equal(ObservationRecordFixtures.Fields, ObservationRecordFixtures.MissingCases.Select(c => (string)c[0]));
        var malformedFields = ObservationRecordFixtures.MalformedCases.Select(c => (string)c[0]).Distinct().ToArray();
        Assert.All(malformedFields, f => Assert.Contains(f, ObservationRecordFixtures.Fields));
        Assert.Equal(["HalfCycle", "Phase", "Access", "Address", "Data", "InterruptMask"], malformedFields);
    }
}
