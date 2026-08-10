namespace ViceSharp.TestHarness.Xbox;

using ViceSharp.Xbox.ViewModels;
using Xunit;

/// <summary>
/// PLAN-XBOXUWP slice S30 (IMPL-XBOXUWP-030). TEST-XBOXUI-008: the About page
/// ViewModel (<see cref="AboutViewModel"/>) and its single-home legal-disclosure
/// constants source (<see cref="AboutInfo"/>) in <c>ViceSharp.Xbox.ViewModels</c>.
/// </summary>
/// <remarks>
/// The About page carries the GPL-2.0-or-later license identity, the VICE attribution
/// (ViceSharp is a port of the VICE project, itself GPL-2.0-or-later), and the written
/// source offer with a source-repository URL. All of the fixed legal text lives in ONE
/// constants location (<see cref="AboutInfo"/>) so it has a single home; the ViewModel
/// only surfaces those constants (plus a runtime <see cref="AboutViewModel.Version"/>).
/// Pure MVVM (TR-MVVM-001): no engine, host, or XAML reference.
/// </remarks>
[Trait("Category", "Xbox")]
public sealed class AboutViewModelTests
{
    /// <summary>
    /// TEST-XBOXUI-008 (IMPL-XBOXUWP-030) license-identity guard.
    /// Use case: the About page must disclose the exact SPDX license identifier of the
    /// derivative work so the on-console GPL compliance surface is unambiguous.
    /// Acceptance: <see cref="AboutInfo.LicenseIdentifier"/> equals the SPDX string
    /// "GPL-2.0-or-later", and <see cref="AboutViewModel.LicenseIdentifier"/> surfaces
    /// that same constant.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void LicenseIdentifier_IsGplSpdxString_AndSurfacedFromConstants()
    {
        Assert.Equal("GPL-2.0-or-later", AboutInfo.LicenseIdentifier);

        var vm = new AboutViewModel();
        Assert.Equal(AboutInfo.LicenseIdentifier, vm.LicenseIdentifier);
    }

    /// <summary>
    /// TEST-XBOXUI-008 (IMPL-XBOXUWP-030) VICE-attribution guard.
    /// Use case: as a derivative work, ViceSharp must attribute the VICE project it is a
    /// port of on the About page.
    /// Acceptance: <see cref="AboutInfo.AttributionText"/> is non-empty and mentions
    /// "VICE", and <see cref="AboutViewModel.AttributionText"/> surfaces that same constant.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void AttributionText_MentionsVice_AndSurfacedFromConstants()
    {
        Assert.False(string.IsNullOrWhiteSpace(AboutInfo.AttributionText));
        Assert.Contains("VICE", AboutInfo.AttributionText);

        var vm = new AboutViewModel();
        Assert.Equal(AboutInfo.AttributionText, vm.AttributionText);
    }

    /// <summary>
    /// TEST-XBOXUI-008 (IMPL-XBOXUWP-030) source-offer guard.
    /// Use case: GPL-2.0-or-later requires that the corresponding source be offered; the
    /// About page carries that written offer with a reachable source-repository URL.
    /// Acceptance: <see cref="AboutInfo.SourceOfferText"/> is non-empty and contains a URL
    /// ("http"), the URL is <see cref="AboutInfo.SourceUrl"/>, and
    /// <see cref="AboutViewModel.SourceOfferText"/> surfaces that same constant.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void SourceOfferText_IsNonEmpty_ContainsUrl_AndSurfacedFromConstants()
    {
        Assert.False(string.IsNullOrWhiteSpace(AboutInfo.SourceOfferText));
        Assert.Contains("http", AboutInfo.SourceOfferText);
        Assert.Contains(AboutInfo.SourceUrl, AboutInfo.SourceOfferText);
        Assert.Contains("http", AboutInfo.SourceUrl);

        var vm = new AboutViewModel();
        Assert.Equal(AboutInfo.SourceOfferText, vm.SourceOfferText);
        Assert.Equal(AboutInfo.SourceUrl, vm.SourceUrl);
    }

    /// <summary>
    /// TEST-XBOXUI-008 (IMPL-XBOXUWP-030) version-surface guard.
    /// Use case: the About page shows the running build version alongside the legal text.
    /// Acceptance: <see cref="AboutViewModel.Version"/> is a non-empty string and is never
    /// the unstamped SDK default <c>1.0.0.0</c> (or bare <c>1.0.0</c>).
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void Version_IsNonEmpty_AndNotSdkDefault()
    {
        var vm = new AboutViewModel();
        Assert.False(string.IsNullOrWhiteSpace(vm.Version));
        Assert.NotEqual("1.0.0.0", vm.Version, StringComparer.Ordinal);
        Assert.NotEqual("1.0.0", vm.Version, StringComparer.Ordinal);
        Assert.NotEqual("0.0.0.0", vm.Version, StringComparer.Ordinal);
        Assert.NotEqual("0.0.0", vm.Version, StringComparer.Ordinal);
    }

    /// <summary>
    /// Prefer AssemblyInformationalVersion (GitVersion / MSBuild Version stamp) over
    /// AssemblyVersion, strip build-metadata (+Branch.Sha...), and ignore unstamped
    /// SDK defaults so About never shows 1.0.0.0 when product version is known.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void ResolveDisplayVersion_PrefersInformational_StripsMetadata_IgnoresSdkDefault()
    {
        Assert.Equal(
            "1.2.7",
            AboutViewModel.ResolveDisplayVersion(
                "1.2.7+Branch.main.Sha.deadbeef",
                "1.0.0.0"));

        Assert.Equal(
            "1.2.2",
            AboutViewModel.ResolveDisplayVersion(
                informationalVersion: null,
                assemblyVersion: "1.2.2.0"));

        Assert.Equal(
            AboutInfo.Version,
            AboutViewModel.ResolveDisplayVersion(
                informationalVersion: null,
                assemblyVersion: "1.0.0.0"));

        Assert.Equal(
            AboutInfo.Version,
            AboutViewModel.ResolveDisplayVersion(
                informationalVersion: "1.0.0",
                assemblyVersion: "1.0.0.0"));
    }

    /// <summary>
    /// Product fallback must track the GitVersion next-version base (not the SDK default).
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void AboutInfo_Version_IsProductBase_NotSdkDefault()
    {
        Assert.False(string.IsNullOrWhiteSpace(AboutInfo.Version));
        Assert.NotEqual("1.0.0", AboutInfo.Version, StringComparer.Ordinal);
        Assert.NotEqual("1.0.0.0", AboutInfo.Version, StringComparer.Ordinal);
        Assert.StartsWith("1.2.", AboutInfo.Version, StringComparison.Ordinal);
    }

    /// <summary>
    /// CC BY-SA 4.0 Commodore C= logo attribution guard.
    /// Use case: product branding that uses the Wikimedia C= logo must disclose
    /// author, license, and source URL on the About surface.
    /// Acceptance: <see cref="AboutInfo.LogoAttributionText"/> mentions CC BY-SA,
    /// Alien426, and the commons URL; ViewModel surfaces the same constant.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void LogoAttributionText_MentionsCcBySa_AndSurfacedFromConstants()
    {
        Assert.False(string.IsNullOrWhiteSpace(AboutInfo.LogoAttributionText));
        Assert.Contains("CC BY-SA 4.0", AboutInfo.LogoAttributionText, StringComparison.Ordinal);
        Assert.Contains("Alien426", AboutInfo.LogoAttributionText, StringComparison.Ordinal);
        Assert.Contains("commons.wikimedia.org", AboutInfo.LogoAttributionText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(AboutInfo.LogoSourceUrl, AboutInfo.LogoAttributionText, StringComparison.Ordinal);
        Assert.Contains(AboutInfo.LogoLicenseUrl, AboutInfo.LogoAttributionText, StringComparison.Ordinal);

        var vm = new AboutViewModel();
        Assert.Equal(AboutInfo.LogoAttributionText, vm.LogoAttributionText);
    }
}
