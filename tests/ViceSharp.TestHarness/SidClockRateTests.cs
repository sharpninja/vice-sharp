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
    /// Acceptance: With voice-3 frequency 0x0100, after stepping the SystemClock 8192
    ///   master cycles, OSC3 ($D41B = the high byte of the 24-bit oscillator, bits 16-23)
    ///   reads 0x20 (= ((8192 x 0x0100) >> 16) &amp; 0xff). The 16x-slow bug ticks only 512
    ///   times and reads 0x02. (OSC3 = bits 16-23, verified cycle-exact against reSID in
    ///   SidEngineParityTests; the old >> 24 readback over-read by one byte.)
    /// </summary>
    [Fact]
    public void SidPhase_AdvancesAtPhi2Rate_WhenClockedBySystemClock()
    {
        var bus = new BasicBus();
        var sid = new Sid6581(bus) { BaseAddress = 0xD400 };
        var clock = new SystemClock(985_248);
        clock.Register(sid);

        // Voice 3 frequency = 0x0100 (FRELO=0x00 @ $D40E, FREHI=0x01 @ $D40F).
        // 0x0100 x 8192 = 0x200000 does not wrap the 24-bit accumulator, so the
        // high byte is an unambiguous 0x20 (vs 0x02 for the 16x-slow 512-tick bug).
        sid.Write(0xD40E, 0x00);
        sid.Write(0xD40F, 0x01);

        clock.Step(8192);

        Assert.Equal(0x20, sid.Read(0xD41B));
    }
}
