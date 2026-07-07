namespace ViceSharp.TestHarness;

using Xunit;
using FluentAssertions;
using ViceSharp.Chips.Audio;

/// <summary>
/// Tests for the 6581 SID filter's non-linear cutoff curve mapping.
/// FR-SID-004 ac.6 (BACKFILL-SID-001): "The 6581 filter's non-linear cutoff
/// frequency mapping (the 'kinked' curve) is modeled."
///
/// The 6581 SID filter does not map its 11-bit cutoff register linearly to a
/// continuous cutoff frequency. Real-chip measurements show a 3-region shape:
///  - 0..0x200: flat low region (cutoff barely changes)
///  - 0x200..0x600: steep middle (cutoff rises rapidly)
///  - 0x600..0x7FF: flatter high region
///
/// These tests verify the curve shape produced by
/// <see cref="Sid6581.MapCutoffRegToFrequency"/> matches that empirical pattern.
/// </summary>
public sealed class Sid6581NonLinearCutoffTests
{
    /// <summary>
    /// FR/TR: FR-SID-004 ac.6 (BACKFILL-SID-001 non-linear cutoff curve).
    /// Use case: Verify the low region of the cutoff curve is flat.
    /// Acceptance: Cutoff frequency between reg=0x00 and reg=0x100 differs by less
    /// than 10% of the full audible range, proving the curve barely moves there.
    /// </summary>
    [Fact(Skip = "Retired in S9: SVF cutoff curve replaced by reSID 6581 f0_dac (PLAN-VICEPARITY-001 S9)")]
    public void CutoffCurve_LowRegion_IsFlat()
    {
        float fAt0x00 = Sid6581.MapCutoffRegToFrequency(0x00);
        float fAt0x100 = Sid6581.MapCutoffRegToFrequency(0x100);

        // Both should sit in the low-frequency region (~200-300Hz).
        fAt0x00.Should().BeGreaterThan(0f);
        fAt0x100.Should().BeGreaterThan(fAt0x00);

        // Difference should be small: < 10% of full audible range (15000Hz ceiling).
        float delta = fAt0x100 - fAt0x00;
        delta.Should().BeLessThan(1500f, "low region must be flat per 6581 curve");
    }

    /// <summary>
    /// FR/TR: FR-SID-004 ac.6 (BACKFILL-SID-001 non-linear cutoff curve).
    /// Use case: Verify the middle region of the cutoff curve is steep.
    /// Acceptance: Cutoff difference between reg=0x200 and reg=0x600 exceeds 50%
    /// of the full audible range, proving the steep rise in this region.
    /// </summary>
    [Fact(Skip = "Retired in S9: SVF cutoff curve replaced by reSID 6581 f0_dac (PLAN-VICEPARITY-001 S9)")]
    public void CutoffCurve_MiddleRegion_IsSteep()
    {
        float fAt0x200 = Sid6581.MapCutoffRegToFrequency(0x200);
        float fAt0x600 = Sid6581.MapCutoffRegToFrequency(0x600);

        fAt0x600.Should().BeGreaterThan(fAt0x200);

        // Steep rise: delta should be > 50% of full audible range (15000Hz ceiling).
        float delta = fAt0x600 - fAt0x200;
        delta.Should().BeGreaterThan(7500f, "middle region must be steep per 6581 curve");
    }

    /// <summary>
    /// FR/TR: FR-SID-004 ac.6 (BACKFILL-SID-001 non-linear cutoff curve).
    /// Use case: Verify the high region of the cutoff curve is flatter than the
    /// middle region.
    /// Acceptance: Slope (Hz per register step) in the high region is less than
    /// the slope in the middle region, proving the curve flattens at the top.
    /// </summary>
    [Fact(Skip = "Retired in S9: SVF cutoff curve replaced by reSID 6581 f0_dac (PLAN-VICEPARITY-001 S9)")]
    public void CutoffCurve_HighRegion_IsFlatterThanMiddle()
    {
        float fAt0x200 = Sid6581.MapCutoffRegToFrequency(0x200);
        float fAt0x600 = Sid6581.MapCutoffRegToFrequency(0x600);
        float fAt0x7FF = Sid6581.MapCutoffRegToFrequency(0x7FF);

        // Middle slope: Hz per register step across 0x200..0x600 (1024 steps).
        float middleSlope = (fAt0x600 - fAt0x200) / (0x600 - 0x200);

        // High slope: Hz per register step across 0x600..0x7FF (511 steps).
        float highSlope = (fAt0x7FF - fAt0x600) / (0x7FF - 0x600);

        highSlope.Should().BeGreaterThan(0f, "cutoff must still increase in high region");
        highSlope.Should().BeLessThan(middleSlope, "high region must be flatter than middle per 6581 curve");
    }

    /// <summary>
    /// FR/TR: FR-SID-004 ac.6 (BACKFILL-SID-001 non-linear cutoff curve).
    /// Use case: Verify monotonicity over the entire 11-bit register range.
    /// Acceptance: Cutoff frequency is non-decreasing for every register step
    /// from 0x000 through 0x7FF.
    /// </summary>
    [Fact(Skip = "Retired in S9: SVF cutoff curve replaced by reSID 6581 f0_dac (PLAN-VICEPARITY-001 S9)")]
    public void CutoffCurve_IsMonotonicNonDecreasing()
    {
        float prev = Sid6581.MapCutoffRegToFrequency(0);
        for (int reg = 1; reg <= 0x7FF; reg++)
        {
            float current = Sid6581.MapCutoffRegToFrequency(reg);
            current.Should().BeGreaterThanOrEqualTo(prev, $"cutoff must not decrease at reg=0x{reg:X3}");
            prev = current;
        }
    }
}
