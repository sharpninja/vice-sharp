namespace ViceSharp.Chips.Audio;

/// <summary>
/// FR-SNP-003. SID execution state that register writes do not retain.
/// VICE <c>SID::write_state</c> restores the accumulator, noise shift
/// register, and envelope counters beside the register file
/// (resid/sid.cc). The latched filter output is included so the current
/// audio sample matches before the next tick.
/// </summary>
internal sealed class SidExecutionState
{
    public SidVoiceImage[] Voices { get; } = new SidVoiceImage[3];

    public int Vhp { get; set; }

    public int Vbp { get; set; }

    public int Vlp { get; set; }

    public int VbpX { get; set; }

    public int VbpVc { get; set; }

    public int VlpX { get; set; }

    public int VlpVc { get; set; }

    public int ExtFiltVlp { get; set; }

    public int ExtFiltVhp { get; set; }

    public int ExtFilterOutput { get; set; }

    public int FilterOutput { get; set; }

    public int VoiceOutput0 { get; set; }

    public int VoiceOutput1 { get; set; }

    public int VoiceOutput2 { get; set; }

    public byte BusValue { get; set; }

    public int BusValueTtl { get; set; }

    /// <summary>
    /// reSID write_pipeline. A 1 means the 8580 Fast-mode write has not
    /// been committed yet. VICE SID::write_state saves this byte.
    /// </summary>
    public byte WritePipeline { get; set; }

    /// <summary>
    /// reSID write_address. The register offset consumed with
    /// <see cref="BusValue"/> when the pipeline slot flushes.
    /// </summary>
    public byte WriteAddress { get; set; }

    public bool MsbRising0 { get; set; }

    public bool MsbRising1 { get; set; }

    public bool MsbRising2 { get; set; }

    public byte FilterLo { get; set; }

    public byte FilterHi { get; set; }

    public byte Resonance { get; set; }

    public byte ModeVol { get; set; }

    /// <summary>
    /// Live resampler ring. VICE <c>SID::State</c> does not carry the host
    /// sample history. Replay still needs it so the FIR does not restart
    /// from silence. Null means no live backend was armed.
    /// </summary>
    public short[]? Ring { get; set; }

    /// <summary>reSID sample_index into <see cref="Ring"/>.</summary>
    public int SampleIndex { get; set; }

    /// <summary>Live 16.16 sample_offset.</summary>
    public int LiveSampleOffset { get; set; }

    /// <summary>Cycles left before the next live convolution.</summary>
    public int LiveDeltaTRemaining { get; set; }
}

/// <summary>One SID voice, including the reSID envelope struct.</summary>
internal struct SidVoiceImage
{
    public uint Accumulator;
    public ushort Osc3;
    public ushort WaveformOutput;
    public ushort Frequency;
    public ushort PulseWidth;
    public byte Control;
    public ushort PulseLevel;
    public ushort TriSawPipeline;
    public int FloatingOutputTtl;
    public uint ShiftRegister;
    public uint ShiftRegisterReset;
    public uint ShiftPipeline;
    public byte Envelope;
    public byte State;
    public Sid6581.ReSidEnvelope Env;
}
