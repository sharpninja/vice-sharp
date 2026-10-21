using NSubstitute;
using Xunit.Sdk;

namespace ViceSharp.VsfCompatibility.Tests;

/// <summary>
/// PLAN-NATIVERESTORE-001 D1, decisions R10 A (adapter launch precondition), N05 (payload validation), N07 and N23
/// (launch budget). TR-VSFLOCKSTEP-RESUME-001 AC1 (real resume of external v1.2 and v1.4 inputs as amended by R15).
/// Pinned VICE cc67418: drive-snapshot.c lines 139 to 162 write has_tde and has_drives as DRIVE payload bytes 0 and 1;
/// the reader (lines 317 to 319, 368 to 419, 540 to 550) copies three uninitialized clocks into every unit whose type
/// is not DRIVE_TYPE_NONE when its module is missing or has has_tde 0, so the native launch passes "-drive&lt;n&gt;type 0"
/// for exactly those units, last on the command line. initcmdline.c 279 to 286: -limitcycles is an absolute
/// maincpu_clk limit. Mock-first: correct mocks pass every assertion and wrong mocks are rejected.
/// </summary>
public sealed class NativeLaunchPreconditionContractTests
{
    /// <summary>One DRIVE unit as read from the container payload.</summary>
    public sealed record DriveUnit(int Unit, bool Present, int? HasTde, int? HasDrives);

    /// <summary>The computed launch budget (N07, N23): limit = C + W unless C + W falls inside the prelude.</summary>
    public sealed record LaunchBudget(ulong LimitCycles, string Reason);

    /// <summary>Mock boundary for the launch adapter; no emulator dependency.</summary>
    public interface ILaunchAdapter
    {
        DriveUnit[] ReadDriveUnits(byte[] snapshot);
        string[] ComputePrecondition(byte[] snapshot);
        LaunchBudget ComputeBudget(ulong mainCpuClock, ulong windowCycles, ulong preludeCycles);
        string[] BuildArguments(string snapshotPath, string[] precondition, ulong limitCycles);
    }

    public sealed record Fixture(string Name, byte[] Snapshot, DriveUnit[] Units, string[] Precondition, bool Valid);

    private static byte[] Payload(params byte[] bytes) => bytes;

    /// <summary>Synthetic containers; the approved fixture binding lives in the implementation tests.</summary>
    public static Fixture GetFixture(string name) => name switch
    {
        "all-tde-on" => new(name, VsfBuilder.Build(VsfBuilder.MainCpu(1, 4, 5000, 0x0400, 0x24),
            VsfBuilder.Drive(8, Payload(1, 1, 9)), VsfBuilder.Drive(9, Payload(1, 1, 9)), VsfBuilder.Drive(10, Payload(1, 1, 9)), VsfBuilder.Drive(11, Payload(1, 1, 9))),
            [new(8, true, 1, 1), new(9, true, 1, 1), new(10, true, 1, 1), new(11, true, 1, 1)], [], true),
        "unit8-tde-off" => new(name, VsfBuilder.Build(VsfBuilder.MainCpu(1, 2, 109163946, 22377, 0xA0),
            VsfBuilder.Drive(8, Payload(0, 1)), VsfBuilder.Drive(9, Payload(1, 1, 9)), VsfBuilder.Drive(10, Payload(1, 1, 9)), VsfBuilder.Drive(11, Payload(1, 1, 9))),
            [new(8, true, 0, 1), new(9, true, 1, 1), new(10, true, 1, 1), new(11, true, 1, 1)], ["-drive8type", "0"], true),
        "unit9-missing-unit11-off" => new(name, VsfBuilder.Build(VsfBuilder.MainCpu(1, 4, 7, 1, 0),
            VsfBuilder.Drive(8, Payload(1, 1, 9)), VsfBuilder.Drive(10, Payload(1, 1, 9)), VsfBuilder.Drive(11, Payload(0, 2))),
            [new(8, true, 1, 1), new(9, false, null, null), new(10, true, 1, 1), new(11, true, 0, 2)], ["-drive9type", "0", "-drive11type", "0"], true),
        "all-missing" => new(name, VsfBuilder.Build(VsfBuilder.MainCpu(1, 4, 7, 1, 0)),
            [new(8, false, null, null), new(9, false, null, null), new(10, false, null, null), new(11, false, null, null)],
            ["-drive8type", "0", "-drive9type", "0", "-drive10type", "0", "-drive11type", "0"], true),
        "truncated-payload" => new(name, VsfBuilder.Build(VsfBuilder.MainCpu(1, 4, 7, 1, 0),
            VsfBuilder.Drive(8, Payload(1)), VsfBuilder.Drive(9, Payload(1, 1, 9))),
            [new(8, true, null, null), new(9, true, 1, 1), new(10, false, null, null), new(11, false, null, null)], [], false),
        "empty-payload" => new(name, VsfBuilder.Build(VsfBuilder.MainCpu(1, 4, 7, 1, 0),
            VsfBuilder.Drive(8, Payload()), VsfBuilder.Drive(9, Payload(1, 1, 9))),
            [new(8, true, null, null), new(9, true, 1, 1), new(10, false, null, null), new(11, false, null, null)], [], false),
        _ => throw new ArgumentOutOfRangeException(nameof(name))
    };

    public static IEnumerable<object[]> ValidNames => new[] { "all-tde-on", "unit8-tde-off", "unit9-missing-unit11-off", "all-missing" }.Select(n => new object[] { n });
    public static IEnumerable<object[]> InvalidNames => new[] { "truncated-payload", "empty-payload" }.Select(n => new object[] { n });

    internal static void AssertUnits(Func<byte[], DriveUnit[]> read, Fixture fixture)
    {
        var units = read(fixture.Snapshot);
        Assert.NotNull(units);
        Assert.Equal(fixture.Units, units);
    }

    internal static void AssertPrecondition(Func<byte[], string[]> compute, Fixture fixture)
    {
        var flags = compute(fixture.Snapshot);
        Assert.NotNull(flags);
        Assert.Equal(fixture.Precondition, flags);
        Assert.Equal(0, flags.Length % 2);
        for (var i = 0; i < flags.Length; i += 2)
        {
            Assert.Matches("^-drive(8|9|10|11)type$", flags[i]);
            Assert.Equal("0", flags[i + 1]);
        }
    }

    internal static void AssertRejected(Func<byte[], string[]> compute, Fixture fixture)
    {
        Assert.Throws<InvalidDataException>(() => compute(fixture.Snapshot));
    }

    internal static void AssertBudget(Func<ulong, ulong, ulong, LaunchBudget> compute, ulong clock, ulong window, ulong prelude, ulong expectedLimit, string expectedReason)
    {
        var budget = compute(clock, window, prelude);
        Assert.NotNull(budget);
        Assert.Equal(expectedLimit, budget.LimitCycles);
        Assert.Equal(expectedReason, budget.Reason);
        Assert.True(budget.LimitCycles > prelude, "the limit must lie beyond the prelude so the load can happen");
        Assert.True(budget.LimitCycles >= clock + window, "the limit must cover the whole window after the restored clock");
    }

    internal static void AssertArguments(Func<string, string[], ulong, string[]> build, string path, string[] precondition, ulong limit)
    {
        var args = build(path, precondition, limit);
        Assert.NotNull(args);
        Assert.Equal("-autostart", args[0]);
        Assert.Equal(path, args[1]);
        Assert.Contains("+logcolorize", args);
        var seed = Array.IndexOf(args, "-seed");
        Assert.True(seed >= 0 && args[seed + 1] == "1", "-seed 1 is required for deterministic RAM init");
        var limitIndex = Array.IndexOf(args, "-limitcycles");
        Assert.True(limitIndex >= 0, "-limitcycles is required");
        Assert.Equal(limit.ToString(System.Globalization.CultureInfo.InvariantCulture), args[limitIndex + 1]);
        // The precondition flags are the last tokens, so no later argument can override them (N05).
        Assert.Equal(precondition, args[(args.Length - precondition.Length)..]);
        Assert.Equal(1, args.Count(a => a == "-limitcycles"));
        Assert.Equal(1, args.Count(a => a == "-autostart"));
    }

    [Theory]
    [MemberData(nameof(ValidNames))]
    public void Correct_mock_units_and_precondition_pass_and_every_wrong_mock_is_rejected(string name)
    {
        var fixture = GetFixture(name);
        var correct = Substitute.For<ILaunchAdapter>();
        correct.ReadDriveUnits(fixture.Snapshot).Returns(fixture.Units.ToArray());
        correct.ComputePrecondition(fixture.Snapshot).Returns(fixture.Precondition.ToArray());
        AssertUnits(correct.ReadDriveUnits, fixture);
        AssertPrecondition(correct.ComputePrecondition, fixture);

        var wrongUnit = Substitute.For<ILaunchAdapter>();
        var flipped = fixture.Units.Select(u => u with { HasTde = u.HasTde is null ? 0 : 1 - u.HasTde }).ToArray();
        wrongUnit.ReadDriveUnits(fixture.Snapshot).Returns(flipped);
        Assert.ThrowsAny<XunitException>(() => AssertUnits(wrongUnit.ReadDriveUnits, fixture));

        var missingFlag = Substitute.For<ILaunchAdapter>();
        missingFlag.ComputePrecondition(fixture.Snapshot).Returns(fixture.Precondition.Length > 0 ? fixture.Precondition[..^2] : ["-drive8type", "0"]);
        Assert.ThrowsAny<XunitException>(() => AssertPrecondition(missingFlag.ComputePrecondition, fixture));

        var wrongValue = Substitute.For<ILaunchAdapter>();
        wrongValue.ComputePrecondition(fixture.Snapshot).Returns([.. fixture.Precondition, "-drive8type", "1"]);
        Assert.ThrowsAny<XunitException>(() => AssertPrecondition(wrongValue.ComputePrecondition, fixture));

        var nullMock = Substitute.For<ILaunchAdapter>();
        nullMock.ComputePrecondition(fixture.Snapshot).Returns((string[])null!);
        Assert.ThrowsAny<XunitException>(() => AssertPrecondition(nullMock.ComputePrecondition, fixture));
    }

    [Theory]
    [MemberData(nameof(InvalidNames))]
    public void Invalid_payload_must_throw_and_an_accepting_mock_is_rejected(string name)
    {
        var fixture = GetFixture(name);
        var correct = Substitute.For<ILaunchAdapter>();
        correct.ComputePrecondition(fixture.Snapshot).Returns(_ => throw new InvalidDataException(name));
        AssertRejected(correct.ComputePrecondition, fixture);

        var accepting = Substitute.For<ILaunchAdapter>();
        accepting.ComputePrecondition(fixture.Snapshot).Returns(["-drive8type", "0"]);
        Assert.ThrowsAny<XunitException>(() => AssertRejected(accepting.ComputePrecondition, fixture));
    }

    [Theory]
    [InlineData(109163946UL, 100000UL, 2956000UL, 109263946UL, "clock plus window")]
    [InlineData(5000UL, 100000UL, 2956000UL, 3056000UL, "prelude plus window")]
    [InlineData(2956000UL, 1UL, 2956000UL, 2956001UL, "clock plus window")]
    public void Budget_contract_accepts_the_correct_mock_and_rejects_wrong_limits(ulong clock, ulong window, ulong prelude, ulong expected, string reason)
    {
        var correct = Substitute.For<ILaunchAdapter>();
        correct.ComputeBudget(clock, window, prelude).Returns(new LaunchBudget(expected, reason));
        AssertBudget(correct.ComputeBudget, clock, window, prelude, expected, reason);

        var tooLow = Substitute.For<ILaunchAdapter>();
        tooLow.ComputeBudget(clock, window, prelude).Returns(new LaunchBudget(expected - 1, reason));
        Assert.ThrowsAny<XunitException>(() => AssertBudget(tooLow.ComputeBudget, clock, window, prelude, expected, reason));

        var wrongReason = Substitute.For<ILaunchAdapter>();
        wrongReason.ComputeBudget(clock, window, prelude).Returns(new LaunchBudget(expected, "guess"));
        Assert.ThrowsAny<XunitException>(() => AssertBudget(wrongReason.ComputeBudget, clock, window, prelude, expected, reason));
    }

    [Fact]
    public void Budget_rejects_an_empty_window_and_overflow()
    {
        var correct = Substitute.For<ILaunchAdapter>();
        correct.ComputeBudget(10, 0, 100).Returns(_ => throw new ArgumentOutOfRangeException("window"));
        Assert.Throws<ArgumentOutOfRangeException>(() => correct.ComputeBudget(10, 0, 100));
        correct.ComputeBudget(ulong.MaxValue, 1, 100).Returns(_ => throw new OverflowException());
        Assert.Throws<OverflowException>(() => correct.ComputeBudget(ulong.MaxValue, 1, 100));
    }

    [Theory]
    [InlineData("a.vsf", new[] { "-drive8type", "0" }, 109263946UL)]
    [InlineData("b.vsf", new string[0], 3056000UL)]
    [InlineData("c.vsf", new[] { "-drive9type", "0", "-drive11type", "0" }, 7UL)]
    public void Argument_contract_accepts_the_correct_mock_and_rejects_reordered_or_missing_tokens(string path, string[] precondition, ulong limit)
    {
        string[] Expected() => ["-autostart", path, "+logcolorize", "-seed", "1", "-limitcycles", limit.ToString(System.Globalization.CultureInfo.InvariantCulture), .. precondition];
        var correct = Substitute.For<ILaunchAdapter>();
        correct.BuildArguments(path, precondition, limit).Returns(Expected());
        AssertArguments(correct.BuildArguments, path, precondition, limit);

        if (precondition.Length > 0)
        {
            var reordered = Substitute.For<ILaunchAdapter>();
            reordered.BuildArguments(path, precondition, limit).Returns([.. precondition, "-autostart", path, "+logcolorize", "-seed", "1", "-limitcycles", limit.ToString(System.Globalization.CultureInfo.InvariantCulture)]);
            Assert.ThrowsAny<XunitException>(() => AssertArguments(reordered.BuildArguments, path, precondition, limit));
        }

        var noSeed = Substitute.For<ILaunchAdapter>();
        noSeed.BuildArguments(path, precondition, limit).Returns(["-autostart", path, "+logcolorize", "-limitcycles", limit.ToString(System.Globalization.CultureInfo.InvariantCulture), .. precondition]);
        Assert.ThrowsAny<XunitException>(() => AssertArguments(noSeed.BuildArguments, path, precondition, limit));

        var wrongLimit = Substitute.For<ILaunchAdapter>();
        wrongLimit.BuildArguments(path, precondition, limit).Returns(["-autostart", path, "+logcolorize", "-seed", "1", "-limitcycles", (limit + 1).ToString(System.Globalization.CultureInfo.InvariantCulture), .. precondition]);
        Assert.ThrowsAny<XunitException>(() => AssertArguments(wrongLimit.BuildArguments, path, precondition, limit));
    }
}
