using ViceSharp.Abstractions;

namespace ViceSharp.Chips.VicIi;

/// <summary>
/// HMOS MOS 8565 PAL VIC-II variant.
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

    /// <summary>
    /// PLAN-VICEPARITY-001 FR-VIC-LIGHTPEN AC-07: the HMOS 8565 has no colour
    /// latency (VICE viciisc/vicii-chip-model.c:260-268, color_latency = 0),
    /// so the light-pen x offset is 1 instead of 2 (vicii-lightpen.c:42).
    /// </summary>
    protected override bool ColorLatency => false;

    /// <summary>
    /// audit M15/L7: 8565 uses the 8565r2 palette (vicii-color.c:641-643).
    /// </summary>
    public override VicPalette.Group PaletteGroup => VicPalette.Group.Mos8565R2;
}
