namespace ViceSharp.Abstractions;

/// <summary>
/// Published for each bus memory write (the byte that was there before the write, plus the
/// new byte) so a time-travel debugger can bundle the writes of each CPU instruction and
/// later reverse-apply them to reconstruct memory as it was at a past tick. An unmanaged
/// value type so it rides the zero-allocation <see cref="IPubSub"/> hot path; the bus only
/// publishes when at least one subscriber is listening (see
/// <see cref="IPubSub.SubscriptionCount"/>), so an unobserved run pays only a null/count
/// check per write.
/// </summary>
public readonly record struct MemoryWriteEvent(ushort Address, byte OldValue, byte NewValue)
{
    /// <summary>Pub/Sub topic used for bus memory-write notifications.</summary>
    public static readonly Topic Topic = Topic.FromName("memory.write");
}
