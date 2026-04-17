namespace ViceSharp.Abstractions;

/// <summary>
/// Auditable queue for all state changes in the system.
/// </summary>
public interface IMutationQueue
{
    /// <summary>
    /// Record a state mutation.
    /// </summary>
    void Enqueue(DeviceId source, ushort address, byte oldValue, byte newValue, ulong cycle);

    /// <summary>
    /// Commit current buffer and swap to next buffer.
    /// </summary>
    void Commit();

    /// <summary>
    /// Reset the queue to empty state.
    /// </summary>
    void Clear();
}

/// <summary>
/// Single mutation entry representing a state change.
/// </summary>
public readonly struct MutationEntry
{
    public DeviceId Source { get; }
    public ushort Address { get; }
    public byte OldValue { get; }
    public byte NewValue { get; }
    public ulong Cycle { get; }

    public MutationEntry(DeviceId source, ushort address, byte oldValue, byte newValue, ulong cycle)
    {
        Source = source;
        Address = address;
        OldValue = oldValue;
        NewValue = newValue;
        Cycle = cycle;
    }
}