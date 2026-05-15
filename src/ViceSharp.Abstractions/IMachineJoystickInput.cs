namespace ViceSharp.Abstractions;

/// <summary>
/// Machine-owned joystick/control-port input surface.
/// </summary>
public interface IMachineJoystickInput : IDevice
{
    /// <summary>
    /// Applies a host joystick state to a machine-specific control port.
    /// </summary>
    bool SetJoystickState(int controlPort, byte directionMask, bool fireButton);
}
