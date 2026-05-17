namespace ViceSharp.TestHarness;

using ViceSharp.Chips;
using Xunit;

public sealed class SidOscillatorTests
{
    [Fact]
    public void NoiseWaveform_ProducesNonZeroOutputFromDefaultLfsr()
    {
        var oscillator = new SidOscillator
        {
            Waveform = 0x80
        };

        Assert.Equal(0xFF, oscillator.Output());
    }

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

    [Fact]
    public void NoiseWaveform_IsDeterministicForSameFrequencyAndClocking()
    {
        var first = GenerateNoiseSequence();
        var second = GenerateNoiseSequence();

        Assert.Equal(first, second);
        Assert.Contains(first, sample => sample != 0xFF);
    }

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
