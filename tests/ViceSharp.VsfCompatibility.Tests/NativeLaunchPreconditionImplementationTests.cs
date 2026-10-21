using System.Security.Cryptography;
using ViceSharp.ViceCompatibility;

namespace ViceSharp.VsfCompatibility.Tests;

/// <summary>
/// PLAN-NATIVERESTORE-001 D1 (R10 A, N05, N07, N23): the mock-validated assertions of
/// <see cref="NativeLaunchPreconditionContractTests"/> bound to the real adapter, plus the approved fixtures
/// (round 2 N01 byte inspection: ready-c64sc-truedrive.vsf has DRIVE8 has_tde 0 and needs "-drive8type 0";
/// native-maincpu-1.4.vsf has every unit with has_tde 1 and needs nothing). No native binary is loaded or built.
/// </summary>
public sealed class NativeLaunchPreconditionImplementationTests
{
    [Theory]
    [MemberData(nameof(NativeLaunchPreconditionContractTests.ValidNames), MemberType = typeof(NativeLaunchPreconditionContractTests))]
    public void Real_adapter_reads_units_and_computes_the_precondition(string name)
    {
        var fixture = NativeLaunchPreconditionContractTests.GetFixture(name);
        NativeLaunchPreconditionContractTests.AssertUnits(
            b => X64ScNativeLaunch.ReadDriveUnits(b).Select(u => new NativeLaunchPreconditionContractTests.DriveUnit(u.Unit, u.Present, u.HasTde, u.HasDrives)).ToArray(), fixture);
        NativeLaunchPreconditionContractTests.AssertPrecondition(b => X64ScNativeLaunch.ComputePrecondition(b), fixture);
    }

    [Theory]
    [MemberData(nameof(NativeLaunchPreconditionContractTests.InvalidNames), MemberType = typeof(NativeLaunchPreconditionContractTests))]
    public void Real_adapter_rejects_invalid_drive_payloads(string name)
    {
        var fixture = NativeLaunchPreconditionContractTests.GetFixture(name);
        NativeLaunchPreconditionContractTests.AssertRejected(b => X64ScNativeLaunch.ComputePrecondition(b), fixture);
    }

    [Theory]
    [InlineData(109163946UL, 100000UL, 2956000UL, 109263946UL, "clock plus window")]
    [InlineData(5000UL, 100000UL, 2956000UL, 3056000UL, "prelude plus window")]
    [InlineData(2956000UL, 1UL, 2956000UL, 2956001UL, "clock plus window")]
    public void Real_budget_matches_the_contract(ulong clock, ulong window, ulong prelude, ulong expected, string reason)
    {
        NativeLaunchPreconditionContractTests.AssertBudget(
            (c, w, p) => { var b = X64ScNativeLaunch.ComputeBudget(c, w, p); return new NativeLaunchPreconditionContractTests.LaunchBudget(b.LimitCycles, b.Reason); },
            clock, window, prelude, expected, reason);
    }

    [Fact]
    public void Real_budget_rejects_an_empty_window_and_overflow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => X64ScNativeLaunch.ComputeBudget(10, 0, 100));
        Assert.Throws<OverflowException>(() => X64ScNativeLaunch.ComputeBudget(ulong.MaxValue, 1, 100));
        Assert.Throws<OverflowException>(() => X64ScNativeLaunch.ComputeBudget(1, 1, ulong.MaxValue));
    }

    [Theory]
    [InlineData("a.vsf", new[] { "-drive8type", "0" }, 109263946UL)]
    [InlineData("b.vsf", new string[0], 3056000UL)]
    [InlineData("c.vsf", new[] { "-drive9type", "0", "-drive11type", "0" }, 7UL)]
    public void Real_arguments_match_the_contract(string path, string[] precondition, ulong limit)
    {
        NativeLaunchPreconditionContractTests.AssertArguments(X64ScNativeLaunch.BuildArguments, path, precondition, limit);
    }

    [Fact]
    public void Approved_fixture_needs_unit8_type_none_and_its_maincpu_fields_match_the_round2_inspection()
    {
        var bytes = ReadFixture("native-maincpu-1.2.vsf", "5C284F5CF0A1BC7E01A2E4117A74158B7A9D60485F5626448F4B7655C0E23C19");
        Assert.Equal(["-drive8type", "0"], X64ScNativeLaunch.ComputePrecondition(bytes));
        var units = X64ScNativeLaunch.ReadDriveUnits(bytes);
        Assert.Equal(new X64ScNativeLaunch.DriveUnit(8, true, 0, 1), units[0]);
        Assert.All(units[1..], u => Assert.Equal(1, u.HasTde));
        var cpu = X64ScNativeLaunch.ReadMainCpu(bytes);
        Assert.Equal((byte)1, cpu.Major);
        Assert.Equal((byte)2, cpu.Minor);
        Assert.Equal(109163946UL, cpu.Clock);
        Assert.Equal((ushort)22377, cpu.Pc);
        Assert.Equal((byte)0xA0, cpu.Status);
        // The normalized (v1.4) bytes keep the same register fields and the same precondition.
        var normalized = X64ScSnapshotCompatibility.Normalize(bytes);
        Assert.Equal(["-drive8type", "0"], X64ScNativeLaunch.ComputePrecondition(normalized));
        Assert.Equal(cpu with { Minor = 4 }, X64ScNativeLaunch.ReadMainCpu(normalized));
    }

    [Fact]
    public void Current_v14_fixture_needs_no_precondition()
    {
        var bytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "native-maincpu-1.4.vsf"));
        Assert.Empty(X64ScNativeLaunch.ComputePrecondition(bytes));
        Assert.All(X64ScNativeLaunch.ReadDriveUnits(bytes), u => Assert.Equal(1, u.HasTde));
        var cpu = X64ScNativeLaunch.ReadMainCpu(bytes);
        Assert.Equal((byte)4, cpu.Minor);
        Assert.Equal(30250590UL, cpu.Clock);
        Assert.Equal((ushort)3937, cpu.Pc);
    }

    private static byte[] ReadFixture(string name, string sha256)
    {
        var bytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));
        Assert.Equal(sha256, Convert.ToHexString(SHA256.HashData(bytes)));
        return bytes;
    }
}
