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

    /// <summary>
    /// PLAN-VICEPARITY-001 FR-VIC-LIGHTPEN AC-10: the 6569R1 uses the old
    /// light-pen IRQ mode (VICE viciisc/vicii-chip-model.c:240-248,
    /// lightpen_old_irq_mode = 1): the LP interrupt fires only on the
    /// frame-start retrigger, never on a normal trigger
    /// (vicii-lightpen.c:93-98,105-107).
    /// </summary>
    protected override bool LightPenOldIrqMode => true;
}
