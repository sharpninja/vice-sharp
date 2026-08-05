namespace ViceSharp.TestHarness.Xbox;

using System;
using System.IO;
using Xunit;

/// <summary>
/// FEAT-XC64FONT-001 (PLAN-XKEYBOARD-001 follow-up). Operator 2026-07-14: "Use open
/// source C64 font for keycaps." The virtual keyboard's keycaps render in PetMe64
/// (Kreative Korporation's pixel-true C64 charset font), vendored VERBATIM from the
/// VICE source tree (native/vice/vice/data/common) together with its license, exactly
/// like the GPL VICE *.vkm keymap precedent.
/// </summary>
/// <remarks>
/// The Kreative Software Relay Fonts Free Use License 1.2f permits free-of-charge
/// redistribution PROVIDED the license text is included verbatim and credit is given
/// to Kreative Korporation (clause 1a) - so the license file must ship byte-identical
/// beside the font and the third-party notices must carry the credit.
/// Acceptance:
///   TEST-XFONT-001a: PetMe64.ttf and PetMe-FreeLicense.txt exist under the head's
///     Assets/Fonts, and the license text is byte-identical to the VICE source copy.
///   TEST-XFONT-001b: the head csproj packages Assets/Fonts content on the UWP build.
///   TEST-XFONT-001c: the keyboard XAML sets the keycap font via the packaged file
///     with the embedded family name ("Pet Me 64").
///   TEST-XFONT-001d: THIRD_PARTY_NOTICES.md credits Kreative Korporation.
/// </remarks>
[Trait("Category", "Xbox")]
public sealed class XboxC64FontTests
{
    [Fact]
    public void FontAndLicense_AreVendored_LicenseVerbatim()
    {
        var fonts = Path.Combine(RepoRoot, "src", "ViceSharp.Xbox", "Assets", "Fonts");
        var font = Path.Combine(fonts, "PetMe64.ttf");
        var license = Path.Combine(fonts, "PetMe-FreeLicense.txt");

        Assert.True(File.Exists(font), $"Expected the C64 keycap font at '{font}'.");
        Assert.True(File.Exists(license), $"Expected the font license beside it at '{license}'.");

        // Clause 1a: the license ships VERBATIM (byte-identical to the VICE source copy).
        var source = Path.Combine(
            RepoRoot, "native", "vice", "vice", "data", "common", "PetMe-FreeLicense.txt");
        if (File.Exists(source))
        {
            Assert.Equal(File.ReadAllBytes(source), File.ReadAllBytes(license));
        }
    }

    [Fact]
    public void Csproj_PackagesTheFontAssets_OnTheUwpBuild()
    {
        var csproj = File.ReadAllText(
            Path.Combine(RepoRoot, "src", "ViceSharp.Xbox", "ViceSharp.Xbox.csproj"));

        Assert.Contains("Assets/Fonts/", csproj);
    }

    [Fact]
    public void KeyboardXaml_UsesThePackagedC64Font()
    {
        // FEAT-XKEYCAPSTYLE-001: the font now rides in the shared app-level keycap
        // style (Styles/C64Keycaps.xaml); the keyboard consumes it via the tile style.
        var xaml = File.ReadAllText(Path.Combine(
                RepoRoot, "src", "ViceSharp.Xbox", "Controls", "VirtualKeyboardOverlay.xaml"))
            .ToLowerInvariant();
        Assert.Contains("c64keycaptilestyle", xaml);

        var styles = File.ReadAllText(Path.Combine(
                RepoRoot, "src", "ViceSharp.Xbox", "Styles", "C64Keycaps.xaml"))
            .ToLowerInvariant();
        Assert.Contains("assets/fonts/petme64.ttf#pet me 64", styles);
        Assert.Contains("fontfamily", styles);
    }

    [Fact]
    public void ThirdPartyNotices_CreditKreative()
    {
        var notices = File.ReadAllText(Path.Combine(RepoRoot, "THIRD_PARTY_NOTICES.md"));

        Assert.Contains("Kreative", notices);
        Assert.Contains("PetMe64", notices);
    }

    private static string RepoRoot
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "ViceSharp.slnx")))
                directory = directory.Parent;

            if (directory is null)
                throw new InvalidOperationException("Could not locate repository root.");

            return directory.FullName;
        }
    }
}
