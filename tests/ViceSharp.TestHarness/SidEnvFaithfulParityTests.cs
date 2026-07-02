namespace ViceSharp.TestHarness;

using ViceSharp.Chips.Audio;
using ViceSharp.Core;
using Xunit;
using EnvState = ViceSharp.Chips.Audio.Sid6581.ReSidEnvelope.EnvStateE;
using ReSidEnv = ViceSharp.Chips.Audio.Sid6581.ReSidEnvelope;

/// <summary>
/// PLAN-VICEPARITY-001 P0-8 / FR-SID-ENV / TR-SID-ORACLE-001.
/// FAITHFUL (green-now regression lock) parity tests for the ADSR envelope
/// generator, one test method per FAITHFUL acceptance criterion in
/// artifacts/vice-parity-requirements/requirements.yaml. The spec is reSID's
/// single-cycle EnvelopeGenerator (native/vice/vice/src/resid/envelope.h and
/// envelope.cc); the managed subject is the internal Sid6581.ReSidEnvelope
/// struct (verbatim port), reached directly via InternalsVisibleTo. The three
/// DIVERGENT criteria (AC-07 reset-preserve, AC-08 power-up 0xaa, AC-50
/// model_dac output) are remediation targets and are intentionally absent.
/// All assertions are bit-exact equality; no tolerances.
/// </summary>
public sealed class SidEnvFaithfulParityTests
{
    /// <summary>
    /// reSID rate_counter_period[16] (resid/envelope.cc:72-89): the exact rate
    /// counter comparison values, one per ATK/DCY/SUS/REL nibble.
    /// </summary>
    private static readonly int[] ReSidRatePeriods =
        { 8, 31, 62, 94, 148, 219, 266, 312, 391, 976, 1953, 3125, 3906, 11719, 19531, 31250 };

    /// <summary>Fresh envelope in the reSID reset state.</summary>
    private static ReSidEnv NewEnv()
    {
        var env = default(ReSidEnv);
        env.Reset();
        return env;
    }

    /// <summary>
    /// Arms a single pending DECAY_SUSTAIN envelope step from
    /// <paramref name="landing"/> + 1 and clocks once, so the counter lands
    /// exactly on <paramref name="landing"/> and set_exponential_counter()
    /// runs on that value. ExponentialCounterPeriod is pre-set to the sentinel
    /// 99 so both a table hit and the deliberate absence of a default case are
    /// observable (resid/envelope.h:386-419).
    /// </summary>
    private static ReSidEnv StepDecayLandingOn(byte landing)
    {
        var env = NewEnv();
        env.State = EnvState.DecaySustain;
        env.EnvelopeCounter = (byte)((landing + 1) & 0xff);
        env.EnvelopePipeline = 1;
        env.HoldZero = false;
        env.ExponentialCounterPeriod = 99;
        env.Clock();
        return env;
    }

    /// <summary>
    /// FR: FR-SID-ENV, TR: TR-SID-ORACLE-001, TEST: TEST-SID-ENV-01.
    /// Use case: the ADSR rate table drives every envelope step interval, so
    /// all 16 rate_counter_period entries must match reSID exactly.
    /// Acceptance: for each release nibble 0..15 written while in RELEASE,
    /// RatePeriod equals rate_counter_period[] = {8,31,62,94,148,219,266,312,
    /// 391,976,1953,3125,3906,11719,19531,31250} (resid/envelope.cc:72-89),
    /// bit-exact.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-ENV-01", ParityTag.Faithful)]
    public void Env_RateCounterPeriods_MatchReSidTable()
    {
        var env = NewEnv();
        for (var rate = 0; rate < 16; rate++)
        {
            // Reset leaves State == RELEASE, so writeSUSTAIN_RELEASE reloads
            // RatePeriod from the table entry selected by the release nibble.
            env.WriteSustainRelease((byte)rate);
            Assert.Equal(ReSidRatePeriods[rate], env.RatePeriod);
        }
    }

    /// <summary>
    /// FR: FR-SID-ENV, TR: TR-SID-ORACLE-001, TEST: TEST-SID-ENV-02.
    /// Use case: DECAY_SUSTAIN compares the envelope counter against
    /// sustain_level[sustain]; both nibbles of the 4-bit sustain value form the
    /// 8-bit level.
    /// Acceptance: for each sustain nibble s, an exponential-pipeline fire with
    /// counter == 0x11*s does NOT requeue the envelope pipeline (counter equals
    /// sustain_level[s]) while counter == 0x11*s ^ 0x01 does, pinning
    /// sustain_level[16] = {0x00,0x11,...,0xff} (resid/envelope.cc:129-146)
    /// bit-exact.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-ENV-02", ParityTag.Faithful)]
    public void Env_SustainLevels_MatchReSidTable()
    {
        for (var s = 0; s < 16; s++)
        {
            byte level = (byte)(0x11 * s);

            var match = NewEnv();
            match.State = EnvState.DecaySustain;
            match.Sustain = (byte)s;
            match.EnvelopeCounter = level;
            match.HoldZero = false;
            match.ExponentialPipeline = 1;
            match.Clock();
            Assert.Equal(0, match.EnvelopePipeline);

            var mismatch = NewEnv();
            mismatch.State = EnvState.DecaySustain;
            mismatch.Sustain = (byte)s;
            mismatch.EnvelopeCounter = (byte)(level ^ 0x01);
            mismatch.HoldZero = false;
            mismatch.ExponentialPipeline = 1;
            mismatch.Clock();
            Assert.Equal(1, mismatch.EnvelopePipeline);
        }
    }

    /// <summary>
    /// FR: FR-SID-ENV, TR: TR-SID-ORACLE-001, TEST: TEST-SID-ENV-03.
    /// Use case: reset() must flush all three single-cycle pipelines so no
    /// stale pipelined step fires after a chip reset.
    /// Acceptance: after dirtying EnvelopePipeline/ExponentialPipeline/
    /// StatePipeline, Reset() forces all three to exactly 0
    /// (resid/envelope.cc:190-193).
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-ENV-03", ParityTag.Faithful)]
    public void Env_Reset_ClearsAllPipelines()
    {
        var env = NewEnv();
        env.EnvelopePipeline = 4;
        env.ExponentialPipeline = 2;
        env.StatePipeline = 3;

        env.Reset();

        Assert.Equal(0, env.EnvelopePipeline);
        Assert.Equal(0, env.ExponentialPipeline);
        Assert.Equal(0, env.StatePipeline);
    }

    /// <summary>
    /// FR: FR-SID-ENV, TR: TR-SID-ORACLE-001, TEST: TEST-SID-ENV-04.
    /// Use case: reset() clears the ADSR register nibbles and the gate latch to
    /// the documented power-on values.
    /// Acceptance: after writing ATK/DCY=0xA7, SUS/REL=0xC3 and gate on,
    /// Reset() yields attack == decay == sustain == release == 0 and gate == 0
    /// (resid/envelope.cc:195-200).
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-ENV-04", ParityTag.Faithful)]
    public void Env_Reset_ClearsAdsrRegistersAndGate()
    {
        var env = NewEnv();
        env.WriteAttackDecay(0xA7);
        env.WriteSustainRelease(0xC3);
        env.WriteControl(0x01);

        env.Reset();

        Assert.Equal((byte)0, env.Attack);
        Assert.Equal((byte)0, env.Decay);
        Assert.Equal((byte)0, env.Sustain);
        Assert.Equal((byte)0, env.Release);
        Assert.Equal((byte)0, env.Gate);
    }

    /// <summary>
    /// FR: FR-SID-ENV, TR: TR-SID-ORACLE-001, TEST: TEST-SID-ENV-05.
    /// Use case: reset() re-arms the rate and exponential prescalers to their
    /// documented initial values.
    /// Acceptance: after dirtying them, Reset() gives rate_counter == 0,
    /// exponential_counter == 0, exponential_counter_period == 1 and
    /// reset_rate_counter == false (resid/envelope.cc:202-206).
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-ENV-05", ParityTag.Faithful)]
    public void Env_Reset_ClearsRateAndExponentialCounters()
    {
        var env = NewEnv();
        env.RateCounter = 1234;
        env.ExponentialCounter = 29;
        env.ExponentialCounterPeriod = 30;
        env.ResetRateCounter = true;

        env.Reset();

        Assert.Equal(0, env.RateCounter);
        Assert.Equal((byte)0, env.ExponentialCounter);
        Assert.Equal((byte)1, env.ExponentialCounterPeriod);
        Assert.False(env.ResetRateCounter);
    }

    /// <summary>
    /// FR: FR-SID-ENV, TR: TR-SID-ORACLE-001, TEST: TEST-SID-ENV-06.
    /// Use case: reset() parks the state machine in RELEASE with the release
    /// rate loaded and the freeze latch clear.
    /// Acceptance: after dirtying state/rate/hold_zero, Reset() gives
    /// state == RELEASE, rate_period == rate_counter_period[release] == 8
    /// (release == 0) and hold_zero == false (resid/envelope.cc:208-210).
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-ENV-06", ParityTag.Faithful)]
    public void Env_Reset_EntersReleaseWithPeriod8AndHoldZeroFalse()
    {
        var env = NewEnv();
        env.State = EnvState.Attack;
        env.RatePeriod = 31250;
        env.HoldZero = true;

        env.Reset();

        Assert.Equal(EnvState.Release, env.State);
        Assert.Equal(8, env.RatePeriod);
        Assert.False(env.HoldZero);
    }

    /// <summary>
    /// FR: FR-SID-ENV, TR: TR-SID-ORACLE-001, TEST: TEST-SID-ENV-09.
    /// Use case: at power-up next_state must already be RELEASE so delta
    /// clocking never reads an uninitialized state target
    /// (resid/envelope.cc:179; the managed port initializes it in Reset(),
    /// which the Sid6581 constructor invokes for every voice).
    /// Acceptance: Reset() forces NextState == RELEASE even after it was
    /// dirtied to ATTACK, bit-exact.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-ENV-09", ParityTag.Faithful)]
    public void Env_PowerUp_NextStateIsRelease()
    {
        var env = NewEnv();
        env.NextState = EnvState.Attack;

        env.Reset();

        Assert.Equal(EnvState.Release, env.NextState);
    }

    /// <summary>
    /// FR: FR-SID-ENV, TR: TR-SID-ORACLE-001, TEST: TEST-SID-ENV-10.
    /// Use case: writeCONTROL only reacts to a change of the gate bit; writing
    /// the same gate value must leave the whole generator untouched.
    /// Acceptance: with gate == gate_next the write is a complete no-op (every
    /// field identical before and after), both for gate low (0x00) and gate
    /// high (0xFF, gate bit still 1) (resid/envelope.cc:228,233).
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-ENV-10", ParityTag.Faithful)]
    public void Env_WriteControl_SameGate_IsNoOp()
    {
        var env = NewEnv();
        var beforeLow = env;
        env.WriteControl(0x00);
        Assert.Equal(beforeLow, env);

        env.WriteControl(0x01);
        var beforeHigh = env;
        env.WriteControl(0xFF);
        Assert.Equal(beforeHigh, env);
    }

    /// <summary>
    /// FR: FR-SID-ENV, TR: TR-SID-ORACLE-001, TEST: TEST-SID-ENV-11.
    /// Use case: gate 0-&gt;1 "accidentally" activates the decay register during
    /// the first cycle of the attack phase and schedules the real attack via
    /// the state pipeline.
    /// Acceptance: from reset with decay == 5, WriteControl(gate on) yields
    /// next_state == ATTACK, state == DECAY_SUSTAIN, rate_period ==
    /// rate_counter_period[5] == 219 and state_pipeline == 2
    /// (resid/envelope.cc:236-241).
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-ENV-11", ParityTag.Faithful)]
    public void Env_GateOn_EntersDecaySustainWithAttackPendingViaPipeline()
    {
        var env = NewEnv();
        env.WriteAttackDecay(0x05); // attack = 0, decay = 5

        env.WriteControl(0x01);

        Assert.Equal(EnvState.Attack, env.NextState);
        Assert.Equal(EnvState.DecaySustain, env.State);
        Assert.Equal(219, env.RatePeriod);
        Assert.Equal(2, env.StatePipeline);
        Assert.Equal(0, env.EnvelopePipeline);
    }

    /// <summary>
    /// FR: FR-SID-ENV, TR: TR-SID-ORACLE-001, TEST: TEST-SID-ENV-12.
    /// Use case: a gate-on that lands while the prescaler is about to fire
    /// (reset_rate_counter pending or exponential pipeline at 2) must arm the
    /// envelope pipeline with the reSID-measured latency.
    /// Acceptance: WriteControl(gate on) with reset_rate_counter set gives
    /// envelope_pipeline == 2 when exp_period == 1 and == 4 when
    /// exp_period == 30; with exponential_pipeline == 2 it gives 2 regardless
    /// (resid/envelope.cc:242-244).
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-ENV-12", ParityTag.Faithful)]
    public void Env_GateOn_WithResetRateCounterOrExpPipeline2_SetsEnvelopePipeline()
    {
        var rrcPeriod1 = NewEnv();
        rrcPeriod1.ResetRateCounter = true; // exp_period == 1 from reset
        rrcPeriod1.WriteControl(0x01);
        Assert.Equal(2, rrcPeriod1.EnvelopePipeline);

        var rrcPeriod30 = NewEnv();
        rrcPeriod30.ResetRateCounter = true;
        rrcPeriod30.ExponentialCounterPeriod = 30;
        rrcPeriod30.WriteControl(0x01);
        Assert.Equal(4, rrcPeriod30.EnvelopePipeline);

        var expPipe2 = NewEnv();
        expPipe2.ExponentialPipeline = 2;
        expPipe2.ExponentialCounterPeriod = 30;
        expPipe2.WriteControl(0x01);
        Assert.Equal(2, expPipe2.EnvelopePipeline);
    }

    /// <summary>
    /// FR: FR-SID-ENV, TR: TR-SID-ORACLE-001, TEST: TEST-SID-ENV-13.
    /// Use case: a gate-on that lands one cycle after an exponential fire
    /// (exponential_pipeline == 1) stretches the state pipeline instead of
    /// arming the envelope pipeline.
    /// Acceptance: WriteControl(gate on) with exponential_pipeline == 1 and
    /// reset_rate_counter clear gives state_pipeline == 3 and leaves
    /// envelope_pipeline == 0 (resid/envelope.cc:245).
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-ENV-13", ParityTag.Faithful)]
    public void Env_GateOn_WithExponentialPipeline1_SetsStatePipeline3()
    {
        var env = NewEnv();
        env.ExponentialPipeline = 1;

        env.WriteControl(0x01);

        Assert.Equal(3, env.StatePipeline);
        Assert.Equal(0, env.EnvelopePipeline);
    }

    /// <summary>
    /// FR: FR-SID-ENV, TR: TR-SID-ORACLE-001, TEST: TEST-SID-ENV-14.
    /// Use case: gate 1-&gt;0 schedules RELEASE through the state pipeline, one
    /// cycle later when an envelope step is already in flight.
    /// Acceptance: WriteControl(gate off) yields next_state == RELEASE with
    /// state_pipeline == 2 when envelope_pipeline == 0 and state_pipeline == 3
    /// when envelope_pipeline &gt; 0 (resid/envelope.cc:247).
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-ENV-14", ParityTag.Faithful)]
    public void Env_GateOff_SetsReleaseNextStateAndStatePipelineByEnvelopePipeline()
    {
        var idle = NewEnv();
        idle.WriteControl(0x01); // envelope_pipeline stays 0 from clean reset
        idle.WriteControl(0x00);
        Assert.Equal(EnvState.Release, idle.NextState);
        Assert.Equal(2, idle.StatePipeline);

        var inFlight = NewEnv();
        inFlight.WriteControl(0x01);
        inFlight.EnvelopePipeline = 2;
        inFlight.WriteControl(0x00);
        Assert.Equal(EnvState.Release, inFlight.NextState);
        Assert.Equal(3, inFlight.StatePipeline);
    }

    /// <summary>
    /// FR: FR-SID-ENV, TR: TR-SID-ORACLE-001, TEST: TEST-SID-ENV-15.
    /// Use case: after handling a gate transition, writeCONTROL latches the new
    /// gate value so the next write compares against it.
    /// Acceptance: gate goes 0 to 1 on WriteControl(0x01) and back to 0 on
    /// WriteControl(0xFE) (gate bit clear, other bits set)
    /// (resid/envelope.cc:248).
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-ENV-15", ParityTag.Faithful)]
    public void Env_WriteControl_CommitsGateAfterTransition()
    {
        var env = NewEnv();
        Assert.Equal((byte)0, env.Gate);

        env.WriteControl(0x01);
        Assert.Equal((byte)1, env.Gate);

        env.WriteControl(0xFE);
        Assert.Equal((byte)0, env.Gate);
    }

    /// <summary>
    /// FR: FR-SID-ENV, TR: TR-SID-ORACLE-001, TEST: TEST-SID-ENV-16.
    /// Use case: writeATTACK_DECAY splits the register byte into the attack
    /// (high) and decay (low) nibbles.
    /// Acceptance: WriteAttackDecay(0xA7) gives attack == 0x0A and
    /// decay == 0x07; WriteAttackDecay(0x3C) gives 0x03/0x0C
    /// (resid/envelope.cc:254-255).
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-ENV-16", ParityTag.Faithful)]
    public void Env_WriteAttackDecay_SplitsNibbles()
    {
        var env = NewEnv();

        env.WriteAttackDecay(0xA7);
        Assert.Equal((byte)0x0A, env.Attack);
        Assert.Equal((byte)0x07, env.Decay);

        env.WriteAttackDecay(0x3C);
        Assert.Equal((byte)0x03, env.Attack);
        Assert.Equal((byte)0x0C, env.Decay);
    }

    /// <summary>
    /// FR: FR-SID-ENV, TR: TR-SID-ORACLE-001, TEST: TEST-SID-ENV-17.
    /// Use case: rewriting ATK/DCY while in ATTACK reloads the live rate
    /// comparison value from the attack nibble.
    /// Acceptance: in ATTACK, WriteAttackDecay(0x2F) sets rate_period ==
    /// rate_counter_period[2] == 62 (the attack nibble, not the decay nibble's
    /// 31250) (resid/envelope.cc:256-258).
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-ENV-17", ParityTag.Faithful)]
    public void Env_WriteAttackDecay_InAttack_ReloadsAttackPeriod()
    {
        var env = NewEnv();
        env.State = EnvState.Attack;

        env.WriteAttackDecay(0x2F); // attack = 2, decay = 0xF

        Assert.Equal(62, env.RatePeriod);
    }

    /// <summary>
    /// FR: FR-SID-ENV, TR: TR-SID-ORACLE-001, TEST: TEST-SID-ENV-18.
    /// Use case: rewriting ATK/DCY while in DECAY_SUSTAIN reloads rate_period
    /// from the decay nibble; RELEASE and FREEZED keep their current period.
    /// Acceptance: in DECAY_SUSTAIN, WriteAttackDecay(0xF5) sets rate_period ==
    /// rate_counter_period[5] == 219; in RELEASE and FREEZED the same write
    /// leaves rate_period unchanged (8 from reset)
    /// (resid/envelope.cc:259-261).
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-ENV-18", ParityTag.Faithful)]
    public void Env_WriteAttackDecay_InDecaySustain_ReloadsDecayPeriod_OthersUnchanged()
    {
        var decaySustain = NewEnv();
        decaySustain.State = EnvState.DecaySustain;
        decaySustain.WriteAttackDecay(0xF5); // attack = 0xF, decay = 5
        Assert.Equal(219, decaySustain.RatePeriod);

        var release = NewEnv(); // State == RELEASE, RatePeriod == 8
        release.WriteAttackDecay(0xFF);
        Assert.Equal(8, release.RatePeriod);

        var freezed = NewEnv();
        freezed.State = EnvState.Freezed;
        freezed.WriteAttackDecay(0xFF);
        Assert.Equal(8, freezed.RatePeriod);
    }

    /// <summary>
    /// FR: FR-SID-ENV, TR: TR-SID-ORACLE-001, TEST: TEST-SID-ENV-19.
    /// Use case: writeSUSTAIN_RELEASE splits the register byte into the sustain
    /// (high) and release (low) nibbles.
    /// Acceptance: WriteSustainRelease(0xC3) gives sustain == 0x0C and
    /// release == 0x03 (resid/envelope.cc:266-267).
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-ENV-19", ParityTag.Faithful)]
    public void Env_WriteSustainRelease_SplitsNibbles()
    {
        var env = NewEnv();

        env.WriteSustainRelease(0xC3);

        Assert.Equal((byte)0x0C, env.Sustain);
        Assert.Equal((byte)0x03, env.Release);
    }

    /// <summary>
    /// FR: FR-SID-ENV, TR: TR-SID-ORACLE-001, TEST: TEST-SID-ENV-20.
    /// Use case: rewriting SUS/REL while in RELEASE reloads the live rate
    /// comparison value from the release nibble; other states keep theirs.
    /// Acceptance: in RELEASE, WriteSustainRelease(0x0B) sets rate_period ==
    /// rate_counter_period[0xB] == 3125; in ATTACK the same write leaves
    /// rate_period unchanged while still latching release == 0x0F
    /// (resid/envelope.cc:268-270).
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-ENV-20", ParityTag.Faithful)]
    public void Env_WriteSustainRelease_InRelease_ReloadsReleasePeriod()
    {
        var release = NewEnv(); // State == RELEASE after reset
        release.WriteSustainRelease(0x0B);
        Assert.Equal(3125, release.RatePeriod);

        var attack = NewEnv();
        attack.State = EnvState.Attack;
        attack.WriteSustainRelease(0x0F);
        Assert.Equal(8, attack.RatePeriod);
        Assert.Equal((byte)0x0F, attack.Release);
    }

    /// <summary>
    /// FR: FR-SID-ENV, TR: TR-SID-ORACLE-001, TEST: TEST-SID-ENV-21.
    /// Use case: ENV3 is sampled at the first phase of the clock, before any
    /// pipelined envelope step lands, so register reads observe the pre-step
    /// counter.
    /// Acceptance: a quiet cycle copies the counter into env3; a cycle whose
    /// envelope pipeline fires reports env3 == 0x10 (entry value) while the
    /// counter has already advanced to 0x11 (resid/envelope.h:118).
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-ENV-21", ParityTag.Faithful)]
    public void Env_Clock_SamplesEnv3AtFirstPhase()
    {
        var quiet = NewEnv();
        quiet.EnvelopeCounter = 0x42;
        quiet.Env3 = 0x99;
        quiet.Clock();
        Assert.Equal((byte)0x42, quiet.Env3);
        Assert.Equal((byte)0x42, quiet.EnvelopeCounter);

        var stepping = NewEnv();
        stepping.State = EnvState.Attack;
        stepping.EnvelopeCounter = 0x10;
        stepping.EnvelopePipeline = 1;
        stepping.HoldZero = false;
        stepping.Env3 = 0x99;
        stepping.Clock();
        Assert.Equal((byte)0x10, stepping.Env3);
        Assert.Equal((byte)0x11, stepping.EnvelopeCounter);
    }

    /// <summary>
    /// FR: FR-SID-ENV, TR: TR-SID-ORACLE-001, TEST: TEST-SID-ENV-22.
    /// Use case: a pending state change must be applied before the envelope
    /// pipeline block, so a step landing on the same cycle uses the NEW state.
    /// Acceptance: with state_pipeline == 1 (next_state ATTACK), hold_zero set
    /// and envelope_pipeline == 1, one clock first fires state_change (clearing
    /// hold_zero, entering ATTACK) and then steps the counter 0 to 1; if the
    /// order were reversed hold_zero would have skipped the step
    /// (resid/envelope.h:120-122).
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-ENV-22", ParityTag.Faithful)]
    public void Env_Clock_RunsStateChangeBeforeEnvelopeStep()
    {
        var env = NewEnv();
        env.State = EnvState.DecaySustain;
        env.NextState = EnvState.Attack;
        env.StatePipeline = 1;
        env.HoldZero = true;
        env.EnvelopeCounter = 0x00;
        env.EnvelopePipeline = 1;
        env.Attack = 0;

        env.Clock();

        Assert.Equal(EnvState.Attack, env.State);
        Assert.False(env.HoldZero);
        Assert.Equal((byte)0x01, env.EnvelopeCounter);
    }

    /// <summary>
    /// FR: FR-SID-ENV, TR: TR-SID-ORACLE-001, TEST: TEST-SID-ENV-23.
    /// Use case: the envelope pipeline is a countdown; the step must land only
    /// on the cycle the pipeline reaches zero.
    /// Acceptance: from envelope_pipeline == 3 in ATTACK, two clocks decrement
    /// the pipeline (3-&gt;2-&gt;1) without touching the counter and the third
    /// clock fires the step (counter 0x10-&gt;0x11) (resid/envelope.h:126).
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-ENV-23", ParityTag.Faithful)]
    public void Env_EnvelopePipeline_DecrementsEachCycleAndFiresAtZero()
    {
        var env = NewEnv();
        env.State = EnvState.Attack;
        env.EnvelopeCounter = 0x10;
        env.EnvelopePipeline = 3;
        env.HoldZero = false;

        env.Clock();
        Assert.Equal(2, env.EnvelopePipeline);
        Assert.Equal((byte)0x10, env.EnvelopeCounter);

        env.Clock();
        Assert.Equal(1, env.EnvelopePipeline);
        Assert.Equal((byte)0x10, env.EnvelopeCounter);

        env.Clock();
        Assert.Equal(0, env.EnvelopePipeline);
        Assert.Equal((byte)0x11, env.EnvelopeCounter);
    }

    /// <summary>
    /// FR: FR-SID-ENV, TR: TR-SID-ORACLE-001, TEST: TEST-SID-ENV-24.
    /// Use case: the hold_zero freeze latch gates the whole envelope step block
    /// (step + set_exponential_counter), while the pipeline itself still
    /// drains.
    /// Acceptance: with hold_zero set and envelope_pipeline == 1 in ATTACK, one
    /// clock drains the pipeline to 0 but leaves the counter at 0x10 and
    /// hold_zero set (resid/envelope.h:127).
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-ENV-24", ParityTag.Faithful)]
    public void Env_HoldZero_SkipsEnvelopeStep()
    {
        var env = NewEnv();
        env.State = EnvState.Attack;
        env.EnvelopeCounter = 0x10;
        env.EnvelopePipeline = 1;
        env.HoldZero = true;

        env.Clock();

        Assert.Equal(0, env.EnvelopePipeline);
        Assert.Equal((byte)0x10, env.EnvelopeCounter);
        Assert.True(env.HoldZero);
    }

    /// <summary>
    /// FR: FR-SID-ENV, TR: TR-SID-ORACLE-001, TEST: TEST-SID-ENV-25.
    /// Use case: an ATTACK step increments the 8-bit envelope counter with an
    /// explicit and-0xff mask (so 0xff wraps to 0x00 instead of widening).
    /// Acceptance: a fired attack step takes 0x7f to 0x80, and from 0xff the
    /// masked increment lands on 0x00 while the state remains ATTACK (the
    /// 0xff comparison no longer matches) (resid/envelope.h:128-129).
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-ENV-25", ParityTag.Faithful)]
    public void Env_AttackStep_IncrementsCounterMasked()
    {
        var plain = NewEnv();
        plain.State = EnvState.Attack;
        plain.EnvelopeCounter = 0x7f;
        plain.EnvelopePipeline = 1;
        plain.HoldZero = false;
        plain.Clock();
        Assert.Equal((byte)0x80, plain.EnvelopeCounter);
        Assert.Equal(EnvState.Attack, plain.State);

        var wrap = NewEnv();
        wrap.State = EnvState.Attack;
        wrap.EnvelopeCounter = 0xff;
        wrap.EnvelopePipeline = 1;
        wrap.HoldZero = false;
        wrap.Clock();
        Assert.Equal((byte)0x00, wrap.EnvelopeCounter);
        Assert.Equal(EnvState.Attack, wrap.State);
    }

    /// <summary>
    /// FR: FR-SID-ENV, TR: TR-SID-ORACLE-001, TEST: TEST-SID-ENV-26.
    /// Use case: when the attack ramp reaches 0xff the generator switches to
    /// DECAY_SUSTAIN in the same step and loads the decay rate.
    /// Acceptance: an attack step from 0xfe lands on 0xff and yields state ==
    /// DECAY_SUSTAIN with rate_period == rate_counter_period[decay == 3] == 94
    /// (resid/envelope.h:130-133).
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-ENV-26", ParityTag.Faithful)]
    public void Env_AttackReachingFF_SwitchesToDecaySustainWithDecayPeriod()
    {
        var env = NewEnv();
        env.State = EnvState.Attack;
        env.EnvelopeCounter = 0xfe;
        env.Decay = 3;
        env.EnvelopePipeline = 1;
        env.HoldZero = false;

        env.Clock();

        Assert.Equal((byte)0xff, env.EnvelopeCounter);
        Assert.Equal(EnvState.DecaySustain, env.State);
        Assert.Equal(94, env.RatePeriod);
    }

    /// <summary>
    /// FR: FR-SID-ENV, TR: TR-SID-ORACLE-001, TEST: TEST-SID-ENV-27.
    /// Use case: DECAY_SUSTAIN and RELEASE steps decrement the 8-bit counter
    /// with an explicit and-0xff mask (0x00 wraps down to 0xff).
    /// Acceptance: a fired step takes 0x80 to 0x7f in DECAY_SUSTAIN, and in
    /// RELEASE the masked decrement takes 0x00 to 0xff
    /// (resid/envelope.h:135-137).
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-ENV-27", ParityTag.Faithful)]
    public void Env_DecaySustainAndReleaseStep_DecrementsCounterMasked()
    {
        var decay = NewEnv();
        decay.State = EnvState.DecaySustain;
        decay.EnvelopeCounter = 0x80;
        decay.EnvelopePipeline = 1;
        decay.HoldZero = false;
        decay.Clock();
        Assert.Equal((byte)0x7f, decay.EnvelopeCounter);

        var release = NewEnv();
        release.State = EnvState.Release;
        release.EnvelopeCounter = 0x00;
        release.EnvelopePipeline = 1;
        release.HoldZero = false;
        release.Clock();
        Assert.Equal((byte)0xff, release.EnvelopeCounter);
    }

    /// <summary>
    /// FR: FR-SID-ENV, TR: TR-SID-ORACLE-001, TEST: TEST-SID-ENV-28.
    /// Use case: every envelope step is followed by set_exponential_counter()
    /// so the exponential divider retunes as soon as the counter lands on a
    /// table value.
    /// Acceptance: a DECAY_SUSTAIN step from 0x5e lands on 0x5d and updates
    /// exponential_counter_period from the sentinel 99 to 2 in the same cycle
    /// (resid/envelope.h:139).
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-ENV-28", ParityTag.Faithful)]
    public void Env_Step_UpdatesExponentialPeriodViaSetExponentialCounter()
    {
        var env = StepDecayLandingOn(0x5d);

        Assert.Equal((byte)0x5d, env.EnvelopeCounter);
        Assert.Equal((byte)2, env.ExponentialCounterPeriod);
    }

    /// <summary>
    /// FR: FR-SID-ENV, TR: TR-SID-ORACLE-001, TEST: TEST-SID-ENV-29.
    /// Use case: the exponential pipeline is a countdown; only the cycle it
    /// reaches zero clears the exponential counter.
    /// Acceptance: from exponential_pipeline == 2 with exponential_counter ==
    /// 17, the first clock leaves the counter at 17 (pipeline 1) and the second
    /// clock zeroes it (pipeline 0) (resid/envelope.h:143-144).
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-ENV-29", ParityTag.Faithful)]
    public void Env_ExponentialPipeline_FireZeroesExponentialCounter()
    {
        var env = NewEnv();
        env.State = EnvState.DecaySustain;
        env.Sustain = 2;
        env.EnvelopeCounter = 0x22; // equals sustain_level[2]: no requeue noise
        env.HoldZero = false;
        env.ExponentialCounter = 17;
        env.ExponentialPipeline = 2;

        env.Clock();
        Assert.Equal(1, env.ExponentialPipeline);
        Assert.Equal((byte)17, env.ExponentialCounter);

        env.Clock();
        Assert.Equal(0, env.ExponentialPipeline);
        Assert.Equal((byte)0, env.ExponentialCounter);
    }

    /// <summary>
    /// FR: FR-SID-ENV, TR: TR-SID-ORACLE-001, TEST: TEST-SID-ENV-30.
    /// Use case: an exponential fire requeues an envelope step only while the
    /// envelope should still be moving: DECAY_SUSTAIN above/below the sustain
    /// level, or RELEASE.
    /// Acceptance: with exponential_pipeline == 1, one clock sets
    /// envelope_pipeline == 1 iff (DECAY_SUSTAIN and counter !=
    /// sustain_level[sustain]) or RELEASE; at the sustain level or in ATTACK it
    /// stays 0 (resid/envelope.h:146-154).
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-ENV-30", ParityTag.Faithful)]
    public void Env_ExponentialFire_RequeueCondition()
    {
        var dsAtSustain = NewEnv();
        dsAtSustain.State = EnvState.DecaySustain;
        dsAtSustain.Sustain = 5;
        dsAtSustain.EnvelopeCounter = 0x55;
        dsAtSustain.HoldZero = false;
        dsAtSustain.ExponentialPipeline = 1;
        dsAtSustain.Clock();
        Assert.Equal(0, dsAtSustain.EnvelopePipeline);

        var dsAboveSustain = NewEnv();
        dsAboveSustain.State = EnvState.DecaySustain;
        dsAboveSustain.Sustain = 5;
        dsAboveSustain.EnvelopeCounter = 0x54;
        dsAboveSustain.HoldZero = false;
        dsAboveSustain.ExponentialPipeline = 1;
        dsAboveSustain.Clock();
        Assert.Equal(1, dsAboveSustain.EnvelopePipeline);

        var releasing = NewEnv();
        releasing.State = EnvState.Release;
        releasing.EnvelopeCounter = 0x54;
        releasing.HoldZero = false;
        releasing.ExponentialPipeline = 1;
        releasing.Clock();
        Assert.Equal(1, releasing.EnvelopePipeline);

        var attacking = NewEnv();
        attacking.State = EnvState.Attack;
        attacking.EnvelopeCounter = 0x54;
        attacking.HoldZero = false;
        attacking.ExponentialPipeline = 1;
        attacking.Clock();
        Assert.Equal(0, attacking.EnvelopePipeline);
    }

    /// <summary>
    /// FR: FR-SID-ENV, TR: TR-SID-ORACLE-001, TEST: TEST-SID-ENV-31.
    /// Use case: the reset_rate_counter block is the else-if arm of the
    /// exponential fire: it zeroes the rate counter and consumes the flag, and
    /// it is skipped entirely on a cycle whose exponential pipeline fires.
    /// Acceptance: with reset_rate_counter set (rate_counter 5000), one clock
    /// leaves rate_counter == 1 (zeroed, then the same cycle's rate block
    /// pre-increments) and reset_rate_counter == false; with an exponential
    /// fire on the same cycle the block is skipped, so rate_counter == 5001 and
    /// the flag stays set (resid/envelope.h:156-158).
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-ENV-31", ParityTag.Faithful)]
    public void Env_ResetRateCounterBlock_IsElseIfOfExponentialFire()
    {
        var consumed = NewEnv();
        consumed.State = EnvState.Release;
        consumed.HoldZero = true; // keep the exponential counter side path inert
        consumed.RateCounter = 5000;
        consumed.ResetRateCounter = true;
        consumed.Clock();
        Assert.Equal(1, consumed.RateCounter);
        Assert.False(consumed.ResetRateCounter);

        var preempted = NewEnv();
        preempted.State = EnvState.Release;
        preempted.HoldZero = false;
        preempted.EnvelopeCounter = 0x80;
        preempted.RateCounter = 5000;
        preempted.ResetRateCounter = true;
        preempted.ExponentialPipeline = 1; // fires this cycle, shadowing the else-if
        preempted.Clock();
        Assert.Equal(5001, preempted.RateCounter);
        Assert.True(preempted.ResetRateCounter);
        Assert.Equal(1, preempted.EnvelopePipeline);
    }

    /// <summary>
    /// FR: FR-SID-ENV, TR: TR-SID-ORACLE-001, TEST: TEST-SID-ENV-32.
    /// Use case: the first envelope step of ATTACK resets the exponential
    /// counter and arms the two-cycle envelope pipeline.
    /// Acceptance: consuming reset_rate_counter in ATTACK zeroes
    /// exponential_counter (was 7) and sets envelope_pipeline == 2
    /// (resid/envelope.h:160-171).
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-ENV-32", ParityTag.Faithful)]
    public void Env_ResetRateCounter_InAttack_ZeroesExpCounterAndArmsEnvelopePipeline()
    {
        var env = NewEnv();
        env.State = EnvState.Attack;
        env.HoldZero = false;
        env.ExponentialCounter = 7;
        env.RateCounter = 1234;
        env.ResetRateCounter = true;

        env.Clock();

        Assert.Equal((byte)0, env.ExponentialCounter);
        Assert.Equal(2, env.EnvelopePipeline);
        Assert.False(env.ResetRateCounter);
    }

    /// <summary>
    /// FR: FR-SID-ENV, TR: TR-SID-ORACLE-001, TEST: TEST-SID-ENV-33.
    /// Use case: outside ATTACK, consuming reset_rate_counter advances the
    /// exponential divider; hitting the period arms the exponential pipeline
    /// with reSID's period-dependent latency, and hold_zero (short-circuit)
    /// blocks the increment.
    /// Acceptance: in RELEASE, ++exponential_counter == period arms
    /// exponential_pipeline == 2 for period 4 and == 1 for period 1; a
    /// non-matching increment arms nothing; with hold_zero the counter does not
    /// even increment (resid/envelope.h:172-176).
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-ENV-33", ParityTag.Faithful)]
    public void Env_ResetRateCounter_NonAttack_ArmsExponentialPipelineAtPeriod()
    {
        var period4 = NewEnv();
        period4.State = EnvState.Release;
        period4.HoldZero = false;
        period4.ExponentialCounterPeriod = 4;
        period4.ExponentialCounter = 3;
        period4.ResetRateCounter = true;
        period4.Clock();
        Assert.Equal((byte)4, period4.ExponentialCounter);
        Assert.Equal(2, period4.ExponentialPipeline);

        var period1 = NewEnv();
        period1.State = EnvState.Release;
        period1.HoldZero = false;
        period1.ExponentialCounterPeriod = 1;
        period1.ExponentialCounter = 0;
        period1.ResetRateCounter = true;
        period1.Clock();
        Assert.Equal((byte)1, period1.ExponentialCounter);
        Assert.Equal(1, period1.ExponentialPipeline);

        var belowPeriod = NewEnv();
        belowPeriod.State = EnvState.Release;
        belowPeriod.HoldZero = false;
        belowPeriod.ExponentialCounterPeriod = 4;
        belowPeriod.ExponentialCounter = 1;
        belowPeriod.ResetRateCounter = true;
        belowPeriod.Clock();
        Assert.Equal((byte)2, belowPeriod.ExponentialCounter);
        Assert.Equal(0, belowPeriod.ExponentialPipeline);

        var frozen = NewEnv();
        frozen.State = EnvState.Release;
        frozen.HoldZero = true;
        frozen.ExponentialCounterPeriod = 1;
        frozen.ExponentialCounter = 5;
        frozen.ResetRateCounter = true;
        frozen.Clock();
        Assert.Equal((byte)5, frozen.ExponentialCounter);
        Assert.Equal(0, frozen.ExponentialPipeline);
    }

    /// <summary>
    /// FR: FR-SID-ENV, TR: TR-SID-ORACLE-001, TEST: TEST-SID-ENV-34.
    /// Use case: while below the comparison value the 15-bit rate counter
    /// pre-increments exactly once per cycle.
    /// Acceptance: with rate_counter == 3 and rate_period == 8, one clock gives
    /// rate_counter == 4 and reset_rate_counter still false
    /// (resid/envelope.h:186-188).
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-ENV-34", ParityTag.Faithful)]
    public void Env_RateCounter_IncrementsWhenNotAtPeriod()
    {
        var env = NewEnv(); // RatePeriod == 8 from reset
        env.RateCounter = 3;

        env.Clock();

        Assert.Equal(4, env.RateCounter);
        Assert.False(env.ResetRateCounter);
    }

    /// <summary>
    /// FR: FR-SID-ENV, TR: TR-SID-ORACLE-001, TEST: TEST-SID-ENV-35.
    /// Use case: the ADSR delay bug: when the increment sets bit 15 (0x8000)
    /// the counter is incremented again and masked to 15 bits, so it re-enters
    /// the count at 1, not 0.
    /// Acceptance: with rate_counter == 0x7fff (period 8), one clock yields
    /// rate_counter == 1 ((0x7fff + 1 + 1) &amp; 0x7fff)
    /// (resid/envelope.h:187-189).
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-ENV-35", ParityTag.Faithful)]
    public void Env_RateCounter_AdsrDelayBugWrapAt8000()
    {
        var env = NewEnv();
        env.RateCounter = 0x7fff;

        env.Clock();

        Assert.Equal(1, env.RateCounter);
    }

    /// <summary>
    /// FR: FR-SID-ENV, TR: TR-SID-ORACLE-001, TEST: TEST-SID-ENV-36.
    /// Use case: when the rate counter equals the comparison value the cycle
    /// spends itself flagging the reset instead of incrementing (the +1 cycle
    /// in the effective period).
    /// Acceptance: with rate_counter == rate_period == 8, one clock leaves
    /// rate_counter == 8 and sets reset_rate_counter == true
    /// (resid/envelope.h:191-192).
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-ENV-36", ParityTag.Faithful)]
    public void Env_RateCounter_AtPeriod_SetsResetFlagWithoutIncrement()
    {
        var env = NewEnv(); // RatePeriod == 8 from reset
        env.RateCounter = 8;

        env.Clock();

        Assert.Equal(8, env.RateCounter);
        Assert.True(env.ResetRateCounter);
    }

    /// <summary>
    /// FR: FR-SID-ENV, TR: TR-SID-ORACLE-001, TEST: TEST-SID-ENV-37.
    /// Use case: the exponential divider table: envelope counter 0xff selects
    /// period 1 (full-rate stepping at the top of the ramp).
    /// Acceptance: a step landing on 0xff sets exponential_counter_period == 1
    /// (resid/envelope.h:390-391).
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-ENV-37", ParityTag.Faithful)]
    public void Env_SetExponentialCounter_FF_Period1()
    {
        var env = StepDecayLandingOn(0xff);

        Assert.Equal((byte)0xff, env.EnvelopeCounter);
        Assert.Equal((byte)1, env.ExponentialCounterPeriod);
    }

    /// <summary>
    /// FR: FR-SID-ENV, TR: TR-SID-ORACLE-001, TEST: TEST-SID-ENV-38.
    /// Use case: the exponential divider table: envelope counter 0x5d selects
    /// period 2.
    /// Acceptance: a step landing on 0x5d sets exponential_counter_period == 2
    /// (resid/envelope.h:392-393).
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-ENV-38", ParityTag.Faithful)]
    public void Env_SetExponentialCounter_5D_Period2()
    {
        var env = StepDecayLandingOn(0x5d);

        Assert.Equal((byte)0x5d, env.EnvelopeCounter);
        Assert.Equal((byte)2, env.ExponentialCounterPeriod);
    }

    /// <summary>
    /// FR: FR-SID-ENV, TR: TR-SID-ORACLE-001, TEST: TEST-SID-ENV-39.
    /// Use case: the exponential divider table: envelope counter 0x36 selects
    /// period 4.
    /// Acceptance: a step landing on 0x36 sets exponential_counter_period == 4
    /// (resid/envelope.h:394-395).
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-ENV-39", ParityTag.Faithful)]
    public void Env_SetExponentialCounter_36_Period4()
    {
        var env = StepDecayLandingOn(0x36);

        Assert.Equal((byte)0x36, env.EnvelopeCounter);
        Assert.Equal((byte)4, env.ExponentialCounterPeriod);
    }

    /// <summary>
    /// FR: FR-SID-ENV, TR: TR-SID-ORACLE-001, TEST: TEST-SID-ENV-40.
    /// Use case: the exponential divider table: envelope counter 0x1a selects
    /// period 8.
    /// Acceptance: a step landing on 0x1a sets exponential_counter_period == 8
    /// (resid/envelope.h:396-397).
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-ENV-40", ParityTag.Faithful)]
    public void Env_SetExponentialCounter_1A_Period8()
    {
        var env = StepDecayLandingOn(0x1a);

        Assert.Equal((byte)0x1a, env.EnvelopeCounter);
        Assert.Equal((byte)8, env.ExponentialCounterPeriod);
    }

    /// <summary>
    /// FR: FR-SID-ENV, TR: TR-SID-ORACLE-001, TEST: TEST-SID-ENV-41.
    /// Use case: the exponential divider table: envelope counter 0x0e selects
    /// period 16.
    /// Acceptance: a step landing on 0x0e sets exponential_counter_period == 16
    /// (resid/envelope.h:398-399).
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-ENV-41", ParityTag.Faithful)]
    public void Env_SetExponentialCounter_0E_Period16()
    {
        var env = StepDecayLandingOn(0x0e);

        Assert.Equal((byte)0x0e, env.EnvelopeCounter);
        Assert.Equal((byte)16, env.ExponentialCounterPeriod);
    }

    /// <summary>
    /// FR: FR-SID-ENV, TR: TR-SID-ORACLE-001, TEST: TEST-SID-ENV-42.
    /// Use case: the exponential divider table: envelope counter 0x06 selects
    /// period 30 (the flattest tail segment).
    /// Acceptance: a step landing on 0x06 sets exponential_counter_period == 30
    /// (resid/envelope.h:400-401).
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-ENV-42", ParityTag.Faithful)]
    public void Env_SetExponentialCounter_06_Period30()
    {
        var env = StepDecayLandingOn(0x06);

        Assert.Equal((byte)0x06, env.EnvelopeCounter);
        Assert.Equal((byte)30, env.ExponentialCounterPeriod);
    }

    /// <summary>
    /// FR: FR-SID-ENV, TR: TR-SID-ORACLE-001, TEST: TEST-SID-ENV-43.
    /// Use case: envelope counter 0x00 selects period 1 AND latches hold_zero:
    /// the counter freezes at zero until the next attack state change.
    /// Acceptance: a step landing on 0x00 sets exponential_counter_period == 1
    /// and hold_zero == true (resid/envelope.h:402-417).
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-ENV-43", ParityTag.Faithful)]
    public void Env_SetExponentialCounter_00_Period1AndHoldZero()
    {
        var env = StepDecayLandingOn(0x00);

        Assert.Equal((byte)0x00, env.EnvelopeCounter);
        Assert.Equal((byte)1, env.ExponentialCounterPeriod);
        Assert.True(env.HoldZero);
    }

    /// <summary>
    /// FR: FR-SID-ENV, TR: TR-SID-ORACLE-001, TEST: TEST-SID-ENV-44.
    /// Use case: set_exponential_counter has no default case: any envelope
    /// counter value outside the seven table entries must leave the exponential
    /// period untouched.
    /// Acceptance: a step landing on 0x80 (non-threshold) leaves
    /// exponential_counter_period at the pre-set sentinel 99
    /// (resid/envelope.h:388-418).
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-ENV-44", ParityTag.Faithful)]
    public void Env_SetExponentialCounter_NonThreshold_LeavesPeriodUnchanged()
    {
        var env = StepDecayLandingOn(0x80);

        Assert.Equal((byte)0x80, env.EnvelopeCounter);
        Assert.Equal((byte)99, env.ExponentialCounterPeriod);
    }

    /// <summary>
    /// FR: FR-SID-ENV, TR: TR-SID-ORACLE-001, TEST: TEST-SID-ENV-45.
    /// Use case: state_change() decrements state_pipeline before comparing, so
    /// a pipeline of 2 fires the ATTACK transition on the second clock, not the
    /// first.
    /// Acceptance: with next_state ATTACK and state_pipeline == 2, the first
    /// clock only decrements (pipeline 1, state unchanged) and the second clock
    /// fires (pipeline 0, state ATTACK, rate_period ==
    /// rate_counter_period[4] == 148) (resid/envelope.h:348).
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-ENV-45", ParityTag.Faithful)]
    public void Env_StateChange_DecrementsStatePipelineBeforeCompare()
    {
        var env = NewEnv();
        env.State = EnvState.DecaySustain;
        env.NextState = EnvState.Attack;
        env.StatePipeline = 2;
        env.Attack = 4;

        env.Clock();
        Assert.Equal(1, env.StatePipeline);
        Assert.Equal(EnvState.DecaySustain, env.State);

        env.Clock();
        Assert.Equal(0, env.StatePipeline);
        Assert.Equal(EnvState.Attack, env.State);
        Assert.Equal(148, env.RatePeriod);
    }

    /// <summary>
    /// FR: FR-SID-ENV, TR: TR-SID-ORACLE-001, TEST: TEST-SID-ENV-46.
    /// Use case: the ATTACK state change fires when the pipeline reaches 0: it
    /// activates the attack rate and unlocks a frozen-at-zero counter.
    /// Acceptance: with next_state ATTACK, state_pipeline == 1 and hold_zero
    /// set, one clock gives state == ATTACK, rate_period ==
    /// rate_counter_period[7] == 312 and hold_zero == false
    /// (resid/envelope.h:351-357).
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-ENV-46", ParityTag.Faithful)]
    public void Env_StateChange_AttackFiresAtZero_SetsAttackPeriodAndClearsHoldZero()
    {
        var env = NewEnv();
        env.State = EnvState.DecaySustain;
        env.NextState = EnvState.Attack;
        env.StatePipeline = 1;
        env.HoldZero = true;
        env.Attack = 7;

        env.Clock();

        Assert.Equal(EnvState.Attack, env.State);
        Assert.Equal(312, env.RatePeriod);
        Assert.False(env.HoldZero);
    }

    /// <summary>
    /// FR: FR-SID-ENV, TR: TR-SID-ORACLE-001, TEST: TEST-SID-ENV-47.
    /// Use case: the RELEASE state change fires one cycle earlier out of
    /// DECAY_SUSTAIN (pipeline 1) than out of ATTACK (pipeline 0), matching the
    /// die's counting-direction switch timing.
    /// Acceptance: from ATTACK with state_pipeline == 1 one clock fires
    /// (pipeline 0) giving state == RELEASE, rate_period ==
    /// rate_counter_period[9] == 976; from DECAY_SUSTAIN with
    /// state_pipeline == 2 one clock fires at pipeline == 1 giving state ==
    /// RELEASE, rate_period == rate_counter_period[2] == 62
    /// (resid/envelope.h:361-367).
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-ENV-47", ParityTag.Faithful)]
    public void Env_StateChange_ReleaseFires_FromAttackAtZeroOrDecaySustainAtOne()
    {
        var fromAttack = NewEnv();
        fromAttack.State = EnvState.Attack;
        fromAttack.NextState = EnvState.Release;
        fromAttack.StatePipeline = 1;
        fromAttack.Release = 9;
        fromAttack.Clock();
        Assert.Equal(0, fromAttack.StatePipeline);
        Assert.Equal(EnvState.Release, fromAttack.State);
        Assert.Equal(976, fromAttack.RatePeriod);

        var fromDecay = NewEnv();
        fromDecay.State = EnvState.DecaySustain;
        fromDecay.NextState = EnvState.Release;
        fromDecay.StatePipeline = 2;
        fromDecay.Release = 2;
        fromDecay.Clock();
        Assert.Equal(1, fromDecay.StatePipeline);
        Assert.Equal(EnvState.Release, fromDecay.State);
        Assert.Equal(62, fromDecay.RatePeriod);
    }

    /// <summary>
    /// FR: FR-SID-ENV, TR: TR-SID-ORACLE-001, TEST: TEST-SID-ENV-48.
    /// Use case: state_change treats next_state DECAY_SUSTAIN and FREEZED as
    /// no-ops: the pipeline still drains but state, rate_period and hold_zero
    /// are untouched.
    /// Acceptance: with state_pipeline == 1, a clock with next_state
    /// DECAY_SUSTAIN leaves state == RELEASE, rate_period == 8 and hold_zero
    /// unchanged; the same holds for next_state FREEZED with rate_period == 148
    /// (resid/envelope.h:359-360,368-370).
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-ENV-48", ParityTag.Faithful)]
    public void Env_StateChange_DecaySustainAndFreezedNextStates_AreNoOps()
    {
        var toDecay = NewEnv(); // State == RELEASE, RatePeriod == 8 from reset
        toDecay.NextState = EnvState.DecaySustain;
        toDecay.StatePipeline = 1;
        toDecay.HoldZero = true;
        toDecay.Clock();
        Assert.Equal(0, toDecay.StatePipeline);
        Assert.Equal(EnvState.Release, toDecay.State);
        Assert.Equal(8, toDecay.RatePeriod);
        Assert.True(toDecay.HoldZero);

        var toFreezed = NewEnv();
        toFreezed.State = EnvState.DecaySustain;
        toFreezed.RatePeriod = 148;
        toFreezed.NextState = EnvState.Freezed;
        toFreezed.StatePipeline = 1;
        toFreezed.Clock();
        Assert.Equal(0, toFreezed.StatePipeline);
        Assert.Equal(EnvState.DecaySustain, toFreezed.State);
        Assert.Equal(148, toFreezed.RatePeriod);
    }

    /// <summary>
    /// FR: FR-SID-ENV, TR: TR-SID-ORACLE-001, TEST: TEST-SID-ENV-49.
    /// Use case: readENV() ($D41C for voice 3) returns the raw env3 latch (the
    /// counter sampled at the first clock phase), never a DAC-mapped value.
    /// Acceptance: across a 200-cycle gated attack, every Sid6581.Read($D41C)
    /// equals the Env3 of a lockstep reference ReSidEnvelope driven by the same
    /// writes and clocks, and the final value is non-zero
    /// (resid/envelope.cc:273-276). The reference is power-up-seeded
    /// (PowerUp: counter 0xaa, envelope.cc:176) because the chip's voices
    /// power up that way since FR-SID-ENV AC-08 was remediated; the AND of
    /// DAC-independence is untouched (Read returns the raw latch while the
    /// voice output path is DAC-mapped per AC-50).
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-ENV-49", ParityTag.Faithful)]
    public void Env_ReadEnv3_ReturnsRawEnvelopeCounterSample()
    {
        var sid = new Sid6581(new BasicBus()) { BaseAddress = 0xD400 };
        var reference = default(ReSidEnv);
        reference.PowerUp();

        sid.Write(0xD413, 0x00); // attack 0 (period 8), decay 0
        reference.WriteAttackDecay(0x00);
        sid.Write(0xD414, 0xF0); // sustain 0xF, release 0
        reference.WriteSustainRelease(0xF0);
        sid.Write(0xD412, 0x01); // gate on, no waveform needed for ENV3
        reference.WriteControl(0x01);

        for (var cycle = 0; cycle < 200; cycle++)
        {
            sid.Tick();
            reference.Clock();
            Assert.Equal(reference.Env3, sid.Read(0xD41C));
        }

        Assert.NotEqual((byte)0, reference.Env3);
    }

    /// <summary>
    /// FR: FR-SID-ENV, TR: TR-SID-ORACLE-001, TEST: TEST-SID-ENV-51.
    /// Use case: ADSR delay bug end-to-end: writing a rate below the current
    /// rate counter forces the counter to climb to the 15-bit wrap before the
    /// envelope can step (the audible stuck-envelope bug).
    /// Acceptance: in RELEASE with counter 0x80, release 0xF (period 31250) for
    /// exactly 10000 cycles (rate_counter == 10000), then release 0 (period 8):
    /// the next envelope step lands exactly (0x7fff - 10000) + 12 == 22779
    /// cycles later, at counter 0x7f (wrap to 1 at 0x8000, climb to 8, then the
    /// 4-cycle reset/exp/step tail) (resid/envelope.h:179-190).
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-ENV-51", ParityTag.Faithful)]
    public void Env_AdsrDelayBug_EndToEnd_DelaysStepUntilWrap()
    {
        var env = NewEnv();
        env.EnvelopeCounter = 0x80;
        env.WriteSustainRelease(0x0F); // release 0xF -> rate_period 31250

        for (var cycle = 0; cycle < 10000; cycle++)
        {
            env.Clock();
        }

        Assert.Equal(10000, env.RateCounter);
        Assert.Equal((byte)0x80, env.EnvelopeCounter);

        env.WriteSustainRelease(0x00); // release 0 -> rate_period 8, far below 10000

        var cyclesUntilStep = 0;
        for (var cycle = 1; cycle <= 30000; cycle++)
        {
            env.Clock();
            if (env.EnvelopeCounter != 0x80)
            {
                cyclesUntilStep = cycle;
                break;
            }
        }

        Assert.Equal(22779, cyclesUntilStep);
        Assert.Equal((byte)0x7f, env.EnvelopeCounter);
    }

    /// <summary>
    /// FR: FR-SID-ENV, TR: TR-SID-ORACLE-001, TEST: TEST-SID-ENV-52.
    /// Use case: freeze-at-zero bug: at counter 0xff, gating off to RELEASE and
    /// quickly back on to ATTACK makes the next attack step wrap 0xff to 0x00,
    /// where set_exponential_counter latches hold_zero and the counter freezes.
    /// Acceptance: after the release-then-attack gate sequence the first
    /// counter change lands exactly on 0x00 with hold_zero == true, and 1000
    /// further gated-attack cycles never move it
    /// (resid/envelope.h:164-168,408-417).
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-ENV-52", ParityTag.Faithful)]
    public void Env_FreezeAtZero_ReleaseThenAttackWrapFreezesAtZero()
    {
        // Established DECAY_SUSTAIN plateau at 0xff (post-attack, sustain 0xF).
        var env = NewEnv();
        env.WriteAttackDecay(0x00);    // attack 0, decay 0 (both period 8)
        env.WriteSustainRelease(0xF0); // sustain 0xF, release 0
        env.Gate = 1;
        env.State = EnvState.DecaySustain;
        env.EnvelopeCounter = 0xff;
        env.ExponentialCounterPeriod = 1;
        env.HoldZero = false;
        env.RateCounter = 0;

        env.WriteControl(0x00); // gate off -> RELEASE via state pipeline
        env.Clock();
        env.Clock();
        Assert.Equal(EnvState.Release, env.State);
        Assert.Equal((byte)0xff, env.EnvelopeCounter);

        env.WriteControl(0x01); // gate on -> ATTACK while the counter sits at 0xff

        var landed = false;
        for (var cycle = 0; cycle < 50 && !landed; cycle++)
        {
            env.Clock();
            landed = env.EnvelopeCounter != 0xff;
        }

        Assert.True(landed, "attack step from 0xff never fired");
        Assert.Equal((byte)0x00, env.EnvelopeCounter);
        Assert.True(env.HoldZero);

        for (var cycle = 0; cycle < 1000; cycle++)
        {
            env.Clock();
        }

        Assert.Equal((byte)0x00, env.EnvelopeCounter);
        Assert.Equal(EnvState.Attack, env.State);
    }

    /// <summary>
    /// FR: FR-SID-ENV, TR: TR-SID-ORACLE-001, TEST: TEST-SID-ENV-53.
    /// Use case: release-continue bug: gating a frozen zero counter to ATTACK
    /// clears hold_zero, and gating back to RELEASE before the first attack
    /// step lets the decrement wrap 0x00 to 0xff and keep counting down.
    /// Acceptance: after attack-then-release from a frozen 0x00, the first
    /// counter change lands exactly on 0xff and the next on 0xfe (counting
    /// down), with hold_zero clear (resid/envelope.h:146-154,135-137).
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-ENV-53", ParityTag.Faithful)]
    public void Env_ReleaseContinue_ZeroWrapsToFFAndCountsDown()
    {
        var env = NewEnv(); // release 0 -> period 8; exp period 1
        env.State = EnvState.Release;
        env.EnvelopeCounter = 0x00;
        env.HoldZero = true;

        env.WriteControl(0x01); // gate on: ATTACK scheduled
        env.Clock();
        env.Clock();            // state change fires: ATTACK, hold_zero cleared
        Assert.Equal(EnvState.Attack, env.State);
        Assert.False(env.HoldZero);
        Assert.Equal((byte)0x00, env.EnvelopeCounter);

        env.WriteControl(0x00); // gate off before any attack step
        env.Clock();
        env.Clock();            // state change fires: RELEASE
        Assert.Equal(EnvState.Release, env.State);

        var changes = new List<byte>();
        var previous = env.EnvelopeCounter;
        for (var cycle = 0; cycle < 40 && changes.Count < 2; cycle++)
        {
            env.Clock();
            if (env.EnvelopeCounter != previous)
            {
                changes.Add(env.EnvelopeCounter);
                previous = env.EnvelopeCounter;
            }
        }

        Assert.Equal(2, changes.Count);
        Assert.Equal((byte)0xff, changes[0]);
        Assert.Equal((byte)0xfe, changes[1]);
        Assert.False(env.HoldZero);
    }

    /// <summary>
    /// FR: FR-SID-ENV, TR: TR-SID-ORACLE-001, TEST: TEST-SID-ENV-54.
    /// Use case: the SID must clock the envelope one cycle per phi2 tick
    /// through reSID's single-cycle clock() (not the batched clock(delta_t)),
    /// preserving the pipeline delays: steady ATTACK steps land every
    /// rate_period + 1 cycles, the spacing the native oracle measured
    /// (SidExactOracleTests TEST-SID-ORACLE-P0-02).
    /// Acceptance: with voice 3 attack == 4 (rate_counter_period 148) gated on,
    /// ENV3 read via Sid6581.Read($D41C) increments with a steady-state spacing
    /// of exactly 149 Tick()s (resid/envelope.h:114-193).
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-ENV-54", ParityTag.Faithful)]
    public void Env_SidTick_ClocksEnvelopeSingleCycle()
    {
        var sid = new Sid6581(new BasicBus()) { BaseAddress = 0xD400 };
        sid.Write(0xD413, 0x40); // attack 4 -> period 148, decay 0
        sid.Write(0xD414, 0x00); // sustain 0, release 0
        sid.Write(0xD412, 0x01); // gate on

        var increments = new List<int>();
        byte previous = sid.Read(0xD41C);
        for (var tick = 1; tick <= 1200 && increments.Count < 5; tick++)
        {
            sid.Tick();
            byte current = sid.Read(0xD41C);
            if (current != previous)
            {
                increments.Add(tick);
                previous = current;
            }
        }

        Assert.True(increments.Count >= 4, $"expected at least 4 envelope steps, saw {increments.Count}");
        // Steady-state spacing (skip the gate-on transient before the first step).
        for (var i = 2; i < increments.Count; i++)
        {
            Assert.Equal(149, increments[i] - increments[i - 1]);
        }
    }
}
