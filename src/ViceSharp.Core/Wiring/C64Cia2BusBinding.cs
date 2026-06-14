using ViceSharp.Abstractions;
using ViceSharp.Chips.Cia;

namespace ViceSharp.Core.Wiring;

/// <summary>
/// Compatibility shim for code that still binds C64 CIA2 externally.
/// New machine wiring should use <see cref="C64Cia2InterfaceDevice"/>.
/// </summary>
public static class C64Cia2BusBinding
{
    /// <summary>
    /// Bind CIA2 port input/output to the supplied bus endpoints. Pass null
    /// for either endpoint to skip that bus.
    /// </summary>
    public static void Bind(
        Mos6526 cia2,
        IBusEndpoint? userPort = null,
        IBusEndpoint? iec = null) =>
        new C64Cia2InterfaceDevice().ConnectCia2(cia2, userPort, iec);
}
