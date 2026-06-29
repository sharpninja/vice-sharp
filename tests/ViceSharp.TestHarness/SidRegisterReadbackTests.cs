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
    /// Acceptance: <c>$D41B</c> returns the high byte (bits 16-23) of the 24-bit
    /// voice 3 phase accumulator and <c>$D41C</c> reflects the envelope state, both
    /// independent of any previous bus write at those addresses. (OSC3 = bits 16-23,
    /// verified cycle-exact against reSID in SidEngineParityTests.)
    /// </summary>
    [Fact]
    public void Voice3Readback_UsesOsc3AndEnv3Addresses()
    {
        var sid = new Sid6581(new BasicBus()) { BaseAddress = 0xD400 };

        // Frequency 0x0100: after 512 ticks the accumulator is 0x020000, so the
        // high byte (bits 16-23) is 0x02 - independent of the 0xAA written to $D41B.
        sid.Write(0xD40E, 0x00);
        sid.Write(0xD40F, 0x01);
        sid.Write(0xD412, 0x01);
        sid.Write(0xD413, 0x00);
        sid.Write(0xD414, 0xF0);
        sid.Write(0xD41B, 0xAA);
        sid.Write(0xD41C, 0xBB);

        for (var i = 0; i < 512; i++)
            sid.Tick();

        Assert.Equal(0x02, sid.Read(0xD41B));
        Assert.NotEqual(0xBB, sid.Read(0xD41C));
        Assert.True(sid.Read(0xD41C) > 0);
    }
}
