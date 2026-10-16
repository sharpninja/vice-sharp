using ViceSharp.Chips.IEC;

namespace ViceSharp.Core.Vic20;

/// <summary>
/// VIC-20 serial IEC board wiring on VIA1 Port A and VIA2 CA2/CB2
/// (VICE <c>vic20iec.c</c> / <c>vic20via1.c</c>).
/// </summary>
/// <remarks>
/// Layout (VICE comments in vic20iec.c):
/// <list type="bullet">
/// <item>VIA1 PA0 = CLK in</item>
/// <item>VIA1 PA1 = DATA in</item>
/// <item>VIA1 PA7 = ATN out (via PRA/DDRA, not this input mask)</item>
/// <item>VIA2 CA2 = CLK out</item>
/// <item>VIA2 CB2 = DATA out</item>
/// <item>VIA2 CB1 = SRQ in</item>
/// </list>
/// Idle open-collector bus after <c>vic20iec_init</c> (cpu_clock=1, cpu_data=0,
/// cpu_atn=0, no drives): bus_clock=0, bus_data=1, bus_atn=1 so
/// <c>iec_pa_read</c> returns bit0=0, bit1=1, bit7=0 before joy/tape OR.
/// Joystick/tape bits 2..6 idle high give <c>0x7E</c> with all-input DDRA or
/// ATN-as-output-low.
/// </remarks>
public static class Vic20IecPort
{
    /// <summary>
    /// Idle IEC + released joystick + no-tape sense on VIA1 PA input bits
    /// (before DDR/OR merge). Matches VICE <c>iec_pa_read() | joy_bits</c>
    /// with released joystick and tape_sense=0.
    /// </summary>
    public const byte IdlePortAInput = 0x7E;

    /// <summary>
    /// Wire VIA1 Port A input to VICE-idle IEC/joy/tape levels. Call before
    /// <see cref="Vic20KeyboardMatrix.Connect"/> so the matrix can wrap this
    /// callback for joystick active-low bits 2..5.
    /// </summary>
    public static void AttachIdleVia1PortA(Via6522 via1)
    {
        ArgumentNullException.ThrowIfNull(via1);
        var previous = via1.PortAInput;
        via1.PortAInput = () =>
        {
            var baseVal = previous?.Invoke() ?? IdlePortAInput;
            // Prefer explicit idle IEC bits when nothing else is chained yet.
            if (previous is null)
                return IdlePortAInput;
            // If a prior callback exists, force CLK-in low / DATA-in high /
            // ATN-in path clear (bit7 is PRA out) while keeping other bits.
            return (byte)((baseVal & 0x7C) | 0x02);
        };
    }
}
