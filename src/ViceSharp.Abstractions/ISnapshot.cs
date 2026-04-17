namespace ViceSharp.Abstractions;

/// <summary>
/// Complete serializable machine state snapshot.
/// </summary>
public interface ISnapshot
{
    /// <summary>
    /// Cycle number when this snapshot was taken.
    /// </summary>
    ulong Cycle { get; }

    /// <summary>
    /// Serialize snapshot to a byte span.
    /// </summary>
    void Serialize(Span<byte> destination);

    /// <summary>
    /// Deserialize snapshot from a byte span.
    /// </summary>
    void Deserialize(ReadOnlySpan<byte> source);

    /// <summary>
    /// Get required buffer size for serialization.
    /// </summary>
    int GetSerializedSize();
}