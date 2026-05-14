namespace ViceSharp.Abstractions;

/// <summary>
/// Machine keyboard input surface that can switch host-key maps.
/// </summary>
public interface IKeyboardInputMapSelection : IDevice
{
    /// <summary>The active host-key map.</summary>
    IKeyboardInputMap KeyboardMap { get; }

    /// <summary>Selects the active host-key map.</summary>
    void SelectKeyboardMap(IKeyboardInputMap keyboardMap);
}
