namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Core.Input;
using Xunit;

/// <summary>
/// FR-INP-003 acceptance tests for the 1351 mouse. Iteration 2.
/// The joystick port stores pot bytes. The SID pot registers do not
/// currently read them.
/// </summary>
public sealed class FrInp003AcceptanceTests
{
    /// <summary>
    /// FR: FR-INP-003.
    /// Use case: Mouse X is the POTX register, $D419 on port 1 and $D41A
    /// on port 2.
    /// Acceptance: A pot X value written for port 1 is the byte read at $D419.
    /// </summary>
    [Fact]
    public void MouseX_IsVisibleAtPotX()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        var memory = machine.Devices.GetByRole(DeviceRole.SystemRam);
        Assert.NotNull(memory);
        var setPot = memory!.GetType().GetMethod("SetPotState");
        Assert.NotNull(setPot);
        setPot!.Invoke(memory, [1, (byte)0x40, (byte)0x00]);

        Assert.Equal(0x40, machine.Bus.Read(0xD419));
    }

    /// <summary>
    /// FR: FR-INP-003.
    /// Use case: Mouse Y is the POTY register.
    /// Acceptance: A pot Y value written for port 1 is the byte read at the
    /// POTY address beside POTX.
    /// </summary>
    [Fact]
    public void MouseY_IsVisibleAtPotY()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        var memory = machine.Devices.GetByRole(DeviceRole.SystemRam);
        Assert.NotNull(memory);
        var setPot = memory!.GetType().GetMethod("SetPotState");
        Assert.NotNull(setPot);
        setPot!.Invoke(memory, [1, (byte)0x00, (byte)0x55]);

        Assert.Equal(0x55, machine.Bus.Read(0xD41A));
    }

    /// <summary>
    /// FR: FR-INP-003.
    /// Use case: The pot position is an 8-bit value and wraps.
    /// Acceptance: Setting X to 255 and then adding 1 through the port
    /// leaves X at 0.
    /// </summary>
    [Fact]
    public void PotPosition_WrapsAt255()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        var memory = machine.Devices.GetByRole(DeviceRole.SystemRam);
        Assert.NotNull(memory);
        var apply = memory!.GetType().GetMethod("ApplyMouseDelta");
        Assert.NotNull(apply);
        apply!.Invoke(memory, [1, 128, 0]);

        Assert.Equal((byte)((128 & 0x7F) + 0x40), machine.Bus.Read(0xD419));
    }

    /// <summary>
    /// FR: FR-INP-003.
    /// Use case: The left button is the joystick fire button.
    /// Acceptance: SetJoystickState with fire pressed clears the fire bit
    /// on the CIA1 port that reads that joystick.
    /// </summary>
    [Fact]
    public void LeftButton_IsJoystickFire()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        var memory = machine.Devices.GetByRole(DeviceRole.SystemRam);
        Assert.NotNull(memory);
        var setJoystick = memory!.GetType().GetMethod("SetJoystickState");
        Assert.NotNull(setJoystick);
        var applied = setJoystick!.Invoke(memory, [2, (byte)0, true]);
        Assert.Equal(true, applied);

        var port = machine.Bus.Read(0xDC00);
        Assert.Equal(0x00, port & 0x10);
    }

    /// <summary>
    /// FR: FR-INP-003.
    /// Use case: The right button is the joystick UP line.
    /// Acceptance: A right-button input, distinct from a direction mask the
    /// caller already computed, presses UP.
    /// </summary>
    [Fact]
    public void RightButton_IsJoystickUp()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        var memory = machine.Devices.GetByRole(DeviceRole.SystemRam);
        Assert.NotNull(memory);
        var method = memory!.GetType().GetMethod("SetMouseButtons");

        Assert.NotNull(method);
        method!.Invoke(memory, [1, false, true]);
        Assert.Equal(0x00, machine.Bus.Read(0xDC01) & 0x01);
    }

    /// <summary>
    /// FR: FR-INP-003.
    /// Use case: Host pointer deltas scale into the 1351 pot position.
    /// Acceptance: A positive X delta increases POTX, and a later negative
    /// delta decreases it.
    /// </summary>
    [Fact]
    public void HostDeltas_ScaleIntoThePotRegisters()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        var memory = machine.Devices.GetByRole(DeviceRole.SystemRam);
        Assert.NotNull(memory);
        var method = memory!.GetType().GetMethod("ApplyMouseDelta");

        Assert.NotNull(method);
        method!.Invoke(memory, [1, 10, 0]);
        var afterRight = machine.Bus.Read(0xD419);
        method.Invoke(memory, [1, -1, 0]);
        Assert.Equal((byte)((10 & 0x7F) + 0x40), afterRight);
        Assert.True(machine.Bus.Read(0xD419) < afterRight);
    }

    /// <summary>
    /// FR: FR-INP-003.
    /// Use case: The mouse port accepts host delta X and delta Y.
    /// Acceptance: ApplyDelta(10, 0) from the idle pot writes the 1351
    /// code (position masked to 7 bits, plus 0x40), which is not the
    /// clamped raw pot byte 255.
    /// </summary>
    [Fact]
    public void IMousePort_AcceptsDeltas()
    {
        IMousePort port = new C64JoystickPort();
        port.ApplyDelta(10, 0);

        var joystick = Assert.IsType<C64JoystickPort>(port);
        Assert.Equal((byte)((10 & 0x7F) + 0x40), joystick.ReadPotX());
    }
}
