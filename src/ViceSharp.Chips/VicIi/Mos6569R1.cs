using ViceSharp.Abstractions;

namespace ViceSharp.Chips.VicIi;

/// <summary>
/// Original MOS 6569R1 PAL VIC-II variant.
/// </summary>
public sealed class Mos6569R1 : Mos6569
{
    public Mos6569R1(IBus bus, IInterruptLine irqLine) : base(bus, irqLine)
    {
        ConfigureTiming(
            TvSystem.PAL,
            PalCyclesPerLine,
            PalVisibleLines,
            PalTotalLines,
            985_248d / (PalCyclesPerLine * PalTotalLines));
    }

    public override string Name => "MOS 6569R1 VIC-II (old PAL)";
    public override DeviceId Id => new DeviceId(0x000A);
}
