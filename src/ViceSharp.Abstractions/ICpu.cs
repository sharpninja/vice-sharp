namespace ViceSharp.Abstractions;

/// <summary>
/// Common interface for all CPU implementations.
/// </summary>
public interface ICpu : IClockedDevice
{
    /// <summary>
    /// Program Counter register.
    /// </summary>
    ushort PC { get; set; }

    /// <summary>
    /// Status register flags.
    /// </summary>
    byte Flags { get; set; }

    /// <summary>
    /// Execute single instruction at current PC.
    /// </summary>
    /// <returns>Number of cycles consumed</returns>
    int ExecuteInstruction();

    /// <summary>
    /// Trigger hardware interrupt
    /// </summary>
    void Irq();

    /// <summary>
    /// Trigger non-maskable interrupt
    /// </summary>
    void Nmi();
}

/// <summary>
/// CPU Status register flags
/// </summary>
[Flags]
public enum CpuFlags : byte
{
    Carry = 1 << 0,
    Zero = 1 << 1,
    InterruptDisable = 1 << 2,
    Decimal = 1 << 3,
    Break = 1 << 4,
    Unused = 1 << 5,
    Overflow = 1 << 6,
    Negative = 1 << 7
}
