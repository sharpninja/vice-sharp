using ViceSharp.Core;

namespace ViceSharp.TestHarness;

/// <summary>
/// PLAN-NATIVERESTORE-001 D2 (round 5 N16, round 7 N24): the run-time skip gate of <see cref="ViceFactAttribute"/>
/// and <see cref="ViceTheoryAttribute"/>. The attributes no longer query the native binding while they are
/// constructed (test discovery); xunit evaluates <see cref="IsAvailable"/> only when a selected test is about to run,
/// so an ordinary gate that excludes the ShimLegacy category never resolves or loads a native library.
/// </summary>
public static class ViceNativeAvailability
{
    /// <summary>The skip reason shown when the gate evaluates false at run time.</summary>
    public const string SkipReason =
        "Native VICE library unavailable: the shim builders are retired (PLAN-NATIVERESTORE-001 R2 A); this ShimLegacy test waits for its Phase E or Phase F rebind.";

    /// <summary>Evaluated by xunit at run time for a selected test, never at discovery.</summary>
    public static bool IsAvailable => ViceNative.IsAvailable;
}
