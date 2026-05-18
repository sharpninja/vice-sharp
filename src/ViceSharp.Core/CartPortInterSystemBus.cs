using ViceSharp.Abstractions;

namespace ViceSharp.Core;

/// <summary>
/// Cartridge-port pin bus realized on the multi-system substrate. Carries
/// the active-cart signals that determine memory mapping and interrupt
/// delivery for the host C64:
///
///   GAME, EXROM  - select cartridge banking mode (memory map)
///   IRQ, NMI     - extension-asserted interrupts
///   DMA          - extension-asserted DMA acquire
///   RESET        - cart reset line (independent of system reset)
///
/// Wired-OR (open-collector) semantics apply via <see cref="InterSystemBus"/>.
/// Phi2 is NOT modeled here - the extension shares the host clock via
/// <see cref="ISystemCoordinator.AttachCartExtension"/> (Phase A substrate).
///
/// Active extension systems (SuperCPU, smart REU, future cartridges that
/// run their own CPU) attach an endpoint to this bus to drive pins. Passive
/// cartridges (StandardCartridgeImage) stay on the cheap path through
/// <see cref="ICartridgePort"/> + memory mapping only.
/// </summary>
public static class CartPortInterSystemBus
{
    /// <summary>GAME line - banking selector (low = ROM at $A000 in 16K mode).</summary>
    public const string Game = "GAME";

    /// <summary>EXROM line - banking selector (low = cart visible at $8000).</summary>
    public const string ExRom = "EXROM";

    /// <summary>IRQ line - extension asserts to request maskable interrupt.</summary>
    public const string Irq = "IRQ";

    /// <summary>NMI line - extension asserts to request non-maskable interrupt.</summary>
    public const string Nmi = "NMI";

    /// <summary>DMA line - extension acquires the bus while pulled low.</summary>
    public const string Dma = "DMA";

    /// <summary>RESET line - cart-port reset (independent of system reset).</summary>
    public const string Reset = "RESET";

    /// <summary>Default bus name.</summary>
    public const string BusName = "CartPort";

    private static readonly string[] DefaultSignals = { Game, ExRom, Irq, Nmi, Dma, Reset };

    /// <summary>Canonical cart-port signal set.</summary>
    public static IReadOnlyList<string> Signals => DefaultSignals;

    /// <summary>
    /// Create an <see cref="InterSystemBus"/> pre-configured with the
    /// cart-port signal set.
    /// </summary>
    public static InterSystemBus Create(string name = BusName)
        => new InterSystemBus(name, DefaultSignals);
}
