namespace ViceSharp.Abstractions;

/// <summary>
/// Abstracts host input devices into emulated input events.
/// Translates keyboard, gamepad, and mouse input from the host platform
/// into Commodore-compatible input events.
/// </summary>
public interface IInputSource
{
    /// <summary>Polls for new input events since the last call.</summary>
    void Poll();

    /// <summary>True if this input source is currently connected/active.</summary>
    bool IsConnected { get; }
}