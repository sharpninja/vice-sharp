using ViceSharp.ViceCompatibility;
using static ViceSharp.VsfCompatibility.Tests.BinaryMonitorContractTests;

namespace ViceSharp.VsfCompatibility.Tests;

/// <summary>
/// The identical preimplementation mock-proven packet, stream and acknowledgement
/// assertions now execute the external codec. No native build or emulator is loaded.
/// Real snapshot acknowledgements and all bus/state parity gates remain separate.
/// </summary>
public sealed class BinaryMonitorImplementationTests
{
    private static readonly IProtocol Real = new ProductionAdapter();

    [Fact]
    public void Encoder_preserves_exact_request_and_input() => AssertEncode(Real);

    [Theory]
    [InlineData("reserved-id")]
    [InlineData("oversize-body")]
    [InlineData("null-body")]
    public void Encoder_rejects_every_mock_validated_invalid_input(string name) => AssertEncodeRejected(Real, name);

    [Theory]
    [InlineData(0, 0u)]
    [InlineData(BodyLimit, 0xfffffffeu)]
    public void Encoder_accepts_inclusive_mock_validated_boundaries(int length, uint id) => AssertEncodeBoundary(Real, length, id);

    [Theory]
    [MemberData(nameof(ReadCases), MemberType = typeof(BinaryMonitorContractTests))]
    public void Reader_preserves_fields_and_exact_frame_boundary(string name, int fragment) =>
        AssertRead(Real, GoodFixtures().Single(f => f.Name == name), fragment);

    [Fact]
    public void Reader_accepts_inclusive_body_limit() => AssertRead(Real, MaximumReadFixture(), 65536);

    [Fact]
    public void Reader_rejects_null_stream() => AssertNullStream(Real);

    [Theory]
    [MemberData(nameof(RejectedCases), MemberType = typeof(BinaryMonitorContractTests))]
    public void Reader_rejects_every_mock_validated_malformed_frame(string name) =>
        AssertRejected(Real, BadFixtures().Single(f => f.Name == name));

    [Theory]
    [MemberData(nameof(AckCases), MemberType = typeof(BinaryMonitorContractTests))]
    public void Success_guard_requires_exact_mock_validated_acknowledgement(string name) => AssertAck(Real, name);

    private sealed class ProductionAdapter : IProtocol
    {
        public byte[] Encode(uint requestId, byte command, byte[] body) =>
            BinaryMonitorProtocol.EncodeCommand(requestId, command, body);

        public ObservedResponse Read(Stream source)
        {
            var response = BinaryMonitorProtocol.ReadResponse(source);
            return new(response.Type, response.Error, response.RequestId, response.Body);
        }

        public void RequireSuccess(ObservedResponse response, uint requestId, byte command, int bodyLength) =>
            BinaryMonitorProtocol.RequireSuccess(new(response.Type, response.Error, response.RequestId, response.Body),
                requestId, command, bodyLength);
    }
}
