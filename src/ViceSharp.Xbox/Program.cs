// PLAN-XBOXUWP S4 (IMPL-XBOXUWP-004): trivial fallback entry point.
//
// This Main exists ONLY so the workload-free net10.0 fallback WinExe compiles
// on agents that lack the windows-app / UWP workload. On the real UWP target
// (built with /p:ViceSharpXboxUwp=true) the csproj defines the HAS_UWP
// constant, which #if's this whole file out so it cannot collide with the UWP
// XAML App entry point that slice S34 adds. Keep this body trivial: the real
// application composition belongs to the UWP head, not here.
#if !HAS_UWP
namespace ViceSharp.Xbox;

internal static class Program
{
    private static int Main() => 0;
}
#endif
