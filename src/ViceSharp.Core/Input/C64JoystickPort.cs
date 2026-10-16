using ViceSharp.Abstractions;

namespace ViceSharp.Core.Input;

/// <summary>
/// C64 Digital Joystick Port implementation.
/// </summary>
/// <summary>
/// FR-INP-003. A mouse port accepts host pointer deltas.
/// </summary>
public interface IMousePort
{
    /// <summary>Apply host pointer deltas to the pot position.</summary>
    void ApplyDelta(int deltaX, int deltaY);
}

public sealed class C64JoystickPort : IInputSource, IMousePort
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
        _mouseMode = false;
        _mouseX = 0;
        _mouseY = 0;
        _potX = 255;
        _potY = 255;
    }

    /// <summary>
    /// Read joystick state for CIA port (active low, VICE-style)
    /// </summary>
    public byte ReadPortState()
    {
        // C64 joystick is active low: 0 = pressed
        return (byte)(0xE0 | ((byte)~(byte)State & 0x1F));
    }
    
    // VICE-style: POT X/Y for paddles (255 = not pressed).
    // Mouse mode keeps a position counter. The pot byte is the 1351 code
    // VICE writes: (position & 0x7f) + 0x40. Adding the delta to the pot
    // byte clamps at 255 and never leaves the idle paddle value.
    private byte _potX = 255;
    private byte _potY = 255;
    private int _mouseX;
    private int _mouseY;
    private bool _mouseMode;
    
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

    /// <inheritdoc />
    public void ApplyDelta(int deltaX, int deltaY)
    {
        if (!_mouseMode)
        {
            _mouseX = 0;
            _mouseY = 0;
            _mouseMode = true;
        }

        _mouseX += deltaX;
        _mouseY += deltaY;
        _potX = (byte)((_mouseX & 0x7F) + 0x40);
        _potY = (byte)((_mouseY & 0x7F) + 0x40);
    }
    
    public bool Up => (State & JoystickButtons.Up) != 0;
    public bool Down => (State & JoystickButtons.Down) != 0;
    public bool Left => (State & JoystickButtons.Left) != 0;
    public bool Right => (State & JoystickButtons.Right) != 0;
    public bool Fire => (State & JoystickButtons.Fire) != 0;
}
