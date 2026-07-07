namespace ViceSharp.TestHarness;

using ViceSharp.Chips.Audio;
using ViceSharp.Core;
using Xunit;

public sealed class SidRegisterReadbackTests
{
    /// <summary>
    /// FR: FR-Audio-SID, TR: TR-SID-VOICE3-READBACK.
    /// Use case: A C64 program programs SID voice 3 and reads back the
    /// OSC3 (<c>$D41B</c>) and ENV3 (<c>$D41C</c>) latches to drive scope
    /// or vibrato effects, exactly as on the MOS 6581.
    /// Acceptance: with sawtooth selected, <c>$D41B</c> returns the top 8
    /// bits of the selected waveform output (= accumulator bits 16-23 for
    /// sawtooth, reSID wave.cc:97,293-296) and <c>$D41C</c> reflects the
    /// envelope state, both independent of any previous bus write at those
    /// addresses. (Verified cycle-exact against reSID in
    /// SidEngineParityTests.)
    /// PLAN-VICEPARITY-001 S3 relock: the probe previously read OSC3 with no
    /// waveform selected (CTRL $01), which pinned the legacy waveform-0
    /// phase readback remediated by FR-SID-OSC3ENV3 AC-07 (waveform 0 reads
    /// the fading floating-DAC latch) and relied on the legacy zero power-on
    /// accumulator remediated by FR-SID-WAVE-ACC AC-05. The identical 0x02
    /// literal is now pinned via the test-bit-zeroed accumulator and a
    /// selected sawtooth, which reads the same on the legacy and the reSID
    /// paths.
    /// </summary>
    [Fact]
    public void Voice3Readback_UsesOsc3AndEnv3Addresses()
    {
        var sid = new Sid6581(new BasicBus()) { BaseAddress = 0xD400 };

        // Pin the voice-3 accumulator to zero via the CTRL test bit so the
        // closed form is independent of the power-on accumulator seed.
        sid.Write(0xD412, 0x08);
        sid.Tick();

        // Frequency 0x0100: after 512 ticks the accumulator is 0x020000, so
        // sawtooth OSC3 (bits 16-23) is 0x02 - independent of the 0xAA
        // written to $D41B.
        sid.Write(0xD40E, 0x00);
        sid.Write(0xD40F, 0x01);
        sid.Write(0xD413, 0x00);
        sid.Write(0xD414, 0xF0);
        sid.Write(0xD412, 0x21); // sawtooth + gate, test released
        sid.Write(0xD41B, 0xAA);
        sid.Write(0xD41C, 0xBB);

        for (var i = 0; i < 512; i++)
            sid.Tick();

        Assert.Equal(0x02, sid.Read(0xD41B));
        Assert.NotEqual(0xBB, sid.Read(0xD41C));
        Assert.True(sid.Read(0xD41C) > 0);
    }
}
