using ViceSharp.Abstractions;

namespace ViceSharp.Chips.VicIi;

/// <summary>
/// MOS 6572 PAL-N VIC-II variant used by Drean/PAL-N C64 models.
/// </summary>
public sealed class Mos6572 : Mos6569
{
    public Mos6572(IBus bus, IInterruptLine irqLine) : base(bus, irqLine)
    {
        ConfigureTiming(
            TvSystem.PALN,
            NtscCyclesPerLine,
            PalVisibleLines,
            PalTotalLines,
            1_023_440d / (NtscCyclesPerLine * PalTotalLines));
    }

    public override string Name => "MOS 6572 VIC-II (PAL-N)";
    public override DeviceId Id => new DeviceId(0x0007);
}
