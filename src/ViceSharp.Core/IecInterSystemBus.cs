using ViceSharp.Abstractions;
using ViceSharp.Core;

namespace ViceSharp.Chips.IEC;

/// <summary>
/// Canonical Commodore IEC Serial Bus realized on the multi-system substrate.
/// Thin factory + signal-name constants on top of <see cref="InterSystemBus"/>.
/// Signals follow VICE's wired-OR (open-collector) model: a line is high
/// when no endpoint pulls it low.
///
/// Wraps three required signals (ATN, CLK, DATA) plus the asynchronous SRQ
/// service request used by IEEE-488-aware devices (1571, 1581, IEEE drives).
/// RESET is intentionally NOT a bus signal here - reset is a per-machine
/// reset operation routed through <see cref="ISystemCoordinator.Reset"/>.
/// </summary>
public static class IecInterSystemBus
{
    /// <summary>ATN (attention) - host pulls low to broadcast a command.</summary>
    public const string Atn = "ATN";

    /// <summary>CLK (clock) - bit clock for byte transfers.</summary>
    public const string Clk = "CLK";

    /// <summary>DATA - bit data + handshake acknowledge.</summary>
    public const string Data = "DATA";

    /// <summary>SRQ (service request) - asynchronous service signal.</summary>
    public const string Srq = "SRQ";

    /// <summary>Default bus name.</summary>
    public const string BusName = "IEC";

    private static readonly string[] DefaultSignals = { Atn, Clk, Data, Srq };

    /// <summary>The canonical IEC signal set (ATN, CLK, DATA, SRQ).</summary>
    public static IReadOnlyList<string> Signals => DefaultSignals;

    /// <summary>
    /// Create an <see cref="InterSystemBus"/> pre-configured with the IEC
    /// signal set. Attach endpoints for the C64 host and each drive, then
    /// register with the coordinator.
    /// </summary>
    public static InterSystemBus Create(string name = BusName)
        => new InterSystemBus(name, DefaultSignals);
}
