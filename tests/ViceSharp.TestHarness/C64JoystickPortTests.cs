namespace ViceSharp.TestHarness;

using ViceSharp.Chips.Input;
using Xunit;

/// <summary>
/// FR: FR-INP-006 (joystick port enumeration).
/// TR: TR-INPUT-JOY-001.
/// Use case: The emulator must expose at least one joystick device so
/// host UI can discover and bind joystick inputs without requiring
/// physical hardware. The stub device must respond correctly to Fire
/// state changes and ReadPortState().
/// </summary>
public sealed class C64JoystickPortTests
{
    /// <summary>
    /// FR: FR-INP-006, TR: TR-INPUT-JOY-001.
    /// Use case: The host layer must be able to discover available joystick
    /// devices without enumerating physical hardware at runtime.
    /// Acceptance: EnumerateDevices returns a non-null list with at least
    /// one entry representing the always-present keyboard-mapped stub device.
    /// </summary>
    [Fact]
    public void EnumerateDevices_ReturnsAtLeastOneDevice()
    {
        var devices = C64JoystickPort.EnumerateDevices();
        Assert.NotNull(devices);
        Assert.True(devices.Count >= 1, "EnumerateDevices must return at least one device.");
    }

    /// <summary>
    /// FR: FR-INP-006, TR: TR-INPUT-JOY-001.
    /// Use case: The first enumerated joystick device must be available for
    /// input binding immediately after enumeration, without any additional
    /// hardware initialization step.
    /// Acceptance: The first returned device reports IsConnected = true.
    /// </summary>
    [Fact]
    public void EnumerateDevices_FirstDevice_IsConnected()
    {
        var devices = C64JoystickPort.EnumerateDevices();
        Assert.True(devices[0].IsConnected);
    }

    /// <summary>
    /// FR: FR-INP-006, TR: TR-INPUT-JOY-001.
    /// Use case: Setting the Fire button state on an enumerated joystick
    /// device must be immediately visible in the active-low CIA port read.
    /// Acceptance: Setting Fire state and calling ReadPortState returns a
    /// value where bit 4 is 0 (active-low, pressed); clearing Fire returns
    /// bit 4 = 1 (released).
    /// </summary>
    [Fact]
    public void EnumerateDevices_FirstDevice_FireState_ReflectedInPortRead()
    {
        var devices = C64JoystickPort.EnumerateDevices();
        var port = devices[0];
        port.State = C64JoystickPort.JoystickButtons.Fire;
        var portValue = port.ReadPortState();
        // Active-low: Fire bit (bit 4) should be 0 when pressed.
        Assert.Equal(0, portValue & 0x10);
        port.State = C64JoystickPort.JoystickButtons.None;
        Assert.Equal(0x10, port.ReadPortState() & 0x10);
    }
}
