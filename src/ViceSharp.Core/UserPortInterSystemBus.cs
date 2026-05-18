using ViceSharp.Abstractions;

namespace ViceSharp.Core;

/// <summary>
/// C64 user-port pin bus realized on the multi-system substrate. Maps the
/// physical 24-pin user port to a named-signal InterSystemBus, excluding
/// power / ground lines. Peripheral cartridges (VIC-1011 RS232, paddles,
/// modems, parallel cables) and peer-to-peer machine links (two C64s
/// hooked user-to-user) attach endpoints to drive these signals.
///
/// Signal set (matches the C64 service manual):
///   PB0..PB7    - CIA2 port B (8 bidirectional data lines)
///   PA2         - CIA2 PA2 output (read by peripherals)
///   PC2         - CIA2 handshake output (pulses on PB read/write)
///   FLAG2       - CIA2 FLAG input (rising-edge interrupt)
///   CNT1, CNT2  - CIA1/CIA2 shift-register clocks
///   SP1, SP2    - CIA1/CIA2 shift-register data
///   ATN         - SER ATN output to user port (cassette / RS232 cart)
///   RESET       - cart reset
///
/// Wired-OR (open-collector) semantics apply via <see cref="InterSystemBus"/>.
/// </summary>
public static class UserPortInterSystemBus
{
    public const string Pb0 = "PB0";
    public const string Pb1 = "PB1";
    public const string Pb2 = "PB2";
    public const string Pb3 = "PB3";
    public const string Pb4 = "PB4";
    public const string Pb5 = "PB5";
    public const string Pb6 = "PB6";
    public const string Pb7 = "PB7";
    public const string Pa2 = "PA2";
    public const string Pc2 = "PC2";
    public const string Flag2 = "FLAG2";
    public const string Cnt1 = "CNT1";
    public const string Cnt2 = "CNT2";
    public const string Sp1 = "SP1";
    public const string Sp2 = "SP2";
    public const string Atn = "ATN";
    public const string Reset = "RESET";

    /// <summary>Default bus name.</summary>
    public const string BusName = "UserPort";

    private static readonly string[] DefaultSignals =
    {
        Pb0, Pb1, Pb2, Pb3, Pb4, Pb5, Pb6, Pb7,
        Pa2, Pc2, Flag2,
        Cnt1, Cnt2, Sp1, Sp2,
        Atn, Reset,
    };

    /// <summary>Canonical user-port signal set (17 logical lines).</summary>
    public static IReadOnlyList<string> Signals => DefaultSignals;

    /// <summary>The PB0..PB7 byte-wide port (in bit order).</summary>
    public static IReadOnlyList<string> PortBLines { get; } =
        new[] { Pb0, Pb1, Pb2, Pb3, Pb4, Pb5, Pb6, Pb7 };

    /// <summary>
    /// Create an <see cref="InterSystemBus"/> pre-configured with the
    /// user-port signal set.
    /// </summary>
    public static InterSystemBus Create(string name = BusName)
        => new InterSystemBus(name, DefaultSignals);

    /// <summary>
    /// Read PB0..PB7 from a bus endpoint's perspective and pack the 8 pins
    /// into a single byte (PB0 = bit 0, PB7 = bit 7). Bit set when pin is
    /// high (idle / driven high); bit clear when pulled low.
    /// </summary>
    public static byte ReadPortB(IBusEndpoint endpoint)
    {
        byte value = 0;
        for (var i = 0; i < PortBLines.Count; i++)
            if (endpoint.ReadLine(PortBLines[i]))
                value |= (byte)(1 << i);
        return value;
    }

    /// <summary>
    /// Drive PB0..PB7 from an endpoint by pulling the bits that are 0 in
    /// <paramref name="value"/> low and releasing bits that are 1. Mirrors
    /// CIA2 PB output behavior with DDR set to outputs.
    /// </summary>
    public static void WritePortB(IBusEndpoint endpoint, byte value)
    {
        for (var i = 0; i < PortBLines.Count; i++)
        {
            var bitSet = (value & (1 << i)) != 0;
            endpoint.Pull(PortBLines[i], low: !bitSet);
        }
    }
}
