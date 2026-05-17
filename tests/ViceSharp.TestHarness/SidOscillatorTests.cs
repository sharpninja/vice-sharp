namespace ViceSharp.TestHarness;

using ViceSharp.Chips;
using Xunit;

public sealed class SidOscillatorTests
{
    /// <summary>
    /// FR: FR-Audio-SID, TR: TR-SID-NOISE-LFSR.
    /// Use case: A program selects the noise waveform on a SID voice without
    /// stepping the oscillator and reads the immediate output value.
    /// Acceptance: With the LFSR in its reset state (all ones in the tapped
    /// bits) the noise output reads back as <c>0xFF</c>.
    /// </summary>
    [Fact]
    public void NoiseWaveform_ProducesNonZeroOutputFromDefaultLfsr()
    {
        var oscillator = new SidOscillator
        {
            Waveform = 0x80
        };

        Assert.Equal(0xFF, oscillator.Output());
    }

    /// <summary>
    /// FR: FR-Audio-SID, TR: TR-SID-NOISE-LFSR.
    /// Use case: The SID oscillator runs with a known frequency; the noise
    /// LFSR must only advance on the bit-19 low-to-high transition of the
    /// 24-bit phase accumulator, matching the MOS 6581 hardware.
    /// Acceptance: After 15 steps the noise output is unchanged at
    /// <c>0xFF</c>; the 16th step produces the first LFSR advance and the
    /// output becomes <c>0xFE</c>.
    /// </summary>
    [Fact]
    public void NoiseWaveform_ClocksWhenAccumulatorBit19TransitionsHigh()
    {
        var oscillator = new SidOscillator
        {
            Frequency = 0x8000,
            Waveform = 0x80
        };

        for (var i = 0; i < 15; i++)
            oscillator.Step();

        Assert.Equal(0xFF, oscillator.Output());

        oscillator.Step();

        Assert.Equal(0xFE, oscillator.Output());
    }

    /// <summary>
    /// FR: FR-Audio-SID, TR: TR-SID-NOISE-LFSR.
    /// Use case: Two independently constructed SID oscillators driven with
    /// the same frequency and step count must emit identical noise samples
    /// so that replay traces and lockstep validation are reproducible.
    /// Acceptance: Two captured sample sequences are byte-for-byte equal
    /// and contain at least one non-<c>0xFF</c> sample (proving the LFSR
    /// actually advanced).
    /// </summary>
    [Fact]
    public void NoiseWaveform_IsDeterministicForSameFrequencyAndClocking()
    {
        var first = GenerateNoiseSequence();
        var second = GenerateNoiseSequence();

        Assert.Equal(first, second);
        Assert.Contains(first, sample => sample != 0xFF);
    }

    /// <summary>
    /// FR: FR-Audio-SID, TR: TR-SID-NOISE-LFSR.
    /// Use case: A program toggles the SID voice TEST bit (waveform
    /// bit 3) to re-seed the noise LFSR and clear the phase accumulator,
    /// matching the MOS 6581 test-bit semantics relied upon by many demos.
    /// Acceptance: After clearing TEST the accumulator is zero and the
    /// noise output is <c>0xFF</c> (LFSR re-seeded with the reset pattern).
    /// </summary>
    [Fact]
    public void TestBit_ResetsNoiseLfsrAndAccumulator()
    {
        var oscillator = new SidOscillator
        {
            Frequency = 0x8000,
            Waveform = 0x80
        };

        for (var i = 0; i < 32; i++)
            oscillator.Step();

        Assert.NotEqual(0xFF, oscillator.Output());

        oscillator.Waveform = 0x88;
        oscillator.Step();
        oscillator.Waveform = 0x80;

        Assert.Equal(0u, oscillator.Accumulator);
        Assert.Equal(0xFF, oscillator.Output());
    }

    private static byte[] GenerateNoiseSequence()
    {
        var oscillator = new SidOscillator
        {
            Frequency = 0x8000,
            Waveform = 0x80
        };
        var samples = new byte[8];

        for (var sample = 0; sample < samples.Length; sample++)
        {
            for (var i = 0; i < 16; i++)
                oscillator.Step();

            samples[sample] = oscillator.Output();
        }

        return samples;
    }
}
