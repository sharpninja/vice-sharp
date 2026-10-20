using System.Text.Json;
using static ViceSharp.NativeClockProbe.Tests.ObservationLineFixtures;

namespace ViceSharp.NativeClockProbe.Tests;

/// <summary>
/// PLAN-OBSERVER-001 S4c: validates whole VICE_OBS traces (the stderr of a gated observer run) with the real
/// step 1 decoder. Every VICE_OBS line must decode; the only tolerated error is Missing:Data on a PHI2 idle
/// record, which is the emitter's fail-closed report for a cycle without a CPU access (a stolen cycle or VICE's
/// own non-cycle reset reads) and is counted, never hidden. Half-cycle indices must be consecutive from 0
/// alternating PHI1, PHI2, and PHI1 must always be idle. With VICESHARP_OBS_TRACE_DIR set, every *.stderr
/// file there is validated and a summary JSON is written beside it; without it, an embedded sample keeps the
/// case executable (never skipped). FR-VICE-001 AC4, TR-VICE-ORACLE-001 AC3/AC5, TEST-VICE-001 AC4.
/// </summary>
public sealed class ObservationTraceValidationTests
{
    public sealed record TraceSummary(string File, long ObsLines, long Records, long Phi1, long Phi2, long IdlePhi2MissingData,
        long OtherErrors, string? FirstOtherError, long SequenceFaults, string? FirstSequenceFault, long Stolen, long MaxHalfCycle);

    internal static TraceSummary Validate(string file, IEnumerable<string> rawLines)
    {
        var decoder = new ObservationLineDecoder();
        long obs = 0, records = 0, phi1 = 0, phi2 = 0, idleMissing = 0, other = 0, seqFaults = 0, stolen = 0, maxHalf = -1;
        string? firstOther = null, firstSeq = null;
        long expectedHalf = 0;
        foreach (var raw in rawLines)
        {
            if (!raw.StartsWith("VICE_OBS,", StringComparison.Ordinal)) continue;
            obs++;
            var line = raw.TrimEnd('\r') + "\n";
            var decode = decoder.Decode(line);
            if (decode.Record is { } r)
            {
                records++;
                if (r.Phase == "PHI1") phi1++; else phi2++;
                if (r.StolenCycle) stolen++;
                if (r.HalfCycle > maxHalf) maxHalf = r.HalfCycle;
                var expectedPhase = expectedHalf % 2 == 0 ? "PHI1" : "PHI2";
                if (r.HalfCycle != expectedHalf || r.Phase != expectedPhase || (r.Phase == "PHI1" && r.Access != "idle"))
                {
                    seqFaults++;
                    firstSeq ??= raw;
                }
                expectedHalf = r.HalfCycle + 1;
            }
            else
            {
                var columns = line.TrimEnd('\n').Split(',');
                var idlePhi2 = decode.Error == "Missing:Data" && columns.Length == 12 && columns[3] == "PHI2" && columns[6] == "idle";
                if (idlePhi2)
                {
                    idleMissing++;
                    if (columns[10] == "1") stolen++;
                    if (long.TryParse(columns[2], out var half))
                    {
                        if (half != expectedHalf || expectedHalf % 2 != 1) { seqFaults++; firstSeq ??= raw; }
                        expectedHalf = half + 1;
                        if (half > maxHalf) maxHalf = half;
                    }
                }
                else
                {
                    other++;
                    firstOther ??= decode.Error + " | " + raw;
                }
            }
        }
        return new(file, obs, records, phi1, phi2, idleMissing, other, firstOther, seqFaults, firstSeq, stolen, maxHalf);
    }

    private const string Sample =
        "VICE_BUS,repatch,514,1\nVICE_OBS,1,0,PHI1,0,1,idle,0,17,0,0,20\nVICE_OBS,1,1,PHI2,1,1,read,65532,226,0,0,20\nVICE_GATE,20\n" +
        "VICE_OBS,1,2,PHI1,0,0,idle,0,18,0,1,21\nVICE_OBS,1,3,PHI2,1,0,idle,0,,0,1,21\nVICE_GATE,21\n" +
        "VICE_OBS,1,4,PHI1,0,1,idle,0,19,2,0,22\nVICE_OBS,1,5,PHI2,1,1,write,53280,7,2,0,22\nVICE_GATE,22\n";

    [Fact]
    public void Embedded_sample_validates_with_one_tolerated_idle_missing_data()
    {
        var s = Validate("sample", Sample.Split('\n'));
        Assert.Equal(6, s.ObsLines);
        Assert.Equal(5, s.Records);
        Assert.Equal(1, s.IdlePhi2MissingData);
        Assert.Equal(0, s.OtherErrors);
        Assert.Equal(0, s.SequenceFaults);
        Assert.Equal(2, s.Stolen);
        Assert.Equal(5, s.MaxHalfCycle);
    }

    [Fact]
    public void Sample_faults_are_detected()
    {
        var malformed = Validate("m", Sample.Replace("read,65532", "fetch,65532").Split('\n'));
        Assert.Equal(1, malformed.OtherErrors);
        Assert.StartsWith("Malformed:Access", malformed.FirstOtherError);
        // A skipped index faults the line that skipped and the line after it (which no longer follows the skipped one).
        var skipped = Validate("s", Sample.Replace("VICE_OBS,1,4,", "VICE_OBS,1,6,").Split('\n'));
        Assert.Equal(2, skipped.SequenceFaults);
        var phi1Read = Validate("p", Sample.Replace("PHI1,0,1,idle,0,17", "PHI1,0,1,read,0,17").Split('\n'));
        Assert.Equal(1, phi1Read.SequenceFaults);
        var missingClock = Validate("c", Sample.Replace("write,53280,7,2,0,22", "write,53280,7,2,0,").Split('\n'));
        Assert.Equal(1, missingClock.OtherErrors);
        Assert.StartsWith("Missing:Clock", missingClock.FirstOtherError);
    }

    public static IEnumerable<object[]> TraceFiles
    {
        get
        {
            var dir = Environment.GetEnvironmentVariable("VICESHARP_OBS_TRACE_DIR");
            if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
            {
                var files = Directory.GetFiles(dir, "*.stderr").OrderBy(f => f, StringComparer.Ordinal).ToArray();
                if (files.Length > 0) return files.Select(f => new object[] { f });
            }
            var sample = Path.Combine(AppContext.BaseDirectory, "obs-trace-sample.stderr");
            File.WriteAllText(sample, Sample);
            return [[sample]];
        }
    }

    /// <summary>Real traces: no malformed or framing fault, no missing field other than idle PHI2 data, no sequence fault; counts retained.</summary>
    [Theory]
    [MemberData(nameof(TraceFiles))]
    public void Trace_file_validates_and_its_summary_is_retained(string file)
    {
        var summary = Validate(file, File.ReadLines(file));
        File.WriteAllText(file + ".obs-validation.json", JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true }));
        Assert.True(summary.ObsLines > 0, "no VICE_OBS lines in " + file);
        Assert.True(summary.OtherErrors == 0, $"{file}: {summary.OtherErrors} decode errors, first: {summary.FirstOtherError}");
        Assert.True(summary.SequenceFaults == 0, $"{file}: {summary.SequenceFaults} sequence faults, first: {summary.FirstSequenceFault}");
        // Every cycle has one PHI1 record and one PHI2 line, the latter either decoded or the tolerated idle-missing form.
        Assert.Equal(summary.Phi1, summary.Phi2 + summary.IdlePhi2MissingData);
    }
}
