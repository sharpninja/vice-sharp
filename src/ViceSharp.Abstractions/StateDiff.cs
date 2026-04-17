namespace ViceSharp.Abstractions;

/// <summary>
/// Result of state comparison between managed and native implementations
/// </summary>
public readonly struct StateDiff
{
    public long Cycle { get; init; }
    public MachineState Expected { get; init; }
    public MachineState Actual { get; init; }
}