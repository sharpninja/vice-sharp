using ViceSharp.Abstractions;

namespace ViceSharp.Chips.VicIi;

/// <summary>
/// Original MOS 6567R56A NTSC VIC-II variant.
/// </summary>
public sealed class Mos6567R56A : Mos6569
{
    public Mos6567R56A(IBus bus, IInterruptLine irqLine) : base(bus, irqLine)
    {
        ConfigureTiming(
            TvSystem.NTSC,
            NtscOldCyclesPerLine,
            NtscVisibleLines,
            NtscOldTotalLines,
            1_022_730d / (NtscOldCyclesPerLine * NtscOldTotalLines));
    }

    public override string Name => "MOS 6567R56A VIC-II (old NTSC)";
    public override DeviceId Id => new DeviceId(0x0006);
}
