using System.Reflection;
using ViceSharp.Chips.Audio;
using Xunit;

namespace ViceSharp.TestHarness;

/// <summary>
/// PLAN-SIDCHAMBERLIN-001: absence locks for the retired legacy Chamberlin SVF
/// filter stack. Since PLAN-VICEPARITY-001 S11 the reSID op-amp filter is the
/// only audio-path filter for both the 6581 and the 8580 (UsesReSidFilter was
/// constant-true with no overrides), leaving the Chamberlin members dead in the
/// audio path. These reflection locks assert the members are GONE and stay gone
/// (pattern: the Sid8580D absence lock, TEST-SID-FILTER-8580-14).
/// </summary>
public sealed class SidChamberlinRetirementTests
{
    private const BindingFlags AllMembers =
        BindingFlags.Public | BindingFlags.NonPublic |
        BindingFlags.Instance | BindingFlags.Static |
        BindingFlags.FlattenHierarchy;

    private static void AssertNoMember(string name)
    {
        var t = typeof(Sid6581);
        Assert.True(t.GetMethod(name, AllMembers) is null, $"method {name} must be deleted");
        Assert.True(t.GetProperty(name, AllMembers) is null, $"property {name} must be deleted");
        Assert.True(t.GetField(name, AllMembers) is null, $"field {name} must be deleted");
        // Sid8580 must not resurrect it either.
        var t8 = typeof(Sid8580);
        Assert.True(
            t8.GetMember(name, AllMembers | BindingFlags.DeclaredOnly).Length == 0,
            $"Sid8580 must not declare {name}");
    }

    /// <summary>
    /// FR: FR-SID-FILTER-6581 (Chamberlin cleanup, PLAN-SIDCHAMBERLIN-001).
    /// Use case: the legacy Chamberlin SVF filter core (ApplyFilter, the _svf
    /// integrator fields, the FilterSaturation soft-clip, and the unity
    /// ClockExternalFilter) was dead in the audio path once the reSID filter
    /// became unconditional; leaving it invited drift and false test coverage.
    /// Acceptance: typeof(Sid6581) exposes NO member named ApplyFilter,
    /// ClockExternalFilter, _svfLowPass, _svfBandPass, or FilterSaturation
    /// (any member kind, any visibility), and Sid8580 declares none of them.
    /// </summary>
    [Fact]
    public void ChamberlinFilterCore_Removed()
    {
        AssertNoMember("ApplyFilter");
        AssertNoMember("ClockExternalFilter");
        AssertNoMember("_svfLowPass");
        AssertNoMember("_svfBandPass");
        AssertNoMember("FilterSaturation");
    }

    /// <summary>
    /// FR: FR-SID-CUTOFFDAC (Chamberlin cleanup, PLAN-SIDCHAMBERLIN-001).
    /// Use case: the hand-authored cutoff-register-to-Hz curves fed only the
    /// Chamberlin SVF; the reSID model maps cutoff through f0_dac tables, so
    /// the Hz curves (including the 8580 variant with zero callers) are dead.
    /// Acceptance: typeof(Sid6581) exposes NO member named
    /// MapCutoffRegToFrequency or MapCutoffRegToFrequency8580.
    /// </summary>
    [Fact]
    public void ChamberlinCutoffCurves_Removed()
    {
        AssertNoMember("MapCutoffRegToFrequency");
        AssertNoMember("MapCutoffRegToFrequency8580");
    }

    /// <summary>
    /// FR: FR-SID-OUTPUT (Chamberlin cleanup, PLAN-SIDCHAMBERLIN-001).
    /// Use case: the non-reSID GenerateSample branch (volume-fraction mix over
    /// VoiceOutputScale plus the DigiDcOffset approximation) and the
    /// UsesReSidFilter dispatch guard were unreachable once the reSID path
    /// became unconditional; the reSID amplify/clip path (S12) is the only
    /// emission contract.
    /// Acceptance: typeof(Sid6581) exposes NO member named UsesReSidFilter,
    /// DigiDcOffset, or VoiceOutputScale.
    /// </summary>
    [Fact]
    public void LegacyGenerateSampleBranch_Removed()
    {
        AssertNoMember("UsesReSidFilter");
        AssertNoMember("DigiDcOffset");
        AssertNoMember("VoiceOutputScale");
    }
}
