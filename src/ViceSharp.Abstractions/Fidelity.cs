namespace ViceSharp.Abstractions;

/// <summary>
/// Per-device fidelity selector. Controls whether a peripheral runs as a
/// lightweight in-host emulation (cheap path) or as a true standalone
/// machine attached to a SystemCoordinator + bus (heavy path).
///
/// Per the ARCH-MULTISYSTEM-001 plan, the cheap-path -001 gates (buffered
/// IEC sector read, raw 8K/16K cart mapping, pulse-only datasette, snapshot,
/// BMP capture) remain selectable so headless CI keeps running fast. Real
/// device behavior is opted into by setting <see cref="TrueDevice"/>.
/// </summary>
public enum Fidelity
{
    /// <summary>
    /// Lightweight in-host emulation. For 1541: IecD64Attachment reads
    /// sectors directly from the D64 image. For cartridges:
    /// StandardCartridgeImage stays the mapping authority. For tape:
    /// TapPulseReader iteration only. Default for backward compatibility
    /// with the existing single-machine test harness.
    /// </summary>
    Buffered = 0,

    /// <summary>
    /// Full true-device emulation as a standalone IMachine attached to a
    /// SystemCoordinator with an InterSystemBus bridging signals to the
    /// host. For 1541: a 6502 drive CPU running the DOS ROM + VIA timer +
    /// IEC handshake. For cartridges: an ICartPortExtension machine. For
    /// user-port peripherals: an IMachine on the UserPort bus.
    /// </summary>
    TrueDevice = 1,
}
