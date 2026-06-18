using System.Buffers.Binary;
using ViceSharp.Abstractions;

namespace ViceSharp.Chips.Audio;

/// <summary>
/// FR-TICKHIST-CHIP-SID: SID full-state capture for the time-travel debugger. Serializes the
/// 32-byte register file plus the per-voice internal state not visible in registers (the
/// 24-bit phase accumulator, the current envelope level, and the ADSR phase).
/// </summary>
public partial class Sid6581 : IStatefulDevice
{
    private const int SidRegisterBytes = 0x20;
    private const int SidPerVoiceBytes = 6;   // accumulator(4) + envelope(1) + adsr-state(1)
    private const int SidVoiceCount = 3;

    public string StateName => "SID";

    public int StateSize => SidRegisterBytes + SidVoiceCount * SidPerVoiceBytes;

    public void CaptureState(Span<byte> destination)
    {
        _registers.AsSpan(0, SidRegisterBytes).CopyTo(destination);

        var offset = SidRegisterBytes;
        for (var v = 0; v < SidVoiceCount; v++)
        {
            ref var voice = ref _voices[v];
            BinaryPrimitives.WriteUInt32LittleEndian(destination.Slice(offset, 4), voice.WaveformAccumulator); offset += 4;
            destination[offset++] = voice.Envelope;
            destination[offset++] = (byte)voice.State;
        }
    }

    public IReadOnlyList<ChipStateField> DecodeState(ReadOnlySpan<byte> state)
    {
        var fields = new List<ChipStateField>(16)
        {
            new("VOL/FILT $D418", state[0x18]),
            new("FILT-LO $D415", state[0x15]),
            new("FILT-HI $D416", state[0x16]),
            new("RES/ROUTE $D417", state[0x17]),
        };

        var offset = SidRegisterBytes;
        for (var v = 0; v < SidVoiceCount; v++)
        {
            var accumulator = (int)(BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(offset, 4)) & 0xFFFFFF); offset += 4;
            var envelope = state[offset++];
            var adsr = state[offset++];
            fields.Add(new($"V{v + 1} ACC", accumulator, 2));
            fields.Add(new($"V{v + 1} ENV", envelope));
            fields.Add(new($"V{v + 1} ADSR", adsr));
        }

        return fields;
    }
}
