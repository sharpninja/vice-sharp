using ViceSharp.Abstractions;

namespace ViceSharp.Core;

public sealed class DoubleBufferedMutationQueue : IMutationQueue
{
    private readonly List<MutationEntry> _buffer0 = new();
    private readonly List<MutationEntry> _buffer1 = new();
    private int _activeBuffer;

    public void Enqueue(DeviceId source, ushort address, byte oldValue, byte newValue, ulong cycle)
    {
        var buffer = _activeBuffer == 0 ? _buffer0 : _buffer1;
        buffer.Add(new MutationEntry(source, address, oldValue, newValue, cycle));
    }

    public void Commit()
    {
        var buffer = _activeBuffer == 0 ? _buffer0 : _buffer1;
        buffer.Clear();
        _activeBuffer ^= 1;
    }

    public void Clear()
    {
        _buffer0.Clear();
        _buffer1.Clear();
        _activeBuffer = 0;
    }
}