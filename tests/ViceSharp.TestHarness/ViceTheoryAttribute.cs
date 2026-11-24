using System.Runtime.CompilerServices;
using Xunit;

namespace ViceSharp.TestHarness;

/// <summary>
/// A theory bound to the native VICE binding. PLAN-NATIVERESTORE-001 D2 (round 5 N16, round 7 N24): construction no
/// longer probes the binding; the skip is conditional on <see cref="ViceNativeAvailability.IsAvailable"/>, evaluated
/// by xunit at run time for selected tests only. Discovery enumeration of the data stays disabled.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class ViceTheoryAttribute : TheoryAttribute
{
    public ViceTheoryAttribute(
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
        : base(sourceFilePath, sourceLineNumber)
    {
        Skip = ViceNativeAvailability.SkipReason;
        SkipUnless = nameof(ViceNativeAvailability.IsAvailable);
        SkipType = typeof(ViceNativeAvailability);
        DisableDiscoveryEnumeration = true;
    }
}
