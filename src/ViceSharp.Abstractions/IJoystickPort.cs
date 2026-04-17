namespace ViceSharp.Abstractions;

/// <summary>
/// Joystick port input interface.
/// </summary>
public interface IJoystickPort
{
    /// <summary>Joystick direction mask</summary>
    byte Direction { get; set; }
    
    /// <summary>Fire button state</summary>
    bool FireButton { get; set; }
}