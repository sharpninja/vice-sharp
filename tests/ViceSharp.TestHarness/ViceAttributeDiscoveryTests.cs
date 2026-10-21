using ViceSharp.Core;
using Xunit;

namespace ViceSharp.TestHarness;

/// <summary>
/// PLAN-NATIVERESTORE-001 D2 (round 5 N16, round 7 N24); TR-VICE-ORACLE-001 AC2: constructing
/// <see cref="ViceFactAttribute"/> and <see cref="ViceTheoryAttribute"/>, which xunit does for every decorated
/// method during discovery whether or not the method is selected, records no resolve or load attempt; the skip is
/// declared as a run-time gate on <see cref="ViceNativeAvailability"/> instead.
/// </summary>
[Collection("NativeLoadAudit")]
public sealed class ViceAttributeDiscoveryTests
{
    private sealed class CountingSink : NativeLoadAudit.ISink
    {
        public int Calls;
        public void Record(string library, string? path, string outcome) => Calls++;
    }

    /// <summary>
    /// TR-VICE-ORACLE-001 AC2; PLAN-NATIVERESTORE-001 D2 round 5 N16 and round 7 N24.
    /// Use case: an ordinary gate that excludes the ShimLegacy category still constructs every ViceFact and
    /// ViceTheory attribute at discovery, so construction must not touch the native binding.
    /// Acceptance: with a counting double installed, constructing both attributes records zero attempts, and each
    /// attribute declares the run-time gate (Skip reason, SkipUnless IsAvailable, SkipType ViceNativeAvailability)
    /// with discovery enumeration disabled for the theory.
    /// </summary>
    [Fact]
    public void AttributeConstruction_RecordsNoNativeAttempt_AndDeclaresARunTimeGate()
    {
        var counting = new CountingSink();
        var previous = NativeLoadAudit.Replace(counting);
        try
        {
            var fact = new ViceFactAttribute();
            var theory = new ViceTheoryAttribute();
            Assert.Equal(0, counting.Calls);
            Assert.Equal(ViceNativeAvailability.SkipReason, fact.Skip);
            Assert.Equal(nameof(ViceNativeAvailability.IsAvailable), fact.SkipUnless);
            Assert.Equal(typeof(ViceNativeAvailability), fact.SkipType);
            Assert.Equal(ViceNativeAvailability.SkipReason, theory.Skip);
            Assert.Equal(nameof(ViceNativeAvailability.IsAvailable), theory.SkipUnless);
            Assert.Equal(typeof(ViceNativeAvailability), theory.SkipType);
            Assert.True(theory.DisableDiscoveryEnumeration);
        }
        finally
        {
            NativeLoadAudit.Replace(previous);
        }
    }
}
