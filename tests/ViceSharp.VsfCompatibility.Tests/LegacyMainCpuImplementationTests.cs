using System.Buffers.Binary;
using System.Security.Cryptography;
using ViceSharp.ViceCompatibility;

namespace ViceSharp.VsfCompatibility.Tests;

/// <summary>
/// TR-VSFLOCKSTEP-RESUME-001 AC1: the exact mock-validated byte assertions now
/// exercise the external implementation. No native binary is loaded or built.
/// Native resume, VIC-II alignment and immutable round-trip gates remain open.
/// </summary>
public sealed class LegacyMainCpuImplementationTests
{
    [Theory]
    [MemberData(nameof(LegacyMainCpuContractTests.ValidCases), MemberType = typeof(LegacyMainCpuContractTests))]
    public void Real_adapter_satisfies_the_same_byte_contract(int version, bool oldHeader, bool leadingModule)
    {
        var fixture = LegacyMainCpuContractTests.ValidFixture(version, oldHeader, leadingModule);
        LegacyMainCpuContractTests.AssertNormalized(b => X64ScSnapshotCompatibility.Normalize(b), fixture);
        var once = X64ScSnapshotCompatibility.Normalize(fixture.Input);
        Assert.Equal(once, X64ScSnapshotCompatibility.Normalize(once));
    }

    [Theory]
    [MemberData(nameof(LegacyMainCpuContractTests.InvalidCases), MemberType = typeof(LegacyMainCpuContractTests))]
    public void Real_adapter_rejects_every_mock_validated_invalid_layout(string name)
    {
        var input = LegacyMainCpuContractTests.InvalidFixtures().Single(f => f.Name == name).Input;
        LegacyMainCpuContractTests.AssertRejected(b => X64ScSnapshotCompatibility.Normalize(b), input);
    }

    [Fact]
    public void Retained_v12_fixture_changes_only_version_length_and_inserted_defaults()
    {
        var input = ReadFixture("native-maincpu-1.2.vsf",
            "5C284F5CF0A1BC7E01A2E4117A74158B7A9D60485F5626448F4B7655C0E23C19");
        var before = input.ToArray();
        var result = X64ScSnapshotCompatibility.Normalize(input);
        // This fixture's MAINCPU module begins at 58. Its payload begins at 80;
        // the 19-byte CPU prefix ends at 99. No parser is shared with production.
        Assert.Equal(input.Length + 12, result.Length);
        Assert.Equal(input[..75], result[..75]);
        Assert.Equal(4, result[75]);
        Assert.Equal(125u, BinaryPrimitives.ReadUInt32LittleEndian(result.AsSpan(76, 4)));
        Assert.Equal(input[80..99], result[80..99]);
        Assert.Equal(new byte[12], result[99..111]);
        Assert.Equal(input[99..], result[111..]);
        Assert.Equal(before, input);
        Assert.Equal(result, X64ScSnapshotCompatibility.Normalize(result));
    }

    [Fact]
    public void Retained_v14_fixture_passes_through_byte_identically()
    {
        var input = ReadFixture("native-maincpu-1.4.vsf",
            "1D6A178A1C415FA1B5BCCC30551E5FC210701E363762B05EEDCDF237F88F8EFF");
        var result = X64ScSnapshotCompatibility.Normalize(input);
        Assert.Equal(input, result);
        Assert.NotSame(input, result);
    }

    private static byte[] ReadFixture(string name, string expectedHash)
    {
        var bytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));
        Assert.Equal(expectedHash, Convert.ToHexString(SHA256.HashData(bytes)));
        return bytes;
    }
}
