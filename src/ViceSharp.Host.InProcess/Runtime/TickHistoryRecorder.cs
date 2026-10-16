using System.Linq;
using ViceSharp.Abstractions;

namespace ViceSharp.Host.Runtime;

/// <summary>One memory write captured for time-travel: the address and the byte that was
/// there BEFORE the write (so it can be reverse-applied to undo the write).</summary>
public readonly record struct TickMemoryWrite(ushort Address, byte OldValue);

/// <summary>One captured tick: a completed CPU instruction, the memory write-deltas that
/// occurred during it, and a snapshot of each stateful chip's full state (empty when no
/// stateful chips are wired).</summary>
public sealed record TickRecord(CpuInstructionCompletedEvent Registers, TickMemoryWrite[] Writes, byte[] ChipState);

/// <summary>
/// BUG/FEAT TICKHIST: bounded ring of the last <see cref="Capacity"/> completed CPU
/// instructions, each bundled with the memory write-deltas observed during it. The
/// emulation worker thread calls <see cref="OnMemoryWrite"/> (per CPU bus write) and
/// <see cref="OnInstructionCompleted"/> (per instruction boundary) - both synchronous on
/// that single thread, so the pending-writes list is touched only there and needs no lock;
/// the UI/RPC thread reads via <see cref="Snapshot"/> under a lock that the worker also
/// takes only when finalizing a tick (not per write).
/// </summary>
public sealed class TickHistoryRecorder
{
    /// <summary>Number of most-recent instructions retained.</summary>
    public const int Capacity = 100;

    private static readonly byte[] EmptyChipState = [];

    private readonly TickRecord[] _ring = new TickRecord[Capacity];
    private readonly byte[][] _chipStateRing = new byte[Capacity][];
    private readonly List<TickMemoryWrite> _pending = new(16);
    private readonly object _gate = new();
    private IReadOnlyList<IStatefulDevice> _statefulDevices = [];
    private int _chipStateSize;
    private int _head;   // next slot to write
    private int _count;  // total appended (saturates conceptually; ring keeps last Capacity)

    /// <summary>Wire the chips whose full state is captured per tick (the order defines the
    /// layout in <see cref="TickRecord.ChipState"/>). Preallocates the per-slot capture
    /// buffers so the hot path never allocates. Call once before recording starts.</summary>
    public void SetStatefulDevices(IReadOnlyList<IStatefulDevice> devices)
    {
        ArgumentNullException.ThrowIfNull(devices);
        lock (_gate)
        {
            _statefulDevices = devices;
            _chipStateSize = devices.Sum(d => d.StateSize);
            for (var i = 0; i < Capacity; i++)
                _chipStateRing[i] = _chipStateSize == 0 ? EmptyChipState : new byte[_chipStateSize];
        }
    }

    /// <summary>Worker thread: record a CPU memory write (old byte) for the in-flight instruction.</summary>
    public void OnMemoryWrite(ushort address, byte oldValue)
        => _pending.Add(new TickMemoryWrite(address, oldValue));

    /// <summary>Worker thread: finalize the in-flight instruction into the ring with its
    /// bundled writes and a snapshot of each stateful chip, then reset the pending set.</summary>
    public void OnInstructionCompleted(CpuInstructionCompletedEvent registers)
    {
        var writes = _pending.Count == 0 ? [] : _pending.ToArray();

        // Capture chip state into this slot's preallocated buffer (zero-alloc).
        var chipState = _chipStateRing[_head] ?? EmptyChipState;
        if (_chipStateSize > 0 && chipState.Length == _chipStateSize)
        {
            var offset = 0;
            for (var i = 0; i < _statefulDevices.Count; i++)
            {
                var device = _statefulDevices[i];
                device.CaptureState(chipState.AsSpan(offset, device.StateSize));
                offset += device.StateSize;
            }
        }

        lock (_gate)
        {
            _ring[_head] = new TickRecord(registers, writes, chipState);
            _head = (_head + 1) % Capacity;
            _count++;
        }

        _pending.Clear();
    }

    /// <summary>Total instructions captured (across the ring's lifetime).</summary>
    public int Count
    {
        get { lock (_gate) return _count; }
    }

    /// <summary>Snapshot the retained ticks oldest-first. Lock-free for the worker's
    /// per-write path; only taken here and on tick finalize.</summary>
    public IReadOnlyList<TickRecord> Snapshot()
    {
        lock (_gate)
        {
            var size = Math.Min(_count, Capacity);
            var result = new TickRecord[size];
            // The oldest retained entry is `size` slots behind _head (mod Capacity).
            var start = ((_head - size) % Capacity + Capacity) % Capacity;
            for (var i = 0; i < size; i++)
            {
                var record = _ring[(start + i) % Capacity];
                // Deep-copy the chip-state blob: the ring buffers are reused on rotation.
                result[i] = record.ChipState.Length == 0
                    ? record
                    : record with { ChipState = (byte[])record.ChipState.Clone() };
            }

            return result;
        }
    }

    /// <summary>Drop all captured history (e.g. on reset).</summary>
    public void Clear()
    {
        lock (_gate)
        {
            Array.Clear(_ring);
            _head = 0;
            _count = 0;
        }

        _pending.Clear();
    }
}

/// <summary>
/// Reconstructs RAM as it was at a past captured tick by reverse-applying the write-deltas
/// of every later tick onto a copy of the current (paused) memory image.
/// </summary>
public static class TickHistoryReconstruction
{
    /// <summary>
    /// Mutates <paramref name="image"/> (the current memory, 64 KiB) in place so it reflects
    /// memory as it was immediately after <c>ticks[targetIndex]</c> completed: for every tick
    /// newer than the target, undo its writes (newest first, and in reverse order within a
    /// tick) by restoring each address's old byte.
    /// </summary>
    public static void ReconstructInto(Span<byte> image, IReadOnlyList<TickRecord> ticks, int targetIndex)
    {
        ArgumentNullException.ThrowIfNull(ticks);
        if (targetIndex < 0 || targetIndex >= ticks.Count)
            throw new ArgumentOutOfRangeException(nameof(targetIndex));

        for (var i = ticks.Count - 1; i > targetIndex; i--)
        {
            var writes = ticks[i].Writes;
            for (var w = writes.Length - 1; w >= 0; w--)
                image[writes[w].Address] = writes[w].OldValue;
        }
    }
}
