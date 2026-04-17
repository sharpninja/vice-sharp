namespace ViceSharp.Abstractions;

/// <summary>
/// Marker interface for state objects that can be compared for equality
/// </summary>
public interface IStateComparable
{
}

public static class StateComparisonExtensions
{
    public static StateDiff CompareTo(this MachineState expected, MachineState actual)
    {
        return new StateDiff
        {
            Cycle = expected.Cycle,
            Expected = expected,
            Actual = actual
        };
    }
}