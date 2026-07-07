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

    /// <summary>
    /// PLAN-VICEPARITY-001 FR-VIC-LIGHTPEN AC-10: the 6567R56A uses the old
    /// light-pen IRQ mode (VICE viciisc/vicii-chip-model.c:568-576,
    /// lightpen_old_irq_mode = 1): the LP interrupt fires only on the
    /// frame-start retrigger, never on a normal trigger
    /// (vicii-lightpen.c:93-98,105-107).
    /// </summary>
    protected override bool LightPenOldIrqMode => true;

    /// <summary>
    /// audit M15/L7: 6567R56A uses the 5-luma 6569r1 palette
    /// (vicii-color.c:632-634) through the NTSC YIQ conversion.
    /// </summary>
    public override VicPalette.Group PaletteGroup => VicPalette.Group.Mos6569R1;

    /// <inheritdoc />
    public override bool IsNtscVideo => true;
}
