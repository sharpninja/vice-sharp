using NSubstitute;
using Xunit.Sdk;

namespace ViceSharp.VsfCompatibility.Tests;

/// <summary>
/// FR-VICE-001 AC4, TR-VICE-ORACLE-001 AC3/AC5, TEST-VICE-001 AC4:
/// a missing or unsuccessful native snapshot acknowledgement cannot pass.
/// Pinned VICE cc67418: doc/vice.texi binary command/response structures,
/// monitor_binary.c:887-915 (undump) and monitor_binary_error (type zero).
/// These mocks validate assertions, not native behavior.
/// </summary>
public sealed class BinaryMonitorContractTests
{
    public interface IProtocol
    {
        byte[] Encode(uint requestId, byte command, byte[] body);
        ObservedResponse Read(Stream source);
        void RequireSuccess(ObservedResponse response, uint requestId, byte command, int bodyLength);
    }

    public sealed record ObservedResponse(byte Type, byte Error, uint RequestId, byte[] Body);
    public sealed record ReadFixture(string Name, byte[] Frame, ObservedResponse Expected);
    public sealed record RejectedFixture(string Name, byte[] Frame, Type ExceptionType, int Consumed);

    internal static readonly byte[] DumpCommand =
        [2, 2, 8, 0, 0, 0, 0x78, 0x56, 0x34, 0x12, 0x41, 0, 0, 5, 97, 46, 118, 115, 102];
    internal static readonly byte[] DumpBody = [0, 0, 5, 97, 46, 118, 115, 102];
    internal static readonly byte[] LoadReply = [2, 2, 2, 0, 0, 0, 0x42, 0, 0x78, 0x56, 0x34, 0x12, 0x69, 0x57];
    internal const int BodyLimit = 1024 * 1024;

    public static IEnumerable<object[]> ReadCases =>
        from fixture in GoodFixtures() from fragment in new[] { 1, 3, 64 }
        select new object[] { fixture.Name, fragment };

    public static IEnumerable<object[]> RejectedCases => BadFixtures().Select(f => new object[] { f.Name });

    public static IEnumerable<object[]> AckCases => new[]
    {
        "dump-ok", "load-ok", "native-error-zero-type", "error-with-expected-type",
        "wrong-id", "unsolicited", "wrong-type", "body-short", "body-long",
        "missing-body", "null-body", "reserved-expected-id", "negative-expected-length", "oversize-expected-length",
        "error-01", "error-02", "error-80", "error-81", "error-82", "error-83", "error-ff"
    }.Select(n => new object[] { n });

    [Theory]
    [InlineData(0, 0u)]
    [InlineData(BodyLimit, 0xfffffffeu)]
    public void Encoder_mock_proves_inclusive_size_and_nonreserved_id_boundaries(int length, uint id)
    {
        var body = Enumerable.Repeat((byte)0xa5, length).ToArray();
        byte[] header = length == 0
            ? [2, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0x41]
            : [2, 2, 0, 0, 16, 0, 254, 255, 255, 255, 0x41];
        byte[] expected = [.. header, .. body];
        var correct = Substitute.For<IProtocol>();
        correct.Encode(id, 0x41, Arg.Any<byte[]>()).Returns(expected);
        AssertEncodeBoundary(correct, length, id);
        var wrong = Substitute.For<IProtocol>();
        wrong.Encode(id, 0x41, Arg.Any<byte[]>()).Returns(expected[..^1]);
        Assert.ThrowsAny<XunitException>(() => AssertEncodeBoundary(wrong, length, id));
    }

    [Fact]
    public void Reader_mock_proves_inclusive_body_size_limit()
    {
        var fixture = MaximumReadFixture();
        AssertRead(ReaderMock(fixture, fixture.Expected, fixture.Frame.Length), fixture, 65536);
        var shortBody = fixture.Expected with { Body = fixture.Expected.Body[..^1] };
        Assert.ThrowsAny<XunitException>(() => AssertRead(ReaderMock(fixture, shortBody, fixture.Frame.Length), fixture, 65536));
    }

    [Fact]
    public void Reader_mock_rejects_null_stream()
    {
        var correct = Substitute.For<IProtocol>();
        correct.Read(null!).Returns(_ => throw new ArgumentNullException("source"));
        AssertNullStream(correct);
        var wrong = Substitute.For<IProtocol>();
        wrong.Read(null!).Returns(new ObservedResponse(0x41, 0, 1, []));
        Assert.ThrowsAny<XunitException>(() => AssertNullStream(wrong));
    }

    [Fact]
    public void Encoder_mock_assertions_reject_every_wrong_byte_length_and_input_mutation()
    {
        var good = Substitute.For<IProtocol>();
        good.Encode(0x12345678u, 0x41, Arg.Any<byte[]>()).Returns(DumpCommand.ToArray());
        AssertEncode(good);
        foreach (var index in Enumerable.Range(0, DumpCommand.Length))
        {
            var badBytes = DumpCommand.ToArray();
            badBytes[index] ^= 0xff;
            var bad = Substitute.For<IProtocol>();
            bad.Encode(Arg.Any<uint>(), Arg.Any<byte>(), Arg.Any<byte[]>()).Returns(badBytes);
            Assert.ThrowsAny<XunitException>(() => AssertEncode(bad));
        }
        foreach (var badBytes in new[] { DumpCommand[..^1], DumpCommand.Concat(new byte[] { 0 }).ToArray() })
        {
            var bad = Substitute.For<IProtocol>();
            bad.Encode(Arg.Any<uint>(), Arg.Any<byte>(), Arg.Any<byte[]>()).Returns(badBytes);
            Assert.ThrowsAny<XunitException>(() => AssertEncode(bad));
        }
        var mutating = Substitute.For<IProtocol>();
        mutating.Encode(Arg.Any<uint>(), Arg.Any<byte>(), Arg.Any<byte[]>()).Returns(call =>
        {
            call.ArgAt<byte[]>(2)[0] ^= 1;
            return DumpCommand.ToArray();
        });
        Assert.ThrowsAny<XunitException>(() => AssertEncode(mutating));
    }

    [Theory]
    [InlineData("reserved-id")]
    [InlineData("oversize-body")]
    [InlineData("null-body")]
    public void Encoder_mock_rejections_discriminate_accepting_and_wrong_exception_mocks(string name)
    {
        var good = Substitute.For<IProtocol>();
        good.Encode(Arg.Any<uint>(), Arg.Any<byte>(), Arg.Any<byte[]>()).Returns(_ =>
            throw (name == "null-body" ? new ArgumentNullException("body") : new ArgumentOutOfRangeException(name)));
        AssertEncodeRejected(good, name);
        var accepting = Substitute.For<IProtocol>();
        accepting.Encode(Arg.Any<uint>(), Arg.Any<byte>(), Arg.Any<byte[]>()).Returns(Array.Empty<byte>());
        Assert.ThrowsAny<XunitException>(() => AssertEncodeRejected(accepting, name));
        var wrong = Substitute.For<IProtocol>();
        wrong.Encode(Arg.Any<uint>(), Arg.Any<byte>(), Arg.Any<byte[]>()).Returns(_ => throw new IOException());
        Assert.ThrowsAny<XunitException>(() => AssertEncodeRejected(wrong, name));
    }

    [Theory]
    [MemberData(nameof(ReadCases))]
    public void Reader_mocks_discriminate_all_fields_and_frame_consumption(string name, int fragment)
    {
        var fixture = GoodFixtures().Single(f => f.Name == name);
        var correct = ReaderMock(fixture, fixture.Expected, fixture.Frame.Length);
        AssertRead(correct, fixture, fragment);
        var wrongResponses = new[]
        {
            fixture.Expected with { Type = (byte)(fixture.Expected.Type ^ 1) },
            fixture.Expected with { Error = (byte)(fixture.Expected.Error ^ 1) },
            fixture.Expected with { RequestId = fixture.Expected.RequestId ^ 1 },
            fixture.Expected with { Body = [.. fixture.Expected.Body, 0] },
            fixture.Expected with { Body = fixture.Expected.Body.Length == 0 ? [1] : fixture.Expected.Body.Select(b => (byte)(b ^ 0xff)).ToArray() }
        };
        foreach (var response in wrongResponses)
            Assert.ThrowsAny<XunitException>(() => AssertRead(ReaderMock(fixture, response, fixture.Frame.Length), fixture, fragment));
        foreach (var consumed in new[] { fixture.Frame.Length - 1, fixture.Frame.Length + 1 })
            Assert.ThrowsAny<XunitException>(() => AssertRead(ReaderMock(fixture, fixture.Expected, consumed), fixture, fragment));
    }

    [Theory]
    [MemberData(nameof(RejectedCases))]
    public void Rejected_frame_assertions_detect_false_success_wrong_error_and_overread(string name)
    {
        var fixture = BadFixtures().Single(f => f.Name == name);
        AssertRejected(RejectingMock(fixture, fixture.ExceptionType, fixture.Consumed), fixture);
        var accepting = Substitute.For<IProtocol>();
        accepting.Read(Arg.Any<Stream>()).Returns(new ObservedResponse(0x42, 0, 0x12345678, [0x69, 0x57]));
        Assert.ThrowsAny<XunitException>(() => AssertRejected(accepting, fixture));
        Assert.ThrowsAny<XunitException>(() => AssertRejected(RejectingMock(fixture, typeof(IOException), fixture.Consumed), fixture));
        if (fixture.Consumed > 0)
            Assert.ThrowsAny<XunitException>(() => AssertRejected(RejectingMock(fixture, fixture.ExceptionType, fixture.Consumed - 1), fixture));
    }

    [Theory]
    [MemberData(nameof(AckCases))]
    public void Acknowledgement_assertions_discriminate_false_acceptance_and_false_rejection(string name)
    {
        var valid = name.EndsWith("-ok", StringComparison.Ordinal);
        var correct = Substitute.For<IProtocol>();
        if (!valid) correct.When(p => p.RequireSuccess(Arg.Any<ObservedResponse>(), Arg.Any<uint>(), Arg.Any<byte>(), Arg.Any<int>()))
            .Do(_ => throw new InvalidDataException());
        AssertAck(correct, name);
        var wrong = Substitute.For<IProtocol>();
        if (valid) wrong.When(p => p.RequireSuccess(Arg.Any<ObservedResponse>(), Arg.Any<uint>(), Arg.Any<byte>(), Arg.Any<int>()))
            .Do(_ => throw new InvalidDataException());
        if (valid) Assert.Throws<InvalidDataException>(() => AssertAck(wrong, name));
        else Assert.ThrowsAny<XunitException>(() => AssertAck(wrong, name));
    }

    internal static void AssertEncode(IProtocol protocol)
    {
        var input = DumpBody.ToArray();
        var result = protocol.Encode(0x12345678, 0x41, input);
        Assert.Equal(DumpCommand, result);
        Assert.Equal(DumpBody, input);
        Assert.NotSame(input, result);
    }

    internal static void AssertEncodeRejected(IProtocol protocol, string name)
    {
        var requestId = name == "reserved-id" ? uint.MaxValue : 1u;
        var input = name == "null-body" ? null! : name == "oversize-body" ? new byte[BodyLimit + 1] : DumpBody.ToArray();
        if (name == "null-body") Assert.Throws<ArgumentNullException>(() => protocol.Encode(requestId, 0x42, input));
        else Assert.Throws<ArgumentOutOfRangeException>(() => protocol.Encode(requestId, 0x42, input));
    }

    internal static void AssertEncodeBoundary(IProtocol protocol, int length, uint id)
    {
        var body = Enumerable.Repeat((byte)0xa5, length).ToArray();
        byte[] header = length == 0
            ? [2, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0x41]
            : [2, 2, 0, 0, 16, 0, 254, 255, 255, 255, 0x41];
        Assert.Equal([.. header, .. body], protocol.Encode(id, 0x41, body));
        Assert.All(body, b => Assert.Equal(0xa5, b));
    }

    internal static void AssertNullStream(IProtocol protocol) =>
        Assert.Throws<ArgumentNullException>(() => protocol.Read(null!));

    internal static ReadFixture MaximumReadFixture()
    {
        var body = Enumerable.Repeat((byte)0xa5, BodyLimit).ToArray();
        return new("maximum", [2, 2, 0, 0, 16, 0, 0x84, 0, 1, 0, 0, 0, .. body], new(0x84, 0, 1, body));
    }

    internal static void AssertRead(IProtocol protocol, ReadFixture fixture, int fragment)
    {
        // Adjacent unsolicited message must remain unread, even when coalesced.
        using var stream = new FragmentedReadStream([.. fixture.Frame, .. LoadReply], fragment);
        var actual = protocol.Read(stream);
        Assert.Equal(fixture.Expected.Type, actual.Type);
        Assert.Equal(fixture.Expected.Error, actual.Error);
        Assert.Equal(fixture.Expected.RequestId, actual.RequestId);
        Assert.Equal(fixture.Expected.Body, actual.Body);
        Assert.Equal(fixture.Frame.Length, stream.Position);
    }

    internal static void AssertRejected(IProtocol protocol, RejectedFixture fixture)
    {
        using var stream = new FragmentedReadStream(fixture.Frame, 1);
        var error = Record.Exception(() => protocol.Read(stream));
        Assert.NotNull(error);
        Assert.Equal(fixture.ExceptionType, error.GetType());
        Assert.Equal(fixture.Consumed, stream.Position);
    }

    internal static void AssertAck(IProtocol protocol, string name)
    {
        var reply = new ObservedResponse(0x42, 0, 0x12345678, [0x69, 0x57]);
        var command = (byte)0x42;
        var length = 2;
        var expectedId = 0x12345678u;
        switch (name)
        {
            case "dump-ok": reply = reply with { Type = 0x41, Body = [] }; command = 0x41; length = 0; break;
            case "native-error-zero-type": reply = reply with { Type = 0, Error = 0x8f, Body = [] }; break;
            case "error-with-expected-type": reply = reply with { Error = 0x8f }; break;
            case "wrong-id": reply = reply with { RequestId = 7 }; break;
            case "unsolicited": reply = reply with { RequestId = uint.MaxValue }; break;
            case "wrong-type": reply = reply with { Type = 0x62 }; break;
            case "body-short": reply = reply with { Body = [0x69] }; break;
            case "body-long": reply = reply with { Body = [0x69, 0x57, 0] }; break;
            case "missing-body": reply = reply with { Body = [] }; break;
            case "null-body": reply = reply with { Body = null! }; break;
            case "reserved-expected-id": reply = reply with { RequestId = uint.MaxValue }; expectedId = uint.MaxValue; break;
            case "negative-expected-length": length = -1; break;
            case "oversize-expected-length": length = BodyLimit + 1; break;
        }
        if (name.Length == 8 && name.StartsWith("error-", StringComparison.Ordinal))
            reply = reply with { Error = Convert.ToByte(name[6..], 16) };
        if (name.EndsWith("-ok", StringComparison.Ordinal)) protocol.RequireSuccess(reply, expectedId, command, length);
        else Assert.Throws<InvalidDataException>(() => protocol.RequireSuccess(reply, expectedId, command, length));
    }

    internal static IEnumerable<ReadFixture> GoodFixtures()
    {
        yield return new("dump", [2, 2, 0, 0, 0, 0, 0x41, 0, 0x78, 0x56, 0x34, 0x12], new(0x41, 0, 0x12345678, []));
        yield return new("load", LoadReply, new(0x42, 0, 0x12345678, [0x69, 0x57]));
        yield return new("native-error", [2, 2, 0, 0, 0, 0, 0, 0x8f, 0x78, 0x56, 0x34, 0x12], new(0, 0x8f, 0x12345678, []));
        yield return new("unsolicited-stop", [2, 2, 2, 0, 0, 0, 0x62, 0, 255, 255, 255, 255, 0x69, 0x57], new(0x62, 0, uint.MaxValue, [0x69, 0x57]));
    }

    internal static IEnumerable<RejectedFixture> BadFixtures()
    {
        for (var length = 0; length < 12; length++)
            yield return new($"header-truncated-{length}", LoadReply[..length], typeof(EndOfStreamException), length);
        foreach (var count in new[] { 12, 13 })
            yield return new($"body-truncated-{count}", LoadReply[..count], typeof(EndOfStreamException), count);
        foreach (var (name, offset, value) in new[] { ("bad-stx", 0, 3), ("api-zero", 1, 0), ("api-one", 1, 1), ("api-three", 1, 3) })
        {
            var frame = LoadReply.ToArray(); frame[offset] = (byte)value;
            yield return new(name, frame, typeof(InvalidDataException), 12);
        }
        var overLimit = LoadReply.ToArray(); overLimit[2] = 1; overLimit[3] = 0; overLimit[4] = 16; overLimit[5] = 0;
        yield return new("body-over-limit", overLimit, typeof(InvalidDataException), 12);
        var max = LoadReply.ToArray(); Array.Fill(max, (byte)255, 2, 4);
        yield return new("body-uint-max", max, typeof(InvalidDataException), 12);
    }

    private static IProtocol ReaderMock(ReadFixture fixture, ObservedResponse result, int consume)
    {
        var mock = Substitute.For<IProtocol>();
        mock.Read(Arg.Any<Stream>()).Returns(call => { Consume(call.Arg<Stream>(), consume); return result; });
        return mock;
    }

    private static IProtocol RejectingMock(RejectedFixture fixture, Type exceptionType, int consume)
    {
        var mock = Substitute.For<IProtocol>();
        mock.Read(Arg.Any<Stream>()).Returns(call =>
        {
            Consume(call.Arg<Stream>(), consume);
            if (exceptionType == typeof(EndOfStreamException)) throw new EndOfStreamException(fixture.Name);
            if (exceptionType == typeof(InvalidDataException)) throw new InvalidDataException(fixture.Name);
            throw new IOException(fixture.Name);
        });
        return mock;
    }

    private static void Consume(Stream stream, int count)
    {
        for (var i = 0; i < count; i++) Assert.NotEqual(-1, stream.ReadByte());
    }

    internal sealed class FragmentedReadStream(byte[] bytes, int fragment) : MemoryStream(bytes, false)
    {
        public override int Read(byte[] buffer, int offset, int count) => base.Read(buffer, offset, Math.Min(fragment, count));
        public override int Read(Span<byte> buffer) => base.Read(buffer[..Math.Min(fragment, buffer.Length)]);
    }
}
