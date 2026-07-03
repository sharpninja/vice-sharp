using System.Buffers.Binary;
using ViceSharp.Abstractions;
using ViceSharp.Chips.Audio;
using ViceSharp.Core;
using Xunit;

namespace ViceSharp.TestHarness;

/// <summary>
/// PLAN-VICEPARITY-001 P0-8: FAITHFUL (green-now regression lock) parity tests
/// for the top-level SID requirements in artifacts/vice-parity-requirements/
/// requirements.yaml: FR-SID-VOICE, FR-SID-MIXVOL, FR-SID-OUTPUT, FR-SID-CLOCK,
/// FR-SID-DATABUS, FR-SID-POT, FR-SID-8580, FR-SID-FILTER-6581,
/// FR-SID-FILTER-8580, FR-SID-EXTFILT, FR-SID-CUTOFFDAC and
/// FR-SID-FILTER-CLOCK. Only the FAITHFUL acceptance criteria are authored
/// here (one test method per AC); every DIVERGENT AC in these FRs is a
/// red-now remediation target owned by later slices. Of the twelve FRs, only
/// FR-SID-FILTER-6581 (AC-17/18), FR-SID-VOICE (AC-07/09/10), FR-SID-MIXVOL
/// (AC-02) and FR-SID-CLOCK (AC-04/10/11) carry FAITHFUL ACs; the other eight
/// FRs are entirely DIVERGENT and therefore contribute no test here.
///
/// Intentionally NOT authored: TEST-SID-FILTER-6581-19 (FR-SID-FILTER-6581
/// AC-19, "writeMODE_VOL mode=v&amp;0xf0, vol=v&amp;0xf"). The managed write
/// path (Sid6581.cs Write case 0x18) captures the mode as value&amp;0x70,
/// dropping bit 7 (V3OFF), so managed does not match the FAITHFUL statement;
/// the same bit-7 drop is already tracked as the DIVERGENT FR-SID-MIXVOL
/// AC-01 (TEST-SID-MIXVOL-01, finding 19). Locking value&amp;0x70 under a
/// FAITHFUL tag would contradict that remediation target, so the disputed AC
/// is excluded rather than faked.
///
/// All tests are managed-only and deterministic: fixed register stimulus,
/// fixed tick counts, exact Assert.Equal expectations derived from the reSID
/// sources (native/vice/vice/src/resid) and verified against the managed
/// implementation (src/ViceSharp.Chips/Audio/Sid6581.cs).
/// </summary>
public sealed class SidTopFaithfulParityTests
{
    private static Sid6581 BuildSid()
    {
        var bus = new BasicBus();
        return new Sid6581(bus) { BaseAddress = 0xD400 };
    }

    private static FilterRegisterProbe BuildProbe()
    {
        var bus = new BasicBus();
        return new FilterRegisterProbe(bus) { BaseAddress = 0xD400 };
    }

    /// <summary>
    /// Exposes the protected filter register latches of <see cref="Sid6581"/>
    /// so the FAITHFUL register-decode locks can assert the exact stored
    /// values (the fields mirror reSID Filter::fc / res / filt / mode).
    /// </summary>
    private sealed class FilterRegisterProbe : Sid6581
    {
        public FilterRegisterProbe(IBus bus) : base(bus) { }

        /// <summary>The 11-bit composed cutoff register (reSID Filter::fc).</summary>
        public int Cutoff => _filterCutoff;

        /// <summary>The 4-bit resonance nibble (reSID Filter::res).</summary>
        public byte Resonance => _filterResonance;

        /// <summary>Composite control: bits 0-3 routing (reSID filt), bits 4-6 mode.</summary>
        public byte Control => _filterControl;
    }

    /// <summary>Register address of <paramref name="offset"/> within a voice block (7 bytes per voice from $D400).</summary>
    private static ushort VoiceReg(int voice, int offset) => (ushort)(0xD400 + (voice * 7) + offset);

    /// <summary>
    /// Reads the per-voice engine state through the public time-travel
    /// capture surface (Sid6581.State.cs): 32 register bytes, then per voice
    /// accumulator(4, little endian) + envelope(1) + adsr(1).
    /// </summary>
    private static (uint Acc0, uint Acc1, uint Acc2, byte Env0, byte Env1, byte Env2) CaptureVoiceState(Sid6581 sid)
    {
        var state = new byte[sid.StateSize];
        sid.CaptureState(state);
        return (
            BinaryPrimitives.ReadUInt32LittleEndian(state.AsSpan(32, 4)),
            BinaryPrimitives.ReadUInt32LittleEndian(state.AsSpan(38, 4)),
            BinaryPrimitives.ReadUInt32LittleEndian(state.AsSpan(44, 4)),
            state[36],
            state[42],
            state[48]);
    }

    /// <summary>
    /// Canonical voice-3 stimulus: frequency $2000 (accumulator gains 2^13 per
    /// cycle), attack 0 / decay 0 / sustain 0 / release 0, sawtooth + gate.
    /// ADSR registers are written before the control register, mirroring how
    /// player code programs the chip.
    /// </summary>
    private static void ApplyVoice3SawtoothGate(Sid6581 sid)
    {
        sid.Write(0xD40E, 0x00); // voice 3 freq lo
        sid.Write(0xD40F, 0x20); // voice 3 freq hi
        sid.Write(0xD413, 0x00); // attack 0, decay 0
        sid.Write(0xD414, 0x00); // sustain 0, release 0
        sid.Write(0xD412, 0x21); // sawtooth + gate
    }

    /// <summary>
    /// FR: FR-SID-FILTER-6581 AC-17 (TEST-SID-FILTER-6581-17), FAITHFUL lock,
    /// PLAN-VICEPARITY-001 P0-8.
    /// Use case: player code writes $D415/$D416 in any order and the chip must
    /// compose the 11-bit filter cutoff exactly like reSID Filter::writeFC_LO /
    /// writeFC_HI (filter8580new.cc:722-732): fc = (fc &amp; 0x7f8) | (lo &amp; 7)
    /// and fc = ((hi &lt;&lt; 3) &amp; 0x7f8) | (fc &amp; 7). Managed mirror:
    /// Sid6581.cs Write cases 0x15/0x16.
    /// Acceptance: from a reset chip the write sequence $D415=FF, $D416=FF,
    /// $D415=00, $D416=A5, $D415=3A leaves the composed cutoff at exactly
    /// 0x007, 0x7FF, 0x7F8, 0x528, 0x52A after each step (upper bits of the
    /// FC_LO byte are discarded; each write preserves the other half).
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-FILTER-6581-17", ParityTag.Faithful)]
    public void WriteFcLoAndFcHi_ComposeElevenBitCutoffExactlyAsReSid()
    {
        var probe = BuildProbe();

        probe.Write(0xD415, 0xFF); // lo bits only: 0xFF masked to 7
        Assert.Equal(0x007, probe.Cutoff);

        probe.Write(0xD416, 0xFF); // hi byte shifts into bits 3-10, keeps low 3
        Assert.Equal(0x7FF, probe.Cutoff);

        probe.Write(0xD415, 0x00); // clears only the low 3 bits
        Assert.Equal(0x7F8, probe.Cutoff);

        probe.Write(0xD416, 0xA5); // 0xA5 shifted left 3 = 0x528, low bits kept (0)
        Assert.Equal(0x528, probe.Cutoff);

        probe.Write(0xD415, 0x3A); // 0x3A masked to 2, upper FC_LO bits ignored
        Assert.Equal(0x52A, probe.Cutoff);
    }

    /// <summary>
    /// FR: FR-SID-FILTER-6581 AC-18 (TEST-SID-FILTER-6581-18), FAITHFUL lock,
    /// PLAN-VICEPARITY-001 P0-8.
    /// Use case: a $D417 write must decode exactly like reSID
    /// Filter::writeRES_FILT (filter8580new.cc:734-740): res = (v &gt;&gt; 4)
    /// &amp; 0x0f and filt = v &amp; 0x0f. This locks the register decode only;
    /// everything downstream of res/filt is DIVERGENT and owned by the other
    /// FR-SID-FILTER-6581 ACs. Managed mirror: Sid6581.cs Write case 0x17.
    /// Acceptance: writing $A5 stores resonance 0x0A and routing nibble 0x05;
    /// writing $5A stores resonance 0x05 and routing nibble 0x0A; writing $00
    /// clears both. All comparisons are exact.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-FILTER-6581-18", ParityTag.Faithful)]
    public void WriteResFilt_DecodesResonanceAndRoutingNibblesExactlyAsReSid()
    {
        var probe = BuildProbe();

        probe.Write(0xD417, 0xA5);
        Assert.Equal(0x0A, probe.Resonance);
        Assert.Equal(0x05, probe.Control & 0x0F);

        probe.Write(0xD417, 0x5A);
        Assert.Equal(0x05, probe.Resonance);
        Assert.Equal(0x0A, probe.Control & 0x0F);

        probe.Write(0xD417, 0x00);
        Assert.Equal(0x00, probe.Resonance);
        Assert.Equal(0x00, probe.Control & 0x0F);
    }

    /// <summary>
    /// FR: FR-SID-VOICE AC-07 (TEST-SID-VOICE-07), FAITHFUL lock,
    /// PLAN-VICEPARITY-001 P0-8, re-rigged in slice S3.
    /// Use case: hard-sync wiring must match reSID SID::SID() (sid.cc:74-76):
    /// voice 0 syncs from voice 2, voice 1 from voice 0, voice 2 from voice 1.
    /// Managed mirror: Sid6581.cs SynchronizeOscillators (and the ring-mod
    /// modulator pick (i + 2) % 3).
    /// Acceptance: each leg first pins all three accumulators to zero through
    /// the CTRL test bit (FAITHFUL TEST-SID-WAVE-TESTBIT-01), so the leg is
    /// independent of the power-on accumulator seed (FR-SID-WAVE-ACC AC-05),
    /// and the captured accumulators are compared at the architectural 24-bit
    /// width (FR-SID-WAVE-ACC AC-02), so the leg is independent of the stored
    /// width. For each wiring leg the source voice runs freq $FFFF so its
    /// accumulator bit 23 falls exactly once in 300 cycles (rises at cycle
    /// 129; 256 * 0xFFFF = 0xFFFF00 still has bit 23 set, 257 * 0xFFFF =
    /// 0x010100FF is 0x0100FF modulo 2^24, so the 1-to-0 edge lands on cycle
    /// 257 whether or not the store is masked). The dependent voice (freq
    /// $0100, SYNC bit set) is reset on that edge and re-accumulates for the
    /// remaining 43 cycles: exactly 43 * 0x100 = 0x2B00. The source ends at
    /// exactly (300 * 0xFFFF) mod 2^24 = 0x2BFED4 and the uninvolved third
    /// voice (freq $0010, no edge in 300 cycles) ends at exactly 300 * 0x10 =
    /// 0x12C0, proving the dependent was reset by its designated source and
    /// nothing else.
    /// S3 relock: previously asserted the raw 32-bit from-zero captures
    /// (source 0x012BFED4, i.e. the unmasked store from a zero power-on
    /// seed); the same wiring subject is now pinned via test-bit zeroing and
    /// 24-bit captures (source 0x2BFED4 = 0x012BFED4 &amp; 0xFFFFFF; the
    /// dependent/third values 0x2B00/0x12C0 are unchanged), which stays
    /// bit-exact across the FR-SID-WAVE-ACC AC-02/AC-05 remediation.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-VOICE-07", ParityTag.Faithful)]
    public void HardSyncSourceTopology_MatchesReSidVoiceWiring()
    {
        Assert.Equal((0x2B00u, 0x2BFED4u, 0x12C0u), RunHardSyncLeg(dependent: 0, source: 2, third: 1));
        Assert.Equal((0x2B00u, 0x2BFED4u, 0x12C0u), RunHardSyncLeg(dependent: 1, source: 0, third: 2));
        Assert.Equal((0x2B00u, 0x2BFED4u, 0x12C0u), RunHardSyncLeg(dependent: 2, source: 1, third: 0));
    }

    private static (uint DependentAcc, uint SourceAcc, uint ThirdAcc) RunHardSyncLeg(int dependent, int source, int third)
    {
        var sid = BuildSid();

        // Pin all three accumulators to zero through the CTRL test bit
        // (FAITHFUL mechanism, TEST-SID-WAVE-TESTBIT-01) so the leg does not
        // depend on the power-on accumulator seed.
        for (var v = 0; v < 3; v++)
            sid.Write(VoiceReg(v, 4), 0x08);
        sid.Tick();
        for (var v = 0; v < 3; v++)
            sid.Write(VoiceReg(v, 4), 0x00);

        // Source sweeps fast enough that its accumulator MSB falls exactly
        // once within the 300-cycle window (cycle 257, see the test doc).
        sid.Write(VoiceReg(source, 0), 0xFF);
        sid.Write(VoiceReg(source, 1), 0xFF);

        // Dependent advances 0x100 per cycle and carries only the SYNC bit.
        sid.Write(VoiceReg(dependent, 0), 0x00);
        sid.Write(VoiceReg(dependent, 1), 0x01);
        sid.Write(VoiceReg(dependent, 4), 0x02);

        // Third voice advances 0x10 per cycle and must remain untouched.
        sid.Write(VoiceReg(third, 0), 0x10);
        sid.Write(VoiceReg(third, 1), 0x00);

        for (var t = 0; t < 300; t++)
            sid.Tick();

        // Compare at the architectural 24-bit accumulator width (reSID
        // wave.h:155); the stored-width divergence is FR-SID-WAVE-ACC AC-02.
        var state = CaptureVoiceState(sid);
        var accumulators = new[] { state.Acc0 & 0xFFFFFFu, state.Acc1 & 0xFFFFFFu, state.Acc2 & 0xFFFFFFu };
        return (accumulators[dependent], accumulators[source], accumulators[third]);
    }

    /// <summary>
    /// FR: FR-SID-VOICE AC-09 (TEST-SID-VOICE-09), FAITHFUL lock,
    /// PLAN-VICEPARITY-001 P0-8, re-rigged in slice S3.
    /// Use case: reSID Voice::reset() (voice.cc:121-125) fans reset out to
    /// both halves of the voice: wave.reset() then envelope.reset(). The
    /// managed mirror (Sid6581.cs Reset()) re-parks the register file and
    /// per-voice wave state per reSID wave.cc:301-332 and the envelope
    /// machinery via Env.Reset(), which preserves the envelope counter and
    /// env3 latch (envelope.cc:189; FR-SID-ENV AC-07).
    /// Acceptance: all three accumulators are first pinned to zero through
    /// the CTRL test bit (FAITHFUL TEST-SID-WAVE-TESTBIT-01, one tick), so
    /// the rig does not depend on the power-on seed (FR-SID-WAVE-ACC AC-05).
    /// After voice 3 then runs 1000 cycles at freq $2000 with attack 0 and
    /// gate on, OSC3 reads exactly 0x7D (1000 * 0x2000 = 0x7D0000; sawtooth
    /// output bits 16-23) and ENV3 exactly tracks a lockstep ReSidEnvelope
    /// reference with the identical history (one idle pre-clock, then the
    /// gated ramp: exactly 0xE6). The wave state is then parked at the
    /// reset-invariant point (test bit held for one tick pins every
    /// accumulator at zero, so reSID's accumulator-preserving reset,
    /// wave.cc:301-303 / FR-SID-WAVE-ACC AC-06, and the legacy zeroing reset
    /// coincide) and Reset() is called. OSC3 then reads exactly 0x00 (reset
    /// clears the osc3 latch, wave.cc:330), the register readback is zeroed,
    /// the captured 24-bit accumulators are exactly (0, 0, 0), ENV3 still
    /// reads its reference-preserved value exactly, and the captured
    /// counters equal the reference counters exactly (idle voices released
    /// the 0xaa power-up seed through the exponential ladder for the same
    /// 1002 clocks). A 500-cycle re-gated replay produces OSC3 sequences
    /// exactly equal to a freshly constructed chip whose accumulators were
    /// pinned the same way (wave state fully reset, ending 0x3E) and ENV3
    /// sequences exactly tracking the lockstep references; both trajectories
    /// end at exactly the reference values.
    /// S3 relock: previously the rig started from the legacy zero power-on
    /// accumulators, asserted the raw captures (0, 0, 0) right after a
    /// mid-run Reset() (pinning the legacy accumulator zeroing that
    /// FR-SID-WAVE-ACC AC-06 remediates), and pinned ENV3 literals for the
    /// unprefixed history (0xE6/0x4D). The same reset-fans-to-both-halves
    /// subject is now pinned with the accumulators parked at zero before
    /// Reset(), which stays bit-exact whether reset preserves or clears the
    /// accumulator, and the envelope literals are derived from the lockstep
    /// ReSidEnvelope references driven through the re-rigged history.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-VOICE-09", ParityTag.Faithful)]
    public void Reset_RestoresWaveAndEnvelopePowerOnStatePerVoice()
    {
        var sid = BuildSid();

        // Reference envelope for voice 3, driven through the exact same
        // write/clock history as the chip (the derivation vehicle for every
        // envelope literal in this lock).
        var resetReference = new Sid6581.ReSidEnvelope();
        resetReference.PowerUp();

        // Pin all three accumulators to zero via the test bit (seed-robust
        // rig; the CTRL writes carry gate 0, so no envelope transition).
        for (var v = 0; v < 3; v++)
            sid.Write(VoiceReg(v, 4), 0x08);
        sid.Tick();
        resetReference.Clock();
        for (var v = 0; v < 3; v++)
            sid.Write(VoiceReg(v, 4), 0x00);

        ApplyVoice3SawtoothGate(sid);
        resetReference.WriteAttackDecay(0x00);
        resetReference.WriteSustainRelease(0x00);
        resetReference.WriteControl(0x01);
        for (var t = 0; t < 1000; t++)
        {
            sid.Tick();
            resetReference.Clock();
        }

        // Accumulated pre-reset state, asserted exactly so the reset below is
        // proven to have destroyed real wave progress while preserving the
        // envelope counter.
        Assert.Equal(0x7D, sid.Read(0xD41B));
        Assert.Equal(resetReference.Env3, sid.Read(0xD41C));
        Assert.Equal(0xE6, sid.Read(0xD41C));

        // Park the wave state at the reset-invariant point: one held test
        // cycle pins every accumulator at zero, so a reSID
        // accumulator-preserving reset and the legacy zeroing reset produce
        // the identical post-reset accumulators. Voice 3's gate drops with
        // the same write (mirrored into the reference).
        for (var v = 0; v < 3; v++)
            sid.Write(VoiceReg(v, 4), 0x08);
        resetReference.WriteControl(0x08);
        sid.Tick();
        resetReference.Clock();

        sid.Reset();
        resetReference.Reset();

        // Wave half: osc3 latch, register file and (parked-at-zero)
        // accumulators all read zero. Envelope half: machinery re-parked in
        // RELEASE with the counters preserved (envelope.cc:189).
        Assert.Equal(0x00, sid.Read(0xD41B));
        Assert.Equal(resetReference.Env3, sid.Read(0xD41C));
        Assert.Equal(0xE6, sid.Read(0xD41C));
        Assert.Equal(0x00, sid.Read(0xD40F));
        Assert.Equal(0x00, sid.Read(0xD412));
        var state = CaptureVoiceState(sid);
        Assert.Equal((0u, 0u, 0u), (state.Acc0 & 0xFFFFFFu, state.Acc1 & 0xFFFFFFu, state.Acc2 & 0xFFFFFFu));
        Assert.Equal(resetReference.EnvelopeCounter, state.Env2);
        Assert.Equal((0x4D, 0x4D, 0xE5), ((int)state.Env0, (int)state.Env1, (int)state.Env2));

        // Replay: the wave half of the reset chip must be indistinguishable
        // from a fresh chip whose accumulators were pinned the same way
        // (identical OSC3 streams); the envelope half must track lockstep
        // ReSidEnvelope references that went through the same histories.
        var fresh = BuildSid();
        var freshReference = new Sid6581.ReSidEnvelope();
        freshReference.PowerUp();
        fresh.Write(0xD412, 0x08);
        fresh.Tick();
        freshReference.Clock();

        ApplyVoice3SawtoothGate(sid);
        ApplyVoice3SawtoothGate(fresh);
        resetReference.WriteAttackDecay(0x00);
        resetReference.WriteSustainRelease(0x00);
        resetReference.WriteControl(0x01);
        freshReference.WriteAttackDecay(0x00);
        freshReference.WriteSustainRelease(0x00);
        freshReference.WriteControl(0x01);

        const int ReplayCycles = 500;
        var oscReset = new byte[ReplayCycles];
        var oscFresh = new byte[ReplayCycles];
        for (var t = 0; t < ReplayCycles; t++)
        {
            sid.Tick();
            fresh.Tick();
            resetReference.Clock();
            freshReference.Clock();
            oscReset[t] = sid.Read(0xD41B);
            oscFresh[t] = fresh.Read(0xD41B);
            Assert.Equal(resetReference.Env3, sid.Read(0xD41C));
            Assert.Equal(freshReference.Env3, fresh.Read(0xD41C));
        }

        Assert.Equal(oscFresh, oscReset);
        Assert.Equal(0x3E, oscReset[ReplayCycles - 1]); // 500 * 0x2000 = 0x3E8000, sawtooth bits 16-23
        Assert.Equal(resetReference.Env3, sid.Read(0xD41C));
        Assert.Equal(freshReference.Env3, fresh.Read(0xD41C));
        Assert.Equal(0xE2, sid.Read(0xD41C));   // reset leg: 0xE6 to the 0xFF flip, then decay
        Assert.Equal(0xE1, fresh.Read(0xD41C)); // fresh leg: 0xaa seed plus 55 attack steps
    }

    /// <summary>
    /// FR: FR-SID-VOICE AC-10 (TEST-SID-VOICE-10), FAITHFUL lock,
    /// PLAN-VICEPARITY-001 P0-8.
    /// Use case: reSID defaults every voice to the MOS6581 model
    /// (voice.cc:30-33: the Voice constructor calls set_chip_model(MOS6581),
    /// selecting wave_zero 0x380). The managed mirror is the canonical
    /// construction path: SidFactory.Create with no machine profile builds a
    /// Sid6581, which is exactly what the C64 machine wires
    /// (Commodore64.cs passes profile: null).
    /// Acceptance: SidFactory.Create(bus, profile: null, audioBackend: null,
    /// masterClockHz: 985248.0) returns an instance whose runtime type is
    /// exactly Sid6581 (not the Sid8580 or Sid8580D die variants).
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-VOICE-10", ParityTag.Faithful)]
    public void DefaultSidModel_IsMos6581()
    {
        var sid = SidFactory.Create(new BasicBus(), profile: null, audioBackend: null, masterClockHz: 985248.0);

        Assert.Equal(typeof(Sid6581), sid.GetType());
    }

    /// <summary>
    /// FR: FR-SID-MIXVOL AC-02 (TEST-SID-MIXVOL-02), FAITHFUL lock,
    /// PLAN-VICEPARITY-001 P0-8.
    /// Use case: reSID Filter::writeRES_FILT (filter8580new.cc:734-740) only
    /// touches res and filt: res = (v &gt;&gt; 4) &amp; 0x0f, filt = v &amp;
    /// 0x0f; the mode and vol latched from $D418 are separate state and must
    /// survive a $D417 write. Managed mirror: Sid6581.cs Write case 0x17
    /// updates the resonance latch and only the low nibble of the composite
    /// control byte. The preloaded $D418 value keeps bit 7 clear so the
    /// asserted mode bits are identical under reSID's 0xF0 mask and the
    /// managed 0x70 mask (the bit-7 difference is the DIVERGENT
    /// TEST-SID-MIXVOL-01, not this lock).
    /// Acceptance: after $D418=7A (vol 0x0A, LP+BP+HP mode), writing $D417=F0
    /// stores resonance exactly 0x0F with routing 0x0 and control exactly
    /// 0x70; writing $D417=0F stores resonance exactly 0x00 with control
    /// exactly 0x7F; the volume nibble still reads exactly 0x0A afterwards.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-MIXVOL-02", ParityTag.Faithful)]
    public void WriteResFilt_SplitsNibblesAndLeavesModeVolUntouched()
    {
        var probe = BuildProbe();

        probe.Write(0xD418, 0x7A); // vol 0x0A, LP+BP+HP mode bits, bit 7 clear
        Assert.Equal(0x0A, probe.MasterVolume);
        Assert.Equal(0x70, probe.Control & 0xF0);

        probe.Write(0xD417, 0xF0);
        Assert.Equal(0x0F, probe.Resonance);
        Assert.Equal(0x70, probe.Control); // routing 0, mode preserved

        probe.Write(0xD417, 0x0F);
        Assert.Equal(0x00, probe.Resonance);
        Assert.Equal(0x7F, probe.Control); // routing 0xF, mode preserved

        Assert.Equal(0x0A, probe.MasterVolume); // vol untouched by $D417 writes
    }

    /// <summary>
    /// FR: FR-SID-CLOCK AC-04 (TEST-SID-CLOCK-04), FAITHFUL lock,
    /// PLAN-VICEPARITY-001 P0-8.
    /// Use case: reSID SID::clock clocks all three envelope generators every
    /// cycle (sid.cc:770-772 batched; sid.h:205-208 single cycle). The managed
    /// mirror clocks each voice's ReSidEnvelope exactly once per Tick
    /// (Sid6581.cs ClockEnvelopes/ProcessEnvelope), i.e. 1:1 with phi2,
    /// matching the oracle's SAMPLE_FAST clocking.
    /// Acceptance: with identical attack 0 programs gated on all three voices
    /// at cycle 0, after exactly 900 ticks ENV3 reads exactly 0xF1 and the
    /// captured per-voice envelope counters are exactly (0xF1, 0xF1, 0xF1):
    /// from the 0xaa power-up seed (envelope.cc:176, FR-SID-ENV AC-08) the
    /// reSID single-cycle envelope first steps at cycle 12 and then every
    /// rate period + 1 = 9 cycles, reaches 0xFF at cycle 768 (85 attack
    /// steps), flips to DECAY_SUSTAIN, and decays 14 steps (cycles 777..894)
    /// to 0xF1 by cycle 900; all three voices advance in lockstep because
    /// each is clocked once per tick.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-CLOCK-04", ParityTag.Faithful)]
    public void EveryPhi2Tick_ClocksAllThreeEnvelopesExactlyOnce()
    {
        var sid = BuildSid();
        for (var v = 0; v < 3; v++)
        {
            sid.Write(VoiceReg(v, 5), 0x00); // attack 0, decay 0
            sid.Write(VoiceReg(v, 6), 0x00); // sustain 0, release 0
            sid.Write(VoiceReg(v, 4), 0x01); // gate on, no waveform needed
        }

        for (var t = 0; t < 900; t++)
            sid.Tick();

        Assert.Equal(0xF1, sid.Read(0xD41C));
        var state = CaptureVoiceState(sid);
        Assert.Equal((0xF1, 0xF1, 0xF1), ((int)state.Env0, (int)state.Env1, (int)state.Env2));
    }

    /// <summary>
    /// FR: FR-SID-CLOCK AC-10 (TEST-SID-CLOCK-10), FAITHFUL lock,
    /// PLAN-VICEPARITY-001 P0-8.
    /// Use case: reSID advances the whole SID once per phi2 cycle (sid.h
    /// SID::clock()); the managed chip must therefore register on the system
    /// clock at phi2 with divisor 1 (Sid6581.cs ClockDivisor/Phase, per
    /// BUG-SIDAUDIO-001 the rate tables are phi2-cycle units).
    /// Acceptance: ClockDivisor is exactly 1 and Phase is exactly
    /// ClockPhase.Phi2.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-CLOCK-10", ParityTag.Faithful)]
    public void SidClock_RunsAtPhi2WithDivisorOne()
    {
        var sid = BuildSid();

        Assert.Equal(1u, sid.ClockDivisor);
        Assert.Equal(ClockPhase.Phi2, sid.Phase);
    }

    /// <summary>
    /// FR: FR-SID-CLOCK AC-11 (TEST-SID-CLOCK-11), FAITHFUL lock,
    /// PLAN-VICEPARITY-001 P0-8, re-rigged in slice S3.
    /// Use case: reSID clocks envelopes before oscillators within a cycle
    /// (sid.h:205-212), the managed Tick advances the accumulator before the
    /// envelope per voice; the orders are equivalent because neither engine
    /// reads the other's state within the cycle. This lock proves that
    /// order-independence functionally: the envelope trajectory must not
    /// depend on oscillator frequency, and the oscillator trajectory must not
    /// depend on envelope gating.
    /// Acceptance: two chips with identical gate programs but frequencies
    /// $0000 vs $FFFF produce exactly equal per-cycle ENV3 sequences over 405
    /// cycles (final read exactly 0xD6: the 0xaa power-up seed plus the 44
    /// attack-0 steps that land at cycles 12 + 9k up to 405); two chips at
    /// freq $2000 with sawtooth selected and gate on vs gate off (CTRL $21
    /// vs $20), accumulators pinned to zero via the test bit first, produce
    /// exactly equal per-cycle OSC3 sequences over 405 cycles (final read
    /// exactly 0x32: sawtooth output = accumulator bits 16-23, with
    /// 405 * 0x2000 = 0x32A000).
    /// S3 relock: the oscillator half previously probed OSC3 with NO
    /// waveform selected (CTRL $01 vs $00), pinning the legacy waveform-0
    /// phase readback that FR-SID-OSC3ENV3 AC-07 remediates (waveform 0
    /// reads the fading floating-DAC latch, wave.h:499-504), and started
    /// from the legacy zero power-on accumulator that FR-SID-WAVE-ACC AC-05
    /// remediates. The same envelope-independence subject is now pinned
    /// through the sawtooth-selected readback (osc3 = accumulator top bits,
    /// wave.cc:97), which produces the identical 0x32 literal on both the
    /// legacy and the reSID readback paths.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-CLOCK-11", ParityTag.Faithful)]
    public void EnvelopeAndOscillator_AdvanceOrderIndependentlyWithinACycle()
    {
        const int Cycles = 405;

        // Envelope must be oscillator-independent: same ADSR + gate, wildly
        // different frequencies, identical ENV3 streams.
        var still = BuildSid();
        var fast = BuildSid();
        still.Write(0xD40E, 0x00);
        still.Write(0xD40F, 0x00);
        fast.Write(0xD40E, 0xFF);
        fast.Write(0xD40F, 0xFF);
        foreach (var sid in new[] { still, fast })
        {
            sid.Write(0xD413, 0x00); // attack 0, decay 0
            sid.Write(0xD414, 0x00); // sustain 0, release 0
            sid.Write(0xD412, 0x01); // gate only
        }

        var envStill = new byte[Cycles];
        var envFast = new byte[Cycles];
        for (var t = 0; t < Cycles; t++)
        {
            still.Tick();
            fast.Tick();
            envStill[t] = still.Read(0xD41C);
            envFast[t] = fast.Read(0xD41C);
        }

        Assert.Equal(envStill, envFast);
        Assert.Equal(0xD6, envStill[Cycles - 1]);

        // Oscillator must be envelope-independent: same frequency and
        // sawtooth selection, gate on vs gate off, identical OSC3 streams.
        // The voice-3 accumulator is pinned to zero via the test bit first
        // (FAITHFUL TEST-SID-WAVE-TESTBIT-01) so the closed form does not
        // depend on the power-on seed.
        var gated = BuildSid();
        var silent = BuildSid();
        foreach (var sid in new[] { gated, silent })
        {
            sid.Write(0xD412, 0x08); // test bit: pin the accumulator to zero
            sid.Tick();
            sid.Write(0xD40E, 0x00); // freq lo
            sid.Write(0xD40F, 0x20); // freq hi ($2000)
            sid.Write(0xD413, 0x00);
            sid.Write(0xD414, 0x00);
        }
        gated.Write(0xD412, 0x21);  // sawtooth + gate
        silent.Write(0xD412, 0x20); // sawtooth, no gate

        var oscGated = new byte[Cycles];
        var oscSilent = new byte[Cycles];
        for (var t = 0; t < Cycles; t++)
        {
            gated.Tick();
            silent.Tick();
            oscGated[t] = gated.Read(0xD41B);
            oscSilent[t] = silent.Read(0xD41B);
        }

        Assert.Equal(oscGated, oscSilent);
        Assert.Equal(0x32, oscGated[Cycles - 1]);
    }
}
