using ViceSharp.Abstractions;

namespace ViceSharp.Core;

public sealed class InterruptLine : IInterruptLine
{
    private int _assertCount;

    public bool IsAsserted => _assertCount > 0;
    public InterruptType Type { get; }

    public InterruptLine(InterruptType type)
    {
        Type = type;
    }

    public void Assert(IInterruptSource source) => _assertCount++;
    public void Release(IInterruptSource source) => _assertCount = Math.Max(0, _assertCount - 1);
    public void Clear() => _assertCount = 0;
}
