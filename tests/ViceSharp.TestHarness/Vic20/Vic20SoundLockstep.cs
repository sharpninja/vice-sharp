namespace ViceSharp.TestHarness.Vic20;

using ViceSharp.Chips.Vic;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR-VIC20-SOUND-001 / TEST-VIC20-SD-*: VIC-I sound determinism and register side effects.
/// Use case: silence + single-channel + multi-register fixtures produce stable PCM.
/// Acceptance: two-run SequenceEqual; stores update model; mute stays near-zero.
/// VICE: native/vice/vice/src/vic20/vic20sound.c
/// </summary>
[Collection("NativeVice")]
public sealed class Vic20SoundLockstep
{
    private const int SampleRate = 44100;
    private const int PalCycles = 1_108_405;
    private const int BudgetCycles = 50_000;

    [Fact]
    public void Silence_MatchesAcrossTwoRuns()
    {
        var a = RenderSilence();
        var b = RenderSilence();
        Assert.Equal(a.Length, b.Length);
        Assert.True(a.AsSpan().SequenceEqual(b), "silence must be bit-identical across runs");
    }

    [Fact]
    public void Tone_Channel0_MatchesAcrossTwoRuns()
    {
        var a = RenderTone(0xA, 0x80 | 64, volume: 8);
        var b = RenderTone(0xA, 0x80 | 64, volume: 8);
        Assert.True(a.AsSpan().SequenceEqual(b), "tone must be bit-identical across runs");
        // Not all zeros when volume and enable set.
        Assert.Contains(a, s => s != 0);
    }

    [Fact]
    public void MultiReg_MatchesAcrossTwoRuns()
    {
        short[] Render()
        {
            var snd = new Vic20Sound();
            snd.Init(SampleRate, PalCycles);
            snd.Store(0xA, 0x80 | 40);
            snd.Store(0xB, 0x80 | 50);
            snd.Store(0xE, 10);
            var buf = new short[2048];
            var n = snd.RenderSamples(buf, BudgetCycles);
            return buf.AsSpan(0, n).ToArray();
        }

        var a = Render();
        var b = Render();
        Assert.True(a.AsSpan().SequenceEqual(b));
        Assert.Contains(a, s => s != 0);
    }

    [Fact]
    public void Mos6561_WriteSoundRegs_UpdatesEngine()
    {
        var vic = new Mos6561();
        vic.Write(0x900E, 0x0F);
        vic.Write(0x900A, 0x80 | 32);
        var buf = new short[512];
        var n = vic.Sound.RenderSamples(buf, 20_000);
        Assert.True(n > 0);
        Assert.Contains(buf.AsSpan(0, n).ToArray(), s => s != 0);
    }

    [Fact]
    public void Store_DoesNotAllocateOnHotPath_WhenSilent()
    {
        var snd = new Vic20Sound();
        snd.Init(SampleRate, PalCycles);
        // Warm up
        snd.Clock(100);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 10_000; i++)
            snd.Clock(1);
        var after = GC.GetAllocatedBytesForCurrentThread();
        Assert.Equal(0, after - before);
    }

    /// <summary>AC-SD-03: silence sample buffer SequenceEqual vs xvic for fixed cycle budget.</summary>
    [Fact]
    public void Silence_MatchesXvic()
    {
        if (!ViceNativeXvic.IsAvailable)
            return;

        using var native = ViceNative.CreateInstance("vic20");
        native.Reset();
        var managed = new Vic20Sound();
        managed.Init(SampleRate, PalCycles);

        var nBuf = new short[4096];
        var mBuf = new short[4096];
        var n = native.RenderVic20Samples(nBuf, SampleRate, PalCycles, BudgetCycles);
        var m = managed.RenderSamples(mBuf, BudgetCycles);
        Assert.True(n > 0 && m > 0);
        var len = Math.Min(n, m);
        Assert.True(
            nBuf.AsSpan(0, len).SequenceEqual(mBuf.AsSpan(0, len)),
            "silence PCM must SequenceEqual managed vs xvic");
    }

    /// <summary>AC-SD-04: single-channel tone SequenceEqual vs xvic.</summary>
    [Fact]
    public void Tone_MatchesXvic()
    {
        if (!ViceNativeXvic.IsAvailable)
            return;

        using var native = ViceNative.CreateInstance("vic20");
        native.Reset();
        // Write sound regs through bus so VICE sound_store path receives them.
        native.WriteBus(0x900E, 8);
        native.WriteBus(0x900A, 0x80 | 64);

        var managed = new Vic20Sound();
        managed.Init(SampleRate, PalCycles);
        managed.Store(0xE, 8);
        managed.Store(0xA, 0x80 | 64);

        var nBuf = new short[4096];
        var mBuf = new short[4096];
        var n = native.RenderVic20Samples(nBuf, SampleRate, PalCycles, BudgetCycles);
        var m = managed.RenderSamples(mBuf, BudgetCycles);
        Assert.True(n > 0 && m > 0, $"n={n} m={m}");
        var len = Math.Min(n, m);
        if (!nBuf.AsSpan(0, len).SequenceEqual(mBuf.AsSpan(0, len)))
        {
            var first = -1;
            for (var i = 0; i < len; i++)
            {
                if (nBuf[i] != mBuf[i])
                {
                    first = i;
                    break;
                }
            }

            throw new Xunit.Sdk.XunitException(
                $"tone PCM mismatch first={first} n={nBuf[first]} m={mBuf[first]} len={len} " +
                $"nNonZero={nBuf.AsSpan(0, len).ToArray().Count(s => s != 0)} " +
                $"mNonZero={mBuf.AsSpan(0, len).ToArray().Count(s => s != 0)}");
        }
    }

    private static short[] RenderSilence()
    {
        var snd = new Vic20Sound();
        snd.Init(SampleRate, PalCycles);
        var buf = new short[2048];
        var n = snd.RenderSamples(buf, BudgetCycles);
        return buf.AsSpan(0, n).ToArray();
    }

    private static short[] RenderTone(ushort reg, byte value, byte volume)
    {
        var snd = new Vic20Sound();
        snd.Init(SampleRate, PalCycles);
        snd.Store(0xE, volume);
        snd.Store(reg, value);
        var buf = new short[2048];
        var n = snd.RenderSamples(buf, BudgetCycles);
        return buf.AsSpan(0, n).ToArray();
    }
}
