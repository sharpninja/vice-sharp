using System.Buffers.Binary;

namespace ViceSharp.ViceCompatibility;

/// <summary>
/// One complete response from the pinned VICE binary monitor API 2. A decoded
/// response is not evidence of success until <see cref="BinaryMonitorProtocol.RequireSuccess"/>
/// validates its correlation and native result. Event IDs and error codes are retained.
/// </summary>
public sealed record BinaryMonitorResponse(byte Type, byte Error, uint RequestId, byte[] Body);

/// <summary>
/// External public-interface codec for FR-VICE-001 AC4, TR-VICE-ORACLE-001 AC3/AC5
/// and TEST-VICE-001 AC4. Pinned upstream cc67418cba58e71d8e70fd353e9de47199ad2075:
/// vice/doc/vice.texi binary command/response structures and monitor_binary.c.
/// Does not start, modify, patch or infer observations from VICE.
/// </summary>
public static class BinaryMonitorProtocol
{
    /// <summary>Fail-closed allocation bound for this external wrapper.</summary>
    public const int MaximumBodyLength = 1024 * 1024;

    /// <summary>
    /// Creates one owned API-2 command frame. The caller owns command-specific
    /// payload semantics; 0xffffffff is reserved for unsolicited native events.
    /// </summary>
    public static byte[] EncodeCommand(uint requestId, byte command, byte[] body)
    {
        ArgumentNullException.ThrowIfNull(body);
        if (requestId == uint.MaxValue) throw new ArgumentOutOfRangeException(nameof(requestId));
        if (body.Length > MaximumBodyLength) throw new ArgumentOutOfRangeException(nameof(body));
        var frame = new byte[11 + body.Length];
        frame[0] = 2;
        frame[1] = 2;
        BinaryPrimitives.WriteUInt32LittleEndian(frame.AsSpan(2, 4), (uint)body.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(frame.AsSpan(6, 4), requestId);
        frame[10] = command;
        body.CopyTo(frame, 11);
        return frame;
    }

    /// <summary>
    /// Reads exactly one API-2 response, including fragmented reads. Never consumes
    /// the next frame. Invalid headers/oversize bodies fail before body allocation.
    /// The stream owner must set a transport timeout; EOF and transport errors propagate.
    /// </summary>
    public static BinaryMonitorResponse ReadResponse(Stream source)
    {
        ArgumentNullException.ThrowIfNull(source);
        Span<byte> header = stackalloc byte[12];
        source.ReadExactly(header);
        if (header[0] != 2 || header[1] != 2)
            throw new InvalidDataException("Expected VICE binary monitor STX and API version 2.");
        var length = BinaryPrimitives.ReadUInt32LittleEndian(header.Slice(2, 4));
        if (length > MaximumBodyLength)
            throw new InvalidDataException("VICE response exceeds the wrapper body limit.");
        var body = new byte[(int)length];
        source.ReadExactly(body);
        return new BinaryMonitorResponse(header[6], header[7],
            BinaryPrimitives.ReadUInt32LittleEndian(header.Slice(8, 4)), body);
    }

    /// <summary>
    /// Requires an explicitly successful response to the exact request. Native
    /// error packets (including type zero), events, mismatches and absent/truncated
    /// payloads cannot satisfy this gate. No retry or inference occurs here.
    /// </summary>
    public static void RequireSuccess(BinaryMonitorResponse response, uint requestId, byte command, int bodyLength)
    {
        ArgumentNullException.ThrowIfNull(response);
        if (requestId == uint.MaxValue || response.RequestId != requestId ||
            response.Error != 0 || response.Type != command ||
            bodyLength < 0 || bodyLength > MaximumBodyLength ||
            response.Body is null || response.Body.Length != bodyLength)
        {
            throw new InvalidDataException(
                $"VICE acknowledgement mismatch/failure: request={response.RequestId:X8}, " +
                $"type={response.Type:X2}, error={response.Error:X2}.");
        }
    }
}
