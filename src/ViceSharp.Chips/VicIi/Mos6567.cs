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
        System = TvSystem.NTSC;
    }

    public override string Name => "MOS 6567 VIC-II (NTSC)";
    public override DeviceId Id => new DeviceId(0x0002);
}
