namespace ViceSharp.Abstractions;

/// <summary>
/// Machine-owned host keyboard input surface.
/// </summary>
public interface IMachineKeyboardInput : IDevice
{
    /// <summary>
    /// Applies a host key state to the machine-specific keyboard implementation.
    /// </summary>
    bool SetKeyState(string key, bool pressed);

    /// <summary>
    /// Sets the state of the machine's RESTORE line. RESTORE is not part of the key
    /// matrix: on real C64 hardware it is wired directly to the CPU NMI line (through a
    /// monostable), so it triggers a hardware non-maskable interrupt rather than an
    /// ordinary key event. Implementations must route this to the dedicated RESTORE/NMI
    /// trigger and never through <see cref="SetKeyState(string, bool)"/>.
    /// </summary>
    /// <param name="pressed"><c>true</c> to assert RESTORE (press), <c>false</c> to release.</param>
    /// <returns><c>true</c> when the machine applied the RESTORE state; <c>false</c> when the
    /// machine has no keyboard (for example a keyboard-disabled profile).</returns>
    bool SetRestoreState(bool pressed);
}
