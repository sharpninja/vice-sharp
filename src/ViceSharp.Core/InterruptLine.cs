using ViceSharp.Abstractions;

namespace ViceSharp.Core;

public sealed class InterruptLine : IInterruptLine
{
    private readonly HashSet<DeviceId> _sources = new();

    public bool IsAsserted => _sources.Count > 0;
    public InterruptType Type { get; }

    public InterruptLine(InterruptType type)
    {
        Type = type;
    }

    public void Assert(IInterruptSource source) => _sources.Add(source.SourceId);
    public void Release(IInterruptSource source) => _sources.Remove(source.SourceId);
    public void Clear() => _sources.Clear();
}
