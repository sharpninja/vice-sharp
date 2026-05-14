namespace ViceSharp.Abstractions;

/// <summary>
/// Machine-owned host keyboard input surface.
/// </summary>
public interface IMachineKeyboardInput : IDevice
{
    /// <summary>
    /// Applies a host key state to the machine-specific keyboard implementation.
    /// </summary>
    bool SetKeyState(string key, bool pressed);
}
