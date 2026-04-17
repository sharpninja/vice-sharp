namespace ViceSharp.Abstractions;

/// <summary>
/// Top-level container for a running emulator instance. Manages machine
/// lifecycle and provides access to system-wide services like ROM loading,
/// media capture, and the monitor/debugger.
/// </summary>
public interface ISystem
{
    /// <summary>The currently loaded machine.</summary>
    IMachine Machine { get; }

    /// <summary>Starts the emulation loop.</summary>
    void Start();

    /// <summary>Stops the emulation loop, preserving state.</summary>
    void Stop();

    /// <summary>Performs a hardware reset (equivalent to power-cycle).</summary>
    void Reset();

    /// <summary>True if the emulation loop is currently running.</summary>
    bool IsRunning { get; }
}