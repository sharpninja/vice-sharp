namespace ViceSharp.Build.Testing;

/// <summary>
/// TR-BUILD-001 ac-12: host selection for the <c>BuildTest</c> target. Linux-host build tests carry the
/// trait <c>Host=Linux</c>; on Linux no host filter is applied, on every other host they are excluded by
/// filter so the run reports them as not selected instead of skipped.
/// </summary>
internal static class BuildTestFilter
{
    /// <summary>The trait filter that excludes Linux-host cases.</summary>
    internal const string ExcludeLinuxHost = "Host!=Linux";

    /// <summary>Returns the host filter for a run, or null when no host filter applies.</summary>
    internal static string? For(bool isLinux) => isLinux ? null : ExcludeLinuxHost;
}
