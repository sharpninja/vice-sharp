namespace ViceSharp.TestHarness;

using System.Runtime.InteropServices;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// PLAN-VICEPARITY-001 Phase 0 (P0-1) / TR-SID-ORACLE-001.
/// Self-proof suite for the single-cycle reSID oracle (vice_sid_exact_*).
/// The batched vice_sid_clock path drives reSID via clock(delta_t), which by
/// reSID's own comment drops the single-cycle envelope pipeline ("Any pipelined
/// envelope counter decrement from single cycle clocking will be lost",
/// resid/envelope.h). Bit-exact SID parity ACs therefore need reSID::SID::clock()
/// driven one cycle at a time. These tests prove the exact API is wired, exact,
/// and deterministic before any parity slice relies on it.
/// </summary>
[Collection("NativeVice")]
public sealed class SidExactOracleTests
{
    private const ushort FreqLoV3 = 0x0E;
    private const ushort FreqHiV3 = 0x0F;
    private const ushort ControlV3 = 0x12;
    private const ushort AttackDecayV3 = 0x13;
    private const ushort SustainReleaseV3 = 0x14;
    private const ushort Osc3 = 0x1B;
    private const ushort Env3 = 0x1C;

    /// <summary>
    /// FR: FR-SID-WAVE-ACC, TR: TR-SID-ORACLE-001, TEST: TEST-SID-ORACLE-P0-01.
    /// Use case: the oracle must advance the 24-bit phase accumulator exactly one
    /// frequency step per clocked cycle so waveform ACs can assert closed-form
    /// phase equality.
    /// Acceptance: with voice 3 sawtooth, TEST bit pulse to zero the accumulator,
    /// then freq=0x1000 for exactly 1000 exact cycles, the oracle state reports
    /// accumulator == (0x1000 * 1000) &amp; 0xFFFFFF and OSC3 == accumulator &gt;&gt; 16
    /// (bit-exact, no tolerance).
    /// </summary>
    [ViceFact]
    public void ExactClock_AccumulatorFollowsClosedForm()
    {
        var native = ViceNativeBridge.CreateMachine("c64");
        try
        {
            Assert.True(ViceNativeBridge.SidExactOpen(native), "exact oracle failed to open");
            ViceNativeBridge.SidExactReset(native);

            // Hold the accumulator at zero via the TEST bit, then release and count.
            ViceNativeBridge.SidExactWrite(native, FreqLoV3, 0x00);
            ViceNativeBridge.SidExactWrite(native, FreqHiV3, 0x10); // freq = 0x1000
            ViceNativeBridge.SidExactWrite(native, ControlV3, 0x28); // TEST | SAW
            ViceNativeBridge.SidExactClock(native, 8);
            ViceNativeBridge.SidExactWrite(native, ControlV3, 0x20); // SAW, TEST released

            ViceNativeBridge.SidExactClock(native, 1000);

            var state = ViceNativeBridge.SidExactGetState(native);
            uint expected = (0x1000u * 1000u) & 0xFFFFFF;
            Assert.Equal(expected, state.GetAccumulators()[2]);
            Assert.Equal((byte)(expected >> 16), ViceNativeBridge.SidExactRead(native, Osc3));
        }
        finally
        {
            ViceNativeBridge.DestroyMachine(native);
        }
    }

    /// <summary>
    /// FR: FR-SID-ENV, TR: TR-SID-ORACLE-001, TEST: TEST-SID-ORACLE-P0-02.
    /// Use case: envelope ACs need the oracle to reproduce reSID's single-cycle
    /// envelope pipeline; the batched clock(delta_t) path steps the envelope at
    /// rate_period cycles and loses the pipeline delay.
    /// Acceptance: voice 3 in steady ATTACK with attack=4 (rate_counter_period
    /// 148 per resid/envelope.cc) increments ENV3 every rate_period + 1 = 149
    /// cycles exactly (single-cycle trace: counter reaches 148, one reset/arm
    /// cycle, two envelope_pipeline cycles overlapping the next count).
    /// </summary>
    [ViceFact]
    public void ExactClock_AttackStepSpacing_MatchesSingleCyclePipeline()
    {
        var native = ViceNativeBridge.CreateMachine("c64");
        try
        {
            Assert.True(ViceNativeBridge.SidExactOpen(native), "exact oracle failed to open");
            ViceNativeBridge.SidExactReset(native);

            ViceNativeBridge.SidExactWrite(native, AttackDecayV3, 0x40); // attack=4 -> period 148
            ViceNativeBridge.SidExactWrite(native, SustainReleaseV3, 0x00);
            ViceNativeBridge.SidExactWrite(native, ControlV3, 0x01); // gate on

            var increments = new List<int>();
            byte previous = ViceNativeBridge.SidExactGetState(native).GetEnvelopeCounters()[2];
            for (int cycle = 1; cycle <= 1200 && increments.Count < 5; cycle++)
            {
                ViceNativeBridge.SidExactClock(native, 1);
                byte current = ViceNativeBridge.SidExactGetState(native).GetEnvelopeCounters()[2];
                if (current != previous)
                {
                    increments.Add(cycle);
                    previous = current;
                }
            }

            Assert.True(increments.Count >= 4, $"expected at least 4 envelope steps, saw {increments.Count}");
            // Steady-state spacing (skip the gate-on transient before the first step).
            for (int i = 2; i < increments.Count; i++)
            {
                Assert.Equal(149, increments[i] - increments[i - 1]);
            }
        }
        finally
        {
            ViceNativeBridge.DestroyMachine(native);
        }
    }

    /// <summary>
    /// FR: FR-SID-CLOCK, TR: TR-SID-ORACLE-001, TEST: TEST-SID-ORACLE-P0-03.
    /// Use case: every parity AC assumes the oracle is a pure deterministic
    /// function of its register program; two identical programs must yield
    /// identical internal state.
    /// Acceptance: two independently created machines running the same
    /// reset/write/clock sequence report byte-identical vice_sid_exact_state
    /// structs (full struct memory compare, delta 0).
    /// </summary>
    [ViceFact]
    public void ExactClock_IsDeterministicAcrossInstances()
    {
        static ViceNative.ViceSidExactState RunProgram()
        {
            var native = ViceNativeBridge.CreateMachine("c64");
            try
            {
                Assert.True(ViceNativeBridge.SidExactOpen(native), "exact oracle failed to open");
                ViceNativeBridge.SidExactReset(native);
                ViceNativeBridge.SidExactWrite(native, FreqLoV3, 0x37);
                ViceNativeBridge.SidExactWrite(native, FreqHiV3, 0x13);
                ViceNativeBridge.SidExactWrite(native, AttackDecayV3, 0x24);
                ViceNativeBridge.SidExactWrite(native, SustainReleaseV3, 0xA5);
                ViceNativeBridge.SidExactWrite(native, ControlV3, 0x21); // saw + gate
                ViceNativeBridge.SidExactClock(native, 777);
                return ViceNativeBridge.SidExactGetState(native);
            }
            finally
            {
                ViceNativeBridge.DestroyMachine(native);
            }
        }

        var first = RunProgram();
        var second = RunProgram();

        var firstBytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref first, 1));
        var secondBytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref second, 1));
        Assert.True(firstBytes.SequenceEqual(secondBytes), "exact oracle state diverged between identical runs");
    }

    /// <summary>
    /// FR: FR-SID-ENV, TR: TR-SID-ORACLE-001, TEST: TEST-SID-ORACLE-P0-04.
    /// Use case: envelope ACs read ENV3 ($D41C) through the oracle's register
    /// read path; it must agree with the exported internal envelope counter.
    /// Acceptance: after attack completes and DECAY_SUSTAIN holds at
    /// sustain=0xF, ENV3 register read == exported envelope_counter[2] == 0xFF
    /// (a quiet plateau, so the ENV3 sample latch and the live counter agree).
    /// </summary>
    [ViceFact]
    public void ExactRead_Env3MatchesExportedEnvelopeCounter()
    {
        var native = ViceNativeBridge.CreateMachine("c64");
        try
        {
            Assert.True(ViceNativeBridge.SidExactOpen(native), "exact oracle failed to open");
            ViceNativeBridge.SidExactReset(native);

            ViceNativeBridge.SidExactWrite(native, AttackDecayV3, 0x00); // fastest attack
            ViceNativeBridge.SidExactWrite(native, SustainReleaseV3, 0xF0); // sustain 0xF
            ViceNativeBridge.SidExactWrite(native, ControlV3, 0x01); // gate on

            // Fastest attack: 255 steps * (8+1) cycles plus transient; 5000 is far past the plateau.
            ViceNativeBridge.SidExactClock(native, 5000);

            var state = ViceNativeBridge.SidExactGetState(native);
            byte env3 = ViceNativeBridge.SidExactRead(native, Env3);
            Assert.Equal(0xFF, state.GetEnvelopeCounters()[2]);
            Assert.Equal(env3, state.GetEnvelopeCounters()[2]);
        }
        finally
        {
            ViceNativeBridge.DestroyMachine(native);
        }
    }
}
