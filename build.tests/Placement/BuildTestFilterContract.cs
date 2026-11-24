namespace ViceSharp.Build.Tests.Placement;

/// <summary>
/// TR-BUILD-001 ac-12: the host-selection contract of the <c>BuildTest</c> filter. On Linux no host
/// filter is applied, so the Linux-host cases run; on any other host the filter is exactly
/// <c>Host!=Linux</c>, so those cases are excluded by selection and never reported as skipped.
/// </summary>
internal static class BuildTestFilterContract
{
    /// <summary>The trait filter that excludes Linux-host cases.</summary>
    public const string ExcludeLinuxHost = "Host!=Linux";

    /// <summary>Returns every contract violation of a filter function; empty means the function conforms.</summary>
    public static IReadOnlyList<string> Violations(Func<bool, string?> filterForHost)
    {
        var violations = new List<string>();
        string? linux = filterForHost(true);
        string? other = filterForHost(false);
        if (!string.IsNullOrEmpty(linux))
            violations.Add($"Linux host must apply no host filter, got '{linux}'.");
        if (!string.Equals(other, ExcludeLinuxHost, StringComparison.Ordinal))
            violations.Add($"Non-Linux host must apply exactly '{ExcludeLinuxHost}', got '{other ?? "<null>"}'.");
        return violations;
    }
}
