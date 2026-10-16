namespace ViceSharp.Xbox.ViewModels;

using System.Reflection;

/// <summary>
/// PLAN-XBOXUWP S30 (IMPL-XBOXUWP-030), area XBOXUI, TEST-XBOXUI-008. The About page
/// ViewModel: a read-only surface over the fixed legal disclosure in
/// <see cref="AboutInfo"/> (license identity, VICE attribution, C= logo attribution,
/// source offer, source URL) plus the running build <see cref="Version"/>.
/// </summary>
/// <remarks>
/// <para>
/// ViceSharp is GPL-2.0-or-later (a derivative of VICE), so the on-console About page
/// must disclose the license, attribute VICE, and offer the corresponding source. All
/// of that text has ONE home (<see cref="AboutInfo"/>); this ViewModel only surfaces it
/// so the page bindings never fork the legal wording.
/// </para>
/// <para>
/// <see cref="Version"/> is resolved once at construction from this assembly's
/// <see cref="AssemblyInformationalVersionAttribute"/> (preferred; stamped by
/// MSBuild <c>Version</c> / GitVersion on publish and DeployXboxLocal), then
/// <see cref="AssemblyName.Version"/>, ignoring the unstamped SDK defaults
/// (<c>1.0.0</c> / <c>1.0.0.0</c>), then <see cref="AboutInfo.Version"/>.
/// Pure MVVM (TR-MVVM-001): no engine, host, or XAML reference.
/// </para>
/// </remarks>
public sealed class AboutViewModel
{
    /// <summary>Creates the About ViewModel, resolving the display <see cref="Version"/>.</summary>
    public AboutViewModel()
    {
        var assembly = typeof(AboutViewModel).Assembly;
        var informational = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;
        var assemblyVersion = assembly.GetName().Version?.ToString();
        Version = ResolveDisplayVersion(informational, assemblyVersion);
    }

    /// <summary>The product / application display name (<see cref="AboutInfo.ProjectName"/>).</summary>
    public string ProjectName => AboutInfo.ProjectName;

    /// <summary>
    /// The SPDX license identifier of the derivative work
    /// (<see cref="AboutInfo.LicenseIdentifier"/>): <c>GPL-2.0-or-later</c>.
    /// </summary>
    public string LicenseIdentifier => AboutInfo.LicenseIdentifier;

    /// <summary>The VICE attribution text (<see cref="AboutInfo.AttributionText"/>).</summary>
    public string AttributionText => AboutInfo.AttributionText;

    /// <summary>The GPL written source-offer text (<see cref="AboutInfo.SourceOfferText"/>).</summary>
    public string SourceOfferText => AboutInfo.SourceOfferText;

    /// <summary>The public source-repository URL (<see cref="AboutInfo.SourceUrl"/>).</summary>
    public string SourceUrl => AboutInfo.SourceUrl;

    /// <summary>
    /// CC BY-SA 4.0 attribution for the Commodore C= logo
    /// (<see cref="AboutInfo.LogoAttributionText"/>).
    /// </summary>
    public string LogoAttributionText => AboutInfo.LogoAttributionText;

    /// <summary>
    /// The running build version, resolved from this assembly at construction with a
    /// fallback to <see cref="AboutInfo.Version"/>. Never null or empty.
    /// </summary>
    public string Version { get; }

    /// <summary>
    /// Resolves the About display version: prefer informational (strip
    /// <c>+build-metadata</c>), then assembly version, skip unstamped SDK defaults,
    /// then <see cref="AboutInfo.Version"/>.
    /// </summary>
    /// <param name="informationalVersion">Raw <see cref="AssemblyInformationalVersionAttribute"/> value, or null.</param>
    /// <param name="assemblyVersion">Raw <see cref="AssemblyName.Version"/> string, or null.</param>
    /// <returns>A non-empty display version string.</returns>
    public static string ResolveDisplayVersion(string? informationalVersion, string? assemblyVersion)
    {
        foreach (var raw in new[] { informationalVersion, assemblyVersion })
        {
            if (string.IsNullOrWhiteSpace(raw))
                continue;

            var display = StripBuildMetadata(raw);
            if (IsUnstampedSdkDefault(display))
                continue;

            // AssemblyVersion is often four-part (1.2.7.0); show three-part product form.
            if (System.Version.TryParse(display, out var parsed) && parsed.Build >= 0)
            {
                // Revision -1 means three-part input; 0 means explicit .0 fourth part.
                if (parsed.Revision is 0 or -1)
                    return $"{parsed.Major}.{parsed.Minor}.{parsed.Build}";
            }

            return display;
        }

        return AboutInfo.Version;
    }

    private static string StripBuildMetadata(string rawVersion)
    {
        var plus = rawVersion.IndexOf('+');
        return plus >= 0 ? rawVersion[..plus] : rawVersion;
    }

    private static bool IsUnstampedSdkDefault(string version) =>
        version is "0.0.0" or "0.0.0.0" or "1.0.0" or "1.0.0.0";
}
