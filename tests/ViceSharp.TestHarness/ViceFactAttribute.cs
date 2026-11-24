using System.Runtime.CompilerServices;
using Xunit;

namespace ViceSharp.TestHarness;

/// <summary>
/// A fact bound to the native VICE binding. PLAN-NATIVERESTORE-001 D2 (round 5 N16, round 7 N24): construction no
/// longer probes the binding; the skip is conditional on <see cref="ViceNativeAvailability.IsAvailable"/>, which xunit
/// evaluates at run time for selected tests only, so discovery with the ShimLegacy category excluded performs no
/// native resolve or load.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class ViceFactAttribute : FactAttribute
{
    public ViceFactAttribute(
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
        : base(sourceFilePath, sourceLineNumber)
    {
        Skip = ViceNativeAvailability.SkipReason;
        SkipUnless = nameof(ViceNativeAvailability.IsAvailable);
        SkipType = typeof(ViceNativeAvailability);
    }
}
