namespace ViceSharp.TestHarness;

using FluentAssertions;
using ViceSharp.Chips.Audio;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: FR-SID-006 (BACKFILL-SID-001 Slice 4).
/// Use case: The MOS 6581 SID envelope generator uses a 15-bit prescaler
/// counter (0..32767) that ticks once per host cycle. The counter is
/// compared against a per-stage threshold selected by the ATK/DCY/SUS/REL
/// nibbles. When the counter matches the threshold, the envelope advances
/// one step and the counter resets to zero. Critically, when the ATK/DCY/
/// SUS/REL register is rewritten mid-stage with a new threshold value, the
/// prescaler counter is NOT reset; it continues counting. If the new
/// threshold is LESS than the current counter value, the counter must
/// wrap around (15-bit, ~32k cycles ~32 ms at 1 MHz) before the next match
/// fires, leaving the envelope stalled. This is the famous SID ADSR bug,
/// audible in C64 music as "stuck note" or frozen-envelope effects.
/// </summary>
public sealed class SidAdsrBugTests
{
    private static Sid6581 BuildSid()
    {
        return new Sid6581(new BasicBus()) { BaseAddress = 0xD400 };
    }

    private static void StartAttack(Sid6581 sid, byte attackRateNibble)
    {
        // Voice 3 (V3 control = $D412) so we can read ENV3 at $D41C.
        // Set ATK/DCY: attack rate in high nibble, decay 0 in low.
        sid.Write(0xD413, (byte)((attackRateNibble & 0x0F) << 4));
        // Sustain F (max), release 0 so envelope stays at peak after attack.
        sid.Write(0xD414, 0xF0);
        // Triangle waveform + gate ON to trigger attack.
        sid.Write(0xD412, 0x11);
    }

    private static void StartRelease(Sid6581 sid, byte releaseRateNibble)
    {
        // First bring envelope to a known non-zero value by completing attack at
        // the fastest rate (rate 0, ~9 cycles per step, full ramp ~2300 cycles).
        sid.Write(0xD413, 0x00);   // attack rate 0
        sid.Write(0xD414, 0xF0);   // sustain F, release 0
        sid.Write(0xD412, 0x11);   // gate ON
        for (var i = 0; i < 4000; i++) sid.Tick();
        // Now set release rate and drop gate to enter release.
        sid.Write(0xD414, (byte)(0xF0 | (releaseRateNibble & 0x0F)));
        sid.Write(0xD412, 0x10);   // gate OFF
    }

    /// <summary>
    /// FR/TR: FR-SID-006
    /// Use case: Baseline sanity. With a fast attack rate and no mid-stage
    /// rate change, the envelope ramps from 0 to 255 within the expected
    /// number of host cycles. No bug, no stall.
    /// Acceptance: After 5000 ticks at attack rate 0 (~9 cycles/step),
    /// ENV3 readback ($D41C) reports a non-zero envelope level.
    /// </summary>
    [Fact]
    public void Baseline_AttackAtFastestRate_ReachesPeakWithoutStall()
    {
        var sid = BuildSid();
        StartAttack(sid, 0x00);

        for (var i = 0; i < 5000; i++) sid.Tick();

        var env = sid.Read(0xD41C);
        env.Should().BeGreaterThan(0, "fastest attack ramp must produce non-zero envelope within 5000 cycles");
    }

    /// <summary>
    /// FR/TR: FR-SID-006
    /// Use case: Mid-attack, software rewrites the attack rate to a LOWER
    /// threshold (faster nibble = lower numeric threshold). Because the
    /// 15-bit prescaler counter is already above the new threshold, the
    /// counter must wrap (~32k cycles) before the next envelope step. The
    /// envelope is stalled for the duration of that wrap.
    /// Acceptance: After triggering the stall, ENV3 stays frozen at the
    /// same value for at least 1000 consecutive cycles (well within the
    /// ~32k-cycle wrap window), proving the bug reproduces.
    /// </summary>
    [Fact]
    public void AttackRateChangeToLowerThreshold_StallsEnvelope()
    {
        var sid = BuildSid();
        // Begin attack at a SLOW rate (nibble 8 = threshold 392) so that the
        // 15-bit counter accumulates a value much larger than the new (smaller)
        // threshold once we switch.
        StartAttack(sid, 0x08);

        // Tick enough cycles to push the prescaler well past the next (faster)
        // threshold but not yet match threshold 392. Choose 150 ticks: counter
        // ~150 (< 392 so no step has fired yet) and clearly > 9 (rate 0).
        for (var i = 0; i < 150; i++) sid.Tick();

        var envBeforeChange = sid.Read(0xD41C);

        // Now switch to attack rate 0 (threshold 9). Counter (~150) > 9, so the
        // counter must wrap up to 32768 before matching 9 again.
        sid.Write(0xD413, 0x00); // ATK = 0, DCY = 0

        // Tick well past the new threshold but well before the wrap completes.
        // At 1000 cycles after the switch, a non-buggy implementation would
        // have stepped the envelope dozens of times (1000 / 9 ~= 111 steps).
        // A bug-accurate implementation has not stepped even once yet.
        for (var i = 0; i < 1000; i++) sid.Tick();

        var envAfterStall = sid.Read(0xD41C);
        envAfterStall.Should().Be(envBeforeChange,
            "ADSR bug: counter must wrap (~32k cycles) before next step fires; envelope frozen for >1000 cycles");
    }

    /// <summary>
    /// FR/TR: FR-SID-006
    /// Use case: Mid-release, software rewrites the release rate to a LOWER
    /// threshold. Same wrap behaviour as attack: the prescaler counter is
    /// not reset on rate-write, so a lower threshold forces a full 15-bit
    /// wrap before the next release step.
    /// Acceptance: After triggering the stall during release, ENV3 stays
    /// frozen for at least 1000 consecutive cycles.
    /// </summary>
    [Fact]
    public void ReleaseRateChangeToLowerThreshold_StallsEnvelope()
    {
        var sid = BuildSid();
        // Bring envelope up, then enter release with a SLOW rate (nibble 8).
        StartRelease(sid, 0x08);

        // Let prescaler accumulate past the future-faster threshold (9).
        for (var i = 0; i < 150; i++) sid.Tick();

        var envBeforeChange = sid.Read(0xD41C);
        envBeforeChange.Should().BeGreaterThan(0, "envelope must be in release with non-zero level");

        // Switch release rate to 0 (threshold 9). Counter (~150) > 9 -> wrap.
        sid.Write(0xD414, 0xF0); // sustain F, release 0

        for (var i = 0; i < 1000; i++) sid.Tick();

        var envAfterStall = sid.Read(0xD41C);
        envAfterStall.Should().Be(envBeforeChange,
            "ADSR bug: release stalls for full 15-bit wrap when new threshold is below current counter");
    }

    /// <summary>
    /// FR/TR: FR-SID-006
    /// Use case: When software rewrites the rate-select to a HIGHER
    /// threshold (slower nibble), the counter is still below the new
    /// threshold so no wrap is required; the envelope simply takes longer
    /// to reach the next step. This case does NOT trigger the bug.
    /// Acceptance: After increasing the attack threshold mid-stage, the
    /// envelope still advances within a reasonable window (well under the
    /// 15-bit wrap), proving the bug is asymmetric.
    /// </summary>
    [Fact]
    public void AttackRateChangeToHigherThreshold_DoesNotTriggerBug()
    {
        var sid = BuildSid();
        // Begin attack at rate 0 (threshold 9). Counter will tick 0..9 repeatedly.
        StartAttack(sid, 0x00);

        // Tick a small amount: counter is at most 8 (below threshold 9 = next step
        // would fire on next tick), more likely some value in 0..8 after a step.
        for (var i = 0; i < 5; i++) sid.Tick();

        var envBeforeChange = sid.Read(0xD41C);

        // Switch to rate 4 (threshold 149). Counter (0..8) is well BELOW 149,
        // so no wrap is needed. The envelope will step on the very next 149-cycle
        // boundary - not a 32k-cycle stall.
        sid.Write(0xD413, 0x40); // ATK = 4, DCY = 0

        // Tick well past 149 cycles. The envelope must advance.
        for (var i = 0; i < 2000; i++) sid.Tick();

        var envAfter = sid.Read(0xD41C);
        envAfter.Should().BeGreaterThan(envBeforeChange,
            "higher threshold mid-stage does not stall - envelope still steps once counter reaches the new threshold");
    }
}
