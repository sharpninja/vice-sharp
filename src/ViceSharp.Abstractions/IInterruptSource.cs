namespace ViceSharp.Abstractions;

/// <summary>
/// A device that can raise interrupt requests (IRQ) or non-maskable interrupts (NMI).
/// </summary>
public interface IInterruptSource : IDevice
{
    /// <summary>
    /// Current IRQ line state.
    /// </summary>
    bool IrqActive { get; }

    /// <summary>
    /// Current NMI line state.
    /// </summary>
    bool NmiActive { get; }

    /// <summary>
    /// Raised when IRQ line state changes.
    /// </summary>
    event Action<bool> IrqChanged;

    /// <summary>
    /// Raised when NMI line state changes.
    /// </summary>
    event Action<bool> NmiChanged;
}