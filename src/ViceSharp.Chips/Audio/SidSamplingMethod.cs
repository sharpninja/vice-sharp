namespace ViceSharp.Chips.Audio;

/// <summary>
/// reSID sampling method (sid.h sampling_method). Selects how the per-cycle
/// SID output stream is decimated to the host sample rate. The enum order
/// matches reSID's so the value doubles as the native shim's method selector.
/// x64sc runs SAMPLE_RESAMPLE by default (sid-resources.c:439-441); the 8580
/// one-cycle write pipeline (sid.cc:211-216) only arms under SAMPLE_FAST, so
/// it stays inert in normal operation exactly as in VICE.
/// PLAN-VICEPARITY-001 S11 (FR-SID-8580 / FR-SID-OUTPUT).
/// </summary>
public enum SidSamplingMethod
{
    /// <summary>Nearest-neighbour decimation (non-cycle-accurate). reSID SAMPLE_FAST.</summary>
    Fast = 0,

    /// <summary>Linear interpolation between cycles. reSID SAMPLE_INTERPOLATE.</summary>
    Interpolate = 1,

    /// <summary>Windowed-sinc (Kaiser FIR) resampling. reSID SAMPLE_RESAMPLE.</summary>
    Resample = 2,

    /// <summary>Resample with a smaller cached FIR table. reSID SAMPLE_RESAMPLE_FASTMEM.</summary>
    ResampleFastMem = 3,
}
