namespace ViceSharp.Abstractions;

/// <summary>
/// Keyboard matrix surface scanned by machine I/O hardware.
/// </summary>
public interface IKeyboardMatrix : IDevice
{
    /// <summary>Sets a matrix key state by encoded row/column key code.</summary>
    void SetKey(byte keyCode, bool pressed);

    /// <summary>True when the RESTORE key is currently pressed.</summary>
    bool IsRestorePressed { get; }

    /// <summary>True when RUN/STOP is currently pressed.</summary>
    bool IsStopPressed { get; }

    /// <summary>True when SHIFT and Commodore are currently pressed together.</summary>
    bool IsShiftCbmPressed { get; }
}
