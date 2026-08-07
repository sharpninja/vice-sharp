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
/// <see cref="Version"/> is resolved once from this assembly's version at construction,
/// falling back to <see cref="AboutInfo.Version"/> when the assembly carries no
/// resolvable version. Pure MVVM (TR-MVVM-001): no engine, host, or XAML reference.
/// </para>
/// </remarks>
public sealed class AboutViewModel
{
    /// <summary>Creates the About ViewModel, resolving the display <see cref="Version"/>.</summary>
    public AboutViewModel()
    {
        var assemblyVersion = typeof(AboutViewModel).Assembly.GetName().Version?.ToString();
        Version = string.IsNullOrWhiteSpace(assemblyVersion) ? AboutInfo.Version : assemblyVersion;
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
}
