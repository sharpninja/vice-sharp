using ViceSharp.Abstractions;

namespace ViceSharp.Chips.VicIi;

/// <summary>
/// MOS 6567 VIC-II NTSC variant.
/// Inherits from 6569 (PAL), sets NTSC system for timing.
/// </summary>
public sealed class Mos6567 : Mos6569
{
    public Mos6567(IBus bus, IInterruptLine irqLine) : base(bus, irqLine)
    {
        ConfigureTiming(
            TvSystem.NTSC,
            NtscCyclesPerLine,
            NtscVisibleLines,
            NtscTotalLines,
            1_022_730d / (NtscCyclesPerLine * NtscTotalLines));
    }

    public override string Name => "MOS 6567 VIC-II (NTSC)";
    public override DeviceId Id => new DeviceId(0x0002);

    /// <summary>
    /// audit M15/L7: 6567 uses the 6569r5 palette (vicii-color.c:636-639)
    /// through the NTSC YIQ conversion (video-color.c:267-278).
    /// </summary>
    public override bool IsNtscVideo => true;
}
