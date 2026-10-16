using System.Buffers.Binary;
using System.Text;
using NSubstitute;
using Xunit.Sdk;

namespace ViceSharp.VsfCompatibility.Tests;

/// <summary>
/// TR-VSFLOCKSTEP-RESUME-001 AC1 byte contract, proven with correct and
/// deliberately incorrect mocks before implementation. Synthetic fixtures are
/// not native snapshot/resume evidence. Source: immutable VICE cc67418,
/// mainc64cpu.c:884-978, interrupt.c:385-419, snapshot.h:112.
/// </summary>
public sealed class LegacyMainCpuContractTests
{
    /// <summary>Mock boundary for the external adapter; no emulator dependency.</summary>
    public interface IAdapter
    {
        /// <summary>Returns validated normalized bytes or fails closed.</summary>
        byte[] Normalize(byte[] input);
    }

    /// <summary>Every supported module version, container header and placement.</summary>
    public static IEnumerable<object[]> ValidCases =>
        from version in new[] { 2, 3, 4 }
        from oldHeader in new[] { false, true }
        from leadingModule in new[] { false, true }
        select new object[] { version, oldHeader, leadingModule };

    /// <summary>All rejected layouts, shared unchanged with the real adapter tests.</summary>
    public static IEnumerable<object[]> InvalidCases => InvalidFixtures().Select(f => new object[] { f.Name });

    [Theory]
    [MemberData(nameof(ValidCases))]
    public void Correct_mock_preserves_exact_bytes_and_assertions_detect_every_corruption(
        int version, bool oldHeader, bool leadingModule)
    {
        var fixture = ValidFixture(version, oldHeader, leadingModule);
        var mock = Substitute.For<IAdapter>();
        mock.Normalize(fixture.Input).Returns(fixture.Expected.ToArray());
        AssertNormalized(mock.Normalize, fixture);

        // No field or preserved byte can be ignored by the acceptance assertion.
        for (var offset = 0; offset < fixture.Expected.Length; offset++)
        {
            var wrong = fixture.Expected.ToArray();
            wrong[offset] ^= 0xff;
            mock.Normalize(fixture.Input).Returns(wrong);
            Assert.ThrowsAny<XunitException>(() => AssertNormalized(mock.Normalize, fixture));
        }
        mock.Normalize(fixture.Input).Returns(fixture.Expected[..^1]);
        Assert.ThrowsAny<XunitException>(() => AssertNormalized(mock.Normalize, fixture));
        mock.Normalize(fixture.Input).Returns([.. fixture.Expected, 0]);
        Assert.ThrowsAny<XunitException>(() => AssertNormalized(mock.Normalize, fixture));
        mock.Normalize(fixture.Input).Returns(_ => throw new InvalidDataException("Wrong rejection"));
        Assert.Throws<InvalidDataException>(() => AssertNormalized(mock.Normalize, fixture));
    }

    [Theory]
    [MemberData(nameof(InvalidCases))]
    public void Invalid_layout_must_throw_and_accepting_mock_is_detected(string name)
    {
        var input = InvalidFixtures().Single(f => f.Name == name).Input;
        var mock = Substitute.For<IAdapter>();
        mock.Normalize(input).Returns(_ => throw new InvalidDataException(name));
        AssertRejected(mock.Normalize, input);
        var accepting = Substitute.For<IAdapter>();
        accepting.Normalize(input).Returns(input.ToArray());
        Assert.ThrowsAny<XunitException>(() => AssertRejected(accepting.Normalize, input));
        var wrongException = Substitute.For<IAdapter>();
        wrongException.Normalize(input).Returns(_ => throw new IOException("Wrong exception"));
        Assert.ThrowsAny<XunitException>(() => AssertRejected(wrongException.Normalize, input));
    }

    [Fact]
    public void Acceptance_assertion_rejects_an_adapter_that_modifies_its_input()
    {
        var fixture = ValidFixture(2, false, true);
        Assert.ThrowsAny<XunitException>(() => AssertNormalized(input =>
        {
            input[^1] ^= 0xff;
            return fixture.Expected.ToArray();
        }, fixture));
    }

    internal sealed record Fixture(byte[] Input, byte[] Expected);

    internal static void AssertNormalized(Func<byte[], byte[]> normalize, Fixture fixture)
    {
        var before = fixture.Input.ToArray();
        Assert.Equal(fixture.Expected, normalize(fixture.Input));
        Assert.Equal(before, fixture.Input);
    }

    internal static void AssertRejected(Func<byte[], byte[]> normalize, byte[] input)
    {
        var before = input.ToArray();
        Assert.Throws<InvalidDataException>(() => normalize(input));
        Assert.Equal(before, input);
    }

    internal static Fixture ValidFixture(int minor, bool oldHeader, bool leadingModule)
    {
        // v1.4 payload: 19-byte CPU prefix, 12 bytes of log/jam fields,
        // 4-byte BA flags, then 68 bytes of interrupt state (8-byte clocks).
        var current = Enumerable.Range(0, 103).Select(i => (byte)(i + 31)).ToArray();
        if (minor == 2) current.AsSpan(19, 12).Clear();
        if (minor == 3) current.AsSpan(27, 4).Clear();
        byte[] legacy = minor switch
        {
            2 => [.. current[..19], .. current[31..]],
            3 => [.. current[..27], .. current[31..]],
            _ => current.ToArray()
        };
        var header = Header(oldHeader);
        var leading = leadingModule ? Module("BEFORE", 9, 7, [8, 0, 255]) : [];
        // Opaque bytes include the target name: searching raw bytes is invalid.
        var trailing = Module("AFTER", 7, 6, Encoding.ASCII.GetBytes("MAINCPU\0opaque\xff"));
        return new Fixture(
            [.. header, .. leading, .. Module("MAINCPU", 1, (byte)minor, legacy), .. trailing],
            [.. header, .. leading, .. Module("MAINCPU", 1, 4, current), .. trailing]);
    }

    internal static IEnumerable<(string Name, byte[] Input)> InvalidFixtures()
    {
        var valid = ValidFixture(4, false, false).Input;
        const int start = 58;
        yield return ("empty", []);
        yield return ("truncated-magic", valid[..18]);
        yield return ("wrong-magic", Changed(valid, 0, 0));
        yield return ("wrong-file-major", Changed(valid, 19, 3));
        yield return ("wrong-file-minor", Changed(valid, 20, 1));
        yield return ("wrong-machine", Changed(valid, 21, (byte)'V'));
        yield return ("nonterminated-machine", Changed(valid, 26, (byte)'X'));
        var nonSc = valid.ToArray();
        nonSc[24] = 0;
        nonSc[25] = 0;
        yield return ("c64-non-cycle-exact-machine", nonSc);
        yield return ("truncated-container", valid[..36]);
        yield return ("truncated-version-header", valid[..57]);
        yield return ("no-modules", Header(false));
        yield return ("missing-maincpu", [.. Header(false), .. Module("OTHER", 1, 0, [9])]);
        yield return ("duplicate-maincpu", [.. valid, .. valid.AsSpan(start, 125).ToArray()]);
        yield return ("unsupported-maincpu-major", Changed(valid, start + 16, 2));
        yield return ("unsupported-maincpu-old", Changed(valid, start + 17, 1));
        yield return ("unsupported-maincpu-future", Changed(valid, start + 17, 5));
        yield return ("version-size-disagreement", Changed(valid, start + 17, 2));
        yield return ("unterminated-maincpu-name", Changed(valid, start + 7, (byte)'X'));
        yield return ("module-length-zero", WithLength(valid, start, 0));
        yield return ("module-length-short-header", WithLength(valid, start, 21));
        yield return ("module-length-overflow", WithLength(valid, start, uint.MaxValue));
        yield return ("truncated-module", valid[..(start + 124)]);
        yield return ("trailing-partial-header", [.. valid, 7]);
        yield return ("trailing-module-overflow", WithLength(valid, start + 125, uint.MaxValue));
        yield return ("legacy-32bit-clock-layout", [.. Header(false), .. Module("MAINCPU", 1, 2, new byte[59])]);
    }

    private static byte[] Changed(byte[] original, int offset, byte value)
    {
        var copy = original.ToArray();
        copy[offset] = value;
        return copy;
    }

    private static byte[] WithLength(byte[] original, int moduleOffset, uint size)
    {
        var copy = original.ToArray();
        BinaryPrimitives.WriteUInt32LittleEndian(copy.AsSpan(moduleOffset + 18), size);
        return copy;
    }

    private static byte[] Header(bool old)
    {
        var header = new byte[old ? 37 : 58];
        Encoding.ASCII.GetBytes("VICE Snapshot File\x1a").CopyTo(header, 0);
        header[19] = 2;
        Encoding.ASCII.GetBytes("C64SC").CopyTo(header, 21);
        if (!old)
        {
            Encoding.ASCII.GetBytes("VICE Version\x1a").CopyTo(header, 37);
            new byte[] { 3, 10, 0, 0, 0x12, 0x34, 0x56, 0x78 }.CopyTo(header, 50);
        }
        return header;
    }

    private static byte[] Module(string name, byte major, byte minor, byte[] payload)
    {
        var module = new byte[22 + payload.Length];
        Encoding.ASCII.GetBytes(name).CopyTo(module, 0);
        module[16] = major;
        module[17] = minor;
        BinaryPrimitives.WriteUInt32LittleEndian(module.AsSpan(18), (uint)module.Length);
        payload.CopyTo(module, 22);
        return module;
    }
}
