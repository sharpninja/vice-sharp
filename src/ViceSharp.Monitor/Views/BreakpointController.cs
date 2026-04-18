using ViceSharp.Abstractions;

namespace ViceSharp.Monitor.Views;

public sealed class BreakpointController
{
    private readonly HashSet<ushort> _breakpoints = new HashSet<ushort>();

    public event EventHandler<ushort>? BreakpointHit;

    public IReadOnlyCollection<ushort> Breakpoints => _breakpoints;

    public void Add(ushort address)
    {
        _breakpoints.Add(address);
    }

    public void Remove(ushort address)
    {
        _breakpoints.Remove(address);
    }

    public void Clear()
    {
        _breakpoints.Clear();
    }

    public bool IsBreakpoint(ushort address)
    {
        return _breakpoints.Contains(address);
    }

    public void CheckBreakpoint(ushort pc)
    {
        if (_breakpoints.Contains(pc))
        {
            BreakpointHit?.Invoke(this, pc);
        }
    }
}