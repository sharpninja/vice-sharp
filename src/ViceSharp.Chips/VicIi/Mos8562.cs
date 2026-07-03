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

    /// <summary>
    /// PLAN-VICEPARITY-001 FR-VIC-LIGHTPEN AC-07: the HMOS 8562 has no colour
    /// latency (VICE viciisc/vicii-chip-model.c:415-423, color_latency = 0),
    /// so the light-pen x offset is 1 instead of 2 (vicii-lightpen.c:42).
    /// </summary>
    protected override bool ColorLatency => false;
}
