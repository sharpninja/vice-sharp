using ViceSharp.Abstractions;
using ViceSharp.Chips.Cia;
using ViceSharp.Chips.Tape;

namespace ViceSharp.Core.Wiring;

/// <summary>
/// Wires a Datasette's pull-model tape pulse stream to CIA1's FLAG pin.
///
/// On real C64 hardware the Datasette READ line is physically tied to
/// CIA1 FLAG (pin 24). Each tape pulse is a high-to-low transition on
/// READ; that edge latches CIA1 ICR bit 4 (FLG) and, when IMR bit 4 is
/// enabled, asserts the CIA1 IRQ output. The KERNAL tape routines rely
/// on this path.
///
/// The Datasette exposes a pull-model API: <see cref="Datasette.TryReadNextPulse(out int)"/>
/// reports the number of host phi2 cycles to the next pulse edge. This
/// binding owns the per-pulse cycle countdown: each Tick decrements the
/// remaining cycles; on reaching zero the binding invokes
/// <see cref="Mos6526.TriggerFlagPin"/> and pulls the next pulse from
/// the datasette. If the datasette has no tape, the motor is off, play
/// is not pressed, or the tape is exhausted, the binding stays idle and
/// FLAG is never triggered.
///
/// The binding is constructed in the C64 system builder once CIA1 and
/// the Datasette exist; the binding itself is an <see cref="IClockedDevice"/>
/// so the system clock ticks it alongside every other Phi2 device.
/// Tests can construct it directly to exercise the wire shape without
/// spinning up the full machine.
/// </summary>
public sealed class DatasetteCia1FlagBinding : IClockedDevice
{
    private readonly Datasette _datasette;
    private readonly Mos6526 _cia1;

    private int _cyclesUntilNextPulse;
    private bool _havePendingPulse;

    /// <summary>
    /// Create a binding that drives the given CIA1's FLAG pin from the
    /// given Datasette's pulse stream.
    /// </summary>
    /// <param name="datasette">The C2N datasette source.</param>
    /// <param name="cia1">CIA1, whose FLAG pin is wired to tape READ.</param>
    public DatasetteCia1FlagBinding(Datasette datasette, Mos6526 cia1)
    {
        _datasette = datasette ?? throw new ArgumentNullException(nameof(datasette));
        _cia1 = cia1 ?? throw new ArgumentNullException(nameof(cia1));
    }

    /// <inheritdoc />
    public DeviceId Id => new(0x0021);

    /// <inheritdoc />
    public string Name => "Datasette to CIA1 FLAG binding";

    /// <inheritdoc />
    public uint ClockDivisor => 1;

    /// <inheritdoc />
    public ClockPhase Phase => ClockPhase.Phi2;

    /// <summary>
    /// Reset the binding's per-pulse countdown. Does not reset the
    /// underlying datasette or CIA1; the builder resets those via the
    /// device registry.
    /// </summary>
    public void Reset()
    {
        _cyclesUntilNextPulse = 0;
        _havePendingPulse = false;
    }

    /// <summary>
    /// Advance the binding by one host phi2 cycle. When the pulse
    /// countdown elapses, trigger CIA1 FLAG and pull the next pulse.
    /// Idempotent when the datasette is idle (no tape, motor off, play
    /// off, or tape exhausted) - the binding simply waits for state to
    /// change.
    /// </summary>
    public void Tick()
    {
        // If no pulse is pending, try to pull one. The datasette gates
        // on HasTape + MotorEnabled + PlayPressed internally; a false
        // return means we just stay idle this cycle.
        if (!_havePendingPulse)
        {
            if (!_datasette.TryReadNextPulse(out var cycles))
                return;

            _cyclesUntilNextPulse = cycles;
            _havePendingPulse = true;
        }

        // Count this cycle off the pulse interval. When the interval
        // elapses, the READ line transitions high-to-low and CIA1 FLAG
        // latches via TriggerFlagPin. The next pulse will be pulled on
        // a subsequent Tick (so a degenerate zero-length pulse doesn't
        // recurse and starve the rest of the system).
        _cyclesUntilNextPulse--;
        if (_cyclesUntilNextPulse > 0)
            return;

        _havePendingPulse = false;
        _cia1.TriggerFlagPin();
    }
}
