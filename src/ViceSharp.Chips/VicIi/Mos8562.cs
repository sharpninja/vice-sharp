using ViceSharp.Abstractions;

namespace ViceSharp.Chips.VicIi;

/// <summary>
/// HMOS MOS 8562 NTSC VIC-II variant.
/// </summary>
public sealed class Mos8562 : Mos6569
{
    public Mos8562(IBus bus, IInterruptLine irqLine) : base(bus, irqLine)
    {
        ConfigureTiming(
            TvSystem.NTSC,
            NtscCyclesPerLine,
            NtscVisibleLines,
            NtscTotalLines,
            1_022_730d / (NtscCyclesPerLine * NtscTotalLines));
    }

    public override string Name => "MOS 8562 VIC-II (new NTSC)";
    public override DeviceId Id => new DeviceId(0x0008);
}
