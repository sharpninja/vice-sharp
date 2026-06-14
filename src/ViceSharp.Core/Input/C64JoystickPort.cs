using ViceSharp.Abstractions;

namespace ViceSharp.Core.Input;

/// <summary>
/// C64 Digital Joystick Port implementation.
/// </summary>
public sealed class C64JoystickPort : IInputSource
{
    [Flags]
    public enum JoystickButtons : byte
    {
        None = 0x00,
        Up = 0x01,
        Down = 0x02,
        Left = 0x04,
        Right = 0x08,
        Fire = 0x10
    }

    public DeviceId Id => new DeviceId(0x0009);
    public string Name => "C64 Joystick Port";
    public bool IsConnected => true;

    public JoystickButtons State { get; set; }

    /// <inheritdoc />
    public void Poll()
    {
    }

    /// <inheritdoc />
    public void Initialize()
    {
        Reset();
    }

    /// <inheritdoc />
    public void Reset()
    {
        State = JoystickButtons.None;
    }

    /// <summary>
    /// Read joystick state for CIA port (active low, VICE-style)
    /// </summary>
    public byte ReadPortState()
    {
        // C64 joystick is active low: 0 = pressed
        return (byte)(0xE0 | ((byte)~(byte)State & 0x1F));
    }
    
    // VICE-style: POT X/Y for paddles (255 = not pressed)
    private byte _potX = 255;
    private byte _potY = 255;
    
    /// <summary>
    /// Read POT X value (paddle 1, VICE-style)
    /// </summary>
    public byte ReadPotX() => _potX;
    
    /// <summary>
    /// Read POT Y value (paddle 2, VICE-style)
    /// </summary>
    public byte ReadPotY() => _potY;
    
    /// <summary>
    /// Set POT X value (0-255, VICE-style)
    /// </summary>
    public void SetPotX(byte value) => _potX = value;
    
    /// <summary>
    /// Set POT Y value (0-255, VICE-style)
    /// </summary>
    public void SetPotY(byte value) => _potY = value;
    
    public bool Up => (State & JoystickButtons.Up) != 0;
    public bool Down => (State & JoystickButtons.Down) != 0;
    public bool Left => (State & JoystickButtons.Left) != 0;
    public bool Right => (State & JoystickButtons.Right) != 0;
    public bool Fire => (State & JoystickButtons.Fire) != 0;
}
