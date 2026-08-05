namespace ViceSharp.TestHarness.Xbox.Fakes;

using ViceSharp.Abstractions;

/// <summary>
/// PLAN-XBOXUWP S21 (IMPL-XBOXUWP-021). Off-console test double for
/// <see cref="IMachineKeyboardInput"/> returned by
/// <see cref="ViceSharp.Xbox.ViewModels.IEmulatorSessionFacade.GetKeyboardInput"/>.
/// Records the last <see cref="SetKeyState"/> call so the virtual-keyboard
/// ViewModel tests can assert key injection without a real machine.
/// </summary>
public sealed class FakeMachineKeyboardInput : IMachineKeyboardInput
{
    /// <inheritdoc />
    public DeviceId Id { get; } = new(0xF1);

    /// <inheritdoc />
    public string Name => "Fake Keyboard Input";

    /// <summary>The key passed to the most recent <see cref="SetKeyState"/> call.</summary>
    public string? LastKey { get; private set; }

    /// <summary>The pressed state passed to the most recent <see cref="SetKeyState"/> call.</summary>
    public bool LastPressed { get; private set; }

    /// <summary>Number of <see cref="SetKeyState"/> calls received.</summary>
    public int SetCount { get; private set; }

    /// <summary>The pressed state passed to the most recent <see cref="SetRestoreState"/> call.</summary>
    public bool LastRestorePressed { get; private set; }

    /// <summary>Number of <see cref="SetRestoreState"/> calls received.</summary>
    public int RestoreCount { get; private set; }

    /// <inheritdoc />
    public void Reset()
    {
        LastKey = null;
        LastPressed = false;
        SetCount = 0;
        LastRestorePressed = false;
        RestoreCount = 0;
    }

    /// <inheritdoc />
    public bool SetKeyState(string key, bool pressed)
    {
        LastKey = key;
        LastPressed = pressed;
        SetCount++;
        return true;
    }

    /// <inheritdoc />
    public bool SetRestoreState(bool pressed)
    {
        LastRestorePressed = pressed;
        RestoreCount++;
        return true;
    }
}
