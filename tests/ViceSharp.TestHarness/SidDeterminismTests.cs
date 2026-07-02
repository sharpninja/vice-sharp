namespace ViceSharp.TestHarness;

using System;
using System.Security.Cryptography;
using FluentAssertions;
using ViceSharp.Chips.Audio;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: TR-DET-001 (audio sample determinism).
/// Use case: Two independent <see cref="Sid6581"/> instances initialized
/// from the default constructor state and driven by identical register-
/// write + Tick sequences must produce bit-exact identical sample
/// streams. This is the audio-side of the cross-run determinism guarantee
/// described in TR-Determinism: "Audio sample checksums match for every
/// audio buffer between two runs of the same replay".
///
/// The check is self-consistency only (no native VICE reference). It
/// catches implicit non-determinism in the managed SID: any reliance on
/// <c>Random</c>, wall-clock time, mutable static state, ordering of
/// dictionary enumeration, or address-dependent identity comparisons
/// would manifest as divergent sample hashes between two otherwise-
/// identical runs.
/// </summary>
[Trait("Category", "Determinism")]
public sealed class SidDeterminismTests
{
    private static Sid6581 BuildSid()
    {
        var bus = new BasicBus();
        return new Sid6581(bus) { BaseAddress = 0xD400 };
    }

    /// <summary>
    /// Compute a content hash of a sample stream. SHA1 is used purely as a
    /// fast fingerprint - cryptographic strength is not relevant here, only
    /// that two identical float arrays produce identical hashes.
    /// </summary>
    private static byte[] HashSamples(float[] samples)
    {
        var bytes = new byte[samples.Length * 4];
        Buffer.BlockCopy(samples, 0, bytes, 0, bytes.Length);
        return SHA1.HashData(bytes);
    }

    /// <summary>
    /// Apply a stimulus to a SID and capture a sample every <paramref name="sampleEveryNTicks"/>
    /// ticks for <paramref name="totalTicks"/> total ticks. Returns the
    /// raw float sample stream.
    /// </summary>
    private static float[] RunStimulus(Sid6581 sid, Action<Sid6581> applyStimulus, int totalTicks, int sampleEveryNTicks)
    {
        applyStimulus(sid);
        var samples = new float[totalTicks / sampleEveryNTicks];
        int sampleIdx = 0;
        for (int t = 0; t < totalTicks; t++)
        {
            sid.Tick();
            if ((t % sampleEveryNTicks) == 0 && sampleIdx < samples.Length)
            {
                samples[sampleIdx++] = sid.GenerateSample();
            }
        }
        return samples;
    }

    /// <summary>
    /// A "musical" stimulus exercising voice 1 only - simple sawtooth at a
    /// medium frequency with a slow ADSR. Used as the baseline determinism
    /// vector.
    /// </summary>
    private static void BasicSawtoothStimulus(Sid6581 sid)
    {
        sid.Write(0xD418, 0x0F); // master volume = 15
        sid.Write(0xD400, 0x34); // V1 freq lo
        sid.Write(0xD401, 0x12); // V1 freq hi (mid range)
        sid.Write(0xD405, 0x09); // V1 ATK=0, DCY=9
        sid.Write(0xD406, 0x88); // V1 SUS=8, REL=8
        sid.Write(0xD404, 0x21); // V1 control = sawtooth + gate
    }

    /// <summary>
    /// FR/TR: TR-DET-001 (audio sample determinism).
    /// Use case: Two SIDs built from scratch and given the same stimulus
    /// produce byte-identical sample streams.
    /// Acceptance: SHA1 hashes of the two captured streams are equal.
    /// </summary>
    [Fact]
    public void TwoInstances_IdenticalStimulus_ProduceIdenticalSampleStreams()
    {
        var sidA = BuildSid();
        var sidB = BuildSid();

        // PAL master clock 985248 Hz / target 44100 Hz ~= 22.34 cycles per
        // sample - we use 22 here, matching how a host audio frontend would
        // tick the chip per sample boundary.
        const int Total = 10_000;
        const int Every = 22;

        var samplesA = RunStimulus(sidA, BasicSawtoothStimulus, Total, Every);
        var samplesB = RunStimulus(sidB, BasicSawtoothStimulus, Total, Every);

        samplesA.Should().Equal(samplesB);
        HashSamples(samplesA).Should().Equal(HashSamples(samplesB));
    }

    /// <summary>
    /// FR/TR: TR-DET-001 (audio sample determinism).
    /// Use case: A single SID instance, after <see cref="Sid6581.Reset"/>,
    /// must produce the same sample stream as a freshly constructed SID
    /// given the same stimulus. This catches Reset-doesn't-fully-reset
    /// bugs (lingering filter state, accumulator residue, envelope
    /// counters, etc.) that would otherwise allow run N+1 to diverge from
    /// run N even when both are "supposed to start clean".
    /// Acceptance: Hash(samples-after-Reset) == Hash(samples-fresh).
    /// </summary>
    [Fact]
    public void ResetClearsAllState_SecondRunMatchesFresh()
    {
        var sid = BuildSid();
        var fresh = BuildSid();

        const int Total = 8_000;
        const int Every = 22;

        // First run on `sid` - we discard these samples; the goal is to
        // accumulate state that Reset() must clear.
        _ = RunStimulus(sid, BasicSawtoothStimulus, Total, Every);

        sid.Reset();
        var samplesAfterReset = RunStimulus(sid, BasicSawtoothStimulus, Total, Every);
        var samplesFresh = RunStimulus(fresh, BasicSawtoothStimulus, Total, Every);

        samplesAfterReset.Should().Equal(samplesFresh,
            "Reset() must restore the SID to a state byte-equivalent to a fresh instance");
        HashSamples(samplesAfterReset).Should().Equal(HashSamples(samplesFresh));
    }

    /// <summary>
    /// FR/TR: TR-DET-001 (audio sample determinism).
    /// Use case: Different stimulus inputs must produce different sample
    /// streams. This is a sanity check that our hash function actually
    /// distinguishes content (i.e. the determinism test above isn't
    /// trivially passing because the SID emits silence regardless of
    /// input).
    /// Acceptance: Hash(samples-A) != Hash(samples-B) when stimulus
    /// differs in frequency.
    /// </summary>
    [Fact]
    public void DifferentStimulus_ProducesDifferentSampleStreams()
    {
        var sidA = BuildSid();
        var sidB = BuildSid();

        const int Total = 4_000;
        const int Every = 22;

        // Two stimuli that differ in master volume - the $D418 DAC drives
        // a DC offset (FR-SID-010) which is directly audible in
        // GenerateSample() output even before envelopes ramp. Differing
        // volumes guarantee differing output regardless of envelope state.
        var samplesA = RunStimulus(sidA, s =>
        {
            s.Write(0xD418, 0x0F); // volume = 15
            s.Write(0xD400, 0x34);
            s.Write(0xD401, 0x12);
            s.Write(0xD404, 0x21);
        }, Total, Every);
        var samplesB = RunStimulus(sidB, s =>
        {
            s.Write(0xD418, 0x07); // volume = 7 - different DC rail
            s.Write(0xD400, 0x00);
            s.Write(0xD401, 0x40); // different freq too
            s.Write(0xD404, 0x21);
        }, Total, Every);

        HashSamples(samplesA).Should().NotEqual(HashSamples(samplesB),
            "differing stimuli must yield differing sample-stream hashes");
    }

    /// <summary>
    /// FR/TR: TR-DET-001 (audio sample determinism) + FR-SID-008
    /// (hard sync).
    /// Use case: A stimulus that engages hard sync (voice 1 sync from
    /// voice 3, with voice 3 wrapping rapidly) must still be deterministic
    /// across instances. The sync path runs inside Tick() and depends on
    /// previous-cycle MSBs - any timing-derived non-determinism in MSB-
    /// edge detection would surface here.
    /// Acceptance: Hash(samples-A) == Hash(samples-B) for two independent
    /// instances given the same hard-sync stimulus.
    /// </summary>
    [Fact]
    public void HardSyncStimulus_IsDeterministicAcrossRuns()
    {
        var sidA = BuildSid();
        var sidB = BuildSid();

        const int Total = 8_000;
        const int Every = 22;

        Action<Sid6581> hardSyncStimulus = s =>
        {
            s.Write(0xD418, 0x0F);
            // Voice 3 - fast-wrapping pulse, no sync (acts as the modulator).
            s.Write(0xD40E, 0x00);
            s.Write(0xD40F, 0x80);
            s.Write(0xD412, 0x41); // pulse + gate
            // Voice 1 - sawtooth + sync bit set, so it resets every time
            // voice 3 MSB transitions 1->0.
            s.Write(0xD400, 0x55);
            s.Write(0xD401, 0x20);
            s.Write(0xD404, 0x23); // sawtooth + sync + gate
            s.Write(0xD405, 0x09);
            s.Write(0xD406, 0x88);
        };

        var samplesA = RunStimulus(sidA, hardSyncStimulus, Total, Every);
        var samplesB = RunStimulus(sidB, hardSyncStimulus, Total, Every);

        samplesA.Should().Equal(samplesB);
        HashSamples(samplesA).Should().Equal(HashSamples(samplesB));
    }

    /// <summary>
    /// FR/TR: TR-DET-001 (audio sample determinism) + FR-SID-003
    /// (combined waveforms) + FR-SID-006 (ADSR bug) + FR-SID-008
    /// (hard sync).
    /// Use case: The most aggressive deterministic case - combine hard
    /// sync, ring modulation, combined waveforms (Tri+Saw+Pulse), ADSR
    /// rate-register writes that exercise the 15-bit prescaler bug, and
    /// filter routing in one stimulus. If anything in the SID is non-
    /// deterministic, this case is where it surfaces.
    /// Acceptance: Hash(samples-A) == Hash(samples-B) across the entire
    /// 10000-tick stimulus.
    /// </summary>
    [Fact]
    public void CombinedWaveformsAndAdsrChanges_AreDeterministicAcrossRuns()
    {
        var sidA = BuildSid();
        var sidB = BuildSid();

        const int Total = 10_000;
        const int Every = 22;

        Action<Sid6581> stimulus = s =>
        {
            s.Write(0xD418, 0x1F); // master volume 15 + LP mode
            // Filter routing: voice 0 through filter, resonance + cutoff.
            s.Write(0xD415, 0x04);
            s.Write(0xD416, 0x40);
            s.Write(0xD417, 0xA1); // res = 0xA, voice 0 filtered
            // Voice 1: combined waveform (Tri+Saw+Pulse), gate, sync.
            s.Write(0xD400, 0x00);
            s.Write(0xD401, 0x10);
            s.Write(0xD402, 0x00);
            s.Write(0xD403, 0x08);
            s.Write(0xD405, 0x09);
            s.Write(0xD406, 0x80);
            s.Write(0xD404, 0x73); // Tri + Saw + Pulse + Sync + Gate
            // Voice 2: ring mod modulator with sawtooth.
            s.Write(0xD407, 0x80);
            s.Write(0xD408, 0x18);
            s.Write(0xD40B, 0x25); // Saw + Ring + Gate
            // Voice 3: pulse for the sync chain.
            s.Write(0xD40E, 0x10);
            s.Write(0xD40F, 0x40);
            s.Write(0xD412, 0x41); // Pulse + Gate
        };

        // Apply stimulus, but also mutate ADSR registers part-way through
        // to exercise FR-SID-006 (the prescaler is NOT reset on ATK/DCY/SUS/
        // REL writes, which is the ADSR bug). The mutation must be applied
        // identically to both instances.
        var samplesA = new float[Total / Every];
        var samplesB = new float[Total / Every];
        stimulus(sidA);
        stimulus(sidB);

        int idx = 0;
        for (int t = 0; t < Total; t++)
        {
            sidA.Tick();
            sidB.Tick();

            // Mid-run: lower the V1 attack rate (forces the prescaler to
            // stall - the famous ADSR bug). Apply to both instances at the
            // exact same tick so determinism is preserved by construction.
            if (t == 2_500)
            {
                sidA.Write(0xD405, 0x01);
                sidB.Write(0xD405, 0x01);
            }
            // Later: raise SUS and write a fresh REL to walk the envelope.
            if (t == 5_000)
            {
                sidA.Write(0xD406, 0xF8);
                sidB.Write(0xD406, 0xF8);
            }
            // Drop the gate on V1 to enter Release - exercises the state-
            // machine transition + envelope decrement path.
            if (t == 7_500)
            {
                sidA.Write(0xD404, 0x72); // clear gate (was 0x73)
                sidB.Write(0xD404, 0x72);
            }

            if ((t % Every) == 0 && idx < samplesA.Length)
            {
                samplesA[idx] = sidA.GenerateSample();
                samplesB[idx] = sidB.GenerateSample();
                idx++;
            }
        }

        samplesA.Should().Equal(samplesB);
        HashSamples(samplesA).Should().Equal(HashSamples(samplesB));
    }
}
