namespace ViceSharp.TestHarness;

using FluentAssertions;
using ViceSharp.Chips.Audio;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR-SID-003 / FR-SID-004 (BACKFILL-SID-001 8580 filter deepening).
/// Use case: The 8580 SID die uses a near-linear cutoff curve and slightly
/// gentler resonance scaling than the 6581. Earlier revisions of Sid8580
/// implemented an ad-hoc cascaded single-pole filter that did not share
/// state with the parent Sid6581 SVF. The deepened model uses a real
/// Chamberlin SVF on the 8580-specific cutoff curve (linear ~30 Hz to
/// ~12.5 kHz across registers 0..2047) and an 8580-specific resonance
/// bias. These tests pin the differences down to numeric values so the
/// 6581 vs 8580 split cannot silently regress.
/// </summary>
public sealed class Sid8580FilterTests
{
    /// <summary>
    /// FR-SID-003 / FR-SID-004.
    /// Use case: The 8580 cutoff curve must be monotone in the register
    /// value and roughly linear (no kinks).
    /// Acceptance: MapCutoffRegToFrequency8580 is monotonically increasing
    /// from reg=0 to reg=0x7FF; the slope between adjacent quartiles is
    /// within a 2x band of the overall average (linear, not kinked).
    /// VICE: sid8580.c filter calibration tables.
    /// </summary>
    [Fact]
    public void MapCutoffRegToFrequency8580_IsMonotoneAndApproximatelyLinear()
    {
        var samples = new (int reg, float hz)[]
        {
            (0,    Sid6581.MapCutoffRegToFrequency8580(0)),
            (0x200, Sid6581.MapCutoffRegToFrequency8580(0x200)),
            (0x400, Sid6581.MapCutoffRegToFrequency8580(0x400)),
            (0x600, Sid6581.MapCutoffRegToFrequency8580(0x600)),
            (0x7FF, Sid6581.MapCutoffRegToFrequency8580(0x7FF)),
        };

        // Monotone.
        for (var i = 1; i < samples.Length; i++)
            samples[i].hz.Should().BeGreaterThan(samples[i - 1].hz,
                $"reg={samples[i].reg:X} cutoff must exceed previous step");

        // Endpoints match the documented 8580 range.
        samples[0].hz.Should().BeApproximately(30f, 1f, "reg=0 should hit ~30 Hz");
        samples[^1].hz.Should().BeApproximately(12500f, 5f, "reg=0x7FF should hit ~12.5 kHz");

        // Linearity: each quartile slope within a 0.5x..2x band of the mean.
        var totalRange = samples[^1].hz - samples[0].hz;
        var meanSlope = totalRange / (samples[^1].reg - samples[0].reg);
        for (var i = 1; i < samples.Length; i++)
        {
            var slope = (samples[i].hz - samples[i - 1].hz)
                      / (samples[i].reg - samples[i - 1].reg);
            (slope / meanSlope).Should().BeInRange(0.5f, 2.0f,
                $"8580 cutoff slope between reg={samples[i - 1].reg:X} and reg={samples[i].reg:X} should be near-linear");
        }
    }

    /// <summary>
    /// FR-SID-003 / FR-SID-004.
    /// Use case: At the same cutoff register, the 6581 and 8580 curves
    /// must produce different cutoff frequencies (different filter audio
    /// behaviour). Specifically the 6581 kinked curve sits well below
    /// the 8580 linear curve in the middle of the range.
    /// Acceptance: At register 0x100, MapCutoffRegToFrequency8580 returns
    /// a noticeably higher Hz than MapCutoffRegToFrequency (6581).
    /// </summary>
    [Fact]
    public void CutoffCurves_6581_vs_8580_Diverge()
    {
        var hz6581 = Sid6581.MapCutoffRegToFrequency(0x100);
        var hz8580 = Sid6581.MapCutoffRegToFrequency8580(0x100);

        hz8580.Should().BeGreaterThan(hz6581 + 100f,
            "8580 linear curve must sit above the 6581 kinked curve in the low/mid register range");
    }

    /// <summary>
    /// FR-SID-003 / FR-SID-004.
    /// Use case: Driving the same constant DC into the SID filter on a
    /// 6581 and 8580 at identical register settings must produce
    /// different output samples (proves the override is wired and not
    /// just inheriting the 6581 ApplyFilter unchanged).
    /// Acceptance: Sid6581 and Sid8580 each produce >= 100 output
    /// samples for a fixed test waveform; the resulting sample streams
    /// are not byte-equal.
    /// </summary>
    [Fact]
    public void FilterOutput_6581_vs_8580_AtSameRegisters_Differs()
    {
        // Identical register programming on both models: voice 1 to a
        // sawtooth at mid-frequency, filter cutoff = 0x400, resonance = 8,
        // LP enabled.
        static float[] Run(Sid6581 sid)
        {
            // Voice 1 frequency, sawtooth, sustain at full.
            sid.Write(0xD400, 0x00);
            sid.Write(0xD401, 0x20);
            sid.Write(0xD405, 0x0F);
            sid.Write(0xD406, 0xF0);
            // Filter: route voice 1 (bit 0 of $D417), resonance = 8.
            sid.Write(0xD415, 0x00);  // cutoff lo
            sid.Write(0xD416, 0x80);  // cutoff hi (~0x400)
            sid.Write(0xD417, 0x81);  // resonance = 8, route voice1 = 1
            sid.Write(0xD418, 0x1F);  // LP on, volume = 15
            // Gate sawtooth.
            sid.Write(0xD404, 0x21);

            // Warm-up: let attack ramp the envelope past 0 so the sample
            // stream is non-silent and the SVF state has converged.
            for (var w = 0; w < 20_000; w++) sid.Tick();

            var buf = new float[256];
            for (var i = 0; i < buf.Length; i++)
            {
                sid.Tick();
                buf[i] = sid.GenerateSample();
            }
            return buf;
        }

        var bus = new BasicBus();
        var sid6581 = new Sid6581(bus) { BaseAddress = 0xD400 };
        var sid8580 = new Sid8580(bus) { BaseAddress = 0xD400 };

        var trace6581 = Run(sid6581);
        var trace8580 = Run(sid8580);

        // O(n) ordered compare. FluentAssertions NotBeEquivalentTo runs an
        // unordered equivalency graph over the buffers (O(n^2) scopes plus
        // per-pair failure context); under full-suite single-process heap
        // pressure that allocation spiral produced an OutOfMemoryException.
        Assert.False(
            trace6581.AsSpan().SequenceEqual(trace8580),
            "Sid8580.ApplyFilter must produce a different sample sequence than Sid6581.ApplyFilter at the same register state");
    }
}
