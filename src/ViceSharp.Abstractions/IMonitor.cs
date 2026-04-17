namespace ViceSharp.Abstractions;

/// <summary>
/// Debugger/monitor engine providing breakpoints, disassembly,
/// memory inspection, register manipulation, and execution control.
/// </summary>
public interface IMonitor
{
    /// <summary>Executes a monitor command and returns the result.</summary>
    string ExecuteCommand(string command);

    /// <summary>Gets the current CPU register state.</summary>
    RegisterSnapshot GetRegisters();

    /// <summary>True if execution is currently paused at a breakpoint.</summary>
    bool IsPaused { get; }
}

/// <summary>
/// CPU register state snapshot.
/// </summary>
public readonly struct RegisterSnapshot
{
    /// <summary>Accumulator register</summary>
    public byte A { get; init; }
    
    /// <summary>X index register</summary>
    public byte X { get; init; }
    
    /// <summary>Y index register</summary>
    public byte Y { get; init; }
    
    /// <summary>Stack pointer</summary>
    public byte S { get; init; }
    
    /// <summary>Processor status flags</summary>
    public byte P { get; init; }
    
    /// <summary>Program counter</summary>
    public ushort PC { get; init; }
}