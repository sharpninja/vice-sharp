namespace ViceSharp.TestHarness;

using ViceSharp.Core.Input;
using Xunit;

/// <summary>
/// FR: FR-INP-006 (joystick port state).
/// TR: TR-INPUT-JOY-001.
/// Use case: The passive C64 joystick port model must expose active-low
/// CIA port state for the owning C64 machine/input host.
/// </summary>
public sealed class C64JoystickPortTests
{
    /// <summary>
    /// FR: FR-INP-006, TR: TR-INPUT-JOY-001.
    /// Use case: A newly constructed passive C64 joystick port starts
    /// connected and idle.
    /// Acceptance: IsConnected is true and ReadPortState reports the low
    /// five input bits high.
    /// </summary>
    [Fact]
    public void NewPort_IsConnectedAndIdle()
    {
        var port = new C64JoystickPort();

        Assert.True(port.IsConnected);
        Assert.Equal(0x1F, port.ReadPortState() & 0x1F);
    }

    /// <summary>
    /// FR: FR-INP-006, TR: TR-INPUT-JOY-001.
    /// Use case: Setting the Fire button state on the passive port must be
    /// immediately visible in the active-low CIA port read.
    /// Acceptance: Setting Fire state and calling ReadPortState returns a
    /// value where bit 4 is 0 (active-low, pressed); clearing Fire returns
    /// bit 4 = 1 (released).
    /// </summary>
    [Fact]
    public void FireState_IsReflectedInPortRead()
    {
        var port = new C64JoystickPort();

        port.State = C64JoystickPort.JoystickButtons.Fire;
        var portValue = port.ReadPortState();

        // Active-low: Fire bit (bit 4) should be 0 when pressed.
        Assert.Equal(0, portValue & 0x10);
        port.State = C64JoystickPort.JoystickButtons.None;
        Assert.Equal(0x10, port.ReadPortState() & 0x10);
    }
}
