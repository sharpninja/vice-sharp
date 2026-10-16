namespace ViceSharp.Chips.Cia;

/// <summary>
/// FIX-XSNAPWARP-001: the CIA state a machine snapshot round-trips, exactly the fields
/// <see cref="Mos6526.InjectSnapshotState"/> consumes (TR-LOCKSTEP-VSF-001 shape: port
/// output latches and directions, LIVE timer counters with reload latches, control
/// registers with the live run bit, the latched interrupt flags, and the ICR enable
/// mask). Value equality makes capture-inject round-trip assertions exact.
/// </summary>
/// <param name="PortA">Port A output latch.</param>
/// <param name="PortB">Port B output latch.</param>
/// <param name="DdrA">Port A data direction register.</param>
/// <param name="DdrB">Port B data direction register.</param>
/// <param name="TimerACounter">Timer A live counter.</param>
/// <param name="TimerALatch">Timer A reload latch.</param>
/// <param name="TimerBCounter">Timer B live counter.</param>
/// <param name="TimerBLatch">Timer B reload latch.</param>
/// <param name="Cra">Control register A with the live run bit (force-load bit is transient).</param>
/// <param name="Crb">Control register B with the live run bit.</param>
/// <param name="InterruptFlags">Latched ICR interrupt flags (bit 7 excluded).</param>
/// <param name="IrqMask">ICR interrupt-enable mask (bit 7 excluded).</param>
internal readonly record struct CiaSnapshotState(
    byte PortA,
    byte PortB,
    byte DdrA,
    byte DdrB,
    ushort TimerACounter,
    ushort TimerALatch,
    ushort TimerBCounter,
    ushort TimerBLatch,
    byte Cra,
    byte Crb,
    byte InterruptFlags,
    byte IrqMask);
