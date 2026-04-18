using ViceSharp.Abstractions;

namespace ViceSharp.Chips.Input;

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
    /// Read joystick state for CIA port
    /// </summary>
    public byte ReadPortState()
    {
        // C64 joystick is active low: 0 = pressed
        return (byte)((byte)~(byte)State & 0x1F);
    }

    public bool Up => (State & JoystickButtons.Up) != 0;
    public bool Down => (State & JoystickButtons.Down) != 0;
    public bool Left => (State & JoystickButtons.Left) != 0;
    public bool Right => (State & JoystickButtons.Right) != 0;
    public bool Fire => (State & JoystickButtons.Fire) != 0;
}