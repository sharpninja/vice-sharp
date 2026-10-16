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

    /// <summary>
    /// FR-SNP-003. Copies oscillator, noise, envelope, and filter execution
    /// state. Register peeks do not include those fields. VICE
    /// <c>SID::State</c> carries the same voice fields (resid/sid.cc).
    /// </summary>
    /// <returns>The execution state <see cref="InjectExecutionState"/> restores.</returns>
    internal SidExecutionState CaptureExecutionState()
    {
        var state = new SidExecutionState();
        for (var i = 0; i < 3; i++)
        {
            ref var voice = ref _voices[i];
            state.Voices[i] = new SidVoiceImage
            {
                Accumulator = voice.WaveformAccumulator,
                Osc3 = voice.Osc3,
                WaveformOutput = voice.WaveformOutput,
                Frequency = voice.Frequency,
                PulseWidth = voice.PulseWidth,
                Control = voice.Control,
                PulseLevel = voice.PulseLevel,
                TriSawPipeline = voice.TriSawPipeline,
                FloatingOutputTtl = voice.FloatingOutputTtl,
                ShiftRegister = voice.ShiftRegister,
                ShiftRegisterReset = voice.ShiftRegisterReset,
                ShiftPipeline = voice.ShiftPipeline,
                Envelope = voice.Envelope,
                State = (byte)voice.State,
                Env = voice.Env
            };
        }

        state.Vhp = _vhp;
        state.Vbp = _vbp;
        state.Vlp = _vlp;
        state.VbpX = _vbpX;
        state.VbpVc = _vbpVc;
        state.VlpX = _vlpX;
        state.VlpVc = _vlpVc;
        state.ExtFiltVlp = _extFiltVlp;
        state.ExtFiltVhp = _extFiltVhp;
        state.ExtFilterOutput = _cycleExtFilterOutput;
        state.FilterOutput = _cycleFilterOutput;
        state.VoiceOutput0 = _cycleVoiceOutput0;
        state.VoiceOutput1 = _cycleVoiceOutput1;
        state.VoiceOutput2 = _cycleVoiceOutput2;
        state.BusValue = _busValue;
        state.BusValueTtl = _busValueTtl;
        state.WritePipeline = _writePipeline;
        state.WriteAddress = _writeAddress;
        state.MsbRising0 = _msbRising0;
        state.MsbRising1 = _msbRising1;
        state.MsbRising2 = _msbRising2;
        state.FilterLo = _registers[0x15];
        state.FilterHi = _registers[0x16];
        state.Resonance = _registers[0x17];
        state.ModeVol = _registers[0x18];
        if (_ring is not null)
        {
            state.Ring = _ring.ToArray();
            state.SampleIndex = _sampleIndex;
            state.LiveSampleOffset = _liveSampleOffset;
            state.LiveDeltaTRemaining = _liveDeltaTRemaining;
        }

        return state;
    }

    /// <summary>
    /// FR-SNP-003. Writes execution state back without the control-register
    /// side effects that would restart the envelope from power-up.
    /// </summary>
    /// <param name="state">State previously returned by <see cref="CaptureExecutionState"/>.</param>
    internal void InjectExecutionState(SidExecutionState state)
    {
        for (var i = 0; i < 3; i++)
        {
            ref var voice = ref _voices[i];
            var image = state.Voices[i];
            voice.WaveformAccumulator = image.Accumulator & 0x00FFFFFFu;
            voice.Osc3 = image.Osc3;
            voice.WaveformOutput = image.WaveformOutput;
            voice.Frequency = image.Frequency;
            voice.PulseWidth = image.PulseWidth;
            voice.Control = image.Control;
            voice.Gate = (image.Control & 0x01) != 0;
            voice.PulseLevel = image.PulseLevel;
            voice.TriSawPipeline = image.TriSawPipeline;
            voice.FloatingOutputTtl = image.FloatingOutputTtl;
            voice.ShiftRegister = image.ShiftRegister;
            voice.ShiftRegisterReset = image.ShiftRegisterReset;
            voice.ShiftPipeline = image.ShiftPipeline;
            voice.Envelope = image.Envelope;
            voice.State = (EnvelopeState)image.State;
            voice.Env = image.Env;
        }

        _vhp = state.Vhp;
        _vbp = state.Vbp;
        _vlp = state.Vlp;
        _vbpX = state.VbpX;
        _vbpVc = state.VbpVc;
        _vlpX = state.VlpX;
        _vlpVc = state.VlpVc;
        _extFiltVlp = state.ExtFiltVlp;
        _extFiltVhp = state.ExtFiltVhp;
        _cycleExtFilterOutput = state.ExtFilterOutput;
        _cycleFilterOutput = state.FilterOutput;
        _cycleVoiceOutput0 = state.VoiceOutput0;
        _cycleVoiceOutput1 = state.VoiceOutput1;
        _cycleVoiceOutput2 = state.VoiceOutput2;
        _busValue = state.BusValue;
        _busValueTtl = state.BusValueTtl;
        // After the IO-window writes. Those writes re-arm the 8580 Fast
        // pipeline with the last $D000 byte. VICE restores write_pipeline
        // and write_address after the register file (resid/sid.cc).
        _writePipeline = state.WritePipeline;
        _writeAddress = state.WriteAddress;
        _msbRising0 = state.MsbRising0;
        _msbRising1 = state.MsbRising1;
        _msbRising2 = state.MsbRising2;
        // VICE SID::State includes the filter and volume registers. The
        // banked-out $D000 window does not write them back.
        _registers[0x15] = state.FilterLo;
        _registers[0x16] = state.FilterHi;
        _registers[0x17] = state.Resonance;
        _registers[0x18] = state.ModeVol;
        _filterCutoff = (_filterCutoff & 0x07F8) | (state.FilterLo & 0x07);
        _filterCutoff = (_filterCutoff & 0x0007) | ((state.FilterHi & 0xFF) << 3);
        _filterResonance = (byte)((state.Resonance >> 4) & 0x0F);
        _filterControl = (byte)((state.Resonance & 0x0F) | (state.ModeVol & 0xF0));
        _volume = (byte)(state.ModeVol & 0x0F);
        LiveResonanceGain = IsMos8580FilterSeam
            ? Resid8580ResonanceGain(_filterResonance)
            : Resid6581ResonanceGain(_filterResonance);
        SetW0();
        SetSumMix_6581();
        if (state.Ring is not null && _ring is not null && state.Ring.Length == _ring.Length)
        {
            state.Ring.AsSpan().CopyTo(_ring);
            _sampleIndex = state.SampleIndex;
            _liveSampleOffset = state.LiveSampleOffset;
            _liveDeltaTRemaining = state.LiveDeltaTRemaining;
        }
    }
}
