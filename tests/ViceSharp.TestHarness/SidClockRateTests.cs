namespace ViceSharp.TestHarness;

using ViceSharp.Chips.Audio;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// BUG-SIDAUDIO-001: the SID phase accumulator must advance at the phi2 master-clock
/// rate (once per cycle), not at 1/16th (it was registered as a slow device with
/// ClockDivisor=16 while advancing the accumulator once per Tick), so notes play at the
/// correct pitch and the audio sample rate is correct enough to be a timing source.
/// </summary>
public sealed class SidClockRateTests
{
    /// <summary>
    /// FR-SIDAUDIO-001, TR-SIDAUDIO-CLOCK-001, TEST-SIDAUDIO-001.
    /// Use case: When the SID is driven by the SystemClock, stepping N master cycles must
    ///   advance a voice's phase by N x Frequency (the accumulator increments every phi2
    ///   cycle), so the output frequency is Frequency x phi2 / 2^24 - the correct pitch.
    /// Acceptance: With voice-3 frequency 0x0100 and sawtooth selected, after stepping
    ///   the SystemClock 8192 master cycles, OSC3 ($D41B = the top 8 bits of the selected
    ///   waveform output; for sawtooth that is accumulator bits 16-23, reSID wave.cc:97)
    ///   reads 0x20 (= ((8192 x 0x0100) >> 16) &amp; 0xff). The 16x-slow bug ticks only 512
    ///   times and reads 0x02. (Verified cycle-exact against reSID in
    ///   SidEngineParityTests; the old >> 24 readback over-read by one byte.)
    /// PLAN-VICEPARITY-001 S3 relock: the probe previously read OSC3 with no waveform
    ///   selected, pinning the legacy waveform-0 phase readback (remediated by
    ///   FR-SID-OSC3ENV3 AC-07) and the legacy zero power-on accumulator (remediated by
    ///   FR-SID-WAVE-ACC AC-05). The identical 0x20 literal is now pinned via the
    ///   test-bit-zeroed accumulator and a selected sawtooth.
    /// </summary>
    [Fact]
    public void SidPhase_AdvancesAtPhi2Rate_WhenClockedBySystemClock()
    {
        var bus = new BasicBus();
        var sid = new Sid6581(bus) { BaseAddress = 0xD400 };
        var clock = new SystemClock(985_248);
        clock.Register(sid);

        // Pin the voice-3 accumulator to zero via the CTRL test bit so the
        // closed form is independent of the power-on accumulator seed.
        sid.Write(0xD412, 0x08);
        clock.Step(1);
        sid.Write(0xD412, 0x20); // sawtooth, test released

        // Voice 3 frequency = 0x0100 (FRELO=0x00 @ $D40E, FREHI=0x01 @ $D40F).
        // 0x0100 x 8192 = 0x200000 does not wrap the 24-bit accumulator, so the
        // high byte is an unambiguous 0x20 (vs 0x02 for the 16x-slow 512-tick bug).
        sid.Write(0xD40E, 0x00);
        sid.Write(0xD40F, 0x01);

        clock.Step(8192);

        Assert.Equal(0x20, sid.Read(0xD41B));
    }
}
