using System.Buffers.Binary;
using ViceSharp.Abstractions;

namespace ViceSharp.Core.Snapshots;

public sealed class RuntimeSnapshot : ISnapshot
{
    private const int HeaderSize = 16;
    private readonly byte[] _memory;

    public RuntimeSnapshot(MachineState state, ReadOnlySpan<byte> memory)
    {
        if (memory.Length != ushort.MaxValue + 1)
            throw new ArgumentOutOfRangeException(nameof(memory), "Runtime snapshots require a complete 64K memory image.");

        State = state;
        _memory = memory.ToArray();
    }

    public RuntimeSnapshot()
    {
        State = default;
        _memory = new byte[ushort.MaxValue + 1];
    }

    public MachineState State { get; private set; }

    public ReadOnlyMemory<byte> Memory => _memory;

    public ulong Cycle => (ulong)State.Cycle;

    public void Serialize(Span<byte> destination)
    {
        if (destination.Length < GetSerializedSize())
            throw new ArgumentException("Destination is smaller than the serialized snapshot size.", nameof(destination));

        destination[0] = State.A;
        destination[1] = State.X;
        destination[2] = State.Y;
        destination[3] = State.S;
        destination[4] = State.P;
        BinaryPrimitives.WriteUInt16LittleEndian(destination[5..7], State.PC);
        BinaryPrimitives.WriteInt64LittleEndian(destination[7..15], State.Cycle);
        destination[15] = 0;
        _memory.CopyTo(destination[HeaderSize..]);
    }

    public void Deserialize(ReadOnlySpan<byte> source)
    {
        if (source.Length < GetSerializedSize())
            throw new ArgumentException("Source is smaller than the serialized snapshot size.", nameof(source));

        State = new MachineState
        {
            A = source[0],
            X = source[1],
            Y = source[2],
            S = source[3],
            P = source[4],
            PC = BinaryPrimitives.ReadUInt16LittleEndian(source[5..7]),
            Cycle = BinaryPrimitives.ReadInt64LittleEndian(source[7..15])
        };

        source[HeaderSize..(HeaderSize + _memory.Length)].CopyTo(_memory);
    }

    public int GetSerializedSize() => HeaderSize + _memory.Length;
}
