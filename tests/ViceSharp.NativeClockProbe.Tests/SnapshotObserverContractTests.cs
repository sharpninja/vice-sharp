using System.Globalization;
using NSubstitute;
using Xunit.Sdk;

namespace ViceSharp.NativeClockProbe.Tests;

/// <summary>
/// PLAN-NATIVERESTORE-001 D1, decision R11 B (native register observer at the resume boundary) and N02, N04.
/// tools/immutable-vice/snapshot-observer.c wraps maincpu_snapshot_read_module (mainc64cpu.c 934, mainviccpu.c 756;
/// called cross-object from c64-snapshot.c 121 and vic20-snapshot.c 129) and machine_read_snapshot (machine.h 148;
/// called cross-object from autostart.c 887). After the real CPU-module read succeeds it emits one record with the
/// clock and the loaded register file, with the status byte recomposed exactly as mos6510.h 116 to 136 does
/// (MOS6510_REGS_GET_STATUS: p | (n &amp; 0x80) | P_UNUSED | (zero ? P_ZERO : 0), zero being !z); on failure it emits
/// resume-fail with the code and no register record. After the real whole-snapshot read it emits load with the
/// result, and on success the effective drive resources (N05), so a failure after a successful MAINCPU read is
/// observable as resume followed by load,-1. The real functions are called exactly once and their results returned
/// unchanged. TR-VSFLOCKSTEP-RESUME-001 AC1; FR-VICE-001 AC2/AC4.
/// </summary>
public sealed class SnapshotObserverContractTests
{
    /// <summary>Raw register copy as the glue reads it from maincpu_regs: p without the zero and sign bits, n holding the sign in bit 7, z being zero when the zero flag is set.</summary>
    public sealed record Registers(ulong Clock, byte A, byte X, byte Y, byte Sp, ushort Pc, byte P, byte N, byte Z);
    public sealed record Scenario(int ReadResult, Registers Registers, int LoadResult, string Drives);
    public sealed record TraceResult(int ExitCode, string[] Lines);
    public sealed record Fixture(Scenario Scenario, string[] Expected);

    public interface ISnapshotObserverProbe
    {
        TraceResult Run(Scenario scenario);
    }

    /// <summary>The pinned recomposition, written once here as the expected value, never read from the unit under test.</summary>
    public static byte Status(byte p, byte n, byte z) => (byte)(p | (n & 0x80) | 0x20 | (z == 0 ? 0x02 : 0));

    private static string Resume(Registers r) =>
        string.Create(CultureInfo.InvariantCulture, $"VICE_SNAP,resume,{r.Clock},{r.A},{r.X},{r.Y},{r.Sp},{r.Pc},{Status(r.P, r.N, r.Z)}");

    private static readonly Registers Approved = new(109163946, 0x11, 0x22, 0x33, 0xF8, 22377, 0x20, 0x80, 1);
    private const string ApprovedDrives = "1541,0,none,1541,1,none,1541,1,none,1541,1,none";

    public static Fixture GetFixture(string name) => name switch
    {
        // The approved fixture's register file: P $A0 = p 0x20 with the sign set and the zero flag clear (z 1).
        "approved" => new(new(0, Approved, 0, ApprovedDrives),
            [Resume(Approved), "VICE_SNAP,load,0", "VICE_SNAP,drives," + ApprovedDrives, "ret,0,0"]),
        "zero-flag-set" => new(new(0, Approved with { P = 0x00, N = 0x00, Z = 0 }, 0, ApprovedDrives),
            [Resume(Approved with { P = 0x00, N = 0x00, Z = 0 }), "VICE_SNAP,load,0", "VICE_SNAP,drives," + ApprovedDrives, "ret,0,0"]),
        "all-flags" => new(new(0, Approved with { P = 0x7D, N = 0xFF, Z = 0 }, 0, ApprovedDrives),
            [Resume(Approved with { P = 0x7D, N = 0xFF, Z = 0 }), "VICE_SNAP,load,0", "VICE_SNAP,drives," + ApprovedDrives, "ret,0,0"]),
        "wide" => new(new(0, new(ulong.MaxValue, 255, 255, 255, 255, 65535, 0x7D, 0x80, 0), 0, "0,0,none,0,0,none,0,0,none,0,0,none"),
            [Resume(new(ulong.MaxValue, 255, 255, 255, 255, 65535, 0x7D, 0x80, 0)), "VICE_SNAP,load,0", "VICE_SNAP,drives,0,0,none,0,0,none,0,0,none,0,0,none", "ret,0,0"]),
        "cpu-read-fails" => new(new(-1, Approved, -1, ApprovedDrives),
            ["VICE_SNAP,resume-fail,-1", "VICE_SNAP,load,-1", "ret,-1,-1"]),
        "later-module-fails" => new(new(0, Approved, -1, ApprovedDrives),
            [Resume(Approved), "VICE_SNAP,load,-1", "ret,0,-1"]),
        _ => throw new ArgumentOutOfRangeException(nameof(name))
    };

    public static readonly string[] FixtureNames = ["approved", "zero-flag-set", "all-flags", "wide", "cpu-read-fails", "later-module-fails"];

    internal static void AssertProbe(ISnapshotObserverProbe probe, string name)
    {
        var fixture = GetFixture(name);
        var actual = probe.Run(fixture.Scenario);
        Assert.NotNull(actual);
        Assert.Equal(0, actual.ExitCode);
        Assert.Equal(fixture.Expected, actual.Lines);
    }

    [Theory]
    [InlineData(0x20, 0x80, 1, 0xA0)]
    [InlineData(0x00, 0x00, 0, 0x22)]
    [InlineData(0x7D, 0xFF, 0, 0xFF)]
    [InlineData(0x01, 0x7F, 1, 0x21)]
    public void Status_recomposition_matches_the_pinned_macro(byte p, byte n, byte z, byte expected)
    {
        Assert.Equal(expected, Status(p, n, z));
    }

    [Theory]
    [InlineData("approved")]
    [InlineData("zero-flag-set")]
    [InlineData("all-flags")]
    [InlineData("wide")]
    [InlineData("cpu-read-fails")]
    [InlineData("later-module-fails")]
    public void Correct_mock_passes_and_every_wrong_mock_is_rejected(string name)
    {
        var fixture = GetFixture(name);
        var correct = Substitute.For<ISnapshotObserverProbe>();
        correct.Run(Arg.Any<Scenario>()).Returns(new TraceResult(0, fixture.Expected.ToArray()));
        AssertProbe(correct, name);

        void Reject(TraceResult result)
        {
            var wrong = Substitute.For<ISnapshotObserverProbe>();
            wrong.Run(Arg.Any<Scenario>()).Returns(result);
            Assert.ThrowsAny<XunitException>(() => AssertProbe(wrong, name));
        }

        Reject(new(72, fixture.Expected));
        Reject(new(0, null!));
        Reject(null!);
        Reject(new(0, fixture.Expected[..^1]));
        Reject(new(0, [.. fixture.Expected, "VICE_SNAP,load,0"]));
        for (var i = 0; i < fixture.Expected.Length; i++)
        {
            var altered = fixture.Expected.ToArray();
            altered[i] = altered[i] + "0";
            Reject(new(0, altered));
            var reordered = fixture.Expected.ToArray();
            if (i > 0) { (reordered[i], reordered[i - 1]) = (reordered[i - 1], reordered[i]); Reject(new(0, reordered)); }
        }
    }

    [Fact]
    public void A_failed_cpu_read_never_carries_a_register_record_and_a_failed_load_never_carries_drives()
    {
        foreach (var name in FixtureNames)
        {
            var f = GetFixture(name);
            if (f.Scenario.ReadResult != 0) Assert.DoesNotContain(f.Expected, l => l.StartsWith("VICE_SNAP,resume,", StringComparison.Ordinal));
            if (f.Scenario.LoadResult != 0) Assert.DoesNotContain(f.Expected, l => l.StartsWith("VICE_SNAP,drives,", StringComparison.Ordinal));
            Assert.Equal("ret," + f.Scenario.ReadResult.ToString(CultureInfo.InvariantCulture) + "," + f.Scenario.LoadResult.ToString(CultureInfo.InvariantCulture), f.Expected[^1]);
        }
    }
}
