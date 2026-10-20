using System.Globalization;
using static ViceSharp.NativeClockProbe.Tests.ObservationLineFixtures;

namespace ViceSharp.NativeClockProbe.Tests;

/// <summary>
/// The real decoder for the step 1 VICE_OBS line, bound to the identical fixtures and assertions the mocks
/// proved (ObservationLineDecoderTests); nothing is weakened. One line in, exactly one of Record or Error out:
/// framing faults in order terminator, prefix, version, column count; then per field an empty column is
/// Missing:&lt;Field&gt; and an out-of-domain token is Malformed:&lt;Field&gt;. Integers are unsigned decimal digits
/// only (no sign, no space, no prefix), checked against each field's range. PLAN-OBSERVER-001 S4c.
/// </summary>
internal sealed class ObservationLineDecoder : IObservationLineProbe
{
    public LineDecode Decode(string line)
    {
        if (line.Length == 0 || line[^1] != '\n' || line.IndexOf('\n') != line.Length - 1 || line.Contains('\r'))
            return new(null, "Framing:Terminator");
        var columns = line[..^1].Split(',');
        if (columns[0] != "VICE_OBS") return new(null, "Framing:Prefix");
        if (columns.Length < 2 || columns[1] != "1") return new(null, "Framing:Version");
        if (columns.Length != 12) return new(null, "Framing:ColumnCount");

        string? error = null;
        var halfCycle = ParseUnsigned(columns[2], "HalfCycle", (ulong)long.MaxValue, ref error);
        var phase = ParseToken(columns[3], "Phase", ["PHI1", "PHI2"], ref error);
        var aec = ParseUnsigned(columns[4], "Aec", 1, ref error);
        var ba = ParseUnsigned(columns[5], "Ba", 1, ref error);
        var access = ParseToken(columns[6], "Access", ["read", "write", "idle", "dummy"], ref error);
        var address = ParseUnsigned(columns[7], "Address", 0xFFFF, ref error);
        var data = ParseUnsigned(columns[8], "Data", 0xFF, ref error);
        var mask = ParseUnsigned(columns[9], "InterruptMask", 0x7F, ref error);
        var stolen = ParseUnsigned(columns[10], "StolenCycle", 1, ref error);
        var clock = ParseUnsigned(columns[11], "Clock", ulong.MaxValue, ref error);
        if (error is not null) return new(null, error);
        return new(new ObservationRecord((long)halfCycle, phase!, aec == 1, ba == 1, access!, (ushort)address, (byte)data, (byte)mask, stolen == 1, clock), null);
    }

    /// <summary>First fault wins, in column order; later columns are still parsed only to keep the code uniform.</summary>
    private static ulong ParseUnsigned(string text, string field, ulong max, ref string? error)
    {
        if (error is not null) return 0;
        if (text.Length == 0) { error = "Missing:" + field; return 0; }
        if (!text.All(char.IsAsciiDigit) || !ulong.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var value) || value > max)
        {
            error = "Malformed:" + field;
            return 0;
        }
        return value;
    }

    private static string? ParseToken(string text, string field, string[] allowed, ref string? error)
    {
        if (error is not null) return null;
        if (text.Length == 0) { error = "Missing:" + field; return null; }
        if (!allowed.Contains(text, StringComparer.Ordinal)) { error = "Malformed:" + field; return null; }
        return text;
    }
}

/// <summary>The identical fixture catalogs and assertions against the real decoder.</summary>
public sealed class ObservationLineDecoderTests
{
    private static readonly ObservationLineDecoder Decoder = new();

    [Theory]
    [MemberData(nameof(ObservationLineFixtures.ValidCases), MemberType = typeof(ObservationLineFixtures))]
    public void Valid_line_decodes_to_the_exact_record(string name) => AssertDecode(Decoder, GetValid(name));

    [Theory]
    [MemberData(nameof(ObservationLineFixtures.MissingCases), MemberType = typeof(ObservationLineFixtures))]
    public void Empty_column_is_a_missing_field_by_name(string field) => AssertDecode(Decoder, GetMissing(field));

    [Theory]
    [MemberData(nameof(ObservationLineFixtures.MalformedCases), MemberType = typeof(ObservationLineFixtures))]
    public void Out_of_domain_column_is_malformed_by_name(string field, string value) => AssertDecode(Decoder, GetMalformed(field, value));

    [Theory]
    [MemberData(nameof(ObservationLineFixtures.FramingCases), MemberType = typeof(ObservationLineFixtures))]
    public void Framing_fault_is_rejected(string name) => AssertDecode(Decoder, GetFraming(name));
}
