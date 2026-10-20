using NSubstitute;
using Xunit.Sdk;

namespace ViceSharp.NativeClockProbe.Tests;

/// <summary>
/// Phase C step 1 wire contract for the observation line an external wrapper writes, one line per half-cycle:
/// <c>VICE_OBS,1,&lt;halfCycle&gt;,&lt;PHI1|PHI2&gt;,&lt;aec 0|1&gt;,&lt;ba 0|1&gt;,&lt;read|write|idle|dummy&gt;,&lt;address 0..65535&gt;,&lt;data 0..255&gt;,&lt;interruptMask 0..127&gt;,&lt;stolen 0|1&gt;,&lt;clock uint64&gt;\n</c>
/// Decimal integers, positional like the existing VICE_GATE line. An empty column is a missing field; it is never
/// defaulted or inferred. FR-VICE-001 AC4; TR-VICE-ORACLE-001 AC3/AC5; TEST-VICE-001 AC4. See OBSERVATION-LINE-SLICE.md.
/// </summary>
public static class ObservationLineFixtures
{
    /// <summary>Decoded record. InterruptMask is VICE enum cpu_int (interrupt.h), Clock is VICE CLOCK (uint64_t).</summary>
    public sealed record ObservationRecord(long HalfCycle, string Phase, bool Aec, bool Ba, string Access,
        ushort Address, byte Data, byte InterruptMask, bool StolenCycle, ulong Clock);

    /// <summary>Decoder result: exactly one of Record or Error is set.</summary>
    public sealed record LineDecode(ObservationRecord? Record, string? Error);

    /// <summary>Shared seam for the mock now and the real C-emitted line decoder later.</summary>
    public interface IObservationLineProbe { LineDecode Decode(string line); }

    /// <summary>Field names in column order after the VICE_OBS prefix and version.</summary>
    public static readonly string[] Fields =
        ["HalfCycle", "Phase", "Aec", "Ba", "Access", "Address", "Data", "InterruptMask", "StolenCycle", "Clock"];

    public sealed record LineFixture(string Line, LineDecode Expected);

    const string Base = "VICE_OBS,1,41,PHI2,1,1,read,65532,226,0,0,20\n";
    static readonly ObservationRecord BaseRecord = new(41, "PHI2", true, true, "read", 0xFFFC, 0xE2, 0, false, 20);

    public static IEnumerable<object[]> ValidCases => new[]
    {
        "base", "write", "phi1-idle-aec-low", "dummy", "ba-low-stolen", "nmi-and-irq", "all-pending-bits",
        "zeros", "maximum-values"
    }.Select(n => new object[] { n });

    public static LineFixture GetValid(string name) => name switch
    {
        "base" => new(Base, new(BaseRecord, null)),
        "write" => new("VICE_OBS,1,57,PHI2,1,1,write,256,127,0,0,28\n",
            new(new(57, "PHI2", true, true, "write", 0x0100, 0x7F, 0, false, 28), null)),
        "phi1-idle-aec-low" => new("VICE_OBS,1,40,PHI1,0,1,idle,16383,255,0,0,20\n",
            new(new(40, "PHI1", false, true, "idle", 0x3FFF, 0xFF, 0, false, 20), null)),
        "dummy" => new("VICE_OBS,1,61,PHI2,1,1,dummy,509,0,0,0,30\n",
            new(new(61, "PHI2", true, true, "dummy", 0x01FD, 0x00, 0, false, 30), null)),
        "ba-low-stolen" => new("VICE_OBS,1,1001,PHI2,0,0,idle,1024,32,0,1,500\n",
            new(new(1001, "PHI2", false, false, "idle", 0x0400, 0x20, 0, true, 500), null)),
        "nmi-and-irq" => new("VICE_OBS,1,2051,PHI2,1,1,read,49153,0,3,0,1025\n",
            new(new(2051, "PHI2", true, true, "read", 0xC001, 0x00, 3, false, 1025), null)),
        "all-pending-bits" => new("VICE_OBS,1,9,PHI2,1,1,read,0,47,127,0,4\n",
            new(new(9, "PHI2", true, true, "read", 0x0000, 0x2F, 0x7F, false, 4), null)),
        "zeros" => new("VICE_OBS,1,0,PHI1,0,0,idle,0,0,0,0,0\n",
            new(new(0, "PHI1", false, false, "idle", 0, 0, 0, false, 0), null)),
        "maximum-values" => new("VICE_OBS,1,9223372036854775807,PHI2,1,1,read,65535,255,127,1,18446744073709551615\n",
            new(new(long.MaxValue, "PHI2", true, true, "read", 0xFFFF, 0xFF, 0x7F, true, ulong.MaxValue), null)),
        _ => throw new ArgumentOutOfRangeException(nameof(name))
    };

    /// <summary>Replace one column of the base line (0 = prefix, 1 = version, 2.. = fields).</summary>
    static string WithColumn(int column, string value)
    {
        var cols = Base.TrimEnd('\n').Split(',');
        cols[column] = value;
        return string.Join(',', cols) + "\n";
    }

    /// <summary>One missing-field line per field: its column is empty.</summary>
    public static IEnumerable<object[]> MissingCases => Fields.Select(f => new object[] { f });

    public static LineFixture GetMissing(string field) =>
        new(WithColumn(Array.IndexOf(Fields, field) + 2, ""), new(null, $"Missing:{field}"));

    /// <summary>Malformed values per field, one fault per case.</summary>
    public static IEnumerable<object[]> MalformedCases => MalformedValues.Select(m => new object[] { m.Field, m.Value });

    static readonly (string Field, string Value)[] MalformedValues =
    [
        ("HalfCycle", "-1"), ("HalfCycle", "x"), ("HalfCycle", "9223372036854775808"), ("HalfCycle", "+5"),
        ("Phase", "PHI3"), ("Phase", "phi2"), ("Aec", "2"), ("Aec", "true"), ("Ba", "-1"), ("Ba", "yes"),
        ("Access", "fetch"), ("Access", "READ"), ("Address", "65536"), ("Address", "-1"), ("Address", "0xFFFC"),
        ("Data", "256"), ("Data", "-1"), ("InterruptMask", "128"), ("InterruptMask", "-1"),
        ("StolenCycle", "2"), ("StolenCycle", "no"), ("Clock", "-1"), ("Clock", "18446744073709551616"),
        ("Clock", " 20")
    ];

    public static LineFixture GetMalformed(string field, string value) =>
        new(WithColumn(Array.IndexOf(Fields, field) + 2, value), new(null, $"Malformed:{field}"));

    /// <summary>Framing faults: prefix, version, column count and line termination.</summary>
    public static IEnumerable<object[]> FramingCases => new[]
    {
        "wrong-prefix", "gate-line", "wrong-version", "missing-version", "too-few-columns", "too-many-columns",
        "no-newline", "crlf", "empty", "two-lines"
    }.Select(n => new object[] { n });

    public static LineFixture GetFraming(string name) => name switch
    {
        "wrong-prefix" => new(WithColumn(0, "VICE_OBX"), new(null, "Framing:Prefix")),
        "gate-line" => new("VICE_GATE,20\n", new(null, "Framing:Prefix")),
        "wrong-version" => new(WithColumn(1, "2"), new(null, "Framing:Version")),
        "missing-version" => new(WithColumn(1, ""), new(null, "Framing:Version")),
        "too-few-columns" => new("VICE_OBS,1,41,PHI2,1,1,read,65532,226,0,0\n", new(null, "Framing:ColumnCount")),
        "too-many-columns" => new("VICE_OBS,1,41,PHI2,1,1,read,65532,226,0,0,20,7\n", new(null, "Framing:ColumnCount")),
        "no-newline" => new(Base.TrimEnd('\n'), new(null, "Framing:Terminator")),
        "crlf" => new(Base.TrimEnd('\n') + "\r\n", new(null, "Framing:Terminator")),
        "empty" => new("", new(null, "Framing:Terminator")),
        "two-lines" => new(Base + Base, new(null, "Framing:Terminator")),
        _ => throw new ArgumentOutOfRangeException(nameof(name))
    };

    /// <summary>Reused without weakening when the same fixtures bind to the real decoder.</summary>
    public static void AssertDecode(IObservationLineProbe probe, LineFixture fixture)
    {
        var actual = probe.Decode(fixture.Line);
        Assert.NotNull(actual);
        Assert.Equal(fixture.Expected.Error, actual.Error);
        Assert.Equal(fixture.Expected.Record, actual.Record);
    }

    /// <summary>Correct mock passes; every single-field corruption of the result is rejected.</summary>
    public static int Prove(LineFixture fixture)
    {
        var correct = Substitute.For<IObservationLineProbe>();
        correct.Decode(fixture.Line).Returns(fixture.Expected);
        AssertDecode(correct, fixture);
        correct.Received(1).Decode(fixture.Line);

        var rejected = 0;
        void Reject(LineDecode? wrong)
        {
            var probe = Substitute.For<IObservationLineProbe>();
            probe.Decode(fixture.Line).Returns(wrong!);
            Assert.ThrowsAny<XunitException>(() => AssertDecode(probe, fixture));
            rejected++;
        }

        Reject(null);
        var e = fixture.Expected;
        if (e.Record is { } r)
        {
            Reject(e with { Record = null });
            Reject(e with { Error = "Missing:HalfCycle" });
            Reject(e with { Record = r with { HalfCycle = r.HalfCycle == 0 ? 1 : r.HalfCycle - 1 } });
            Reject(e with { Record = r with { Phase = r.Phase == "PHI1" ? "PHI2" : "PHI1" } });
            Reject(e with { Record = r with { Aec = !r.Aec } });
            Reject(e with { Record = r with { Ba = !r.Ba } });
            Reject(e with { Record = r with { Access = r.Access == "read" ? "write" : "read" } });
            Reject(e with { Record = r with { Address = (ushort)(r.Address ^ 1) } });
            Reject(e with { Record = r with { Data = (byte)(r.Data ^ 1) } });
            Reject(e with { Record = r with { InterruptMask = (byte)(r.InterruptMask ^ 1) } });
            Reject(e with { Record = r with { StolenCycle = !r.StolenCycle } });
            Reject(e with { Record = r with { Clock = r.Clock ^ 1 } });
        }
        else
        {
            Reject(e with { Error = null });
            Reject(e with { Error = e.Error + "!" });
            Reject(e with { Record = BaseRecord });
        }
        return rejected;
    }
}

/// <summary>Mocks only: proves the assertions discriminate. No decoder or C emitter is exercised here.</summary>
public sealed class ObservationLineContractTests
{
    [Theory]
    [MemberData(nameof(ObservationLineFixtures.ValidCases), MemberType = typeof(ObservationLineFixtures))]
    public void Valid_line_decodes_to_the_exact_record(string name) =>
        // null result, record removed, error set, and one corruption per each of the ten fields.
        Assert.Equal(13, ObservationLineFixtures.Prove(ObservationLineFixtures.GetValid(name)));

    [Theory]
    [MemberData(nameof(ObservationLineFixtures.MissingCases), MemberType = typeof(ObservationLineFixtures))]
    public void Empty_column_is_a_missing_field_by_name(string field) =>
        Assert.Equal(4, ObservationLineFixtures.Prove(ObservationLineFixtures.GetMissing(field)));

    [Theory]
    [MemberData(nameof(ObservationLineFixtures.MalformedCases), MemberType = typeof(ObservationLineFixtures))]
    public void Out_of_domain_column_is_malformed_by_name(string field, string value) =>
        Assert.Equal(4, ObservationLineFixtures.Prove(ObservationLineFixtures.GetMalformed(field, value)));

    [Theory]
    [MemberData(nameof(ObservationLineFixtures.FramingCases), MemberType = typeof(ObservationLineFixtures))]
    public void Framing_fault_is_rejected(string name) =>
        Assert.Equal(4, ObservationLineFixtures.Prove(ObservationLineFixtures.GetFraming(name)));

    /// <summary>Coverage guard: ten fields each with a missing case and at least one malformed case.</summary>
    [Fact]
    public void Every_field_has_missing_and_malformed_controls()
    {
        Assert.Equal(10, ObservationLineFixtures.Fields.Length);
        var malformed = ObservationLineFixtures.MalformedCases.Select(c => (string)c[0]).Distinct().OrderBy(f => f).ToArray();
        Assert.Equal(ObservationLineFixtures.Fields.OrderBy(f => f).ToArray(), malformed);
        Assert.Equal(ObservationLineFixtures.Fields, ObservationLineFixtures.MissingCases.Select(c => (string)c[0]));
    }

    /// <summary>The base line has exactly 12 columns: prefix, version and the ten fields.</summary>
    [Fact]
    public void Base_line_has_prefix_version_and_ten_fields() =>
        Assert.Equal(12, ObservationLineFixtures.GetValid("base").Line.TrimEnd('\n').Split(',').Length);
}
