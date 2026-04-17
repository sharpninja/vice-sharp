namespace ViceSharp.Abstractions;

/// <summary>
/// Represents a physical interrupt line (IRQ or NMI) with open-drain
/// semantics. The line is active-low: it is asserted when any source
/// pulls it low, and deasserted only when all sources release.
/// </summary>
public interface IInterruptLine
{
    /// <summary>Assert (pull low) the interrupt line from the given source.</summary>
    void Assert(IInterruptSource source);

    /// <summary>Deassert (release) the interrupt line from the given source.</summary>
    void Release(IInterruptSource source);

    /// <summary>True when at least one source is asserting the line.</summary>
    bool IsAsserted { get; }

    /// <summary>The type of interrupt this line carries.</summary>
    InterruptType Type { get; }
}

/// <summary>
/// Type of interrupt.
/// </summary>
public enum InterruptType
{
    /// <summary>Maskable Interrupt Request</summary>
    Irq = 1,
    
    /// <summary>Non-Maskable Interrupt</summary>
    Nmi = 2,
    
    /// <summary>Reset line</summary>
    Reset = 3
}