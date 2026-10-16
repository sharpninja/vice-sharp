namespace ViceSharp.TestHarness.Xbox.Fakes;

using ViceSharp.Abstractions;

/// <summary>
/// PLAN-XBOXUWP S21 (IMPL-XBOXUWP-021). Off-console test double for
/// <see cref="IMachineJoystickInput"/> returned by
/// <see cref="ViceSharp.Xbox.ViewModels.IEmulatorSessionFacade.GetJoystickInput"/>.
/// Records the last <see cref="SetJoystickState"/> call so the 10-foot ViewModel
/// tests can assert control-port injection without a real machine.
/// </summary>
public sealed class FakeMachineJoystickInput : IMachineJoystickInput
{
    /// <inheritdoc />
    public DeviceId Id { get; } = new(0xF0);

    /// <inheritdoc />
    public string Name => "Fake Joystick Input";

    /// <summary>The control port passed to the most recent <see cref="SetJoystickState"/> call.</summary>
    public int LastControlPort { get; private set; }

    /// <summary>The direction mask passed to the most recent <see cref="SetJoystickState"/> call.</summary>
    public byte LastDirectionMask { get; private set; }

    /// <summary>The fire-button state passed to the most recent <see cref="SetJoystickState"/> call.</summary>
    public bool LastFireButton { get; private set; }

    /// <summary>Number of <see cref="SetJoystickState"/> calls received.</summary>
    public int SetCount { get; private set; }

    /// <inheritdoc />
    public void Reset()
    {
        LastControlPort = 0;
        LastDirectionMask = 0;
        LastFireButton = false;
        SetCount = 0;
    }

    /// <inheritdoc />
    public bool SetJoystickState(int controlPort, byte directionMask, bool fireButton)
    {
        LastControlPort = controlPort;
        LastDirectionMask = directionMask;
        LastFireButton = fireButton;
        SetCount++;
        return true;
    }
}
