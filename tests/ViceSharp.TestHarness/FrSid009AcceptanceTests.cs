namespace ViceSharp.TestHarness;

using ViceSharp.Chips.Audio;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR-SID-009 acceptance tests. The oracle is the reSID noise LFSR used by
/// x64sc. The written "all-ones on test-bit write" sentence is not what
/// reSID does, and these tests follow reSID.
/// </summary>
public sealed class FrSid009AcceptanceTests
{
    /// <summary>
    /// FR: FR-SID-009.
    /// Use case: The noise LFSR is 23 bits with XOR feedback from bits 22
    /// and 17, clocked two cycles after bit 19 of the accumulator rises.
    /// Acceptance: From the reSID power-up register $7FFFFE, one clock
    /// produces $7FFFFC. The result stays inside 23 bits.
    /// </summary>
    [Fact]
    public void Lfsr_Is23BitsWithFeedbackFromBits22And17()
    {
        var sid = NewSid();
        Assert.Equal(0x7FFFFEu, sid.VoiceShiftRegister(0));

        sid.Write(0xD401, 0x80);
        for (var i = 0; i < 32; i++)
            sid.Tick();

        var shifted = sid.VoiceShiftRegister(0);
        Assert.Equal(0x7FFFFCu, shifted);
        Assert.True(shifted <= 0x7FFFFFu);
    }

    /// <summary>
    /// FR: FR-SID-009.
    /// Use case: Noise output is taken from LFSR bits 20, 18, 14, 11, 9, 5,
    /// 2, and 0. OSC3 publishes that 12-bit value shifted right by 4.
    /// Acceptance: With the power-up register, OSC3 equals the packing of
    /// those bits.
    /// </summary>
    [Fact]
    public void NoiseOutput_UsesTheResidTapBits()
    {
        var sid = NewSid();
        // OSC3 is voice 3. $D41B reads _voices[2], whose control register is $D412.
        var sr = sid.VoiceShiftRegister(2);
        sid.Write(0xD412, 0x80);

        var packed =
            ((sr & 0x100000) >> 9) |
            ((sr & 0x040000) >> 8) |
            ((sr & 0x004000) >> 5) |
            ((sr & 0x000800) >> 3) |
            ((sr & 0x000200) >> 2) |
            ((sr & 0x000020) << 1) |
            ((sr & 0x000004) << 3) |
            ((sr & 0x000001) << 4);

        Assert.Equal((byte)(packed >> 4), sid.Peek(0xD41B));
    }

    /// <summary>
    /// FR: FR-SID-009.
    /// Use case: reSID does not replace the shift register with all-ones
    /// when the test bit rises. It arms the shift-register reset counter.
    /// Acceptance: After a noise clock, raising the test bit leaves the
    /// shift register unchanged and arms a non-zero reset count.
    /// </summary>
    [Fact]
    public void TestBitRising_ArmsResetAndDoesNotReplaceTheRegister()
    {
        var sid = NewSid();
        sid.Write(0xD401, 0x80);
        sid.Write(0xD404, 0x80);
        for (var i = 0; i < 32; i++)
            sid.Tick();

        var before = sid.VoiceShiftRegister(0);
        sid.Write(0xD404, 0x88);

        Assert.Equal(before, sid.VoiceShiftRegister(0));
        Assert.NotEqual(0x7FFFFFu, sid.VoiceShiftRegister(0));
        Assert.NotEqual(0u, sid.VoiceShiftRegisterReset(0));
    }

    /// <summary>
    /// FR: FR-SID-009.
    /// Use case: Selecting noise together with another waveform writes the
    /// other waveform's zero bits back into the LFSR taps.
    /// Acceptance: Noise combined with pulse changes the shift register
    /// away from the pure-noise value.
    /// </summary>
    [Fact]
    public void CombinedWaveform_WritesBackIntoTheLfsr()
    {
        var sid = NewSid();
        var before = sid.VoiceShiftRegister(0);

        sid.Write(0xD404, 0xC0);

        Assert.NotEqual(before, sid.VoiceShiftRegister(0));
    }

    private static Sid6581 NewSid() => new(new BasicBus()) { BaseAddress = 0xD400 };
}
