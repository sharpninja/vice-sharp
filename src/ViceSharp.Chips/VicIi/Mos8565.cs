using ViceSharp.Abstractions;

namespace ViceSharp.Chips.VicIi;

/// <summary>
/// HMOS MOS 8565 PAL VIC-II variant used by newer C64C and C64GS models.
/// </summary>
public sealed class Mos8565 : Mos6569
{
    public Mos8565(IBus bus, IInterruptLine irqLine) : base(bus, irqLine)
    {
        ConfigureTiming(
            TvSystem.PAL,
            PalCyclesPerLine,
            PalVisibleLines,
            PalTotalLines,
            985_248d / (PalCyclesPerLine * PalTotalLines));
    }

    public override string Name => "MOS 8565 VIC-II (new PAL)";
    public override DeviceId Id => new DeviceId(0x0009);
}
