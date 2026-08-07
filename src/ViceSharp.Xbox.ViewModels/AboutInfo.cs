namespace ViceSharp.Xbox.ViewModels;

/// <summary>
/// PLAN-XBOXUWP S30 (IMPL-XBOXUWP-030), area XBOXUI. The SINGLE home for the fixed
/// legal-disclosure text shown on the About page: the SPDX license identifier, the
/// VICE attribution, the Commodore C= logo attribution (CC BY-SA 4.0), the written
/// source offer, and the source-repository URL.
/// </summary>
/// <remarks>
/// <para>
/// ViceSharp is a derivative work of VICE (the Versatile Commodore Emulator) and ships
/// under GPL-2.0-or-later, so the on-console app must disclose the license, attribute
/// the upstream project, and offer the corresponding source. Keeping every one of those
/// strings here (rather than scattered across ViewModels or XAML) gives the legal text
/// one authoritative home that the <see cref="AboutViewModel"/> merely surfaces.
/// </para>
/// <para>
/// <b>Source URL.</b> <see cref="SourceUrl"/> is the public GitHub mirror
/// (<c>https://github.com/sharpninja/vice-sharp</c>), which is the source URL used
/// across the project's packaging (Chocolatey / Scoop / winget build metadata). Azure
/// DevOps (<c>https://dev.azure.com/McpServer/VICE-Sharp</c>) is the primary of record,
/// but the GitHub mirror is the publicly reachable source-offer target. This is a
/// constant and can be updated if the canonical public source URL moves.
/// </para>
/// <para>
/// Pure MVVM (TR-MVVM-001): plain constants, no engine, host, or XAML reference.
/// </para>
/// </remarks>
public static class AboutInfo
{
    /// <summary>The product / application display name.</summary>
    public const string ProjectName = "ViceSharp";

    /// <summary>
    /// The SPDX license identifier of the derivative work: the exact string
    /// <c>GPL-2.0-or-later</c> (matching <c>Directory.Build.props</c>
    /// <c>PackageLicenseExpression</c>).
    /// </summary>
    public const string LicenseIdentifier = "GPL-2.0-or-later";

    /// <summary>
    /// The publicly reachable source-repository URL used for the GPL source offer. This
    /// is the GitHub mirror; see the type remarks for the source-URL rationale.
    /// </summary>
    public const string SourceUrl = "https://github.com/sharpninja/vice-sharp";

    /// <summary>
    /// The VICE attribution: ViceSharp is a clean-room C# port of the VICE project, which
    /// is itself licensed GPL-2.0-or-later. Mentions "VICE" so the About page attributes
    /// the upstream emulator this work derives from.
    /// </summary>
    public const string AttributionText =
        "ViceSharp is a derivative work of VICE (the Versatile Commodore Emulator), " +
        "a clean-room C# port informed by VICE's architecture and behavior. VICE is " +
        "developed by the VICE Team and is licensed under GPL-2.0-or-later " +
        "(https://vice-emu.sourceforge.io/).";

    /// <summary>
    /// The GPL written source offer: states that the complete corresponding source for
    /// ViceSharp is available under GPL-2.0-or-later, and includes the
    /// <see cref="SourceUrl"/> where it can be obtained.
    /// </summary>
    public const string SourceOfferText =
        "Complete corresponding source code for ViceSharp is available under " +
        "GPL-2.0-or-later at " + SourceUrl + ".";

    /// <summary>
    /// Wikimedia Commons page for the official Commodore C= logo SVG used in product branding.
    /// </summary>
    public const string LogoSourceUrl =
        "https://commons.wikimedia.org/wiki/File:Commodore_C%3D_logo.svg";

    /// <summary>
    /// License deed for the Commodore C= logo (CC BY-SA 4.0).
    /// </summary>
    public const string LogoLicenseUrl = "https://creativecommons.org/licenses/by-sa/4.0/";

    /// <summary>
    /// Required CC BY-SA 4.0 attribution for the Commodore C= logo (Wikimedia Commons,
    /// author Alien426). Shown on About screens whenever branding uses that mark.
    /// </summary>
    public const string LogoAttributionText =
        "Commodore C= logo: Wikimedia Commons file \"Commodore C= logo.svg\" by Alien426, " +
        "licensed under Creative Commons Attribution-ShareAlike 4.0 International (CC BY-SA 4.0). " +
        "Source: " + LogoSourceUrl + "  License: " + LogoLicenseUrl;

    /// <summary>
    /// The fallback version string used when the running assembly does not carry a
    /// resolvable version (see <see cref="AboutViewModel.Version"/>).
    /// </summary>
    public const string Version = "1.0.0";
}
