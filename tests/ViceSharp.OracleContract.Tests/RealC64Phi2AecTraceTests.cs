using Xunit.Sdk;

namespace ViceSharp.OracleContract.Tests;

/// <summary>
/// REAL-C64-PHI2-AEC-001 (PLAN-NATIVERESTORE-001 D5, F10; PLAN-VICE-IMMUTABLE-001 deferred real gate): trace-level
/// reference evidence from the oracle side over retained x64sc VICE_OBS traces (the committed resume windows of the
/// D5 receipt and any committed equivalence head sample). Asserts that every CPU read, write and dummy access occurs
/// on PHI2 with AEC high, and that the recorded AEC equals the approved A2 derivation (OBSERVATION-RECORD-SLICE.md:
/// low on every PHI1; low on PHI2 once BA has been low for three consecutive cycles including the current one)
/// wherever three cycles of BA history exist in the trace. A negative control trace proves the assertion rejects a
/// PHI1 access, an AEC-low access and a derivation mismatch. TR-CYCLE-001 AC4 itself concerns the managed core and
/// stays pending with PLAN-VICEORACLE-001 task 5 until Phase E; this test does not claim it.
/// </summary>
public sealed class RealC64Phi2AecTraceTests
{
    /// <summary>One VICE_OBS record: VICE_OBS,1,halfCycle,phase,aec,ba,access,address,data,mask,stolen,clock.</summary>
    public sealed record Obs(long HalfCycle, string Phase, bool Aec, bool Ba, string Access, int Address, string Data, int Mask, bool Stolen, ulong Clock);

    internal static Obs Parse(string line)
    {
        var c = line.Split(',');
        if (c.Length != 12 || c[0] != "VICE_OBS" || c[1] != "1") throw new FormatException("not a step 1 record: " + line);
        return new Obs(long.Parse(c[2], System.Globalization.CultureInfo.InvariantCulture), c[3], c[4] == "1", c[5] == "1", c[6],
            int.Parse(c[7], System.Globalization.CultureInfo.InvariantCulture), c[8], int.Parse(c[9], System.Globalization.CultureInfo.InvariantCulture), c[10] == "1",
            ulong.Parse(c[11], System.Globalization.CultureInfo.InvariantCulture));
    }

    internal static Obs[] ReadTrace(string path) =>
        File.ReadLines(path).Where(l => l.StartsWith("VICE_OBS,1,", StringComparison.Ordinal)).Select(Parse).ToArray();

    /// <summary>The assertion under test: every CPU access on PHI2 with AEC high, and AEC equal to the A2 derivation where derivable.</summary>
    internal static (int Accesses, int Derived) AssertPhi2AecHigh(Obs[] records)
    {
        Assert.NotEmpty(records);
        var accesses = 0;
        var derived = 0;
        var baLowRun = 0;   // consecutive cycles with BA low, including the current one, counted on PHI2
        for (var i = 0; i < records.Length; i++)
        {
            var r = records[i];
            Assert.True(r.Phase is "PHI1" or "PHI2", $"half-cycle {r.HalfCycle}: phase {r.Phase}");
            if (r.Access is "read" or "write" or "dummy")
            {
                accesses++;
                Assert.True(r.Phase == "PHI2", $"half-cycle {r.HalfCycle} (clock {r.Clock}): CPU {r.Access} on {r.Phase}");
                Assert.True(r.Aec, $"half-cycle {r.HalfCycle} (clock {r.Clock}): CPU {r.Access} with AEC low");
            }
            if (r.Phase == "PHI1")
            {
                Assert.False(r.Aec, $"half-cycle {r.HalfCycle} (clock {r.Clock}): AEC high on PHI1 (A2: the VIC-II owns PHI1)");
                continue;
            }
            baLowRun = r.Ba ? 0 : baLowRun + 1;
            if (i >= 5)   // three cycles of BA history (this PHI2 and the two before it) exist inside the trace
            {
                derived++;
                var expectedAec = baLowRun < 3;
                Assert.True(r.Aec == expectedAec, $"half-cycle {r.HalfCycle} (clock {r.Clock}): AEC {(r.Aec ? 1 : 0)} but A2 derives {(expectedAec ? 1 : 0)} (BA low for {baLowRun} cycles)");
            }
        }
        return (accesses, derived);
    }

    internal static string RepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "ViceSharp.slnx"))) return current.FullName;
            current = current.Parent;
        }
        throw new InvalidOperationException("ViceSharp.slnx not found above " + AppContext.BaseDirectory);
    }

    /// <summary>Committed x64sc traces: the D5 resume windows and head samples under docs/receipts/native-vice.</summary>
    public static IEnumerable<object[]> CommittedTraces()
    {
        var root = Path.Combine(RepoRoot(), "docs", "receipts", "native-vice");
        if (!Directory.Exists(root)) yield break;
        foreach (var path in Directory.EnumerateFiles(root, "*.txt", SearchOption.AllDirectories)
                     .Where(p => Path.GetFileName(p).Contains("x64sc", StringComparison.Ordinal) || Path.GetFileName(p).Contains("v12-resume", StringComparison.Ordinal) || Path.GetFileName(p).Contains("v14-roundtrip", StringComparison.Ordinal))
                     .Where(p => Path.GetFileName(p).Contains("window", StringComparison.Ordinal) || Path.GetFileName(p).Contains("obs", StringComparison.Ordinal))
                     .OrderBy(p => p, StringComparer.Ordinal))
            yield return [Path.GetRelativePath(root, path)];
    }

    /// <summary>
    /// REAL-C64-PHI2-AEC-001; PLAN-NATIVERESTORE-001 D5 (reference evidence).
    /// Use case: a retained x64sc observer trace is checked record by record.
    /// Acceptance: at least one CPU access is present; every CPU access is on PHI2 with AEC high; AEC equals the A2
    /// derivation at every PHI2 with three cycles of BA history; AEC is low on every PHI1.
    /// </summary>
    [Theory]
    [MemberData(nameof(CommittedTraces))]
    public void Every_cpu_access_in_the_retained_trace_is_on_phi2_with_aec_high(string relativePath)
    {
        var path = Path.Combine(RepoRoot(), "docs", "receipts", "native-vice", relativePath);
        var records = ReadTrace(path);
        var (accesses, derived) = AssertPhi2AecHigh(records);
        Assert.True(accesses > 0, "no CPU access in " + relativePath);
        Assert.True(derived > 0 || records.Length < 6, "no derivable PHI2 in " + relativePath);
    }

    /// <summary>Coverage guard: the committed trace set is not empty (absence fails, never skips).</summary>
    [Fact]
    public void At_least_one_retained_trace_is_committed() => Assert.NotEmpty(CommittedTraces());

    private static Obs[] PositiveControl() =>
    [
        new(0, "PHI1", false, true, "idle", 0, "17", 0, false, 100),
        new(1, "PHI2", true, true, "read", 0xFFFC, "226", 0, false, 100),
        new(2, "PHI1", false, true, "idle", 0, "17", 0, false, 101),
        new(3, "PHI2", true, false, "write", 0x0100, "127", 0, false, 101),
        new(4, "PHI1", false, false, "idle", 0, "17", 0, false, 102),
        new(5, "PHI2", true, false, "dummy", 0x01FD, "0", 0, false, 102),
        new(6, "PHI1", false, false, "idle", 0, "17", 0, false, 103),
        new(7, "PHI2", false, false, "idle", 0, "", 0, true, 103),   // BA low for three cycles: AEC low, CPU held off the bus
        new(8, "PHI1", false, true, "idle", 0, "17", 0, false, 104),
        new(9, "PHI2", true, true, "read", 0xC000, "169", 2, false, 104),
    ];

    /// <summary>
    /// REAL-C64-PHI2-AEC-001 negative control; PLAN-NATIVERESTORE-001 D5.
    /// Use case: synthetic traces with one fault each are handed to the same assertion.
    /// Acceptance: the positive control passes; a CPU access on PHI1, a CPU access with AEC low, an AEC-high PHI1 and
    /// an AEC value that contradicts the A2 derivation are each rejected.
    /// </summary>
    [Fact]
    public void Negative_control_traces_are_rejected()
    {
        var good = PositiveControl();
        var (accesses, derived) = AssertPhi2AecHigh(good);
        Assert.Equal(4, accesses);
        Assert.Equal(3, derived);
        void Reject(Func<Obs[], Obs[]> mutate) => Assert.ThrowsAny<XunitException>(() => AssertPhi2AecHigh(mutate([.. good])));
        Reject(t => { t[0] = t[0] with { Access = "read", Address = 0xFFFC }; return t; });        // CPU access on PHI1
        Reject(t => { t[1] = t[1] with { Aec = false }; return t; });                              // CPU read with AEC low
        Reject(t => { t[2] = t[2] with { Aec = true }; return t; });                               // AEC high on PHI1
        Reject(t => { t[7] = t[7] with { Aec = true }; return t; });                               // A2: BA low three cycles, AEC must be low
        Reject(t => { t[9] = t[9] with { Aec = false, Access = "idle", Data = "" }; return t; });  // A2: BA high, AEC must be high
        Reject(_ => []);                                                                           // empty trace
    }
}
