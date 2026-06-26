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
    /// This CPU's own independent executed-cycle counter: the number of cycles it has
    /// actually executed since the last reset. The clock does not <see cref="IClockedDevice.Tick"/>
    /// the CPU on cycles that are stolen from it (e.g. VIC badlines), so this counts
    /// executed cycles only. Each CPU instance tracks its own value, which lets a rig with
    /// more than one CPU (host + each drive, or the C128's 8502 + Z80) measure each CPU's
    /// real-time rate against its own clock independently.
    /// </summary>
    long ExecutedCycles { get; }

    /// <summary>
    /// True when the CPU is positioned at the boundary before fetching the next instruction.
    /// </summary>
    bool IsInstructionBoundary => true;

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
