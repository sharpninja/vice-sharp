using System.Buffers.Binary;
using ViceSharp.Abstractions;

namespace ViceSharp.Chips.Cia;

/// <summary>
/// FR-TICKHIST-CHIP-CIA: CIA full-state capture for the time-travel debugger. Serializes the
/// I/O ports + directions, both timers (latch, counter, control, running), the time-of-day
/// clock, and the interrupt mask/flags.
/// </summary>
public sealed partial class Mos6526 : IStatefulDevice
{
    private const int CiaStateBytes = 4 /*ports*/ + 12 /*timers*/ + 4 /*tod*/ + 2 /*irq*/;

    public string StateName => "CIA";

    public int StateSize => CiaStateBytes;

    public void CaptureState(Span<byte> destination)
    {
        var offset = 0;
        destination[offset++] = _portA;
        destination[offset++] = _portB;
        destination[offset++] = _portADir;
        destination[offset++] = _portBDir;

        offset = WriteTimer(destination, offset, _timerA);
        offset = WriteTimer(destination, offset, _timerB);

        destination[offset++] = _todHours;
        destination[offset++] = _todMinutes;
        destination[offset++] = _todSeconds;
        destination[offset++] = _todTenths;

        destination[offset++] = _interruptMask;
        destination[offset] = _interruptFlags;
    }

    private static int WriteTimer(Span<byte> destination, int offset, TimerState timer)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(offset, 2), timer.Latch); offset += 2;
        BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(offset, 2), timer.Counter); offset += 2;
        destination[offset++] = timer.Control;
        destination[offset++] = (byte)(timer.Running ? 1 : 0);
        return offset;
    }

    public IReadOnlyList<ChipStateField> DecodeState(ReadOnlySpan<byte> state)
    {
        var offset = 0;
        var fields = new List<ChipStateField>(16)
        {
            new("PORT-A", state[offset++]),
            new("PORT-B", state[offset++]),
            new("DDR-A", state[offset++]),
            new("DDR-B", state[offset++]),
        };

        offset = DecodeTimer(fields, state, offset, "TA");
        offset = DecodeTimer(fields, state, offset, "TB");

        fields.Add(new("TOD-H", state[offset++]));
        fields.Add(new("TOD-M", state[offset++]));
        fields.Add(new("TOD-S", state[offset++]));
        fields.Add(new("TOD-T", state[offset++]));
        fields.Add(new("ICR-MASK", state[offset++]));
        fields.Add(new("ICR-DATA", state[offset]));
        return fields;
    }

    private static int DecodeTimer(List<ChipStateField> fields, ReadOnlySpan<byte> state, int offset, string name)
    {
        fields.Add(new($"{name}-LATCH", BinaryPrimitives.ReadUInt16LittleEndian(state.Slice(offset, 2)), 2)); offset += 2;
        fields.Add(new($"{name}-COUNT", BinaryPrimitives.ReadUInt16LittleEndian(state.Slice(offset, 2)), 2)); offset += 2;
        fields.Add(new($"{name}-CTRL", state[offset++]));
        fields.Add(new($"{name}-RUN", state[offset++]));
        return offset;
    }
}
