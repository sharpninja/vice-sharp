using System.Globalization;
using NSubstitute;
using Xunit.Sdk;

namespace ViceSharp.NativeClockProbe.Tests;

/// <summary>
/// PLAN-NATIVERESTORE-001 D1, decision R12 (literal checkpoint cycle) with N15, N21, N22, N28.
/// tools/immutable-vice/snapshot-checkpoint.c is armed only by VICESHARP_SNAPSHOT_AT=&lt;trigger&gt;:&lt;path&gt;. When the gate
/// clock first reaches the trigger it registers one trap through the pinned public API
/// interrupt_maincpu_trigger_trap (interrupt.h 305); the pinned core runs the trap at the next instruction boundary
/// whose copied pending mask includes it (6510dtvcore.c 354 to 412, 1758 to 1763), which can be later than the
/// trigger (a dispatched interrupt, a stolen-cycle run). The callback calls machine_write_snapshot (machine.h 145)
/// and emits checkpoint,&lt;trigger&gt;,&lt;write clock&gt; with the clock measured inside the callback, never equated to the
/// trigger; every nonzero writer result, including the +1 of vic20-snapshot.c 99 to 102, is checkpoint-fail with
/// the code. Nothing re-arms; unset or malformed configuration never registers a trap. Mock-first.
/// </summary>
public sealed class SnapshotCheckpointContractTests
{
    /// <summary>Spec is the raw VICESHARP_SNAPSHOT_AT value or null for unset; the driver advances the clock from Start to End, and the fake core dispatches the registered trap at BoundaryClock.</summary>
    public sealed record Scenario(string? Spec, ulong Start, ulong End, ulong BoundaryClock, int WriteResult);
    public sealed record TraceResult(int ExitCode, string[] Lines);
    public sealed record Fixture(Scenario Scenario, string[] Expected);

    public interface ISnapshotCheckpointProbe
    {
        TraceResult Run(Scenario scenario);
    }

    public static Fixture GetFixture(string name) => name switch
    {
        "unset" => new(new(null, 90, 110, 100, 0), ["end,0"]),
        "exact-boundary" => new(new("100:out.vsf", 90, 110, 100, 0),
            ["trap-registered,100", "write,out.vsf", "VICE_SNAP,checkpoint,100,100", "end,1"]),
        "delayed-by-interrupt" => new(new("100:out.vsf", 90, 110, 107, 0),
            ["trap-registered,100", "write,out.vsf", "VICE_SNAP,checkpoint,100,107", "end,1"]),
        "write-fails-negative" => new(new("100:out.vsf", 90, 110, 100, -1),
            ["trap-registered,100", "write,out.vsf", "VICE_SNAP,checkpoint-fail,100,100,-1", "end,1"]),
        "write-fails-positive" => new(new("100:out.vsf", 90, 110, 100, 1),
            ["trap-registered,100", "write,out.vsf", "VICE_SNAP,checkpoint-fail,100,100,1", "end,1"]),
        "once-only" => new(new("100:out.vsf", 90, 400, 100, 0),
            ["trap-registered,100", "write,out.vsf", "VICE_SNAP,checkpoint,100,100", "end,1"]),
        "trigger-not-reached" => new(new("500:out.vsf", 1, 100, 50, 0), ["end,0"]),
        "literal-100000" => new(new("100000:fixture.vsf", 99990, 100010, 100000, 0),
            ["trap-registered,100000", "write,fixture.vsf", "VICE_SNAP,checkpoint,100000,100000", "end,1"]),
        "malformed" => new(new("abc", 90, 110, 100, 0), ["VICE_SNAP,checkpoint-fail,0,0,-2", "end,0"]),
        "missing-path" => new(new("100:", 90, 110, 100, 0), ["VICE_SNAP,checkpoint-fail,0,0,-2", "end,0"]),
        _ => throw new ArgumentOutOfRangeException(nameof(name))
    };

    public static readonly string[] FixtureNames = ["unset", "exact-boundary", "delayed-by-interrupt", "write-fails-negative", "write-fails-positive", "once-only", "trigger-not-reached", "literal-100000", "malformed", "missing-path"];

    internal static void AssertProbe(ISnapshotCheckpointProbe probe, string name)
    {
        var fixture = GetFixture(name);
        var actual = probe.Run(fixture.Scenario);
        Assert.NotNull(actual);
        Assert.Equal(0, actual.ExitCode);
        Assert.Equal(fixture.Expected, actual.Lines);
    }

    [Fact]
    public void Write_clock_is_the_measured_callback_clock_and_a_trap_is_registered_at_most_once()
    {
        foreach (var name in FixtureNames)
        {
            var f = GetFixture(name);
            var registrations = f.Expected.Count(l => l.StartsWith("trap-registered,", StringComparison.Ordinal));
            Assert.True(registrations <= 1);
            Assert.Equal("end," + registrations.ToString(CultureInfo.InvariantCulture), f.Expected[^1]);
            foreach (var line in f.Expected.Where(l => l.StartsWith("VICE_SNAP,checkpoint,", StringComparison.Ordinal)))
            {
                var parts = line.Split(',');
                Assert.Equal(f.Scenario.BoundaryClock.ToString(CultureInfo.InvariantCulture), parts[3]);
                Assert.True(ulong.Parse(parts[3], CultureInfo.InvariantCulture) >= ulong.Parse(parts[2], CultureInfo.InvariantCulture));
            }
            if (f.Scenario.WriteResult != 0 && registrations == 1)
                Assert.Contains(f.Expected, l => l.StartsWith("VICE_SNAP,checkpoint-fail,", StringComparison.Ordinal));
        }
    }

    [Theory]
    [InlineData("unset")]
    [InlineData("exact-boundary")]
    [InlineData("delayed-by-interrupt")]
    [InlineData("write-fails-negative")]
    [InlineData("write-fails-positive")]
    [InlineData("once-only")]
    [InlineData("trigger-not-reached")]
    [InlineData("literal-100000")]
    [InlineData("malformed")]
    [InlineData("missing-path")]
    public void Correct_mock_passes_and_every_wrong_mock_is_rejected(string name)
    {
        var fixture = GetFixture(name);
        var correct = Substitute.For<ISnapshotCheckpointProbe>();
        correct.Run(Arg.Any<Scenario>()).Returns(new TraceResult(0, fixture.Expected.ToArray()));
        AssertProbe(correct, name);

        void Reject(TraceResult result)
        {
            var wrong = Substitute.For<ISnapshotCheckpointProbe>();
            wrong.Run(Arg.Any<Scenario>()).Returns(result);
            Assert.ThrowsAny<XunitException>(() => AssertProbe(wrong, name));
        }

        Reject(new(1, fixture.Expected));
        Reject(new(0, null!));
        Reject(new(0, fixture.Expected[..^1]));
        Reject(new(0, [.. fixture.Expected, "trap-registered,100"]));
        for (var i = 0; i < fixture.Expected.Length; i++)
        {
            var altered = fixture.Expected.ToArray();
            altered[i] = altered[i] + "0";
            Reject(new(0, altered));
        }
        if (fixture.Expected.Any(l => l.StartsWith("VICE_SNAP,checkpoint,", StringComparison.Ordinal)))
        {
            // A write clock equal to the trigger when the boundary was later would hide the delay (N22).
            var equated = fixture.Expected.Select(l => l.StartsWith("VICE_SNAP,checkpoint,", StringComparison.Ordinal) ? "VICE_SNAP,checkpoint,100,100" : l).ToArray();
            if (!fixture.Expected.SequenceEqual(equated)) Reject(new(0, equated));
            // A positive writer result reported as success must be rejected (N28).
            var successOnFail = fixture.Expected.Select(l => l.Replace("checkpoint-fail,100,100,1", "checkpoint,100,100", StringComparison.Ordinal)).ToArray();
            if (!fixture.Expected.SequenceEqual(successOnFail)) Reject(new(0, successOnFail));
        }
    }
}
