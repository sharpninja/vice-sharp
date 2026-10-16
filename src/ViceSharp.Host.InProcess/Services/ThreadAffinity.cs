using System.Runtime.InteropServices;

namespace ViceSharp.Host.Services;

/// <summary>
/// Opt-in CPU pinning for host threads (TR-HOST-AFFINITY-001). The emulation
/// worker reads <c>VICESHARP_EMU_CPU</c> and the Avalonia UI thread reads
/// <c>VICESHARP_UI_CPU</c>; each holds a single logical CPU index (0-based).
/// Unset or invalid values mean "no pin", preserving today's scheduler-managed
/// behavior. Windows-only; on other platforms pinning is a no-op.
/// </summary>
public static partial class ThreadAffinity
{
    /// <summary>
    /// Parse a single logical CPU index ("13") into an affinity mask
    /// (bit 13). Returns null for unset, blank, non-numeric, negative, or
    /// out-of-range (&gt;63) values so callers simply skip pinning.
    /// </summary>
    public static ulong? ParseCpuIndex(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (!int.TryParse(value.Trim(), out var index))
            return null;

        if (index is < 0 or > 63)
            return null;

        return 1ul << index;
    }

    /// <summary>
    /// Pin the calling thread to <paramref name="mask"/>. Returns true when
    /// the OS accepted the mask; <paramref name="previous"/> then holds the
    /// thread's prior affinity mask (useful for tests and undo). Always false
    /// off Windows or when the mask is rejected (e.g. no overlap with the
    /// process affinity), in which case the thread is left untouched.
    /// </summary>
    public static bool TryPinCurrentThread(ulong mask, out ulong previous)
    {
        previous = 0;
        if (!OperatingSystem.IsWindows() || mask == 0)
            return false;

        var result = SetThreadAffinityMask(GetCurrentThread(), (nuint)mask);
        if (result == 0)
            return false;

        previous = result;
        return true;
    }

    /// <summary>
    /// Read <paramref name="environmentVariable"/> and pin the calling thread
    /// to the CPU it names. Returns the applied mask, or null when the
    /// variable is unset/invalid or the pin failed.
    /// </summary>
    public static ulong? TryPinCurrentThreadFromEnvironment(string environmentVariable)
    {
        var mask = ParseCpuIndex(Environment.GetEnvironmentVariable(environmentVariable));
        if (mask is null)
            return null;

        return TryPinCurrentThread(mask.Value, out _) ? mask : null;
    }

    [LibraryImport("kernel32.dll")]
    private static partial IntPtr GetCurrentThread();

    [LibraryImport("kernel32.dll")]
    private static partial nuint SetThreadAffinityMask(IntPtr thread, nuint mask);
}
