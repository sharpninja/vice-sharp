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
    /// FR-SID-TIMING-001, TR-SID-PHI2-001.
    /// Use case: When the SID is driven by the SystemClock, stepping N master cycles must
    ///   advance a voice's phase by N x Frequency (the accumulator increments every phi2
    ///   cycle), so the output frequency is Frequency x phi2 / 2^24 - the correct pitch.
    /// Acceptance: With voice-3 frequency 0x8000, after stepping the SystemClock 8192
    ///   master cycles, OSC3 ($D41B = accumulator bits 24-31) reads 0x10
    ///   (= (8192 x 0x8000) >> 24). The 16x-slow bug ticks only 512 times and reads 0x01.
    /// </summary>
    [Fact]
    public void SidPhase_AdvancesAtPhi2Rate_WhenClockedBySystemClock()
    {
        var bus = new BasicBus();
        var sid = new Sid6581(bus) { BaseAddress = 0xD400 };
        var clock = new SystemClock(985_248);
        clock.Register(sid);

        // Voice 3 frequency = 0x8000 (FRELO=0x00 @ $D40E, FREHI=0x80 @ $D40F).
        sid.Write(0xD40E, 0x00);
        sid.Write(0xD40F, 0x80);

        clock.Step(8192);

        Assert.Equal(0x10, sid.Read(0xD41B));
    }
}
