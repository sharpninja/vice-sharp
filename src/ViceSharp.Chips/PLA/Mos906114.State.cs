using ViceSharp.Abstractions;

namespace ViceSharp.Chips.Pla;

/// <summary>
/// FR-TICKHIST-CHIP-PLA: PLA full-state capture for the time-travel debugger. The PLA's state
/// is the 6510 processor-port direction ($00) and data ($01) registers, which drive the
/// LORAM/HIRAM/CHAREN banking lines.
/// </summary>
public sealed partial class Mos906114 : IStatefulDevice
{
    public string StateName => "PLA";

    public int StateSize => 2;

    public void CaptureState(Span<byte> destination)
    {
        destination[0] = DataDirection;
        destination[1] = DataRegister;
    }

    public IReadOnlyList<ChipStateField> DecodeState(ReadOnlySpan<byte> state)
    {
        var port = state[1];
        return new ChipStateField[]
        {
            new("$00 DDR", state[0]),
            new("$01 PORT", port),
            new("LORAM", (port & 0x01) != 0 ? 1 : 0),
            new("HIRAM", (port & 0x02) != 0 ? 1 : 0),
            new("CHAREN", (port & 0x04) != 0 ? 1 : 0),
        };
    }
}
