using ViceSharp.Abstractions;

namespace ViceSharp.Chips.VicIi;

/// <summary>
/// MOS 6567 VIC-II NTSC variant.
/// Inherits from 6569 (PAL), overrides timing constants for NTSC operation.
/// </summary>
public sealed class Mos6567 : Mos6569
{
    public Mos6567(IBus bus, IInterruptLine irqLine) : base(bus, irqLine)
    {
        IsPal = false;
    }

    /// <summary>
    /// NTSC 6567 has 64 cycles per line (vs 63 for PAL)
    /// </summary>
    public new int CyclesPerLine => NtscCyclesPerLine;

    /// <summary>
    /// NTSC 6567 has 262 visible lines
    /// </summary>
    public new int VisibleLines => NtscVisibleLines;

    /// <summary>
    /// NTSC 6567 has 263 total lines
    /// </summary>
    public new int TotalLines => NtscTotalLines;

    /// <summary>
    /// NTSC frame rate is 60 Hz
    /// </summary>
    public new double FrameRate => 60.0;

    public override string Name => "MOS 6567 VIC-II (NTSC)";
    public override DeviceId Id => new DeviceId(0x0002);
}
